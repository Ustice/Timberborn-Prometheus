using System.Collections.Generic;

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

    public void SetSnapshot(int entityId, FireSimulationSnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out FireSimulationSnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
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
        return;
      }

      var clampedPropagationChance = propagationChance < 0f ? 0f : propagationChance;
      if (_spreadIgnitionRequestsByEntityId.TryGetValue(targetEntityId, out var existingRequest)) {
        if (existingRequest.PropagationChance >= clampedPropagationChance) {
          return;
        }
      }

      _spreadIgnitionRequestsByEntityId[targetEntityId] = new SpreadIgnitionRequest(sourceEntityId, clampedPropagationChance, sourceKind);
    }

    public bool ConsumeSpreadIgnitionRequest(int entityId, out SpreadIgnitionRequest request) {
      if (_spreadIgnitionRequestsByEntityId.TryGetValue(entityId, out request)) {
        _spreadIgnitionRequestsByEntityId.Remove(entityId);
        return true;
      }

      request = default;
      return false;
    }

  }
}