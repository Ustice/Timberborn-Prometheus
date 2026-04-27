using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;

namespace Mods.Prometheus.Scripts {
  internal class FireRecoveryEffectApplier : BaseComponent,
                                            IAwakableComponent,
                                            IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireRuntimeProjectionRuntimeState _fireRuntimeProjectionRuntimeState;

    private float _timeSinceLastUpdate;
    private object _growable;
    private PropertyInfo _growthTimeInDaysProperty;
    private float _baseGrowthTimeInDays;
    private bool _hasModifiedGrowthTime;

    [Inject]
    public void InjectDependencies(FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState) {
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
    }

    public void Awake() {
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
    }

    public void Update() {
      if (_growable is null || _growthTimeInDaysProperty is null) {
        return;
      }

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      if (!_fireRuntimeProjectionRuntimeState.TryGetSnapshot(entityId, out var projection) || !projection.HasRecovery) {
        RestoreBaseGrowthTimeIfNeeded();
        return;
      }

      var recoverySnapshot = projection.Recovery;
      if (!recoverySnapshot.FertileAshAvailable || recoverySnapshot.GrowthSpeedBonus <= 0f) {
        RestoreBaseGrowthTimeIfNeeded();
        return;
      }

      var growthSpeedMultiplier = 1f + recoverySnapshot.GrowthSpeedBonus;
      var boostedGrowthTime = _baseGrowthTimeInDays / growthSpeedMultiplier;
      _growthTimeInDaysProperty.SetValue(_growable, boostedGrowthTime);
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
    }

  }
}
