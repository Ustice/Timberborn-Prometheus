using TimberUi;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mods.Prometheus.Scripts {
  internal partial class PrometheusDebugPanel {

    private void PollQaInstructionAndRefresh() {
      if (!IsOpen || _activeTab != PrometheusDebugPanelTab.Qa) {
        return;
      }

      RefreshQaPanel(force: false);
    }

    private void RefreshQaPanel(bool force) {
      if (!IsOpen || _qaInstructionLabel == null) {
        return;
      }

      var instruction = _qaExchange.ReadInstruction();
      if (!force && instruction.Signature == _lastRenderedQaSignature) {
        return;
      }

      _qaInstruction = instruction;
      _lastRenderedQaSignature = instruction.Signature;

      if (_qaPathsLabel != null) {
        _qaPathsLabel.text = $"Instructions: {_qaExchange.InstructionsPath}\nResults: {_qaExchange.ResultsPath}";
      }

      _qaInstructionLabel.text = instruction.Text;
      SetQaStatus(instruction.Exists
        ? $"Updated {instruction.LastUpdatedUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
        : "No instruction file yet.");
    }

    private void RecordQaResult(PrometheusQaResult result) {
      var instruction = _qaInstruction.Text == null ? _qaExchange.ReadInstruction() : _qaInstruction;
      var note = _qaNoteField?.value ?? string.Empty;
      if (_qaExchange.RecordResult(result, note, instruction, out var message)) {
        if (_qaNoteField != null) {
          _qaNoteField.value = string.Empty;
        }
      }

      SetQaStatus(message);
    }

    private void CopyQaPanelText() {
      var instruction = _qaInstruction.Text == null ? _qaExchange.ReadInstruction() : _qaInstruction;
      GUIUtility.systemCopyBuffer =
        $"Prometheus QA\nInstructions: {_qaExchange.InstructionsPath}\nResults: {_qaExchange.ResultsPath}\n\n{instruction.Text}";
      SetQaStatus("QA text copied.");
    }

    private void SetQaStatus(string message) {
      if (_qaStatusLabel != null) {
        _qaStatusLabel.text = message;
      }
    }

    private VisualElement BuildQaPanel() {
      var controls = new VisualElement();

      var instructionSection = CreateSection(controls, "QA Instructions");
      _qaPathsLabel = instructionSection.AddGameLabel();
      _qaInstructionLabel = instructionSection.AddGameLabel();

      var noteSection = CreateSection(controls, "Result Note");
      _qaNoteField = noteSection.AddTextField();
      _qaNoteField.multiline = true;
      _qaNoteField.style.minHeight = 58;

      var actionsSection = CreateSection(controls, "Result");
      var actionsRow = actionsSection.AddHorizontalContainer();

      var passedButton = AddGameButtonTo(actionsRow, "Passed", () => RecordQaResult(PrometheusQaResult.Passed)).SetMarginRight(8);
      passedButton.tooltip = "Record the current QA instruction as passed.";

      var failedButton = AddGameButtonTo(actionsRow, "Failed", () => RecordQaResult(PrometheusQaResult.Failed), destructive: true).SetMarginRight(8);
      failedButton.tooltip = "Record the current QA instruction as failed.";

      var blockedButton = AddGameButtonTo(actionsRow, "Blocked", () => RecordQaResult(PrometheusQaResult.Blocked)).SetMarginRight(8);
      blockedButton.tooltip = "Record the current QA instruction as blocked.";

      var refreshButton = AddGameButtonTo(actionsRow, "Refresh", () => RefreshQaPanel(force: true)).SetMarginRight(8);
      refreshButton.tooltip = "Read the latest QA instruction file now.";

      var copyButton = AddGameButtonTo(actionsRow, "Copy", CopyQaPanelText);
      copyButton.tooltip = "Copy current QA instruction and file paths.";

      _qaStatusLabel = actionsSection.AddGameLabel();
      RefreshQaPanel(force: true);
      return controls;
    }

  }
}
