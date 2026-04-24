using System.Collections.Generic;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal enum PropagationIgnitionSourceKind {
    Spread,
    Explosion,
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
          FireTelemetry.Log($"event=explosion_ignition_request_ignored sourceId={sourceEntityId} targetId={targetEntityId} reason=invalid_ids");
        }
        return;
      }

      var clampedPropagationChance = propagationChance < 0f ? 0f : propagationChance;
      if (_spreadIgnitionRequestsByEntityId.TryGetValue(targetEntityId, out var existingRequest)) {
        if (existingRequest.PropagationChance >= clampedPropagationChance) {
          if (sourceKind == PropagationIgnitionSourceKind.Explosion || existingRequest.SourceKind == PropagationIgnitionSourceKind.Explosion) {
            FireTelemetry.Log(
              $"event=explosion_ignition_request_ignored sourceId={sourceEntityId} targetId={targetEntityId} reason=weaker_or_equal_request incomingChance={clampedPropagationChance:0.000} existingChance={existingRequest.PropagationChance:0.000} existingSourceKind={existingRequest.SourceKind}");
          }
          return;
        }

        if (sourceKind == PropagationIgnitionSourceKind.Explosion || existingRequest.SourceKind == PropagationIgnitionSourceKind.Explosion) {
          FireTelemetry.Log(
            $"event=explosion_ignition_request_replaced sourceId={sourceEntityId} targetId={targetEntityId} incomingChance={clampedPropagationChance:0.000} previousChance={existingRequest.PropagationChance:0.000} previousSourceKind={existingRequest.SourceKind}");
        }
      }

      _spreadIgnitionRequestsByEntityId[targetEntityId] = new SpreadIgnitionRequest(sourceEntityId, clampedPropagationChance, sourceKind);
      if (sourceKind == PropagationIgnitionSourceKind.Explosion) {
        FireTelemetry.Log($"event=explosion_ignition_request_queued sourceId={sourceEntityId} targetId={targetEntityId} chance={clampedPropagationChance:0.000}");
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
}
