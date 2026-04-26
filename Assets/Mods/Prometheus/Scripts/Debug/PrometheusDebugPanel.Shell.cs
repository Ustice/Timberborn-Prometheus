using System;
using TimberUi;
using TimberUi.CommonUi;
using UnityEngine.UIElements;

namespace Mods.Prometheus.Scripts {
  internal partial class PrometheusDebugPanel {

    private VisualElement BuildPanelRoot() {
      var root = new NineSliceVisualElement()
        .AddClass("square-large--green")
        .SetPadding(10)
        .SetWidth(520)
        .SetMargin(right: 8, bottom: 82, left: 8)
        .SetDisplay(false);
      root.pickingMode = PickingMode.Ignore;
      root.style.flexGrow = 0;
      root.style.flexShrink = 0;
      root.style.alignSelf = Align.FlexStart;
      root.style.maxHeight = 520;

      _contentContainer = root.AddChild();
      _contentContainer.pickingMode = PickingMode.Position;
      _contentContainer.style.flexGrow = 0;
      _contentContainer.style.flexShrink = 0;

      var closeButton = root.AddCloseButton("PrometheusDebugCloseButton");
      closeButton.clicked += () => SetOpen(false);
      closeButton.tooltip = "Close Prometheus debug panel.";

      ApplyActiveTab();
      root.Initialize(_visualElementInitializer);

      return root;
    }

    private VisualElement CreateSection(VisualElement parent, string title) {
      parent.AddGameLabel(title, bold: true);
      return parent;
    }

    private Button AddGameButtonTo(VisualElement parent, string text, Action action, bool destructive = false, bool stretched = false) {
      var button = parent.AddGameButtonPadded(text, action, stretched: stretched, paddingX: 6, paddingY: 3);
      if (destructive) {
        button.AddToClassList(UiCssClasses.Red);
      }
      return button;
    }

    private TextField AddDefaultTextFieldTo(VisualElement parent, string label) {
      var textField = parent.AddTextField();
      textField.value = string.Empty;
      textField.label = label;
      return textField;
    }

    private Toggle AddGameToggleTo(VisualElement parent, string text, bool value) {
      var toggle = parent.AddToggle(text);
      toggle.value = value;
      return toggle;
    }

    private ScrollView AddDefaultScrollViewTo(VisualElement parent, float minHeight, float maxHeight) {
      var scrollView = parent.AddGameScrollView();
      scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
      scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
      scrollView.style.minHeight = minHeight;
      scrollView.style.maxHeight = maxHeight;
      scrollView.style.flexGrow = 0;
      scrollView.style.flexShrink = 1;
      return scrollView;
    }

    private void SetUnreadCount(int unreadCount) {
      var normalized = unreadCount < 0 ? 0 : unreadCount;
      if (_unreadCount == normalized) {
        return;
      }

      _unreadCount = normalized;
      UnreadCountChanged?.Invoke(_unreadCount);
    }

    private void SetActiveTab(PrometheusDebugPanelTab tab) {
      _activeTab = tab;
      ApplyActiveTab();
      if (tab == PrometheusDebugPanelTab.Log) {
        RefreshLogPanel(force: true);
      }
    }

    private void ApplyActiveTab() {
      if (_contentContainer == null) {
        return;
      }

      _logLinesContainer = null;
      _logScrollView = null;
      _selectionContainer = null;
      _qaInstructionLabel = null;
      _qaStatusLabel = null;
      _qaPathsLabel = null;
      _qaNoteField = null;
      _contentContainer.Clear();

      _contentContainer.Add(_activeTab switch {
        PrometheusDebugPanelTab.Visuals => BuildVisualTuningPanel(),
        PrometheusDebugPanelTab.Selection => BuildSelectionPanel(),
        PrometheusDebugPanelTab.Qa => BuildQaPanel(),
        PrometheusDebugPanelTab.Log => BuildLogPanel(),
        _ => BuildCommandsPanel(),
      });

      if (_root != null) {
        _contentContainer.Initialize(_visualElementInitializer);
      }

      if (_activeTab == PrometheusDebugPanelTab.Log) {
        UpdateFilterButtonStyles();
        RefreshLogPanel(force: true);
      }

      if (_activeTab == PrometheusDebugPanelTab.Qa) {
        RefreshQaPanel(force: true);
      }
    }

  }
}
