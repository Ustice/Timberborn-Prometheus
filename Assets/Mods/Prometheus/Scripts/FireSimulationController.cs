using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.QuickNotificationSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireSimulationController : BaseComponent,
                                           IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;
    private const float ResponseNotificationCooldownInSeconds = 9f;
    private const float BurningTelemetryLogIntervalInSeconds = 5f;

    private FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private FireTuningRuntimeState _fireTuningRuntimeState;
    private FireSimulationRuntimeState _fireSimulationRuntimeState;
    private FireEntityRegistryRuntimeState _fireEntityRegistryRuntimeState;
    private FireWaterContextRuntimeState _fireWaterContextRuntimeState;
    private FireFestivalRuntimeState _fireFestivalRuntimeState;
    private FireImpactRuntimeState _fireImpactRuntimeState;
    private FireDispatchScoringRuntimeState _fireDispatchScoringRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private QuickNotificationService _quickNotificationService;
    private FireResponseProfile _fireResponseProfile;

    private float _timeSinceLastUpdate;
    private float _currentIntensity;
    private bool _wasBurning;
    private float _dispatchAssignedScore;
    private float _dispatchAssignmentLockRemainingSeconds;
    private string _responseState = "Idle";
    private float _responseNotificationCooldownRemainingSeconds;
    private float _burningTelemetryLogCooldownRemainingSeconds;
    private GameObject _fireMarkerObject;
    private TextMesh _fireMarkerText;
    private float _fireMarkerBaseHeight = 2.25f;
    private float _fireMarkerPulseTime;
    private bool _explosionDetonatedDuringCurrentBurn;
    private float _explosionSuppressionDisruptionSecondsRemaining;

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
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      QuickNotificationService quickNotificationService) {
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
      _fireTuningRuntimeState = fireTuningRuntimeState;
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireEntityRegistryRuntimeState = fireEntityRegistryRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
      _fireFestivalRuntimeState = fireFestivalRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDispatchScoringRuntimeState = fireDispatchScoringRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _quickNotificationService = quickNotificationService;
    }

    public void Update() {
      var entityId = GameObject.GetInstanceID();
      var hasExistingSnapshot = _fireSimulationRuntimeState.TryGetSnapshot(entityId, out _);

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds, hasExistingSnapshot)) {
        return;
      }

      AdvanceCooldownTimers();

      if (!_fireSuppressionRuntimeState.TryGetSnapshot(entityId, out var suppressionSnapshot)) {
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
      if (_fireWaterContextRuntimeState.TryGetSnapshot(entityId, out var waterContextSnapshot)) {
        localWaterExposure = Mathf.Clamp01(waterContextSnapshot.LocalWaterExposure);
        localQuenchingBonus = Mathf.Max(0f, waterContextSnapshot.QuenchingBonus);
        localSpreadReduction = Mathf.Max(0f, waterContextSnapshot.SpreadReduction);
      }

      var festivalRiskBonus = 0f;
      if (_fireFestivalRuntimeState.TryGetSnapshot(entityId, out var festivalSnapshot)) {
        festivalRiskBonus = festivalSnapshot.FestivalRiskBonus;
      }

      var neighborSpreadPressure = _fireEntityRegistryRuntimeState.ComputeNeighborSpreadPressure(
        entityId,
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
      var explosionIgnition = 0f;

      var ignitionChance = weatherIgnition + industrialIgnition + fireworksIgnition + controlledBurnIgnition + neighborSpreadIgnition;
      ignitionChance *= (1f - (localWaterExposure * 0.5f));
      ignitionChance *= tuning.IgnitionMultiplier;
      ignitionChance = Mathf.Clamp01(ignitionChance);
      var shouldIgnite = _currentIntensity <= 0f && Random.value < ignitionChance;

      var forcedIgnition = _fireSimulationRuntimeState.ConsumeForcedIgnitionRequest(entityId);
      var spreadIgnitionTriggered = false;
      var spreadIgnitionSourceEntityId = 0;
      var spreadIgnitionPropagationChance = 0f;
      var spreadIgnitionSourceKind = PropagationIgnitionSourceKind.Spread;
      if (_fireSimulationRuntimeState.ConsumeSpreadIgnitionRequest(entityId, out var spreadIgnitionRequest)) {
        spreadIgnitionTriggered = true;
        spreadIgnitionSourceEntityId = spreadIgnitionRequest.SourceEntityId;
        spreadIgnitionPropagationChance = spreadIgnitionRequest.PropagationChance;
        spreadIgnitionSourceKind = spreadIgnitionRequest.SourceKind;

        var spreadSourceKindText = spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion ? "Explosion" : "Spread";
        var consumeEvent = spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion ? "explosion_ignition_request_consumed" : "spread_ignition_request_consumed";
        FireTelemetry.Log($"event={consumeEvent} entity={GameObject.name} id={entityId} sourceId={spreadIgnitionSourceEntityId} sourceKind={spreadSourceKindText} chance={spreadIgnitionPropagationChance:0.000} burningBeforeConsume={(_currentIntensity > 0f)}");
      }

      if (forcedIgnition) {
        shouldIgnite = true;
        FireTelemetry.Log($"event=debug_ignite_request entity={GameObject.name} id={entityId}");
      }

      if (spreadIgnitionTriggered && _currentIntensity <= 0f) {
        shouldIgnite = true;
        ignitionChance = Mathf.Max(ignitionChance, spreadIgnitionPropagationChance);
        if (spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion) {
          explosionIgnition = Mathf.Max(explosionIgnition, spreadIgnitionPropagationChance);
        }
      } else if (spreadIgnitionTriggered && spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion) {
        FireTelemetry.Log($"event=explosion_ignite_not_applied entity={GameObject.name} id={entityId} sourceId={spreadIgnitionSourceEntityId} reason=already_burning intensity={_currentIntensity:0.000} chance={spreadIgnitionPropagationChance:0.000}");
      }

      if (shouldIgnite) {
        _currentIntensity = forcedIgnition
          ? Mathf.Max(_currentIntensity, 0.35f)
          : spreadIgnitionTriggered
            ? Mathf.Max(_currentIntensity, 0.24f)
            : 0.2f;

        if (forcedIgnition) {
          _quickNotificationService.SendNotification($"Prometheus: debug ignition triggered at {GameObject.name}.");
          FireTelemetry.Log($"event=debug_ignite_applied entity={GameObject.name} id={entityId} intensity={_currentIntensity:0.000}");
        }

        if (spreadIgnitionTriggered) {
          var spreadSourceKindText = spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion ? "Explosion" : "Spread";
          var igniteEvent = spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion ? "explosion_ignite_applied" : "spread_ignite_applied";
          FireTelemetry.Log($"event={igniteEvent} entity={GameObject.name} id={entityId} sourceId={spreadIgnitionSourceEntityId} sourceKind={spreadSourceKindText} chance={spreadIgnitionPropagationChance:0.000} intensity={_currentIntensity:0.000}");
        }
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

      if (_explosionSuppressionDisruptionSecondsRemaining > 0f) {
        quenchingPower *= 0.65f;
      }

      if (burning) {
        _currentIntensity = Mathf.Clamp01(_currentIntensity + spreadPressure - quenchingPower);
      }

      if (_currentIntensity < 0.02f) {
        _currentIntensity = 0f;
      }

      if (!_wasBurning && _currentIntensity > 0f) {
        _explosionDetonatedDuringCurrentBurn = false;
      }

      var explosionDetonated = false;
      var isExplosiveHazardEntity = IsExplosiveHazardEntity();
      var explosionIgnitionEnabled = tuning.ExplosionIgnitionMode switch {
        ExplosionIgnitionMode.Always => true,
        ExplosionIgnitionMode.HighOnly => tuning.Profile == FireTuningProfile.High,
        _ => false,
      };
      var debugForceExplosionDetonation = forcedIgnition && isExplosiveHazardEntity;
      if (_currentIntensity > 0f
          && !_explosionDetonatedDuringCurrentBurn
          && isExplosiveHazardEntity
          && (explosionIgnitionEnabled || debugForceExplosionDetonation)) {
        var explosionSeverity = ComputeExplosionSeverityFromEntityName();
        var moistureMitigation = 1f - Mathf.Clamp01((localWaterExposure * 0.65f) + localSpreadReduction);
        var detonationChance = Mathf.Clamp01(
          (((_currentIntensity * 0.45f) + (drynessFactor * 0.35f) + (explosionSeverity * 0.2f))
           * tuning.ExplosionIgnitionMultiplier)
          * moistureMitigation);

        explosionIgnition = Mathf.Max(explosionIgnition, detonationChance);

        if (debugForceExplosionDetonation || (detonationChance > 0.05f && Random.value < detonationChance)) {
          _explosionDetonatedDuringCurrentBurn = true;
          _explosionSuppressionDisruptionSecondsRemaining = Mathf.Max(_explosionSuppressionDisruptionSecondsRemaining, 8f + (explosionSeverity * 4f));
          _currentIntensity = Mathf.Clamp01(_currentIntensity + (0.1f * explosionSeverity));
          explosionDetonated = true;

          var explosionRadius = Mathf.Lerp(10f, 16f, explosionSeverity);
          if (_fireEntityRegistryRuntimeState.TryGetNearestSpreadTarget(entityId, GameObject.transform.position, explosionRadius, out var explosionTargetEntityId, out var explosionTargetDistanceNormalized)) {
            var explosionProximity = 1f - explosionTargetDistanceNormalized;
            var explosionPropagationChance = Mathf.Clamp01(
              (((0.42f * explosionSeverity) + (0.38f * _currentIntensity) + (0.2f * explosionProximity))
               * tuning.ExplosionIgnitionMultiplier)
              * moistureMitigation);

            if (explosionPropagationChance > 0.04f) {
              _fireSimulationRuntimeState.RequestSpreadIgnition(
                explosionTargetEntityId,
                entityId,
                explosionPropagationChance,
                PropagationIgnitionSourceKind.Explosion);
              FireTelemetry.Log($"event=explosion_ignition_request source={GameObject.name} sourceId={entityId} targetId={explosionTargetEntityId} chance={explosionPropagationChance:0.000} severity={explosionSeverity:0.000} mode={tuning.ExplosionIgnitionMode} forced={debugForceExplosionDetonation}");
            }
          }

          FireTelemetry.Log($"event=explosion_detonated entity={GameObject.name} id={entityId} severity={explosionSeverity:0.000} chance={detonationChance:0.000} mode={tuning.ExplosionIgnitionMode} forced={debugForceExplosionDetonation}");
        }
      }

      if (_currentIntensity <= 0f) {
        _explosionDetonatedDuringCurrentBurn = false;
      }

      if (_currentIntensity > 0f) {
        var spreadRadius = Mathf.Lerp(8f, 14f, Mathf.Clamp01(_currentIntensity));
        if (_fireEntityRegistryRuntimeState.TryGetNearestSpreadTarget(entityId, GameObject.transform.position, spreadRadius, out var spreadTargetEntityId, out var spreadTargetDistanceNormalized)) {
          var spreadProximity = 1f - spreadTargetDistanceNormalized;
          var spreadPropagationChance = Mathf.Clamp01(
            ((spreadPressure * 2.2f)
             + (_currentIntensity * 0.45f)
             + (spreadProximity * 0.4f))
            * tuning.NeighborIgnitionMultiplier);

          if (spreadPropagationChance > 0.025f && Random.value < spreadPropagationChance) {
            _fireSimulationRuntimeState.RequestSpreadIgnition(spreadTargetEntityId, entityId, spreadPropagationChance);
            FireTelemetry.Log($"event=spread_propagation source={GameObject.name} sourceId={entityId} targetId={spreadTargetEntityId} chance={spreadPropagationChance:0.000} proximity={spreadProximity:0.000}");
          }
        }
      }

      var heatExposure = _currentIntensity * (1f - heatMitigation);

      var dominantIgnitionSource = DetermineDominantIgnitionSource(
        spreadIgnitionTriggered,
        spreadIgnitionSourceKind,
        ignitionChance,
        weatherIgnition,
        industrialIgnition,
        fireworksIgnition,
        controlledBurnIgnition,
        neighborSpreadIgnition,
        explosionIgnition);

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
        explosionIgnition,
        drynessFactor,
        fuelFactor,
        barrierFactor);

      var impactPressure = 0f;
      if (_fireImpactRuntimeState.TryGetSnapshot(entityId, out var impactSnapshot)) {
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

      var responseState = DetermineResponseState(burning, _currentIntensity, spreadPressure, quenchingPower);

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

      if (responseState != _responseState) {
        FireTelemetry.Log($"event=response_state entity={GameObject.name} id={entityId} state={responseState} intensity={_currentIntensity:0.000} spread={spreadPressure:0.000} quench={quenchingPower:0.000}");
      }

      _responseState = responseState;

      var topDispatchFactor = DetermineTopDispatchFactor(
        dispatchSeverityFactor,
        dispatchAssetRiskFactor,
        dispatchTravelCostFactor,
        dispatchContainmentLeverageFactor);

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

      _fireDispatchScoringRuntimeState.SetSnapshot(entityId, dispatchScoringSnapshot);

      _fireSimulationRuntimeState.SetSnapshot(entityId, simulationSnapshot);
      var isDead = _fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageStateSnapshot)
           && damageStateSnapshot.State == FireDamageState.Dead;
      UpdateFireMarker(simulationSnapshot.Burning, simulationSnapshot.Intensity, isDead);

      var registrySnapshot = new FireEntityRegistrySnapshot(
        GameObject.transform.position,
        simulationSnapshot.Burning,
        simulationSnapshot.Intensity,
        simulationSnapshot.SpreadPressure);
      _fireEntityRegistryRuntimeState.SetSnapshot(entityId, registrySnapshot);

      if (simulationSnapshot.Burning && !_wasBurning) {
        _quickNotificationService.SendNotification($"🔥 Prometheus: fire ignited at {GameObject.name}.");
        FireTelemetry.Log($"event=ignited entity={GameObject.name} id={entityId} source={dominantIgnitionSource} ignitionChance={ignitionChance:0.000} intensity={_currentIntensity:0.000}");
        if (explosionDetonated) {
          _quickNotificationService.SendNotification($"💥 Prometheus: explosion risk triggered at {GameObject.name}.");
        }
      } else if (!simulationSnapshot.Burning && _wasBurning) {
        _quickNotificationService.SendNotification($"✅ Prometheus: fire extinguished at {GameObject.name}.");
        FireTelemetry.Log($"event=extinguished entity={GameObject.name} id={entityId}");
      }

      if (simulationSnapshot.Burning && _burningTelemetryLogCooldownRemainingSeconds <= 0f) {
        FireTelemetry.Log($"event=burning_tick entity={GameObject.name} id={entityId} intensity={simulationSnapshot.Intensity:0.000} spread={simulationSnapshot.SpreadPressure:0.000} quench={simulationSnapshot.QuenchingPower:0.000} heat={simulationSnapshot.HeatExposure:0.000} response={responseState}");
        _burningTelemetryLogCooldownRemainingSeconds = BurningTelemetryLogIntervalInSeconds;
      }

      _wasBurning = simulationSnapshot.Burning;
    }

    private void AdvanceCooldownTimers() {
      _responseNotificationCooldownRemainingSeconds = Mathf.Max(0f, _responseNotificationCooldownRemainingSeconds - UpdateIntervalInSeconds);
      _dispatchAssignmentLockRemainingSeconds = Mathf.Max(0f, _dispatchAssignmentLockRemainingSeconds - UpdateIntervalInSeconds);
      _burningTelemetryLogCooldownRemainingSeconds = Mathf.Max(0f, _burningTelemetryLogCooldownRemainingSeconds - UpdateIntervalInSeconds);
      _explosionSuppressionDisruptionSecondsRemaining = Mathf.Max(0f, _explosionSuppressionDisruptionSecondsRemaining - UpdateIntervalInSeconds);
    }

    private static string DetermineDominantIgnitionSource(
      bool spreadIgnitionTriggered,
      PropagationIgnitionSourceKind spreadIgnitionSourceKind,
      float ignitionChance,
      float weatherIgnition,
      float industrialIgnition,
      float fireworksIgnition,
      float controlledBurnIgnition,
      float neighborSpreadIgnition,
      float explosionIgnition) {
      if (spreadIgnitionTriggered && spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion) {
        return "Explosion";
      }

      if (spreadIgnitionTriggered) {
        return "NeighborSpread";
      }

      if (ignitionChance <= 0f) {
        return "None";
      }

      var dominantContribution = Mathf.Max(
        Mathf.Max(weatherIgnition, industrialIgnition),
        Mathf.Max(Mathf.Max(fireworksIgnition, controlledBurnIgnition), Mathf.Max(neighborSpreadIgnition, explosionIgnition)));

      if (dominantContribution == weatherIgnition) {
        return "Weather";
      }

      if (dominantContribution == industrialIgnition) {
        return "Industrial";
      }

      if (dominantContribution == fireworksIgnition) {
        return "Fireworks";
      }

      if (dominantContribution == controlledBurnIgnition) {
        return "ControlledBurn";
      }

      return dominantContribution == explosionIgnition ? "Explosion" : "NeighborSpread";
    }

    private static string DetermineResponseState(bool burning, float intensity, float spreadPressure, float quenchingPower) {
      if (!burning) {
        return "Stabilized";
      }

      if (intensity >= 0.6f && spreadPressure > (quenchingPower * 1.05f)) {
        return "Overwhelmed";
      }

      return quenchingPower >= (spreadPressure * 1.2f) && intensity <= 0.45f ? "Contained" : "Stabilized";
    }

    private static string DetermineTopDispatchFactor(
      float dispatchSeverityFactor,
      float dispatchAssetRiskFactor,
      float dispatchTravelCostFactor,
      float dispatchContainmentLeverageFactor) {
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

      return topDispatchFactor;
    }

    private bool IsExplosiveHazardEntity() {
      var entityName = GameObject.name;
      if (string.IsNullOrEmpty(entityName)) {
        return false;
      }

      var normalizedName = entityName.ToLowerInvariant();
      return normalizedName.Contains("explosive")
             || normalizedName.Contains("dynamite")
             || normalizedName.Contains("blast")
             || normalizedName.Contains("powder")
             || normalizedName.Contains("charge")
             || normalizedName.Contains("tnt")
             || normalizedName.Contains("warehouse");
    }

    private float ComputeExplosionSeverityFromEntityName() {
      var normalizedName = (GameObject.name ?? string.Empty).ToLowerInvariant();

      if (normalizedName.Contains("factory")) {
        return 1f;
      }

      if (normalizedName.Contains("warehouse") || normalizedName.Contains("storage")) {
        return 0.78f;
      }

      if (normalizedName.Contains("crate") || normalizedName.Contains("charge") || normalizedName.Contains("dynamite")) {
        return 0.56f;
      }

      return 0.45f;
    }

    private void UpdateFireMarker(bool burning, float intensity, bool dead) {
      if (!burning && !dead) {
        if (_fireMarkerObject != null) {
          _fireMarkerObject.SetActive(false);
        }

        return;
      }

      EnsureFireMarkerCreated();
      if (_fireMarkerObject == null || _fireMarkerText == null) {
        return;
      }

      if (!_fireMarkerObject.activeSelf) {
        _fireMarkerObject.SetActive(true);
      }

      float scale;
      if (dead) {
        _fireMarkerText.text = "DEAD";
        _fireMarkerText.color = new Color(0.68f, 0.63f, 0.62f, 1f);
        scale = 1.05f;
      } else {
        _fireMarkerText.text = "FIRE!";
        _fireMarkerPulseTime += Time.deltaTime;
        var pulse = 0.85f + (Mathf.Sin(_fireMarkerPulseTime * 6f) * 0.15f);
        scale = Mathf.Lerp(0.8f, 1.35f, Mathf.Clamp01(intensity)) * pulse;

        var intensity01 = Mathf.Clamp01(intensity);
        _fireMarkerText.color = Color.Lerp(new Color(1f, 0.82f, 0.28f, 1f), new Color(1f, 0.24f, 0.12f, 1f), intensity01);
      }

      _fireMarkerObject.transform.localPosition = new Vector3(0f, _fireMarkerBaseHeight, 0f);
      _fireMarkerObject.transform.localScale = new Vector3(scale, scale, scale);

      var mainCamera = Camera.main;
      if (mainCamera != null) {
        var direction = _fireMarkerObject.transform.position - mainCamera.transform.position;
        if (direction.sqrMagnitude > 0.0001f) {
          _fireMarkerObject.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
      }
    }

    private void EnsureFireMarkerCreated() {
      if (_fireMarkerObject != null && _fireMarkerText != null) {
        return;
      }

      var renderers = GameObject.GetComponentsInChildren<Renderer>();
      if (renderers.Length > 0) {
        var maxY = float.MinValue;
        for (var i = 0; i < renderers.Length; i++) {
          if (renderers[i] == null) {
            continue;
          }

          maxY = Mathf.Max(maxY, renderers[i].bounds.max.y);
        }

        if (maxY > float.MinValue) {
          var localOffset = maxY - GameObject.transform.position.y;
          _fireMarkerBaseHeight = Mathf.Clamp(localOffset + 0.9f, 1.8f, 8f);
        }
      }

      _fireMarkerObject = new GameObject("PrometheusFireMarker");
      _fireMarkerObject.transform.SetParent(GameObject.transform, false);
      _fireMarkerObject.transform.localPosition = new Vector3(0f, _fireMarkerBaseHeight, 0f);
      _fireMarkerObject.transform.localRotation = Quaternion.identity;
      _fireMarkerObject.transform.localScale = Vector3.one;

      _fireMarkerText = _fireMarkerObject.AddComponent<TextMesh>();
      _fireMarkerText.text = "FIRE!";
      _fireMarkerText.anchor = TextAnchor.MiddleCenter;
      _fireMarkerText.alignment = TextAlignment.Center;
      _fireMarkerText.fontSize = 64;
      _fireMarkerText.characterSize = 0.12f;
      _fireMarkerText.color = new Color(1f, 0.4f, 0.18f, 1f);

      _fireMarkerObject.SetActive(false);
    }

  }
}