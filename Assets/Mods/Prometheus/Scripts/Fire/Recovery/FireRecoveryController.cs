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
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FertileAshRecoveredGoodStackSpawner _fertileAshRecoveredGoodStackSpawner;
    private FireBurnedGroundAshDepositRuntimeState _burnedGroundAshDepositRuntimeState;
#if !PROMETHEUS_TESTS
    private FireBurnedGroundAshDepositMarkerSpawner _burnedGroundAshDepositMarkerSpawner;
#endif
    private PrometheusWorldLoadState _prometheusWorldLoadState;

    private float _timeSinceLastUpdate;
    private bool _sawBurnPhase;
    private float _peakIntensityDuringBurn;
    private float _recoveryHoursRemaining;
    private bool _fertileAshSpawnAttempted;

    [Inject]
    public void InjectDependencies(
      FireExposureRuntimeState fireExposureRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState,
      FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FertileAshRecoveredGoodStackSpawner fertileAshRecoveredGoodStackSpawner,
      FireBurnedGroundAshDepositRuntimeState burnedGroundAshDepositRuntimeState,
#if !PROMETHEUS_TESTS
      FireBurnedGroundAshDepositMarkerSpawner burnedGroundAshDepositMarkerSpawner,
#endif
      PrometheusWorldLoadState prometheusWorldLoadState) {
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fertileAshRecoveredGoodStackSpawner = fertileAshRecoveredGoodStackSpawner;
      _burnedGroundAshDepositRuntimeState = burnedGroundAshDepositRuntimeState;
#if !PROMETHEUS_TESTS
      _burnedGroundAshDepositMarkerSpawner = burnedGroundAshDepositMarkerSpawner;
#endif
      _prometheusWorldLoadState = prometheusWorldLoadState;
    }

    public void Update() {
      if (_prometheusWorldLoadState?.WorldReady != true) {
        return;
      }

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      if (!_fireExposureRuntimeState.TryGetSnapshot(entityId, out var exposureSnapshot)) {
        return;
      }

      _fireRuntimeProjectionRuntimeState.SetExposure(entityId, exposureSnapshot);
      TryQueueFertileAshFromAftermath(entityId, exposureSnapshot);
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
      _fertileAshSpawnAttempted = false;
      _fireRecoveryRuntimeState.RemoveSnapshot(GameObject.GetInstanceID());
      _fireRuntimeProjectionRuntimeState.SetRecovery(GameObject.GetInstanceID(), FireRuntimeProjectionRules.DefaultRecovery);
    }

    private void TryQueueFertileAshFromAftermath(int entityId, FireExposureSnapshot exposureSnapshot) {
      if (_fertileAshSpawnAttempted || !IsBurnedOut(exposureSnapshot)) {
        return;
      }

      if (!_fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageState)
          || damageState.State != FireDamageState.Dead) {
        return;
      }

      _fertileAshSpawnAttempted = true;
      var structureKind = GetStructureKind(damageState.Category);
      var eligibility = FireAftermathEligibilityPolicy.Evaluate(
        new FireAftermathEligibilityCandidate(
          structureKind,
          damageState.Category,
          damageState.State,
          burnedOut: true));
      var decision = FertileAshSpawnPolicy.Evaluate(eligibility);
      if (!decision.ShouldQueue) {
        FireTelemetry.Log(
          $"event={FireTelemetryEvents.FertileAshSpawnSkipped} entity={GameObject.name} id={entityId} reason={decision.Reason} status={eligibility.Status.ToString().ToLowerInvariant()} sourceKind={eligibility.SourceKind.ToString().ToLowerInvariant()} structure={structureKind.ToString().ToLowerInvariant()} damageCategory={damageState.Category.ToString().ToLowerInvariant()} cropContext={GetCropContext(damageState.Category)}");
        return;
      }

      var coordinates = GetRecoveredGoodStackCoordinates();
      if (_fertileAshRecoveredGoodStackSpawner is null) {
        FireTelemetry.LogWarning(
          $"event={FireTelemetryEvents.FertileAshSpawnFailed} entity={GameObject.name} id={entityId} amount={decision.Amount} reason=fertile_ash_spawner_missing sourceKind={eligibility.SourceKind.ToString().ToLowerInvariant()} damageCategory={damageState.Category.ToString().ToLowerInvariant()} cropContext={GetCropContext(damageState.Category)} coordinates={coordinates.x},{coordinates.y},{coordinates.z}");
        return;
      }

      var telemetryContext = new FertileAshSpawnTelemetryContext(
        exposureSnapshot.DominantSource,
        eligibility.SourceKind.ToString().ToLowerInvariant(),
        damageState.Category.ToString().ToLowerInvariant(),
        entityId,
        GetCropContext(damageState.Category));
      RecordBurnedGroundAshDeposit(coordinates, decision.Amount, telemetryContext);

      if (FertileAshSpawnPolicy.ShouldUseRemnantHarvest(eligibility.SourceKind)) {
        FireTelemetry.Log(
          $"event={FireTelemetryEvents.FertileAshSpawnQueued} entity={GameObject.name} id={entityId} amount={decision.Amount} reason={FertileAshSpawnPolicy.CharredTreeRemnantHarvestReason} source={telemetryContext.SourceAttribution} sourceKind={telemetryContext.SourceKind} damageCategory={telemetryContext.DamageCategory} cropContext={telemetryContext.CropContext} coordinates={coordinates.x},{coordinates.y},{coordinates.z}");
        return;
      }

      if (_fertileAshRecoveredGoodStackSpawner.TryQueueFertileAsh(coordinates, decision.Amount, telemetryContext, out var queueReason)) {
        FireTelemetry.Log(
          $"event={FireTelemetryEvents.FertileAshSpawnQueued} entity={GameObject.name} id={entityId} amount={decision.Amount} reason={decision.Reason} source={telemetryContext.SourceAttribution} sourceKind={telemetryContext.SourceKind} damageCategory={telemetryContext.DamageCategory} cropContext={telemetryContext.CropContext} coordinates={coordinates.x},{coordinates.y},{coordinates.z}");
        return;
      }

      FireTelemetry.LogWarning(
        $"event={FireTelemetryEvents.FertileAshSpawnFailed} entity={GameObject.name} id={entityId} amount={decision.Amount} reason={queueReason} source={telemetryContext.SourceAttribution} sourceKind={telemetryContext.SourceKind} damageCategory={telemetryContext.DamageCategory} cropContext={telemetryContext.CropContext} coordinates={coordinates.x},{coordinates.y},{coordinates.z}");
    }

    private FireGridStructureKind GetStructureKind(FireDamageCategory damageCategory) {
      var fireProfile = GetComponent<FireProfile>();
      if (fireProfile is not null) {
        var profileSample = FireGridEnvironmentSampler.FromProfile(
          fireProfile.StructureKind,
          fireProfile.Fuel,
          fireProfile.MoistureResistance,
          fireProfile.BarrierResistance);
        if (profileSample.StructureKind != FireGridStructureKind.Unknown) {
          return profileSample.StructureKind;
        }
      }

      return damageCategory switch {
        FireDamageCategory.Tree => FireGridStructureKind.Vegetation,
        FireDamageCategory.Crop => FireGridStructureKind.Vegetation,
        FireDamageCategory.Building => FireGridStructureKind.Building,
        _ => FireGridStructureKind.Unknown,
      };
    }

    private static string GetCropContext(FireDamageCategory damageCategory) =>
      damageCategory == FireDamageCategory.Crop ? "burned_crop" : "none";

    private Vector3Int GetRecoveredGoodStackCoordinates() {
      var position = GameObject.transform.position;
      return new Vector3Int(
        Mathf.RoundToInt(position.x),
        Mathf.RoundToInt(position.y),
        Mathf.RoundToInt(position.z));
    }

    private void RecordBurnedGroundAshDeposit(
      Vector3Int coordinates,
      int amount,
      FertileAshSpawnTelemetryContext telemetryContext) {
      if (_burnedGroundAshDepositRuntimeState is null
          || !_burnedGroundAshDepositRuntimeState.TryRecordDeposit(coordinates, telemetryContext.SourceEntityId, amount, telemetryContext, out var deposit)) {
        return;
      }

      FireTelemetry.Log(
        $"event={FireTelemetryEvents.BurnedGroundAshDepositCreated} sourceEntityId={deposit.SourceEntityId} amount={deposit.Amount} sourceKind={deposit.SourceKind} damageCategory={deposit.DamageCategory} cropContext={deposit.CropContext} coordinates={deposit.Coordinates.x},{deposit.Coordinates.y},{deposit.Coordinates.z}");
#if !PROMETHEUS_TESTS
      _burnedGroundAshDepositMarkerSpawner?.TryCreateMarker(deposit);
#endif
    }

    private static bool IsBurnedOut(FireExposureSnapshot exposureSnapshot) =>
      !exposureSnapshot.Burning
      && exposureSnapshot.FuelConsumed >= 0.999f
      && (exposureSnapshot.DominantSource == "BurnedOut" || exposureSnapshot.DominantSource == "DeadBuilding");

  }
}
