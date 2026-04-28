namespace Mods.Prometheus.Scripts {
  internal enum FireDamageCategory {
    Unknown,
    Crop,
    Tree,
    Building,
  }

  internal enum FireDamageState {
    Healthy,
    Scorched,
    Burning,
    Dead,
  }

  internal enum FireNaturalResourceVisualStage {
    Healthy,
    Dried,
    DriedAndCharred,
    DeadAndCharred,
    StumpAndCharred,
  }

  internal readonly struct FireDamageStateSnapshot {

    public FireDamageCategory Category { get; }
    public FireDamageState State { get; }
    public float Severity { get; }
    public float TickProgress { get; }
    public int DamageTicksApplied { get; }

    public FireDamageStateSnapshot(
      FireDamageCategory category,
      FireDamageState state,
      float severity,
      float tickProgress,
      int damageTicksApplied) {
      Category = category;
      State = state;
      Severity = severity;
      TickProgress = tickProgress;
      DamageTicksApplied = damageTicksApplied;
    }

  }

  internal class FireDamageStateRuntimeState : EntitySnapshotStore<FireDamageStateSnapshot> {
  }

  internal static class FireDamageStateRules {

    internal static FireDamageState DetermineState(float severity) {
      if (severity >= 0.95f) {
        return FireDamageState.Dead;
      }

      if (severity >= 0.6f) {
        return FireDamageState.Burning;
      }

      if (severity >= 0.2f) {
        return FireDamageState.Scorched;
      }

      return FireDamageState.Healthy;
    }

    internal static bool IsTerminalDeadState(
      FireDamageCategory category,
      FireDamageState previousState) =>
      previousState == FireDamageState.Dead
      && (category == FireDamageCategory.Tree || category == FireDamageCategory.Building);

  }

  internal static class FireNaturalResourceVisualRules {

    private const float DriedMoistureDampeningThreshold = 0.35f;
    private const float CharredFuelConsumedThreshold = 0.08f;
    private const float DeadFuelConsumedThreshold = 0.18f;
    internal const float StumpFuelConsumedThreshold = 0.45f;

    internal static FireNaturalResourceVisualStage DetermineTreeStage(
      FireDamageStateSnapshot damageState,
      FireExposureSnapshot exposure) {
      if (damageState.Category != FireDamageCategory.Tree) {
        return FireNaturalResourceVisualStage.Healthy;
      }

      if (IsBurnedOut(exposure)) {
        return FireNaturalResourceVisualStage.StumpAndCharred;
      }

      if (damageState.State == FireDamageState.Dead
          || exposure.FuelConsumed >= DeadFuelConsumedThreshold) {
        return FireNaturalResourceVisualStage.DeadAndCharred;
      }

      if (damageState.State is FireDamageState.Scorched or FireDamageState.Burning
          || damageState.Severity >= 0.2f
          || exposure.FuelConsumed >= CharredFuelConsumedThreshold) {
        return FireNaturalResourceVisualStage.DriedAndCharred;
      }

      if (exposure.MoistureDampening <= DriedMoistureDampeningThreshold) {
        return FireNaturalResourceVisualStage.Dried;
      }

      return FireNaturalResourceVisualStage.Healthy;
    }

    internal static bool UsesDriedVisual(FireNaturalResourceVisualStage stage) =>
      stage != FireNaturalResourceVisualStage.Healthy;

    internal static bool UsesStumpVisual(FireNaturalResourceVisualStage stage) =>
      stage == FireNaturalResourceVisualStage.StumpAndCharred;

    internal static bool UsesDeadVisual(FireNaturalResourceVisualStage stage) =>
      stage is FireNaturalResourceVisualStage.DeadAndCharred
        or FireNaturalResourceVisualStage.StumpAndCharred;

    internal static FireNaturalResourceVisualStage ClampToLatchedTreeStage(
      FireNaturalResourceVisualStage stage,
      bool reachedDeadVisualStage,
      bool reachedStumpVisualStage) {
      if (reachedStumpVisualStage) {
        return FireNaturalResourceVisualStage.StumpAndCharred;
      }

      if (reachedDeadVisualStage && stage < FireNaturalResourceVisualStage.DeadAndCharred) {
        return FireNaturalResourceVisualStage.DeadAndCharred;
      }

      return stage;
    }

    internal static string ModelStateName(FireNaturalResourceVisualStage stage) =>
      stage switch {
        FireNaturalResourceVisualStage.Dried => "#Dying",
        FireNaturalResourceVisualStage.DriedAndCharred => "#Dying",
        FireNaturalResourceVisualStage.DeadAndCharred => "#Dead",
        FireNaturalResourceVisualStage.StumpAndCharred => "#Leftover",
        _ => "#Alive",
      };

    private static bool IsBurnedOut(FireExposureSnapshot exposure) =>
      exposure.FuelConsumed >= StumpFuelConsumedThreshold
      || exposure.DominantSource == "BurnedOut";

  }
}
