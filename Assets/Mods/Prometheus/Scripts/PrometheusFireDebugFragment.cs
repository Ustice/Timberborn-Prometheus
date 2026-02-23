using System;
using System.Text;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
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

  internal class PrometheusFireDebugFragment : IEntityPanelFragment {

    private readonly FireTuningRuntimeState _fireTuningRuntimeState;
    private readonly FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private readonly FireSimulationRuntimeState _fireSimulationRuntimeState;
    private readonly FireEntityRegistryRuntimeState _fireEntityRegistryRuntimeState;
    private readonly FireDispatchScoringRuntimeState _fireDispatchScoringRuntimeState;
    private readonly FireWaterContextRuntimeState _fireWaterContextRuntimeState;
    private readonly FireFestivalRuntimeState _fireFestivalRuntimeState;
    private readonly FireImpactRuntimeState _fireImpactRuntimeState;
    private readonly FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private readonly FireRecoveryRuntimeState _fireRecoveryRuntimeState;

    private VisualElement _root;
    private Label _titleLabel;
    private Button _copyButton;
    private Button _igniteButton;
    private Label _copyStatusLabel;
    private Foldout _detailsFoldout;
    private VisualElement _detailsContainer;
    private Foldout _fireLogFoldout;
    private VisualElement _fireLogContainer;
    private ScrollView _fireLogScrollView;
    private VisualElement _fireLogLinesContainer;
    private Button _clearLogButton;
    private Toggle _autoScrollToggle;
    private Button _allLogFilterButton;
    private Button _eventsLogFilterButton;
    private Button _warningsLogFilterButton;
    private Button _errorsLogFilterButton;
    private TextField _fireLogSearchField;
    private ScrollView _dataScrollView;
    private Label _dataLabel;
    private int _selectedEntityId;
    private bool _selectedEntityHasFireProfile;
    private bool _selectedEntityHasSimulationController;
    private bool _selectedEntityHasSuppressionApplier;
    private string _latestDebugText = string.Empty;
    private bool _autoScrollFireLog = true;
    private FireLogFilter _fireLogFilter = FireLogFilter.All;
    private string _fireLogSearchText = string.Empty;
    private int _baselineSuppressionSnapshotCount;
    private int _baselineSimulationSnapshotCount;
    private int _baselineDispatchSnapshotCount;
    private int _baselineWaterSnapshotCount;
    private int _baselineFestivalSnapshotCount;
    private int _baselineImpactSnapshotCount;
    private int _baselineDamageSnapshotCount;
    private int _baselineRecoverySnapshotCount;
    private int _baselineRegistrySnapshotCount;
    private int _baselinePendingForcedIgnitionCount;
    private int _baselinePendingSpreadIgnitionCount;
    private int _copyFeedbackVersion;
    private bool _copyFeedbackActive;
    private static readonly Color PanelTextColor = new(0.84f, 0.92f, 0.83f, 1f);
    private static readonly Color CopyButtonDefaultTint = new(1f, 1f, 1f, 1f);
    private static readonly Color CopyButtonSuccessTint = new(0.64f, 0.86f, 0.64f, 1f);
    private static readonly Color CopyButtonWarningTint = new(0.93f, 0.72f, 0.38f, 1f);

    public PrometheusFireDebugFragment(
      FireTuningRuntimeState fireTuningRuntimeState,
      FireSuppressionRuntimeState fireSuppressionRuntimeState,
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireEntityRegistryRuntimeState fireEntityRegistryRuntimeState,
      FireDispatchScoringRuntimeState fireDispatchScoringRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireFestivalRuntimeState fireFestivalRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState) {
      _fireTuningRuntimeState = fireTuningRuntimeState;
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireEntityRegistryRuntimeState = fireEntityRegistryRuntimeState;
      _fireDispatchScoringRuntimeState = fireDispatchScoringRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
      _fireFestivalRuntimeState = fireFestivalRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
    }

    public VisualElement InitializeFragment() {
      _root = new VisualElement();
      _root.style.display = DisplayStyle.None;
      _root.style.overflow = Overflow.Visible;
      _root.style.marginTop = 4;
      _root.style.marginBottom = 4;
      _root.style.paddingLeft = 8;
      _root.style.paddingRight = 8;
      _root.style.paddingTop = 6;
      _root.style.paddingBottom = 6;
      _root.style.backgroundColor = new Color(0.12f, 0.24f, 0.18f, 0.95f);
      _root.style.borderTopLeftRadius = 4;
      _root.style.borderTopRightRadius = 4;
      _root.style.borderBottomLeftRadius = 4;
      _root.style.borderBottomRightRadius = 4;
      _root.style.borderTopWidth = 1;
      _root.style.borderRightWidth = 1;
      _root.style.borderBottomWidth = 1;
      _root.style.borderLeftWidth = 1;
      _root.style.borderTopColor = new Color(0.27f, 0.46f, 0.33f, 1f);
      _root.style.borderRightColor = new Color(0.27f, 0.46f, 0.33f, 1f);
      _root.style.borderBottomColor = new Color(0.27f, 0.46f, 0.33f, 1f);
      _root.style.borderLeftColor = new Color(0.27f, 0.46f, 0.33f, 1f);

      _titleLabel = new Label("Prometheus Fire Debug");
      _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _titleLabel.style.color = new Color(0.88f, 0.95f, 0.86f, 1f);
      _titleLabel.style.marginBottom = 4;
      _root.Add(_titleLabel);

      var toolbar = new VisualElement();
      toolbar.style.flexDirection = FlexDirection.Row;
      toolbar.style.alignItems = Align.Center;
      toolbar.style.justifyContent = Justify.FlexEnd;
      toolbar.style.marginBottom = 4;

      _copyStatusLabel = new Label();
      _copyStatusLabel.style.fontSize = 10;
      _copyStatusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _copyStatusLabel.style.marginRight = 6;
      _copyStatusLabel.style.flexGrow = 1;
      _copyStatusLabel.style.color = new Color(0.75f, 0.88f, 0.75f, 1f);
      toolbar.Add(_copyStatusLabel);

      _copyButton = new Button(CopyDebugTextToClipboard) {
        text = "Copy"
      };
      _copyButton.style.height = 20;
      _copyButton.style.fontSize = 11;
      _copyButton.style.unityFontStyleAndWeight = FontStyle.Bold;
      _copyButton.style.unityBackgroundImageTintColor = CopyButtonDefaultTint;
      toolbar.Add(_copyButton);

      _igniteButton = new Button(RequestDebugIgnition) {
        text = "Ignite"
      };
      _igniteButton.style.height = 20;
      _igniteButton.style.fontSize = 11;
      _igniteButton.style.unityFontStyleAndWeight = FontStyle.Bold;
      _igniteButton.style.unityBackgroundImageTintColor = CopyButtonWarningTint;
      _igniteButton.style.marginLeft = 4;
      toolbar.Add(_igniteButton);

      _root.Add(toolbar);

      _detailsFoldout = new Foldout {
        text = "Show details",
        value = false
      };
      _detailsFoldout.style.marginBottom = 4;
      _detailsFoldout.style.color = new Color(0.84f, 0.92f, 0.83f, 1f);
      _detailsFoldout.RegisterValueChangedCallback(evt => UpdateDetailsVisibility(evt.newValue));
      _root.Add(_detailsFoldout);

        var foldoutToggle = _detailsFoldout.Q<Toggle>();
        if (foldoutToggle is not null) {
          foldoutToggle.style.color = PanelTextColor;
          foldoutToggle.style.unityBackgroundImageTintColor = PanelTextColor;
        }

        var foldoutCheckmark = _detailsFoldout.Q<VisualElement>(className: "unity-foldout__checkmark");
        if (foldoutCheckmark is not null) {
          foldoutCheckmark.style.unityBackgroundImageTintColor = PanelTextColor;
        }

      _detailsContainer = new VisualElement();
      _detailsContainer.style.display = DisplayStyle.None;
      _root.Add(_detailsContainer);

      _dataScrollView = new ScrollView(ScrollViewMode.Vertical);
      _dataScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
      _dataScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
      _dataScrollView.style.minHeight = 160;
      _dataScrollView.style.maxHeight = 360;
      _dataScrollView.style.flexShrink = 0;
      _dataScrollView.style.backgroundColor = new Color(0.08f, 0.16f, 0.12f, 0.9f);
      _dataScrollView.style.borderTopLeftRadius = 3;
      _dataScrollView.style.borderTopRightRadius = 3;
      _dataScrollView.style.borderBottomLeftRadius = 3;
      _dataScrollView.style.borderBottomRightRadius = 3;
      _dataScrollView.style.borderTopWidth = 1;
      _dataScrollView.style.borderRightWidth = 1;
      _dataScrollView.style.borderBottomWidth = 1;
      _dataScrollView.style.borderLeftWidth = 1;
      _dataScrollView.style.borderTopColor = new Color(0.22f, 0.38f, 0.29f, 1f);
      _dataScrollView.style.borderRightColor = new Color(0.22f, 0.38f, 0.29f, 1f);
      _dataScrollView.style.borderBottomColor = new Color(0.22f, 0.38f, 0.29f, 1f);
      _dataScrollView.style.borderLeftColor = new Color(0.22f, 0.38f, 0.29f, 1f);

      _dataLabel = new Label();
      _dataLabel.style.whiteSpace = WhiteSpace.PreWrap;
      _dataLabel.style.fontSize = 11;
      _dataLabel.style.color = new Color(0.84f, 0.92f, 0.83f, 1f);
      _dataLabel.style.unityTextAlign = TextAnchor.UpperLeft;
      _dataLabel.style.flexGrow = 1;
      _dataLabel.style.minWidth = 0;
      _dataLabel.style.paddingLeft = 6;
      _dataLabel.style.paddingRight = 6;
      _dataLabel.style.paddingTop = 6;
      _dataLabel.style.paddingBottom = 6;

      _dataScrollView.Add(_dataLabel);
      _detailsContainer.Add(_dataScrollView);

      _fireLogFoldout = new Foldout {
        text = "Show fire log",
        value = false
      };
      _fireLogFoldout.style.marginBottom = 4;
      _fireLogFoldout.style.color = PanelTextColor;
      _fireLogFoldout.RegisterValueChangedCallback(evt => UpdateFireLogVisibility(evt.newValue));
      _root.Add(_fireLogFoldout);

      var fireLogFoldoutToggle = _fireLogFoldout.Q<Toggle>();
      if (fireLogFoldoutToggle is not null) {
        fireLogFoldoutToggle.style.color = PanelTextColor;
        fireLogFoldoutToggle.style.unityBackgroundImageTintColor = PanelTextColor;
      }

      var fireLogFoldoutCheckmark = _fireLogFoldout.Q<VisualElement>(className: "unity-foldout__checkmark");
      if (fireLogFoldoutCheckmark is not null) {
        fireLogFoldoutCheckmark.style.unityBackgroundImageTintColor = PanelTextColor;
      }

      _fireLogContainer = new VisualElement();
      _fireLogContainer.style.display = DisplayStyle.None;
      _root.Add(_fireLogContainer);

      var fireLogToolbar = new VisualElement();
      fireLogToolbar.style.flexDirection = FlexDirection.Row;
      fireLogToolbar.style.alignItems = Align.Center;
      fireLogToolbar.style.justifyContent = Justify.SpaceBetween;
      fireLogToolbar.style.marginBottom = 4;

      var fireLogFilterButtons = new VisualElement();
      fireLogFilterButtons.style.flexDirection = FlexDirection.Row;
      fireLogFilterButtons.style.alignItems = Align.Center;

      _allLogFilterButton = CreateFireLogFilterButton("All", FireLogFilter.All);
      _eventsLogFilterButton = CreateFireLogFilterButton("Events", FireLogFilter.Events);
      _warningsLogFilterButton = CreateFireLogFilterButton("Warnings", FireLogFilter.Warnings);
      _errorsLogFilterButton = CreateFireLogFilterButton("Errors", FireLogFilter.Errors);

      fireLogFilterButtons.Add(_allLogFilterButton);
      fireLogFilterButtons.Add(_eventsLogFilterButton);
      fireLogFilterButtons.Add(_warningsLogFilterButton);
      fireLogFilterButtons.Add(_errorsLogFilterButton);

      _fireLogSearchField = new TextField {
        value = string.Empty
      };
      _fireLogSearchField.label = "Search";
      _fireLogSearchField.style.minWidth = 220;
      _fireLogSearchField.style.marginLeft = 6;
      _fireLogSearchField.style.color = PanelTextColor;
      _fireLogSearchField.RegisterValueChangedCallback(evt => {
        _fireLogSearchText = evt.newValue ?? string.Empty;
        UpdateInGameFireLogPanel();
      });
      fireLogFilterButtons.Add(_fireLogSearchField);

      fireLogToolbar.Add(fireLogFilterButtons);

      _autoScrollToggle = new Toggle("Auto-scroll") {
        value = _autoScrollFireLog
      };
      _autoScrollToggle.style.color = PanelTextColor;
      _autoScrollToggle.style.fontSize = 11;
      _autoScrollToggle.RegisterValueChangedCallback(evt => _autoScrollFireLog = evt.newValue);
      fireLogToolbar.Add(_autoScrollToggle);

      _clearLogButton = new Button(ClearInGameFireLog) {
        text = "Clear log"
      };
      _clearLogButton.style.height = 20;
      _clearLogButton.style.fontSize = 11;
      _clearLogButton.style.unityFontStyleAndWeight = FontStyle.Bold;
      _clearLogButton.style.unityBackgroundImageTintColor = CopyButtonWarningTint;
      _clearLogButton.style.marginLeft = 6;
      fireLogToolbar.Add(_clearLogButton);
      _fireLogContainer.Add(fireLogToolbar);

      _fireLogScrollView = new ScrollView(ScrollViewMode.Vertical);
      _fireLogScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
      _fireLogScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
      _fireLogScrollView.style.minHeight = 140;
      _fireLogScrollView.style.maxHeight = 300;
      _fireLogScrollView.style.flexShrink = 0;
      _fireLogScrollView.style.backgroundColor = new Color(0.08f, 0.13f, 0.10f, 0.9f);
      _fireLogScrollView.style.borderTopLeftRadius = 3;
      _fireLogScrollView.style.borderTopRightRadius = 3;
      _fireLogScrollView.style.borderBottomLeftRadius = 3;
      _fireLogScrollView.style.borderBottomRightRadius = 3;
      _fireLogScrollView.style.borderTopWidth = 1;
      _fireLogScrollView.style.borderRightWidth = 1;
      _fireLogScrollView.style.borderBottomWidth = 1;
      _fireLogScrollView.style.borderLeftWidth = 1;
      _fireLogScrollView.style.borderTopColor = new Color(0.22f, 0.33f, 0.27f, 1f);
      _fireLogScrollView.style.borderRightColor = new Color(0.22f, 0.33f, 0.27f, 1f);
      _fireLogScrollView.style.borderBottomColor = new Color(0.22f, 0.33f, 0.27f, 1f);
      _fireLogScrollView.style.borderLeftColor = new Color(0.22f, 0.33f, 0.27f, 1f);

      _fireLogLinesContainer = new VisualElement();
      _fireLogLinesContainer.style.flexDirection = FlexDirection.Column;
      _fireLogLinesContainer.style.flexGrow = 1;
      _fireLogLinesContainer.style.paddingLeft = 6;
      _fireLogLinesContainer.style.paddingRight = 6;
      _fireLogLinesContainer.style.paddingTop = 6;
      _fireLogLinesContainer.style.paddingBottom = 6;

      _fireLogScrollView.Add(_fireLogLinesContainer);
      _fireLogContainer.Add(_fireLogScrollView);

      UpdateDetailsVisibility(_detailsFoldout.value);
      UpdateFireLogVisibility(_fireLogFoldout.value);

      return _root;
    }

    private void UpdateDetailsVisibility(bool showDetails) {
      _detailsContainer.style.display = showDetails ? DisplayStyle.Flex : DisplayStyle.None;
      _detailsFoldout.text = showDetails ? "Hide details" : "Show details";
    }

    private void UpdateFireLogVisibility(bool showFireLog) {
      _fireLogContainer.style.display = showFireLog ? DisplayStyle.Flex : DisplayStyle.None;
      _fireLogFoldout.text = showFireLog ? "Hide fire log" : "Show fire log";
    }

    private void CopyDebugTextToClipboard() {
      var debugText = _latestDebugText ?? string.Empty;
      if (string.IsNullOrWhiteSpace(debugText)) {
        SetCopyButtonVisuals("Copy", CopyButtonWarningTint);
        SetCopyFeedback("Nothing to copy.", new Color(0.96f, 0.74f, 0.40f, 1f));
        ScheduleCopyButtonReset();
        return;
      }

      GUIUtility.systemCopyBuffer = debugText;

      var textEditor = new TextEditor {
        text = debugText
      };
      textEditor.SelectAll();
      textEditor.Copy();

      var lineCount = debugText.Split('\n').Length;
      SetCopyFeedback($"Copied {lineCount} lines.", new Color(0.72f, 0.93f, 0.72f, 1f));
      SetCopyButtonVisuals("Copied ✓", CopyButtonSuccessTint);
      ScheduleCopyButtonReset();
    }

    private void ScheduleCopyButtonReset() {
      var feedbackVersion = ++_copyFeedbackVersion;
      _root.schedule.Execute(() => {
        if (feedbackVersion != _copyFeedbackVersion) {
          return;
        }

        SetCopyButtonVisuals("Copy", CopyButtonDefaultTint);
      }).StartingIn(1500);
    }

    private void SetCopyButtonVisuals(string text, Color tint) {
      _copyButton.text = text;
      _copyButton.style.unityBackgroundImageTintColor = tint;
    }

    private void SetCopyFeedback(string message, Color color) {
      _copyFeedbackActive = true;
      _copyStatusLabel.text = message;
      _copyStatusLabel.style.color = color;

      var feedbackVersion = ++_copyFeedbackVersion;
      _root.schedule.Execute(() => {
        if (feedbackVersion != _copyFeedbackVersion) {
          return;
        }

        _copyStatusLabel.text = string.Empty;
        _copyFeedbackActive = false;
      }).StartingIn(3000);
    }

    private void RequestDebugIgnition() {
      if (_selectedEntityId == 0 || !_selectedEntityHasFireProfile) {
        SetCopyFeedback("Cannot ignite: selected entity has no fire profile.", new Color(0.96f, 0.74f, 0.40f, 1f));
        return;
      }

      if (!_selectedEntityHasSimulationController) {
        SetCopyFeedback("Cannot ignite: simulation controller not attached.", new Color(0.96f, 0.74f, 0.40f, 1f));
        return;
      }

      _fireSimulationRuntimeState.RequestForcedIgnition(_selectedEntityId);
      SetCopyFeedback("Ignition request queued.", new Color(0.72f, 0.93f, 0.72f, 1f));
    }

    public void ShowFragment(BaseComponent entity) {
      var fireProfile = entity.GetComponent<FireResponseProfile>();
      _selectedEntityHasFireProfile = fireProfile is not null;
      _selectedEntityHasSimulationController = entity.GetComponent<FireSimulationController>() is not null;
      _selectedEntityHasSuppressionApplier = entity.GetComponent<FireSuppressionProfileApplier>() is not null;

      _selectedEntityId = entity.GameObject.GetInstanceID();
      _titleLabel.text = _selectedEntityHasFireProfile
        ? $"Prometheus Fire Debug — {entity.GameObject.name}"
        : $"Prometheus Fire Debug — {entity.GameObject.name} (no fire profile)";

      _igniteButton.SetEnabled(_selectedEntityHasFireProfile);
      _detailsFoldout.value = false;
      _fireLogFoldout.value = false;
      _autoScrollFireLog = true;
      _fireLogFilter = FireLogFilter.All;
      _fireLogSearchText = string.Empty;
      if (_autoScrollToggle != null) {
        _autoScrollToggle.value = true;
      }
      if (_fireLogSearchField != null) {
        _fireLogSearchField.value = string.Empty;
      }
      ApplyFireLogFilterButtonStyles();
      _copyButton.style.color = PanelTextColor;

      CaptureRuntimeCountBaselines();
      _root.style.display = DisplayStyle.Flex;
      UpdateFragment();
    }

    public void ClearFragment() {
      _selectedEntityId = 0;
      _selectedEntityHasFireProfile = false;
      _selectedEntityHasSimulationController = false;
      _selectedEntityHasSuppressionApplier = false;
      _latestDebugText = string.Empty;
      _copyStatusLabel.text = string.Empty;
      _detailsFoldout.style.color = PanelTextColor;
      _copyFeedbackVersion++;
      SetCopyButtonVisuals("Copy", CopyButtonDefaultTint);
      _igniteButton.SetEnabled(false);
      _detailsFoldout.value = false;
      _fireLogFoldout.value = false;
      _fireLogLinesContainer?.Clear();
      UpdateDetailsVisibility(false);
      UpdateFireLogVisibility(false);
      _root.style.display = DisplayStyle.None;
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
      stringBuilder.AppendLine($"- Festival risk x{tuning.FestivalRiskMultiplier:0.00}");
      stringBuilder.AppendLine($"- Ignition/weather x{tuning.WeatherIgnitionMultiplier:0.00}");
      _dataLabel.style.color = PanelTextColor;
      stringBuilder.AppendLine($"- Ignition/fireworks x{tuning.FireworksIgnitionMultiplier:0.00}");
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
      AppendRuntimeCountLine(stringBuilder, "Festival snapshots", _fireFestivalRuntimeState.SnapshotCount, _baselineFestivalSnapshotCount);
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

      if (_fireFestivalRuntimeState.TryGetSnapshot(_selectedEntityId, out var festival)) {
        AppendSnapshotSection(stringBuilder, "Festival", festival, static (builder, snapshot) => {
          builder.AppendLine($"- Active: {snapshot.FestivalActive}");
          builder.AppendLine($"- Risk bonus: {snapshot.FestivalRiskBonus:0.000}");
          builder.AppendLine($"- Safety prep: {snapshot.SafetyPreparation:0.000}");
          builder.AppendLine($"- Hours to start: {snapshot.HoursUntilFestivalStart:0.0}");
          builder.AppendLine($"- Festival hours left: {snapshot.FestivalHoursRemaining:0.0}");
        });
      } else {
        AppendSnapshotUnavailableSection(stringBuilder, "Festival");
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
          builder.AppendLine($"- Ash fertility active: {snapshot.AshenFertilityActive}");
          builder.AppendLine($"- Fertility boost: {snapshot.FertilityBoost:0.000}");
          builder.AppendLine($"- Growth speed bonus: {snapshot.GrowthSpeedBonus:0.000}");
          builder.AppendLine($"- Yield bonus: {snapshot.YieldBonus:0.000}");
          builder.AppendLine($"- Remaining hours: {snapshot.RemainingHours:0.0}");
        });
      } else {
        AppendSnapshotUnavailableSection(stringBuilder, "Recovery");
      }

      _latestDebugText = stringBuilder.ToString();
      _dataLabel.text = _latestDebugText;
      UpdateInGameFireLogPanel();
      if (!_copyFeedbackActive) {
        _copyStatusLabel.text = string.Empty;
        SetCopyButtonVisuals("Copy", CopyButtonDefaultTint);
      }
    }

    private void CaptureRuntimeCountBaselines() {
      _baselineSuppressionSnapshotCount = _fireSuppressionRuntimeState.SnapshotCount;
      _baselineSimulationSnapshotCount = _fireSimulationRuntimeState.SnapshotCount;
      _baselineDispatchSnapshotCount = _fireDispatchScoringRuntimeState.SnapshotCount;
      _baselineWaterSnapshotCount = _fireWaterContextRuntimeState.SnapshotCount;
      _baselineFestivalSnapshotCount = _fireFestivalRuntimeState.SnapshotCount;
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

    private void UpdateInGameFireLogPanel() {
      if (_fireLogLinesContainer == null) {
        return;
      }

      _fireLogLinesContainer.Clear();

      var entries = FireTelemetry.GetRecentInGameLogEntries();
      var filteredCount = 0;
      for (var i = 0; i < entries.Length; i++) {
        var entry = entries[i];
        if (!ShouldIncludeLogEntry(entry)) {
          continue;
        }

        filteredCount++;
        _fireLogLinesContainer.Add(CreateLogEntryRow(entry));
      }

      if (filteredCount == 0) {
        var emptyLabel = new Label("No log entries for current filter.");
        emptyLabel.style.fontSize = 10;
        emptyLabel.style.color = new Color(0.72f, 0.78f, 0.72f, 1f);
        emptyLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        _fireLogLinesContainer.Add(emptyLabel);
      }

      if (_autoScrollFireLog && _fireLogScrollView != null) {
        _fireLogScrollView.scrollOffset = new Vector2(_fireLogScrollView.scrollOffset.x, float.MaxValue);
      }
    }

    private Button CreateFireLogFilterButton(string text, FireLogFilter filter) {
      var button = new Button(() => SetFireLogFilter(filter)) {
        text = text
      };
      button.style.height = 20;
      button.style.fontSize = 10;
      button.style.unityFontStyleAndWeight = FontStyle.Bold;
      button.style.marginRight = 4;
      button.style.color = PanelTextColor;
      return button;
    }

    private void SetFireLogFilter(FireLogFilter filter) {
      _fireLogFilter = filter;
      ApplyFireLogFilterButtonStyles();
      UpdateInGameFireLogPanel();
    }

    private void ApplyFireLogFilterButtonStyles() {
      ApplyFireLogFilterButtonStyle(_allLogFilterButton, _fireLogFilter == FireLogFilter.All);
      ApplyFireLogFilterButtonStyle(_eventsLogFilterButton, _fireLogFilter == FireLogFilter.Events);
      ApplyFireLogFilterButtonStyle(_warningsLogFilterButton, _fireLogFilter == FireLogFilter.Warnings);
      ApplyFireLogFilterButtonStyle(_errorsLogFilterButton, _fireLogFilter == FireLogFilter.Errors);
    }

    private static void ApplyFireLogFilterButtonStyle(Button button, bool selected) {
      if (button == null) {
        return;
      }

      button.style.unityBackgroundImageTintColor = selected
        ? new Color(0.33f, 0.57f, 0.40f, 1f)
        : new Color(1f, 1f, 1f, 1f);
    }

    private bool ShouldIncludeLogEntry(FireInGameLogEntry entry) {
      var includesBySeverity = _fireLogFilter switch {
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

      if (string.IsNullOrWhiteSpace(_fireLogSearchText)) {
        return true;
      }

      var needle = _fireLogSearchText.Trim();
      return entry.Message.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private VisualElement CreateLogEntryRow(FireInGameLogEntry entry) {
      var row = new VisualElement();
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.FlexStart;
      row.style.marginBottom = 2;
      row.style.minWidth = 0;

      var label = CreateLogEntryLabel(entry);
      label.style.flexGrow = 1;
      label.style.minWidth = 0;
      label.style.marginRight = 6;
      row.Add(label);

      var viewButton = new Button(() => ViewEntityFromLogEntry(entry)) {
        text = "View"
      };
      viewButton.style.height = 18;
      viewButton.style.fontSize = 10;
      viewButton.style.unityFontStyleAndWeight = FontStyle.Bold;
      viewButton.style.minWidth = 44;
      viewButton.style.unityBackgroundImageTintColor = new Color(0.55f, 0.70f, 0.92f, 1f);
      viewButton.SetEnabled(TryExtractEntityId(entry.Message, out _));
      row.Add(viewButton);

      return row;
    }

    private static Label CreateLogEntryLabel(FireInGameLogEntry entry) {
      var severityLabel = entry.LogType switch {
        LogType.Warning => "WARN",
        LogType.Error => "ERROR",
        LogType.Assert => "ERROR",
        LogType.Exception => "ERROR",
        _ => "EVENT",
      };

      var label = new Label($"[{entry.Timestamp}] [{severityLabel}] {entry.Message}");
      label.style.whiteSpace = WhiteSpace.PreWrap;
      label.style.fontSize = 10;
      label.style.unityTextAlign = TextAnchor.UpperLeft;
      label.style.marginBottom = 2;
      label.style.color = entry.LogType switch {
        LogType.Warning => new Color(0.98f, 0.78f, 0.40f, 1f),
        LogType.Error => new Color(0.96f, 0.47f, 0.47f, 1f),
        LogType.Assert => new Color(0.96f, 0.47f, 0.47f, 1f),
        LogType.Exception => new Color(0.96f, 0.47f, 0.47f, 1f),
        _ => new Color(0.76f, 0.93f, 0.76f, 1f),
      };

      return label;
    }

    private void ViewEntityFromLogEntry(FireInGameLogEntry entry) {
      if (!TryExtractEntityId(entry.Message, out var entityId)) {
        SetCopyFeedback("No entity id in this log line.", new Color(0.96f, 0.74f, 0.40f, 1f));
        return;
      }

      if (TryFocusCameraOnEntity(entityId, out var entityName)) {
        SetCopyFeedback($"Viewing entity {entityName} (id={entityId}).", new Color(0.72f, 0.93f, 0.72f, 1f));
      } else {
        SetCopyFeedback($"Entity id {entityId} not found in loaded scene.", new Color(0.96f, 0.74f, 0.40f, 1f));
      }
    }

    private static bool TryExtractEntityId(string message, out int entityId) {
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
      while (end < message.Length && char.IsDigit(message[end])) {
        end++;
      }

      if (end <= start) {
        return false;
      }

      return int.TryParse(message.Substring(start, end - start), out value) && value != 0;
    }

    private static bool TryFocusCameraOnEntity(int entityId, out string entityName) {
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

    private void ClearInGameFireLog() {
      FireTelemetry.ClearInGameLog();
      UpdateInGameFireLogPanel();
      SetCopyFeedback("Fire log cleared.", new Color(0.72f, 0.93f, 0.72f, 1f));
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

  internal class PrometheusFireLogPanel : ILoadableSingleton {

    private readonly UILayout _uiLayout;
    private readonly Color _panelTextColor = new(0.84f, 0.92f, 0.83f, 1f);

    public event Action<bool> OpenStateChanged;
    public event Action<int> UnreadCountChanged;
    public bool IsOpen => _panelFoldout is not null && _panelFoldout.value;
    public int UnreadCount => _unreadCount;

    private VisualElement _root;
    private Foldout _panelFoldout;
    private ScrollView _logScrollView;
    private TextField _logTextField;
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

    private bool _autoScroll = true;
    private FireLogFilter _filter = FireLogFilter.All;
    private string _searchText = string.Empty;
    private int _lastRenderedEntryCount = -1;
    private int _lastObservedEntryCount;
    private int _unreadCount;

    public PrometheusFireLogPanel(UILayout uiLayout) {
      _uiLayout = uiLayout;
    }

    public void Load() {
      _root = BuildPanelRoot();
      _uiLayout.AddBottomRight(_root, 5);
      UpdateFilterButtonStyles();

      _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
      SetUnreadCount(0);
      RefreshLogPanel(force: true);
      OpenStateChanged?.Invoke(IsOpen);

      _root.schedule.Execute(PollLogStateAndRefresh).Every(500);
    }

    public void ToggleOpenClose() {
      SetOpen(_panelFoldout is not null && !_panelFoldout.value);
    }

    public void SetOpen(bool isOpen) {
      if (_panelFoldout == null) {
        return;
      }

      if (_panelFoldout.value == isOpen) {
        if (isOpen) {
          RefreshLogPanel(force: true);
        }

        return;
      }

      _panelFoldout.value = isOpen;
    }

    private VisualElement BuildPanelRoot() {
      var root = new VisualElement();
      root.style.width = 460;
      root.style.maxWidth = 560;
      root.style.marginRight = 8;
      root.style.marginBottom = 8;
      root.style.paddingLeft = 8;
      root.style.paddingRight = 8;
      root.style.paddingTop = 6;
      root.style.paddingBottom = 6;
      root.style.backgroundColor = new Color(0.12f, 0.24f, 0.18f, 0.95f);
      root.style.borderTopLeftRadius = 4;
      root.style.borderTopRightRadius = 4;
      root.style.borderBottomLeftRadius = 4;
      root.style.borderBottomRightRadius = 4;
      root.style.borderTopWidth = 1;
      root.style.borderRightWidth = 1;
      root.style.borderBottomWidth = 1;
      root.style.borderLeftWidth = 1;
      root.style.borderTopColor = new Color(0.27f, 0.46f, 0.33f, 1f);
      root.style.borderRightColor = new Color(0.27f, 0.46f, 0.33f, 1f);
      root.style.borderBottomColor = new Color(0.27f, 0.46f, 0.33f, 1f);
      root.style.borderLeftColor = new Color(0.27f, 0.46f, 0.33f, 1f);
      root.style.overflow = Overflow.Visible;

      _panelFoldout = new Foldout {
        text = "Prometheus Fire Log",
        value = false
      };
      _panelFoldout.style.color = _panelTextColor;
      _panelFoldout.RegisterValueChangedCallback(evt => HandlePanelFoldoutChanged(evt.newValue));
      root.Add(_panelFoldout);

      var foldoutToggle = _panelFoldout.Q<Toggle>();
      if (foldoutToggle is not null) {
        foldoutToggle.style.color = _panelTextColor;
        foldoutToggle.style.unityBackgroundImageTintColor = _panelTextColor;
      }

      var foldoutCheckmark = _panelFoldout.Q<VisualElement>(className: "unity-foldout__checkmark");
      if (foldoutCheckmark is not null) {
        foldoutCheckmark.style.unityBackgroundImageTintColor = _panelTextColor;
      }

      _panelFoldout.Add(BuildToolbar());
      _panelFoldout.Add(BuildTypeSummaryRow());

      _logScrollView = new ScrollView(ScrollViewMode.Vertical);
      _logScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
      _logScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
      _logScrollView.style.minHeight = 180;
      _logScrollView.style.maxHeight = 340;
      _logScrollView.style.backgroundColor = new Color(0.08f, 0.13f, 0.10f, 0.9f);
      _logScrollView.style.borderTopLeftRadius = 3;
      _logScrollView.style.borderTopRightRadius = 3;
      _logScrollView.style.borderBottomLeftRadius = 3;
      _logScrollView.style.borderBottomRightRadius = 3;
      _logScrollView.style.borderTopWidth = 1;
      _logScrollView.style.borderRightWidth = 1;
      _logScrollView.style.borderBottomWidth = 1;
      _logScrollView.style.borderLeftWidth = 1;
      _logScrollView.style.borderTopColor = new Color(0.22f, 0.33f, 0.27f, 1f);
      _logScrollView.style.borderRightColor = new Color(0.22f, 0.33f, 0.27f, 1f);
      _logScrollView.style.borderBottomColor = new Color(0.22f, 0.33f, 0.27f, 1f);
      _logScrollView.style.borderLeftColor = new Color(0.22f, 0.33f, 0.27f, 1f);

      _logTextField = new TextField {
        value = string.Empty,
        multiline = true,
        isReadOnly = true
      };
      _logTextField.style.whiteSpace = WhiteSpace.PreWrap;
      _logTextField.style.fontSize = 10;
      _logTextField.style.unityTextAlign = TextAnchor.UpperLeft;
      _logTextField.style.minWidth = 0;
      _logTextField.style.marginBottom = 2;
      _logTextField.style.color = _panelTextColor;
      _logTextField.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
      _logTextField.style.borderTopWidth = 0;
      _logTextField.style.borderRightWidth = 0;
      _logTextField.style.borderBottomWidth = 0;
      _logTextField.style.borderLeftWidth = 0;

      var logInput = _logTextField.Q(className: "unity-text-field__input");
      if (logInput is not null) {
        logInput.style.whiteSpace = WhiteSpace.PreWrap;
        logInput.style.unityTextAlign = TextAnchor.UpperLeft;
        logInput.style.paddingLeft = 0;
        logInput.style.paddingRight = 0;
        logInput.style.paddingTop = 0;
        logInput.style.paddingBottom = 0;
        logInput.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
        logInput.style.borderTopWidth = 0;
        logInput.style.borderRightWidth = 0;
        logInput.style.borderBottomWidth = 0;
        logInput.style.borderLeftWidth = 0;
      }

      _logScrollView.Add(_logTextField);

      _panelFoldout.Add(_logScrollView);
      UpdateFoldoutTitle(_panelFoldout.value);

      return root;
    }

    private void UpdateFoldoutTitle(bool isOpen) {
      if (_panelFoldout == null) {
        return;
      }

      _panelFoldout.text = isOpen ? "Prometheus Fire Log ▾" : "Prometheus Fire Log ▸";
    }

    private void HandlePanelFoldoutChanged(bool isOpen) {
      UpdateFoldoutTitle(isOpen);
      if (isOpen) {
        _lastObservedEntryCount = FireTelemetry.GetRecentInGameLogEntries().Length;
        SetUnreadCount(0);
        RefreshLogPanel(force: true);
      }

      OpenStateChanged?.Invoke(isOpen);
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

    private VisualElement BuildToolbar() {
      var toolbar = new VisualElement();
      toolbar.style.flexDirection = FlexDirection.Row;
      toolbar.style.alignItems = Align.Center;
      toolbar.style.marginBottom = 4;

      _allFilterButton = CreateFilterButton("All", FireLogFilter.All);
      _eventsFilterButton = CreateFilterButton("Events", FireLogFilter.Events);
      _warningsFilterButton = CreateFilterButton("Warnings", FireLogFilter.Warnings);
      _errorsFilterButton = CreateFilterButton("Errors", FireLogFilter.Errors);

      toolbar.Add(_allFilterButton);
      toolbar.Add(_eventsFilterButton);
      toolbar.Add(_warningsFilterButton);
      toolbar.Add(_errorsFilterButton);

      _searchField = new TextField {
        value = string.Empty,
        label = "Search"
      };
      _searchField.style.minWidth = 190;
      _searchField.style.marginLeft = 6;
      _searchField.style.color = _panelTextColor;
      _searchField.RegisterValueChangedCallback(evt => {
        _searchText = evt.newValue ?? string.Empty;
        RefreshLogPanel(force: true);
      });
      toolbar.Add(_searchField);

      _autoScrollToggle = new Toggle("Auto") {
        value = _autoScroll
      };
      _autoScrollToggle.style.marginLeft = 6;
      _autoScrollToggle.style.color = _panelTextColor;
      _autoScrollToggle.style.fontSize = 11;
      _autoScrollToggle.RegisterValueChangedCallback(evt => _autoScroll = evt.newValue);
      toolbar.Add(_autoScrollToggle);

      var clearButton = new Button(() => {
        FireTelemetry.ClearInGameLog();
        _lastObservedEntryCount = 0;
        SetUnreadCount(0);
        RefreshLogPanel(force: true);
      }) {
        text = "Clear Log"
      };
      clearButton.style.height = 20;
      clearButton.style.fontSize = 11;
      clearButton.style.unityFontStyleAndWeight = FontStyle.Bold;
      clearButton.style.unityBackgroundImageTintColor = new Color(0.93f, 0.72f, 0.38f, 1f);
      clearButton.style.marginLeft = 6;
      clearButton.tooltip = "Clear all in-game fire log entries.";
      toolbar.Add(clearButton);

      return toolbar;
    }

    private VisualElement BuildTypeSummaryRow() {
      var summaryRow = new VisualElement();
      summaryRow.style.flexDirection = FlexDirection.Row;
      summaryRow.style.alignItems = Align.Center;
      summaryRow.style.flexWrap = Wrap.Wrap;
      summaryRow.style.marginBottom = 4;

      _eventsSummaryLabel = CreateSeveritySummaryLabel("EVENT", new Color(0.76f, 0.93f, 0.76f, 1f));
      _warningsSummaryLabel = CreateSeveritySummaryLabel("WARN", new Color(0.98f, 0.78f, 0.40f, 1f));
      _errorsSummaryLabel = CreateSeveritySummaryLabel("ERROR", new Color(0.96f, 0.47f, 0.47f, 1f));
      _assertsSummaryLabel = CreateSeveritySummaryLabel("ASSERT", new Color(0.96f, 0.47f, 0.47f, 1f));
      _exceptionsSummaryLabel = CreateSeveritySummaryLabel("EXCEPTION", new Color(0.96f, 0.47f, 0.47f, 1f));

      summaryRow.Add(_eventsSummaryLabel);
      summaryRow.Add(_warningsSummaryLabel);
      summaryRow.Add(_errorsSummaryLabel);
      summaryRow.Add(_assertsSummaryLabel);
      summaryRow.Add(_exceptionsSummaryLabel);

      return summaryRow;
    }

    private static Label CreateSeveritySummaryLabel(string typeLabel, Color textColor) {
      var label = new Label($"{typeLabel}: 0");
      label.style.fontSize = 10;
      label.style.unityFontStyleAndWeight = FontStyle.Bold;
      label.style.color = textColor;
      label.style.marginRight = 6;
      label.style.marginBottom = 2;
      label.style.paddingLeft = 5;
      label.style.paddingRight = 5;
      label.style.paddingTop = 2;
      label.style.paddingBottom = 2;
      label.style.borderTopLeftRadius = 3;
      label.style.borderTopRightRadius = 3;
      label.style.borderBottomLeftRadius = 3;
      label.style.borderBottomRightRadius = 3;
      label.style.backgroundColor = new Color(0.09f, 0.16f, 0.12f, 0.95f);
      return label;
    }

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

    private Button CreateFilterButton(string text, FireLogFilter filter) {
      var button = new Button(() => {
        _filter = filter;
        UpdateFilterButtonStyles();
        RefreshLogPanel(force: true);
      }) {
        text = text
      };

      button.style.height = 20;
      button.style.fontSize = 10;
      button.style.unityFontStyleAndWeight = FontStyle.Bold;
      button.style.marginRight = 4;
      button.style.color = _panelTextColor;
      return button;
    }

    private void UpdateFilterButtonStyles() {
      ApplyFilterStyle(_allFilterButton, _filter == FireLogFilter.All);
      ApplyFilterStyle(_eventsFilterButton, _filter == FireLogFilter.Events);
      ApplyFilterStyle(_warningsFilterButton, _filter == FireLogFilter.Warnings);
      ApplyFilterStyle(_errorsFilterButton, _filter == FireLogFilter.Errors);
    }

    private static void ApplyFilterStyle(Button button, bool selected) {
      if (button == null) {
        return;
      }

      button.style.unityBackgroundImageTintColor = selected
        ? new Color(0.33f, 0.57f, 0.40f, 1f)
        : new Color(1f, 1f, 1f, 1f);
    }

    private void RefreshLogPanel(bool force) {
      if (_panelFoldout == null || !_panelFoldout.value) {
        return;
      }

      var entries = FireTelemetry.GetRecentInGameLogEntries();
      UpdateTypeSummary(entries);
      if (!force && entries.Length == _lastRenderedEntryCount) {
        return;
      }

      _lastRenderedEntryCount = entries.Length;

      var visibleLinesBuilder = new StringBuilder();

      var filteredCount = 0;
      for (var i = 0; i < entries.Length; i++) {
        var entry = entries[i];
        if (!ShouldIncludeEntry(entry)) {
          continue;
        }

        filteredCount++;
        AppendEntryLine(visibleLinesBuilder, entry);
      }

      if (filteredCount == 0) {
        _logTextField.value = "No log entries for current filter.";
      } else {
        _logTextField.value = visibleLinesBuilder.ToString();
      }

      if (_autoScroll) {
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

    private static void AppendEntryLine(StringBuilder stringBuilder, FireInGameLogEntry entry) {
      var severityToken = entry.LogType switch {
        LogType.Warning => "▲ WARN",
        LogType.Error => "✖ ERROR",
        LogType.Assert => "◆ ASSERT",
        LogType.Exception => "✸ EXCEPTION",
        _ => "● EVENT",
      };

      if (stringBuilder.Length > 0) {
        stringBuilder.AppendLine();
      }

      stringBuilder.Append($"[{entry.Timestamp}] [{severityToken}] {entry.Message}");
    }

  }
}