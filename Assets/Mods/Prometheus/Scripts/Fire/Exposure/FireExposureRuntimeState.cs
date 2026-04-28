using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal static class FireTelemetryEvents {

    public const string ModLoaded = "mod_loaded";
    public const string DebugIgnitionQueued = "debug_ignition_queued";
    public const string DebugIgnitionConsumed = "debug_ignition_consumed";
    public const string IgniteSelectedQueued = "ignite_selected_queued";
    public const string IgniteSelectedRejected = "ignite_selected_rejected";
    public const string DebugStopAllFires = "debug_stop_all_fires";
    public const string DebugStopAllFiresResult = "debug_stop_all_fires_result";
    public const string FireSuppressionAreaQueued = "fire_suppression_area_queued";
    public const string FireSuppressionAreaApplied = "fire_suppression_area_applied";
    public const string FireSuppressionAreaExpired = "fire_suppression_area_expired";
    public const string DebugResetFireExposure = "debug_reset_fire_exposure";
    public const string RuntimeResetRegistryStarted = "runtime_reset_registry_started";
    public const string RuntimeResetRegistryCompleted = "runtime_reset_registry_completed";
    public const string RuntimeResetHookFailed = "runtime_reset_hook_failed";
    public const string DebugClearBeaverFireEffectsResult = "debug_clear_beaver_fire_effects_result";
    public const string DebugClearBeaverFireEffects = "debug_clear_beaver_fire_effects";
    public const string DebugViewFocus = "debug_view_focus";
    public const string QaCommandResult = "qa_command_result";
    public const string EntityDestroyCleanup = "entity_destroy_cleanup";
    public const string Ignited = "ignited";
    public const string Extinguished = "extinguished";
    public const string BurningTick = "burning_tick";
    public const string DamageStateChanged = "damage_state_changed";
    public const string DamageTickApplied = "damage_tick_applied";
    public const string DamageStateReset = "damage_state_reset";
    public const string WorkplaceIndoorExposure = "workplace_indoor_exposure";
    public const string WorkplaceSpeedApiResolved = "workplace_speed_api_resolved";
    public const string WorkplaceSpeedPenaltyState = "workplace_speed_penalty_state";
    public const string WorkplaceSupportDisabled = "workplace_support_disabled";
    public const string WorkplaceSupportRestored = "workplace_support_restored";
    public const string BuildingOperationsDisabled = "building_operations_disabled";
    public const string BuildingOperationsRestored = "building_operations_restored";
    public const string BeaverExposureApplied = "beaver_exposure_applied";
    public const string BeaverExposureCleared = "beaver_exposure_cleared";
    public const string BeaverEffectNeedManagerScan = "beaver_effect_need_manager_scan";
    public const string BeaverEffectApiMissing = "beaver_effect_api_missing";
    public const string BeaverEffectApiResolved = "beaver_effect_api_resolved";
    public const string RecoveryStarted = "recovery_started";
    public const string RecoveryExpired = "recovery_expired";
    public const string FieldAmendmentGrowthBuffApplied = "field_amendment_growth_buff_applied";
    public const string FieldAmendmentGrowthBuffRestored = "field_amendment_growth_buff_restored";
    public const string FertileAshFarmhouseAmendmentApplied = "fertile_ash_farmhouse_amendment_applied";
    public const string FertileAshFarmhouseAmendmentSkipped = "fertile_ash_farmhouse_amendment_skipped";
    public const string FertileAshSpawnQueued = "fertile_ash_spawn_queued";
    public const string FertileAshSpawnSkipped = "fertile_ash_spawn_skipped";
    public const string FertileAshSpawnFailed = "fertile_ash_spawn_failed";
    public const string FertileAshRecoveredGoodStackQueued = "fertile_ash_recovered_good_stack_queued";
    public const string FertileAshRecoveredGoodStackFailed = "fertile_ash_recovered_good_stack_failed";
    public const string FertileAshTreeRemnantYieldApplied = "fertile_ash_tree_remnant_yield_applied";
    public const string FertileAshTreeRemnantYieldFailed = "fertile_ash_tree_remnant_yield_failed";
    public const string BurnedGroundAshDepositCreated = "burned_ground_ash_deposit_created";
    public const string BurnedGroundAshDepositMarkerCreated = "burned_ground_ash_deposit_marker_created";
    public const string BurnedGroundAshDepositsReset = "burned_ground_ash_deposits_reset";
    public const string BurnedGroundAshDepositMarkersReset = "burned_ground_ash_deposit_markers_reset";
    public const string FertileAshResetState = "fertile_ash_reset_state";
    public const string VisualPreviewApply = "visual_preview_apply";
    public const string VisualPreviewClear = "visual_preview_clear";
    public const string VisualTuningJson = "visual_tuning_json";
    public const string VisualRuntimeIntensity = "visual_runtime_intensity";
    public const string NativeVisualEffectResolved = "native_visual_effect_resolved";
    public const string NativeVisualEffectUnavailable = "native_visual_effect_unavailable";
    public const string GridIgnitionSeeded = "grid_ignition_seeded";
    public const string GridRuntimeState = "grid_runtime_state";
    public const string GridSourceInjected = "grid_source_injected";
    public const string GridSourceSuppressed = "grid_source_suppressed";
    public const string GridBurstInjected = "grid_burst_injected";
    public const string TimberbornCompatibilitySummary = "timberborn_compatibility_summary";
    public const string TimberbornCompatibilityProbe = "timberborn_compatibility_probe";
    public const string WorldLoadStateChanged = "world_load_state_changed";

    public static readonly string[] All = {
      ModLoaded,
      DebugIgnitionQueued,
      DebugIgnitionConsumed,
      IgniteSelectedQueued,
      IgniteSelectedRejected,
      DebugStopAllFires,
      DebugStopAllFiresResult,
      FireSuppressionAreaQueued,
      FireSuppressionAreaApplied,
      FireSuppressionAreaExpired,
      DebugResetFireExposure,
      RuntimeResetRegistryStarted,
      RuntimeResetRegistryCompleted,
      RuntimeResetHookFailed,
      DebugClearBeaverFireEffectsResult,
      DebugClearBeaverFireEffects,
      DebugViewFocus,
      QaCommandResult,
      EntityDestroyCleanup,
      Ignited,
      Extinguished,
      BurningTick,
      DamageStateChanged,
      DamageTickApplied,
      DamageStateReset,
      WorkplaceIndoorExposure,
      WorkplaceSpeedApiResolved,
      WorkplaceSpeedPenaltyState,
      WorkplaceSupportDisabled,
      WorkplaceSupportRestored,
      BuildingOperationsDisabled,
      BuildingOperationsRestored,
      BeaverExposureApplied,
      BeaverExposureCleared,
      BeaverEffectNeedManagerScan,
      BeaverEffectApiMissing,
      BeaverEffectApiResolved,
      RecoveryStarted,
      RecoveryExpired,
      FieldAmendmentGrowthBuffApplied,
      FieldAmendmentGrowthBuffRestored,
      FertileAshFarmhouseAmendmentApplied,
      FertileAshFarmhouseAmendmentSkipped,
      FertileAshSpawnQueued,
      FertileAshSpawnSkipped,
      FertileAshSpawnFailed,
      FertileAshRecoveredGoodStackQueued,
      FertileAshRecoveredGoodStackFailed,
      FertileAshTreeRemnantYieldApplied,
      FertileAshTreeRemnantYieldFailed,
      BurnedGroundAshDepositCreated,
      BurnedGroundAshDepositMarkerCreated,
      BurnedGroundAshDepositsReset,
      BurnedGroundAshDepositMarkersReset,
      FertileAshResetState,
      VisualPreviewApply,
      VisualPreviewClear,
      VisualTuningJson,
      VisualRuntimeIntensity,
      NativeVisualEffectResolved,
      NativeVisualEffectUnavailable,
      GridIgnitionSeeded,
      GridRuntimeState,
      GridSourceInjected,
      GridSourceSuppressed,
      GridBurstInjected,
      TimberbornCompatibilitySummary,
      TimberbornCompatibilityProbe,
      WorldLoadStateChanged,
    };

  }

  internal readonly struct FireSuppressionZoneSnapshot {

    public FireGridCoordinate Center { get; }
    public int Radius { get; }
    public float Strength { get; }
    public float RemainingSeconds { get; }

    public FireSuppressionZoneSnapshot(
      FireGridCoordinate center,
      int radius,
      float strength,
      float remainingSeconds) {
      Center = center;
      Radius = radius < 0 ? 0 : radius;
      Strength = Mathf.Clamp01(strength);
      RemainingSeconds = Mathf.Max(0f, remainingSeconds);
    }

  }

  internal readonly struct FireExposureSnapshot {

    public bool Burning { get; }
    public float Intensity { get; }
    public float HeatExposure { get; }
    public float EmberPressure { get; }
    public float Smoke { get; }
    public float IgnitionProgress { get; }
    public float FuelConsumed { get; }
    public float MoistureDampening { get; }
    public float OxygenAvailability { get; }
    public string DominantSource { get; }

    public FireExposureSnapshot(
      bool burning,
      float intensity,
      float heatExposure,
      float emberPressure,
      float smoke,
      float ignitionProgress,
      float fuelConsumed,
      float moistureDampening,
      float oxygenAvailability,
      string dominantSource) {
      Burning = burning;
      Intensity = Mathf.Clamp01(intensity);
      HeatExposure = Mathf.Clamp01(heatExposure);
      EmberPressure = Mathf.Clamp01(emberPressure);
      Smoke = Mathf.Clamp01(smoke);
      IgnitionProgress = Mathf.Clamp01(ignitionProgress);
      FuelConsumed = Mathf.Clamp01(fuelConsumed);
      MoistureDampening = Mathf.Clamp01(moistureDampening);
      OxygenAvailability = Mathf.Clamp01(oxygenAvailability);
      DominantSource = string.IsNullOrWhiteSpace(dominantSource) ? "Grid" : dominantSource;
    }

  }

  internal class FireExposureRuntimeState : EntitySnapshotStore<FireExposureSnapshot> {

    private readonly HashSet<int> _forcedIgnitionEntityIds = new();
    private readonly List<FireSuppressionZoneState> _suppressionZones = new();
    private float _debugIgnitionBlockSecondsRemaining;

    public int PendingForcedIgnitionCount => _forcedIgnitionEntityIds.Count;

    public bool DebugIgnitionsBlocked => _debugIgnitionBlockSecondsRemaining > 0f;

    public int ActiveSuppressionZoneCount => _suppressionZones.Count;

    public bool RequestForcedIgnition(int entityId) {
      if (entityId == 0 || DebugIgnitionsBlocked) {
        return false;
      }

      _forcedIgnitionEntityIds.Add(entityId);
      FireTelemetry.Log($"event={FireTelemetryEvents.DebugIgnitionQueued} id={entityId}");
      return true;
    }

    public bool ConsumeForcedIgnitionRequest(int entityId) {
      if (entityId == 0) {
        return false;
      }

      var consumed = _forcedIgnitionEntityIds.Remove(entityId);
      if (consumed) {
        FireTelemetry.Log($"event={FireTelemetryEvents.DebugIgnitionConsumed} id={entityId}");
      }

      return consumed;
    }

    public int ExtinguishAllBurning() {
      var extinguishedCount = 0;
      foreach (var entry in SnapshotEntries.ToArray()) {
        if (!entry.Value.Burning && entry.Value.Intensity <= 0f) {
          continue;
        }

        SetSnapshot(entry.Key, FireExposureRules.CreateColdSnapshot("DebugStopAllFires"));
        extinguishedCount++;
      }

      return extinguishedCount;
    }

    public bool RequestSuppressionArea(
      FireGridCoordinate center,
      int radius,
      float strength,
      float durationSeconds,
      string source = "DebugSelection") {
      if (radius <= 0 || strength <= 0f || durationSeconds <= 0f) {
        return false;
      }

      var zone = new FireSuppressionZoneState(
        center,
        radius,
        Mathf.Clamp01(strength),
        Mathf.Max(0f, durationSeconds),
        string.IsNullOrWhiteSpace(source) ? "Unknown" : source);
      _suppressionZones.Add(zone);
      FireTelemetry.Log($"event={FireTelemetryEvents.FireSuppressionAreaQueued} source={zone.Source} center={zone.Center} radius={zone.Radius} strength={zone.Strength:0.000} durationSeconds={zone.RemainingSeconds:0.000} activeZones={_suppressionZones.Count}");
      return true;
    }

    public int TickSuppression(float deltaSeconds) {
      if (_suppressionZones.Count == 0) {
        return 0;
      }

      var expiredCount = 0;
      var safeDeltaSeconds = Mathf.Max(0f, deltaSeconds);
      for (var i = _suppressionZones.Count - 1; i >= 0; i--) {
        var zone = _suppressionZones[i];
        zone.RemainingSeconds = Mathf.Max(0f, zone.RemainingSeconds - safeDeltaSeconds);
        if (zone.RemainingSeconds > 0f) {
          continue;
        }

        _suppressionZones.RemoveAt(i);
        expiredCount++;
        FireTelemetry.Log($"event={FireTelemetryEvents.FireSuppressionAreaExpired} source={zone.Source} center={zone.Center} radius={zone.Radius} strength={zone.Strength:0.000} activeZones={_suppressionZones.Count}");
      }

      return expiredCount;
    }

    public int ClearSuppressionAreas() {
      var count = _suppressionZones.Count;
      _suppressionZones.Clear();
      return count;
    }

    public float GetSuppressionStrength(FireGridCoordinate coordinate) {
      var strength = 0f;
      for (var i = 0; i < _suppressionZones.Count; i++) {
        strength = Mathf.Max(strength, _suppressionZones[i].StrengthAt(coordinate));
      }

      return Mathf.Clamp01(strength);
    }

    public float GetSuppressionStrength(IEnumerable<FireGridCoordinate> coordinates) {
      var strength = 0f;
      foreach (var coordinate in coordinates) {
        strength = Mathf.Max(strength, GetSuppressionStrength(coordinate));
      }

      return Mathf.Clamp01(strength);
    }

    public FireSuppressionZoneSnapshot[] GetSuppressionZones() =>
      _suppressionZones
        .Select(zone => new FireSuppressionZoneSnapshot(zone.Center, zone.Radius, zone.Strength, zone.RemainingSeconds))
        .ToArray();

    public void ClearSnapshotsAndIgnitionRequests() {
      ClearSnapshots();
      _forcedIgnitionEntityIds.Clear();
      _debugIgnitionBlockSecondsRemaining = 0f;
      ClearSuppressionAreas();
    }

    public void BlockDebugIgnitionsForSeconds(float seconds) {
      _debugIgnitionBlockSecondsRemaining = Mathf.Max(_debugIgnitionBlockSecondsRemaining, seconds);
      _forcedIgnitionEntityIds.Clear();
    }

    public void TickIgnitionBlock(float deltaSeconds) {
      if (_debugIgnitionBlockSecondsRemaining <= 0f) {
        return;
      }

      _debugIgnitionBlockSecondsRemaining = Mathf.Max(0f, _debugIgnitionBlockSecondsRemaining - Mathf.Max(0f, deltaSeconds));
    }

  }

  internal static class FireSuppressionRules {

    private const float BaselineBurningHeatFloor = 0.65f;
    private const float BaselineBurningEmberFloor = 0.35f;
    private const float BaselineBurningSmokeFloor = 0.25f;

    public static FireGridSample ApplyToSample(FireGridSample sample, float suppressionStrength) {
      var strength = Mathf.Clamp01(suppressionStrength);
      if (strength <= 0f) {
        return sample;
      }

      var heatScale = Mathf.Lerp(1f, 0.24f, strength);
      var emberScale = Mathf.Lerp(1f, 0.18f, strength);
      var smokeScale = Mathf.Lerp(1f, 0.45f, strength);
      var ignitionScale = Mathf.Lerp(1f, 0.15f, strength);
      return new FireGridSample(
        sample.HasActivity,
        sample.Heat * heatScale,
        sample.EmberPressure * emberScale,
        sample.Smoke * smokeScale,
        sample.IgnitionProgress * ignitionScale,
        sample.FuelConsumed,
        Mathf.Max(sample.MoistureDampening, strength * 0.85f),
        sample.OxygenAvailability,
        sample.DominantBurnState,
        sample.SourceAttribution);
    }

    public static FireCellState ApplyToCell(FireCellState state, float suppressionStrength) {
      var strength = Mathf.Clamp01(suppressionStrength);
      if (strength <= 0f) {
        return state;
      }

      return state.With(
        heat: state.Heat * Mathf.Lerp(1f, 0.24f, strength),
        emberPressure: state.EmberPressure * Mathf.Lerp(1f, 0.18f, strength),
        smoke: state.Smoke * Mathf.Lerp(1f, 0.45f, strength),
        ignitionProgress: state.IgnitionProgress * Mathf.Lerp(1f, 0.15f, strength));
    }

    public static float FuelConsumptionMultiplier(float suppressionStrength) =>
      Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(suppressionStrength));

    public static float BurningHeatFloor(float suppressionStrength) =>
      Mathf.Lerp(BaselineBurningHeatFloor, 0.25f, Mathf.Clamp01(suppressionStrength));

    public static float BurningEmberFloor(float suppressionStrength) =>
      Mathf.Lerp(BaselineBurningEmberFloor, 0.12f, Mathf.Clamp01(suppressionStrength));

    public static float BurningSmokeFloor(float suppressionStrength) =>
      Mathf.Lerp(BaselineBurningSmokeFloor, 0.08f, Mathf.Clamp01(suppressionStrength));

  }

  internal sealed class FireSuppressionZoneState {

    public FireGridCoordinate Center { get; }
    public int Radius { get; }
    public float Strength { get; }
    public string Source { get; }
    public float RemainingSeconds { get; set; }

    public FireSuppressionZoneState(
      FireGridCoordinate center,
      int radius,
      float strength,
      float remainingSeconds,
      string source) {
      Center = center;
      Radius = radius < 0 ? 0 : radius;
      Strength = Mathf.Clamp01(strength);
      RemainingSeconds = Mathf.Max(0f, remainingSeconds);
      Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source;
    }

    public float StrengthAt(FireGridCoordinate coordinate) {
      var distance = Mathf.Max(
        Mathf.Abs(coordinate.X - Center.X),
        Mathf.Abs(coordinate.Y - Center.Y),
        Mathf.Abs(coordinate.Z - Center.Z));
      if (distance > Radius) {
        return 0f;
      }

      var edgeFalloff = Radius <= 0
        ? 1f
        : Mathf.Lerp(1f, 0.45f, distance / (float)Radius);
      return Mathf.Clamp01(Strength * edgeFalloff);
    }

  }

  internal static class FireExposureRules {

    internal static FireExposureSnapshot CreateColdSnapshot(string source = "None") =>
      new(false, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, source);

    internal static FireExposureSnapshot CreateBurnedOutSnapshot() =>
      new(false, 0f, 0f, 0f, 0.08f, 0f, 1f, 0f, 1f, "BurnedOut");

    internal static FireExposureSnapshot CreateTerminalDeadBuildingSnapshot() =>
      new(false, 0f, 0f, 0f, 0.15f, 0f, 1f, 0f, 1f, "DeadBuilding");

  }

  internal static class FireIgnitionRules {

    internal static float ComputeIgnitionProbability(
      float heat,
      float emberPressure,
      float oxygenAvailability,
      float fuelRemaining,
      float moistureRemaining,
      float ignitionThreshold,
      float tickSeconds) {
      if (fuelRemaining <= 0f || oxygenAvailability <= 0f || tickSeconds <= 0f) {
        return 0f;
      }

      var drynessFactor = 1f - UnityEngine.Mathf.Clamp01(moistureRemaining);
      var fieldStrength = UnityEngine.Mathf.Clamp01(((heat * 0.62f) + (emberPressure * 0.9f)) * UnityEngine.Mathf.Lerp(0.35f, 1f, drynessFactor));
      var threshold = UnityEngine.Mathf.Clamp(ignitionThreshold, 0.05f, 0.95f);
      if (fieldStrength <= threshold) {
        return 0f;
      }

      var excess = (fieldStrength - threshold) / (1f - threshold);
      var oxygenFactor = UnityEngine.Mathf.Clamp01(oxygenAvailability);
      var fuelFactor = UnityEngine.Mathf.Clamp01(fuelRemaining);
      var ignitionCurve = (excess * 0.65f) + (excess * excess * 0.35f);
      var perSecondProbability = UnityEngine.Mathf.Clamp01(ignitionCurve * oxygenFactor * fuelFactor);
      return UnityEngine.Mathf.Clamp01(1f - UnityEngine.Mathf.Pow(1f - perSecondProbability, tickSeconds));
    }

    internal static float Roll(int entityId, int tick) {
      unchecked {
        var value = (uint)entityId;
        value ^= (uint)tick * 0x9E3779B9u;
        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        value *= 0x846CA68Bu;
        value ^= value >> 16;
        return (value & 0x00FFFFFFu) / 16777216f;
      }
    }

  }
}
