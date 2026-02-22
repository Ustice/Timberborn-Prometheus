using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.QuickNotificationSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireSimulationController : BaseComponent,
                                           IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;
    private const float ResponseNotificationCooldownInSeconds = 9f;

    private FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private FireTuningRuntimeState _fireTuningRuntimeState;
    private FireSimulationRuntimeState _fireSimulationRuntimeState;
    private FireEntityRegistryRuntimeState _fireEntityRegistryRuntimeState;
    private FireWaterContextRuntimeState _fireWaterContextRuntimeState;
    private FireFestivalRuntimeState _fireFestivalRuntimeState;
    private FireImpactRuntimeState _fireImpactRuntimeState;
    private FireDispatchScoringRuntimeState _fireDispatchScoringRuntimeState;
    private QuickNotificationService _quickNotificationService;
    private FireResponseProfile _fireResponseProfile;

    private float _timeSinceLastUpdate;
    private float _currentIntensity;
    private bool _wasBurning;
    private float _dispatchAssignedScore;
    private float _dispatchAssignmentLockRemainingSeconds;
    private string _responseState = "Idle";
    private float _responseNotificationCooldownRemainingSeconds;

    [Inject]
    public void InjectDependencies(
      FireSuppressionRuntimeState fireSuppressionRuntimeState,
      FireTuningRuntimeState fireTuningRuntimeState,
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireEntityRegistryRuntimeState fireEntityRegistryRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireFestivalRuntimeState fireFestivalRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDispatchScoringRuntimeState fireDispatchScoringRuntimeState,
      QuickNotificationService quickNotificationService) {
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
      _fireTuningRuntimeState = fireTuningRuntimeState;
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireEntityRegistryRuntimeState = fireEntityRegistryRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
      _fireFestivalRuntimeState = fireFestivalRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDispatchScoringRuntimeState = fireDispatchScoringRuntimeState;
      _quickNotificationService = quickNotificationService;
    }

    public void Update() {
      _timeSinceLastUpdate += Time.deltaTime;
      if (_timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      _responseNotificationCooldownRemainingSeconds = Mathf.Max(0f, _responseNotificationCooldownRemainingSeconds - UpdateIntervalInSeconds);
      _dispatchAssignmentLockRemainingSeconds = Mathf.Max(0f, _dispatchAssignmentLockRemainingSeconds - UpdateIntervalInSeconds);

      if (!_fireSuppressionRuntimeState.TryGetSnapshot(GameObject.GetInstanceID(), out var suppressionSnapshot)) {
        return;
      }

      _fireResponseProfile ??= GetComponent<FireResponseProfile>();
      if (_fireResponseProfile == null) {
        return;
      }

      var heatMitigation = Mathf.Clamp01(suppressionSnapshot.HeatMitigation);
      var suppressionPower = Mathf.Max(0f, suppressionSnapshot.SuppressionPower);
      var waterEfficiency = Mathf.Max(0f, suppressionSnapshot.WaterEfficiency);

      var localWaterExposure = 0f;
      var localQuenchingBonus = 0f;
      var localSpreadReduction = 0f;
      if (_fireWaterContextRuntimeState.TryGetSnapshot(GameObject.GetInstanceID(), out var waterContextSnapshot)) {
        localWaterExposure = Mathf.Clamp01(waterContextSnapshot.LocalWaterExposure);
        localQuenchingBonus = Mathf.Max(0f, waterContextSnapshot.QuenchingBonus);
        localSpreadReduction = Mathf.Max(0f, waterContextSnapshot.SpreadReduction);
      }

      var festivalRiskBonus = 0f;
      if (_fireFestivalRuntimeState.TryGetSnapshot(GameObject.GetInstanceID(), out var festivalSnapshot)) {
        festivalRiskBonus = festivalSnapshot.FestivalRiskBonus;
      }

      var neighborSpreadPressure = _fireEntityRegistryRuntimeState.ComputeNeighborSpreadPressure(
        GameObject.GetInstanceID(),
        GameObject.transform.position,
        14f);

      var drynessFactor = Mathf.Clamp01(1f - localWaterExposure);
      var ignitionSensitivity = _fireResponseProfile.IgnitionSensitivity;
      var tuning = _fireTuningRuntimeState.Current;

      var weatherIgnition = _fireResponseProfile.WeatherIgnitionChance
        * drynessFactor
        * ignitionSensitivity
        * tuning.WeatherIgnitionMultiplier;
      var industrialIgnition = _fireResponseProfile.IndustrialIgnitionChance
        * (1f - (heatMitigation * 0.45f))
        * ignitionSensitivity
        * tuning.IndustrialIgnitionMultiplier;
      var fireworksIgnition = (festivalRiskBonus + _fireResponseProfile.FireworksIgnitionBonus)
        * drynessFactor
        * tuning.FireworksIgnitionMultiplier;

      var controlledBurnReadiness = Mathf.Clamp01((suppressionPower * 0.45f) + (waterEfficiency * 0.2f) + localWaterExposure);
      var controlledBurnEnvelope = neighborSpreadPressure <= 0.02f && localWaterExposure >= 0.08f && localWaterExposure <= 0.95f;
      var controlledBurnIgnition = _fireResponseProfile.SupportsControlledBurns && controlledBurnEnvelope
        ? _fireResponseProfile.ControlledBurnIgnitionChance * controlledBurnReadiness * tuning.ControlledBurnIgnitionMultiplier
        : 0f;

      var neighborSpreadIgnition = neighborSpreadPressure * 0.35f * tuning.NeighborIgnitionMultiplier;

      var ignitionChance = weatherIgnition + industrialIgnition + fireworksIgnition + controlledBurnIgnition + neighborSpreadIgnition;
      ignitionChance *= (1f - (localWaterExposure * 0.5f));
      ignitionChance *= tuning.IgnitionMultiplier;
      ignitionChance = Mathf.Clamp01(ignitionChance);
      var shouldIgnite = _currentIntensity <= 0f && Random.value < ignitionChance;

      if (shouldIgnite) {
        _currentIntensity = 0.2f;
      }

      var burning = _currentIntensity > 0f;

      var fuelFactor = _fireResponseProfile.FuelSpreadMultiplier * tuning.FuelSpreadMultiplier;
      var barrierFactor = Mathf.Clamp01((localSpreadReduction + _fireResponseProfile.SpreadBarrierResistance + (localWaterExposure * 0.2f)) * tuning.BarrierResistanceMultiplier);
      var baseSpread = 0.06f * fuelFactor;
      var drynessSpreadFactor = 0.55f + (drynessFactor * tuning.DrynessSpreadMultiplier);
      var spreadPressure = burning ? Mathf.Max(0f, ((baseSpread + neighborSpreadPressure) * drynessSpreadFactor) - barrierFactor) : 0f;
      spreadPressure *= tuning.SpreadMultiplier;

      var quenchingPower = burning ? ((suppressionPower * 0.035f) + (waterEfficiency * 0.02f) + localQuenchingBonus) : 0f;

      var factionApproach = suppressionSnapshot.FactionApproach ?? string.Empty;
      if (factionApproach.Contains("Folktails", System.StringComparison.OrdinalIgnoreCase)) {
        var relayDistancePenalty = Mathf.Clamp01(1f - localWaterExposure);
        var relayEfficiency = Mathf.Clamp(1f - (relayDistancePenalty * 0.45f), 0.55f, 1.2f);
        quenchingPower *= relayEfficiency;
      } else if (factionApproach.Contains("Ironteeth", System.StringComparison.OrdinalIgnoreCase)
                 || factionApproach.Contains("IronTeeth", System.StringComparison.OrdinalIgnoreCase)) {
        var highHeatSuppressionBonus = Mathf.Clamp01(_currentIntensity * 1.1f) * 0.35f;
        quenchingPower *= 1f + highHeatSuppressionBonus;
      }

      quenchingPower *= tuning.QuenchingMultiplier;

      if (burning) {
        _currentIntensity = Mathf.Clamp01(_currentIntensity + spreadPressure - quenchingPower);
      }

      if (_currentIntensity < 0.02f) {
        _currentIntensity = 0f;
      }

      var heatExposure = _currentIntensity * (1f - heatMitigation);

      var dominantIgnitionSource = "None";
      if (ignitionChance > 0f) {
        var dominantContribution = Mathf.Max(
          Mathf.Max(weatherIgnition, industrialIgnition),
          Mathf.Max(Mathf.Max(fireworksIgnition, controlledBurnIgnition), neighborSpreadIgnition));

        if (dominantContribution == weatherIgnition) {
          dominantIgnitionSource = "Weather";
        } else if (dominantContribution == industrialIgnition) {
          dominantIgnitionSource = "Industrial";
        } else if (dominantContribution == fireworksIgnition) {
          dominantIgnitionSource = "Fireworks";
        } else if (dominantContribution == controlledBurnIgnition) {
          dominantIgnitionSource = "ControlledBurn";
        } else {
          dominantIgnitionSource = "NeighborSpread";
        }
      }

      var simulationSnapshot = new FireSimulationSnapshot(
        _currentIntensity > 0f,
        _currentIntensity,
        heatExposure,
        quenchingPower,
        spreadPressure,
        neighborSpreadPressure,
        ignitionChance,
        dominantIgnitionSource,
        weatherIgnition,
        industrialIgnition,
        fireworksIgnition,
        controlledBurnIgnition,
        drynessFactor,
        fuelFactor,
        barrierFactor);

      var impactPressure = 0f;
      if (_fireImpactRuntimeState.TryGetSnapshot(GameObject.GetInstanceID(), out var impactSnapshot)) {
        impactPressure = Mathf.Clamp01(
          impactSnapshot.CropDamagePressure
          + impactSnapshot.TreeDamagePressure
          + impactSnapshot.BuildingDamagePressure
          + (impactSnapshot.DehydrationPressure * 0.5f)
          + (impactSnapshot.InjuryPressure * 0.75f));
      }

      var dispatchSeverityFactor = Mathf.Clamp01((simulationSnapshot.Intensity * 0.65f) + (simulationSnapshot.SpreadPressure * 0.35f));
      var dispatchAssetRiskFactor = Mathf.Clamp01((impactPressure * 0.8f) + (simulationSnapshot.HeatExposure * 0.2f));
      var dispatchTravelCostFactor = Mathf.Clamp01((1f - localWaterExposure) * 0.55f + (1f - Mathf.Clamp01(waterEfficiency)) * 0.45f);
      var dispatchContainmentLeverageFactor = Mathf.Clamp01((simulationSnapshot.NeighborSpreadPressure * 0.7f) + (drynessFactor * 0.3f));

      var dispatchSeverityWeight = Mathf.Max(0f, _fireResponseProfile.DispatchSeverityWeight);
      var dispatchAssetRiskWeight = Mathf.Max(0f, _fireResponseProfile.DispatchAssetRiskWeight);
      var dispatchTravelCostWeight = Mathf.Max(0f, _fireResponseProfile.DispatchTravelCostWeight);
      var dispatchContainmentLeverageWeight = Mathf.Max(0f, _fireResponseProfile.DispatchContainmentLeverageWeight);

      var dispatchWeightSum = dispatchSeverityWeight + dispatchAssetRiskWeight + dispatchTravelCostWeight + dispatchContainmentLeverageWeight;
      if (dispatchWeightSum < 0.001f) {
        dispatchSeverityWeight = 0.4f;
        dispatchAssetRiskWeight = 0.3f;
        dispatchTravelCostWeight = 0.2f;
        dispatchContainmentLeverageWeight = 0.25f;
        dispatchWeightSum = dispatchSeverityWeight + dispatchAssetRiskWeight + dispatchTravelCostWeight + dispatchContainmentLeverageWeight;
      }

      dispatchSeverityWeight /= dispatchWeightSum;
      dispatchAssetRiskWeight /= dispatchWeightSum;
      dispatchTravelCostWeight /= dispatchWeightSum;
      dispatchContainmentLeverageWeight /= dispatchWeightSum;

      var dispatchTotalScore =
        (dispatchSeverityFactor * dispatchSeverityWeight)
        + (dispatchAssetRiskFactor * dispatchAssetRiskWeight)
        + (dispatchContainmentLeverageFactor * dispatchContainmentLeverageWeight)
        - (dispatchTravelCostFactor * dispatchTravelCostWeight);
      dispatchTotalScore = Mathf.Max(0f, dispatchTotalScore);

      var hysteresisThreshold = Mathf.Max(0f, suppressionSnapshot.RetargetHysteresisThreshold);
      var assignmentLockDurationInSeconds = Mathf.Max(0f, suppressionSnapshot.AssignmentLockDurationInSeconds);
      var scoreDelta = dispatchTotalScore - _dispatchAssignedScore;
      var assignmentLocked = _dispatchAssignmentLockRemainingSeconds > 0f;
      var retargetSuppressed = false;

      if (_dispatchAssignedScore <= 0.0001f) {
        _dispatchAssignedScore = dispatchTotalScore;
        _dispatchAssignmentLockRemainingSeconds = assignmentLockDurationInSeconds;
      } else if (assignmentLocked) {
        retargetSuppressed = scoreDelta >= hysteresisThreshold;
        _dispatchAssignedScore = Mathf.Lerp(_dispatchAssignedScore, dispatchTotalScore, 0.08f);
      } else if (scoreDelta >= hysteresisThreshold) {
        _dispatchAssignedScore = dispatchTotalScore;
        _dispatchAssignmentLockRemainingSeconds = assignmentLockDurationInSeconds;
      } else {
        retargetSuppressed = scoreDelta > 0f;
        _dispatchAssignedScore = Mathf.Lerp(_dispatchAssignedScore, dispatchTotalScore, 0.12f);
      }

      _dispatchAssignedScore = Mathf.Max(0f, _dispatchAssignedScore);

      var responseState = "Stabilized";
      if (!burning) {
        responseState = "Stabilized";
      } else if (_currentIntensity >= 0.6f && spreadPressure > (quenchingPower * 1.05f)) {
        responseState = "Overwhelmed";
      } else if (quenchingPower >= (spreadPressure * 1.2f) && _currentIntensity <= 0.45f) {
        responseState = "Contained";
      }

      if (responseState != _responseState && _responseNotificationCooldownRemainingSeconds <= 0f) {
        if (responseState == "Overwhelmed") {
          _quickNotificationService.SendNotification($"Prometheus: fire response overwhelmed at {GameObject.name}.");
        } else if (responseState == "Contained") {
          _quickNotificationService.SendNotification($"Prometheus: containment established at {GameObject.name}.");
        } else if (responseState == "Stabilized") {
          _quickNotificationService.SendNotification($"Prometheus: firefront stabilized at {GameObject.name}.");
        }

        _responseNotificationCooldownRemainingSeconds = ResponseNotificationCooldownInSeconds;
      }

      _responseState = responseState;

      var topDispatchFactor = "Severity";
      var highestFactorValue = dispatchSeverityFactor;
      if (dispatchAssetRiskFactor > highestFactorValue) {
        topDispatchFactor = "AssetRisk";
        highestFactorValue = dispatchAssetRiskFactor;
      }

      if (dispatchTravelCostFactor > highestFactorValue) {
        topDispatchFactor = "TravelCost";
        highestFactorValue = dispatchTravelCostFactor;
      }

      if (dispatchContainmentLeverageFactor > highestFactorValue) {
        topDispatchFactor = "ContainmentLeverage";
      }

      var dispatchScoringSnapshot = new FireDispatchScoringSnapshot(
        dispatchTotalScore,
        _dispatchAssignedScore,
        dispatchSeverityFactor,
        dispatchAssetRiskFactor,
        dispatchTravelCostFactor,
        dispatchContainmentLeverageFactor,
        _dispatchAssignmentLockRemainingSeconds,
        hysteresisThreshold,
        _dispatchAssignmentLockRemainingSeconds > 0f,
        retargetSuppressed,
        responseState,
        topDispatchFactor);

      _fireDispatchScoringRuntimeState.SetSnapshot(GameObject.GetInstanceID(), dispatchScoringSnapshot);

      _fireSimulationRuntimeState.SetSnapshot(GameObject.GetInstanceID(), simulationSnapshot);

      var registrySnapshot = new FireEntityRegistrySnapshot(
        GameObject.transform.position,
        simulationSnapshot.Burning,
        simulationSnapshot.Intensity,
        simulationSnapshot.SpreadPressure);
      _fireEntityRegistryRuntimeState.SetSnapshot(GameObject.GetInstanceID(), registrySnapshot);

      if (simulationSnapshot.Burning && !_wasBurning) {
        _quickNotificationService.SendNotification($"Prometheus: fire ignited at {GameObject.name}.");
      } else if (!simulationSnapshot.Burning && _wasBurning) {
        _quickNotificationService.SendNotification($"Prometheus: fire extinguished at {GameObject.name}.");
      }

      _wasBurning = simulationSnapshot.Burning;
    }

  }
}