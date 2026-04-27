using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireRecoveryController : BaseComponent,
                                         IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;
    private const float SimHoursPerTick = 0.25f;

    private FireExposureRuntimeState _fireExposureRuntimeState;
    private FireRecoveryRuntimeState _fireRecoveryRuntimeState;
    private FireRuntimeProjectionRuntimeState _fireRuntimeProjectionRuntimeState;

    private float _timeSinceLastUpdate;
    private bool _sawBurnPhase;
    private float _peakIntensityDuringBurn;
    private float _recoveryHoursRemaining;

    [Inject]
    public void InjectDependencies(
      FireExposureRuntimeState fireExposureRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState,
      FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState) {
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
    }

    public void Update() {
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      if (!_fireExposureRuntimeState.TryGetSnapshot(entityId, out var exposureSnapshot)) {
        return;
      }

      _fireRuntimeProjectionRuntimeState.SetExposure(entityId, exposureSnapshot);
      if (exposureSnapshot.Burning) {
        _sawBurnPhase = true;
        _peakIntensityDuringBurn = Mathf.Max(_peakIntensityDuringBurn, exposureSnapshot.Intensity);
      }

      if (!exposureSnapshot.Burning && _sawBurnPhase) {
        _recoveryHoursRemaining = 18f;

        _sawBurnPhase = false;
        _peakIntensityDuringBurn = 0f;
      }

      if (_recoveryHoursRemaining > 0f) {
        _recoveryHoursRemaining = Mathf.Max(0f, _recoveryHoursRemaining - SimHoursPerTick);
      }

      var active = _recoveryHoursRemaining > 0f;
      var fertilityBoost = active ? 0.12f : 0f;
      var growthSpeedBonus = active ? 0.1f : 0f;
      var yieldBonus = active ? 0.05f : 0f;

      var recoverySnapshot = new FireRecoverySnapshot(
        active,
        fertilityBoost,
        growthSpeedBonus,
        yieldBonus,
        _recoveryHoursRemaining);

      _fireRecoveryRuntimeState.SetSnapshot(entityId, recoverySnapshot);
      _fireRuntimeProjectionRuntimeState.SetRecovery(entityId, recoverySnapshot);
    }

    internal void DebugResetRecoveryState() {
      _sawBurnPhase = false;
      _peakIntensityDuringBurn = 0f;
      _recoveryHoursRemaining = 0f;
      _fireRecoveryRuntimeState.RemoveSnapshot(GameObject.GetInstanceID());
      _fireRuntimeProjectionRuntimeState.SetRecovery(GameObject.GetInstanceID(), FireRuntimeProjectionRules.DefaultRecovery);
    }

  }
}
