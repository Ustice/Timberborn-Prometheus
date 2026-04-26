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

    private FireImpactRuntimeState _fireImpactRuntimeState;
    private QuickNotificationService _quickNotificationService;

    private static bool _loggedMissingNeedManagerApi;
    private static bool _loggedNeedManagerApiResolved;
    private static bool _loggedNeedManagerScanSummary;
    private static FireResetRegistration _resetRegistration = FireResetRegistration.Empty;
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
      FireImpactRuntimeState fireImpactRuntimeState,
      QuickNotificationService quickNotificationService,
      FireResetRegistry fireResetRegistry) {
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _quickNotificationService = quickNotificationService;
      if (_resetRegistration == FireResetRegistration.Empty) {
        _resetRegistration = fireResetRegistry.RegisterGlobal(
          FireResetHookKind.BeaverEffect,
          nameof(FireBeaverEffectApplier),
          () => DebugClearFireNeedEffects());
      }
    }

    public void Update() {
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      if (Time.time - _lastNeedManagerRefreshTime >= NeedManagerRefreshIntervalInSeconds || _cachedNeedManagers.Count == 0) {
        RefreshNeedManagers();
        _lastNeedManagerRefreshTime = Time.time;
      }

      if (!_fireImpactRuntimeState.TryGetSnapshot(GameObject.GetInstanceID(), out var impactSnapshot)) {
        return;
      }

      if (_cachedNeedManagers.Count == 0 || !HasNeedApplicationApi()) {
        return;
      }

      var fullExposureNeedDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, 0f);
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
          FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, distance));
      }
    }

    internal static bool TryApplyIndoorExposure(Transform targetTransform, FireImpactSnapshot impactSnapshot) {
      if (targetTransform == null) {
        return false;
      }

      if (_cachedNeedManagers.Count == 0 || !HasNeedApplicationApi()) {
        RefreshNeedManagersFromScene();
      }

      if (_cachedNeedManagers.Count == 0 || !HasNeedApplicationApi()) {
        return false;
      }

      var needDeltas = FireBeaverExposureRules.ComputeIndoorNeedDeltas(impactSnapshot);
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

        TrySetNeedPoints(needManagerTarget.NeedManager, "HeatStress", 0f);
        TrySetNeedPoints(needManagerTarget.NeedManager, "Injury", 0f);
        clearedCount++;
      }

      _nextEffectTimeByNeedManager.Clear();
      FireTelemetry.Log($"event={FireTelemetryEvents.DebugClearBeaverFireEffects} count={clearedCount}");
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
          if (component is null || component.GetType().Name != "NeedManager") {
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
      var managerAddPointsCandidate = needManagerType.GetMethod(
        "AddPoints",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      if (HasParameters(managerAddPointsCandidate, typeof(string), typeof(float))) {
        _managerAddPointsMethod = managerAddPointsCandidate;
        LogNeedManagerApiResolved("NeedManager.AddPoints(string,float)");
        return;
      }

      var getNeedCandidate = needManagerType.GetMethod(
        "GetNeed",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        null,
        new[] { typeof(string) },
        null);

      if (getNeedCandidate is not null && TryBindNeedMethods(getNeedCandidate.ReturnType)) {
        _getNeedMethod = getNeedCandidate;
        LogNeedManagerApiResolved("NeedManager.GetNeed(string) + Need.AddPoints(float)");
        return;
      }

      var tryGetNeedCandidate = needManagerType.GetMethod(
        "TryGetNeed",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      if (tryGetNeedCandidate is null) {
        return;
      }

      var parameters = tryGetNeedCandidate.GetParameters();
      if (parameters.Length != 2
          || parameters[0].ParameterType != typeof(string)
          || !parameters[1].ParameterType.IsByRef) {
        return;
      }

      var needType = parameters[1].ParameterType.GetElementType();
      if (!TryBindNeedMethods(needType)) {
        return;
      }

      _tryGetNeedMethod = tryGetNeedCandidate;
      LogNeedManagerApiResolved("NeedManager.TryGetNeed + Need.AddPoints(float)");
    }

    private static bool TryBindNeedMethods(Type needType) {
      var needAddPointsCandidate = needType?.GetMethod(
        "AddPoints",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        null,
        new[] { typeof(float) },
        null);
      var needSetPointsCandidate = needType?.GetMethod(
        "SetPoints",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        null,
        new[] { typeof(float) },
        null);

      if (needAddPointsCandidate is null) {
        return false;
      }

      _needAddPointsMethod = needAddPointsCandidate;
      _needSetPointsMethod = needSetPointsCandidate;
      return true;
    }

    private static bool HasParameters(MethodInfo methodInfo, params Type[] parameterTypes) {
      if (methodInfo is null) {
        return false;
      }

      var parameters = methodInfo.GetParameters();
      if (parameters.Length != parameterTypes.Length) {
        return false;
      }

      for (var i = 0; i < parameterTypes.Length; i++) {
        if (parameters[i].ParameterType != parameterTypes[i]) {
          return false;
        }
      }

      return true;
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

    private static void TrySetNeedPoints(object needManager, string needId, float points) {
      if (_needSetPointsMethod is null) {
        TryApplyNeedDelta(needManager, needId, 10f);
        return;
      }

      try {
        var need = TryGetNeed(needManager, needId);
        if (need is null) {
          return;
        }

        _needSetPointsMethod.Invoke(need, new object[] { points });
      } catch (TargetInvocationException) {
        // Intentionally ignored: some need managers may reject a need id for current worker type.
      } catch (ArgumentException) {
        // Intentionally ignored: fallback for unexpected signature drift.
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
