using System.Collections.Generic;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal enum PropagationIgnitionSourceKind {
    Spread,
    Explosion,
  }

  internal static class FireTelemetryEvents {

    public const string BeaverEffectApiMissing = "beaver_effect_api_missing";
    public const string BeaverEffectApiResolved = "beaver_effect_api_resolved";
    public const string BeaverEffectNeedManagerScan = "beaver_effect_need_manager_scan";
    public const string BuildingOperationsRestored = "building_operations_restored";
    public const string BuildingOperationsSuppressed = "building_operations_suppressed";
    public const string BurningTick = "burning_tick";
    public const string DeadBuildingFireTerminal = "dead_building_fire_terminal";
    public const string DebugClearBeaverFireEffects = "debug_clear_beaver_fire_effects";
    public const string DebugClearBeaverFireEffectsResult = "debug_clear_beaver_fire_effects_result";
    public const string DebugIgniteApplied = "debug_ignite_applied";
    public const string DebugIgniteRequest = "debug_ignite_request";
    public const string DebugResetFireSimulation = "debug_reset_fire_simulation";
    public const string DebugStopAllFires = "debug_stop_all_fires";
    public const string DebugStopAllFiresResult = "debug_stop_all_fires_result";
    public const string DebugViewFocus = "debug_view_focus";
    public const string EntityDestroyCleanup = "entity_destroy_cleanup";
    public const string ExplosionDetonated = "explosion_detonated";
    public const string ExplosionIgniteApplied = "explosion_ignite_applied";
    public const string ExplosionIgniteNotApplied = "explosion_ignite_not_applied";
    public const string ExplosionIgnitionRequest = "explosion_ignition_request";
    public const string ExplosionIgnitionRequestConsumed = "explosion_ignition_request_consumed";
    public const string ExplosionIgnitionRequestIgnored = "explosion_ignition_request_ignored";
    public const string ExplosionIgnitionRequestQueued = "explosion_ignition_request_queued";
    public const string ExplosionIgnitionRequestReplaced = "explosion_ignition_request_replaced";
    public const string Extinguished = "extinguished";
    public const string Ignited = "ignited";
    public const string NativeVisualEffectResolved = "native_visual_effect_resolved";
    public const string NativeVisualEffectUnavailable = "native_visual_effect_unavailable";
    public const string PreviewExcluded = "preview_excluded";
    public const string ResponseState = "response_state";
    public const string SpreadIgniteApplied = "spread_ignite_applied";
    public const string SpreadIgnitionRequestConsumed = "spread_ignition_request_consumed";
    public const string SpreadPropagation = "spread_propagation";
    public const string VisualPreviewApply = "visual_preview_apply";
    public const string VisualPreviewClear = "visual_preview_clear";
    public const string VisualTuningJson = "visual_tuning_json";
    public const string WorkplaceIndoorExposure = "workplace_indoor_exposure";
    public const string WorkplaceSpeedApiResolved = "workplace_speed_api_resolved";
    public const string WorkplaceSpeedPenaltyState = "workplace_speed_penalty_state";
    public const string WorkplaceSupportRestored = "workplace_support_restored";
    public const string WorkplaceSupportSuppressed = "workplace_support_suppressed";

    public static readonly string[] All = {
      BeaverEffectApiMissing,
      BeaverEffectApiResolved,
      BeaverEffectNeedManagerScan,
      BuildingOperationsRestored,
      BuildingOperationsSuppressed,
      BurningTick,
      DeadBuildingFireTerminal,
      DebugClearBeaverFireEffects,
      DebugClearBeaverFireEffectsResult,
      DebugIgniteApplied,
      DebugIgniteRequest,
      DebugResetFireSimulation,
      DebugStopAllFires,
      DebugStopAllFiresResult,
      DebugViewFocus,
      EntityDestroyCleanup,
      ExplosionDetonated,
      ExplosionIgniteApplied,
      ExplosionIgniteNotApplied,
      ExplosionIgnitionRequest,
      ExplosionIgnitionRequestConsumed,
      ExplosionIgnitionRequestIgnored,
      ExplosionIgnitionRequestQueued,
      ExplosionIgnitionRequestReplaced,
      Extinguished,
      Ignited,
      NativeVisualEffectResolved,
      NativeVisualEffectUnavailable,
      PreviewExcluded,
      ResponseState,
      SpreadIgniteApplied,
      SpreadIgnitionRequestConsumed,
      SpreadPropagation,
      VisualPreviewApply,
      VisualPreviewClear,
      VisualTuningJson,
      WorkplaceIndoorExposure,
      WorkplaceSpeedApiResolved,
      WorkplaceSpeedPenaltyState,
      WorkplaceSupportRestored,
      WorkplaceSupportSuppressed,
    };

  }

  internal readonly struct SpreadIgnitionRequest {

    public int SourceEntityId { get; }
    public float PropagationChance { get; }
    public PropagationIgnitionSourceKind SourceKind { get; }

    public SpreadIgnitionRequest(int sourceEntityId, float propagationChance, PropagationIgnitionSourceKind sourceKind) {
      SourceEntityId = sourceEntityId;
      PropagationChance = propagationChance;
      SourceKind = sourceKind;
    }

  }

  internal readonly struct FireSimulationSnapshot {

    public bool Burning { get; }
    public float Intensity { get; }
    public float HeatExposure { get; }
    public float QuenchingPower { get; }
    public float SpreadPressure { get; }
    public float NeighborSpreadPressure { get; }
    public float IgnitionChance { get; }
    public string DominantIgnitionSource { get; }
    public float WeatherIgnitionContribution { get; }
    public float IndustrialIgnitionContribution { get; }
    public float FireworksIgnitionContribution { get; }
    public float ControlledBurnIgnitionContribution { get; }
    public float ExplosionIgnitionContribution { get; }
    public float DrynessFactor { get; }
    public float FuelFactor { get; }
    public float BarrierFactor { get; }

    public FireSimulationSnapshot(
      bool burning,
      float intensity,
      float heatExposure,
      float quenchingPower,
      float spreadPressure,
      float neighborSpreadPressure,
      float ignitionChance,
      string dominantIgnitionSource,
      float weatherIgnitionContribution,
      float industrialIgnitionContribution,
      float fireworksIgnitionContribution,
      float controlledBurnIgnitionContribution,
      float explosionIgnitionContribution,
      float drynessFactor,
      float fuelFactor,
      float barrierFactor) {
      Burning = burning;
      Intensity = intensity;
      HeatExposure = heatExposure;
      QuenchingPower = quenchingPower;
      SpreadPressure = spreadPressure;
      NeighborSpreadPressure = neighborSpreadPressure;
      IgnitionChance = ignitionChance;
      DominantIgnitionSource = dominantIgnitionSource;
      WeatherIgnitionContribution = weatherIgnitionContribution;
      IndustrialIgnitionContribution = industrialIgnitionContribution;
      FireworksIgnitionContribution = fireworksIgnitionContribution;
      ControlledBurnIgnitionContribution = controlledBurnIgnitionContribution;
      ExplosionIgnitionContribution = explosionIgnitionContribution;
      DrynessFactor = drynessFactor;
      FuelFactor = fuelFactor;
      BarrierFactor = barrierFactor;
    }

  }

  internal class FireSimulationRuntimeState {

    private readonly Dictionary<int, FireSimulationSnapshot> _snapshotsByEntityId = new();
    private readonly HashSet<int> _forcedIgnitionEntityIds = new();
    private readonly Dictionary<int, SpreadIgnitionRequest> _spreadIgnitionRequestsByEntityId = new();
    private float _debugIgnitionSuppressedUntilRealtime;

    public int SnapshotCount => _snapshotsByEntityId.Count;
    public int PendingForcedIgnitionCount => _forcedIgnitionEntityIds.Count;
    public int PendingSpreadIgnitionCount => _spreadIgnitionRequestsByEntityId.Count;
    public bool DebugIgnitionSuppressed => UnityEngine.Time.realtimeSinceStartup < _debugIgnitionSuppressedUntilRealtime;
    public float DebugIgnitionSuppressionRemainingSeconds => UnityEngine.Mathf.Max(0f, _debugIgnitionSuppressedUntilRealtime - UnityEngine.Time.realtimeSinceStartup);

    public void SetSnapshot(int entityId, FireSimulationSnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out FireSimulationSnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
    }

    public void RemoveSnapshot(int entityId) {
      _snapshotsByEntityId.Remove(entityId);
      _forcedIgnitionEntityIds.Remove(entityId);
      _spreadIgnitionRequestsByEntityId.Remove(entityId);
    }

    public void ClearSnapshotsAndIgnitionRequests() {
      _snapshotsByEntityId.Clear();
      _forcedIgnitionEntityIds.Clear();
      _spreadIgnitionRequestsByEntityId.Clear();
    }

    public void RequestForcedIgnition(int entityId) {
      if (entityId == 0) {
        return;
      }

      _forcedIgnitionEntityIds.Add(entityId);
    }

    public bool ConsumeForcedIgnitionRequest(int entityId) {
      return _forcedIgnitionEntityIds.Remove(entityId);
    }

    public void RequestSpreadIgnition(int targetEntityId, int sourceEntityId, float propagationChance, PropagationIgnitionSourceKind sourceKind = PropagationIgnitionSourceKind.Spread) {
      if (targetEntityId == 0 || sourceEntityId == 0 || targetEntityId == sourceEntityId) {
        if (sourceKind == PropagationIgnitionSourceKind.Explosion) {
          FireTelemetry.Log($"event={FireTelemetryEvents.ExplosionIgnitionRequestIgnored} sourceId={sourceEntityId} targetId={targetEntityId} reason=invalid_ids");
        }
        return;
      }

      var clampedPropagationChance = propagationChance < 0f ? 0f : propagationChance;
      if (_spreadIgnitionRequestsByEntityId.TryGetValue(targetEntityId, out var existingRequest)) {
        if (existingRequest.PropagationChance >= clampedPropagationChance) {
          if (sourceKind == PropagationIgnitionSourceKind.Explosion || existingRequest.SourceKind == PropagationIgnitionSourceKind.Explosion) {
            FireTelemetry.Log(
              $"event={FireTelemetryEvents.ExplosionIgnitionRequestIgnored} sourceId={sourceEntityId} targetId={targetEntityId} reason=weaker_or_equal_request incomingChance={clampedPropagationChance:0.000} existingChance={existingRequest.PropagationChance:0.000} existingSourceKind={existingRequest.SourceKind}");
          }
          return;
        }

        if (sourceKind == PropagationIgnitionSourceKind.Explosion || existingRequest.SourceKind == PropagationIgnitionSourceKind.Explosion) {
          FireTelemetry.Log(
            $"event={FireTelemetryEvents.ExplosionIgnitionRequestReplaced} sourceId={sourceEntityId} targetId={targetEntityId} incomingChance={clampedPropagationChance:0.000} previousChance={existingRequest.PropagationChance:0.000} previousSourceKind={existingRequest.SourceKind}");
        }
      }

      _spreadIgnitionRequestsByEntityId[targetEntityId] = new SpreadIgnitionRequest(sourceEntityId, clampedPropagationChance, sourceKind);
      if (sourceKind == PropagationIgnitionSourceKind.Explosion) {
        FireTelemetry.Log($"event={FireTelemetryEvents.ExplosionIgnitionRequestQueued} sourceId={sourceEntityId} targetId={targetEntityId} chance={clampedPropagationChance:0.000}");
      }
    }

    public bool ConsumeSpreadIgnitionRequest(int entityId, out SpreadIgnitionRequest request) {
      if (_spreadIgnitionRequestsByEntityId.TryGetValue(entityId, out request)) {
        _spreadIgnitionRequestsByEntityId.Remove(entityId);
        return true;
      }

      request = default;
      return false;
    }

    public int ExtinguishAllBurning() {
      var extinguishedCount = 0;
      var entityIds = new List<int>(_snapshotsByEntityId.Keys);

      for (var i = 0; i < entityIds.Count; i++) {
        var entityId = entityIds[i];
        var snapshot = _snapshotsByEntityId[entityId];
        if (!snapshot.Burning && snapshot.Intensity <= 0f) {
          continue;
        }

        _snapshotsByEntityId[entityId] = new FireSimulationSnapshot(
          false,
          0f,
          0f,
          snapshot.QuenchingPower,
          0f,
          snapshot.NeighborSpreadPressure,
          snapshot.IgnitionChance,
          "ForcedExtinguish",
          snapshot.WeatherIgnitionContribution,
          snapshot.IndustrialIgnitionContribution,
          snapshot.FireworksIgnitionContribution,
          snapshot.ControlledBurnIgnitionContribution,
          snapshot.ExplosionIgnitionContribution,
          snapshot.DrynessFactor,
          snapshot.FuelFactor,
          snapshot.BarrierFactor);
        extinguishedCount++;
      }

      _forcedIgnitionEntityIds.Clear();
      _spreadIgnitionRequestsByEntityId.Clear();
      return extinguishedCount;
    }

    public void SuppressDebugIgnitionsForSeconds(float durationInSeconds) {
      _debugIgnitionSuppressedUntilRealtime = UnityEngine.Mathf.Max(
        _debugIgnitionSuppressedUntilRealtime,
        UnityEngine.Time.realtimeSinceStartup + UnityEngine.Mathf.Max(0f, durationInSeconds));
      _spreadIgnitionRequestsByEntityId.Clear();
    }

  }

  internal static class FireSimulationRules {

    internal static FireSimulationSnapshot CreateTerminalDeadBuildingSnapshot() {
      return new FireSimulationSnapshot(
        false,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        "DeadBuilding",
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f);
    }

    internal static string DetermineResponseState(bool burning, float intensity, float spreadPressure, float quenchingPower) {
      if (!burning) {
        return "Stabilized";
      }

      if (intensity >= 0.6f && spreadPressure > (quenchingPower * 1.05f)) {
        return "Overwhelmed";
      }

      return quenchingPower + 0.0001f >= (spreadPressure * 1.2f) && intensity <= 0.45f ? "Contained" : "Stabilized";
    }

    internal static float ComputeQuenchingPower(
      bool burning,
      float suppressionPower,
      float waterEfficiency,
      float localQuenchingBonus,
      string factionApproach,
      float intensity,
      float localWaterExposure,
      float quenchingMultiplier,
      bool explosionDisrupted) {
      if (!burning) {
        return 0f;
      }

      var quenchingPower = (UnityEngine.Mathf.Max(0f, suppressionPower) * 0.035f)
                           + (UnityEngine.Mathf.Max(0f, waterEfficiency) * 0.02f)
                           + UnityEngine.Mathf.Max(0f, localQuenchingBonus);
      var normalizedFactionApproach = factionApproach ?? string.Empty;
      if (normalizedFactionApproach.Contains("Folktails", System.StringComparison.OrdinalIgnoreCase)
          || normalizedFactionApproach.Contains("BucketBrigade", System.StringComparison.OrdinalIgnoreCase)) {
        var relayDistancePenalty = UnityEngine.Mathf.Clamp01(1f - localWaterExposure);
        var relayEfficiency = UnityEngine.Mathf.Clamp(1f - (relayDistancePenalty * 0.45f), 0.55f, 1.2f);
        quenchingPower *= relayEfficiency;
      } else if (normalizedFactionApproach.Contains("Ironteeth", System.StringComparison.OrdinalIgnoreCase)
                 || normalizedFactionApproach.Contains("IronTeeth", System.StringComparison.OrdinalIgnoreCase)
                 || normalizedFactionApproach.Contains("IndustrialControl", System.StringComparison.OrdinalIgnoreCase)) {
        var highHeatSuppressionBonus = UnityEngine.Mathf.Clamp01(intensity * 1.1f) * 0.35f;
        quenchingPower *= 1f + highHeatSuppressionBonus;
      }

      quenchingPower *= UnityEngine.Mathf.Max(0f, quenchingMultiplier);
      return explosionDisrupted ? quenchingPower * 0.65f : quenchingPower;
    }

    internal static FireDispatchDecision ComputeDispatchDecision(
      float intensity,
      float spreadPressure,
      float heatExposure,
      float neighborSpreadPressure,
      float impactPressure,
      float localWaterExposure,
      float waterEfficiency,
      float drynessFactor,
      float dispatchSeverityWeight,
      float dispatchAssetRiskWeight,
      float dispatchTravelCostWeight,
      float dispatchContainmentLeverageWeight,
      float assignedScore,
      float assignmentLockRemainingSeconds,
      float assignmentLockDurationInSeconds,
      float hysteresisThreshold) {
      var severityFactor = UnityEngine.Mathf.Clamp01((intensity * 0.65f) + (spreadPressure * 0.35f));
      var assetRiskFactor = UnityEngine.Mathf.Clamp01((impactPressure * 0.8f) + (heatExposure * 0.2f));
      var travelCostFactor = UnityEngine.Mathf.Clamp01((1f - localWaterExposure) * 0.55f + (1f - UnityEngine.Mathf.Clamp01(waterEfficiency)) * 0.45f);
      var containmentLeverageFactor = UnityEngine.Mathf.Clamp01((neighborSpreadPressure * 0.7f) + (drynessFactor * 0.3f));

      NormalizeDispatchWeights(
        ref dispatchSeverityWeight,
        ref dispatchAssetRiskWeight,
        ref dispatchTravelCostWeight,
        ref dispatchContainmentLeverageWeight);

      var candidateScore =
        (severityFactor * dispatchSeverityWeight)
        + (assetRiskFactor * dispatchAssetRiskWeight)
        + (containmentLeverageFactor * dispatchContainmentLeverageWeight)
        - (travelCostFactor * dispatchTravelCostWeight);
      candidateScore = UnityEngine.Mathf.Max(0f, candidateScore);

      var clampedAssignedScore = UnityEngine.Mathf.Max(0f, assignedScore);
      var clampedHysteresisThreshold = UnityEngine.Mathf.Max(0f, hysteresisThreshold);
      var clampedAssignmentLockDuration = UnityEngine.Mathf.Max(0f, assignmentLockDurationInSeconds);
      var assignmentLocked = assignmentLockRemainingSeconds > 0f;
      var scoreDelta = candidateScore - clampedAssignedScore;
      var retargetSuppressed = false;
      var nextAssignedScore = clampedAssignedScore;
      var nextAssignmentLockRemainingSeconds = assignmentLockRemainingSeconds;

      if (nextAssignedScore <= 0.0001f) {
        nextAssignedScore = candidateScore;
        nextAssignmentLockRemainingSeconds = clampedAssignmentLockDuration;
      } else if (assignmentLocked) {
        retargetSuppressed = scoreDelta >= clampedHysteresisThreshold;
        nextAssignedScore = UnityEngine.Mathf.Lerp(nextAssignedScore, candidateScore, 0.08f);
      } else if (scoreDelta >= clampedHysteresisThreshold) {
        nextAssignedScore = candidateScore;
        nextAssignmentLockRemainingSeconds = clampedAssignmentLockDuration;
      } else {
        retargetSuppressed = scoreDelta > 0f;
        nextAssignedScore = UnityEngine.Mathf.Lerp(nextAssignedScore, candidateScore, 0.12f);
      }

      return new FireDispatchDecision(
        candidateScore,
        UnityEngine.Mathf.Max(0f, nextAssignedScore),
        nextAssignmentLockRemainingSeconds,
        clampedHysteresisThreshold,
        assignmentLocked,
        retargetSuppressed,
        severityFactor,
        assetRiskFactor,
        travelCostFactor,
        containmentLeverageFactor);
    }

    private static void NormalizeDispatchWeights(
      ref float severityWeight,
      ref float assetRiskWeight,
      ref float travelCostWeight,
      ref float containmentLeverageWeight) {
      severityWeight = UnityEngine.Mathf.Max(0f, severityWeight);
      assetRiskWeight = UnityEngine.Mathf.Max(0f, assetRiskWeight);
      travelCostWeight = UnityEngine.Mathf.Max(0f, travelCostWeight);
      containmentLeverageWeight = UnityEngine.Mathf.Max(0f, containmentLeverageWeight);

      var weightSum = severityWeight + assetRiskWeight + travelCostWeight + containmentLeverageWeight;
      if (weightSum < 0.001f) {
        severityWeight = 0.4f;
        assetRiskWeight = 0.3f;
        travelCostWeight = 0.2f;
        containmentLeverageWeight = 0.25f;
        weightSum = severityWeight + assetRiskWeight + travelCostWeight + containmentLeverageWeight;
      }

      severityWeight /= weightSum;
      assetRiskWeight /= weightSum;
      travelCostWeight /= weightSum;
      containmentLeverageWeight /= weightSum;
    }

  }

  internal readonly struct FireDispatchDecision {

    public float CandidateScore { get; }
    public float AssignedScore { get; }
    public float AssignmentLockRemainingSeconds { get; }
    public float HysteresisThreshold { get; }
    public bool AssignmentLocked { get; }
    public bool RetargetSuppressed { get; }
    public float SeverityFactor { get; }
    public float AssetRiskFactor { get; }
    public float TravelCostFactor { get; }
    public float ContainmentLeverageFactor { get; }

    public FireDispatchDecision(
      float candidateScore,
      float assignedScore,
      float assignmentLockRemainingSeconds,
      float hysteresisThreshold,
      bool assignmentLocked,
      bool retargetSuppressed,
      float severityFactor,
      float assetRiskFactor,
      float travelCostFactor,
      float containmentLeverageFactor) {
      CandidateScore = candidateScore;
      AssignedScore = assignedScore;
      AssignmentLockRemainingSeconds = assignmentLockRemainingSeconds;
      HysteresisThreshold = hysteresisThreshold;
      AssignmentLocked = assignmentLocked;
      RetargetSuppressed = retargetSuppressed;
      SeverityFactor = severityFactor;
      AssetRiskFactor = assetRiskFactor;
      TravelCostFactor = travelCostFactor;
      ContainmentLeverageFactor = containmentLeverageFactor;
    }

  }
}
