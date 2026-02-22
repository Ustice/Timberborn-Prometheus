using System;
using System.Collections.Generic;
using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.QuickNotificationSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireBeaverEffectApplier : BaseComponent,
                                          IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;
    private const float NeedManagerRefreshIntervalInSeconds = 5f;
    private const float EffectRadius = 18f;

    private FireImpactRuntimeState _fireImpactRuntimeState;
    private QuickNotificationService _quickNotificationService;

    private readonly List<Component> _cachedNeedManagers = new();

    private MethodInfo _addPointsMethod;
    private float _timeSinceLastUpdate;
    private float _timeSinceLastNeedManagerRefresh;
    private bool _loggedMissingNeedManagerApi;

    [Inject]
    public void InjectDependencies(
      FireImpactRuntimeState fireImpactRuntimeState,
      QuickNotificationService quickNotificationService) {
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _quickNotificationService = quickNotificationService;
    }

    public void Update() {
      _timeSinceLastUpdate += Time.deltaTime;
      if (_timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      _timeSinceLastNeedManagerRefresh += UpdateIntervalInSeconds;
      if (_timeSinceLastNeedManagerRefresh >= NeedManagerRefreshIntervalInSeconds || _cachedNeedManagers.Count == 0) {
        RefreshNeedManagers();
        _timeSinceLastNeedManagerRefresh = 0f;
      }

      if (!_fireImpactRuntimeState.TryGetSnapshot(GameObject.GetInstanceID(), out var impactSnapshot)) {
        return;
      }

      if (_cachedNeedManagers.Count == 0 || _addPointsMethod is null) {
        return;
      }

      var thirstPenalty = -Mathf.Clamp(impactSnapshot.DehydrationPressure * 0.03f, 0f, 0.03f);
      var injuryPenalty = -Mathf.Clamp(impactSnapshot.InjuryPressure * 0.02f, 0f, 0.02f);

      if (Mathf.Approximately(thirstPenalty, 0f) && Mathf.Approximately(injuryPenalty, 0f)) {
        return;
      }

      var sourcePosition = GameObject.transform.position;

      for (var i = _cachedNeedManagers.Count - 1; i >= 0; i--) {
        var needManagerComponent = _cachedNeedManagers[i];
        if (needManagerComponent is null) {
          _cachedNeedManagers.RemoveAt(i);
          continue;
        }

        var targetPosition = needManagerComponent.transform.position;
        if (Vector3.Distance(sourcePosition, targetPosition) > EffectRadius) {
          continue;
        }

        TryApplyNeedPenalty(needManagerComponent, "Thirst", thirstPenalty);
        TryApplyNeedPenalty(needManagerComponent, "Injury", injuryPenalty);
      }
    }

    private void RefreshNeedManagers() {
      _cachedNeedManagers.Clear();
      _addPointsMethod = null;

      var components = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsSortMode.None);
      foreach (var component in components) {
        if (component is null) {
          continue;
        }

        var type = component.GetType();
        if (type.Name != "NeedManager") {
          continue;
        }

        _cachedNeedManagers.Add(component);

        if (_addPointsMethod is null) {
          var candidate = type.GetMethod(
            "AddPoints",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

          if (candidate is not null) {
            var parameters = candidate.GetParameters();
            if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(float)) {
              _addPointsMethod = candidate;
            }
          }
        }
      }

      if (_addPointsMethod is null && !_loggedMissingNeedManagerApi) {
        _loggedMissingNeedManagerApi = true;
        _quickNotificationService.SendNotification("Prometheus: NeedManager AddPoints API not found; beaver fire effects disabled.");
      }
    }

    private void TryApplyNeedPenalty(Component needManagerComponent, string needId, float pointsDelta) {
      if (_addPointsMethod is null || Mathf.Approximately(pointsDelta, 0f)) {
        return;
      }

      try {
        _addPointsMethod.Invoke(needManagerComponent, new object[] { needId, pointsDelta });
      } catch (TargetInvocationException) {
        // Intentionally ignored: some need managers may reject a need id for current worker type.
      } catch (ArgumentException) {
        // Intentionally ignored: fallback for unexpected signature drift.
      }
    }

  }
}