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
    private const float TargetEffectCooldownInSeconds = 1f;

    private FireRuntimeProjectionRuntimeState _fireRuntimeProjectionRuntimeState;
    private PrometheusWorldLoadState _prometheusWorldLoadState;
    private QuickNotificationService _quickNotificationService;

    private static bool _loggedMissingNeedManagerApi;
    private static bool _loggedNeedManagerApiResolved;
    private static bool _loggedNeedManagerScanSummary;
    private static readonly List<NeedManagerTarget> _cachedNeedManagers = new();
    private static readonly Dictionary<object, float> _nextEffectTimeByNeedManager = new();

    private static MethodInfo _managerAddPointsMethod;
    private static MethodInfo _getNeedMethod;
    private static MethodInfo _tryGetNeedMethod;
    private static MethodInfo _needAddPointsMethod;
    private static MethodInfo _needSetPointsMethod;
    private static float _lastNeedManagerRefreshTime = -NeedManagerRefreshIntervalInSeconds;
    private float _timeSinceLastUpdate;

    [Inject]
    public void InjectDependencies(
      FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState,
      PrometheusWorldLoadState prometheusWorldLoadState,
      QuickNotificationService quickNotificationService) {
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
      _prometheusWorldLoadState = prometheusWorldLoadState;
      _quickNotificationService = quickNotificationService;
    }

    public void Update() {
      if (_prometheusWorldLoadState?.WorldReady != true) {
        return;
      }

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      if (Time.time - _lastNeedManagerRefreshTime >= NeedManagerRefreshIntervalInSeconds) {
        RefreshNeedManagers();
        _lastNeedManagerRefreshTime = Time.time;
      }

      if (!_fireRuntimeProjectionRuntimeState.TryGetSnapshot(GameObject.GetInstanceID(), out var projection) || !projection.HasImpact) {
        return;
      }

      if (_cachedNeedManagers.Count == 0 || !HasNeedApplicationApi()) {
        return;
      }

      var fullExposureNeedDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(projection, 0f);
      if (!fullExposureNeedDeltas.HasEffect) {
        return;
      }

      var sourceTransform = GameObject == null ? null : GameObject.transform;
      if (sourceTransform == null) {
        return;
      }

      var sourcePosition = sourceTransform.position;

      for (var i = _cachedNeedManagers.Count - 1; i >= 0; i--) {
        var needManagerTarget = _cachedNeedManagers[i];
        if (needManagerTarget.NeedManager is null || needManagerTarget.Transform == null) {
          _cachedNeedManagers.RemoveAt(i);
          continue;
        }

        var targetPosition = needManagerTarget.Transform.position;
        var distance = Vector3.Distance(sourcePosition, targetPosition);
        if (distance > FireBeaverExposureRules.EffectRadius) {
          continue;
        }

        if (_nextEffectTimeByNeedManager.TryGetValue(needManagerTarget.NeedManager, out var nextEffectTime)
            && Time.time < nextEffectTime) {
          continue;
        }
        _nextEffectTimeByNeedManager[needManagerTarget.NeedManager] = Time.time + TargetEffectCooldownInSeconds;

        ApplyNeedDeltas(
          needManagerTarget.NeedManager,
          FireBeaverExposureRules.ComputeProximityNeedDeltas(projection, distance));
      }
    }

    internal static bool TryApplyIndoorExposure(Transform targetTransform, FireRuntimeProjectionSnapshot projection) {
      if (targetTransform == null) {
        return false;
      }

      if (_cachedNeedManagers.Count == 0 || !HasNeedApplicationApi()) {
        RefreshNeedManagersFromScene();
      }

      if (_cachedNeedManagers.Count == 0 || !HasNeedApplicationApi()) {
        return false;
      }

      var needDeltas = FireBeaverExposureRules.ComputeIndoorNeedDeltas(projection);
      if (!needDeltas.HasEffect) {
        return false;
      }

      for (var i = _cachedNeedManagers.Count - 1; i >= 0; i--) {
        var needManagerTarget = _cachedNeedManagers[i];
        if (needManagerTarget.NeedManager is null || needManagerTarget.Transform == null) {
          _cachedNeedManagers.RemoveAt(i);
          continue;
        }

        if (needManagerTarget.Transform != targetTransform) {
          continue;
        }

        ApplyNeedDeltas(needManagerTarget.NeedManager, needDeltas);
        return true;
      }

      return false;
    }

    internal static int DebugClearFireNeedEffects() {
      RefreshNeedManagersFromScene();

      var clearedCount = 0;
      for (var i = _cachedNeedManagers.Count - 1; i >= 0; i--) {
        var needManagerTarget = _cachedNeedManagers[i];
        if (needManagerTarget.NeedManager is null || needManagerTarget.Transform == null) {
          _cachedNeedManagers.RemoveAt(i);
          continue;
        }

        var clearedHeatStress = TrySetNeedPoints(needManagerTarget.NeedManager, "HeatStress", 0f);
        var clearedInjury = TrySetNeedPoints(needManagerTarget.NeedManager, "Injury", 0f);
        if (clearedHeatStress || clearedInjury) {
          clearedCount++;
        }
      }

      _nextEffectTimeByNeedManager.Clear();
      FireTelemetry.Log($"event={FireTelemetryEvents.DebugClearBeaverFireEffects} count={clearedCount} resetApiBound={_needSetPointsMethod is not null}");
      return clearedCount;
    }

    private void RefreshNeedManagers() {
      var scanSummary = RefreshNeedManagersFromScene();

      if (!_loggedNeedManagerScanSummary) {
        _loggedNeedManagerScanSummary = true;
        FireTelemetry.Log($"event={FireTelemetryEvents.BeaverEffectNeedManagerScan} componentCaches={scanSummary.ComponentCacheCount} cachedComponents={scanSummary.CachedComponentCount} needManagers={scanSummary.NeedManagerCount} apiBound={HasNeedApplicationApi()}");
      }

      if (!HasNeedApplicationApi() && !_loggedMissingNeedManagerApi) {
        _loggedMissingNeedManagerApi = true;
        const string warning = "Prometheus: compatible NeedManager API not found; beaver fire effects disabled.";
        _quickNotificationService.SendNotification(warning);
        FireTelemetry.LogWarning($"event={FireTelemetryEvents.BeaverEffectApiMissing} message=\"{warning}\"");
      }
    }

    private static NeedManagerScanSummary RefreshNeedManagersFromScene() {
      _cachedNeedManagers.Clear();
      _managerAddPointsMethod = null;
      _getNeedMethod = null;
      _tryGetNeedMethod = null;
      _needAddPointsMethod = null;
      _needSetPointsMethod = null;
      _nextEffectTimeByNeedManager.Clear();
      var componentCacheCount = 0;
      var cachedComponentCount = 0;
      var needManagerCount = 0;

      foreach (var componentCache in TimberbornComponentCacheLookup.FindLoadedComponentCaches()) {
        componentCacheCount++;

        if (!componentCache.HasCachedComponents) {
          continue;
        }

        foreach (var component in componentCache.CachedComponents) {
          cachedComponentCount++;
          if (component is null || !TimberbornCompatibility.IsNeedManagerTypeName(component.GetType().Name)) {
            continue;
          }
          needManagerCount++;

          _cachedNeedManagers.Add(new NeedManagerTarget(component, componentCache.ComponentCache.transform));

          if (!HasNeedApplicationApi()) {
            BindNeedApplicationApi(component.GetType());
          }
        }
      }

      return new NeedManagerScanSummary(componentCacheCount, cachedComponentCount, needManagerCount);
    }

    private static bool HasNeedApplicationApi() {
      return _managerAddPointsMethod is not null
             || (_getNeedMethod is not null && _needAddPointsMethod is not null)
             || (_tryGetNeedMethod is not null && _needAddPointsMethod is not null);
    }

    private static void BindNeedApplicationApi(Type needManagerType) {
      var api = TimberbornCompatibility.ProbeNeedManagerApi(needManagerType);
      if (!api.IsResolved) {
        TimberbornCompatibility.RecordProbe(TimberbornCompatibilityArea.Beaver, false, api.Description);
        return;
      }

      _managerAddPointsMethod = api.ManagerAddPointsMethod;
      _getNeedMethod = api.GetNeedMethod;
      _tryGetNeedMethod = api.TryGetNeedMethod;
      _needAddPointsMethod = api.NeedAddPointsMethod;
      _needSetPointsMethod = api.NeedSetPointsMethod;
      TimberbornCompatibility.RecordProbe(TimberbornCompatibilityArea.Beaver, true, api.Description);
      LogNeedManagerApiResolved(api.Description);
    }

    private static void LogNeedManagerApiResolved(string api) {
      if (_loggedNeedManagerApiResolved) {
        return;
      }

      _loggedNeedManagerApiResolved = true;
      FireTelemetry.Log($"event={FireTelemetryEvents.BeaverEffectApiResolved} api=\"{api}\"");
    }

    private static void TryApplyNeedDelta(object needManager, string needId, float pointsDelta) {
      if (Mathf.Approximately(pointsDelta, 0f)) {
        return;
      }

      try {
        if (_managerAddPointsMethod is not null) {
          _managerAddPointsMethod.Invoke(needManager, new object[] { needId, pointsDelta });
          return;
        }

        if (_tryGetNeedMethod is null || _needAddPointsMethod is null) {
          if (_getNeedMethod is null || _needAddPointsMethod is null) {
            return;
          }

          var need = _getNeedMethod.Invoke(needManager, new object[] { needId });
          if (need is null) {
            return;
          }

          _needAddPointsMethod.Invoke(need, new object[] { pointsDelta });
          return;
        }

        var tryGetNeedArguments = new object[] { needId, null };
        var foundNeed = (bool)_tryGetNeedMethod.Invoke(needManager, tryGetNeedArguments);
        if (!foundNeed || tryGetNeedArguments[1] is null) {
          return;
        }

        _needAddPointsMethod.Invoke(tryGetNeedArguments[1], new object[] { pointsDelta });
      } catch (TargetInvocationException) {
        // Intentionally ignored: some need managers may reject a need id for current worker type.
      } catch (ArgumentException) {
        // Intentionally ignored: fallback for unexpected signature drift.
      }
    }

    private static void ApplyNeedDeltas(object needManager, FireBeaverNeedDeltas needDeltas) {
      if (!needDeltas.HasEffect) {
        return;
      }

      TryApplyNeedDelta(needManager, "Thirst", needDeltas.ThirstDelta);
      TryApplyNeedDelta(needManager, "HeatStress", needDeltas.HeatStressDelta);
    }

    private static bool TrySetNeedPoints(object needManager, string needId, float points) {
      if (_needSetPointsMethod is null) {
        return false;
      }

      try {
        var need = TryGetNeed(needManager, needId);
        if (need is null) {
          return false;
        }

        _needSetPointsMethod.Invoke(need, new object[] { points });
        return true;
      } catch (TargetInvocationException) {
        return false;
      } catch (ArgumentException) {
        return false;
      }
    }

    private static object TryGetNeed(object needManager, string needId) {
      if (_getNeedMethod is not null) {
        return _getNeedMethod.Invoke(needManager, new object[] { needId });
      }

      if (_tryGetNeedMethod is null) {
        return null;
      }

      var tryGetNeedArguments = new object[] { needId, null };
      var foundNeed = (bool)_tryGetNeedMethod.Invoke(needManager, tryGetNeedArguments);
      return foundNeed ? tryGetNeedArguments[1] : null;
    }

    private readonly struct NeedManagerTarget {

      public readonly object NeedManager;
      public readonly Transform Transform;

      public NeedManagerTarget(object needManager, Transform transform) {
        NeedManager = needManager;
        Transform = transform;
      }

    }

    private readonly struct NeedManagerScanSummary {

      public readonly int ComponentCacheCount;
      public readonly int CachedComponentCount;
      public readonly int NeedManagerCount;

      public NeedManagerScanSummary(
        int componentCacheCount,
        int cachedComponentCount,
        int needManagerCount) {
        ComponentCacheCount = componentCacheCount;
        CachedComponentCount = cachedComponentCount;
        NeedManagerCount = needManagerCount;
      }

    }

  }
}
