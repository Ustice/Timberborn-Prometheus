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

      _selectionIgniteButton = AddGameButtonTo(selectionToolbar, "Ignite", RequestSelectedDebugIgnition);

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

    private void RequestSelectedDebugIgnition() {
      if (_selectedEntityId == 0 || !_selectedEntityHasFireProfile || !_selectedEntityHasExposureController) {
        SetSelectionFeedback("Cannot ignite selected entity.");
        return;
      }

      _fireExposureRuntimeState.RequestForcedIgnition(_selectedEntityId);
      SetSelectionFeedback("Ignition request queued.");
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
