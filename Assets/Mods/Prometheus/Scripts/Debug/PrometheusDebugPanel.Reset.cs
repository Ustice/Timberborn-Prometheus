using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal partial class PrometheusDebugPanel {

    private void ExtinguishAllFires() {
      _fireExposureRuntimeState.BlockDebugIgnitionsForSeconds(DebugStopAllFiresIgnitionBlockSeconds);
      var liveExtinguishedCount = 0;
      foreach (var exposureController in TimberbornComponentCacheLookup.FindLoadedFireExposureControllers()) {
        if (exposureController.DebugForceExtinguish()) {
          liveExtinguishedCount++;
        }
      }

      var exposureExtinguishedCount = _fireExposureRuntimeState.ExtinguishAllBurning();
      _fireGridRuntimeState.Clear();

      var effectiveCount = exposureExtinguishedCount;
      effectiveCount = effectiveCount > liveExtinguishedCount
        ? effectiveCount
        : liveExtinguishedCount;

      FireTelemetry.Log($"event={FireTelemetryEvents.DebugStopAllFires} liveExtinguished={liveExtinguishedCount} exposureExtinguished={exposureExtinguishedCount} ignitionBlockSeconds={DebugStopAllFiresIgnitionBlockSeconds:0}");
      FireTelemetry.Log(effectiveCount > 0
        ? $"event={FireTelemetryEvents.DebugStopAllFiresResult} result=success count={effectiveCount}"
        : $"event={FireTelemetryEvents.DebugStopAllFiresResult} result=no_active_fires");

      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      RefreshLogPanel(force: true);
    }

    private void ResetAllFireState() {
      _fireVisualEffectPreviewRuntimeState.ClearAllPreviews();
      _fireGridRuntimeState.Clear();
      var resetEntityCount = 0;
      foreach (var gameObject in TimberbornComponentCacheLookup.FindLoadedPrometheusFireEntityGameObjects()) {
        ResetLoadedFireEntity(gameObject);
        resetEntityCount++;
      }

      ClearAllRuntimeStores();
      FireBeaverEffectApplier.DebugClearFireNeedEffects();

      FireTelemetry.Log($"event={FireTelemetryEvents.DebugResetFireExposure} result=success loadedEntities={resetEntityCount}");
      SetAdminFeedback($"Reset fire state for {resetEntityCount} entities");
      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      RefreshLogPanel(force: true);
      RefreshSelectionPanel();
    }

    private static void ResetLoadedFireEntity(GameObject gameObject) {
      var componentCache = gameObject.GetComponent<ComponentCache>();
      if (componentCache is not null) {
        if (componentCache.TryGetCachedComponent<FireExposureController>(out var cachedFireExposureController)) {
          cachedFireExposureController.DebugResetFireExposureState();
        }

        if (componentCache.TryGetCachedComponent<FireDamageStateController>(out var cachedFireDamageStateController)) {
          cachedFireDamageStateController.DebugResetDamageStateToHealthy();
        }

        if (componentCache.TryGetCachedComponent<FireDamageEffectApplier>(out var cachedFireDamageEffectApplier)) {
          cachedFireDamageEffectApplier.DebugRestoreHealthyState();
        }

        if (componentCache.TryGetCachedComponent<FireVisualEffectApplier>(out var cachedFireVisualEffectApplier)) {
          cachedFireVisualEffectApplier.DebugResetVisualEffects();
        }

        if (componentCache.TryGetCachedComponent<FireWorkplaceEffectApplier>(out var cachedFireWorkplaceEffectApplier)) {
          cachedFireWorkplaceEffectApplier.DebugResetFireEffects();
        }

        if (componentCache.TryGetCachedComponent<FireRecoveryController>(out var cachedFireRecoveryController)) {
          cachedFireRecoveryController.DebugResetRecoveryState();
        }

        if (componentCache.TryGetCachedComponent<FireRecoveryEffectApplier>(out var cachedFireRecoveryEffectApplier)) {
          cachedFireRecoveryEffectApplier.DebugRestoreBaseRecoveryEffects();
        }
      }

      var fireExposureController = gameObject.GetComponent<FireExposureController>();
      if (fireExposureController is not null) {
        fireExposureController.DebugResetFireExposureState();
      }

      var fireDamageStateController = gameObject.GetComponent<FireDamageStateController>();
      if (fireDamageStateController is not null) {
        fireDamageStateController.DebugResetDamageStateToHealthy();
      }

      var fireDamageEffectApplier = gameObject.GetComponent<FireDamageEffectApplier>();
      if (fireDamageEffectApplier is not null) {
        fireDamageEffectApplier.DebugRestoreHealthyState();
      }

      var fireVisualEffectApplier = gameObject.GetComponent<FireVisualEffectApplier>();
      if (fireVisualEffectApplier is not null) {
        fireVisualEffectApplier.DebugResetVisualEffects();
      }

      var fireWorkplaceEffectApplier = gameObject.GetComponent<FireWorkplaceEffectApplier>();
      if (fireWorkplaceEffectApplier is not null) {
        fireWorkplaceEffectApplier.DebugResetFireEffects();
      }

      var fireRecoveryController = gameObject.GetComponent<FireRecoveryController>();
      if (fireRecoveryController is not null) {
        fireRecoveryController.DebugResetRecoveryState();
      }

      var fireRecoveryEffectApplier = gameObject.GetComponent<FireRecoveryEffectApplier>();
      if (fireRecoveryEffectApplier is not null) {
        fireRecoveryEffectApplier.DebugRestoreBaseRecoveryEffects();
      }
    }

    private void ClearAllRuntimeStores() {
      _fireGridRuntimeState.Clear();
      _fireExposureRuntimeState.ClearSnapshotsAndIgnitionRequests();
      _fireImpactRuntimeState.ClearSnapshots();
      _fireDamageStateRuntimeState.ClearSnapshots();
      _fireRecoveryRuntimeState.ClearSnapshots();
    }

  }
}
