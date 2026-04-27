using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireRuntimeProjectionSnapshot {

    public bool HasExposure { get; }
    public FireExposureSnapshot Exposure { get; }
    public bool HasImpact { get; }
    public FireImpactSnapshot Impact { get; }
    public bool HasDamageState { get; }
    public FireDamageStateSnapshot DamageState { get; }
    public bool HasRecovery { get; }
    public FireRecoverySnapshot Recovery { get; }

    public FireExposureSnapshot VisualExposure => HasExposure
      ? Exposure
      : FireExposureRules.CreateTerminalDeadBuildingSnapshot();

    public FireDamageStateSnapshot VisualDamageState => HasDamageState
      ? DamageState
      : FireRuntimeProjectionRules.DefaultDamageState;

    public FireRuntimeProjectionSnapshot(
      bool hasExposure,
      FireExposureSnapshot exposure,
      bool hasImpact,
      FireImpactSnapshot impact,
      bool hasDamageState,
      FireDamageStateSnapshot damageState,
      bool hasRecovery,
      FireRecoverySnapshot recovery) {
      HasExposure = hasExposure;
      Exposure = exposure;
      HasImpact = hasImpact;
      Impact = impact;
      HasDamageState = hasDamageState;
      DamageState = damageState;
      HasRecovery = hasRecovery;
      Recovery = recovery;
    }

    public FireRuntimeProjectionSnapshot WithExposure(FireExposureSnapshot exposure) =>
      new(true, exposure, HasImpact, Impact, HasDamageState, DamageState, HasRecovery, Recovery);

    public FireRuntimeProjectionSnapshot WithImpact(FireImpactSnapshot impact) =>
      new(HasExposure, Exposure, true, impact, HasDamageState, DamageState, HasRecovery, Recovery);

    public FireRuntimeProjectionSnapshot WithDamageState(FireDamageStateSnapshot damageState) =>
      new(HasExposure, Exposure, HasImpact, Impact, true, damageState, HasRecovery, Recovery);

    public FireRuntimeProjectionSnapshot WithRecovery(FireRecoverySnapshot recovery) =>
      new(HasExposure, Exposure, HasImpact, Impact, HasDamageState, DamageState, true, recovery);

  }

  internal class FireRuntimeProjectionRuntimeState : EntitySnapshotStore<FireRuntimeProjectionSnapshot> {

    public void SetExposure(int entityId, FireExposureSnapshot exposure) {
      SetSnapshot(entityId, GetOrCreate(entityId).WithExposure(exposure));
    }

    public void SetImpact(int entityId, FireImpactSnapshot impact) {
      SetSnapshot(entityId, GetOrCreate(entityId).WithImpact(impact));
    }

    public void SetDamageState(int entityId, FireDamageStateSnapshot damageState) {
      SetSnapshot(entityId, GetOrCreate(entityId).WithDamageState(damageState));
    }

    public void SetRecovery(int entityId, FireRecoverySnapshot recovery) {
      SetSnapshot(entityId, GetOrCreate(entityId).WithRecovery(recovery));
    }

    private FireRuntimeProjectionSnapshot GetOrCreate(int entityId) =>
      TryGetSnapshot(entityId, out var projection)
        ? projection
        : FireRuntimeProjectionRules.EmptyProjection;

  }

  internal static class FireRuntimeProjectionRules {

    internal static FireImpactSnapshot DefaultImpact { get; } = new(0f, 0f, 0f, 0f, 0f);

    internal static FireDamageStateSnapshot DefaultDamageState { get; } =
      new(FireDamageCategory.Unknown, FireDamageState.Healthy, 0f, 0f, 0);

    internal static FireRecoverySnapshot DefaultRecovery { get; } = new(false, 0f, 0f, 0f, 0f);

    internal static FireRuntimeProjectionSnapshot EmptyProjection { get; } = new(
      false,
      FireExposureRules.CreateColdSnapshot(),
      false,
      DefaultImpact,
      false,
      DefaultDamageState,
      false,
      DefaultRecovery);

    internal static FireImpactSnapshot CreateImpact(FireExposureSnapshot exposure, float impactMultiplier) {
      var effectiveIntensity = Mathf.Clamp01(Mathf.Max(
        exposure.Intensity,
        exposure.HeatExposure,
        exposure.EmberPressure * 0.75f));
      var dehydrationPressure = Mathf.Clamp01((exposure.HeatExposure * 0.85f) + (exposure.Smoke * 0.25f));

      return new FireImpactSnapshot(
        Mathf.Clamp01(effectiveIntensity * 0.8f * impactMultiplier),
        Mathf.Clamp01(effectiveIntensity * 0.65f * impactMultiplier),
        Mathf.Clamp01(effectiveIntensity * 0.45f * impactMultiplier),
        Mathf.Clamp01(dehydrationPressure * impactMultiplier),
        Mathf.Clamp01(((dehydrationPressure * 0.6f) + (effectiveIntensity * 0.2f)) * impactMultiplier));
    }

    internal static float GetDamagePressure(FireRuntimeProjectionSnapshot projection, FireDamageCategory category) {
      if (!projection.HasImpact) {
        return 0f;
      }

      return category switch {
        FireDamageCategory.Crop => projection.Impact.CropDamagePressure,
        FireDamageCategory.Tree => projection.Impact.TreeDamagePressure,
        FireDamageCategory.Building => projection.Impact.BuildingDamagePressure,
        _ => projection.Impact.BuildingDamagePressure,
      };
    }

    internal static float ComputeWorkplaceSpeedMultiplier(FireRuntimeProjectionSnapshot projection, bool isWorkplaceEntity) {
      if (!projection.HasImpact) {
        return 1f;
      }

      var productivityPenalty = Mathf.Clamp01(projection.Impact.BuildingDamagePressure * 0.75f);
      var baseWorkingSpeedMultiplier = Mathf.Clamp(1f - productivityPenalty, 0.2f, 1f);
      var stateWorkingSpeedMultiplier = 1f;

      if (projection.HasDamageState
          && (projection.DamageState.Category == FireDamageCategory.Building || isWorkplaceEntity)) {
        stateWorkingSpeedMultiplier = projection.DamageState.State switch {
          FireDamageState.Healthy => 1f,
          FireDamageState.Scorched => Mathf.Clamp(1f - (projection.DamageState.Severity * 0.55f), 0.45f, 0.95f),
          FireDamageState.Burning => Mathf.Clamp(1f - (projection.DamageState.Severity * 0.9f), 0.1f, 0.55f),
          FireDamageState.Dead => 0f,
          _ => 1f,
        };
      }

      return Mathf.Min(baseWorkingSpeedMultiplier, stateWorkingSpeedMultiplier);
    }

    internal static bool ShouldDisableWorkplaceOperations(FireRuntimeProjectionSnapshot projection, bool isWorkplaceEntity) =>
      isWorkplaceEntity
      && projection.HasDamageState
      && projection.DamageState.State == FireDamageState.Dead;

  }
}
