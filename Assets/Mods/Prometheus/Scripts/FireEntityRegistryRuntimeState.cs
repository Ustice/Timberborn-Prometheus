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

    public int SnapshotCount => _snapshotsByEntityId.Count;

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

        if (!TryGetDistanceSquaredWithinRadius(position, snapshot.Position, radiusSquared, out var distanceSquared)) {
          continue;
        }

        var normalizedDistance = Mathf.Clamp01(Mathf.Sqrt(distanceSquared) / radius);
        var falloff = 1f - normalizedDistance;

        accumulatedPressure += snapshot.SpreadPotential * snapshot.Intensity * falloff;
      }

      return Mathf.Clamp(accumulatedPressure, 0f, 0.12f);
    }

    public void RemoveSnapshot(int entityId) {
      _snapshotsByEntityId.Remove(entityId);
    }

    public void ClearSnapshots() {
      _snapshotsByEntityId.Clear();
    }

    public bool TryGetNearestSpreadTarget(int sourceEntityId, Vector3 sourcePosition, float radius, out int targetEntityId, out float normalizedDistance) {
      var radiusSquared = radius * radius;
      var bestDistanceSquared = float.MaxValue;
      var bestTargetEntityId = 0;

      foreach (var pair in _snapshotsByEntityId) {
        if (pair.Key == sourceEntityId) {
          continue;
        }

        var snapshot = pair.Value;
        if (snapshot.Burning) {
          continue;
        }

        if (!TryGetDistanceSquaredWithinRadius(sourcePosition, snapshot.Position, radiusSquared, out var distanceSquared)) {
          continue;
        }

        if (distanceSquared < bestDistanceSquared) {
          bestDistanceSquared = distanceSquared;
          bestTargetEntityId = pair.Key;
        }
      }

      if (bestTargetEntityId == 0) {
        targetEntityId = 0;
        normalizedDistance = 1f;
        return false;
      }

      targetEntityId = bestTargetEntityId;
      normalizedDistance = Mathf.Clamp01(Mathf.Sqrt(bestDistanceSquared) / radius);
      return true;
    }

    public int ExtinguishAllBurning() {
      var extinguishedCount = 0;
      var entityIds = new List<int>(_snapshotsByEntityId.Keys);

      for (var i = 0; i < entityIds.Count; i++) {
        var entityId = entityIds[i];
        var snapshot = _snapshotsByEntityId[entityId];
        if (!snapshot.Burning && snapshot.Intensity <= 0f) {
          continue;
        }

        _snapshotsByEntityId[entityId] = new FireEntityRegistrySnapshot(snapshot.Position, false, 0f, 0f);
        extinguishedCount++;
      }

      return extinguishedCount;
    }

    private static bool TryGetDistanceSquaredWithinRadius(Vector3 sourcePosition, Vector3 targetPosition, float radiusSquared, out float distanceSquared) {
      var offset = targetPosition - sourcePosition;
      distanceSquared = offset.sqrMagnitude;
      return distanceSquared <= radiusSquared;
    }

  }
}
