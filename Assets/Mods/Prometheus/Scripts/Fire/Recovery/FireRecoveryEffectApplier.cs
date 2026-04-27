using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireRecoveryEffectApplier : BaseComponent,
                                            IAwakableComponent,
                                            IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireFieldAmendmentRuntimeState _fireFieldAmendmentRuntimeState;

    private float _timeSinceLastUpdate;
    private object _growable;
    private PropertyInfo _growthTimeInDaysProperty;
    private float _baseGrowthTimeInDays;
    private FireGridCoordinate _primaryCoordinate;
    private FireGridCoordinate _groundCoordinate;
    private bool _eligibleCropGrowable;
    private bool _hasModifiedGrowthTime;

    [Inject]
    public void InjectDependencies(FireFieldAmendmentRuntimeState fireFieldAmendmentRuntimeState) {
      _fireFieldAmendmentRuntimeState = fireFieldAmendmentRuntimeState;
    }

    public void Awake() {
      _eligibleCropGrowable = FireFieldAmendmentGrowthRules.IsEligibleCropGrowable(
        TimberbornComponentCacheLookup.EnumerateGameObjectAndCachedComponentTypeNames(GameObject));
      if (!_eligibleCropGrowable) {
        return;
      }

      if (!TimberbornComponentCacheLookup.TryGetCachedOrDirectComponentByTypeName(
        GameObject,
        TimberbornCompatibility.GrowableTypeName,
        out _growable)) {
        return;
      }

      var type = _growable.GetType();
      _growthTimeInDaysProperty = TimberbornCompatibility.FindProperty(type, "GrowthTimeInDays");
      if (_growthTimeInDaysProperty is null || !_growthTimeInDaysProperty.CanRead || !_growthTimeInDaysProperty.CanWrite) {
        TimberbornCompatibility.RecordProbe(TimberbornCompatibilityArea.Recovery, false, "Growable.GrowthTimeInDays read/write");
        _growable = null;
        _growthTimeInDaysProperty = null;
        return;
      }

      TimberbornCompatibility.RecordProbe(TimberbornCompatibilityArea.Recovery, true, "Growable.GrowthTimeInDays read/write");
      _baseGrowthTimeInDays = (float)_growthTimeInDaysProperty.GetValue(_growable);
      var footprint = FireGridFootprintSampler.FromWorldPosition(GameObject.transform.position);
      _primaryCoordinate = footprint.PrimaryCoordinate;
      _groundCoordinate = new FireGridCoordinate(_primaryCoordinate.X, _primaryCoordinate.Y - 1, _primaryCoordinate.Z);
    }

    public void Update() {
      if (!_eligibleCropGrowable || _growable is null || _growthTimeInDaysProperty is null) {
        return;
      }

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      if (!TryGetActiveAmendment(out var amendment)) {
        RestoreBaseGrowthTimeIfNeeded();
        return;
      }

      if (!_hasModifiedGrowthTime) {
        _baseGrowthTimeInDays = (float)_growthTimeInDaysProperty.GetValue(_growable);
      }

      var boostedGrowthTime = FireFieldAmendmentGrowthRules.ComputeBoostedGrowthTimeInDays(_baseGrowthTimeInDays);
      _growthTimeInDaysProperty.SetValue(_growable, boostedGrowthTime);
      if (!_hasModifiedGrowthTime) {
        FireTelemetry.Log($"event={FireTelemetryEvents.FieldAmendmentGrowthBuffApplied} entity={GameObject.name} id={GameObject.GetInstanceID()} coordinate={amendment.Coordinate} baseGrowthTimeDays={_baseGrowthTimeInDays:0.###} boostedGrowthTimeDays={boostedGrowthTime:0.###} remainingHours={amendment.RemainingHours:0.###} charges={amendment.RemainingCharges}");
      }

      _hasModifiedGrowthTime = true;
    }

    internal void DebugRestoreBaseRecoveryEffects() {
      RestoreBaseGrowthTimeIfNeeded();
    }

    private void RestoreBaseGrowthTimeIfNeeded() {
      if (!_hasModifiedGrowthTime || _growable is null || _growthTimeInDaysProperty is null) {
        return;
      }

      _growthTimeInDaysProperty.SetValue(_growable, _baseGrowthTimeInDays);
      _hasModifiedGrowthTime = false;
      FireTelemetry.Log($"event={FireTelemetryEvents.FieldAmendmentGrowthBuffRestored} entity={GameObject.name} id={GameObject.GetInstanceID()} baseGrowthTimeDays={_baseGrowthTimeInDays:0.###}");
    }

    private bool TryGetActiveAmendment(out FireFieldAmendmentSnapshot amendment) {
      if (_fireFieldAmendmentRuntimeState.TryGetAmendment(_primaryCoordinate, out amendment) && amendment.IsActive) {
        return true;
      }

      return _fireFieldAmendmentRuntimeState.TryGetAmendment(_groundCoordinate, out amendment) && amendment.IsActive;
    }

  }
}
