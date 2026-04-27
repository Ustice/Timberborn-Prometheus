using System.Collections.Generic;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireConfiguredSourceSpec {

    public float HeatSourceIntensity { get; }
    public float EmberSourceIntensity { get; }
    public float SmokeSourceIntensity { get; }
    public float SourceRadius { get; }
    public bool RequiresOperation { get; }
    public bool HasSource =>
      HeatSourceIntensity > FireGridPropagationPolicy.ActiveCellThreshold
      || EmberSourceIntensity > FireGridPropagationPolicy.ActiveCellThreshold
      || SmokeSourceIntensity > FireGridPropagationPolicy.ActiveCellThreshold;

    public FireConfiguredSourceSpec(
      float heatSourceIntensity,
      float emberSourceIntensity,
      float smokeSourceIntensity,
      float sourceRadius,
      bool requiresOperation) {
      HeatSourceIntensity = Mathf.Clamp01(heatSourceIntensity);
      EmberSourceIntensity = Mathf.Clamp01(emberSourceIntensity);
      SmokeSourceIntensity = Mathf.Clamp01(smokeSourceIntensity);
      SourceRadius = Mathf.Max(0f, sourceRadius);
      RequiresOperation = requiresOperation;
    }

  }

  internal static class FireConfiguredSourceInjector {

    internal static bool ShouldInject(FireConfiguredSourceSpec spec, TimberbornOperationState operationState) {
      if (!spec.HasSource) {
        return false;
      }

      return !spec.RequiresOperation || operationState == TimberbornOperationState.Active;
    }

    internal static IReadOnlyList<FireGridSourceInjection> CreateInjections(
      FireGridFootprint footprint,
      FireConfiguredSourceSpec spec,
      string identity) {
      if (!spec.HasSource || footprint.Coordinates.Count == 0) {
        return new FireGridSourceInjection[0];
      }

      var sourceCoordinates = CreateSourceCoordinates(footprint, spec.SourceRadius);
      var injections = new List<FireGridSourceInjection>(sourceCoordinates.Count);
      foreach (var pair in sourceCoordinates) {
        var attenuation = pair.Value;
        var state = CreateSourceCell(spec, attenuation);
        if (!state.IsActive) {
          continue;
        }

        injections.Add(FireGridSourceInjection.ConfiguredSource(pair.Key, state, identity));
      }

      return injections;
    }

    internal static FireCellState CreateSourceCell(FireConfiguredSourceSpec spec, float attenuation) {
      var effectiveAttenuation = Mathf.Clamp01(attenuation);
      var heat = spec.HeatSourceIntensity * effectiveAttenuation;
      var ember = spec.EmberSourceIntensity * effectiveAttenuation;
      var smoke = spec.SmokeSourceIntensity * effectiveAttenuation;
      var ignitionProgress = Mathf.Clamp01(
        (heat * FireGridPropagationPolicy.IgnitionHeatWeight)
        + (ember * FireGridPropagationPolicy.IgnitionEmberWeight));
      return new FireCellState(
        heat,
        ember,
        smoke,
        ignitionProgress,
        0f,
        FireGridPropagationPolicy.BurnStateFromValues(heat, ember, ignitionProgress));
    }

    private static Dictionary<FireGridCoordinate, float> CreateSourceCoordinates(
      FireGridFootprint footprint,
      float sourceRadius) {
      var coordinates = new Dictionary<FireGridCoordinate, float>();
      var radius = Mathf.Max(0f, sourceRadius);
      var cellRadius = Mathf.CeilToInt(radius);
      for (var footprintIndex = 0; footprintIndex < footprint.Coordinates.Count; footprintIndex++) {
        var source = footprint.Coordinates[footprintIndex];
        for (var dx = -cellRadius; dx <= cellRadius; dx++) {
          for (var dy = -cellRadius; dy <= cellRadius; dy++) {
            for (var dz = -cellRadius; dz <= cellRadius; dz++) {
              var distance = Mathf.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
              if (distance > radius) {
                continue;
              }

              var coordinate = new FireGridCoordinate(source.X + dx, source.Y + dy, source.Z + dz);
              var attenuation = radius <= 0f
                ? 1f
                : Mathf.Clamp01(1f - (distance / (radius + 1f)));
              if (!coordinates.TryGetValue(coordinate, out var existing) || attenuation > existing) {
                coordinates[coordinate] = attenuation;
              }
            }
          }
        }
      }

      return coordinates;
    }

  }
}
