using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireSimulationController : BaseComponent,
                                            IAwakableComponent,
                                            IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 0.5f;

    private FireSimulationRuntimeState _fireSimulationRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireProfile _fireProfile;
    private float _timeSinceLastUpdate;
    private bool _wasBurning;

    [Inject]
    public void InjectDependencies(
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState) {
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
    }

    public void Awake() {
      _fireProfile = GetComponent<FireProfile>();
    }

    public void Update() {
      _fireSimulationRuntimeState.TickIgnitionBlock(Time.deltaTime);
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      if (_fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageState)
          && damageState.State == FireDamageState.Dead) {
        PublishSnapshot(entityId, FireSimulationRules.CreateTerminalDeadBuildingSnapshot());
        return;
      }

      if (_fireSimulationRuntimeState.ConsumeForcedIgnitionRequest(entityId)) {
        PublishSnapshot(entityId, FireSimulationRules.CreateSeededDebugSnapshot());
        FireTelemetry.Log($"event={FireTelemetryEvents.GridIgnitionSeeded} entity={GameObject.name} id={entityId}");
        return;
      }

      if (!_fireSimulationRuntimeState.TryGetSnapshot(entityId, out var existing)) {
        PublishSnapshot(entityId, FireSimulationRules.CreateColdSnapshot());
        return;
      }

      if (!existing.Burning) {
        PublishSnapshot(entityId, existing);
        return;
      }

      var cooling = Mathf.Clamp01(existing.Intensity - 0.04f);
      var next = cooling > 0.02f
        ? new FireSimulationSnapshot(
          true,
          cooling,
          cooling,
          Mathf.Clamp01(existing.EmberPressure * 0.82f),
          Mathf.Clamp01(Mathf.Max(existing.Smoke, cooling * 0.5f)),
          existing.IgnitionProgress,
          Mathf.Clamp01(existing.FuelConsumed + 0.02f),
          existing.MoistureDampening,
          existing.OxygenAvailability,
          existing.DominantSource)
        : FireSimulationRules.CreateColdSnapshot("Cooling");

      PublishSnapshot(entityId, next);
    }

    internal bool DebugForceExtinguish() {
      var entityId = GameObject.GetInstanceID();
      var hadActiveFire = _fireSimulationRuntimeState.TryGetSnapshot(entityId, out var snapshot)
                          && (snapshot.Burning || snapshot.Intensity > 0f);
      _fireSimulationRuntimeState.SetSnapshot(entityId, FireSimulationRules.CreateColdSnapshot("DebugExtinguish"));
      _wasBurning = false;
      return hadActiveFire;
    }

    internal void DebugResetFireSimulationState() {
      _fireSimulationRuntimeState.RemoveSnapshot(GameObject.GetInstanceID());
      _wasBurning = false;
    }

    private void PublishSnapshot(int entityId, FireSimulationSnapshot snapshot) {
      _fireSimulationRuntimeState.SetSnapshot(entityId, snapshot);
      if (snapshot.Burning && !_wasBurning) {
        FireTelemetry.Log($"event={FireTelemetryEvents.Ignited} entity={GameObject.name} id={entityId} source={snapshot.DominantSource}");
      } else if (!snapshot.Burning && _wasBurning) {
        FireTelemetry.Log($"event={FireTelemetryEvents.Extinguished} entity={GameObject.name} id={entityId}");
      }

      if (snapshot.Burning) {
        FireTelemetry.Log($"event={FireTelemetryEvents.BurningTick} entity={GameObject.name} id={entityId} intensity={snapshot.Intensity:0.000} heat={snapshot.HeatExposure:0.000} ember={snapshot.EmberPressure:0.000} smoke={snapshot.Smoke:0.000}");
      }

      _wasBurning = snapshot.Burning;
    }

  }
}
