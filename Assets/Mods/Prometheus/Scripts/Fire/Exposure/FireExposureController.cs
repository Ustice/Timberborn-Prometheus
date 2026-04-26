using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireExposureController : BaseComponent,
                                            IAwakableComponent,
                                            IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 0.5f;
    private const float BaseFuelConsumptionPerTick = 0.006f;
    private const float BaseMoistureEvaporationPerTick = 0.025f;
    private const float BurningHeatFloor = 0.65f;
    private const float BurningEmberFloor = 0.35f;
    private const float BurningSmokeFloor = 0.25f;

    private FireExposureRuntimeState _fireExposureRuntimeState;
    private FireGridRuntimeState _fireGridRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireProfile _fireProfile;
    private FireResetRegistration _resetRegistration = FireResetRegistration.Empty;
    private float _remainingFuel = -1f;
    private float _remainingMoisture = -1f;
    private float _timeSinceLastUpdate;
    private float _lastBurningTelemetryTime = -999f;
    private float _lastBurningTelemetryIntensity = -1f;
    private float _lastBurningTelemetryHeat = -1f;
    private float _lastBurningTelemetryEmber = -1f;
    private float _lastBurningTelemetrySmoke = -1f;
    private float _lastBurningTelemetryFuel = -1f;
    private float _lastBurningTelemetryMoisture = -1f;
    private int _ignitionRollTick;
    private bool _isIgnited;
    private bool _isBurnedOut;
    private bool _wasBurning;
    private FireSourceAttribution _ignitionSourceAttribution = FireSourceAttribution.Unknown;

    [Inject]
    public void InjectDependencies(
      FireExposureRuntimeState fireExposureRuntimeState,
      FireGridRuntimeState fireGridRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireResetRegistry fireResetRegistry) {
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _fireGridRuntimeState = fireGridRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _resetRegistration = fireResetRegistry.RegisterEntity(
        GameObject.GetInstanceID(),
        FireResetHookKind.SourceState,
        nameof(FireExposureController),
        DebugResetFireExposureState);
    }

    public void Awake() {
      _fireProfile = GetComponent<FireProfile>();
      _remainingFuel = MaxFuel;
      _remainingMoisture = MaxMoisture;
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

      if (_isBurnedOut) {
        _fireGridRuntimeState.ClearCell(coordinate);
        PublishSnapshot(entityId, FireExposureRules.CreateBurnedOutSnapshot());
        return;
      }

      if (_fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageState)
          && damageState.State == FireDamageState.Dead
          && damageState.Category == FireDamageCategory.Building) {
        _fireGridRuntimeState.ClearCell(coordinate);
        _isIgnited = false;
        _isBurnedOut = true;
        _remainingFuel = 0f;
        PublishSnapshot(entityId, FireExposureRules.CreateTerminalDeadBuildingSnapshot());
        return;
      }

      if (_fireExposureRuntimeState.ConsumeForcedIgnitionRequest(entityId)) {
        TryIgnite(FireSourceAttribution.DebugIgnition(entityId.ToString()));
        FireTelemetry.Log($"event={FireTelemetryEvents.GridIgnitionSeeded} entity={GameObject.name} id={entityId}");
      }

      if (_isIgnited) {
        _fireGridRuntimeState.Inject(new FireGridSourceInjection(coordinate, CreateBurningSourceCell(), _ignitionSourceAttribution));
      }

      PublishSnapshot(entityId, CreateSnapshotFromGrid(entityId, footprint));
    }

    internal bool DebugForceExtinguish() {
      var entityId = GameObject.GetInstanceID();
      var hadActiveFire = _fireExposureRuntimeState.TryGetSnapshot(entityId, out var snapshot)
                          && (snapshot.Burning || snapshot.Intensity > 0f);
      _fireExposureRuntimeState.SetSnapshot(entityId, FireExposureRules.CreateColdSnapshot("DebugExtinguish"));
      _fireGridRuntimeState.ClearCell(GetGridCoordinate());
      _isIgnited = false;
      _wasBurning = false;
      _ignitionSourceAttribution = FireSourceAttribution.Unknown;
      return hadActiveFire;
    }

    internal void DebugResetFireExposureState() {
      _fireExposureRuntimeState.RemoveSnapshot(GameObject.GetInstanceID());
      _fireGridRuntimeState.ClearCell(GetGridCoordinate());
      _remainingFuel = MaxFuel;
      _remainingMoisture = MaxMoisture;
      _ignitionRollTick = 0;
      _isIgnited = false;
      _isBurnedOut = false;
      _wasBurning = false;
      _ignitionSourceAttribution = FireSourceAttribution.Unknown;
    }

    private void OnDestroy() {
      _resetRegistration.Dispose();
    }

    private FireExposureSnapshot CreateSnapshotFromGrid(int entityId, FireGridFootprint footprint) {
      var sample = _fireGridRuntimeState.Sample(footprint);
      if (!sample.HasActivity) {
        if (!_isIgnited) {
          return CreateColdSnapshotWithFuel();
        }
      }

      if (!_isIgnited && ShouldIgniteFromField(entityId, sample)) {
        TryIgnite(sample.SourceAttribution);
      }

      EvaporateMoisture(sample);

      if (_isIgnited) {
        ConsumeFuel(sample);
      }

      if (_isBurnedOut) {
        return FireExposureRules.CreateBurnedOutSnapshot();
      }

      var intensity = _isIgnited
        ? Mathf.Clamp01(Mathf.Max(BurningHeatFloor, sample.Heat, sample.IgnitionProgress))
        : Mathf.Clamp01(Mathf.Max(sample.Heat, sample.IgnitionProgress));
      var heat = _isIgnited ? Mathf.Max(BurningHeatFloor, sample.Heat) : sample.Heat;
      var ember = _isIgnited ? Mathf.Max(BurningEmberFloor, sample.EmberPressure) : sample.EmberPressure;
      var smoke = _isIgnited ? Mathf.Max(BurningSmokeFloor, sample.Smoke) : sample.Smoke;
      return new FireExposureSnapshot(
        _isIgnited,
        intensity,
        heat,
        ember,
        smoke,
        _isIgnited ? 1f : sample.IgnitionProgress,
        FuelConsumed,
        MoistureRemainingFraction,
        sample.OxygenAvailability,
        _isIgnited ? _ignitionSourceAttribution.ToTelemetryToken() : sample.SourceAttribution.ToTelemetryToken());
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

    private float MaxFuel => Mathf.Max(0.1f, _fireProfile == null ? 1f : _fireProfile.Fuel);

    private float MaxMoisture => Mathf.Max(0.1f, 0.25f + (_fireProfile == null ? 0f : _fireProfile.MoistureResistance));

    private float FuelConsumed => Mathf.Clamp01(1f - (_remainingFuel / MaxFuel));

    private float MoistureRemainingFraction => Mathf.Clamp01(_remainingMoisture / MaxMoisture);

    private FireExposureSnapshot CreateColdSnapshotWithFuel() =>
      new(false, 0f, 0f, 0f, 0f, 0f, FuelConsumed, MoistureRemainingFraction, 1f, "None");

    private bool TryIgnite(FireSourceAttribution sourceAttribution) {
      if (_isBurnedOut || _remainingFuel <= 0f) {
        return false;
      }

      _isIgnited = true;
      _ignitionSourceAttribution = sourceAttribution.HasSource ? sourceAttribution : FireSourceAttribution.Unknown;
      return true;
    }

    private bool ShouldIgniteFromField(int entityId, FireGridSample sample) {
      if (_remainingFuel <= 0f || sample.OxygenAvailability < OxygenDemand) {
        return false;
      }

      var probability = FireIgnitionRules.ComputeIgnitionProbability(
        sample.Heat,
        sample.EmberPressure,
        sample.OxygenAvailability,
        _remainingFuel / MaxFuel,
        MoistureRemainingFraction,
        IgnitionThreshold,
        UpdateIntervalInSeconds);
      if (probability <= 0f) {
        return false;
      }

      _ignitionRollTick++;
      return FireIgnitionRules.Roll(entityId, _ignitionRollTick) < probability;
    }

    private float IgnitionThreshold => _fireProfile == null ? 0.45f : _fireProfile.IgnitionThreshold;

    private float OxygenDemand => _fireProfile == null ? 0.35f : _fireProfile.OxygenDemand;

    private void EvaporateMoisture(FireGridSample sample) {
      if (_remainingMoisture <= 0f) {
        return;
      }

      var dryingPressure = Mathf.Clamp01(Mathf.Max(sample.Heat, sample.EmberPressure * 0.7f));
      _remainingMoisture = Mathf.Max(0f, _remainingMoisture - (BaseMoistureEvaporationPerTick * dryingPressure));
    }

    private void ConsumeFuel(FireGridSample sample) {
      var pressure = Mathf.Clamp01(Mathf.Max(BurningHeatFloor, sample.Heat, sample.EmberPressure));
      _remainingFuel = Mathf.Max(0f, _remainingFuel - (BaseFuelConsumptionPerTick * pressure));
      if (_remainingFuel > 0f) {
        return;
      }

      _isIgnited = false;
      _isBurnedOut = true;
      _ignitionSourceAttribution = FireSourceAttribution.Unknown;
      _fireGridRuntimeState.ClearCell(GetGridCoordinate());
    }

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

    private FireCellState CreateBurningSourceCell() {
      var fuelRemaining = Mathf.Clamp01(_remainingFuel / MaxFuel);
      return new(
        Mathf.Lerp(0.45f, 1f, fuelRemaining),
        Mathf.Lerp(0.2f, 0.85f, fuelRemaining),
        Mathf.Lerp(0.15f, 0.35f, fuelRemaining),
        1f,
        FuelConsumed,
        FireGridBurnState.Burning,
        _ignitionSourceAttribution);
    }

    private void PublishSnapshot(int entityId, FireExposureSnapshot snapshot) {
      _fireExposureRuntimeState.SetSnapshot(entityId, snapshot);
      if (snapshot.Burning && !_wasBurning) {
        FireTelemetry.Log($"event={FireTelemetryEvents.Ignited} entity={GameObject.name} id={entityId} source={snapshot.DominantSource}");
      } else if (!snapshot.Burning && _wasBurning) {
        FireTelemetry.Log($"event={FireTelemetryEvents.Extinguished} entity={GameObject.name} id={entityId}");
      }

      if (ShouldLogBurningTick(snapshot)) {
        FireTelemetry.Log($"event={FireTelemetryEvents.BurningTick} entity={GameObject.name} id={entityId} intensity={snapshot.Intensity:0.000} heat={snapshot.HeatExposure:0.000} ember={snapshot.EmberPressure:0.000} smoke={snapshot.Smoke:0.000} fuel={snapshot.FuelConsumed:0.000} moisture={snapshot.MoistureDampening:0.000}");
      }

      _wasBurning = snapshot.Burning;
    }

    private bool ShouldLogBurningTick(FireExposureSnapshot snapshot) {
      if (!snapshot.Burning) {
        return false;
      }

      var changedEnough = _lastBurningTelemetryFuel < 0f
                          || Mathf.Abs(snapshot.FuelConsumed - _lastBurningTelemetryFuel) >= 0.1f
                          || Mathf.Abs(snapshot.MoistureDampening - _lastBurningTelemetryMoisture) >= 0.1f
                          || Mathf.Abs(snapshot.Intensity - _lastBurningTelemetryIntensity) >= 0.15f;
      var elapsedEnough = Time.realtimeSinceStartup - _lastBurningTelemetryTime >= 2f;
      if (!changedEnough && !elapsedEnough) {
        return false;
      }

      _lastBurningTelemetryTime = Time.realtimeSinceStartup;
      _lastBurningTelemetryIntensity = snapshot.Intensity;
      _lastBurningTelemetryHeat = snapshot.HeatExposure;
      _lastBurningTelemetryEmber = snapshot.EmberPressure;
      _lastBurningTelemetrySmoke = snapshot.Smoke;
      _lastBurningTelemetryFuel = snapshot.FuelConsumed;
      _lastBurningTelemetryMoisture = snapshot.MoistureDampening;
      return true;
    }

  }
}
