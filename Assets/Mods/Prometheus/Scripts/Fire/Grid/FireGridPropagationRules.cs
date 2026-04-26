using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal static class FireGridPropagationRules {

    public static FireCellState Transfer(
      FireCellState source,
      FireCellEnvironment sourceEnvironment,
      FireCellEnvironment targetEnvironment,
      FireGridKernelEntry entry) {
      if (!entry.IsSelf && !FacesAllowTransfer(sourceEnvironment, targetEnvironment, entry.Offset)) {
        return FireCellState.Cold;
      }

      var targetMultiplier = entry.IsSelf ? 1f : targetEnvironment.TransferMultiplier;
      if (targetMultiplier <= 0f) {
        return FireCellState.Cold;
      }

      var fuelRemaining = Mathf.Clamp01(1f - source.FuelConsumed);
      var fuelMultiplier = entry.IsSelf ? 1f : Mathf.Max(FireGridPropagationPolicy.MinimumTargetFuelMultiplier, targetEnvironment.Fuel);
      var oxygen = targetEnvironment.EffectiveOxygen(source.Smoke);
      var emissionMultiplier = entry.IsSelf ? 1f : FireGridPropagationPolicy.EmissionMultiplier(source);
      var activeHeat = Mathf.Max(source.Heat, source.IgnitionProgress * FireGridPropagationPolicy.IgnitionHeatFromProgress);
      var activeEmberPressure = Mathf.Max(source.EmberPressure, source.IgnitionProgress * FireGridPropagationPolicy.IgnitionEmberFromProgress);
      var activeSmoke = Mathf.Max(source.Smoke, source.IgnitionProgress * FireGridPropagationPolicy.IgnitionSmokeFromProgress);
      var heat = activeHeat * entry.HeatWeight * targetMultiplier * emissionMultiplier * fuelRemaining;
      var ember = activeEmberPressure * entry.EmberWeight * targetMultiplier * fuelMultiplier * emissionMultiplier * fuelRemaining;
      var smoke = activeSmoke * entry.SmokeWeight * targetMultiplier * emissionMultiplier * fuelRemaining;
      var ignition = (heat * FireGridPropagationPolicy.IgnitionHeatWeight)
        + (ember * FireGridPropagationPolicy.IgnitionEmberWeight * oxygen * fuelMultiplier);
      return new FireCellState(
        heat,
        ember,
        smoke,
        ignition,
        source.FuelConsumed,
        entry.IsSelf ? source.BurnState : FireGridBurnState.Heating,
        source.SourceAttribution);
    }

    public static FireCellState FinalizeCell(FireCellState state, FireCellEnvironment environment) {
      if (environment.IsUnderwater) {
        return FireCellState.Cold;
      }

      var oxygen = environment.EffectiveOxygen(state.Smoke);
      var heat = state.Heat * oxygen;
      var emberPressure = state.EmberPressure * oxygen;
      var smoke = Mathf.Clamp01(state.Smoke * FireGridPropagationPolicy.SmokeDecayMultiplier);
      var ignitionProgress = Mathf.Clamp01(state.IgnitionProgress * oxygen);
      var burnState = state.BurnState == FireGridBurnState.Burning
        ? FireGridBurnState.Burning
        : FireGridPropagationPolicy.BurnStateFromValues(heat, emberPressure, ignitionProgress);
      return new FireCellState(heat, emberPressure, smoke, ignitionProgress, state.FuelConsumed, burnState, state.SourceAttribution);
    }

    private static bool FacesAllowTransfer(
      FireCellEnvironment sourceEnvironment,
      FireCellEnvironment targetEnvironment,
      FireGridOffset offset) {
      var sourceMask = sourceEnvironment.ExposedFaceMask == FireGridExposedFaces.None
        ? FireGridExposedFaces.None
        : sourceEnvironment.ExposedFaceMask;
      var targetMask = targetEnvironment.ExposedFaceMask == FireGridExposedFaces.None
        ? FireGridExposedFaces.None
        : targetEnvironment.ExposedFaceMask;
      return HasFace(sourceMask, offset) && HasFace(targetMask, new FireGridOffset(-offset.Dx, -offset.Dy, -offset.Dz));
    }

    private static bool HasFace(int faceMask, FireGridOffset offset) {
      var requiredFaces = FireGridExposedFaces.None;
      if (offset.Dx < 0) {
        requiredFaces |= FireGridExposedFaces.NegativeX;
      } else if (offset.Dx > 0) {
        requiredFaces |= FireGridExposedFaces.PositiveX;
      }

      if (offset.Dy < 0) {
        requiredFaces |= FireGridExposedFaces.NegativeY;
      } else if (offset.Dy > 0) {
        requiredFaces |= FireGridExposedFaces.PositiveY;
      }

      if (offset.Dz < 0) {
        requiredFaces |= FireGridExposedFaces.NegativeZ;
      } else if (offset.Dz > 0) {
        requiredFaces |= FireGridExposedFaces.PositiveZ;
      }

      return requiredFaces == FireGridExposedFaces.None || (faceMask & requiredFaces) == requiredFaces;
    }

  }
}
