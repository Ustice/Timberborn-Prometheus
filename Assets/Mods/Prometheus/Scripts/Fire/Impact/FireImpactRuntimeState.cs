namespace Mods.Prometheus.Scripts {
  internal readonly struct FireImpactSnapshot {

    public float CropDamagePressure { get; }
    public float TreeDamagePressure { get; }
    public float BuildingDamagePressure { get; }
    public float DehydrationPressure { get; }
    public float InjuryPressure { get; }

    public FireImpactSnapshot(
      float cropDamagePressure,
      float treeDamagePressure,
      float buildingDamagePressure,
      float dehydrationPressure,
      float injuryPressure) {
      CropDamagePressure = cropDamagePressure;
      TreeDamagePressure = treeDamagePressure;
      BuildingDamagePressure = buildingDamagePressure;
      DehydrationPressure = dehydrationPressure;
      InjuryPressure = injuryPressure;
    }

  }

  internal class FireImpactRuntimeState : EntitySnapshotStore<FireImpactSnapshot> {
  }

  internal readonly struct FireBeaverNeedDeltas {

    public float ThirstDelta { get; }
    public float HeatStressDelta { get; }

    public bool HasEffect => !UnityEngine.Mathf.Approximately(ThirstDelta, 0f)
                             || !UnityEngine.Mathf.Approximately(HeatStressDelta, 0f);

    public FireBeaverNeedDeltas(float thirstDelta, float heatStressDelta) {
      ThirstDelta = thirstDelta;
      HeatStressDelta = heatStressDelta;
    }

  }

  internal static class FireBeaverExposureRules {

    internal const float EffectRadius = 8f;
    private const float MaxThirstPenaltyPerSecond = 0.0001f;
    private const float MaxHeatStressPenaltyPerSecond = 0.0005f;
    private const float IndoorExposureMultiplier = 1f;

    internal static FireBeaverNeedDeltas ComputeProximityNeedDeltas(FireImpactSnapshot impactSnapshot, float distance) {
      return ComputeNeedDeltas(impactSnapshot, ComputeProximityMultiplier(distance, EffectRadius));
    }

    internal static FireBeaverNeedDeltas ComputeIndoorNeedDeltas(FireImpactSnapshot impactSnapshot) {
      return ComputeNeedDeltas(impactSnapshot, IndoorExposureMultiplier);
    }

    internal static float ComputeProximityMultiplier(float distance, float radius) {
      if (radius <= 0f) {
        return 0f;
      }

      return UnityEngine.Mathf.Clamp01(1f - (UnityEngine.Mathf.Max(0f, distance) / radius));
    }

    private static FireBeaverNeedDeltas ComputeNeedDeltas(FireImpactSnapshot impactSnapshot, float exposureMultiplier) {
      var clampedExposureMultiplier = UnityEngine.Mathf.Clamp01(exposureMultiplier);
      var thirstPenalty = -UnityEngine.Mathf.Clamp(
        impactSnapshot.DehydrationPressure * MaxThirstPenaltyPerSecond * clampedExposureMultiplier,
        0f,
        MaxThirstPenaltyPerSecond);
      var heatStressPenalty = -UnityEngine.Mathf.Clamp(
        (impactSnapshot.DehydrationPressure + impactSnapshot.InjuryPressure) * MaxHeatStressPenaltyPerSecond * 0.5f * clampedExposureMultiplier,
        0f,
        MaxHeatStressPenaltyPerSecond);

      return new FireBeaverNeedDeltas(thirstPenalty, heatStressPenalty);
    }

  }
}
