using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireRecoveryController : BaseComponent,
                                         IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;
    private const float SimHoursPerTick = 0.25f;

    private FireSimulationRuntimeState _fireSimulationRuntimeState;
    private FireWaterContextRuntimeState _fireWaterContextRuntimeState;
    private FireRecoveryRuntimeState _fireRecoveryRuntimeState;

    private float _timeSinceLastUpdate;
    private bool _wasBurning;
    private bool _sawBurnPhase;
    private float _peakIntensityDuringBurn;
    private float _recoveryHoursRemaining;
    private bool _lastControlledBurn;

    public void InjectDependencies(
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState) {
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
    }

    public void Update() {
      _timeSinceLastUpdate += Time.deltaTime;
      if (_timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      var entityId = GameObject.GetInstanceID();
      if (!_fireSimulationRuntimeState.TryGetSnapshot(entityId, out var simulationSnapshot)) {
        return;
      }

      _wasBurning = simulationSnapshot.Burning;
      if (_wasBurning) {
        _sawBurnPhase = true;
        _peakIntensityDuringBurn = Mathf.Max(_peakIntensityDuringBurn, simulationSnapshot.Intensity);
      }

      if (!simulationSnapshot.Burning && _sawBurnPhase) {
        var controlledBurn = IsControlledBurn(simulationSnapshot, entityId);

        _lastControlledBurn = controlledBurn;
        _recoveryHoursRemaining = controlledBurn ? 48f : 18f;

        _sawBurnPhase = false;
        _peakIntensityDuringBurn = 0f;
      }

      if (_recoveryHoursRemaining > 0f) {
        _recoveryHoursRemaining = Mathf.Max(0f, _recoveryHoursRemaining - SimHoursPerTick);
      }

      var active = _recoveryHoursRemaining > 0f;
      var fertilityBoost = active ? (_lastControlledBurn ? 0.35f : 0.12f) : 0f;
      var growthSpeedBonus = active ? (_lastControlledBurn ? 0.30f : 0.1f) : 0f;
      var yieldBonus = active ? (_lastControlledBurn ? 0.2f : 0.05f) : 0f;

      var recoverySnapshot = new FireRecoverySnapshot(
        _lastControlledBurn,
        active,
        fertilityBoost,
        growthSpeedBonus,
        yieldBonus,
        _recoveryHoursRemaining);

      _fireRecoveryRuntimeState.SetSnapshot(entityId, recoverySnapshot);
    }

    private bool IsControlledBurn(FireSimulationSnapshot simulationSnapshot, int entityId) {
      var waterExposure = 0f;
      if (_fireWaterContextRuntimeState.TryGetSnapshot(entityId, out var waterSnapshot)) {
        waterExposure = waterSnapshot.LocalWaterExposure;
      }

      var intensityInRange = _peakIntensityDuringBurn >= 0.2f && _peakIntensityDuringBurn <= 0.75f;
      var containedSpread = simulationSnapshot.NeighborSpreadPressure <= 0.025f;
      var moderatedByWater = waterExposure >= 0.08f && waterExposure <= 0.95f;

      return intensityInRange && containedSpread && moderatedByWater;
    }

  }
}