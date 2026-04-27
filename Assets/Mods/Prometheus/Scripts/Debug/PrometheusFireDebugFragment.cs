using System;
using System.Collections.Generic;
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

  internal partial class PrometheusDebugPanel : ILoadableSingleton {

    private readonly UILayout _uiLayout;
    private readonly VisualElementInitializer _visualElementInitializer;
    private readonly FireGridRuntimeState _fireGridRuntimeState;
    private readonly FireExposureRuntimeState _fireExposureRuntimeState;
    private readonly FireImpactRuntimeState _fireImpactRuntimeState;
    private readonly FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private readonly FireRecoveryRuntimeState _fireRecoveryRuntimeState;
    private readonly FireVisualEffectPreviewRuntimeState _fireVisualEffectPreviewRuntimeState;
    private readonly FireResetRegistry _fireResetRegistry;
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
    private Button _selectionCopyButton;
    private Button _selectionIgniteButton;
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
      FireResetRegistry fireResetRegistry,
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
      _fireResetRegistry = fireResetRegistry;
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

  }
}
