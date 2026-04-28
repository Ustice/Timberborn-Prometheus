using System.Collections.Generic;
using System.Linq;

namespace Mods.Prometheus.Scripts {
  internal sealed class FireGridRuntimeState {

    private readonly Dictionary<FireGridChunkCoordinate, FireGridChunk> _chunks = new();
    public int TotalChunkCount => _chunks.Count;

    public int ActiveChunkCount => _chunks.Values.Count(chunk => chunk.ActiveCellCount > 0);

    public int ActiveCellCount => _chunks.Values.Sum(chunk => chunk.ActiveCellCount);

    public void SetEnvironment(FireGridCoordinate coordinate, FireCellEnvironment environment) {
      GetOrCreateChunk(coordinate).SetEnvironment(coordinate, environment);
    }

    public FireCellEnvironment GetEnvironment(FireGridCoordinate coordinate) {
      var chunkCoordinate = FireGridChunkCoordinate.FromCell(coordinate);
      return _chunks.TryGetValue(chunkCoordinate, out var chunk)
        ? chunk.GetEnvironment(coordinate)
        : FireCellEnvironment.OpenAir;
    }

    public void Inject(FireGridCoordinate coordinate, FireCellState state) {
      if (!state.IsActive) {
        return;
      }

      var chunk = GetOrCreateChunk(coordinate);
      chunk.SetState(coordinate, chunk.GetState(coordinate).Add(state));
    }

    public void Inject(FireGridSourceInjection injection) {
      Inject(injection.Coordinate, injection.State);
    }

    public bool TryGetState(FireGridCoordinate coordinate, out FireCellState state) {
      var chunkCoordinate = FireGridChunkCoordinate.FromCell(coordinate);
      if (_chunks.TryGetValue(chunkCoordinate, out var chunk) && chunk.TryGetState(coordinate, out state)) {
        return true;
      }

      state = FireCellState.Cold;
      return false;
    }

    public FireGridSample Sample(FireGridFootprint footprint) =>
      Sample(footprint.Coordinates);

    public FireGridSample Sample(IEnumerable<FireGridCoordinate> coordinates) {
      var hasAnyCoordinate = false;
      var hasActivity = false;
      var heat = 0f;
      var emberPressure = 0f;
      var smoke = 0f;
      var ignitionProgress = 0f;
      var fuelConsumed = 0f;
      var moistureDampening = 0f;
      var oxygenAvailability = 0f;
      var dominantBurnState = FireGridBurnState.Cold;
      var sourceAttribution = FireSourceAttribution.Unknown;
      var sourceStrength = 0f;
      var environmentCount = 0;

      foreach (var coordinate in coordinates) {
        hasAnyCoordinate = true;
        var environment = GetEnvironment(coordinate);
        moistureDampening += environment.Moisture;
        oxygenAvailability += environment.EffectiveOxygen(0f);
        environmentCount++;

        if (!TryGetState(coordinate, out var state) || !state.IsActive) {
          continue;
        }

        hasActivity = true;
        heat = UnityEngine.Mathf.Max(heat, state.Heat);
        emberPressure = UnityEngine.Mathf.Max(emberPressure, state.EmberPressure);
        smoke = UnityEngine.Mathf.Max(smoke, state.Smoke);
        ignitionProgress = UnityEngine.Mathf.Max(ignitionProgress, state.IgnitionProgress);
        fuelConsumed = UnityEngine.Mathf.Max(fuelConsumed, state.FuelConsumed);
        if (state.BurnState > dominantBurnState) {
          dominantBurnState = state.BurnState;
        }

        var stateSourceStrength = UnityEngine.Mathf.Max(state.Heat, state.EmberPressure, state.Smoke, state.IgnitionProgress);
        if (state.SourceAttribution.HasSource && stateSourceStrength >= sourceStrength) {
          sourceAttribution = state.SourceAttribution;
          sourceStrength = stateSourceStrength;
        }
      }

      if (!hasAnyCoordinate) {
        return FireGridSample.Empty;
      }

      return new FireGridSample(
        hasActivity,
        heat,
        emberPressure,
        smoke,
        ignitionProgress,
        fuelConsumed,
        environmentCount == 0 ? 0f : moistureDampening / environmentCount,
        environmentCount == 0 ? 1f : oxygenAvailability / environmentCount,
        dominantBurnState,
        sourceAttribution);
    }

    public void ClearCell(FireGridCoordinate coordinate) {
      var chunkCoordinate = FireGridChunkCoordinate.FromCell(coordinate);
      if (_chunks.TryGetValue(chunkCoordinate, out var chunk)) {
        chunk.ClearState(coordinate);
      }
    }

    public void Clear() {
      _chunks.Clear();
    }

    public int ApplySuppressionArea(FireSuppressionZoneSnapshot zone) {
      if (zone.Radius <= 0 || zone.Strength <= 0f) {
        return 0;
      }

      var dampedCells = 0;
      var currentEntries = _chunks.Values.SelectMany(chunk => chunk.StateEntries).ToArray();
      for (var i = 0; i < currentEntries.Length; i++) {
        var coordinate = currentEntries[i].Key;
        var state = currentEntries[i].Value;
        var strength = SuppressionStrengthAt(zone, coordinate);
        if (strength <= 0f) {
          continue;
        }

        var suppressed = FireSuppressionRules.ApplyToCell(state, strength);
        GetOrCreateChunk(coordinate).SetState(coordinate, suppressed);
        dampedCells++;
      }

      PruneInactiveChunks();
      return dampedCells;
    }

    public void Step(FireGridKernel kernel) {
      var nextStates = new Dictionary<FireGridCoordinate, FireCellState>();
      var currentEntries = _chunks.Values.SelectMany(chunk => chunk.StateEntries).ToArray();
      for (var i = 0; i < currentEntries.Length; i++) {
        var sourceCoordinate = currentEntries[i].Key;
        var sourceState = currentEntries[i].Value;
        var sourceEnvironment = GetEnvironment(sourceCoordinate);
        for (var entryIndex = 0; entryIndex < kernel.Entries.Count; entryIndex++) {
          var kernelEntry = kernel.Entries[entryIndex];
          var targetCoordinate = sourceCoordinate + kernelEntry.Offset;
          var targetEnvironment = GetEnvironment(targetCoordinate);
          var transfer = FireGridPropagationRules.Transfer(sourceState, sourceEnvironment, targetEnvironment, kernelEntry);
          if (!transfer.IsActive) {
            continue;
          }

          nextStates.TryGetValue(targetCoordinate, out var existing);
          nextStates[targetCoordinate] = existing.Add(transfer);
        }
      }

      foreach (var chunk in _chunks.Values) {
        chunk.ClearStates();
      }

      foreach (var pair in nextStates) {
        GetOrCreateChunk(pair.Key).SetState(pair.Key, FireGridPropagationRules.FinalizeCell(pair.Value, GetEnvironment(pair.Key)));
      }

      PruneInactiveChunks();
    }

    public void PruneInactiveChunks() {
      var emptyChunkKeys = _chunks
        .Where(pair => pair.Value.ActiveCellCount == 0 && pair.Value.EnvironmentCount == 0)
        .Select(pair => pair.Key)
        .ToArray();
      for (var i = 0; i < emptyChunkKeys.Length; i++) {
        _chunks.Remove(emptyChunkKeys[i]);
      }
    }

    private FireGridChunk GetOrCreateChunk(FireGridCoordinate coordinate) {
      var chunkCoordinate = FireGridChunkCoordinate.FromCell(coordinate);
      if (_chunks.TryGetValue(chunkCoordinate, out var chunk)) {
        return chunk;
      }

      chunk = new FireGridChunk();
      _chunks[chunkCoordinate] = chunk;
      return chunk;
    }

    private static float SuppressionStrengthAt(FireSuppressionZoneSnapshot zone, FireGridCoordinate coordinate) {
      var distance = UnityEngine.Mathf.Max(
        UnityEngine.Mathf.Abs(coordinate.X - zone.Center.X),
        UnityEngine.Mathf.Abs(coordinate.Y - zone.Center.Y),
        UnityEngine.Mathf.Abs(coordinate.Z - zone.Center.Z));
      if (distance > zone.Radius) {
        return 0f;
      }

      var edgeFalloff = UnityEngine.Mathf.Lerp(1f, 0.45f, distance / (float)zone.Radius);
      return UnityEngine.Mathf.Clamp01(zone.Strength * edgeFalloff);
    }

  }
}
