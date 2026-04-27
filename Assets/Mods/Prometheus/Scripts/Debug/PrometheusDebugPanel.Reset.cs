using System;

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
      FireResetRegistryResult result;
      try {
        result = _fireResetRegistry.ResetAll("debug_reset_fire_state");
      } catch (Exception exception) {
        FireTelemetry.LogWarning($"event={FireTelemetryEvents.RuntimeResetHookFailed} reason=debug_reset_fire_state kind=Registry owner=\"PrometheusDebugPanel\" entityId=0 errorType={exception.GetType().Name} error=\"{FireResetRegistry.EscapeToken(exception.Message)}\"");
        SetAdminFeedback("Reset fire state failed before completion");
        _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
        RefreshLogPanel(force: true);
        return;
      }

      FireTelemetry.Log($"event={FireTelemetryEvents.DebugResetFireExposure} result={(result.FailureCount == 0 ? "success" : "partial_failure")} globalHooks={result.GlobalHookCount} entityHooks={result.EntityHookCount} entities={result.EntityCount} failures={result.FailureCount}");
      SetAdminFeedback(result.FailureCount == 0
        ? $"Reset fire state for {result.EntityCount} entities"
        : $"Reset fire state with {result.FailureCount} failures");
      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      RefreshLogPanel(force: true);
      RefreshSelectionPanel();
    }

  }
}
