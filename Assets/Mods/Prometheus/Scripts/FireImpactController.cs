using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireImpactController : BaseComponent,
                                       IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireSimulationRuntimeState _fireSimulationRuntimeState;
    private FireImpactRuntimeState _fireImpactRuntimeState;
    private FireTuningRuntimeState _fireTuningRuntimeState;

    private float _timeSinceLastUpdate;

    [Inject]
    public void InjectDependencies(
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireTuningRuntimeState fireTuningRuntimeState) {
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireTuningRuntimeState = fireTuningRuntimeState;
    }

    public void Update() {
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      if (!_fireSimulationRuntimeState.TryGetSnapshot(entityId, out var simulationSnapshot)) {
        return;
      }

      var effectiveIntensity = Mathf.Clamp01(Mathf.Max(
        simulationSnapshot.Intensity,
        simulationSnapshot.HeatExposure,
        simulationSnapshot.EmberPressure * 0.75f));
      var dehydrationPressure = Mathf.Clamp01((simulationSnapshot.HeatExposure * 0.85f) + (simulationSnapshot.Smoke * 0.25f));

      var cropDamagePressure = effectiveIntensity * 0.8f;
      var treeDamagePressure = effectiveIntensity * 0.65f;
      var buildingDamagePressure = effectiveIntensity * 0.45f;
      var injuryPressure = Mathf.Clamp01((dehydrationPressure * 0.6f) + (effectiveIntensity * 0.2f));

      var impactMultiplier = _fireTuningRuntimeState.Current.ImpactMultiplier;
      cropDamagePressure = Mathf.Clamp01(cropDamagePressure * impactMultiplier);
      treeDamagePressure = Mathf.Clamp01(treeDamagePressure * impactMultiplier);
      buildingDamagePressure = Mathf.Clamp01(buildingDamagePressure * impactMultiplier);
      dehydrationPressure = Mathf.Clamp01(dehydrationPressure * impactMultiplier);
      injuryPressure = Mathf.Clamp01(injuryPressure * impactMultiplier);

      var impactSnapshot = new FireImpactSnapshot(
        cropDamagePressure,
        treeDamagePressure,
        buildingDamagePressure,
        dehydrationPressure,
        injuryPressure);

      _fireImpactRuntimeState.SetSnapshot(entityId, impactSnapshot);
    }

  }
}
