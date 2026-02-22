using Timberborn.BaseComponentSystem;
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
      _timeSinceLastUpdate += Time.deltaTime;
      if (_timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      var entityId = GameObject.GetInstanceID();
      if (!_fireImpactRuntimeState.TryGetSnapshot(entityId, out var impactSnapshot)) {
        return;
      }

      var pressure = GetRelevantPressure(impactSnapshot);
      var tickRate = GetTickRate(_category) * _fireTuningRuntimeState.Current.DamageTickMultiplier;
      var tickSeverityDelta = GetTickSeverityDelta(_category) * _fireTuningRuntimeState.Current.DamageTickMultiplier;

      if (pressure > 0.02f) {
        _tickProgress += pressure * tickRate;

        while (_tickProgress >= 1f) {
          _tickProgress -= 1f;
          _severity = Mathf.Clamp01(_severity + tickSeverityDelta);
          _damageTicksApplied++;
        }
      } else {
        _severity = Mathf.Clamp01(_severity - (0.05f * Mathf.Max(0.5f, _fireTuningRuntimeState.Current.QuenchingMultiplier)));
        _tickProgress = Mathf.Clamp01(_tickProgress - 0.1f);
      }

      var state = DetermineState(_severity);
      var snapshot = new FireDamageStateSnapshot(_category, state, _severity, _tickProgress, _damageTicksApplied);
      _fireDamageStateRuntimeState.SetSnapshot(entityId, snapshot);
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

    private static FireDamageState DetermineState(float severity) {
      if (severity >= 0.95f) {
        return FireDamageState.Dead;
      }

      if (severity >= 0.6f) {
        return FireDamageState.Burning;
      }

      if (severity >= 0.2f) {
        return FireDamageState.Scorched;
      }

      return FireDamageState.Healthy;
    }

    private FireDamageCategory DetectCategory() {
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

  }
}