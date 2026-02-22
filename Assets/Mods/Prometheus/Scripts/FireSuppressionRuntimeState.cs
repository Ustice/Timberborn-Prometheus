using System.Collections.Generic;

namespace Mods.Prometheus.Scripts {
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

  internal class FireSuppressionRuntimeState {

    private readonly Dictionary<int, FireSuppressionSnapshot> _snapshotsByEntityId = new();

    public void SetSnapshot(int entityId, FireSuppressionSnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out FireSuppressionSnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
    }

  }
}