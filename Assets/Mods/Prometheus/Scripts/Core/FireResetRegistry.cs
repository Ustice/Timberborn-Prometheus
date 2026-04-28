using System;
using System.Collections.Generic;
using System.Linq;
#if !PROMETHEUS_TESTS
using Timberborn.BaseComponentSystem;
#endif
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal enum FireResetHookKind {
    GridState,
    SourceState,
    DamageState,
    DamageEffect,
    WorkplaceEffect,
    BeaverEffect,
    RecoveryState,
    RecoveryEffect,
    VisualEffect,
    PreviewState,
    AshState,
  }

  internal sealed class FireResetRegistry {

    private readonly List<FireResetHook> _globalHooks = new();
    private readonly Dictionary<int, List<FireResetHook>> _entityHooksByEntityId = new();
    private int _nextRegistrationId;

    public FireResetRegistry(
      FireGridRuntimeState fireGridRuntimeState,
      FireExposureRuntimeState fireExposureRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState,
      FertileAshRecoveredGoodStackTelemetryState fertileAshRecoveredGoodStackTelemetryState,
      FireBurnedGroundAshDepositRuntimeState burnedGroundAshDepositRuntimeState,
      FireFieldAmendmentRuntimeState fireFieldAmendmentRuntimeState,
      FireVisualEffectPreviewRuntimeState fireVisualEffectPreviewRuntimeState) {
      RegisterGlobal(FireResetHookKind.GridState, nameof(FireGridRuntimeState), fireGridRuntimeState.Clear);
      RegisterGlobal(FireResetHookKind.SourceState, nameof(FireExposureRuntimeState), fireExposureRuntimeState.ClearSnapshotsAndIgnitionRequests);
      RegisterGlobal(FireResetHookKind.DamageState, nameof(FireImpactRuntimeState), fireImpactRuntimeState.ClearSnapshots);
      RegisterGlobal(FireResetHookKind.DamageState, nameof(FireDamageStateRuntimeState), fireDamageStateRuntimeState.ClearSnapshots);
      RegisterGlobal(FireResetHookKind.DamageState, nameof(FireRuntimeProjectionRuntimeState), fireRuntimeProjectionRuntimeState.ClearSnapshots);
      RegisterGlobal(FireResetHookKind.AshState, nameof(FireRecoveryRuntimeState), fireRecoveryRuntimeState.ClearSnapshots);
      RegisterGlobal(FireResetHookKind.AshState, nameof(FertileAshRecoveredGoodStackTelemetryState), () => ResetFertileAshTelemetryState(fertileAshRecoveredGoodStackTelemetryState));
      RegisterGlobal(FireResetHookKind.AshState, nameof(FireBurnedGroundAshDepositRuntimeState), () => ResetBurnedGroundAshDeposits(burnedGroundAshDepositRuntimeState));
      RegisterGlobal(FireResetHookKind.AshState, nameof(FireFieldAmendmentRuntimeState), fireFieldAmendmentRuntimeState.ClearAmendments);
      RegisterGlobal(FireResetHookKind.PreviewState, nameof(FireVisualEffectPreviewRuntimeState), fireVisualEffectPreviewRuntimeState.ClearAllPreviews);
#if !PROMETHEUS_TESTS
      RegisterGlobal(FireResetHookKind.BeaverEffect, nameof(FireBeaverEffectApplier), () => FireBeaverEffectApplier.DebugClearFireNeedEffects());
      RegisterGlobal(FireResetHookKind.AshState, nameof(FireBurnedGroundAshDepositMarkerSpawner), () => FireBurnedGroundAshDepositMarkerSpawner.ClearAllMarkers());
#endif
    }

    public FireResetRegistration RegisterGlobal(FireResetHookKind kind, string owner, Action reset) {
      var hook = CreateHook(kind, 0, owner, reset);
      _globalHooks.Add(hook);
      return new FireResetRegistration(() => _globalHooks.Remove(hook));
    }

    public FireResetRegistration RegisterEntity(int entityId, FireResetHookKind kind, string owner, Action reset) {
      if (entityId == 0) {
        return FireResetRegistration.Empty;
      }

      var hook = CreateHook(kind, entityId, owner, reset);
      if (!_entityHooksByEntityId.TryGetValue(entityId, out var hooks)) {
        hooks = new List<FireResetHook>();
        _entityHooksByEntityId[entityId] = hooks;
      }

      hooks.Add(hook);
      return new FireResetRegistration(() => UnregisterEntityHook(entityId, hook));
    }

    public FireResetRegistryResult ResetAll(string reason) {
      var globalHooks = _globalHooks.ToArray();
      var entityHooks = CreateLoadedEntityHooks()
        .Concat(_entityHooksByEntityId.SelectMany(pair => pair.Value))
        .ToArray();
      var hooks = globalHooks.Concat(entityHooks).ToArray();

      var entityCount = entityHooks
        .Where(hook => hook.EntityId != 0)
        .Select(hook => hook.EntityId)
        .Distinct()
        .Count();
      FireTelemetry.Log($"event={FireTelemetryEvents.RuntimeResetRegistryStarted} reason={reason} globalHooks={globalHooks.Length} entityHooks={entityHooks.Length} entities={entityCount}");

      var failures = 0;
      foreach (var hook in hooks) {
        if (TryRunHook(hook, reason, out var exception)) {
          continue;
        }

        failures++;
        LogHookFailure(hook, reason, exception);
        if (exception is MissingReferenceException && hook.EntityId != 0) {
          UnregisterEntityHook(hook.EntityId, hook);
        }
      }

      var result = new FireResetRegistryResult(globalHooks.Length, entityHooks.Length, entityCount, failures);

      FireTelemetry.Log($"event={FireTelemetryEvents.RuntimeResetRegistryCompleted} reason={reason} globalHooks={result.GlobalHookCount} entityHooks={result.EntityHookCount} entities={result.EntityCount} failures={result.FailureCount} kinds=\"{CreateKindSummary(hooks)}\"");
      return result;
    }

    private FireResetHook CreateHook(FireResetHookKind kind, int entityId, string owner, Action reset) =>
      new(++_nextRegistrationId, kind, entityId, string.IsNullOrWhiteSpace(owner) ? "unknown" : owner, reset);

    private static void ResetFertileAshTelemetryState(FertileAshRecoveredGoodStackTelemetryState telemetryState) {
      var snapshot = telemetryState.ClearForReset();
      FireTelemetry.Log($"event={FireTelemetryEvents.FertileAshResetState} queuedStacks={snapshot.QueuedStackCount} queuedAmount={snapshot.QueuedAshAmount} source={snapshot.LastSourceAttribution} sourceKind={snapshot.LastSourceKind} damageCategory={snapshot.LastDamageCategory} cropContext={snapshot.LastCropContext} nativeStacksDestroyed=0 reason=native_recovered_good_stack_owned_by_timberborn");
    }

    private static void ResetBurnedGroundAshDeposits(FireBurnedGroundAshDepositRuntimeState state) {
      var depositCount = state.ClearDeposits();
      FireTelemetry.Log($"event={FireTelemetryEvents.BurnedGroundAshDepositsReset} depositsCleared={depositCount}");
    }

    private IEnumerable<FireResetHook> CreateLoadedEntityHooks() {
#if PROMETHEUS_TESTS
      return Array.Empty<FireResetHook>();
#else
      foreach (var gameObject in TimberbornComponentCacheLookup.FindLoadedPrometheusFireEntityGameObjects()) {
        var entityId = gameObject.GetInstanceID();
        if (TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<FireExposureController>(gameObject, out var fireExposureController)) {
          yield return CreateHook(FireResetHookKind.SourceState, entityId, nameof(FireExposureController), fireExposureController.DebugResetFireExposureState);
        }

        if (TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<FireDamageStateController>(gameObject, out var fireDamageStateController)) {
          yield return CreateHook(FireResetHookKind.DamageState, entityId, nameof(FireDamageStateController), fireDamageStateController.DebugResetDamageStateToHealthy);
        }

        if (TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<FireDamageEffectApplier>(gameObject, out var fireDamageEffectApplier)) {
          yield return CreateHook(FireResetHookKind.DamageEffect, entityId, nameof(FireDamageEffectApplier), fireDamageEffectApplier.DebugRestoreHealthyState);
        }

        if (TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<FireWorkplaceEffectApplier>(gameObject, out var fireWorkplaceEffectApplier)) {
          yield return CreateHook(FireResetHookKind.WorkplaceEffect, entityId, nameof(FireWorkplaceEffectApplier), fireWorkplaceEffectApplier.DebugResetFireEffects);
        }

        if (TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<FireRecoveryController>(gameObject, out var fireRecoveryController)) {
          yield return CreateHook(FireResetHookKind.RecoveryState, entityId, nameof(FireRecoveryController), fireRecoveryController.DebugResetRecoveryState);
        }

        if (TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<FireRecoveryEffectApplier>(gameObject, out var fireRecoveryEffectApplier)) {
          yield return CreateHook(FireResetHookKind.RecoveryEffect, entityId, nameof(FireRecoveryEffectApplier), fireRecoveryEffectApplier.DebugRestoreBaseRecoveryEffects);
        }

        if (TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<FireVisualEffectApplier>(gameObject, out var fireVisualEffectApplier)) {
          yield return CreateHook(FireResetHookKind.VisualEffect, entityId, nameof(FireVisualEffectApplier), fireVisualEffectApplier.DebugResetVisualEffects);
        }
      }
#endif
    }

    private void UnregisterEntityHook(int entityId, FireResetHook hook) {
      if (!_entityHooksByEntityId.TryGetValue(entityId, out var hooks)) {
        return;
      }

      hooks.Remove(hook);
      if (hooks.Count == 0) {
        _entityHooksByEntityId.Remove(entityId);
      }
    }

    private static bool TryRunHook(FireResetHook hook, string reason, out Exception exception) {
      exception = null;
      if (hook.Reset is null) {
        exception = new InvalidOperationException("Reset hook has no callback.");
        return false;
      }

      try {
        hook.Reset();
        return true;
      } catch (Exception caughtException) {
        exception = caughtException;
        return false;
      }
    }

    private static void LogHookFailure(FireResetHook hook, string reason, Exception exception) {
      var exceptionType = exception?.GetType().Name ?? "Unknown";
      var message = EscapeToken(exception?.Message ?? "Unknown reset hook failure.");
      FireTelemetry.LogWarning($"event={FireTelemetryEvents.RuntimeResetHookFailed} reason={reason} kind={hook.Kind} owner=\"{EscapeToken(hook.Owner)}\" entityId={hook.EntityId} errorType={exceptionType} error=\"{message}\"");
    }

    private static string CreateKindSummary(IEnumerable<FireResetHook> hooks) =>
      string.Join(",", hooks
        .GroupBy(hook => hook.Kind)
        .OrderBy(group => group.Key.ToString())
        .Select(group => $"{group.Key}:{group.Count()}"));

    internal static string EscapeToken(string value) =>
      string.IsNullOrEmpty(value)
        ? string.Empty
        : value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private readonly struct FireResetHook {

      public int RegistrationId { get; }
      public FireResetHookKind Kind { get; }
      public int EntityId { get; }
      public string Owner { get; }
      public Action Reset { get; }

      public FireResetHook(int registrationId, FireResetHookKind kind, int entityId, string owner, Action reset) {
        RegistrationId = registrationId;
        Kind = kind;
        EntityId = entityId;
        Owner = owner;
        Reset = reset;
      }

    }

  }

  internal sealed class FireResetRegistration : IDisposable {

    public static readonly FireResetRegistration Empty = new(null);

    private Action _dispose;

    public FireResetRegistration(Action dispose) {
      _dispose = dispose;
    }

    public void Dispose() {
      _dispose?.Invoke();
      _dispose = null;
    }

  }

  internal readonly struct FireResetRegistryResult {

    public int GlobalHookCount { get; }
    public int EntityHookCount { get; }
    public int EntityCount { get; }
    public int FailureCount { get; }

    public FireResetRegistryResult(int globalHookCount, int entityHookCount, int entityCount, int failureCount) {
      GlobalHookCount = globalHookCount;
      EntityHookCount = entityHookCount;
      EntityCount = entityCount;
      FailureCount = failureCount;
    }

  }
}
