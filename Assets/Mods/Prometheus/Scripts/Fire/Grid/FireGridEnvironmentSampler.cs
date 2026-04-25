using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal static class FireGridExposedFaces {

    public const int None = 0;
    public const int NegativeX = 1 << 0;
    public const int PositiveX = 1 << 1;
    public const int NegativeY = 1 << 2;
    public const int PositiveY = 1 << 3;
    public const int NegativeZ = 1 << 4;
    public const int PositiveZ = 1 << 5;
    public const int All = NegativeX | PositiveX | NegativeY | PositiveY | NegativeZ | PositiveZ;

  }

  internal readonly struct FireGridEnvironmentSample {

    public static FireGridEnvironmentSample OpenAir { get; } = new(
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

    public FireGridEnvironmentSample(
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
      ExposedFaceMask = exposedFaceMask & FireGridExposedFaces.All;
    }

    public FireCellEnvironment ToEnvironment() =>
      new(StructureKind, Fuel, Moisture, Barrier, OxygenAvailability, WaterDepth, ExposedFaceMask);

  }

  internal static class FireGridEnvironmentSampler {

    public static FireGridEnvironmentSample FromProfile(
      string structureKind,
      float fuel,
      float moistureResistance,
      float barrierResistance) =>
      new(
        ParseStructureKind(structureKind),
        fuel,
        1f - Mathf.Clamp01(moistureResistance),
        barrierResistance,
        1f,
        0f,
        FireGridExposedFaces.All);

    public static FireGridEnvironmentSample Merge(
      FireGridEnvironmentSample profile,
      FireGridEnvironmentSample world) {
      var structureKind = MergeStructureKind(profile.StructureKind, world.StructureKind);
      var exposedFaceMask = world.ExposedFaceMask == FireGridExposedFaces.All
        ? profile.ExposedFaceMask
        : profile.ExposedFaceMask & world.ExposedFaceMask;

      return new FireGridEnvironmentSample(
        structureKind,
        Mathf.Max(profile.Fuel, world.Fuel),
        Mathf.Max(profile.Moisture, world.Moisture),
        Mathf.Max(profile.Barrier, world.Barrier),
        Mathf.Min(profile.OxygenAvailability, world.OxygenAvailability),
        Mathf.Max(profile.WaterDepth, world.WaterDepth),
        exposedFaceMask);
    }

    public static FireGridEnvironmentSample CreateDefaultWorldSample() =>
      FireGridEnvironmentSample.OpenAir;

    public static FireGridEnvironmentSample FromTerrainColumn(
      FireGridCoordinate coordinate,
      int floor,
      int ceiling) {
      if (ceiling <= floor || coordinate.Y < floor || coordinate.Y > ceiling) {
        return FireGridEnvironmentSample.OpenAir;
      }

      if (coordinate.Y == ceiling) {
        return new FireGridEnvironmentSample(
          FireGridStructureKind.Terrain,
          0.2f,
          0.35f,
          0.25f,
          1f,
          0f,
          FireGridExposedFaces.PositiveY
          | FireGridExposedFaces.NegativeX
          | FireGridExposedFaces.PositiveX
          | FireGridExposedFaces.NegativeZ
          | FireGridExposedFaces.PositiveZ);
      }

      return new FireGridEnvironmentSample(
        FireGridStructureKind.Terrain,
        0.05f,
        0.45f,
        0.85f,
        0.1f,
        0f,
        FireGridExposedFaces.None);
    }

    private static FireGridStructureKind MergeStructureKind(
      FireGridStructureKind profileKind,
      FireGridStructureKind worldKind) {
      if (worldKind == FireGridStructureKind.Water) {
        return FireGridStructureKind.Water;
      }

      if (profileKind != FireGridStructureKind.Unknown && profileKind != FireGridStructureKind.Air) {
        return profileKind;
      }

      return worldKind == FireGridStructureKind.Air ? profileKind : worldKind;
    }

    private static FireGridStructureKind ParseStructureKind(string structureKind) {
      if (string.IsNullOrWhiteSpace(structureKind)) {
        return FireGridStructureKind.Unknown;
      }

      var normalized = structureKind.ToLowerInvariant();
      if (normalized.Contains("tree") || normalized.Contains("berry") || normalized.Contains("crop")) {
        return FireGridStructureKind.Vegetation;
      }

      if (normalized.Contains("barrier")) {
        return FireGridStructureKind.Barrier;
      }

      return FireGridStructureKind.Building;
    }

  }
}
