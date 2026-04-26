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
}
