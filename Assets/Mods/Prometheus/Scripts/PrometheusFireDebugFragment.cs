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
    Log,
  }

  internal class PrometheusFireDebugFragment : IEntityPanelFragment {

    private readonly FireTuningRuntimeState _fireTuningRuntimeState;
    private readonly FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private readonly FireSimulationRuntimeState _fireSimulationRuntimeState;
    private readonly FireEntityRegistryRuntimeState _fireEntityRegistryRuntimeState;
    private readonly FireDispatchScoringRuntimeState _fireDispatchScoringRuntimeState;
    private readonly FireWaterContextRuntimeState _fireWaterContextRuntimeState;
    private readonly FireImpactRuntimeState _fireImpactRuntimeState;
    private readonly FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private readonly FireRecoveryRuntimeState _fireRecoveryRuntimeState;
    private readonly PrometheusDebugPanel _prometheusDebugPanel;

    private VisualElement _root;
    private int _selectedEntityId;
    private bool _selectedEntityHasFireProfile;
    private bool _selectedEntityHasSimulationController;
    private bool _selectedEntityHasSuppressionApplier;
    private string _selectedEntityDebugTitle = "No selected fire entity";
    private string _latestDebugText = string.Empty;
    private int _baselineSuppressionSnapshotCount;
    private int _baselineSimulationSnapshotCount;
    private int _baselineDispatchSnapshotCount;
    private int _baselineWaterSnapshotCount;
    private int _baselineImpactSnapshotCount;
    private int _baselineDamageSnapshotCount;
    private int _baselineRecoverySnapshotCount;
    private int _baselineRegistrySnapshotCount;
    private int _baselinePendingForcedIgnitionCount;
    private int _baselinePendingSpreadIgnitionCount;

    public PrometheusFireDebugFragment(
      FireTuningRuntimeState fireTuningRuntimeState,
      FireSuppressionRuntimeState fireSuppressionRuntimeState,
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireEntityRegistryRuntimeState fireEntityRegistryRuntimeState,
      FireDispatchScoringRuntimeState fireDispatchScoringRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState,
      PrometheusDebugPanel prometheusDebugPanel) {
      _fireTuningRuntimeState = fireTuningRuntimeState;
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireEntityRegistryRuntimeState = fireEntityRegistryRuntimeState;
      _fireDispatchScoringRuntimeState = fireDispatchScoringRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
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
      var fireProfile = entity.GetComponent<FireResponseProfile>();
      _selectedEntityHasFireProfile = fireProfile is not null;
      _selectedEntityHasSimulationController = entity.GetComponent<FireSimulationController>() is not null;
      _selectedEntityHasSuppressionApplier = entity.GetComponent<FireSuppressionProfileApplier>() is not null;

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
      _selectedEntityHasSimulationController = false;
      _selectedEntityHasSuppressionApplier = false;
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
      stringBuilder.AppendLine($"- FireResponseProfile component: {_selectedEntityHasFireProfile}");
      stringBuilder.AppendLine($"- FireSimulationController component: {_selectedEntityHasSimulationController}");
      stringBuilder.AppendLine($"- FireSuppressionProfileApplier component: {_selectedEntityHasSuppressionApplier}");
      stringBuilder.AppendLine();

      var tuning = _fireTuningRuntimeState.Current;
      stringBuilder.AppendLine("Tuning");
      stringBuilder.AppendLine($"- Profile: {tuning.Profile}");
      stringBuilder.AppendLine($"- Ignition x{tuning.IgnitionMultiplier:0.00}");
      stringBuilder.AppendLine($"- Spread x{tuning.SpreadMultiplier:0.00}");
      stringBuilder.AppendLine($"- Quenching x{tuning.QuenchingMultiplier:0.00}");
      stringBuilder.AppendLine($"- Impact x{tuning.ImpactMultiplier:0.00}");
      stringBuilder.AppendLine($"- Damage ticks x{tuning.DamageTickMultiplier:0.00}");
      stringBuilder.AppendLine($"- Ignition/weather x{tuning.WeatherIgnitionMultiplier:0.00}");
      stringBuilder.AppendLine($"- Ignition/controlled burn x{tuning.ControlledBurnIgnitionMultiplier:0.00}");
      stringBuilder.AppendLine($"- Ignition/neighbor x{tuning.NeighborIgnitionMultiplier:0.00}");
      stringBuilder.AppendLine($"- Ignition/explosion x{tuning.ExplosionIgnitionMultiplier:0.00} ({tuning.ExplosionIgnitionMode})");
      stringBuilder.AppendLine($"- Spread/dryness x{tuning.DrynessSpreadMultiplier:0.00}");
      stringBuilder.AppendLine($"- Spread/fuel x{tuning.FuelSpreadMultiplier:0.00}");
      stringBuilder.AppendLine($"- Spread/barrier x{tuning.BarrierResistanceMultiplier:0.00}");

      stringBuilder.AppendLine();

      stringBuilder.AppendLine("Runtime store counts");
      AppendRuntimeCountLine(stringBuilder, "Suppression snapshots", _fireSuppressionRuntimeState.SnapshotCount, _baselineSuppressionSnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Simulation snapshots", _fireSimulationRuntimeState.SnapshotCount, _baselineSimulationSnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Dispatch snapshots", _fireDispatchScoringRuntimeState.SnapshotCount, _baselineDispatchSnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Water snapshots", _fireWaterContextRuntimeState.SnapshotCount, _baselineWaterSnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Impact snapshots", _fireImpactRuntimeState.SnapshotCount, _baselineImpactSnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Damage snapshots", _fireDamageStateRuntimeState.SnapshotCount, _baselineDamageSnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Recovery snapshots", _fireRecoveryRuntimeState.SnapshotCount, _baselineRecoverySnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Registry snapshots", _fireEntityRegistryRuntimeState.SnapshotCount, _baselineRegistrySnapshotCount);
      AppendRuntimeCountLine(stringBuilder, "Pending forced ignitions", _fireSimulationRuntimeState.PendingForcedIgnitionCount, _baselinePendingForcedIgnitionCount);
      AppendRuntimeCountLine(stringBuilder, "Pending spread ignitions", _fireSimulationRuntimeState.PendingSpreadIgnitionCount, _baselinePendingSpreadIgnitionCount);
      stringBuilder.AppendLine();

      if (_fireSuppressionRuntimeState.TryGetSnapshot(_selectedEntityId, out var suppression)) {
        AppendSnapshotSection(stringBuilder, "Suppression", suppression, static (builder, snapshot) => {
          builder.AppendLine($"- Approach: {snapshot.FactionApproach}");
          builder.AppendLine($"- Power: {snapshot.SuppressionPower:0.000}");
          builder.AppendLine($"- Heat mitigation: {snapshot.HeatMitigation:0.000}");
          builder.AppendLine($"- Water efficiency: {snapshot.WaterEfficiency:0.000}");
          builder.AppendLine($"- Dispatch lock (s): {snapshot.AssignmentLockDurationInSeconds:0.0}");
          builder.AppendLine($"- Dispatch hysteresis: {snapshot.RetargetHysteresisThreshold:0.000}");
        });
      } else {
        AppendWarmupSnapshotUnavailableSection(
          stringBuilder,
          "Suppression",
          _selectedEntityHasSuppressionApplier,
          "- Snapshot unavailable (suppression applier not attached)");
      }

      if (_fireSimulationRuntimeState.TryGetSnapshot(_selectedEntityId, out var simulation)) {
        AppendSnapshotSection(stringBuilder, "Simulation", simulation, static (builder, snapshot) => {
          builder.AppendLine($"- Burning: {snapshot.Burning}");
          builder.AppendLine($"- Intensity: {snapshot.Intensity:0.000}");
          builder.AppendLine($"- Ignition chance: {snapshot.IgnitionChance:0.000}");
          builder.AppendLine($"- Dominant ignition source: {snapshot.DominantIgnitionSource}");
          builder.AppendLine($"- Ignition/weather: {snapshot.WeatherIgnitionContribution:0.000}");
          builder.AppendLine($"- Ignition/industrial: {snapshot.IndustrialIgnitionContribution:0.000}");
          builder.AppendLine($"- Ignition/fireworks: {snapshot.FireworksIgnitionContribution:0.000}");
          builder.AppendLine($"- Ignition/controlled burn: {snapshot.ControlledBurnIgnitionContribution:0.000}");
          builder.AppendLine($"- Ignition/explosion: {snapshot.ExplosionIgnitionContribution:0.000}");
          builder.AppendLine($"- Heat exposure: {snapshot.HeatExposure:0.000}");
          builder.AppendLine($"- Quenching: {snapshot.QuenchingPower:0.000}");
          builder.AppendLine($"- Spread pressure: {snapshot.SpreadPressure:0.000}");
          builder.AppendLine($"- Neighbor spread pressure: {snapshot.NeighborSpreadPressure:0.000}");
          builder.AppendLine($"- Spread dryness factor: {snapshot.DrynessFactor:0.000}");
          builder.AppendLine($"- Spread fuel factor: {snapshot.FuelFactor:0.000}");
          builder.AppendLine($"- Spread barrier factor: {snapshot.BarrierFactor:0.000}");
        });
      } else {
        AppendWarmupSnapshotUnavailableSection(
          stringBuilder,
          "Simulation",
          _selectedEntityHasSimulationController,
          "- Snapshot unavailable (simulation controller not attached)");
      }

      if (_fireDispatchScoringRuntimeState.TryGetSnapshot(_selectedEntityId, out var dispatchScoring)) {
        AppendSnapshotSection(stringBuilder, "Dispatch scoring", dispatchScoring, static (builder, snapshot) => {
          builder.AppendLine($"- Candidate score: {snapshot.CandidateScore:0.000}");
          builder.AppendLine($"- Assigned score: {snapshot.AssignedScore:0.000}");
          builder.AppendLine($"- Severity factor: {snapshot.SeverityFactor:0.000}");
          builder.AppendLine($"- Asset risk factor: {snapshot.AssetRiskFactor:0.000}");
          builder.AppendLine($"- Travel cost factor: {snapshot.TravelCostFactor:0.000}");
          builder.AppendLine($"- Containment leverage factor: {snapshot.ContainmentLeverageFactor:0.000}");
          builder.AppendLine($"- Assignment locked: {snapshot.AssignmentLocked}");
          builder.AppendLine($"- Lock remaining (s): {snapshot.AssignmentLockRemainingSeconds:0.0}");
          builder.AppendLine($"- Hysteresis threshold: {snapshot.HysteresisThreshold:0.000}");
          builder.AppendLine($"- Retarget suppressed: {snapshot.RetargetSuppressed}");
          builder.AppendLine($"- Response state: {snapshot.ResponseState}");
          builder.AppendLine($"- Top factor: {snapshot.TopFactor}");
        });
      } else {
        AppendSnapshotUnavailableSection(stringBuilder, "Dispatch scoring");
      }

      if (_fireWaterContextRuntimeState.TryGetSnapshot(_selectedEntityId, out var waterContext)) {
        AppendSnapshotSection(stringBuilder, "Water context", waterContext, static (builder, snapshot) => {
          builder.AppendLine($"- Flooded: {snapshot.IsFlooded}");
          builder.AppendLine($"- Water above base: {snapshot.WaterAboveBase:0.000}");
          builder.AppendLine($"- Water needs met: {snapshot.WaterNeedsMet}");
          builder.AppendLine($"- Local exposure: {snapshot.LocalWaterExposure:0.000}");
          builder.AppendLine($"- Quenching bonus: {snapshot.QuenchingBonus:0.000}");
          builder.AppendLine($"- Spread reduction: {snapshot.SpreadReduction:0.000}");
        });
      } else {
        AppendSnapshotUnavailableSection(stringBuilder, "Water context");
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
          builder.AppendLine($"- Controlled burn: {snapshot.ControlledBurn}");
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
        _selectedEntityHasSimulationController);
    }

    private void CaptureRuntimeCountBaselines() {
      _baselineSuppressionSnapshotCount = _fireSuppressionRuntimeState.SnapshotCount;
      _baselineSimulationSnapshotCount = _fireSimulationRuntimeState.SnapshotCount;
      _baselineDispatchSnapshotCount = _fireDispatchScoringRuntimeState.SnapshotCount;
      _baselineWaterSnapshotCount = _fireWaterContextRuntimeState.SnapshotCount;
      _baselineImpactSnapshotCount = _fireImpactRuntimeState.SnapshotCount;
      _baselineDamageSnapshotCount = _fireDamageStateRuntimeState.SnapshotCount;
      _baselineRecoverySnapshotCount = _fireRecoveryRuntimeState.SnapshotCount;
      _baselineRegistrySnapshotCount = _fireEntityRegistryRuntimeState.SnapshotCount;
      _baselinePendingForcedIgnitionCount = _fireSimulationRuntimeState.PendingForcedIgnitionCount;
      _baselinePendingSpreadIgnitionCount = _fireSimulationRuntimeState.PendingSpreadIgnitionCount;
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
    private readonly FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private readonly FireSimulationRuntimeState _fireSimulationRuntimeState;
    private readonly FireDispatchScoringRuntimeState _fireDispatchScoringRuntimeState;
    private readonly FireEntityRegistryRuntimeState _fireEntityRegistryRuntimeState;
    private readonly FireImpactRuntimeState _fireImpactRuntimeState;
    private readonly FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private readonly FireWaterContextRuntimeState _fireWaterContextRuntimeState;
    private readonly FireRecoveryRuntimeState _fireRecoveryRuntimeState;
    private readonly FireVisualEffectRuntimeState _fireVisualEffectRuntimeState;
    private readonly EntitySelectionService _entitySelectionService;
    private const float DebugStopAllFiresIgnitionSuppressionSeconds = 60f;

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
    private Label _visualTuningFeedbackLabel;
    private Button _selectionCopyButton;
    private Button _selectionIgniteButton;
    private int _selectedEntityId;
    private bool _selectedEntityHasFireProfile;
    private bool _selectedEntityHasSimulationController;
    private string _selectedEntityTitle = "No selected fire entity";
    private string _selectedEntityDebugText = "Select a fire-profiled building to inspect Prometheus runtime details.";

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
      FireSuppressionRuntimeState fireSuppressionRuntimeState,
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireDispatchScoringRuntimeState fireDispatchScoringRuntimeState,
      FireEntityRegistryRuntimeState fireEntityRegistryRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState,
      FireVisualEffectRuntimeState fireVisualEffectRuntimeState,
      EntitySelectionService entitySelectionService) {
      _uiLayout = uiLayout;
      _visualElementInitializer = visualElementInitializer;
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireDispatchScoringRuntimeState = fireDispatchScoringRuntimeState;
      _fireEntityRegistryRuntimeState = fireEntityRegistryRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
      _fireVisualEffectRuntimeState = fireVisualEffectRuntimeState;
      _entitySelectionService = entitySelectionService;
    }

    public void Load() {
      _root = BuildPanelRoot();
      _uiLayout.AddBottomLeft(_root, 5);
      UpdateFilterButtonStyles();

      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      SetUnreadCount(0);
      RefreshLogPanel(force: true);
      OpenStateChanged?.Invoke(IsOpen);

      _root.schedule.Execute(PollLogStateAndRefresh).Every(500);
    }

    public void ToggleOpenClose() {
      SetOpen(!IsOpen);
    }

    public void Open(PrometheusDebugPanelTab view) {
      SetActiveTab(view);
      SetOpen(true);
    }

    public void SetOpen(bool isOpen) {
      if (_root == null) {
        return;
      }

      if (_isOpen == isOpen) {
        if (isOpen) {
          RefreshLogPanel(force: true);
        }

        return;
      }

      _isOpen = isOpen;
      _root.SetDisplay(isOpen);

      if (isOpen) {
        _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
        SetUnreadCount(0);
        RefreshLogPanel(force: true);
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

      _contentContainer = root.AddChild();

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
      _contentContainer.Clear();

      _contentContainer.Add(_activeTab switch {
        PrometheusDebugPanelTab.Visuals => BuildVisualTuningPanel(),
        PrometheusDebugPanelTab.Selection => BuildSelectionPanel(),
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
    }

    private VisualElement BuildCommandsPanel() {
      var controls = new VisualElement();

      var commandsSection = CreateSection(controls, "Commands");
      var commandsRow = commandsSection.AddHorizontalContainer();

      var resetFireSimulationButton = AddGameButtonTo(commandsRow, "Reset Fire Sim", ResetAllFireSimulation, destructive: true).SetMarginRight(8);
      resetFireSimulationButton.tooltip = "Reset Prometheus fire simulation, damage, ash/dead state, workplace suppression, and runtime snapshots for all loaded fire entities.";

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

    private VisualElement BuildVisualTuningPanel() {
      var controls = new VisualElement();
      var visualSection = CreateSection(controls, "Visual Tuning");
      var tuning = _fireVisualEffectRuntimeState.CurrentTuning;

      AddVisualSlider(visualSection, "Embers", tuning.EmberScale, _fireVisualEffectRuntimeState.SetEmberScale);
      AddVisualSlider(visualSection, "Smoke", tuning.SmokeScale, _fireVisualEffectRuntimeState.SetSmokeScale);
      AddVisualSlider(visualSection, "Fire", tuning.FireScale, _fireVisualEffectRuntimeState.SetFireScale);
      AddVisualSlider(visualSection, "Steam", tuning.SteamScale, _fireVisualEffectRuntimeState.SetSteamScale);
      AddVisualSlider(visualSection, "Char", tuning.CharScale, _fireVisualEffectRuntimeState.SetCharScale);
      AddVisualSlider(visualSection, "Text marker", _fireVisualEffectRuntimeState.TextMarkerScale, _fireVisualEffectRuntimeState.SetTextMarkerScale);

      var controlRow = visualSection.AddHorizontalContainer();

      var textMarkerToggle = AddGameToggleTo(controlRow, "Text markers", _fireVisualEffectRuntimeState.TextMarkersEnabled).SetMarginRight(8);
      textMarkerToggle.RegisterValueChangedCallback(evt => {
        _fireVisualEffectRuntimeState.SetTextMarkersEnabled(evt.newValue);
        SetVisualTuningFeedback(evt.newValue ? "Text markers on" : "Text markers off");
      });

      var copyButton = AddGameButtonTo(controlRow, "Copy Visuals", CopyVisualTuningSettings).SetMarginRight(8);
      copyButton.tooltip = "Copy current visual tuning values to the clipboard.";

      var resetButton = AddGameButtonTo(controlRow, "Reset Visuals", () => {
        _fireVisualEffectRuntimeState.ResetDefaults();
        SetVisualTuningFeedback("Visuals reset. Reopen panel to refresh sliders.");
      });
      resetButton.tooltip = "Reset visual tuning scales to default and turn text markers off.";

      _visualTuningFeedbackLabel = controlRow.AddGameLabel();
      return controls;
    }

    private void CopyVisualTuningSettings() {
      var tuning = _fireVisualEffectRuntimeState.CurrentTuning;
      var settings = "Prometheus visual tuning: "
                     + $"embers={tuning.EmberScale:0.00}, "
                     + $"smoke={tuning.SmokeScale:0.00}, "
                     + $"fire={tuning.FireScale:0.00}, "
                     + $"steam={tuning.SteamScale:0.00}, "
                     + $"char={tuning.CharScale:0.00}, "
                     + $"textMarkers={_fireVisualEffectRuntimeState.TextMarkersEnabled}, "
                     + $"textMarkerScale={_fireVisualEffectRuntimeState.TextMarkerScale:0.00}";
      GUIUtility.systemCopyBuffer = settings;
      SetVisualTuningFeedback("Copied visual tuning.");
    }

    private void AddVisualSlider(VisualElement parent, string labelText, float initialValue, Action<float> setter) {
      var slider = parent.AddSlider(label: labelText, values: new SliderValues<float>(0, 3, initialValue));
      slider.RegisterChange(newValue => {
        var rounded = Mathf.Round(newValue * 20f) / 20f;
        setter(rounded);
        SetVisualTuningFeedback($"{labelText} x{rounded:0.00}");
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
      bool hasSimulationController) {
      _selectedEntityId = selectedEntityId;
      _selectedEntityTitle = string.IsNullOrWhiteSpace(title) ? "Selected entity" : title;
      _selectedEntityDebugText = string.IsNullOrWhiteSpace(debugText) ? "No selected entity details available." : debugText;
      _selectedEntityHasFireProfile = hasFireProfile;
      _selectedEntityHasSimulationController = hasSimulationController;
      RefreshSelectionPanel();
    }

    internal void ClearSelectedEntityDebug() {
      _selectedEntityId = 0;
      _selectedEntityHasFireProfile = false;
      _selectedEntityHasSimulationController = false;
      _selectedEntityTitle = "No selected fire entity";
      _selectedEntityDebugText = "Select a fire-profiled building to inspect Prometheus runtime details.";
      RefreshSelectionPanel();
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
      if (_selectedEntityId == 0 || !_selectedEntityHasFireProfile || !_selectedEntityHasSimulationController) {
        SetSelectionFeedback("Cannot ignite selected entity.");
        return;
      }

      _fireSimulationRuntimeState.RequestForcedIgnition(_selectedEntityId);
      SetSelectionFeedback("Ignition request queued.");
    }

    private void SetSelectionFeedback(string message) {
      if (_selectionFeedbackLabel == null) {
        return;
      }

      _selectionFeedbackLabel.text = message;
    }

    private void ExtinguishAllFires() {
      _fireSimulationRuntimeState.SuppressDebugIgnitionsForSeconds(DebugStopAllFiresIgnitionSuppressionSeconds);
      var liveExtinguishedCount = 0;
      foreach (var simulationController in FindLoadedFireSimulationControllers()) {
        if (simulationController.DebugForceExtinguish()) {
          liveExtinguishedCount++;
        }
      }

      var simulationExtinguishedCount = _fireSimulationRuntimeState.ExtinguishAllBurning();
      var registryExtinguishedCount = _fireEntityRegistryRuntimeState.ExtinguishAllBurning();

      var effectiveCount = simulationExtinguishedCount > registryExtinguishedCount
        ? simulationExtinguishedCount
        : registryExtinguishedCount;
      effectiveCount = effectiveCount > liveExtinguishedCount
        ? effectiveCount
        : liveExtinguishedCount;

      FireTelemetry.Log($"event={FireTelemetryEvents.DebugStopAllFires} liveExtinguished={liveExtinguishedCount} simulationExtinguished={simulationExtinguishedCount} registryExtinguished={registryExtinguishedCount} ignitionSuppressionSeconds={DebugStopAllFiresIgnitionSuppressionSeconds:0}");
      FireTelemetry.Log(effectiveCount > 0
        ? $"event={FireTelemetryEvents.DebugStopAllFiresResult} result=success count={effectiveCount}"
        : $"event={FireTelemetryEvents.DebugStopAllFiresResult} result=no_active_fires");

      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      RefreshLogPanel(force: true);
    }

    private static IEnumerable<FireSimulationController> FindLoadedFireSimulationControllers() {
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
          if (component is FireSimulationController fireSimulationController) {
            yield return fireSimulationController;
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

    private void ResetAllFireSimulation() {
      var resetEntityCount = 0;
      foreach (var gameObject in FindLoadedFireEntityGameObjects()) {
        ResetLoadedFireEntity(gameObject);
        resetEntityCount++;
      }

      ClearAllRuntimeStores();
      FireBeaverEffectApplier.DebugClearFireNeedEffects();

      FireTelemetry.Log($"event={FireTelemetryEvents.DebugResetFireSimulation} result=success loadedEntities={resetEntityCount}");
      SetAdminFeedback($"Reset fire sim for {resetEntityCount} entities");
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
      if (gameObject.GetComponent<FireSimulationController>() is not null
          || gameObject.GetComponent<FireDamageStateController>() is not null
          || gameObject.GetComponent<FireDamageEffectApplier>() is not null
          || gameObject.GetComponent<FireWorkplaceEffectApplier>() is not null
          || gameObject.GetComponent<FireRecoveryController>() is not null
          || gameObject.GetComponent<FireRecoveryEffectApplier>() is not null) {
        return true;
      }

      var componentCache = gameObject.GetComponent<ComponentCache>();
      return componentCache is not null
             && (componentCache.TryGetCachedComponent<FireSimulationController>(out _)
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
        if (componentCache.TryGetCachedComponent<FireSimulationController>(out var cachedFireSimulationController)) {
          cachedFireSimulationController.DebugResetFireSimulationState();
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

      var fireSimulationController = gameObject.GetComponent<FireSimulationController>();
      if (fireSimulationController is not null) {
        fireSimulationController.DebugResetFireSimulationState();
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
      _fireSuppressionRuntimeState.ClearSnapshots();
      _fireSimulationRuntimeState.ClearSnapshotsAndIgnitionRequests();
      _fireDispatchScoringRuntimeState.ClearSnapshots();
      _fireEntityRegistryRuntimeState.ClearSnapshots();
      _fireImpactRuntimeState.ClearSnapshots();
      _fireDamageStateRuntimeState.ClearSnapshots();
      _fireWaterContextRuntimeState.ClearSnapshots();
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
      _fireSuppressionRuntimeState.RemoveSnapshot(entityId);
      _fireSimulationRuntimeState.RemoveSnapshot(entityId);
      _fireDispatchScoringRuntimeState.RemoveSnapshot(entityId);
      _fireEntityRegistryRuntimeState.RemoveSnapshot(entityId);
      _fireImpactRuntimeState.RemoveSnapshot(entityId);
      _fireDamageStateRuntimeState.RemoveSnapshot(entityId);
      _fireWaterContextRuntimeState.RemoveSnapshot(entityId);
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
      if (componentCache is not null && componentCache.TryGetCachedComponent<FireSimulationController>(out var cachedFireSimulationController)) {
        _entitySelectionService.SelectAndFocusOn(cachedFireSimulationController);
        entityName = gameObject.name;
        FireTelemetry.Log($"event={FireTelemetryEvents.DebugViewFocus} entity={entityName} id={entityId} method=selection_service_cached");
        return true;
      }

      var fireSimulationController = gameObject.GetComponent<FireSimulationController>();
      if (fireSimulationController is not null) {
        _entitySelectionService.SelectAndFocusOn(fireSimulationController);
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
