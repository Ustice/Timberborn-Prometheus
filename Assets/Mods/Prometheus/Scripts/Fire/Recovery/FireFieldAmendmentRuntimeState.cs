using System;
using System.Collections.Generic;
using System.Linq;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireFieldAmendmentSnapshot {

    public FireGridCoordinate Coordinate { get; }
    public float RemainingHours { get; }
    public int RemainingCharges { get; }
    public bool IsActive => RemainingHours > 0f && RemainingCharges > 0;

    public FireFieldAmendmentSnapshot(FireGridCoordinate coordinate, float remainingHours, int remainingCharges) {
      Coordinate = coordinate;
      RemainingHours = Math.Max(0f, remainingHours);
      RemainingCharges = Math.Max(0, remainingCharges);
    }

    public FireFieldAmendmentSnapshot WithRemainingHours(float remainingHours) =>
      new(Coordinate, remainingHours, RemainingCharges);

    public FireFieldAmendmentSnapshot WithRemainingCharges(int remainingCharges) =>
      new(Coordinate, RemainingHours, remainingCharges);

  }

  internal sealed class FireFieldAmendmentRuntimeState {

    private readonly Dictionary<FireGridCoordinate, FireFieldAmendmentSnapshot> _amendmentsByCoordinate = new();

    public int ActiveAmendmentCount => _amendmentsByCoordinate.Count;

    public IReadOnlyCollection<FireFieldAmendmentSnapshot> ActiveAmendments => _amendmentsByCoordinate.Values.ToArray();

    public void SetAmendment(FireGridCoordinate coordinate, float durationHours, int charges) {
      var snapshot = new FireFieldAmendmentSnapshot(coordinate, durationHours, charges);
      if (!snapshot.IsActive) {
        _amendmentsByCoordinate.Remove(coordinate);
        return;
      }

      _amendmentsByCoordinate[coordinate] = snapshot;
    }

    public bool TryGetAmendment(FireGridCoordinate coordinate, out FireFieldAmendmentSnapshot snapshot) =>
      _amendmentsByCoordinate.TryGetValue(coordinate, out snapshot);

    public bool ConsumeCharge(FireGridCoordinate coordinate) {
      if (!_amendmentsByCoordinate.TryGetValue(coordinate, out var snapshot) || !snapshot.IsActive) {
        _amendmentsByCoordinate.Remove(coordinate);
        return false;
      }

      var consumed = snapshot.WithRemainingCharges(snapshot.RemainingCharges - 1);
      if (consumed.IsActive) {
        _amendmentsByCoordinate[coordinate] = consumed;
      } else {
        _amendmentsByCoordinate.Remove(coordinate);
      }

      return true;
    }

    public void Tick(float deltaHours) {
      if (deltaHours <= 0f || _amendmentsByCoordinate.Count == 0) {
        return;
      }

      var updatedSnapshots = _amendmentsByCoordinate.Values
        .Select(snapshot => snapshot.WithRemainingHours(snapshot.RemainingHours - deltaHours))
        .Where(snapshot => snapshot.IsActive)
        .ToArray();

      _amendmentsByCoordinate.Clear();
      foreach (var snapshot in updatedSnapshots) {
        _amendmentsByCoordinate[snapshot.Coordinate] = snapshot;
      }
    }

    public void ClearAmendments() {
      _amendmentsByCoordinate.Clear();
    }

  }
}
