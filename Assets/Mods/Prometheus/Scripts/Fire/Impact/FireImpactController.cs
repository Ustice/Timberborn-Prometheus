using Bindito.Core;
using Timberborn.BaseComponentSystem;

namespace Mods.Prometheus.Scripts {
  internal class FireImpactController : BaseComponent,
                                       IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireExposureRuntimeState _fireExposureRuntimeState;
    private FireImpactRuntimeState _fireImpactRuntimeState;
    private FireRuntimeProjectionRuntimeState _fireRuntimeProjectionRuntimeState;
    private FireTuningRuntimeState _fireTuningRuntimeState;

    private float _timeSinceLastUpdate;

    [Inject]
    public void InjectDependencies(
      FireExposureRuntimeState fireExposureRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState,
      FireTuningRuntimeState fireTuningRuntimeState) {
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
      _fireTuningRuntimeState = fireTuningRuntimeState;
    }

    public void Update() {
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      if (!_fireExposureRuntimeState.TryGetSnapshot(entityId, out var exposureSnapshot)) {
        return;
      }

      var impactSnapshot = FireRuntimeProjectionRules.CreateImpact(
        exposureSnapshot,
        _fireTuningRuntimeState.Current.ImpactMultiplier);
      _fireImpactRuntimeState.SetSnapshot(entityId, impactSnapshot);
      _fireRuntimeProjectionRuntimeState.SetExposure(entityId, exposureSnapshot);
      _fireRuntimeProjectionRuntimeState.SetImpact(entityId, impactSnapshot);
    }

  }
}
