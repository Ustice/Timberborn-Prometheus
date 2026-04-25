using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.WorkSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireDamageStateController : BaseComponent,
                                            IAwakableComponent,
                                            IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireImpactRuntimeState _fireImpactRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireTuningRuntimeState _fireTuningRuntimeState;

    private float _timeSinceLastUpdate;
    private FireDamageCategory _category;
    private float _severity;
    private float _tickProgress;
    private int _damageTicksApplied;

    [Inject]
    public void InjectDependencies(
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireTuningRuntimeState fireTuningRuntimeState) {
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireTuningRuntimeState = fireTuningRuntimeState;
    }

    public void Awake() {
      _category = DetectCategory();
    }

    public void Update() {
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      if (!_fireImpactRuntimeState.TryGetSnapshot(entityId, out var impactSnapshot)) {
        return;
      }

      var pressure = GetRelevantPressure(impactSnapshot);
      var tickRate = GetTickRate(_category) * _fireTuningRuntimeState.Current.DamageTickMultiplier;
      var tickSeverityDelta = GetTickSeverityDelta(_category) * _fireTuningRuntimeState.Current.DamageTickMultiplier;

      var previousState = FireDamageStateRules.DetermineState(_severity);

      if (pressure > 0.02f) {
        _tickProgress += pressure * tickRate;

        while (_tickProgress >= 1f) {
          _tickProgress -= 1f;
          _severity = Mathf.Clamp01(_severity + tickSeverityDelta);
          _damageTicksApplied++;
        }
      } else {
        var canDecay = !(_category == FireDamageCategory.Building && previousState == FireDamageState.Dead);
        if (canDecay) {
          _severity = Mathf.Clamp01(_severity - 0.05f);
          _tickProgress = Mathf.Clamp01(_tickProgress - 0.1f);
        } else {
          _severity = 1f;
          _tickProgress = 1f;
        }
      }

      var state = FireDamageStateRules.DetermineState(_severity);
      var snapshot = new FireDamageStateSnapshot(_category, state, _severity, _tickProgress, _damageTicksApplied);
      _fireDamageStateRuntimeState.SetSnapshot(entityId, snapshot);
    }

    internal void DebugResetDamageStateToHealthy() {
      var entityId = GameObject.GetInstanceID();
      _severity = 0f;
      _tickProgress = 0f;
      _damageTicksApplied = 0;
      _fireDamageStateRuntimeState.SetSnapshot(
        entityId,
        new FireDamageStateSnapshot(_category, FireDamageState.Healthy, 0f, 0f, 0));
    }

    private static float GetTickRate(FireDamageCategory category) {
      return category switch {
        FireDamageCategory.Crop => 1.25f,
        FireDamageCategory.Tree => 1.0f,
        FireDamageCategory.Building => 0.8f,
        _ => 0.75f,
      };
    }

    private static float GetTickSeverityDelta(FireDamageCategory category) {
      return category switch {
        FireDamageCategory.Crop => 0.14f,
        FireDamageCategory.Tree => 0.11f,
        FireDamageCategory.Building => 0.08f,
        _ => 0.07f,
      };
    }

    private float GetRelevantPressure(FireImpactSnapshot impactSnapshot) {
      return _category switch {
        FireDamageCategory.Crop => impactSnapshot.CropDamagePressure,
        FireDamageCategory.Tree => impactSnapshot.TreeDamagePressure,
        FireDamageCategory.Building => impactSnapshot.BuildingDamagePressure,
        _ => impactSnapshot.BuildingDamagePressure,
      };
    }

    private FireDamageCategory DetectCategory() {
      var componentCache = GameObject.GetComponent<ComponentCache>();
      if (componentCache is not null) {
        if (componentCache.TryGetCachedComponent<Workplace>(out _)) {
          return FireDamageCategory.Building;
        }

        if (HasCachedComponentNamed(componentCache, "TreeComponent")) {
          return FireDamageCategory.Tree;
        }

        if (HasCachedComponentNamed(componentCache, "Growable")) {
          return FireDamageCategory.Crop;
        }

        if (HasCachedComponentNamed(componentCache, "Deteriorable")
            || HasCachedComponentNamed(componentCache, "WorkplaceBonuses")
            || HasCachedComponentNamed(componentCache, "Manufactory")
            || HasCachedComponentNamed(componentCache, "Workshop")) {
          return FireDamageCategory.Building;
        }
      }

      if (GameObject.GetComponent("TreeComponent") is not null) {
        return FireDamageCategory.Tree;
      }

      if (GameObject.GetComponent("Growable") is not null) {
        return FireDamageCategory.Crop;
      }

      if (GameObject.GetComponent("WorkplaceBonuses") is not null || GameObject.GetComponent("Deteriorable") is not null) {
        return FireDamageCategory.Building;
      }

      return FireDamageCategory.Unknown;
    }

    private static bool HasCachedComponentNamed(ComponentCache componentCache, string componentTypeName) {
      foreach (var component in componentCache.AllComponents) {
        if (component is null) {
          continue;
        }

        if (component.GetType().Name == componentTypeName) {
          return true;
        }
      }

      return false;
    }

  }
}
