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
      var fuelMultiplier = entry.IsSelf ? 1f : Mathf.Max(0.2f, targetEnvironment.Fuel);
      var oxygen = targetEnvironment.EffectiveOxygen(source.Smoke);
      var emissionMultiplier = entry.IsSelf ? 1f : EmissionMultiplier(source);
      var activeHeat = Mathf.Max(source.Heat, source.IgnitionProgress * 0.65f);
      var activeEmberPressure = Mathf.Max(source.EmberPressure, source.IgnitionProgress * 0.55f);
      var activeSmoke = Mathf.Max(source.Smoke, source.IgnitionProgress * 0.35f);
      var heat = activeHeat * entry.HeatWeight * targetMultiplier * emissionMultiplier * fuelRemaining;
      var ember = activeEmberPressure * entry.EmberWeight * targetMultiplier * fuelMultiplier * emissionMultiplier * fuelRemaining;
      var smoke = activeSmoke * entry.SmokeWeight * targetMultiplier * emissionMultiplier * fuelRemaining;
      var ignition = (heat * 0.55f) + (ember * 0.85f * oxygen * fuelMultiplier);
      return new FireCellState(
        heat,
        ember,
        smoke,
        ignition,
        source.FuelConsumed,
        entry.IsSelf ? source.BurnState : FireGridBurnState.Heating);
    }

    public static FireCellState FinalizeCell(FireCellState state, FireCellEnvironment environment) {
      if (environment.IsUnderwater) {
        return FireCellState.Cold;
      }

      var oxygen = environment.EffectiveOxygen(state.Smoke);
      var heat = state.Heat * oxygen;
      var emberPressure = state.EmberPressure * oxygen;
      var smoke = Mathf.Clamp01(state.Smoke * 0.92f);
      var ignitionProgress = Mathf.Clamp01(state.IgnitionProgress * oxygen);
      var burnState = state.BurnState == FireGridBurnState.Burning
        ? FireGridBurnState.Burning
        : (ignitionProgress >= 0.35f || emberPressure >= 0.2f ? FireGridBurnState.Smoldering : FireGridBurnState.Heating);
      return new FireCellState(heat, emberPressure, smoke, ignitionProgress, state.FuelConsumed, burnState);
    }

    private static float EmissionMultiplier(FireCellState source) {
      if (source.BurnState == FireGridBurnState.Burning) {
        return 2.25f;
      }

      if (source.BurnState == FireGridBurnState.Smoldering) {
        return 1.1f;
      }

      return 0.85f;
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
