using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireBurnedGroundAshDepositSnapshot {

    public Vector3Int Coordinates { get; }
    public int SourceEntityId { get; }
    public int Amount { get; }
    public string SourceKind { get; }
    public string DamageCategory { get; }
    public string CropContext { get; }

    public FireBurnedGroundAshDepositSnapshot(
      Vector3Int coordinates,
      int sourceEntityId,
      int amount,
      string sourceKind,
      string damageCategory,
      string cropContext) {
      Coordinates = coordinates;
      SourceEntityId = sourceEntityId;
      Amount = amount;
      SourceKind = string.IsNullOrWhiteSpace(sourceKind) ? "unknown" : sourceKind;
      DamageCategory = string.IsNullOrWhiteSpace(damageCategory) ? "unknown" : damageCategory;
      CropContext = string.IsNullOrWhiteSpace(cropContext) ? "none" : cropContext;
    }

  }

  internal sealed class FireBurnedGroundAshDepositRuntimeState {

    private readonly Dictionary<int, FireBurnedGroundAshDepositSnapshot> _depositsBySourceEntityId = new();

    public int DepositCount => _depositsBySourceEntityId.Count;

    public bool TryRecordDeposit(
      Vector3Int coordinates,
      int sourceEntityId,
      int amount,
      FertileAshSpawnTelemetryContext context,
      out FireBurnedGroundAshDepositSnapshot snapshot) {
      if (sourceEntityId == 0 || amount <= 0) {
        snapshot = default;
        return false;
      }

      if (_depositsBySourceEntityId.TryGetValue(sourceEntityId, out snapshot)) {
        return false;
      }

      snapshot = new FireBurnedGroundAshDepositSnapshot(
        coordinates,
        sourceEntityId,
        amount,
        context.SourceKind,
        context.DamageCategory,
        context.CropContext);
      _depositsBySourceEntityId[sourceEntityId] = snapshot;
      return true;
    }

    public bool TryGetDeposit(int sourceEntityId, out FireBurnedGroundAshDepositSnapshot snapshot) =>
      _depositsBySourceEntityId.TryGetValue(sourceEntityId, out snapshot);

    public FireBurnedGroundAshDepositSnapshot[] GetDeposits() =>
      _depositsBySourceEntityId.Values
        .OrderBy(deposit => deposit.SourceEntityId)
        .ToArray();

    public int ClearDeposits() {
      var count = _depositsBySourceEntityId.Count;
      _depositsBySourceEntityId.Clear();
      return count;
    }

  }

#if !PROMETHEUS_TESTS
  internal sealed class FireBurnedGroundAshDepositMarkerSpawner {

    private static readonly Dictionary<int, GameObject> MarkersBySourceEntityId = new();

    public bool TryCreateMarker(FireBurnedGroundAshDepositSnapshot deposit) {
      if (deposit.SourceEntityId == 0 || MarkersBySourceEntityId.ContainsKey(deposit.SourceEntityId)) {
        return false;
      }

      var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
      marker.name = $"PrometheusBurnedGroundAshDeposit:{deposit.SourceEntityId}";
      marker.transform.position = new Vector3(
        deposit.Coordinates.x + 0.5f,
        deposit.Coordinates.y + 0.018f,
        deposit.Coordinates.z + 0.5f);
      marker.transform.localScale = new Vector3(0.72f, 0.018f, 0.72f);

      var collider = marker.GetComponent<Collider>();
      if (collider is not null) {
        Object.Destroy(collider);
      }

      var renderer = marker.GetComponent<Renderer>();
      if (renderer is not null) {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = shader is null
          ? new Material(renderer.sharedMaterial)
          : new Material(shader);
        material.name = "Prometheus_BurnedGroundAshDeposit";
        material.color = new Color(0.025f, 0.021f, 0.018f, 0.92f);
        renderer.sharedMaterial = material;
      }

      MarkersBySourceEntityId[deposit.SourceEntityId] = marker;
      FireTelemetry.Log(
        $"event={FireTelemetryEvents.BurnedGroundAshDepositMarkerCreated} sourceEntityId={deposit.SourceEntityId} amount={deposit.Amount} sourceKind={deposit.SourceKind} damageCategory={deposit.DamageCategory} cropContext={deposit.CropContext} coordinates={deposit.Coordinates.x},{deposit.Coordinates.y},{deposit.Coordinates.z}");
      return true;
    }

    public static int ClearAllMarkers() {
      var count = MarkersBySourceEntityId.Count;
      foreach (var marker in MarkersBySourceEntityId.Values.ToArray()) {
        if (marker is not null) {
          Object.Destroy(marker);
        }
      }

      MarkersBySourceEntityId.Clear();
      FireTelemetry.Log($"event={FireTelemetryEvents.BurnedGroundAshDepositMarkersReset} markersDestroyed={count}");
      return count;
    }

  }
#endif
}
