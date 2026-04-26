using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireGridCoordinate : IEquatable<FireGridCoordinate> {

    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    public FireGridCoordinate(int x, int y, int z) {
      X = x;
      Y = y;
      Z = z;
    }

    public static FireGridCoordinate operator +(FireGridCoordinate left, FireGridOffset right) =>
      new(left.X + right.Dx, left.Y + right.Dy, left.Z + right.Dz);

    public bool Equals(FireGridCoordinate other) => X == other.X && Y == other.Y && Z == other.Z;

    public override bool Equals(object obj) => obj is FireGridCoordinate other && Equals(other);

    public override int GetHashCode() {
      unchecked {
        var hash = X;
        hash = (hash * 397) ^ Y;
        hash = (hash * 397) ^ Z;
        return hash;
      }
    }

    public override string ToString() => $"{X},{Y},{Z}";

  }

  internal readonly struct FireGridOffset {

    public int Dx { get; }
    public int Dy { get; }
    public int Dz { get; }

    public FireGridOffset(int dx, int dy, int dz) {
      Dx = dx;
      Dy = dy;
      Dz = dz;
    }

  }

  internal readonly struct FireGridChunkCoordinate : IEquatable<FireGridChunkCoordinate> {

    public const int Size = 8;

    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    public FireGridChunkCoordinate(int x, int y, int z) {
      X = x;
      Y = y;
      Z = z;
    }

    public static FireGridChunkCoordinate FromCell(FireGridCoordinate coordinate) =>
      new(FloorDiv(coordinate.X, Size), FloorDiv(coordinate.Y, Size), FloorDiv(coordinate.Z, Size));

    public static int LocalIndex(FireGridCoordinate coordinate) {
      var localX = Mod(coordinate.X, Size);
      var localY = Mod(coordinate.Y, Size);
      var localZ = Mod(coordinate.Z, Size);
      return localX + (localY * Size) + (localZ * Size * Size);
    }

    public bool Equals(FireGridChunkCoordinate other) => X == other.X && Y == other.Y && Z == other.Z;

    public override bool Equals(object obj) => obj is FireGridChunkCoordinate other && Equals(other);

    public override int GetHashCode() {
      unchecked {
        var hash = X;
        hash = (hash * 397) ^ Y;
        hash = (hash * 397) ^ Z;
        return hash;
      }
    }

    private static int FloorDiv(int value, int divisor) {
      var quotient = value / divisor;
      var remainder = value % divisor;
      return remainder != 0 && ((remainder < 0) != (divisor < 0)) ? quotient - 1 : quotient;
    }

    private static int Mod(int value, int divisor) {
      var result = value % divisor;
      return result < 0 ? result + divisor : result;
    }

  }

  internal enum FireGridStructureKind {
    Unknown,
    Air,
    Terrain,
    Building,
    Vegetation,
    Water,
    Barrier,
  }

  internal enum FireGridBurnState {
    Cold,
    Heating,
    Smoldering,
    Burning,
    Cooling,
    BurnedOut,
  }

  internal readonly struct FireCellEnvironment {

    public static FireCellEnvironment OpenAir { get; } = new(
      FireGridStructureKind.Air,
      0f,
      0f,
      0f,
      1f,
      0f,
      FireGridExposedFaces.All);

    public FireGridStructureKind StructureKind { get; }
    public float Fuel { get; }
    public float Moisture { get; }
    public float Barrier { get; }
    public float OxygenAvailability { get; }
    public float WaterDepth { get; }
    public int ExposedFaceMask { get; }

    public bool IsUnderwater => WaterDepth > 0.05f || StructureKind == FireGridStructureKind.Water;

    public FireCellEnvironment(
      FireGridStructureKind structureKind,
      float fuel,
      float moisture,
      float barrier,
      float oxygenAvailability,
      float waterDepth,
      int exposedFaceMask) {
      StructureKind = structureKind;
      Fuel = Mathf.Clamp(fuel, 0f, 2f);
      Moisture = Mathf.Clamp01(moisture);
      Barrier = Mathf.Clamp01(barrier);
      OxygenAvailability = Mathf.Clamp01(oxygenAvailability);
      WaterDepth = Mathf.Max(0f, waterDepth);
      ExposedFaceMask = exposedFaceMask;
    }

    public float TransferMultiplier =>
      IsUnderwater ? 0f : Mathf.Clamp01((1f - Moisture) * (1f - Barrier));

    public float EffectiveOxygen(float smoke) =>
      IsUnderwater ? 0f : Mathf.Clamp01(OxygenAvailability - (Mathf.Clamp01(smoke) * 0.35f));

  }

  internal readonly struct FireCellState {

    public static FireCellState Cold { get; } = new(0f, 0f, 0f, 0f, 0f, FireGridBurnState.Cold);

    public float Heat { get; }
    public float EmberPressure { get; }
    public float Smoke { get; }
    public float IgnitionProgress { get; }
    public float FuelConsumed { get; }
    public FireGridBurnState BurnState { get; }
    public bool IsActive => Heat > 0.001f || EmberPressure > 0.001f || Smoke > 0.001f || IgnitionProgress > 0.001f;

    public FireCellState(
      float heat,
      float emberPressure,
      float smoke,
      float ignitionProgress,
      float fuelConsumed,
      FireGridBurnState burnState) {
      Heat = Mathf.Clamp01(heat);
      EmberPressure = Mathf.Clamp01(emberPressure);
      Smoke = Mathf.Clamp01(smoke);
      IgnitionProgress = Mathf.Clamp01(ignitionProgress);
      FuelConsumed = Mathf.Clamp01(fuelConsumed);
      BurnState = burnState;
    }

    public FireCellState Add(FireCellState other) =>
      With(
        Heat + other.Heat,
        EmberPressure + other.EmberPressure,
        Smoke + other.Smoke,
        IgnitionProgress + other.IgnitionProgress,
        FuelConsumed + other.FuelConsumed);

    public FireCellState With(
      float? heat = null,
      float? emberPressure = null,
      float? smoke = null,
      float? ignitionProgress = null,
      float? fuelConsumed = null,
      FireGridBurnState? burnState = null) =>
      new(
        heat ?? Heat,
        emberPressure ?? EmberPressure,
        smoke ?? Smoke,
        ignitionProgress ?? IgnitionProgress,
        fuelConsumed ?? FuelConsumed,
        burnState ?? burnStateFromValues(heat ?? Heat, emberPressure ?? EmberPressure, ignitionProgress ?? IgnitionProgress));

    private static FireGridBurnState burnStateFromValues(float heat, float emberPressure, float ignitionProgress) {
      if (ignitionProgress >= 0.75f && heat >= 0.35f) {
        return FireGridBurnState.Burning;
      }

      if (ignitionProgress >= 0.35f || emberPressure >= 0.2f) {
        return FireGridBurnState.Smoldering;
      }

      return heat > 0.001f ? FireGridBurnState.Heating : FireGridBurnState.Cold;
    }

  }

  internal readonly struct FireGridSample {

    public static FireGridSample Empty { get; } = new(
      false,
      0f,
      0f,
      0f,
      0f,
      0f,
      0f,
      1f,
      FireGridBurnState.Cold);

    public bool HasActivity { get; }
    public float Heat { get; }
    public float EmberPressure { get; }
    public float Smoke { get; }
    public float IgnitionProgress { get; }
    public float FuelConsumed { get; }
    public float MoistureDampening { get; }
    public float OxygenAvailability { get; }
    public FireGridBurnState DominantBurnState { get; }
    public bool Burning => DominantBurnState == FireGridBurnState.Burning || DominantBurnState == FireGridBurnState.Smoldering;

    public FireGridSample(
      bool hasActivity,
      float heat,
      float emberPressure,
      float smoke,
      float ignitionProgress,
      float fuelConsumed,
      float moistureDampening,
      float oxygenAvailability,
      FireGridBurnState dominantBurnState) {
      HasActivity = hasActivity;
      Heat = Mathf.Clamp01(heat);
      EmberPressure = Mathf.Clamp01(emberPressure);
      Smoke = Mathf.Clamp01(smoke);
      IgnitionProgress = Mathf.Clamp01(ignitionProgress);
      FuelConsumed = Mathf.Clamp01(fuelConsumed);
      MoistureDampening = Mathf.Clamp01(moistureDampening);
      OxygenAvailability = Mathf.Clamp01(oxygenAvailability);
      DominantBurnState = dominantBurnState;
    }

  }

  internal readonly struct FireGridFootprint {

    public IReadOnlyList<FireGridCoordinate> Coordinates { get; }
    public FireGridCoordinate PrimaryCoordinate { get; }

    public FireGridFootprint(IReadOnlyList<FireGridCoordinate> coordinates, FireGridCoordinate primaryCoordinate) {
      Coordinates = coordinates;
      PrimaryCoordinate = primaryCoordinate;
    }

  }

  internal static class FireGridFootprintSampler {

    private const int MaxFootprintCells = 512;

    internal static FireGridFootprint FromWorldPosition(Vector3 position) {
      var coordinate = new FireGridCoordinate(
        Mathf.RoundToInt(position.x),
        Mathf.RoundToInt(position.y),
        Mathf.RoundToInt(position.z));
      return new FireGridFootprint(new[] { coordinate }, coordinate);
    }

    internal static FireGridFootprint FromBounds(Bounds bounds) {
      if (bounds.size.sqrMagnitude <= 0.0001f) {
        return FromWorldPosition(bounds.center);
      }

      var coordinates = new List<FireGridCoordinate>();
      var minX = Mathf.FloorToInt(bounds.min.x);
      var minY = Mathf.FloorToInt(bounds.min.y);
      var minZ = Mathf.FloorToInt(bounds.min.z);
      var maxX = Mathf.Max(minX, Mathf.CeilToInt(bounds.max.x) - 1);
      var maxY = Mathf.Max(minY, Mathf.CeilToInt(bounds.max.y) - 1);
      var maxZ = Mathf.Max(minZ, Mathf.CeilToInt(bounds.max.z) - 1);

      for (var x = minX; x <= maxX; x++) {
        for (var y = minY; y <= maxY; y++) {
          for (var z = minZ; z <= maxZ; z++) {
            coordinates.Add(new FireGridCoordinate(x, y, z));
            if (coordinates.Count >= MaxFootprintCells) {
              return new FireGridFootprint(coordinates, CreatePrimaryCoordinate(bounds.center));
            }
          }
        }
      }

      return new FireGridFootprint(coordinates, CreatePrimaryCoordinate(bounds.center));
    }

    private static FireGridCoordinate CreatePrimaryCoordinate(Vector3 position) =>
      new(
        Mathf.RoundToInt(position.x),
        Mathf.RoundToInt(position.y),
        Mathf.RoundToInt(position.z));

  }

  internal readonly struct FireGridKernelEntry {

    public FireGridOffset Offset { get; }
    public float HeatWeight { get; }
    public float EmberWeight { get; }
    public float SmokeWeight { get; }
    public bool IsSelf => Offset.Dx == 0 && Offset.Dy == 0 && Offset.Dz == 0;

    public FireGridKernelEntry(FireGridOffset offset, float heatWeight, float emberWeight, float smokeWeight) {
      Offset = offset;
      HeatWeight = Mathf.Max(0f, heatWeight);
      EmberWeight = Mathf.Max(0f, emberWeight);
      SmokeWeight = Mathf.Max(0f, smokeWeight);
    }

  }

  internal sealed class FireGridKernel {

    public static FireGridKernel Full27 { get; } = CreateFull27();

    public IReadOnlyList<FireGridKernelEntry> Entries { get; }

    private FireGridKernel(IReadOnlyList<FireGridKernelEntry> entries) {
      Entries = entries;
    }

    private static FireGridKernel CreateFull27() {
      var entries = new List<FireGridKernelEntry>(27);
      for (var dx = -1; dx <= 1; dx++) {
        for (var dy = -1; dy <= 1; dy++) {
          for (var dz = -1; dz <= 1; dz++) {
            entries.Add(CreateEntry(dx, dy, dz));
          }
        }
      }

      return new FireGridKernel(entries);
    }

    private static FireGridKernelEntry CreateEntry(int dx, int dy, int dz) {
      if (dx == 0 && dy == 0 && dz == 0) {
        return new FireGridKernelEntry(new FireGridOffset(dx, dy, dz), 0.72f, 0.60f, 0.55f);
      }

      var manhattan = Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz);
      var distancePenalty = 1f / manhattan;
      var upward = dy > 0 ? 1.8f : dy < 0 ? 0.35f : 1f;
      var smokeUpward = dy > 0 ? 2.2f : dy < 0 ? 0.08f : 0.35f;
      var emberVertical = dy > 0 ? 0.7f : dy < 0 ? 0.25f : 1f;
      return new FireGridKernelEntry(
        new FireGridOffset(dx, dy, dz),
        0.08f * distancePenalty * upward,
        0.06f * distancePenalty * emberVertical,
        0.07f * distancePenalty * smokeUpward);
    }

  }

  internal sealed class FireGridRuntimeState {

    private readonly Dictionary<FireGridChunkCoordinate, FireGridChunk> _chunks = new();
    private int _lastSteppedFrame = -1;

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
        heat = Mathf.Max(heat, state.Heat);
        emberPressure = Mathf.Max(emberPressure, state.EmberPressure);
        smoke = Mathf.Max(smoke, state.Smoke);
        ignitionProgress = Mathf.Max(ignitionProgress, state.IgnitionProgress);
        fuelConsumed = Mathf.Max(fuelConsumed, state.FuelConsumed);
        if (state.BurnState > dominantBurnState) {
          dominantBurnState = state.BurnState;
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
        dominantBurnState);
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
          var transfer = Transfer(sourceState, sourceEnvironment, targetEnvironment, kernelEntry);
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
        GetOrCreateChunk(pair.Key).SetState(pair.Key, FinalizeCell(pair.Value, GetEnvironment(pair.Key)));
      }

      PruneInactiveChunks();
    }

    public void StepOncePerFrame(int frame, FireGridKernel kernel) {
      if (_lastSteppedFrame == frame) {
        return;
      }

      _lastSteppedFrame = frame;
      Step(kernel);
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

    private static FireCellState Transfer(
      FireCellState source,
      FireCellEnvironment sourceEnvironment,
      FireCellEnvironment targetEnvironment,
      FireGridKernelEntry entry) {
      if (!entry.IsSelf && !FacesAllowTransfer(sourceEnvironment, targetEnvironment, entry.Offset)) {
        return FireCellState.Cold;
      }

      var targetMultiplier = entry.IsSelf ? 1f : targetEnvironment.TransferMultiplier;
      if (targetMultiplier <= 0f) {
        return FireCellState.Cold;
      }

      var fuelRemaining = Mathf.Clamp01(1f - source.FuelConsumed);
      var fuelMultiplier = entry.IsSelf ? 1f : Mathf.Max(0.2f, targetEnvironment.Fuel);
      var oxygen = targetEnvironment.EffectiveOxygen(source.Smoke);
      var emissionMultiplier = entry.IsSelf ? 1f : EmissionMultiplier(source);
      var activeHeat = Mathf.Max(source.Heat, source.IgnitionProgress * 0.65f);
      var activeEmberPressure = Mathf.Max(source.EmberPressure, source.IgnitionProgress * 0.55f);
      var activeSmoke = Mathf.Max(source.Smoke, source.IgnitionProgress * 0.35f);
      var heat = activeHeat * entry.HeatWeight * targetMultiplier * emissionMultiplier * fuelRemaining;
      var ember = activeEmberPressure * entry.EmberWeight * targetMultiplier * fuelMultiplier * emissionMultiplier * fuelRemaining;
      var smoke = activeSmoke * entry.SmokeWeight * targetMultiplier * emissionMultiplier * fuelRemaining;
      var ignition = (heat * 0.55f) + (ember * 0.85f * oxygen * fuelMultiplier);
      return new FireCellState(
        heat,
        ember,
        smoke,
        ignition,
        source.FuelConsumed,
        entry.IsSelf ? source.BurnState : FireGridBurnState.Heating);
    }

    private static FireCellState FinalizeCell(FireCellState state, FireCellEnvironment environment) {
      if (environment.IsUnderwater) {
        return FireCellState.Cold;
      }

      var oxygen = environment.EffectiveOxygen(state.Smoke);
      var heat = state.Heat * oxygen;
      var emberPressure = state.EmberPressure * oxygen;
      var smoke = Mathf.Clamp01(state.Smoke * 0.92f);
      var ignitionProgress = Mathf.Clamp01(state.IgnitionProgress * oxygen);
      var burnState = state.BurnState == FireGridBurnState.Burning
        ? FireGridBurnState.Burning
        : (ignitionProgress >= 0.35f || emberPressure >= 0.2f ? FireGridBurnState.Smoldering : FireGridBurnState.Heating);
      return new FireCellState(heat, emberPressure, smoke, ignitionProgress, state.FuelConsumed, burnState);
    }

    private static float EmissionMultiplier(FireCellState source) {
      if (source.BurnState == FireGridBurnState.Burning) {
        return 2.25f;
      }

      if (source.BurnState == FireGridBurnState.Smoldering) {
        return 1.1f;
      }

      return 0.85f;
    }

    private static bool FacesAllowTransfer(
      FireCellEnvironment sourceEnvironment,
      FireCellEnvironment targetEnvironment,
      FireGridOffset offset) {
      var sourceMask = sourceEnvironment.ExposedFaceMask == FireGridExposedFaces.None
        ? FireGridExposedFaces.None
        : sourceEnvironment.ExposedFaceMask;
      var targetMask = targetEnvironment.ExposedFaceMask == FireGridExposedFaces.None
        ? FireGridExposedFaces.None
        : targetEnvironment.ExposedFaceMask;
      return HasFace(sourceMask, offset) && HasFace(targetMask, new FireGridOffset(-offset.Dx, -offset.Dy, -offset.Dz));
    }

    private static bool HasFace(int faceMask, FireGridOffset offset) {
      var requiredFaces = FireGridExposedFaces.None;
      if (offset.Dx < 0) {
        requiredFaces |= FireGridExposedFaces.NegativeX;
      } else if (offset.Dx > 0) {
        requiredFaces |= FireGridExposedFaces.PositiveX;
      }

      if (offset.Dy < 0) {
        requiredFaces |= FireGridExposedFaces.NegativeY;
      } else if (offset.Dy > 0) {
        requiredFaces |= FireGridExposedFaces.PositiveY;
      }

      if (offset.Dz < 0) {
        requiredFaces |= FireGridExposedFaces.NegativeZ;
      } else if (offset.Dz > 0) {
        requiredFaces |= FireGridExposedFaces.PositiveZ;
      }

      return requiredFaces == FireGridExposedFaces.None || (faceMask & requiredFaces) == requiredFaces;
    }

    private sealed class FireGridChunk {

      private readonly Dictionary<int, FireCellState> _statesByLocalIndex = new();
      private readonly Dictionary<int, FireCellEnvironment> _environmentsByLocalIndex = new();

      public int ActiveCellCount => _statesByLocalIndex.Count(pair => pair.Value.IsActive);

      public int EnvironmentCount => _environmentsByLocalIndex.Count;

      public IEnumerable<KeyValuePair<FireGridCoordinate, FireCellState>> StateEntries =>
        _statesByLocalIndex.Select(pair => new KeyValuePair<FireGridCoordinate, FireCellState>(_coordinatesByLocalIndex[pair.Key], pair.Value));

      private readonly Dictionary<int, FireGridCoordinate> _coordinatesByLocalIndex = new();

      public FireCellState GetState(FireGridCoordinate coordinate) {
        var index = FireGridChunkCoordinate.LocalIndex(coordinate);
        return _statesByLocalIndex.TryGetValue(index, out var state) ? state : FireCellState.Cold;
      }

      public bool TryGetState(FireGridCoordinate coordinate, out FireCellState state) {
        var index = FireGridChunkCoordinate.LocalIndex(coordinate);
        return _statesByLocalIndex.TryGetValue(index, out state);
      }

      public void SetState(FireGridCoordinate coordinate, FireCellState state) {
        var index = FireGridChunkCoordinate.LocalIndex(coordinate);
        if (!state.IsActive) {
          ClearState(coordinate);
          return;
        }

        _statesByLocalIndex[index] = state;
        _coordinatesByLocalIndex[index] = coordinate;
      }

      public void ClearState(FireGridCoordinate coordinate) {
        var index = FireGridChunkCoordinate.LocalIndex(coordinate);
        _statesByLocalIndex.Remove(index);
        if (!_environmentsByLocalIndex.ContainsKey(index)) {
          _coordinatesByLocalIndex.Remove(index);
        }
      }

      public void ClearStates() {
        _statesByLocalIndex.Clear();
        var stateOnlyCoordinates = _coordinatesByLocalIndex
          .Where(pair => !_environmentsByLocalIndex.ContainsKey(pair.Key))
          .Select(pair => pair.Key)
          .ToArray();
        for (var i = 0; i < stateOnlyCoordinates.Length; i++) {
          _coordinatesByLocalIndex.Remove(stateOnlyCoordinates[i]);
        }
      }

      public FireCellEnvironment GetEnvironment(FireGridCoordinate coordinate) {
        var index = FireGridChunkCoordinate.LocalIndex(coordinate);
        return _environmentsByLocalIndex.TryGetValue(index, out var environment) ? environment : FireCellEnvironment.OpenAir;
      }

      public void SetEnvironment(FireGridCoordinate coordinate, FireCellEnvironment environment) {
        var index = FireGridChunkCoordinate.LocalIndex(coordinate);
        _environmentsByLocalIndex[index] = environment;
        _coordinatesByLocalIndex[index] = coordinate;
      }

    }

  }
}
