using System;
using TimberUi;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mods.Prometheus.Scripts {
  internal partial class PrometheusDebugPanel {

    private VisualElement BuildSelectionPanel() {
      var controls = new VisualElement();
      var selectionSection = CreateSection(controls, "Selection");

      _selectionContainer = selectionSection.AddChild();

      var selectionToolbar = _selectionContainer.AddHorizontalContainer();

      _selectionTitleLabel = selectionToolbar.AddGameLabel(_selectedEntityTitle).SetMarginRight(8);

      _selectionCopyButton = AddGameButtonTo(selectionToolbar, "Copy", CopySelectedEntityDebugText).SetMarginRight(8);

      _selectionIgniteButton = AddGameButtonTo(selectionToolbar, "Ignite Selected", RequestSelectedIgnition);
      _selectionIgniteButton.tooltip = "Ignite the currently selected Prometheus fire-profiled target.";

      _selectionFeedbackLabel = selectionToolbar.AddGameLabel();

      var selectionScrollView = AddDefaultScrollViewTo(_selectionContainer, 84, 170);

      _selectionDebugLabel = selectionScrollView.AddGameLabel(_selectedEntityDebugText);

      RefreshSelectionPanel();
      return controls;
    }

    internal void SetSelectedEntityDebug(
      int selectedEntityId,
      string title,
      string debugText,
      bool hasFireProfile,
      bool hasExposureController) {
      var previousPreviewTargetId = _selectedPreviewTarget.Id;
      var previousPreviewRawName = _selectedPreviewTarget.RawName;
      _selectedEntityId = selectedEntityId;
      _selectedEntityTitle = string.IsNullOrWhiteSpace(title) ? "Selected entity" : title;
      _selectedEntityDebugText = string.IsNullOrWhiteSpace(debugText) ? "No selected entity details available." : debugText;
      _selectedEntityHasFireProfile = hasFireProfile;
      _selectedEntityHasExposureController = hasExposureController;
      if (TryFindLoadedGameObject(selectedEntityId, out var gameObject)) {
        _selectedPreviewGameObject = gameObject;
        _selectedPreviewTarget = FireVisualPreviewTarget.FromGameObject(gameObject, _loc);
      } else {
        _selectedPreviewGameObject = null;
        _selectedPreviewTarget = new FireVisualPreviewTarget(selectedEntityId, _selectedEntityTitle, _selectedEntityTitle, false);
      }

      RefreshSelectionPanel();
      var targetChanged = previousPreviewTargetId != _selectedPreviewTarget.Id
                          || previousPreviewRawName != _selectedPreviewTarget.RawName;
      if (targetChanged && _activeTab == PrometheusDebugPanelTab.Visuals) {
        if (_livePreviewTargetId != _selectedPreviewTarget.Id) {
          _livePreviewMode = FireVisualLivePreviewMode.None;
          _livePreviewTargetId = 0;
        }

        ApplyActiveTab();
      }
    }

    internal void ClearSelectedEntityDebug() {
      var hadPreviewTarget = _selectedPreviewTarget.Id != 0;
      _selectedEntityId = 0;
      _selectedEntityHasFireProfile = false;
      _selectedEntityHasExposureController = false;
      _selectedEntityTitle = "No selected fire entity";
      _selectedEntityDebugText = "Select a fire-profiled building to inspect Prometheus runtime details.";
      _selectedPreviewGameObject = null;
      _selectedPreviewTarget = FireVisualPreviewTarget.None;
      RefreshSelectionPanel();
      if (hadPreviewTarget && _activeTab == PrometheusDebugPanelTab.Visuals) {
        _livePreviewMode = FireVisualLivePreviewMode.None;
        _livePreviewTargetId = 0;
        ApplyActiveTab();
      }
    }

    private void RefreshSelectionPanel() {
      if (_selectionTitleLabel != null) {
        _selectionTitleLabel.text = _selectedEntityTitle;
      }

      if (_selectionDebugLabel != null) {
        _selectionDebugLabel.text = _selectedEntityDebugText;
      }
    }

    private void CopySelectedEntityDebugText() {
      if (string.IsNullOrWhiteSpace(_selectedEntityDebugText)) {
        SetSelectionFeedback("Nothing to copy.");
        return;
      }

      GUIUtility.systemCopyBuffer = _selectedEntityDebugText;
      SetSelectionFeedback("Copied selection details.");
    }

    private void RequestSelectedIgnition() {
      var rejectionReason = GetSelectedIgnitionRejectionReason();
      if (!string.IsNullOrWhiteSpace(rejectionReason)) {
        SetSelectionFeedback(GetSelectedIgnitionRejectionFeedback(rejectionReason));
        LogSelectedIgnitionRejected(rejectionReason);
        return;
      }

      if (!_fireExposureRuntimeState.RequestForcedIgnition(_selectedEntityId)) {
        const string blockedReason = "ignition_blocked";
        SetSelectionFeedback(GetSelectedIgnitionRejectionFeedback(blockedReason));
        LogSelectedIgnitionRejected(blockedReason);
        return;
      }

      FireTelemetry.Log($"event={FireTelemetryEvents.IgniteSelectedQueued} id={_selectedEntityId} title=\"{FireResetRegistry.EscapeToken(_selectedEntityTitle)}\"");
      SetSelectionFeedback("Ignite Selected queued.");
    }

    private string GetSelectedIgnitionRejectionReason() {
      if (_selectedEntityId == 0) {
        return "none_selected";
      }

      if (!_selectedEntityHasFireProfile) {
        return "missing_fire_profile";
      }

      if (!_selectedEntityHasExposureController) {
        return "missing_exposure_controller";
      }

      return string.Empty;
    }

    private static string GetSelectedIgnitionRejectionFeedback(string reason) {
      return reason switch {
        "none_selected" => "Select a Prometheus fire target first.",
        "missing_fire_profile" => "Selected target has no fire profile.",
        "missing_exposure_controller" => "Selected target cannot be ignited by Prometheus.",
        "ignition_blocked" => "Ignition is temporarily blocked after Stop Fires.",
        _ => "Cannot ignite selected target.",
      };
    }

    private void LogSelectedIgnitionRejected(string reason) {
      FireTelemetry.Log($"event={FireTelemetryEvents.IgniteSelectedRejected} id={_selectedEntityId} reason={reason} hasFireProfile={_selectedEntityHasFireProfile} hasExposureController={_selectedEntityHasExposureController} title=\"{FireResetRegistry.EscapeToken(_selectedEntityTitle)}\"");
    }

    private void SetSelectionFeedback(string message) {
      if (_selectionFeedbackLabel == null) {
        return;
      }

      _selectionFeedbackLabel.text = message;
    }

    private static bool TryFindLoadedGameObject(int entityId, out GameObject loadedGameObject) {
      var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
      for (var i = 0; i < allObjects.Length; i++) {
        var gameObject = allObjects[i];
        if (gameObject == null || gameObject.GetInstanceID() != entityId) {
          continue;
        }

        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) {
          continue;
        }

        loadedGameObject = gameObject;
        return true;
      }

      loadedGameObject = null;
      return false;
    }

  }
}
