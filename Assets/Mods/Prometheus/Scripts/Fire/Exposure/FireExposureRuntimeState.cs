using System.Collections.Generic;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal static class FireTelemetryEvents {

    public const string ModLoaded = "mod_loaded";
    public const string DebugIgnitionQueued = "debug_ignition_queued";
    public const string DebugIgnitionConsumed = "debug_ignition_consumed";
    public const string DebugStopAllFires = "debug_stop_all_fires";
    public const string DebugStopAllFiresResult = "debug_stop_all_fires_result";
    public const string DebugResetFireExposure = "debug_reset_fire_exposure";
    public const string DebugClearBeaverFireEffectsResult = "debug_clear_beaver_fire_effects_result";
    public const string DebugClearBeaverFireEffects = "debug_clear_beaver_fire_effects";
    public const string DebugViewFocus = "debug_view_focus";
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
    public const string VisualPreviewApply = "visual_preview_apply";
    public const string VisualPreviewClear = "visual_preview_clear";
    public const string VisualTuningJson = "visual_tuning_json";
    public const string NativeVisualEffectResolved = "native_visual_effect_resolved";
    public const string NativeVisualEffectUnavailable = "native_visual_effect_unavailable";
    public const string GridIgnitionSeeded = "grid_ignition_seeded";
    public const string GridSourceInjected = "grid_source_injected";
    public const string GridBurstInjected = "grid_burst_injected";

    public static readonly string[] All = {
      ModLoaded,
      DebugIgnitionQueued,
      DebugIgnitionConsumed,
      DebugStopAllFires,
      DebugStopAllFiresResult,
      DebugResetFireExposure,
      DebugClearBeaverFireEffectsResult,
      DebugClearBeaverFireEffects,
      DebugViewFocus,
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
      VisualPreviewApply,
      VisualPreviewClear,
      VisualTuningJson,
      NativeVisualEffectResolved,
      NativeVisualEffectUnavailable,
      GridIgnitionSeeded,
      GridSourceInjected,
      GridBurstInjected,
    };

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
    private float _debugIgnitionBlockSecondsRemaining;

    public int PendingForcedIgnitionCount => _forcedIgnitionEntityIds.Count;

    public bool DebugIgnitionsBlocked => _debugIgnitionBlockSecondsRemaining > 0f;

    public void RequestForcedIgnition(int entityId) {
      if (entityId == 0 || DebugIgnitionsBlocked) {
        return;
      }

      _forcedIgnitionEntityIds.Add(entityId);
      FireTelemetry.Log($"event={FireTelemetryEvents.DebugIgnitionQueued} id={entityId}");
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
      foreach (var entry in SnapshotEntries) {
        if (!entry.Value.Burning && entry.Value.Intensity <= 0f) {
          continue;
        }

        SetSnapshot(entry.Key, FireExposureRules.CreateColdSnapshot("DebugStopAllFires"));
        extinguishedCount++;
      }

      return extinguishedCount;
    }

    public void ClearSnapshotsAndIgnitionRequests() {
      ClearSnapshots();
      _forcedIgnitionEntityIds.Clear();
      _debugIgnitionBlockSecondsRemaining = 0f;
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

  internal static class FireExposureRules {

    internal static FireExposureSnapshot CreateColdSnapshot(string source = "None") =>
      new(false, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, source);

    internal static FireExposureSnapshot CreateTerminalDeadBuildingSnapshot() =>
      new(false, 0f, 0f, 0f, 0.15f, 0f, 1f, 0f, 1f, "DeadBuilding");

  }
}
