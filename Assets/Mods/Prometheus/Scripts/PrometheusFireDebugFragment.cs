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
    private TextField _dataTextField;
    private int _selectedEntityId;
    private bool _selectedEntityHasFireProfile;

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

      _titleLabel = new Label("Prometheus Fire Debug");
      _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _titleLabel.style.marginBottom = 4;
      _root.Add(_titleLabel);

      var toolbar = new VisualElement();
      toolbar.style.flexDirection = FlexDirection.Row;
      toolbar.style.justifyContent = Justify.FlexEnd;
      toolbar.style.marginBottom = 4;

      _copyButton = new Button(CopyDebugTextToClipboard) {
        text = "Copy"
      };
      _copyButton.style.height = 20;
      _copyButton.style.fontSize = 11;
      toolbar.Add(_copyButton);
      _root.Add(toolbar);

      _dataTextField = new TextField {
        multiline = true,
        isReadOnly = true,
      };
      _dataTextField.style.whiteSpace = WhiteSpace.Normal;
      _dataTextField.style.fontSize = 11;
      _dataTextField.style.minHeight = 160;
      _dataTextField.style.maxHeight = 360;
      _root.Add(_dataTextField);

      return _root;
    }

    private void CopyDebugTextToClipboard() {
      GUIUtility.systemCopyBuffer = _dataTextField?.value ?? string.Empty;
      _copyButton.text = "Copied";
    }

    public void ShowFragment(BaseComponent entity) {
      var fireProfile = entity.GetComponent<FireResponseProfile>();
      _selectedEntityHasFireProfile = fireProfile is not null;

      _selectedEntityId = entity.GameObject.GetInstanceID();
      _titleLabel.text = _selectedEntityHasFireProfile
        ? $"Prometheus Fire Debug — {entity.GameObject.name}"
        : $"Prometheus Fire Debug — {entity.GameObject.name} (no fire profile)";
      _root.style.display = DisplayStyle.Flex;
      UpdateFragment();
    }

    public void ClearFragment() {
      _selectedEntityId = 0;
      _selectedEntityHasFireProfile = false;
      _copyButton.text = "Copy";
      _root.style.display = DisplayStyle.None;
    }

    public void UpdateFragment() {
      if (_selectedEntityId == 0) {
        return;
      }

      var stringBuilder = new StringBuilder();

      stringBuilder.AppendLine("Entity");
      stringBuilder.AppendLine($"- FireResponseProfile component: {_selectedEntityHasFireProfile}");
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
      stringBuilder.AppendLine($"- Ignition/industrial x{tuning.IndustrialIgnitionMultiplier:0.00}");
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
        stringBuilder.AppendLine("- Snapshot unavailable");
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
        stringBuilder.AppendLine("- Snapshot unavailable");
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

      _dataTextField.value = stringBuilder.ToString();
      _copyButton.text = "Copy";
    }

  }
}