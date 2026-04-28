using System;
using System.Collections;
using System.Linq;
using System.Reflection;

#if !PROMETHEUS_TESTS
using Bindito.Core;
using Timberborn.BehaviorSystem;
using Timberborn.Goods;
using Timberborn.InventorySystem;
using Timberborn.WorkSystem;
using UnityEngine;
#endif

namespace Mods.Prometheus.Scripts {
  internal static class FarmHouseFertileAshAmendmentRules {

    internal const float AmendmentDurationHours = 72f;
    internal const int AmendmentCharges = 3;
    internal const float WorkerWaitHours = 0.05f;
    internal const int AshAmountPerApplication = 1;

    internal static bool CanApply(
      bool hasTarget,
      bool targetAlreadyAmended,
      bool hasUnreservedAsh) =>
      hasTarget
      && !targetAlreadyAmended
      && hasUnreservedAsh;

  }

#if !PROMETHEUS_TESTS
  internal sealed class FarmHouseFertileAshAmendmentWorkplaceBehavior : WorkplaceBehavior {

    private const string InRangePlantingCoordinatesTypeName = "Timberborn.Planting.InRangePlantingCoordinates, Timberborn.Planting";

    private FireFieldAmendmentRuntimeState _fireFieldAmendmentRuntimeState;
    private DistrictInventoryRegistry _districtInventoryRegistry;
    private Type _inRangePlantingCoordinatesType;
    private MethodInfo _getCoordinatesMethod;
    private bool _compatibilityProbeLogged;
    private bool _missingTargetLogged;
    private bool _missingAshLogged;

    [Inject]
    public void InjectDependencies(
      FireFieldAmendmentRuntimeState fireFieldAmendmentRuntimeState,
      DistrictInventoryRegistry districtInventoryRegistry) {
      _fireFieldAmendmentRuntimeState = fireFieldAmendmentRuntimeState;
      _districtInventoryRegistry = districtInventoryRegistry;
    }

    public override Decision Decide(BehaviorAgent agent) {
      if (_fireFieldAmendmentRuntimeState is null || _districtInventoryRegistry is null) {
        LogSkippedOnce(ref _missingAshLogged, "dependencies_missing");
        return Decision.ReleaseNow();
      }

      if (!TryFindTargetCoordinate(out var coordinate, out var targetReason)) {
        LogSkippedOnce(ref _missingTargetLogged, targetReason);
        return Decision.ReleaseNow();
      }

      if (_fireFieldAmendmentRuntimeState.TryGetAmendment(coordinate, out var existing) && existing.IsActive) {
        return Decision.ReleaseNow();
      }

      if (!TryTakeFertileAsh(out var inventoryEntityId)) {
        LogSkippedOnce(ref _missingAshLogged, "fertile_ash_unavailable");
        return Decision.ReleaseNow();
      }

      _fireFieldAmendmentRuntimeState.SetAmendment(
        coordinate,
        FarmHouseFertileAshAmendmentRules.AmendmentDurationHours,
        FarmHouseFertileAshAmendmentRules.AmendmentCharges);
      FireTelemetry.Log(
        $"event={FireTelemetryEvents.FertileAshFarmhouseAmendmentApplied} farmhouse={GameObject.name} farmhouseId={GameObject.GetInstanceID()} inventoryId={inventoryEntityId} good={FertileAshRecoveredGoodStackRules.FertileAshGoodId} amount={FarmHouseFertileAshAmendmentRules.AshAmountPerApplication} coordinate={coordinate} durationHours={FarmHouseFertileAshAmendmentRules.AmendmentDurationHours:0.###} charges={FarmHouseFertileAshAmendmentRules.AmendmentCharges}");

      var waitExecutor = agent.GetComponent<WaitExecutor>();
      if (waitExecutor is null) {
        return Decision.ReleaseNow();
      }

      waitExecutor.LaunchForSpecifiedTime(FarmHouseFertileAshAmendmentRules.WorkerWaitHours);
      return Decision.ReleaseWhenFinished(waitExecutor);
    }

    private bool TryFindTargetCoordinate(out FireGridCoordinate coordinate, out string reason) {
      coordinate = default;
      _inRangePlantingCoordinatesType ??= Type.GetType(InRangePlantingCoordinatesTypeName);
      if (_inRangePlantingCoordinatesType is null) {
        RecordAgricultureProbe(false, "InRangePlantingCoordinates type missing");
        reason = "planting_coordinates_type_missing";
        return false;
      }

      var inRangeCoordinates = GameObject.GetComponent(_inRangePlantingCoordinatesType);
      if (inRangeCoordinates is null) {
        RecordAgricultureProbe(false, "InRangePlantingCoordinates component missing");
        reason = "planting_coordinates_component_missing";
        return false;
      }

      _getCoordinatesMethod ??= TimberbornCompatibility.FindMethod(inRangeCoordinates.GetType(), "GetCoordinates");
      if (_getCoordinatesMethod is null) {
        RecordAgricultureProbe(false, "InRangePlantingCoordinates.GetCoordinates missing");
        reason = "planting_coordinates_api_missing";
        return false;
      }

      var coordinates = (_getCoordinatesMethod.Invoke(inRangeCoordinates, Array.Empty<object>()) as IEnumerable)
        ?.Cast<object>()
        .OfType<Vector3Int>()
        .Select(ToFireGridCoordinate)
        .Where(candidate => !_fireFieldAmendmentRuntimeState.TryGetAmendment(candidate, out var amendment) || !amendment.IsActive)
        .OrderBy(candidate => Mathf.Abs(candidate.X - Mathf.RoundToInt(GameObject.transform.position.x))
                              + Mathf.Abs(candidate.Z - Mathf.RoundToInt(GameObject.transform.position.z)))
        .ToArray() ?? Array.Empty<FireGridCoordinate>();
      if (coordinates.Length == 0) {
        RecordAgricultureProbe(true, "InRangePlantingCoordinates.GetCoordinates resolved but no unamended target");
        reason = "no_unamended_planting_coordinate";
        return false;
      }

      RecordAgricultureProbe(true, "InRangePlantingCoordinates.GetCoordinates");
      coordinate = coordinates[0];
      reason = "ready";
      return true;
    }

    private bool TryTakeFertileAsh(out int inventoryEntityId) {
      inventoryEntityId = 0;
      var goodAmount = new GoodAmount(
        FertileAshRecoveredGoodStackRules.FertileAshGoodId,
        FarmHouseFertileAshAmendmentRules.AshAmountPerApplication);
      foreach (var inventory in _districtInventoryRegistry.ActiveInventoriesWithStock(FertileAshRecoveredGoodStackRules.FertileAshGoodId)) {
        if (!inventory.HasUnreservedStock(goodAmount)) {
          continue;
        }

        inventory.Take(goodAmount);
        inventoryEntityId = inventory.GameObject.GetInstanceID();
        return true;
      }

      return false;
    }

    private void RecordAgricultureProbe(bool resolved, string detail) {
      if (_compatibilityProbeLogged && resolved) {
        return;
      }

      _compatibilityProbeLogged = resolved;
      TimberbornCompatibility.RecordProbe(TimberbornCompatibilityArea.Agriculture, resolved, detail);
    }

    private void LogSkippedOnce(ref bool logged, string reason) {
      if (logged) {
        return;
      }

      logged = true;
      FireTelemetry.Log(
        $"event={FireTelemetryEvents.FertileAshFarmhouseAmendmentSkipped} farmhouse={GameObject.name} farmhouseId={GameObject.GetInstanceID()} reason={reason}");
    }

    private static FireGridCoordinate ToFireGridCoordinate(Vector3Int coordinates) =>
      new(coordinates.x, coordinates.y, coordinates.z);

  }
#endif
}
