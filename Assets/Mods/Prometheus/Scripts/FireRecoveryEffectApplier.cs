using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;

namespace Mods.Prometheus.Scripts {
  internal class FireRecoveryEffectApplier : BaseComponent,
                                            IAwakableComponent,
                                            IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireRecoveryRuntimeState _fireRecoveryRuntimeState;

    private float _timeSinceLastUpdate;
    private object _growable;
    private PropertyInfo _growthTimeInDaysProperty;
    private float _baseGrowthTimeInDays;
    private bool _hasModifiedGrowthTime;

    [Inject]
    public void InjectDependencies(FireRecoveryRuntimeState fireRecoveryRuntimeState) {
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
    }

    public void Awake() {
      _growable = GameObject.GetComponent("Growable");
      if (_growable is null) {
        return;
      }

      var type = _growable.GetType();
      _growthTimeInDaysProperty = type.GetProperty("GrowthTimeInDays", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (_growthTimeInDaysProperty is null || !_growthTimeInDaysProperty.CanRead || !_growthTimeInDaysProperty.CanWrite) {
        _growable = null;
        _growthTimeInDaysProperty = null;
        return;
      }

      _baseGrowthTimeInDays = (float)_growthTimeInDaysProperty.GetValue(_growable);
    }

    public void Update() {
      if (_growable is null || _growthTimeInDaysProperty is null) {
        return;
      }

      _timeSinceLastUpdate += UnityEngine.Time.deltaTime;
      if (_timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      var entityId = GameObject.GetInstanceID();
      if (!_fireRecoveryRuntimeState.TryGetSnapshot(entityId, out var recoverySnapshot)) {
        RestoreBaseGrowthTimeIfNeeded();
        return;
      }

      if (!recoverySnapshot.AshenFertilityActive || recoverySnapshot.GrowthSpeedBonus <= 0f) {
        RestoreBaseGrowthTimeIfNeeded();
        return;
      }

      var growthSpeedMultiplier = 1f + recoverySnapshot.GrowthSpeedBonus;
      var boostedGrowthTime = _baseGrowthTimeInDays / growthSpeedMultiplier;
      _growthTimeInDaysProperty.SetValue(_growable, boostedGrowthTime);
      _hasModifiedGrowthTime = true;
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