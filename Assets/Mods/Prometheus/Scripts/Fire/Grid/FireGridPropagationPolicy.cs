using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal static class FireGridPropagationPolicy {

    public const float ActiveCellThreshold = 0.001f;
    public const float MaximumEnvironmentFuel = 2f;
    public const float WaterSuppressionDepth = 0.05f;
    public const float SmokeOxygenSuppression = 0.35f;

    public const float SelfHeatWeight = 0.72f;
    public const float SelfEmberWeight = 0.60f;
    public const float SelfSmokeWeight = 0.55f;

    public const float NeighborHeatBaseWeight = 0.08f;
    public const float NeighborEmberBaseWeight = 0.06f;
    public const float NeighborSmokeBaseWeight = 0.07f;

    public const float UpwardHeatMultiplier = 1.8f;
    public const float DownwardHeatMultiplier = 0.35f;
    public const float LateralHeatMultiplier = 1f;

    public const float UpwardSmokeMultiplier = 2.2f;
    public const float DownwardSmokeMultiplier = 0.08f;
    public const float LateralSmokeMultiplier = 0.35f;

    public const float UpwardEmberMultiplier = 0.7f;
    public const float DownwardEmberMultiplier = 0.25f;
    public const float OutwardEmberMultiplier = 1f;

    public const float MinimumTargetFuelMultiplier = 0.2f;
    public const float IgnitionHeatWeight = 0.55f;
    public const float IgnitionEmberWeight = 0.85f;
    public const float IgnitionHeatFromProgress = 0.65f;
    public const float IgnitionEmberFromProgress = 0.55f;
    public const float IgnitionSmokeFromProgress = 0.35f;
    public const float SmokeDecayMultiplier = 0.92f;
    public const float SmolderIgnitionThreshold = 0.35f;
    public const float SmolderEmberThreshold = 0.2f;
    public const float BurningIgnitionThreshold = 0.75f;
    public const float BurningHeatThreshold = 0.35f;

    public const float BurningEmissionMultiplier = 2.25f;
    public const float SmolderingEmissionMultiplier = 1.1f;
    public const float HeatingEmissionMultiplier = 0.85f;

    public static float TransferMultiplier(FireCellEnvironment environment) =>
      environment.IsUnderwater ? 0f : Mathf.Clamp01((1f - environment.Moisture) * (1f - environment.Barrier));

    public static float EffectiveOxygen(FireCellEnvironment environment, float smoke) =>
      environment.IsUnderwater
        ? 0f
        : Mathf.Clamp01(environment.OxygenAvailability - (Mathf.Clamp01(smoke) * SmokeOxygenSuppression));

    public static float DistancePenalty(int dx, int dy, int dz) =>
      1f / (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz));

    public static float HeatDirectionMultiplier(int dy) {
      if (dy > 0) {
        return UpwardHeatMultiplier;
      }

      return dy < 0 ? DownwardHeatMultiplier : LateralHeatMultiplier;
    }

    public static float SmokeDirectionMultiplier(int dy) {
      if (dy > 0) {
        return UpwardSmokeMultiplier;
      }

      return dy < 0 ? DownwardSmokeMultiplier : LateralSmokeMultiplier;
    }

    public static float EmberDirectionMultiplier(int dy) {
      if (dy > 0) {
        return UpwardEmberMultiplier;
      }

      return dy < 0 ? DownwardEmberMultiplier : OutwardEmberMultiplier;
    }

    public static float EmissionMultiplier(FireCellState source) {
      if (source.BurnState == FireGridBurnState.Burning) {
        return BurningEmissionMultiplier;
      }

      if (source.BurnState == FireGridBurnState.Smoldering) {
        return SmolderingEmissionMultiplier;
      }

      return HeatingEmissionMultiplier;
    }

    public static FireGridBurnState BurnStateFromValues(float heat, float emberPressure, float ignitionProgress) {
      if (ignitionProgress >= BurningIgnitionThreshold && heat >= BurningHeatThreshold) {
        return FireGridBurnState.Burning;
      }

      if (ignitionProgress >= SmolderIgnitionThreshold || emberPressure >= SmolderEmberThreshold) {
        return FireGridBurnState.Smoldering;
      }

      return heat > ActiveCellThreshold ? FireGridBurnState.Heating : FireGridBurnState.Cold;
    }

  }
}
