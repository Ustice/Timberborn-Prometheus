using System.Reflection;
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

    [Inject]
    public void InjectDependencies(
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState) {
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
    }

    public void Update() {
      _timeSinceLastUpdate += Time.deltaTime;
      if (_timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      EnsureWorkplaceBonusesBound();
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
      if (_fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageState)
          && damageState.Category == FireDamageCategory.Building) {
        stateWorkingSpeedMultiplier = damageState.State switch {
          FireDamageState.Healthy => 1f,
          FireDamageState.Scorched => Mathf.Clamp(1f - (damageState.Severity * 0.55f), 0.45f, 0.95f),
          FireDamageState.Burning => Mathf.Clamp(1f - (damageState.Severity * 0.9f), 0.1f, 0.55f),
          FireDamageState.Dead => 0f,
          _ => 1f,
        };
      }

      var workingSpeedMultiplier = Mathf.Min(baseWorkingSpeedMultiplier, stateWorkingSpeedMultiplier);

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

  }
}