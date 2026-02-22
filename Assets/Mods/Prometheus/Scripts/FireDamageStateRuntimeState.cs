using System.Collections.Generic;

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

  internal class FireDamageStateRuntimeState {

    private readonly Dictionary<int, FireDamageStateSnapshot> _snapshotsByEntityId = new();

    public void SetSnapshot(int entityId, FireDamageStateSnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out FireDamageStateSnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
    }

  }
}