using TimberUi;
using UnityEngine.UIElements;

namespace Mods.Prometheus.Scripts {
  internal partial class PrometheusDebugPanel {

    private VisualElement BuildCommandsPanel() {
      var controls = new VisualElement();

      var commandsSection = CreateSection(controls, "Commands");
      var commandsRow = commandsSection.AddHorizontalContainer();

      var resetFireStateButton = AddGameButtonTo(commandsRow, "Reset Fire State", ResetAllFireState, destructive: true).SetMarginRight(8);
      resetFireStateButton.tooltip = "Reset Prometheus fire exposure, damage, ash/dead state, workplace disabled state, and runtime snapshots for all loaded fire entities.";

      var stopAllFiresButton = AddGameButtonTo(commandsRow, "Stop Fires", ExtinguishAllFires, destructive: true).SetMarginRight(8);
      stopAllFiresButton.tooltip = "Immediately extinguish all currently burning entities tracked by Prometheus.";

      var clearBeaverEffectsButton = AddGameButtonTo(commandsRow, "Clear Beavers", ClearBeaverFireEffects).SetMarginRight(8);
      clearBeaverEffectsButton.tooltip = "Clear Prometheus HeatStress and mod-applied Injury debt from currently loaded beavers.";

      var clearButton = AddGameButtonTo(commandsRow, "Clear Log", () => {
        FireTelemetry.ClearInGameLog();
        _lastObservedEntryCount = 0;
        SetUnreadCount(0);
        RefreshLogPanel(force: true);
      });
      clearButton.tooltip = "Clear all in-game fire log entries.";

      _adminFeedbackLabel = commandsRow.AddGameLabel();

      return controls;
    }

    private void ClearBeaverFireEffects() {
      var clearedCount = FireBeaverEffectApplier.DebugClearFireNeedEffects();
      FireTelemetry.Log(clearedCount > 0
        ? $"event={FireTelemetryEvents.DebugClearBeaverFireEffectsResult} result=success count={clearedCount}"
        : $"event={FireTelemetryEvents.DebugClearBeaverFireEffectsResult} result=none_found");
      SetAdminFeedback(clearedCount > 0
        ? $"Cleared {clearedCount} beavers"
        : "No beavers found");

      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      RefreshLogPanel(force: true);
    }

    private void SetAdminFeedback(string message) {
      if (_adminFeedbackLabel == null) {
        return;
      }

      _adminFeedbackLabel.text = message;
    }

  }
}
