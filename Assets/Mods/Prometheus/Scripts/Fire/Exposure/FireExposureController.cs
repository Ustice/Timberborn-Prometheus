using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireExposureController : BaseComponent,
                                            IAwakableComponent,
                                            IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 0.5f;

    private FireExposureRuntimeState _fireExposureRuntimeState;
    private FireGridRuntimeState _fireGridRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireProfile _fireProfile;
    private float _timeSinceLastUpdate;
    private bool _wasBurning;

    [Inject]
    public void InjectDependencies(
      FireExposureRuntimeState fireExposureRuntimeState,
      FireGridRuntimeState fireGridRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState) {
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _fireGridRuntimeState = fireGridRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
    }

    public void Awake() {
      _fireProfile = GetComponent<FireProfile>();
    }

    public void Update() {
      _fireExposureRuntimeState.TickIgnitionBlock(Time.deltaTime);
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      var footprint = GetGridFootprint();
      var coordinate = footprint.PrimaryCoordinate;
      SetEnvironment(footprint, CreateEnvironment());

      if (_fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageState)
          && damageState.State == FireDamageState.Dead) {
        _fireGridRuntimeState.ClearCell(coordinate);
        PublishSnapshot(entityId, FireExposureRules.CreateTerminalDeadBuildingSnapshot());
        return;
      }

      if (_fireExposureRuntimeState.ConsumeForcedIgnitionRequest(entityId)) {
        _fireGridRuntimeState.Inject(coordinate, CreateDebugIgnitionCell());
        FireTelemetry.Log($"event={FireTelemetryEvents.GridIgnitionSeeded} entity={GameObject.name} id={entityId}");
      }

      _fireGridRuntimeState.StepOncePerFrame(Time.frameCount, FireGridKernel.Full27);
      PublishSnapshot(entityId, CreateSnapshotFromGrid(footprint));
    }

    internal bool DebugForceExtinguish() {
      var entityId = GameObject.GetInstanceID();
      var hadActiveFire = _fireExposureRuntimeState.TryGetSnapshot(entityId, out var snapshot)
                          && (snapshot.Burning || snapshot.Intensity > 0f);
      _fireExposureRuntimeState.SetSnapshot(entityId, FireExposureRules.CreateColdSnapshot("DebugExtinguish"));
      _fireGridRuntimeState.ClearCell(GetGridCoordinate());
      _wasBurning = false;
      return hadActiveFire;
    }

    internal void DebugResetFireExposureState() {
      _fireExposureRuntimeState.RemoveSnapshot(GameObject.GetInstanceID());
      _fireGridRuntimeState.ClearCell(GetGridCoordinate());
      _wasBurning = false;
    }

    private FireExposureSnapshot CreateSnapshotFromGrid(FireGridFootprint footprint) {
      var sample = _fireGridRuntimeState.Sample(footprint);
      if (!sample.HasActivity) {
        return FireExposureRules.CreateColdSnapshot();
      }

      var intensity = Mathf.Clamp01(Mathf.Max(sample.Heat, sample.IgnitionProgress));
      return new FireExposureSnapshot(
        sample.Burning,
        intensity,
        sample.Heat,
        sample.EmberPressure,
        sample.Smoke,
        sample.IgnitionProgress,
        sample.FuelConsumed,
        sample.MoistureDampening,
        sample.OxygenAvailability,
        "Grid");
    }

    private void SetEnvironment(FireGridFootprint footprint, FireCellEnvironment environment) {
      for (var i = 0; i < footprint.Coordinates.Count; i++) {
        _fireGridRuntimeState.SetEnvironment(footprint.Coordinates[i], environment);
      }
    }

    private FireCellEnvironment CreateEnvironment() {
      var profileSample = _fireProfile == null
        ? new FireGridEnvironmentSample(FireGridStructureKind.Unknown, 1f, 0f, 0f, 1f, 0f, FireGridExposedFaces.All)
        : FireGridEnvironmentSampler.FromProfile(
          _fireProfile.StructureKind,
          _fireProfile.Fuel,
          _fireProfile.MoistureResistance,
          _fireProfile.BarrierResistance);
      var worldSample = FireGridEnvironmentSampler.CreateDefaultWorldSample();
      return FireGridEnvironmentSampler.Merge(profileSample, worldSample).ToEnvironment();
    }

    private FireGridFootprint GetGridFootprint() {
      if (TryGetRendererBounds(out var bounds)) {
        return FireGridFootprintSampler.FromBounds(bounds);
      }

      var position = GameObject.transform.position;
      return FireGridFootprintSampler.FromWorldPosition(position);
    }

    private FireGridCoordinate GetGridCoordinate() =>
      GetGridFootprint().PrimaryCoordinate;

    private bool TryGetRendererBounds(out Bounds bounds) {
      var renderers = GameObject.GetComponentsInChildren<Renderer>();
      var hasBounds = false;
      bounds = default;
      for (var i = 0; i < renderers.Length; i++) {
        var renderer = renderers[i];
        if (renderer == null || renderer is ParticleSystemRenderer) {
          continue;
        }

        if (!hasBounds) {
          bounds = renderer.bounds;
          hasBounds = true;
          continue;
        }

        bounds.Encapsulate(renderer.bounds);
      }

      return hasBounds;
    }

    private static FireCellState CreateDebugIgnitionCell() =>
      new(1f, 0.85f, 0.35f, 1f, 0f, FireGridBurnState.Burning);

    private void PublishSnapshot(int entityId, FireExposureSnapshot snapshot) {
      _fireExposureRuntimeState.SetSnapshot(entityId, snapshot);
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
