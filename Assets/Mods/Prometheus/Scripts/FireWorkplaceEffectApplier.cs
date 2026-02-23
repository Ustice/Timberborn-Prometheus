using System.Reflection;
using System.Collections.Generic;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireWorkplaceEffectApplier : BaseComponent,
                                             IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireImpactRuntimeState _fireImpactRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private float _timeSinceLastUpdate;
    private object _workplaceBonuses;
    private PropertyInfo _workingSpeedMultiplierProperty;
    private readonly List<Behaviour> _workplaceSupportBehaviours = new();
    private readonly Dictionary<Behaviour, bool> _workplaceSupportOriginalEnabledState = new();
    private readonly List<Behaviour> _operationalBehaviours = new();
    private readonly Dictionary<Behaviour, bool> _operationalOriginalEnabledState = new();
    private bool _workplaceSupportSuppressed;
    private bool _operationalSuppressed;

    [Inject]
    public void InjectDependencies(
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState) {
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
    }

    public void Update() {
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      EnsureWorkplaceBonusesBound();
      EnsureWorkplaceSupportBehavioursBound();
      EnsureOperationalBehavioursBound();
      if (_workplaceBonuses is null || _workingSpeedMultiplierProperty is null) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      if (!_fireImpactRuntimeState.TryGetSnapshot(entityId, out var impactSnapshot)) {
        return;
      }

      var productivityPenalty = Mathf.Clamp01(impactSnapshot.BuildingDamagePressure * 0.75f);
      var baseWorkingSpeedMultiplier = Mathf.Clamp(1f - productivityPenalty, 0.2f, 1f);

      var stateWorkingSpeedMultiplier = 1f;
      var buildingDead = false;
      if (_fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageState)
          && damageState.Category == FireDamageCategory.Building) {
        buildingDead = damageState.State == FireDamageState.Dead;
        stateWorkingSpeedMultiplier = damageState.State switch {
          FireDamageState.Healthy => 1f,
          FireDamageState.Scorched => Mathf.Clamp(1f - (damageState.Severity * 0.55f), 0.45f, 0.95f),
          FireDamageState.Burning => Mathf.Clamp(1f - (damageState.Severity * 0.9f), 0.1f, 0.55f),
          FireDamageState.Dead => 0f,
          _ => 1f,
        };
      }

      var workingSpeedMultiplier = Mathf.Min(baseWorkingSpeedMultiplier, stateWorkingSpeedMultiplier);

      if (buildingDead) {
        SuppressWorkplaceSupport();
        SuppressOperationalBehaviours();
      } else {
        RestoreWorkplaceSupport();
        RestoreOperationalBehaviours();
      }

      _workingSpeedMultiplierProperty.SetValue(_workplaceBonuses, workingSpeedMultiplier);
    }

    private void EnsureWorkplaceBonusesBound() {
      if (_workplaceBonuses is not null && _workingSpeedMultiplierProperty is not null) {
        return;
      }

      foreach (var component in GameObject.GetComponents<Component>()) {
        if (component is null) {
          continue;
        }

        var componentType = component.GetType();
        if (componentType.Name != "WorkplaceBonuses") {
          continue;
        }

        var property = componentType.GetProperty(
          "WorkingSpeedMultiplier",
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (property is null || !property.CanWrite) {
          continue;
        }

        _workplaceBonuses = component;
        _workingSpeedMultiplierProperty = property;
        return;
      }
    }

    private void EnsureWorkplaceSupportBehavioursBound() {
      if (_workplaceSupportBehaviours.Count > 0 || _workplaceSupportSuppressed) {
        return;
      }

      foreach (var component in GameObject.GetComponents<Component>()) {
        if (component is not Behaviour behaviour) {
          continue;
        }

        var componentTypeName = component.GetType().Name;
        if (!IsWorkplaceSupportComponentName(componentTypeName)) {
          continue;
        }

        if (!_workplaceSupportOriginalEnabledState.ContainsKey(behaviour)) {
          _workplaceSupportOriginalEnabledState[behaviour] = behaviour.enabled;
        }

        _workplaceSupportBehaviours.Add(behaviour);
      }
    }

    private void SuppressWorkplaceSupport() {
      if (_workplaceSupportSuppressed) {
        return;
      }

      for (var i = 0; i < _workplaceSupportBehaviours.Count; i++) {
        var behaviour = _workplaceSupportBehaviours[i];
        if (behaviour == null) {
          continue;
        }

        behaviour.enabled = false;
      }

      _workplaceSupportSuppressed = true;
      FireTelemetry.Log($"event=workplace_support_suppressed entity={GameObject.name} id={GameObject.GetInstanceID()} reason=building_dead components={_workplaceSupportBehaviours.Count}");
    }

    private void RestoreWorkplaceSupport() {
      if (!_workplaceSupportSuppressed) {
        return;
      }

      foreach (var pair in _workplaceSupportOriginalEnabledState) {
        var behaviour = pair.Key;
        if (behaviour == null) {
          continue;
        }

        behaviour.enabled = pair.Value;
      }

      _workplaceSupportSuppressed = false;
      FireTelemetry.Log($"event=workplace_support_restored entity={GameObject.name} id={GameObject.GetInstanceID()} components={_workplaceSupportOriginalEnabledState.Count}");
    }

    private void EnsureOperationalBehavioursBound() {
      if (_operationalBehaviours.Count > 0 || _operationalSuppressed) {
        return;
      }

      foreach (var component in GameObject.GetComponents<Component>()) {
        if (component is not Behaviour behaviour) {
          continue;
        }

        var componentTypeName = component.GetType().Name;
        if (!IsOperationalComponentName(componentTypeName)) {
          continue;
        }

        if (!_operationalOriginalEnabledState.ContainsKey(behaviour)) {
          _operationalOriginalEnabledState[behaviour] = behaviour.enabled;
        }

        _operationalBehaviours.Add(behaviour);
      }
    }

    private void SuppressOperationalBehaviours() {
      if (_operationalSuppressed) {
        return;
      }

      for (var i = 0; i < _operationalBehaviours.Count; i++) {
        var behaviour = _operationalBehaviours[i];
        if (behaviour == null) {
          continue;
        }

        behaviour.enabled = false;
      }

      _operationalSuppressed = true;
      FireTelemetry.Log($"event=building_operations_suppressed entity={GameObject.name} id={GameObject.GetInstanceID()} reason=building_dead components={_operationalBehaviours.Count}");
    }

    private void RestoreOperationalBehaviours() {
      if (!_operationalSuppressed) {
        return;
      }

      foreach (var pair in _operationalOriginalEnabledState) {
        var behaviour = pair.Key;
        if (behaviour == null) {
          continue;
        }

        behaviour.enabled = pair.Value;
      }

      _operationalSuppressed = false;
      FireTelemetry.Log($"event=building_operations_restored entity={GameObject.name} id={GameObject.GetInstanceID()} components={_operationalOriginalEnabledState.Count}");
    }

    private static bool IsWorkplaceSupportComponentName(string componentTypeName) {
      if (string.IsNullOrWhiteSpace(componentTypeName)) {
        return false;
      }

      if (componentTypeName.Contains("Bonuses")) {
        return false;
      }

      return componentTypeName == "Workplace"
             || componentTypeName.EndsWith("Workplace")
             || componentTypeName.Contains("WorkplaceWorker")
             || componentTypeName.Contains("WorkplaceEmployee");
    }

    private static bool IsOperationalComponentName(string componentTypeName) {
      if (string.IsNullOrWhiteSpace(componentTypeName)) {
        return false;
      }

      if (componentTypeName.Contains("Fire")
          || componentTypeName.Contains("Workplace")
          || componentTypeName.Contains("Bonuses")
          || componentTypeName == "Deteriorable") {
        return false;
      }

      return componentTypeName == "Manufactory"
             || componentTypeName == "Workshop"
             || componentTypeName == "SimpleManufactoryBehaviors"
             || componentTypeName.Contains("Manufactory")
             || componentTypeName.Contains("Production")
             || componentTypeName.Contains("Workshop")
             || componentTypeName.Contains("Factory")
             || componentTypeName.Contains("Crafter")
             || componentTypeName.Contains("Recipe");
    }

  }
}