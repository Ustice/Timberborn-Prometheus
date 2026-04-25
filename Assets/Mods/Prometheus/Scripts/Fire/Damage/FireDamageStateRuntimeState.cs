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

  }
}
