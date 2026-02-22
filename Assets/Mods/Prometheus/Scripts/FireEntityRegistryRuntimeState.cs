using System.Collections.Generic;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireEntityRegistrySnapshot {

    public Vector3 Position { get; }
    public bool Burning { get; }
    public float Intensity { get; }
    public float SpreadPotential { get; }

    public FireEntityRegistrySnapshot(
      Vector3 position,
      bool burning,
      float intensity,
      float spreadPotential) {
      Position = position;
      Burning = burning;
      Intensity = intensity;
      SpreadPotential = spreadPotential;
    }

  }

  internal class FireEntityRegistryRuntimeState {

    private readonly Dictionary<int, FireEntityRegistrySnapshot> _snapshotsByEntityId = new();

    public void SetSnapshot(int entityId, FireEntityRegistrySnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public float ComputeNeighborSpreadPressure(int entityId, Vector3 position, float radius) {
      var radiusSquared = radius * radius;
      var accumulatedPressure = 0f;

      foreach (var pair in _snapshotsByEntityId) {
        if (pair.Key == entityId) {
          continue;
        }

        var snapshot = pair.Value;
        if (!snapshot.Burning || snapshot.Intensity <= 0f) {
          continue;
        }

        var offset = snapshot.Position - position;
        var distanceSquared = offset.sqrMagnitude;
        if (distanceSquared > radiusSquared) {
          continue;
        }

        var normalizedDistance = Mathf.Clamp01(Mathf.Sqrt(distanceSquared) / radius);
        var falloff = 1f - normalizedDistance;

        accumulatedPressure += snapshot.SpreadPotential * snapshot.Intensity * falloff;
      }

      return Mathf.Clamp(accumulatedPressure, 0f, 0.12f);
    }

  }
}