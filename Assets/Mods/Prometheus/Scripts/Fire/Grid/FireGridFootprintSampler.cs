using System.Collections.Generic;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
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
}
