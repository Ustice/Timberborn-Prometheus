using Bindito.Core;
using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.WorkSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireDamageStateController : BaseComponent,
                                            IAwakableComponent,
                                            IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireRuntimeProjectionRuntimeState _fireRuntimeProjectionRuntimeState;
    private FireTuningRuntimeState _fireTuningRuntimeState;
    private PrometheusWorldLoadState _prometheusWorldLoadState;
    private float _timeSinceLastUpdate;
    private FireDamageCategory _category = FireDamageCategory.Unknown;
    private float _severity;
    private float _tickProgress;
    private int _damageTicksApplied;
    private bool _categoryDetected;
    private bool _reachedTerminalDeadState;

    [Inject]
    public void InjectDependencies(
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState,
      FireTuningRuntimeState fireTuningRuntimeState,
      PrometheusWorldLoadState prometheusWorldLoadState) {
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
      _fireTuningRuntimeState = fireTuningRuntimeState;
      _prometheusWorldLoadState = prometheusWorldLoadState;
    }

    public void Awake() {
    }

    public void Update() {
      if (!EnsureWorldReadyAndCategoryDetected()) {
        return;
      }

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      _fireRuntimeProjectionRuntimeState.TryGetSnapshot(entityId, out var projection);
      var previousState = DeterminePreviousState(entityId);
      if (FireDamageStateRules.IsTerminalDeadState(_category, previousState)) {
        _reachedTerminalDeadState = true;
        _severity = 1f;
        _tickProgress = 1f;
      }

      if (_category == FireDamageCategory.Tree
          && projection.HasExposure
          && projection.Exposure.FuelConsumed >= 0.25f) {
        _severity = 1f;
        _tickProgress = 1f;
        var deadTreeSnapshot = new FireDamageStateSnapshot(_category, FireDamageState.Dead, _severity, _tickProgress, _damageTicksApplied);
        _fireDamageStateRuntimeState.SetSnapshot(entityId, deadTreeSnapshot);
        _fireRuntimeProjectionRuntimeState.SetDamageState(entityId, deadTreeSnapshot);
        return;
      }

      if (!_fireRuntimeProjectionRuntimeState.TryGetSnapshot(entityId, out projection) || !projection.HasImpact) {
        return;
      }

      var pressure = FireRuntimeProjectionRules.GetDamagePressure(projection, _category);
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
        var canDecay = !FireDamageStateRules.IsTerminalDeadState(_category, previousState);
        if (canDecay) {
          _severity = Mathf.Clamp01(_severity - 0.05f);
          _tickProgress = Mathf.Clamp01(_tickProgress - 0.1f);
        } else {
          _severity = 1f;
          _tickProgress = 1f;
        }
      }

      var state = FireDamageStateRules.DetermineState(_severity);
      if (FireDamageStateRules.IsTerminalDeadState(_category, previousState)) {
        state = FireDamageState.Dead;
        _severity = 1f;
        _tickProgress = 1f;
      }

      if (state == FireDamageState.Dead) {
        _reachedTerminalDeadState = FireDamageStateRules.IsTerminalDeadState(_category, state);
      }

      var snapshot = new FireDamageStateSnapshot(_category, state, _severity, _tickProgress, _damageTicksApplied);
      _fireDamageStateRuntimeState.SetSnapshot(entityId, snapshot);
      _fireRuntimeProjectionRuntimeState.SetDamageState(entityId, snapshot);
    }

    internal void DebugResetDamageStateToHealthy() {
      var entityId = GameObject.GetInstanceID();
      _severity = 0f;
      _tickProgress = 0f;
      _damageTicksApplied = 0;
      _reachedTerminalDeadState = false;
      _fireDamageStateRuntimeState.SetSnapshot(
        entityId,
        new FireDamageStateSnapshot(_category, FireDamageState.Healthy, 0f, 0f, 0));
      _fireRuntimeProjectionRuntimeState.SetDamageState(
        entityId,
        new FireDamageStateSnapshot(_category, FireDamageState.Healthy, 0f, 0f, 0));
    }

    private FireDamageState DeterminePreviousState(int entityId) {
      if (_reachedTerminalDeadState) {
        return FireDamageState.Dead;
      }

      if (_fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageSnapshot)) {
        return damageSnapshot.State;
      }

      if (_fireRuntimeProjectionRuntimeState.TryGetSnapshot(entityId, out var projection)
          && projection.HasDamageState) {
        return projection.DamageState.State;
      }

      return FireDamageStateRules.DetermineState(_severity);
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

    private bool EnsureWorldReadyAndCategoryDetected() {
      if (_prometheusWorldLoadState?.WorldReady != true) {
        return false;
      }

      if (_categoryDetected) {
        return true;
      }

      _category = DetectCategory();
      _categoryDetected = true;
      return true;
    }

    private FireDamageCategory DetectCategory() {
      var componentCache = GameObject.GetComponent<ComponentCache>();
      if (componentCache is not null) {
        if (componentCache.TryGetCachedComponent<Workplace>(out _)) {
          return FireDamageCategory.Building;
        }

        var cachedCategory = TimberbornCompatibility.ClassifyDamageCategory(GetCachedComponentTypeNames(componentCache), false);
        if (cachedCategory != FireDamageCategory.Unknown) {
          return cachedCategory;
        }
      }

      var category = TimberbornCompatibility.ClassifyDamageCategory(GetFallbackComponentTypeNames(), false);
      TimberbornCompatibility.RecordProbe(
        TimberbornCompatibilityArea.Damage,
        category != FireDamageCategory.Unknown,
        category == FireDamageCategory.Unknown ? "damage category classifier found no Timberborn type" : $"damage category {category}");
      return category;
    }

    private static IEnumerable<string> GetCachedComponentTypeNames(ComponentCache componentCache) {
      if (!TimberbornComponentCacheLookup.TryGetCachedComponents(componentCache, out var cachedComponents)) {
        yield break;
      }

      foreach (var component in cachedComponents) {
        if (component is null) {
          continue;
        }

        yield return component.GetType().Name;
      }
    }

    private IEnumerable<string> GetFallbackComponentTypeNames() {
      if (GameObject.GetComponent("TreeComponent") is not null) {
        yield return "TreeComponent";
      }

      if (GameObject.GetComponent("Growable") is not null) {
        yield return "Growable";
      }

      if (GameObject.GetComponent("WorkplaceBonuses") is not null) {
        yield return "WorkplaceBonuses";
      }

      if (GameObject.GetComponent("Deteriorable") is not null) {
        yield return "Deteriorable";
      }
    }

  }
}
