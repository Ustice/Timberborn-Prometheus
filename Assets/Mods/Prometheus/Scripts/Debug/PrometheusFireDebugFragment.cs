using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UiBuilder;
using TimberUi;
using TimberUi.CommonUi;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.Demolishing;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using Timberborn.UILayoutSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mods.Prometheus.Scripts {
  internal enum FireLogFilter {
    All,
    Events,
    Warnings,
    Errors,
  }

  internal enum PrometheusDebugPanelTab {
    Actions,
    Visuals,
    Selection,
    Qa,
    Log,
  }

  internal enum FireVisualLivePreviewMode {
    None,
    Effect,
    Preset,
  }

  internal class PrometheusFireDebugFragment : IEntityPanelFragment {

    private readonly FireTuningRuntimeState _fireTuningRuntimeState;
    private readonly FireExposureRuntimeState _fireExposureRuntimeState;
    private readonly FireImpactRuntimeState _fireImpactRuntimeState;
    private readonly FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private readonly FireRecoveryRuntimeState _fireRecoveryRuntimeState;
    private readonly PrometheusDebugPanel _prometheusDebugPanel;

    private VisualElement _root;
    private int _selectedEntityId;
    private bool _selectedEntityHasFireProfile;
    private bool _selectedEntityHasExposureController;
    private string _selectedEntityDebugTitle = "No selected fire entity";
    private string _latestDebugText = string.Empty;
    private int _baselineExposureSnapshotCount;
    private int _baselineImpactSnapshotCount;
    private int _baselineDamageSnapshotCount;
    private int _baselineRecoverySnapshotCount;
    private int _baselinePendingForcedIgnitionCount;

    public PrometheusFireDebugFragment(
      FireTuningRuntimeState fireTuningRuntimeState,
      FireExposureRuntimeState fireExposureRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState,
      PrometheusDebugPanel prometheusDebugPanel) {
      _fireTuningRuntimeState = fireTuningRuntimeState;
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
      _prometheusDebugPanel = prometheusDebugPanel;
    }

    public VisualElement InitializeFragment() {
      _root = new VisualElement();
      ApplyHiddenSelectionBridgeRootStyle();
      return _root;
    }

    private void ApplyHiddenSelectionBridgeRootStyle() {
      if (_root == null) {
        return;
      }

      _root.visible = false;
      _root.pickingMode = PickingMode.Ignore;
    }

    public void ShowFragment(BaseComponent entity) {
      var fireProfile = entity.GetComponent<FireProfile>();
      _selectedEntityHasFireProfile = fireProfile is not null;
      _selectedEntityHasExposureController = entity.GetComponent<FireExposureController>() is not null;

      _selectedEntityId = entity.GameObject.GetInstanceID();
      _selectedEntityDebugTitle = _selectedEntityHasFireProfile
        ? $"Prometheus Fire Debug — {entity.GameObject.name}"
        : $"Prometheus Fire Debug — {entity.GameObject.name} (no fire profile)";

      CaptureRuntimeCountBaselines();
      ApplyHiddenSelectionBridgeRootStyle();
      UpdateFragment();
    }

    public void ClearFragment() {
      _selectedEntityId = 0;
      _selectedEntityHasFireProfile = false;
      _selectedEntityHasExposureController = false;
      _selectedEntityDebugTitle = "No selected fire entity";
      _latestDebugText = string.Empty;
      _prometheusDebugPanel.ClearSelectedEntityDebug();
      ApplyHiddenSelectionBridgeRootStyle();
    }

    public void UpdateFragment() {
      if (_selectedEntityId == 0) {
        return;
      }

      var stringBuilder = new StringBuilder();

      stringBuilder.AppendLine("Entity");
      stringBuilder.AppendLine($"- FireProfile component: {_selectedEntityHasFireProfile}");
      stringBuilder.AppendLine($"- FireExposureController component: {_selectedEntityHasExposureController}");
      stringBuilder.AppendLine();

      var tuning = _fireTuningRuntimeState.Current;
      stringBuilder.AppendLine("Tuning");
      stringBuilder.AppendLine($"- Profile: {tuning.Profile}");
      stringBuilder.AppendLine($"- Ignition x{tuning.IgnitionMultiplier:0.00}");
      stringBuilder.AppendLine($"- Impact x{tuning.ImpactMultiplier:0.00}");
      stringBuilder.AppendLine($"- Damage ticks x{tuning.DamageTickMultiplier:0.00}");

      stringBuilder.AppendLine();

      stringBuilder.AppendLine("Runtime store counts");
      AppendRuntimeCountLine(stringBuilder, "Exposure snapshots", _fireExposureRuntimeState.SnapshotCount, _baselineExposureSnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Impact snapshots", _fireImpactRuntimeState.SnapshotCount, _baselineImpactSnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Damage snapshots", _fireDamageStateRuntimeState.SnapshotCount, _baselineDamageSnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Recovery snapshots", _fireRecoveryRuntimeState.SnapshotCount, _baselineRecoverySnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Pending forced ignitions", _fireExposureRuntimeState.PendingForcedIgnitionCount, _baselinePendingForcedIgnitionCount);
      stringBuilder.AppendLine();

      if (_fireExposureRuntimeState.TryGetSnapshot(_selectedEntityId, out var exposure)) {
        AppendSnapshotSection(stringBuilder, "Exposure", exposure, static (builder, snapshot) => {
          builder.AppendLine($"- Burning: {snapshot.Burning}");
          builder.AppendLine($"- Intensity: {snapshot.Intensity:0.000}");
          builder.AppendLine($"- Heat exposure: {snapshot.HeatExposure:0.000}");
          builder.AppendLine($"- Ember pressure: {snapshot.EmberPressure:0.000}");
          builder.AppendLine($"- Smoke: {snapshot.Smoke:0.000}");
          builder.AppendLine($"- Ignition progress: {snapshot.IgnitionProgress:0.000}");
          builder.AppendLine($"- Fuel consumed: {snapshot.FuelConsumed:0.000}");
          builder.AppendLine($"- Moisture dampening: {snapshot.MoistureDampening:0.000}");
          builder.AppendLine($"- Oxygen availability: {snapshot.OxygenAvailability:0.000}");
          builder.AppendLine($"- Dominant source: {snapshot.DominantSource}");
        });
      } else {
        AppendWarmupSnapshotUnavailableSection(
          stringBuilder,
          "Exposure",
          _selectedEntityHasExposureController,
          "- Snapshot unavailable (exposure controller not attached)");
      }

      if (_fireImpactRuntimeState.TryGetSnapshot(_selectedEntityId, out var impact)) {
        AppendSnapshotSection(stringBuilder, "Impact", impact, static (builder, snapshot) => {
          builder.AppendLine($"- Crop damage pressure: {snapshot.CropDamagePressure:0.000}");
          builder.AppendLine($"- Tree damage pressure: {snapshot.TreeDamagePressure:0.000}");
          builder.AppendLine($"- Building damage pressure: {snapshot.BuildingDamagePressure:0.000}");
          builder.AppendLine($"- Dehydration pressure: {snapshot.DehydrationPressure:0.000}");
          builder.AppendLine($"- Injury pressure: {snapshot.InjuryPressure:0.000}");
        });
      } else {
        AppendSnapshotUnavailableSection(stringBuilder, "Impact");
      }

      if (_fireDamageStateRuntimeState.TryGetSnapshot(_selectedEntityId, out var damageState)) {
        AppendSnapshotSection(stringBuilder, "Damage state", damageState, static (builder, snapshot) => {
          builder.AppendLine($"- Category: {snapshot.Category}");
          builder.AppendLine($"- State: {snapshot.State}");
          builder.AppendLine($"- Severity: {snapshot.Severity:0.000}");
          builder.AppendLine($"- Tick progress: {snapshot.TickProgress:0.000}");
          builder.AppendLine($"- Damage ticks applied: {snapshot.DamageTicksApplied}");
        });
      } else {
        AppendSnapshotUnavailableSection(stringBuilder, "Damage state");
      }

      if (_fireRecoveryRuntimeState.TryGetSnapshot(_selectedEntityId, out var recovery)) {
        AppendSnapshotSection(stringBuilder, "Recovery", recovery, static (builder, snapshot) => {
          builder.AppendLine($"- Fertile ash available: {snapshot.FertileAshAvailable}");
          builder.AppendLine($"- Fertility boost: {snapshot.FertilityBoost:0.000}");
          builder.AppendLine($"- Growth speed bonus: {snapshot.GrowthSpeedBonus:0.000}");
          builder.AppendLine($"- Yield bonus: {snapshot.YieldBonus:0.000}");
          builder.AppendLine($"- Remaining hours: {snapshot.RemainingHours:0.0}");
        });
      } else {
        AppendSnapshotUnavailableSection(stringBuilder, "Recovery");
      }

      _latestDebugText = stringBuilder.ToString();
      _prometheusDebugPanel.SetSelectedEntityDebug(
        _selectedEntityId,
        _selectedEntityDebugTitle,
        _latestDebugText,
        _selectedEntityHasFireProfile,
        _selectedEntityHasExposureController);
    }

    private void CaptureRuntimeCountBaselines() {
      _baselineExposureSnapshotCount = _fireExposureRuntimeState.SnapshotCount;
      _baselineImpactSnapshotCount = _fireImpactRuntimeState.SnapshotCount;
      _baselineDamageSnapshotCount = _fireDamageStateRuntimeState.SnapshotCount;
      _baselineRecoverySnapshotCount = _fireRecoveryRuntimeState.SnapshotCount;
      _baselinePendingForcedIgnitionCount = _fireExposureRuntimeState.PendingForcedIgnitionCount;
    }

    private static void AppendRuntimeCountLine(StringBuilder stringBuilder, string label, int current, int baseline) {
      var delta = current - baseline;
      var deltaText = delta == 0 ? "±0" : delta > 0 ? $"+{delta}" : delta.ToString();
      stringBuilder.AppendLine($"- {label}: {current} ({deltaText} since selection)");
    }

    internal static bool TryExtractEntityId(string message, out int entityId) {
      entityId = 0;
      if (string.IsNullOrWhiteSpace(message)) {
        return false;
      }

      return TryParseIntValueAfterToken(message, "id=", out entityId)
             || TryParseIntValueAfterToken(message, "sourceId=", out entityId)
             || TryParseIntValueAfterToken(message, "targetId=", out entityId);
    }

    private static bool TryParseIntValueAfterToken(string message, string token, out int value) {
      value = 0;
      var tokenIndex = message.IndexOf(token, StringComparison.OrdinalIgnoreCase);
      if (tokenIndex < 0) {
        return false;
      }

      var start = tokenIndex + token.Length;
      var end = start;
      if (end < message.Length && message[end] == '-') {
        end++;
      }

      var digitStart = end;
      while (end < message.Length && char.IsDigit(message[end])) {
        end++;
      }

      if (end <= digitStart) {
        return false;
      }

      return int.TryParse(message.Substring(start, end - start), out value) && value != 0;
    }

    internal static bool TryFocusCameraOnEntity(int entityId, out string entityName) {
      entityName = string.Empty;
      var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
      for (var i = 0; i < allObjects.Length; i++) {
        var gameObject = allObjects[i];
        if (gameObject == null || gameObject.GetInstanceID() != entityId) {
          continue;
        }

        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) {
          continue;
        }

        var camera = Camera.main;
        if (camera == null) {
          entityName = gameObject.name;
          return false;
        }

        var targetPosition = gameObject.transform.position;
        var currentForward = camera.transform.forward.sqrMagnitude > 0.0001f
          ? camera.transform.forward.normalized
          : Vector3.forward;
        var desiredOffset = (-currentForward * 22f) + (Vector3.up * 8f);
        camera.transform.position = targetPosition + desiredOffset;
        camera.transform.LookAt(targetPosition + (Vector3.up * 1.2f));
        entityName = gameObject.name;
        return true;
      }

      return false;
    }

    private static void AppendSnapshotUnavailableSection(StringBuilder stringBuilder, string sectionTitle) {
      stringBuilder.AppendLine(sectionTitle);
      stringBuilder.AppendLine("- Snapshot unavailable");
      stringBuilder.AppendLine();
    }

    private static void AppendWarmupSnapshotUnavailableSection(
      StringBuilder stringBuilder,
      string sectionTitle,
      bool hasRequiredComponent,
      string missingComponentMessage) {
      stringBuilder.AppendLine(sectionTitle);
      if (!hasRequiredComponent) {
        stringBuilder.AppendLine(missingComponentMessage);
      } else {
        stringBuilder.AppendLine("- Snapshot unavailable (waiting for first runtime tick; unpause for ~1s)");
      }

      stringBuilder.AppendLine();
    }

    private static void AppendSnapshotSection<TSnapshot>(
      StringBuilder stringBuilder,
      string sectionTitle,
      TSnapshot snapshot,
      Action<StringBuilder, TSnapshot> appendSnapshotLines) {
      stringBuilder.AppendLine(sectionTitle);
      appendSnapshotLines(stringBuilder, snapshot);
      stringBuilder.AppendLine();
    }

  }

  internal class PrometheusDebugPanel : ILoadableSingleton {

    private readonly UILayout _uiLayout;
    private readonly VisualElementInitializer _visualElementInitializer;
    private readonly FireGridRuntimeState _fireGridRuntimeState;
    private readonly FireExposureRuntimeState _fireExposureRuntimeState;
    private readonly FireImpactRuntimeState _fireImpactRuntimeState;
    private readonly FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private readonly FireRecoveryRuntimeState _fireRecoveryRuntimeState;
    private readonly FireVisualEffectPreviewRuntimeState _fireVisualEffectPreviewRuntimeState;
    private readonly EntitySelectionService _entitySelectionService;
    private readonly ILoc _loc;
    private readonly PrometheusQaExchange _qaExchange = new();
    private const float DebugStopAllFiresIgnitionBlockSeconds = 60f;

    public event Action<bool> OpenStateChanged;
    public event Action<int> UnreadCountChanged;
    public bool IsOpen => _isOpen;
    public int UnreadCount => _unreadCount;

    private VisualElement _root;
    private ScrollView _logScrollView;
    private VisualElement _logLinesContainer;
    private TextField _searchField;
    private Toggle _autoScrollToggle;
    private Button _allFilterButton;
    private Button _eventsFilterButton;
    private Button _warningsFilterButton;
    private Button _errorsFilterButton;
    private Label _eventsSummaryLabel;
    private Label _warningsSummaryLabel;
    private Label _errorsSummaryLabel;
    private Label _assertsSummaryLabel;
    private Label _exceptionsSummaryLabel;
    private Label _adminFeedbackLabel;
    private VisualElement _contentContainer;
    private VisualElement _selectionContainer;
    private Label _selectionTitleLabel;
    private Label _selectionFeedbackLabel;
    private Label _selectionDebugLabel;
    private Label _qaInstructionLabel;
    private Label _qaStatusLabel;
    private Label _qaPathsLabel;
    private TextField _qaNoteField;
    private Label _visualTuningFeedbackLabel;
    private Button _selectionCopyButton;
    private Button _selectionIgniteButton;
    private readonly FireVisualPreset _visualPreset = new();
    private FireVisualEffectKind _selectedVisualEffect = FireVisualEffectKind.Smoke;
    private bool _advancedVisualControls;
    private bool _showAllNativeSources;
    private string _nativeSourceSearchText = string.Empty;
    private FireVisualPreviewTarget _selectedPreviewTarget = FireVisualPreviewTarget.None;
    private GameObject _selectedPreviewGameObject;
    private FireVisualLivePreviewMode _livePreviewMode = FireVisualLivePreviewMode.None;
    private FireVisualEffectKind _livePreviewEffectKind = FireVisualEffectKind.Smoke;
    private int _livePreviewTargetId;
    private SelectableObject _selectionBeforePanelToolSwitch;
    private int _selectedEntityId;
    private bool _selectedEntityHasFireProfile;
    private bool _selectedEntityHasExposureController;
    private string _selectedEntityTitle = "No selected fire entity";
    private string _selectedEntityDebugText = "Select a fire-profiled building to inspect Prometheus runtime details.";
    private PrometheusQaInstruction _qaInstruction;
    private string _lastRenderedQaSignature = string.Empty;

    private bool _autoScroll = true;
    private FireLogFilter _filter = FireLogFilter.All;
    private string _searchText = string.Empty;
    private string _lastRenderedEntrySignature = string.Empty;
    private int _lastObservedEntryCount;
    private int _unreadCount;
    private PrometheusDebugPanelTab _activeTab = PrometheusDebugPanelTab.Actions;
    private bool _isOpen;

    public PrometheusDebugPanel(
      UILayout uiLayout,
      VisualElementInitializer visualElementInitializer,
      FireGridRuntimeState fireGridRuntimeState,
      FireExposureRuntimeState fireExposureRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState,
      FireVisualEffectPreviewRuntimeState fireVisualEffectPreviewRuntimeState,
      EntitySelectionService entitySelectionService,
      ILoc loc) {
      _uiLayout = uiLayout;
      _visualElementInitializer = visualElementInitializer;
      _fireGridRuntimeState = fireGridRuntimeState;
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
      _fireVisualEffectPreviewRuntimeState = fireVisualEffectPreviewRuntimeState;
      _entitySelectionService = entitySelectionService;
      _loc = loc;
    }

    public void Load() {
      _root = BuildPanelRoot();
      _uiLayout.AddBottomLeft(_root, 5);
      UpdateFilterButtonStyles();

      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      SetUnreadCount(0);
      RefreshLogPanel(force: true);
      RefreshQaPanel(force: true);
      OpenStateChanged?.Invoke(IsOpen);

      _root.schedule.Execute(PollLogStateAndRefresh).Every(500);
      _root.schedule.Execute(PollQaInstructionAndRefresh).Every(1000);
    }

    public void ToggleOpenClose() {
      SetOpen(!IsOpen);
    }

    public void Open(PrometheusDebugPanelTab view) {
      _selectionBeforePanelToolSwitch = _entitySelectionService.SelectedObject;
      SetActiveTab(view);
      SetOpen(true);
    }

    public void RestoreSelectionIfToolSwitchClearedIt() {
      if (_selectionBeforePanelToolSwitch == null || _entitySelectionService.IsAnythingSelected) {
        _selectionBeforePanelToolSwitch = null;
        return;
      }

      _entitySelectionService.SelectSelectable(_selectionBeforePanelToolSwitch);
      _selectionBeforePanelToolSwitch = null;
    }

    public void SetOpen(bool isOpen) {
      if (_root == null) {
        return;
      }

      if (_isOpen == isOpen) {
        if (isOpen) {
          RefreshLogPanel(force: true);
          RefreshQaPanel(force: true);
        }

        return;
      }

      _isOpen = isOpen;
      _root.SetDisplay(isOpen);

      if (isOpen) {
        _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
        SetUnreadCount(0);
        RefreshLogPanel(force: true);
        RefreshQaPanel(force: true);
      }

      OpenStateChanged?.Invoke(isOpen);
    }

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

    private void PollLogStateAndRefresh() {
      var totalCount = FireTelemetry.GetRecentInGameLogEntries().Length;

      if (totalCount < _lastObservedEntryCount) {
        _lastObservedEntryCount = totalCount;
        SetUnreadCount(0);
      } else if (totalCount > _lastObservedEntryCount) {
        var addedCount = totalCount - _lastObservedEntryCount;
        _lastObservedEntryCount = totalCount;
        if (!IsOpen) {
          SetUnreadCount(_unreadCount + addedCount);
        }
      }

      RefreshLogPanel(force: false);
    }

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

    private VisualElement BuildLogPanel() {
      var controls = new VisualElement();

      var filtersSection = CreateSection(controls, "Filters");
      filtersSection.Add(BuildTypeSummaryRow());

      var filterButtonsRow = filtersSection.AddHorizontalContainer();

      _allFilterButton = AddFilterButtonTo(filterButtonsRow, "All", FireLogFilter.All).SetMarginRight(8);
      _eventsFilterButton = AddFilterButtonTo(filterButtonsRow, "Events", FireLogFilter.Events).SetMarginRight(8);
      _warningsFilterButton = AddFilterButtonTo(filterButtonsRow, "Warnings", FireLogFilter.Warnings).SetMarginRight(8);
      _errorsFilterButton = AddFilterButtonTo(filterButtonsRow, "Errors", FireLogFilter.Errors);
      var filterSearchRow = filtersSection.AddHorizontalContainer();

      _searchField = AddDefaultTextFieldTo(filterSearchRow, "Search");
      _searchField.RegisterValueChangedCallback(evt => {
        _searchText = evt.newValue ?? string.Empty;
        RefreshLogPanel(force: true);
      });

      _autoScrollToggle = AddGameToggleTo(filterSearchRow, "Auto", _autoScroll);
      _autoScrollToggle.RegisterValueChangedCallback(evt => _autoScroll = evt.newValue);
      UpdateFilterButtonStyles();

      var logSection = CreateSection(controls, "Log");

      _logScrollView = AddDefaultScrollViewTo(logSection, 170, 300);

      _logLinesContainer = _logScrollView.AddChild();

      return controls;
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

    private VisualElement BuildVisualTuningPanel() {
      var controls = new VisualElement();
      var visualScrollView = AddDefaultScrollViewTo(controls, 260, 430);
      var visualSection = CreateSection(visualScrollView.AddChild(), "Effect Inspector");

      var switcherRow = AddWrappingRowTo(visualSection);
      foreach (var kind in Enum.GetValues(typeof(FireVisualEffectKind)).Cast<FireVisualEffectKind>()) {
        AddEffectSwitcherButtonTo(switcherRow, kind).SetMarginRight(4);
      }

      AddPreviewTargetSummary(visualSection);
      AddPreviewActions(visualSection);

      var globalRow = AddWrappingRowTo(visualSection);
      var advancedToggle = AddGameToggleTo(globalRow, "Advanced", _advancedVisualControls).SetMarginRight(8);
      advancedToggle.tooltip = "Show velocity, gravity, noise, rotation, shape mode, and sorting controls.";
      advancedToggle.RegisterValueChangedCallback(evt => {
        _advancedVisualControls = evt.newValue;
        ApplyActiveTab();
      });

      var allSourcesToggle = AddGameToggleTo(globalRow, "All sources", _showAllNativeSources).SetMarginRight(8);
      allSourcesToggle.tooltip = "Show searchable native ParticleSystem prefabs discovered in loaded resources.";
      allSourcesToggle.RegisterValueChangedCallback(evt => {
        _showAllNativeSources = evt.newValue;
        ApplyActiveTab();
      });

      _visualTuningFeedbackLabel = globalRow.AddGameLabel();
      SetVisualTuningFeedback(CreateLivePreviewStatusText());

      if (_selectedVisualEffect == FireVisualEffectKind.Char) {
        AddCharEffectControls(visualSection);
      } else {
        AddParticleEffectControls(visualSection, _visualPreset.GetParticle(_selectedVisualEffect));
      }

      return controls;
    }

    private static VisualElement AddWrappingRowTo(VisualElement parent) {
      var row = parent.AddChild();
      row.SetAsRow().SetWrap();
      return row;
    }

    private Button AddEffectSwitcherButtonTo(VisualElement parent, FireVisualEffectKind kind) {
      var button = AddGameButtonTo(parent, _selectedVisualEffect == kind ? $"✓ {kind}" : kind.ToString(), () => {
        _selectedVisualEffect = kind;
        if (_livePreviewMode == FireVisualLivePreviewMode.Effect && _livePreviewTargetId == _selectedPreviewTarget.Id) {
          _livePreviewEffectKind = kind;
          RefreshLivePreviewIfArmed();
        }

        ApplyActiveTab();
      });
      button.tooltip = $"Tune {kind}.";
      return button;
    }

    private void AddPreviewTargetSummary(VisualElement parent) {
      var targetRow = AddWrappingRowTo(parent);
      targetRow.AddGameLabel("Target").SetMarginRight(8);
      var target = _selectedPreviewTarget;
      if (target.Id == 0) {
        targetRow.AddGameLabel("No selected entity");
        return;
      }

      targetRow.AddGameLabel(target.Kind).SetMarginRight(8);
      targetRow.AddGameLabel($"id={target.Id}").SetMarginRight(8);
      targetRow.AddGameLabel($"supported={target.Supported}");
      var rawNameRow = AddWrappingRowTo(parent);
      rawNameRow.AddGameLabel($"raw={target.RawName}");
    }

    private void AddPreviewActions(VisualElement parent) {
      var actionsRow = AddWrappingRowTo(parent);

      var applyEffectButton = AddGameButtonTo(actionsRow, "Apply Effect", ApplySelectedVisualEffectPreview).SetMarginRight(8);
      applyEffectButton.tooltip = "Apply only the currently selected effect to the selected entity as a temporary preview.";

      var applyPresetButton = AddGameButtonTo(actionsRow, "Apply Preset", ApplyVisualPresetPreview).SetMarginRight(8);
      applyPresetButton.tooltip = "Apply every particle effect plus Char to the selected entity as a temporary preview.";

      var clearPreviewButton = AddGameButtonTo(actionsRow, "Clear Preview", ClearSelectedVisualPreview).SetMarginRight(8);
      clearPreviewButton.tooltip = "Remove temporary preview particles and material overrides from the selected entity.";

      var jsonRow = AddWrappingRowTo(parent);

      var copyJsonButton = AddGameButtonTo(jsonRow, "Copy JSON", CopyVisualPresetJson).SetMarginRight(8);
      copyJsonButton.tooltip = "Copy the full effect preset and current target context.";

      var logJsonButton = AddGameButtonTo(jsonRow, "Log JSON", LogVisualPresetJson).SetMarginRight(8);
      logJsonButton.tooltip = "Write the full effect preset and current target context to Fire.log.";

      var resetButton = AddGameButtonTo(jsonRow, "Reset", () => {
        _visualPreset.ResetDefaults();
        SetVisualTuningFeedback("Preset reset.");
        RefreshLivePreviewIfArmed();
        ApplyActiveTab();
      });
      resetButton.tooltip = "Reset all effect authoring controls to defaults.";
    }

    private void AddParticleEffectControls(VisualElement parent, FireParticleEffectTuning tuning) {
      var sourceSection = CreateSection(parent, $"{_selectedVisualEffect} Source");
      sourceSection.AddGameLabel($"Current: {tuning.SourceName}");

      var recommendedRow = AddWrappingRowTo(sourceSection);
      foreach (var sourceName in FireNativeParticleSourceCatalog.GetRecommendedSources(tuning.Kind)) {
        var isAvailable = FireNativeParticleSourceCatalog.TryGetSource(sourceName) != null;
        var button = AddGameButtonTo(recommendedRow, sourceName, () => {
          tuning.SourceName = sourceName;
          SetVisualTuningFeedback($"Source {sourceName}");
          RefreshLivePreviewIfArmed();
          ApplyActiveTab();
        }).SetMarginRight(4);
        button.SetEnabled(isAvailable);
      }

      if (_showAllNativeSources) {
        AddNativeSourceSearch(sourceSection, tuning);
      }

      var controlsSection = CreateSection(parent, $"{_selectedVisualEffect} Controls");
      var enabledRow = AddWrappingRowTo(controlsSection);
      var enabledToggle = AddGameToggleTo(enabledRow, "Enabled", tuning.Enabled).SetMarginRight(8);
      enabledToggle.RegisterValueChangedCallback(evt => {
        tuning.Enabled = evt.newValue;
        SetVisualTuningFeedback(evt.newValue ? $"{tuning.Kind} enabled" : $"{tuning.Kind} disabled");
        RefreshLivePreviewIfArmed();
      });

      AddSizeOverLifetimePresetRow(controlsSection, tuning);

      AddVisualSlider(controlsSection, "Intensity", tuning.Intensity, value => tuning.Intensity = value, 0f, 3f, "");
      AddVisualSlider(controlsSection, "Emission", tuning.Emission, value => tuning.Emission = value, 0f, 5f, "");
      AddVector3Sliders(controlsSection, "Position", () => tuning.Position, value => tuning.Position = value, -5f, 5f);
      AddVisualSlider(controlsSection, "Size", tuning.Size, value => tuning.Size = value, 0.1f, 5f, "x");
      AddVisualSlider(controlsSection, "Lifetime", tuning.Lifetime, value => tuning.Lifetime = value, 0.1f, 5f, "x");
      AddVisualSlider(controlsSection, "Speed", tuning.Speed, value => tuning.Speed = value, 0f, 5f, "x");
      AddVisualSlider(controlsSection, "Alpha", tuning.Alpha, value => tuning.Alpha = value, 0f, 1f, "");
      AddColorSliders(controlsSection, "Color", () => tuning.Color, value => tuning.Color = value);
      AddVisualSlider(controlsSection, "Spread", tuning.Spread, value => tuning.Spread = value, 0f, 5f, "");

      if (!_advancedVisualControls) {
        return;
      }

      var advancedSection = CreateSection(parent, "Advanced");
      AddVector3Sliders(advancedSection, "Velocity", () => tuning.Velocity, value => tuning.Velocity = value, -5f, 5f);
      AddVisualSlider(advancedSection, "Gravity", tuning.Gravity, value => tuning.Gravity = value, -2f, 2f, "");
      AddVisualSlider(advancedSection, "Noise", tuning.NoiseStrength, value => tuning.NoiseStrength = value, 0f, 3f, "");
      AddVisualSlider(advancedSection, "Rotation", tuning.RotationSpeed, value => tuning.RotationSpeed = value, -10f, 10f, "");
      AddVisualSlider(advancedSection, "Sorting", tuning.SortingOrder, value => tuning.SortingOrder = Mathf.RoundToInt(value), -20f, 80f, "");
      AddShapeModeRow(advancedSection, tuning);
    }

    private void AddNativeSourceSearch(VisualElement parent, FireParticleEffectTuning tuning) {
      var searchRow = AddWrappingRowTo(parent);
      var sourceSearchField = AddDefaultTextFieldTo(searchRow, "Search");
      sourceSearchField.value = _nativeSourceSearchText;
      sourceSearchField.RegisterValueChangedCallback(evt => {
        _nativeSourceSearchText = evt.newValue ?? string.Empty;
        ApplyActiveTab();
      });

      var needle = _nativeSourceSearchText.Trim();
      var allSources = FireNativeParticleSourceCatalog.GetAllSourceNames()
        .Where(source => string.IsNullOrWhiteSpace(needle) || source.Contains(needle, StringComparison.OrdinalIgnoreCase))
        .Take(18)
        .ToArray();
      var allSourcesRow = AddWrappingRowTo(parent);
      foreach (var sourceName in allSources) {
        AddGameButtonTo(allSourcesRow, sourceName, () => {
          tuning.SourceName = sourceName;
          SetVisualTuningFeedback($"Source {sourceName}");
          RefreshLivePreviewIfArmed();
          ApplyActiveTab();
        }).SetMarginRight(4);
      }
    }

    private void AddSizeOverLifetimePresetRow(VisualElement parent, FireParticleEffectTuning tuning) {
      var row = AddWrappingRowTo(parent);
      row.AddGameLabel("Size over life").SetMarginRight(8);
      foreach (var preset in Enum.GetValues(typeof(FireVisualSizeOverLifetimePreset)).Cast<FireVisualSizeOverLifetimePreset>()) {
        AddGameButtonTo(row, tuning.SizeOverLifetime == preset ? $"✓ {preset}" : preset.ToString(), () => {
          tuning.SizeOverLifetime = preset;
          SetVisualTuningFeedback($"Size over life {preset}");
          RefreshLivePreviewIfArmed();
          ApplyActiveTab();
        }).SetMarginRight(4);
      }
    }

    private void AddShapeModeRow(VisualElement parent, FireParticleEffectTuning tuning) {
      var row = AddWrappingRowTo(parent);
      row.AddGameLabel("Shape").SetMarginRight(8);
      foreach (var shapeMode in Enum.GetValues(typeof(FireVisualShapeMode)).Cast<FireVisualShapeMode>()) {
        AddGameButtonTo(row, tuning.ShapeMode == shapeMode ? $"✓ {shapeMode}" : shapeMode.ToString(), () => {
          tuning.ShapeMode = shapeMode;
          SetVisualTuningFeedback($"Shape {shapeMode}");
          RefreshLivePreviewIfArmed();
          ApplyActiveTab();
        }).SetMarginRight(4);
      }
    }

    private void AddCharEffectControls(VisualElement parent) {
      var tuning = _visualPreset.Char;
      var controlsSection = CreateSection(parent, "Char Controls");
      var enabledRow = AddWrappingRowTo(controlsSection);
      var enabledToggle = AddGameToggleTo(enabledRow, "Enabled", tuning.Enabled).SetMarginRight(8);
      enabledToggle.RegisterValueChangedCallback(evt => {
        tuning.Enabled = evt.newValue;
        SetVisualTuningFeedback(evt.newValue ? "Char enabled" : "Char disabled");
        RefreshLivePreviewIfArmed();
      });
      controlsSection.AddGameLabel("Cutaway preview uses material properties only when supported; custom shader clipping is still gated by shader inspection.");

      AddVisualSlider(controlsSection, "Cut amount", tuning.CutAmount, value => tuning.CutAmount = value, 0f, 1f, "");
      AddVisualSlider(controlsSection, "Noise scale", tuning.NoiseScale, value => tuning.NoiseScale = value, 0f, 8f, "");
      AddVisualSlider(controlsSection, "Noise contrast", tuning.NoiseContrast, value => tuning.NoiseContrast = value, 0.1f, 4f, "");
      AddVisualSlider(controlsSection, "Edge width", tuning.EdgeWidth, value => tuning.EdgeWidth = value, 0f, 1f, "");
      AddVisualSlider(controlsSection, "Edge depth", tuning.EdgeDepth, value => tuning.EdgeDepth = value, 0f, 1f, "");
      AddVisualSlider(controlsSection, "Active glow", tuning.ActiveGlow, value => tuning.ActiveGlow = value, 0f, 2f, "");
      AddVisualSlider(controlsSection, "Ash edge", tuning.AshEdgeBrightness, value => tuning.AshEdgeBrightness = value, 0f, 2f, "");
      AddVisualSlider(controlsSection, "Black interior", tuning.BlackInteriorStrength, value => tuning.BlackInteriorStrength = value, 0f, 2f, "");
      AddVisualSlider(controlsSection, "Seed", tuning.Seed, value => tuning.Seed = value, 0f, 20f, "");
      AddVisualSlider(controlsSection, "Tint strength", tuning.TintStrength, value => tuning.TintStrength = value, 0f, 1f, "");
      AddVisualSlider(controlsSection, "Darkening", tuning.Darkening, value => tuning.Darkening = value, 0f, 1f, "");
      AddColorSliders(controlsSection, "Tint", () => tuning.TintColor, value => tuning.TintColor = value);
    }

    private void AddVector3Sliders(VisualElement parent, string labelText, Func<Vector3> getter, Action<Vector3> setter, float min, float max) {
      var value = getter();
      AddVisualSlider(parent, $"{labelText} X", value.x, newValue => {
        var current = getter();
        setter(new Vector3(newValue, current.y, current.z));
      }, min, max, "");
      AddVisualSlider(parent, $"{labelText} Y", value.y, newValue => {
        var current = getter();
        setter(new Vector3(current.x, newValue, current.z));
      }, min, max, "");
      AddVisualSlider(parent, $"{labelText} Z", value.z, newValue => {
        var current = getter();
        setter(new Vector3(current.x, current.y, newValue));
      }, min, max, "");
    }

    private void AddColorSliders(VisualElement parent, string labelText, Func<Color> getter, Action<Color> setter) {
      var value = getter();
      AddVisualSlider(parent, $"{labelText} R", value.r, newValue => {
        var current = getter();
        setter(new Color(newValue, current.g, current.b, current.a));
      }, 0f, 1f, "");
      AddVisualSlider(parent, $"{labelText} G", value.g, newValue => {
        var current = getter();
        setter(new Color(current.r, newValue, current.b, current.a));
      }, 0f, 1f, "");
      AddVisualSlider(parent, $"{labelText} B", value.b, newValue => {
        var current = getter();
        setter(new Color(current.r, current.g, newValue, current.a));
      }, 0f, 1f, "");
    }

    private void ApplySelectedVisualEffectPreview() {
      var success = _fireVisualEffectPreviewRuntimeState.TryApplyEffect(_selectedPreviewGameObject, _visualPreset, _selectedVisualEffect, out var message);
      if (success) {
        _livePreviewMode = FireVisualLivePreviewMode.Effect;
        _livePreviewEffectKind = _selectedVisualEffect;
        _livePreviewTargetId = _selectedPreviewTarget.Id;
      }

      LogVisualPreviewEvent(FireTelemetryEvents.VisualPreviewApply, success, message, $"effect={_selectedVisualEffect}");
      SetVisualTuningFeedback(message);
    }

    private void ApplyVisualPresetPreview() {
      var success = _fireVisualEffectPreviewRuntimeState.TryApplyPreset(_selectedPreviewGameObject, _visualPreset, out var message);
      if (success) {
        _livePreviewMode = FireVisualLivePreviewMode.Preset;
        _livePreviewTargetId = _selectedPreviewTarget.Id;
      }

      LogVisualPreviewEvent(FireTelemetryEvents.VisualPreviewApply, success, message, "effect=Preset");
      SetVisualTuningFeedback(message);
    }

    private void ClearSelectedVisualPreview() {
      var success = _fireVisualEffectPreviewRuntimeState.ClearPreview(_selectedPreviewGameObject, out var message);
      if (success || _livePreviewTargetId == _selectedPreviewTarget.Id) {
        _livePreviewMode = FireVisualLivePreviewMode.None;
        _livePreviewTargetId = 0;
      }

      LogVisualPreviewEvent(FireTelemetryEvents.VisualPreviewClear, success, message, string.Empty);
      SetVisualTuningFeedback(message);
    }

    private void RefreshLivePreviewIfArmed() {
      if (_livePreviewMode == FireVisualLivePreviewMode.None
          || _selectedPreviewTarget.Id == 0
          || _selectedPreviewTarget.Id != _livePreviewTargetId) {
        return;
      }

      var success = _livePreviewMode == FireVisualLivePreviewMode.Preset
        ? _fireVisualEffectPreviewRuntimeState.TryApplyPreset(_selectedPreviewGameObject, _visualPreset, out var message)
        : _fireVisualEffectPreviewRuntimeState.TryApplyEffect(_selectedPreviewGameObject, _visualPreset, _livePreviewEffectKind, out message);
      if (!success) {
        _livePreviewMode = FireVisualLivePreviewMode.None;
        _livePreviewTargetId = 0;
        SetVisualTuningFeedback(message);
      }
    }

    private string CreateLivePreviewStatusText() {
      if (_livePreviewMode == FireVisualLivePreviewMode.None || _livePreviewTargetId != _selectedPreviewTarget.Id) {
        return string.Empty;
      }

      return _livePreviewMode == FireVisualLivePreviewMode.Preset
        ? "Live preview: preset"
        : $"Live preview: {_livePreviewEffectKind}";
    }

    private void CopyVisualPresetJson() {
      GUIUtility.systemCopyBuffer = CreateVisualPresetJson();
      SetVisualTuningFeedback("Copied JSON.");
    }

    private void LogVisualPresetJson() {
      var json = CreateVisualPresetJson();
      FireTelemetry.Log($"event={FireTelemetryEvents.VisualTuningJson} json={json}");
      SetVisualTuningFeedback("Logged JSON.");
      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      RefreshLogPanel(force: true);
    }

    private string CreateVisualPresetJson() =>
      FireVisualPresetJson.Create(_visualPreset, _selectedVisualEffect, _advancedVisualControls, _selectedPreviewTarget);

    private void LogVisualPreviewEvent(string eventName, bool success, string message, string extra) {
      var target = _selectedPreviewTarget;
      var extraToken = string.IsNullOrWhiteSpace(extra) ? string.Empty : $" {extra}";
      FireTelemetry.Log($"event={eventName} result={(success ? "success" : "unsupported")} targetId={target.Id} kind=\"{target.Kind}\" rawName=\"{target.RawName}\" supported={target.Supported}{extraToken} message=\"{message}\"");
      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      RefreshLogPanel(force: true);
    }

    private void AddVisualSlider(
      VisualElement parent,
      string labelText,
      float initialValue,
      Action<float> setter,
      float min = 0f,
      float max = 3f,
      string valuePrefix = "x") {
      var slider = parent.AddSlider(label: labelText, values: new SliderValues<float>(min, max, initialValue));
      slider.RegisterChange(newValue => {
        var rounded = Mathf.Round(newValue * 20f) / 20f;
        setter(rounded);
        SetVisualTuningFeedback($"{labelText} {valuePrefix}{rounded:0.00}");
        RefreshLivePreviewIfArmed();
      });
    }

    private void SetVisualTuningFeedback(string message) {
      if (_visualTuningFeedbackLabel == null) {
        return;
      }

      _visualTuningFeedbackLabel.text = message;
    }

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

    private void ExtinguishAllFires() {
      _fireExposureRuntimeState.BlockDebugIgnitionsForSeconds(DebugStopAllFiresIgnitionBlockSeconds);
      var liveExtinguishedCount = 0;
      foreach (var exposureController in FindLoadedFireExposureControllers()) {
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

    private static IEnumerable<FireExposureController> FindLoadedFireExposureControllers() {
      var unityComponents = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsSortMode.None);
      for (var i = 0; i < unityComponents.Length; i++) {
        var unityComponent = unityComponents[i];
        if (unityComponent == null || unityComponent.GetType().Name != "ComponentCache") {
          continue;
        }

        if (!TryGetCachedComponents(unityComponent, out var cachedComponents)) {
          continue;
        }

        foreach (var component in cachedComponents) {
          if (component is FireExposureController fireExposureController) {
            yield return fireExposureController;
          }
        }
      }
    }

    private static bool TryGetCachedComponents(Component componentCache, out System.Collections.IEnumerable cachedComponents) {
      var componentCacheType = componentCache.GetType();
      var componentsField = componentCacheType.GetField(
        "_components",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      if (componentsField?.GetValue(componentCache) is System.Collections.IEnumerable components) {
        cachedComponents = components;
        return true;
      }

      var allComponentsProperty = componentCacheType.GetProperty(
        "AllComponents",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (allComponentsProperty?.GetValue(componentCache) is System.Collections.IEnumerable allComponents) {
        cachedComponents = allComponents;
        return true;
      }

      cachedComponents = null;
      return false;
    }

    private void ResetAllFireState() {
      _fireVisualEffectPreviewRuntimeState.ClearAllPreviews();
      _fireGridRuntimeState.Clear();
      var resetEntityCount = 0;
      foreach (var gameObject in FindLoadedFireEntityGameObjects()) {
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

    private static IEnumerable<GameObject> FindLoadedFireEntityGameObjects() {
      var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
      for (var i = 0; i < allObjects.Length; i++) {
        var gameObject = allObjects[i];
        if (gameObject == null || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded) {
          continue;
        }

        if (HasFireResetComponent(gameObject)) {
          yield return gameObject;
        }
      }
    }

    private static bool HasFireResetComponent(GameObject gameObject) {
      if (gameObject.GetComponent<FireExposureController>() is not null
          || gameObject.GetComponent<FireDamageStateController>() is not null
          || gameObject.GetComponent<FireDamageEffectApplier>() is not null
          || gameObject.GetComponent<FireWorkplaceEffectApplier>() is not null
          || gameObject.GetComponent<FireRecoveryController>() is not null
          || gameObject.GetComponent<FireRecoveryEffectApplier>() is not null) {
        return true;
      }

      var componentCache = gameObject.GetComponent<ComponentCache>();
      return componentCache is not null
             && (componentCache.TryGetCachedComponent<FireExposureController>(out _)
                 || componentCache.TryGetCachedComponent<FireDamageStateController>(out _)
                 || componentCache.TryGetCachedComponent<FireDamageEffectApplier>(out _)
                 || componentCache.TryGetCachedComponent<FireVisualEffectApplier>(out _)
                 || componentCache.TryGetCachedComponent<FireWorkplaceEffectApplier>(out _)
                 || componentCache.TryGetCachedComponent<FireRecoveryController>(out _)
                 || componentCache.TryGetCachedComponent<FireRecoveryEffectApplier>(out _));
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

    private static Dictionary<int, GameObject> BuildLoadedSceneGameObjectIndexByEntityId() {
      var loadedObjectsByEntityId = new Dictionary<int, GameObject>();
      var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

      for (var i = 0; i < allObjects.Length; i++) {
        var gameObject = allObjects[i];
        if (gameObject == null) {
          continue;
        }

        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) {
          continue;
        }

        var entityId = gameObject.GetInstanceID();
        if (entityId == 0 || loadedObjectsByEntityId.ContainsKey(entityId)) {
          continue;
        }

        loadedObjectsByEntityId[entityId] = gameObject;
      }

      return loadedObjectsByEntityId;
    }

    private void RemoveEntityFromRuntimeStores(int entityId) {
      _fireExposureRuntimeState.RemoveSnapshot(entityId);
      _fireImpactRuntimeState.RemoveSnapshot(entityId);
      _fireDamageStateRuntimeState.RemoveSnapshot(entityId);
      _fireRecoveryRuntimeState.RemoveSnapshot(entityId);
    }

    private VisualElement BuildTypeSummaryRow() {
      var summaryRow = new VisualElement().SetAsRow().SetWrap();

      _eventsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "EVENT");
      _warningsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "WARN");
      _errorsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "ERROR");
      _assertsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "ASSERT");
      _exceptionsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "EXCEPTION");

      return summaryRow;
    }

    private static Label AddSeveritySummaryLabelTo(VisualElement parent, string typeLabel) => parent.AddGameLabel($"{typeLabel}: 0");

    private void UpdateTypeSummary(ReadOnlySpan<FireInGameLogEntry> entries) {
      var eventsCount = 0;
      var warningsCount = 0;
      var errorsCount = 0;
      var assertsCount = 0;
      var exceptionsCount = 0;

      for (var i = 0; i < entries.Length; i++) {
        var logType = entries[i].LogType;
        switch (logType) {
          case LogType.Warning:
            warningsCount++;
            break;
          case LogType.Error:
            errorsCount++;
            break;
          case LogType.Assert:
            assertsCount++;
            break;
          case LogType.Exception:
            exceptionsCount++;
            break;
          default:
            eventsCount++;
            break;
        }
      }

      if (_eventsSummaryLabel != null) {
        _eventsSummaryLabel.text = $"EVENT: {eventsCount}";
      }

      if (_warningsSummaryLabel != null) {
        _warningsSummaryLabel.text = $"WARN: {warningsCount}";
      }

      if (_errorsSummaryLabel != null) {
        _errorsSummaryLabel.text = $"ERROR: {errorsCount}";
      }

      if (_assertsSummaryLabel != null) {
        _assertsSummaryLabel.text = $"ASSERT: {assertsCount}";
      }

      if (_exceptionsSummaryLabel != null) {
        _exceptionsSummaryLabel.text = $"EXCEPTION: {exceptionsCount}";
      }
    }

    private Button AddFilterButtonTo(VisualElement parent, string text, FireLogFilter filter) {
      var button = AddGameButtonTo(parent, text, () => {
        _filter = filter;
        UpdateFilterButtonStyles();
        RefreshLogPanel(force: true);
      });
      return button;
    }

    private void UpdateFilterButtonStyles() {
      ApplyFilterStyle(_allFilterButton, _filter == FireLogFilter.All);
      ApplyFilterStyle(_eventsFilterButton, _filter == FireLogFilter.Events);
      ApplyFilterStyle(_warningsFilterButton, _filter == FireLogFilter.Warnings);
      ApplyFilterStyle(_errorsFilterButton, _filter == FireLogFilter.Errors);
    }

    private void ApplyFilterStyle(Button button, bool selected) {
      if (button == null) {
        return;
      }

      button.SetEnabled(true);
      button.text = selected ? $"✓ {button.text.TrimStart('✓', ' ')}" : button.text.TrimStart('✓', ' ');
    }

    private void RefreshLogPanel(bool force) {
      if (!IsOpen || _logLinesContainer == null) {
        return;
      }

      var entries = FireTelemetry.GetRecentInGameLogEntries();
      UpdateTypeSummary(entries);
      var entrySignature = CreateEntrySignature(entries);
      if (!force && entrySignature == _lastRenderedEntrySignature) {
        return;
      }

      _lastRenderedEntrySignature = entrySignature;
      _logLinesContainer.Clear();

      var filteredCount = 0;
      for (var i = 0; i < entries.Length; i++) {
        var entry = entries[i];
        if (!ShouldIncludeEntry(entry)) {
          continue;
        }

        filteredCount++;
        _logLinesContainer.Add(CreatePanelLogEntryRow(entry));
      }

      if (filteredCount == 0) {
        AddEmptyLogLabelTo(_logLinesContainer, "No log entries for current filter.");
      }

      if (_autoScroll && _logScrollView != null) {
        _logScrollView.scrollOffset = new Vector2(_logScrollView.scrollOffset.x, float.MaxValue);
      }
    }

    private bool ShouldIncludeEntry(FireInGameLogEntry entry) {
      var includesBySeverity = _filter switch {
        FireLogFilter.All => true,
        FireLogFilter.Events => entry.LogType == LogType.Log,
        FireLogFilter.Warnings => entry.LogType == LogType.Warning,
        FireLogFilter.Errors => entry.LogType == LogType.Error
                                || entry.LogType == LogType.Assert
                                || entry.LogType == LogType.Exception,
        _ => true,
      };

      if (!includesBySeverity) {
        return false;
      }

      if (string.IsNullOrWhiteSpace(_searchText)) {
        return true;
      }

      var needle = _searchText.Trim();
      return entry.Message.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private VisualElement CreatePanelLogEntryRow(FireInGameLogEntry entry) {
      var row = new VisualElement();
      row.SetAsRow();

      var viewButton = AddGameButtonTo(row, "View", () => ViewPanelLogEntry(entry));
      viewButton.tooltip = "View selected log entity.";
      viewButton.SetEnabled(PrometheusFireDebugFragment.TryExtractEntityId(entry.Message, out _));

      var label = AddPanelLogEntryLabelTo(row, entry);

      return row;
    }

    private static Label AddPanelLogEntryLabelTo(VisualElement parent, FireInGameLogEntry entry) {
      var severityToken = entry.LogType switch {
        LogType.Warning => "▲ WARN",
        LogType.Error => "✖ ERROR",
        LogType.Assert => "◆ ASSERT",
        LogType.Exception => "✸ EXCEPTION",
        _ => "● EVENT",
      };

      var label = parent.AddGameLabel($"[{entry.Timestamp}] [{severityToken}] {entry.Message}");

      return label;
    }

    private static Label AddEmptyLogLabelTo(VisualElement parent, string message) {
      var label = parent.AddGameLabel(message);
      return label;
    }

    private void ViewPanelLogEntry(FireInGameLogEntry entry) {
      if (!PrometheusFireDebugFragment.TryExtractEntityId(entry.Message, out var entityId)) {
        SetAdminFeedback("No entity id in this log line.");
        return;
      }

      if (TrySelectAndFocusEntity(entityId, out var entityName)
          || PrometheusFireDebugFragment.TryFocusCameraOnEntity(entityId, out entityName)) {
        SetAdminFeedback($"Viewing {entityName} (id={entityId})");
      } else {
        SetAdminFeedback($"Entity id {entityId} not found");
      }
    }

    private bool TrySelectAndFocusEntity(int entityId, out string entityName) {
      entityName = string.Empty;
      if (!TryFindLoadedGameObject(entityId, out var gameObject)) {
        return false;
      }

      var componentCache = gameObject.GetComponent<ComponentCache>();
      if (componentCache is not null && componentCache.TryGetCachedComponent<FireExposureController>(out var cachedFireExposureController)) {
        _entitySelectionService.SelectAndFocusOn(cachedFireExposureController);
        entityName = gameObject.name;
        FireTelemetry.Log($"event={FireTelemetryEvents.DebugViewFocus} entity={entityName} id={entityId} method=selection_service_cached");
        return true;
      }

      var fireExposureController = gameObject.GetComponent<FireExposureController>();
      if (fireExposureController is not null) {
        _entitySelectionService.SelectAndFocusOn(fireExposureController);
        entityName = gameObject.name;
        FireTelemetry.Log($"event={FireTelemetryEvents.DebugViewFocus} entity={entityName} id={entityId} method=selection_service_component");
        return true;
      }

      return false;
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

    private static string CreateEntrySignature(ReadOnlySpan<FireInGameLogEntry> entries) {
      if (entries.Length == 0) {
        return "0";
      }

      var lastEntry = entries[^1];
      return $"{entries.Length}|{lastEntry.Timestamp}|{lastEntry.LogType}|{lastEntry.Message}";
    }

  }
}
