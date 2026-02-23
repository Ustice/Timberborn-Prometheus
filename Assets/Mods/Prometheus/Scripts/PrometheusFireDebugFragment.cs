using System;
using System.Text;
using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mods.Prometheus.Scripts {
  internal class PrometheusFireDebugFragment : IEntityPanelFragment {

    private readonly FireTuningRuntimeState _fireTuningRuntimeState;
    private readonly FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private readonly FireSimulationRuntimeState _fireSimulationRuntimeState;
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
    private ScrollView _dataScrollView;
    private Label _dataLabel;
    private int _selectedEntityId;
    private bool _selectedEntityHasFireProfile;
    private bool _selectedEntityHasSimulationController;
    private bool _selectedEntityHasSuppressionApplier;
    private string _latestDebugText = string.Empty;
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
      FireDispatchScoringRuntimeState fireDispatchScoringRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireFestivalRuntimeState fireFestivalRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState) {
      _fireTuningRuntimeState = fireTuningRuntimeState;
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
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

      _dataScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
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
      _dataLabel.style.paddingLeft = 6;
      _dataLabel.style.paddingRight = 6;
      _dataLabel.style.paddingTop = 6;
      _dataLabel.style.paddingBottom = 6;

      _dataScrollView.Add(_dataLabel);
      _detailsContainer.Add(_dataScrollView);

      UpdateDetailsVisibility(_detailsFoldout.value);

      return _root;
    }

    private void UpdateDetailsVisibility(bool showDetails) {
      _detailsContainer.style.display = showDetails ? DisplayStyle.Flex : DisplayStyle.None;
      _detailsFoldout.text = showDetails ? "Hide details" : "Show details";
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
      _copyButton.style.color = PanelTextColor;
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
      UpdateDetailsVisibility(false);
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
      if (!_copyFeedbackActive) {
        _copyStatusLabel.text = string.Empty;
        SetCopyButtonVisuals("Copy", CopyButtonDefaultTint);
      }
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
}