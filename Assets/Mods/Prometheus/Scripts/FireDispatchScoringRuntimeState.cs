using System.Collections.Generic;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireDispatchScoringSnapshot {

    public float CandidateScore { get; }
    public float AssignedScore { get; }
    public float SeverityFactor { get; }
    public float AssetRiskFactor { get; }
    public float TravelCostFactor { get; }
    public float ContainmentLeverageFactor { get; }
    public float AssignmentLockRemainingSeconds { get; }
    public float HysteresisThreshold { get; }
    public bool AssignmentLocked { get; }
    public bool RetargetSuppressed { get; }
    public string ResponseState { get; }
    public string TopFactor { get; }

    public FireDispatchScoringSnapshot(
      float candidateScore,
      float assignedScore,
      float severityFactor,
      float assetRiskFactor,
      float travelCostFactor,
      float containmentLeverageFactor,
      float assignmentLockRemainingSeconds,
      float hysteresisThreshold,
      bool assignmentLocked,
      bool retargetSuppressed,
      string responseState,
      string topFactor) {
      CandidateScore = candidateScore;
      AssignedScore = assignedScore;
      SeverityFactor = severityFactor;
      AssetRiskFactor = assetRiskFactor;
      TravelCostFactor = travelCostFactor;
      ContainmentLeverageFactor = containmentLeverageFactor;
      AssignmentLockRemainingSeconds = assignmentLockRemainingSeconds;
      HysteresisThreshold = hysteresisThreshold;
      AssignmentLocked = assignmentLocked;
      RetargetSuppressed = retargetSuppressed;
      ResponseState = responseState;
      TopFactor = topFactor;
    }

  }

  internal class FireDispatchScoringRuntimeState {

    private readonly Dictionary<int, FireDispatchScoringSnapshot> _snapshotsByEntityId = new();

    public void SetSnapshot(int entityId, FireDispatchScoringSnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out FireDispatchScoringSnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
    }

  }
}