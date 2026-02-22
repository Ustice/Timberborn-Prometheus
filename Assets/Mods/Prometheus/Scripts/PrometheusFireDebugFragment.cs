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
      stringBuilder.AppendLine($"- Spread/dryness x{tuning.DrynessSpreadMultiplier:0.00}");
      stringBuilder.AppendLine($"- Spread/fuel x{tuning.FuelSpreadMultiplier:0.00}");
      stringBuilder.AppendLine($"- Spread/barrier x{tuning.BarrierResistanceMultiplier:0.00}");

      stringBuilder.AppendLine();

      if (_fireSuppressionRuntimeState.TryGetSnapshot(_selectedEntityId, out var suppression)) {
        stringBuilder.AppendLine("Suppression");
        stringBuilder.AppendLine($"- Approach: {suppression.FactionApproach}");
        stringBuilder.AppendLine($"- Power: {suppression.SuppressionPower:0.000}");
        stringBuilder.AppendLine($"- Heat mitigation: {suppression.HeatMitigation:0.000}");
        stringBuilder.AppendLine($"- Water efficiency: {suppression.WaterEfficiency:0.000}");
        stringBuilder.AppendLine($"- Dispatch lock (s): {suppression.AssignmentLockDurationInSeconds:0.0}");
        stringBuilder.AppendLine($"- Dispatch hysteresis: {suppression.RetargetHysteresisThreshold:0.000}");
      } else {
        stringBuilder.AppendLine("Suppression");
        if (!_selectedEntityHasSuppressionApplier) {
          stringBuilder.AppendLine("- Snapshot unavailable (suppression applier not attached)");
        } else {
          stringBuilder.AppendLine("- Snapshot unavailable (waiting for first runtime tick; unpause for ~1s)");
        }
      }

      stringBuilder.AppendLine();

      if (_fireSimulationRuntimeState.TryGetSnapshot(_selectedEntityId, out var simulation)) {
        stringBuilder.AppendLine("Simulation");
        stringBuilder.AppendLine($"- Burning: {simulation.Burning}");
        stringBuilder.AppendLine($"- Intensity: {simulation.Intensity:0.000}");
        stringBuilder.AppendLine($"- Ignition chance: {simulation.IgnitionChance:0.000}");
        stringBuilder.AppendLine($"- Dominant ignition source: {simulation.DominantIgnitionSource}");
        stringBuilder.AppendLine($"- Ignition/weather: {simulation.WeatherIgnitionContribution:0.000}");
        stringBuilder.AppendLine($"- Ignition/industrial: {simulation.IndustrialIgnitionContribution:0.000}");
        stringBuilder.AppendLine($"- Ignition/fireworks: {simulation.FireworksIgnitionContribution:0.000}");
        stringBuilder.AppendLine($"- Ignition/controlled burn: {simulation.ControlledBurnIgnitionContribution:0.000}");
        stringBuilder.AppendLine($"- Heat exposure: {simulation.HeatExposure:0.000}");
        stringBuilder.AppendLine($"- Quenching: {simulation.QuenchingPower:0.000}");
        stringBuilder.AppendLine($"- Spread pressure: {simulation.SpreadPressure:0.000}");
        stringBuilder.AppendLine($"- Neighbor spread pressure: {simulation.NeighborSpreadPressure:0.000}");
        stringBuilder.AppendLine($"- Spread dryness factor: {simulation.DrynessFactor:0.000}");
        stringBuilder.AppendLine($"- Spread fuel factor: {simulation.FuelFactor:0.000}");
        stringBuilder.AppendLine($"- Spread barrier factor: {simulation.BarrierFactor:0.000}");
      } else {
        stringBuilder.AppendLine("Simulation");
        if (!_selectedEntityHasSimulationController) {
          stringBuilder.AppendLine("- Snapshot unavailable (simulation controller not attached)");
        } else {
          stringBuilder.AppendLine("- Snapshot unavailable (waiting for first runtime tick; unpause for ~1s)");
        }
      }

      stringBuilder.AppendLine();

      if (_fireDispatchScoringRuntimeState.TryGetSnapshot(_selectedEntityId, out var dispatchScoring)) {
        stringBuilder.AppendLine("Dispatch scoring");
        stringBuilder.AppendLine($"- Candidate score: {dispatchScoring.CandidateScore:0.000}");
        stringBuilder.AppendLine($"- Assigned score: {dispatchScoring.AssignedScore:0.000}");
        stringBuilder.AppendLine($"- Severity factor: {dispatchScoring.SeverityFactor:0.000}");
        stringBuilder.AppendLine($"- Asset risk factor: {dispatchScoring.AssetRiskFactor:0.000}");
        stringBuilder.AppendLine($"- Travel cost factor: {dispatchScoring.TravelCostFactor:0.000}");
        stringBuilder.AppendLine($"- Containment leverage factor: {dispatchScoring.ContainmentLeverageFactor:0.000}");
        stringBuilder.AppendLine($"- Assignment locked: {dispatchScoring.AssignmentLocked}");
        stringBuilder.AppendLine($"- Lock remaining (s): {dispatchScoring.AssignmentLockRemainingSeconds:0.0}");
        stringBuilder.AppendLine($"- Hysteresis threshold: {dispatchScoring.HysteresisThreshold:0.000}");
        stringBuilder.AppendLine($"- Retarget suppressed: {dispatchScoring.RetargetSuppressed}");
        stringBuilder.AppendLine($"- Response state: {dispatchScoring.ResponseState}");
        stringBuilder.AppendLine($"- Top factor: {dispatchScoring.TopFactor}");
      } else {
        stringBuilder.AppendLine("Dispatch scoring");
        stringBuilder.AppendLine("- Snapshot unavailable");
      }

      stringBuilder.AppendLine();

      if (_fireWaterContextRuntimeState.TryGetSnapshot(_selectedEntityId, out var waterContext)) {
        stringBuilder.AppendLine("Water context");
        stringBuilder.AppendLine($"- Flooded: {waterContext.IsFlooded}");
        stringBuilder.AppendLine($"- Water above base: {waterContext.WaterAboveBase:0.000}");
        stringBuilder.AppendLine($"- Water needs met: {waterContext.WaterNeedsMet}");
        stringBuilder.AppendLine($"- Local exposure: {waterContext.LocalWaterExposure:0.000}");
        stringBuilder.AppendLine($"- Quenching bonus: {waterContext.QuenchingBonus:0.000}");
        stringBuilder.AppendLine($"- Spread reduction: {waterContext.SpreadReduction:0.000}");
      } else {
        stringBuilder.AppendLine("Water context");
        stringBuilder.AppendLine("- Snapshot unavailable");
      }

      stringBuilder.AppendLine();

      if (_fireFestivalRuntimeState.TryGetSnapshot(_selectedEntityId, out var festival)) {
        stringBuilder.AppendLine("Festival");
        stringBuilder.AppendLine($"- Active: {festival.FestivalActive}");
        stringBuilder.AppendLine($"- Risk bonus: {festival.FestivalRiskBonus:0.000}");
        stringBuilder.AppendLine($"- Safety prep: {festival.SafetyPreparation:0.000}");
        stringBuilder.AppendLine($"- Hours to start: {festival.HoursUntilFestivalStart:0.0}");
        stringBuilder.AppendLine($"- Festival hours left: {festival.FestivalHoursRemaining:0.0}");
      } else {
        stringBuilder.AppendLine("Festival");
        stringBuilder.AppendLine("- Snapshot unavailable");
      }

      stringBuilder.AppendLine();

      if (_fireImpactRuntimeState.TryGetSnapshot(_selectedEntityId, out var impact)) {
        stringBuilder.AppendLine("Impact");
        stringBuilder.AppendLine($"- Crop damage pressure: {impact.CropDamagePressure:0.000}");
        stringBuilder.AppendLine($"- Tree damage pressure: {impact.TreeDamagePressure:0.000}");
        stringBuilder.AppendLine($"- Building damage pressure: {impact.BuildingDamagePressure:0.000}");
        stringBuilder.AppendLine($"- Dehydration pressure: {impact.DehydrationPressure:0.000}");
        stringBuilder.AppendLine($"- Injury pressure: {impact.InjuryPressure:0.000}");
      } else {
        stringBuilder.AppendLine("Impact");
        stringBuilder.AppendLine("- Snapshot unavailable");
      }

      stringBuilder.AppendLine();

      if (_fireDamageStateRuntimeState.TryGetSnapshot(_selectedEntityId, out var damageState)) {
        stringBuilder.AppendLine("Damage state");
        stringBuilder.AppendLine($"- Category: {damageState.Category}");
        stringBuilder.AppendLine($"- State: {damageState.State}");
        stringBuilder.AppendLine($"- Severity: {damageState.Severity:0.000}");
        stringBuilder.AppendLine($"- Tick progress: {damageState.TickProgress:0.000}");
        stringBuilder.AppendLine($"- Damage ticks applied: {damageState.DamageTicksApplied}");
      } else {
        stringBuilder.AppendLine("Damage state");
        stringBuilder.AppendLine("- Snapshot unavailable");
      }

      stringBuilder.AppendLine();

      if (_fireRecoveryRuntimeState.TryGetSnapshot(_selectedEntityId, out var recovery)) {
        stringBuilder.AppendLine("Recovery");
        stringBuilder.AppendLine($"- Controlled burn: {recovery.ControlledBurn}");
        stringBuilder.AppendLine($"- Ash fertility active: {recovery.AshenFertilityActive}");
        stringBuilder.AppendLine($"- Fertility boost: {recovery.FertilityBoost:0.000}");
        stringBuilder.AppendLine($"- Growth speed bonus: {recovery.GrowthSpeedBonus:0.000}");
        stringBuilder.AppendLine($"- Yield bonus: {recovery.YieldBonus:0.000}");
        stringBuilder.AppendLine($"- Remaining hours: {recovery.RemainingHours:0.0}");
      } else {
        stringBuilder.AppendLine("Recovery");
        stringBuilder.AppendLine("- Snapshot unavailable");
      }

      _latestDebugText = stringBuilder.ToString();
      _dataLabel.text = _latestDebugText;
      if (!_copyFeedbackActive) {
        _copyStatusLabel.text = string.Empty;
        SetCopyButtonVisuals("Copy", CopyButtonDefaultTint);
      }
    }

  }
}