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
    private const float PreviewExclusionLogIntervalInSeconds = 12f;

    private FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private FireTuningRuntimeState _fireTuningRuntimeState;
    private FireSimulationRuntimeState _fireSimulationRuntimeState;
    private FireEntityRegistryRuntimeState _fireEntityRegistryRuntimeState;
    private FireWaterContextRuntimeState _fireWaterContextRuntimeState;
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
    private float _lastBurningTelemetryLogRealtime = -BurningTelemetryLogIntervalInSeconds;
    private GameObject _fireMarkerObject;
    private TextMesh _fireMarkerText;
    private float _fireMarkerBaseHeight = 2.25f;
    private float _fireMarkerPulseTime;
    private bool _explosionDetonatedDuringCurrentBurn;
    private float _explosionSuppressionDisruptionSecondsRemaining;
    private float _previewExclusionLogCooldownRemainingSeconds;
    private bool _terminalDeadStateApplied;
    private bool _demolitionSuppressed;

    [Inject]
    public void InjectDependencies(
      FireSuppressionRuntimeState fireSuppressionRuntimeState,
      FireTuningRuntimeState fireTuningRuntimeState,
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireEntityRegistryRuntimeState fireEntityRegistryRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDispatchScoringRuntimeState fireDispatchScoringRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      QuickNotificationService quickNotificationService) {
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
      _fireTuningRuntimeState = fireTuningRuntimeState;
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireEntityRegistryRuntimeState = fireEntityRegistryRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDispatchScoringRuntimeState = fireDispatchScoringRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _quickNotificationService = quickNotificationService;
    }

    public void Update() {
      var entityId = GameObject.GetInstanceID();

      if (TryGetPlacementPreviewExclusionReason(GameObject, out var exclusionReason)) {
        if (_previewExclusionLogCooldownRemainingSeconds <= 0f) {
          FireTelemetry.Log($"event={FireTelemetryEvents.PreviewExcluded} entity={GameObject.name} id={entityId} reason={exclusionReason}");
          _previewExclusionLogCooldownRemainingSeconds = PreviewExclusionLogIntervalInSeconds;
        }

        ResetSimulationForExcludedEntity(entityId);
        return;
      }

      if (_demolitionSuppressed) {
        ResetSimulationForExcludedEntity(entityId);
        return;
      }

      var hasExistingSnapshot = _fireSimulationRuntimeState.TryGetSnapshot(entityId, out _);

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds, hasExistingSnapshot)) {
        return;
      }

      AdvanceCooldownTimers();

      if (IsDeadBuilding(entityId)) {
        ApplyTerminalDeadBuildingState(entityId);
        return;
      }

      _terminalDeadStateApplied = false;

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
      var fireworksIgnition = 0f;
      var controlledBurnReadiness = Mathf.Clamp01((suppressionPower * 0.45f) + (waterEfficiency * 0.2f) + localWaterExposure);
      var controlledBurnEnvelope = neighborSpreadPressure <= 0.02f && localWaterExposure >= 0.08f && localWaterExposure <= 0.95f;
      var controlledBurnIgnition = _fireResponseProfile.SupportsControlledBurns && controlledBurnEnvelope
        ? _fireResponseProfile.ControlledBurnIgnitionChance * controlledBurnReadiness * tuning.ControlledBurnIgnitionMultiplier
        : 0f;

      var neighborSpreadIgnition = neighborSpreadPressure * 0.35f * tuning.NeighborIgnitionMultiplier;
      var explosionIgnition = 0f;

      var ignitionChance = weatherIgnition + industrialIgnition + controlledBurnIgnition + neighborSpreadIgnition;
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
        var consumeEvent = spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion
          ? FireTelemetryEvents.ExplosionIgnitionRequestConsumed
          : FireTelemetryEvents.SpreadIgnitionRequestConsumed;
        FireTelemetry.Log($"event={consumeEvent} entity={GameObject.name} id={entityId} sourceId={spreadIgnitionSourceEntityId} sourceKind={spreadSourceKindText} chance={spreadIgnitionPropagationChance:0.000} burningBeforeConsume={(_currentIntensity > 0f)}");
      }

      if (forcedIgnition) {
        shouldIgnite = true;
        FireTelemetry.Log($"event={FireTelemetryEvents.DebugIgniteRequest} entity={GameObject.name} id={entityId}");
      }

      if (!forcedIgnition
          && _currentIntensity <= 0f
          && _fireSimulationRuntimeState.DebugIgnitionSuppressed
          && (shouldIgnite || spreadIgnitionTriggered)) {
        shouldIgnite = false;
        spreadIgnitionTriggered = false;
        ignitionChance = 0f;
      }

      if (spreadIgnitionTriggered && _currentIntensity <= 0f) {
        shouldIgnite = true;
        ignitionChance = Mathf.Max(ignitionChance, spreadIgnitionPropagationChance);
        if (spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion) {
          explosionIgnition = Mathf.Max(explosionIgnition, spreadIgnitionPropagationChance);
        }
      } else if (spreadIgnitionTriggered && spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion) {
        FireTelemetry.Log($"event={FireTelemetryEvents.ExplosionIgniteNotApplied} entity={GameObject.name} id={entityId} sourceId={spreadIgnitionSourceEntityId} reason=already_burning intensity={_currentIntensity:0.000} chance={spreadIgnitionPropagationChance:0.000}");
      }

      if (shouldIgnite) {
        _currentIntensity = forcedIgnition
          ? Mathf.Max(_currentIntensity, 0.35f)
          : spreadIgnitionTriggered
            ? Mathf.Max(_currentIntensity, 0.24f)
            : 0.2f;

        if (forcedIgnition) {
          _quickNotificationService.SendNotification($"Prometheus: debug ignition triggered at {GameObject.name}.");
          FireTelemetry.Log($"event={FireTelemetryEvents.DebugIgniteApplied} entity={GameObject.name} id={entityId} intensity={_currentIntensity:0.000}");
        }

        if (spreadIgnitionTriggered) {
          var spreadSourceKindText = spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion ? "Explosion" : "Spread";
          var igniteEvent = spreadIgnitionSourceKind == PropagationIgnitionSourceKind.Explosion
            ? FireTelemetryEvents.ExplosionIgniteApplied
            : FireTelemetryEvents.SpreadIgniteApplied;
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

      var quenchingPower = FireSimulationRules.ComputeQuenchingPower(
        burning,
        suppressionPower,
        waterEfficiency,
        localQuenchingBonus,
        suppressionSnapshot.FactionApproach,
        _currentIntensity,
        localWaterExposure,
        tuning.QuenchingMultiplier,
        _explosionSuppressionDisruptionSecondsRemaining > 0f);

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
              FireTelemetry.Log($"event={FireTelemetryEvents.ExplosionIgnitionRequest} source={GameObject.name} sourceId={entityId} targetId={explosionTargetEntityId} chance={explosionPropagationChance:0.000} severity={explosionSeverity:0.000} mode={tuning.ExplosionIgnitionMode} forced={debugForceExplosionDetonation}");
            }
          }

          FireTelemetry.Log($"event={FireTelemetryEvents.ExplosionDetonated} entity={GameObject.name} id={entityId} severity={explosionSeverity:0.000} chance={detonationChance:0.000} mode={tuning.ExplosionIgnitionMode} forced={debugForceExplosionDetonation}");
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
            FireTelemetry.Log($"event={FireTelemetryEvents.SpreadPropagation} source={GameObject.name} sourceId={entityId} targetId={spreadTargetEntityId} chance={spreadPropagationChance:0.000} proximity={spreadProximity:0.000}");
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

      var dispatchDecision = FireSimulationRules.ComputeDispatchDecision(
        simulationSnapshot.Intensity,
        simulationSnapshot.SpreadPressure,
        simulationSnapshot.HeatExposure,
        simulationSnapshot.NeighborSpreadPressure,
        impactPressure,
        localWaterExposure,
        waterEfficiency,
        drynessFactor,
        _fireResponseProfile.DispatchSeverityWeight,
        _fireResponseProfile.DispatchAssetRiskWeight,
        _fireResponseProfile.DispatchTravelCostWeight,
        _fireResponseProfile.DispatchContainmentLeverageWeight,
        _dispatchAssignedScore,
        _dispatchAssignmentLockRemainingSeconds,
        suppressionSnapshot.AssignmentLockDurationInSeconds,
        suppressionSnapshot.RetargetHysteresisThreshold);
      _dispatchAssignedScore = dispatchDecision.AssignedScore;
      _dispatchAssignmentLockRemainingSeconds = dispatchDecision.AssignmentLockRemainingSeconds;

      var responseState = FireSimulationRules.DetermineResponseState(burning, _currentIntensity, spreadPressure, quenchingPower);

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
        FireTelemetry.Log($"event={FireTelemetryEvents.ResponseState} entity={GameObject.name} id={entityId} state={responseState} intensity={_currentIntensity:0.000} spread={spreadPressure:0.000} quench={quenchingPower:0.000}");
      }

      _responseState = responseState;

      var topDispatchFactor = DetermineTopDispatchFactor(
        dispatchDecision.SeverityFactor,
        dispatchDecision.AssetRiskFactor,
        dispatchDecision.TravelCostFactor,
        dispatchDecision.ContainmentLeverageFactor);

      var dispatchScoringSnapshot = new FireDispatchScoringSnapshot(
        dispatchDecision.CandidateScore,
        _dispatchAssignedScore,
        dispatchDecision.SeverityFactor,
        dispatchDecision.AssetRiskFactor,
        dispatchDecision.TravelCostFactor,
        dispatchDecision.ContainmentLeverageFactor,
        _dispatchAssignmentLockRemainingSeconds,
        dispatchDecision.HysteresisThreshold,
        dispatchDecision.AssignmentLocked,
        dispatchDecision.RetargetSuppressed,
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
        FireTelemetry.Log($"event={FireTelemetryEvents.Ignited} entity={GameObject.name} id={entityId} source={dominantIgnitionSource} ignitionChance={ignitionChance:0.000} intensity={_currentIntensity:0.000}");
        if (explosionDetonated) {
          _quickNotificationService.SendNotification($"💥 Prometheus: explosion risk triggered at {GameObject.name}.");
        }
      } else if (!simulationSnapshot.Burning && _wasBurning) {
        _quickNotificationService.SendNotification($"✅ Prometheus: fire extinguished at {GameObject.name}.");
        FireTelemetry.Log($"event={FireTelemetryEvents.Extinguished} entity={GameObject.name} id={entityId}");
      }

      if (simulationSnapshot.Burning
          && Time.realtimeSinceStartup - _lastBurningTelemetryLogRealtime >= BurningTelemetryLogIntervalInSeconds) {
        FireTelemetry.Log($"event={FireTelemetryEvents.BurningTick} entity={GameObject.name} id={entityId} intensity={simulationSnapshot.Intensity:0.000} spread={simulationSnapshot.SpreadPressure:0.000} quench={simulationSnapshot.QuenchingPower:0.000} heat={simulationSnapshot.HeatExposure:0.000} response={responseState}");
        _lastBurningTelemetryLogRealtime = Time.realtimeSinceStartup;
      }

      _wasBurning = simulationSnapshot.Burning;
    }

    internal bool DebugForceExtinguish() {
      var entityId = GameObject.GetInstanceID();
      var hadActiveFire = _currentIntensity > 0f || _wasBurning;

      _currentIntensity = 0f;
      _wasBurning = false;
      _dispatchAssignedScore = 0f;
      _dispatchAssignmentLockRemainingSeconds = 0f;
      _responseState = "Idle";
      _responseNotificationCooldownRemainingSeconds = 0f;
      _lastBurningTelemetryLogRealtime = -BurningTelemetryLogIntervalInSeconds;
      _explosionDetonatedDuringCurrentBurn = false;
      _explosionSuppressionDisruptionSecondsRemaining = 0f;

      if (_fireSimulationRuntimeState.TryGetSnapshot(entityId, out var snapshot)) {
        _fireSimulationRuntimeState.SetSnapshot(
          entityId,
          new FireSimulationSnapshot(
            false,
            0f,
            0f,
            snapshot.QuenchingPower,
            0f,
            snapshot.NeighborSpreadPressure,
            snapshot.IgnitionChance,
            "DebugForceExtinguish",
            snapshot.WeatherIgnitionContribution,
            snapshot.IndustrialIgnitionContribution,
            snapshot.FireworksIgnitionContribution,
            snapshot.ControlledBurnIgnitionContribution,
            snapshot.ExplosionIgnitionContribution,
            snapshot.DrynessFactor,
            snapshot.FuelFactor,
            snapshot.BarrierFactor));
      }

      var registrySnapshot = new FireEntityRegistrySnapshot(
        GameObject.transform.position,
        false,
        0f,
        0f);
      _fireEntityRegistryRuntimeState.SetSnapshot(entityId, registrySnapshot);

      if (_fireMarkerObject != null && _fireMarkerObject.activeSelf) {
        _fireMarkerObject.SetActive(false);
      }

      return hadActiveFire;
    }

    internal void DebugResetFireSimulationState() {
      var entityId = GameObject.GetInstanceID();
      _currentIntensity = 0f;
      _wasBurning = false;
      _dispatchAssignedScore = 0f;
      _dispatchAssignmentLockRemainingSeconds = 0f;
      _responseState = "Idle";
      _responseNotificationCooldownRemainingSeconds = 0f;
      _lastBurningTelemetryLogRealtime = -BurningTelemetryLogIntervalInSeconds;
      _explosionDetonatedDuringCurrentBurn = false;
      _explosionSuppressionDisruptionSecondsRemaining = 0f;
      _terminalDeadStateApplied = false;
      _demolitionSuppressed = false;

      _fireSimulationRuntimeState.RemoveSnapshot(entityId);
      _fireEntityRegistryRuntimeState.RemoveSnapshot(entityId);
      _fireDispatchScoringRuntimeState.RemoveSnapshot(entityId);

      if (_fireMarkerObject != null && _fireMarkerObject.activeSelf) {
        _fireMarkerObject.SetActive(false);
      }
    }

    private void ResetSimulationForExcludedEntity(int entityId) {
      _currentIntensity = 0f;
      _wasBurning = false;
      _dispatchAssignedScore = 0f;
      _dispatchAssignmentLockRemainingSeconds = 0f;
      _responseNotificationCooldownRemainingSeconds = 0f;
      _lastBurningTelemetryLogRealtime = -BurningTelemetryLogIntervalInSeconds;
      _explosionDetonatedDuringCurrentBurn = false;
      _explosionSuppressionDisruptionSecondsRemaining = 0f;

      _fireSimulationRuntimeState.RemoveSnapshot(entityId);
      _fireEntityRegistryRuntimeState.RemoveSnapshot(entityId);
      _fireDispatchScoringRuntimeState.RemoveSnapshot(entityId);

      if (_fireMarkerObject != null && _fireMarkerObject.activeSelf) {
        _fireMarkerObject.SetActive(false);
      }
    }

    private void AdvanceCooldownTimers() {
      _responseNotificationCooldownRemainingSeconds = Mathf.Max(0f, _responseNotificationCooldownRemainingSeconds - UpdateIntervalInSeconds);
      _dispatchAssignmentLockRemainingSeconds = Mathf.Max(0f, _dispatchAssignmentLockRemainingSeconds - UpdateIntervalInSeconds);
      _explosionSuppressionDisruptionSecondsRemaining = Mathf.Max(0f, _explosionSuppressionDisruptionSecondsRemaining - UpdateIntervalInSeconds);
      _previewExclusionLogCooldownRemainingSeconds = Mathf.Max(0f, _previewExclusionLogCooldownRemainingSeconds - UpdateIntervalInSeconds);
    }

    private bool IsDeadBuilding(int entityId) {
      return _fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageStateSnapshot)
             && damageStateSnapshot.Category == FireDamageCategory.Building
             && damageStateSnapshot.State == FireDamageState.Dead;
    }

    private void ApplyTerminalDeadBuildingState(int entityId) {
      var hadActiveFire = _currentIntensity > 0f
                          || _wasBurning
                          || (_fireSimulationRuntimeState.TryGetSnapshot(entityId, out var previousSnapshot)
                              && (previousSnapshot.Burning || previousSnapshot.Intensity > 0f));

      _currentIntensity = 0f;
      _wasBurning = false;
      _dispatchAssignedScore = 0f;
      _dispatchAssignmentLockRemainingSeconds = 0f;
      _responseState = "Stabilized";
      _explosionDetonatedDuringCurrentBurn = false;
      _explosionSuppressionDisruptionSecondsRemaining = 0f;

      _fireSimulationRuntimeState.RemoveSnapshot(entityId);
      _fireSimulationRuntimeState.SetSnapshot(
        entityId,
        FireSimulationRules.CreateTerminalDeadBuildingSnapshot());

      _fireEntityRegistryRuntimeState.RemoveSnapshot(entityId);
      _fireDispatchScoringRuntimeState.RemoveSnapshot(entityId);
      UpdateFireMarker(false, 0f, true);

      if (!_terminalDeadStateApplied) {
        FireTelemetry.Log($"event={FireTelemetryEvents.DeadBuildingFireTerminal} entity={GameObject.name} id={entityId} extinguished={hadActiveFire}");
        _terminalDeadStateApplied = true;
      }
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

    private static bool TryGetPlacementPreviewExclusionReason(GameObject gameObject, out string exclusionReason) {
      exclusionReason = string.Empty;

      if (gameObject == null) {
        return false;
      }

      if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) {
        exclusionReason = "scene_invalid";
        return true;
      }

      if ((gameObject.hideFlags & (HideFlags.DontSave | HideFlags.HideInHierarchy)) != 0) {
        exclusionReason = "hidden_or_dontsave";
        return true;
      }

      if (ContainsPreviewToken(gameObject.name)) {
        exclusionReason = "name_token";
        return true;
      }

      var parent = gameObject.transform.parent;
      if (parent != null && ContainsPreviewToken(parent.name)) {
        exclusionReason = "parent_name_token";
        return true;
      }

      var components = gameObject.GetComponents<Component>();
      for (var i = 0; i < components.Length; i++) {
        var component = components[i];
        if (component == null) {
          continue;
        }

        var componentTypeName = component.GetType().Name;
        if (IsKnownPreviewComponentTypeName(componentTypeName)) {
          exclusionReason = $"component_exact:{componentTypeName}";
          return true;
        }

        if (ContainsPreviewToken(componentTypeName)) {
          exclusionReason = $"component_token:{componentTypeName}";
          return true;
        }
      }

      exclusionReason = string.Empty;
      return false;
    }

    private static bool IsKnownPreviewComponentTypeName(string componentTypeName) {
      return componentTypeName == "BuildingPreview"
             || componentTypeName == "PlacementPreview"
             || componentTypeName == "BuildingPlacerPreview"
             || componentTypeName == "BlockObjectPlacerPreview"
             || componentTypeName == "BlueprintPreview"
             || componentTypeName == "PreviewCursor";
    }

    private static bool ContainsPreviewToken(string value) {
      if (string.IsNullOrWhiteSpace(value)) {
        return false;
      }

      var lower = value.ToLowerInvariant();
      return lower.Contains("preview")
             || lower.Contains("ghost")
             || lower.Contains("placement")
             || lower.Contains("placer")
             || lower.Contains("blueprint")
             || lower.Contains("cursor");
    }

  }
}
