using System;
using System.Collections.Generic;
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

  internal enum FireSourceKind {
    Unknown,
    DebugIgnition,
    ConfiguredSource,
    BurstSource,
    ControlledBurnSource,
  }

  internal readonly struct FireSourceAttribution : IEquatable<FireSourceAttribution> {

    public static FireSourceAttribution Unknown { get; } = new(FireSourceKind.Unknown, string.Empty);

    public FireSourceKind Kind { get; }
    public string Identity { get; }
    public bool HasSource => Kind != FireSourceKind.Unknown;

    public FireSourceAttribution(FireSourceKind kind, string identity) {
      Kind = kind;
      Identity = identity ?? string.Empty;
    }

    public static FireSourceAttribution DebugIgnition(string identity) =>
      new(FireSourceKind.DebugIgnition, identity);

    public static FireSourceAttribution ConfiguredSource(string identity) =>
      new(FireSourceKind.ConfiguredSource, identity);

    public static FireSourceAttribution BurstSource(string identity) =>
      new(FireSourceKind.BurstSource, identity);

    public static FireSourceAttribution ControlledBurnSource(string identity) =>
      new(FireSourceKind.ControlledBurnSource, identity);

    public string ToTelemetryToken() {
      if (Kind == FireSourceKind.Unknown) {
        return "Grid";
      }

      var kindToken = Kind.ToString();
      return string.IsNullOrWhiteSpace(Identity) ? kindToken : $"{kindToken}:{Identity}";
    }

    public bool Equals(FireSourceAttribution other) =>
      Kind == other.Kind && string.Equals(Identity, other.Identity, StringComparison.Ordinal);

    public override bool Equals(object obj) => obj is FireSourceAttribution other && Equals(other);

    public override int GetHashCode() {
      unchecked {
        return ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(Identity ?? string.Empty);
      }
    }

    public override string ToString() => ToTelemetryToken();

  }

  internal readonly struct FireGridSourceInjection {

    public FireGridCoordinate Coordinate { get; }
    public FireCellState State { get; }
    public FireSourceAttribution SourceAttribution { get; }

    public FireGridSourceInjection(
      FireGridCoordinate coordinate,
      FireCellState state,
      FireSourceAttribution sourceAttribution) {
      Coordinate = coordinate;
      SourceAttribution = sourceAttribution;
      State = state.WithSource(sourceAttribution);
    }

    public static FireGridSourceInjection DebugIgnition(
      FireGridCoordinate coordinate,
      FireCellState state,
      string identity) =>
      new(coordinate, state, FireSourceAttribution.DebugIgnition(identity));

    public static FireGridSourceInjection ConfiguredSource(
      FireGridCoordinate coordinate,
      FireCellState state,
      string identity) =>
      new(coordinate, state, FireSourceAttribution.ConfiguredSource(identity));

    public static FireGridSourceInjection BurstSource(
      FireGridCoordinate coordinate,
      FireCellState state,
      string identity) =>
      new(coordinate, state, FireSourceAttribution.BurstSource(identity));

    public static FireGridSourceInjection ControlledBurnSource(
      FireGridCoordinate coordinate,
      FireCellState state,
      string identity) =>
      new(coordinate, state, FireSourceAttribution.ControlledBurnSource(identity));

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

    public bool IsUnderwater => WaterDepth > FireGridPropagationPolicy.WaterSuppressionDepth || StructureKind == FireGridStructureKind.Water;

    public FireCellEnvironment(
      FireGridStructureKind structureKind,
      float fuel,
      float moisture,
      float barrier,
      float oxygenAvailability,
      float waterDepth,
      int exposedFaceMask) {
      StructureKind = structureKind;
      Fuel = Mathf.Clamp(fuel, 0f, FireGridPropagationPolicy.MaximumEnvironmentFuel);
      Moisture = Mathf.Clamp01(moisture);
      Barrier = Mathf.Clamp01(barrier);
      OxygenAvailability = Mathf.Clamp01(oxygenAvailability);
      WaterDepth = Mathf.Max(0f, waterDepth);
      ExposedFaceMask = exposedFaceMask;
    }

    public float TransferMultiplier =>
      FireGridPropagationPolicy.TransferMultiplier(this);

    public float EffectiveOxygen(float smoke) =>
      FireGridPropagationPolicy.EffectiveOxygen(this, smoke);

  }

  internal readonly struct FireCellState {

    public static FireCellState Cold { get; } = new(0f, 0f, 0f, 0f, 0f, FireGridBurnState.Cold, FireSourceAttribution.Unknown);

    public float Heat { get; }
    public float EmberPressure { get; }
    public float Smoke { get; }
    public float IgnitionProgress { get; }
    public float FuelConsumed { get; }
    public FireGridBurnState BurnState { get; }
    public FireSourceAttribution SourceAttribution { get; }
    public bool IsActive =>
      Heat > FireGridPropagationPolicy.ActiveCellThreshold
      || EmberPressure > FireGridPropagationPolicy.ActiveCellThreshold
      || IgnitionProgress > FireGridPropagationPolicy.ActiveCellThreshold;

    public FireCellState(
      float heat,
      float emberPressure,
      float smoke,
      float ignitionProgress,
      float fuelConsumed,
      FireGridBurnState burnState,
      FireSourceAttribution sourceAttribution = default) {
      Heat = Mathf.Clamp01(heat);
      EmberPressure = Mathf.Clamp01(emberPressure);
      Smoke = Mathf.Clamp01(smoke);
      IgnitionProgress = Mathf.Clamp01(ignitionProgress);
      FuelConsumed = Mathf.Clamp01(fuelConsumed);
      BurnState = burnState;
      SourceAttribution = sourceAttribution;
    }

    public FireCellState Add(FireCellState other) =>
      With(
        Heat + other.Heat,
        EmberPressure + other.EmberPressure,
        Smoke + other.Smoke,
        IgnitionProgress + other.IgnitionProgress,
        FuelConsumed + other.FuelConsumed,
        null,
        DominantSource(this, other));

    public FireCellState With(
      float? heat = null,
      float? emberPressure = null,
      float? smoke = null,
      float? ignitionProgress = null,
      float? fuelConsumed = null,
      FireGridBurnState? burnState = null,
      FireSourceAttribution? sourceAttribution = null) =>
      new(
        heat ?? Heat,
        emberPressure ?? EmberPressure,
        smoke ?? Smoke,
        ignitionProgress ?? IgnitionProgress,
        fuelConsumed ?? FuelConsumed,
        burnState ?? FireGridPropagationPolicy.BurnStateFromValues(
          heat ?? Heat,
          emberPressure ?? EmberPressure,
          ignitionProgress ?? IgnitionProgress),
        sourceAttribution ?? SourceAttribution);

    public FireCellState WithSource(FireSourceAttribution sourceAttribution) =>
      With(sourceAttribution: sourceAttribution);

    private static FireSourceAttribution DominantSource(FireCellState first, FireCellState second) {
      if (!first.SourceAttribution.HasSource) {
        return second.SourceAttribution;
      }

      if (!second.SourceAttribution.HasSource) {
        return first.SourceAttribution;
      }

      return SourceStrength(second) > SourceStrength(first)
        ? second.SourceAttribution
        : first.SourceAttribution;
    }

    private static float SourceStrength(FireCellState state) =>
      Mathf.Max(state.Heat, state.EmberPressure, state.Smoke, state.IgnitionProgress);

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
      FireGridBurnState.Cold,
      FireSourceAttribution.Unknown);

    public bool HasActivity { get; }
    public float Heat { get; }
    public float EmberPressure { get; }
    public float Smoke { get; }
    public float IgnitionProgress { get; }
    public float FuelConsumed { get; }
    public float MoistureDampening { get; }
    public float OxygenAvailability { get; }
    public FireGridBurnState DominantBurnState { get; }
    public FireSourceAttribution SourceAttribution { get; }
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
      FireGridBurnState dominantBurnState,
      FireSourceAttribution sourceAttribution = default) {
      HasActivity = hasActivity;
      Heat = Mathf.Clamp01(heat);
      EmberPressure = Mathf.Clamp01(emberPressure);
      Smoke = Mathf.Clamp01(smoke);
      IgnitionProgress = Mathf.Clamp01(ignitionProgress);
      FuelConsumed = Mathf.Clamp01(fuelConsumed);
      MoistureDampening = Mathf.Clamp01(moistureDampening);
      OxygenAvailability = Mathf.Clamp01(oxygenAvailability);
      DominantBurnState = dominantBurnState;
      SourceAttribution = sourceAttribution;
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
}
