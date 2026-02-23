namespace Mods.Prometheus.Scripts {
  internal class EntitySnapshotStore<TSnapshot> {

    private readonly System.Collections.Generic.Dictionary<int, TSnapshot> _snapshotsByEntityId = new();

    public void SetSnapshot(int entityId, TSnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out TSnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
    }

    public int SnapshotCount => _snapshotsByEntityId.Count;

    public void RemoveSnapshot(int entityId) {
      _snapshotsByEntityId.Remove(entityId);
    }

  }

  internal static class TickGate {

    public static bool ShouldRun(ref float elapsedSeconds, float intervalSeconds, bool shouldThrottle = true) {
      elapsedSeconds += UnityEngine.Time.deltaTime;
      if (shouldThrottle && elapsedSeconds < intervalSeconds) {
        return false;
      }

      elapsedSeconds = 0f;
      return true;
    }

  }

  internal readonly struct FireSuppressionSnapshot {

    public string FactionApproach { get; }
    public float SuppressionPower { get; }
    public float HeatMitigation { get; }
    public float WaterEfficiency { get; }
    public float AssignmentLockDurationInSeconds { get; }
    public float RetargetHysteresisThreshold { get; }

    public FireSuppressionSnapshot(
      string factionApproach,
      float suppressionPower,
      float heatMitigation,
      float waterEfficiency,
      float assignmentLockDurationInSeconds,
      float retargetHysteresisThreshold) {
      FactionApproach = factionApproach;
      SuppressionPower = suppressionPower;
      HeatMitigation = heatMitigation;
      WaterEfficiency = waterEfficiency;
      AssignmentLockDurationInSeconds = assignmentLockDurationInSeconds;
      RetargetHysteresisThreshold = retargetHysteresisThreshold;
    }

  }

  internal class FireSuppressionRuntimeState : EntitySnapshotStore<FireSuppressionSnapshot> {
  }
}