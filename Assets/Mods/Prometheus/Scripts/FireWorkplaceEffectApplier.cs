using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireWorkplaceEffectApplier : BaseComponent,
                                             IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireImpactRuntimeState _fireImpactRuntimeState;
    private float _timeSinceLastUpdate;
    private object _workplaceBonuses;
    private PropertyInfo _workingSpeedMultiplierProperty;

    [Inject]
    public void InjectDependencies(FireImpactRuntimeState fireImpactRuntimeState) {
      _fireImpactRuntimeState = fireImpactRuntimeState;
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
      var workingSpeedMultiplier = Mathf.Clamp(1f - productivityPenalty, 0.2f, 1f);

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