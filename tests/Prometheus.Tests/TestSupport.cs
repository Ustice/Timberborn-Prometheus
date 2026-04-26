using System;
using System.Collections.Generic;
using Mods.Prometheus.Scripts;

namespace Prometheus.Tests
{
    internal static class TestSupport
    {

        public static FireExposureSnapshot CreateExposureSnapshot(
          bool burning,
          float intensity,
          float moistureDampening = 0f)
        {
            return new FireExposureSnapshot(
              burning,
              intensity,
              intensity * 0.7f,
              intensity * 0.4f,
              intensity * 0.5f,
              burning ? 1f : 0f,
              burning ? 0.15f : 0f,
              moistureDampening,
              1f,
              burning ? "Grid" : "None");
        }

        public static FireGridRuntimeState CreateGridWithFuelAroundOrigin()
        {
            var grid = new FireGridRuntimeState();
            for (var x = -1; x <= 2; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    for (var z = -1; z <= 1; z++)
                    {
                        grid.SetEnvironment(new FireGridCoordinate(x, y, z), BurnableEnvironment());
                    }
                }
            }

            return grid;
        }

        public static FireCellEnvironment BurnableEnvironment() =>
          new(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 1f, 0f, 63);

        public static FireCellState HotCell() =>
          new(1f, 1f, 0.5f, 1f, 0f, FireGridBurnState.Burning);

        public static FireGridRuntimeState CreateTwoCellTransferGrid(FireCellEnvironment targetEnvironment)
        {
            var grid = new FireGridRuntimeState();
            var source = new FireGridCoordinate(0, 0, 0);
            var target = new FireGridCoordinate(1, 0, 0);

            grid.SetEnvironment(source, BurnableEnvironment());
            grid.SetEnvironment(target, targetEnvironment);
            grid.Inject(source, HotCell());
            return grid;
        }

        public static FireGridKernelEntry FindKernelEntry(IReadOnlyList<FireGridKernelEntry> entries, int dx, int dy, int dz)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Offset.Dx == dx && entry.Offset.Dy == dy && entry.Offset.Dz == dz)
                {
                    return entry;
                }
            }

            throw new InvalidOperationException($"Missing kernel entry {dx},{dy},{dz}.");
        }

        public static int CountKernelEntries(IReadOnlyList<FireGridKernelEntry> entries, int dx, int dy, int dz)
        {
            var count = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Offset.Dx == dx && entry.Offset.Dy == dy && entry.Offset.Dz == dz)
                {
                    count++;
                }
            }

            return count;
        }

        public static bool ContainsCoordinate(IReadOnlyList<FireGridCoordinate> coordinates, FireGridCoordinate coordinate)
        {
            for (var i = 0; i < coordinates.Count; i++)
            {
                if (coordinates[i].Equals(coordinate))
                {
                    return true;
                }
            }

            return false;
        }

        public static void True(bool value)
        {
            if (!value)
            {
                throw new InvalidOperationException("Expected true.");
            }
        }

        public static void False(bool value)
        {
            if (value)
            {
                throw new InvalidOperationException("Expected false.");
            }
        }

        public static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException($"Expected {expected}, got {actual}.");
            }
        }

        public static void NearlyEqual(float expected, float actual, float tolerance = 0.0001f)
        {
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new InvalidOperationException($"Expected {expected:0.####}, got {actual:0.####}.");
            }
        }

    }
}
