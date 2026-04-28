using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal sealed class FireGridSimulationCoordinator {

#if !PROMETHEUS_TESTS
    private const float RuntimeTelemetryIntervalInSeconds = 1f;
#endif

    private readonly FireGridRuntimeState _fireGridRuntimeState;
    private readonly FireExposureRuntimeState _fireExposureRuntimeState;
    private int _lastSteppedFrame = -1;
#if !PROMETHEUS_TESTS
    private float _lastRuntimeTelemetryTime = -999f;
    private float _lastSuppressionTelemetryTime = -999f;
#endif

    public FireGridSimulationCoordinator(
      FireGridRuntimeState fireGridRuntimeState,
      FireExposureRuntimeState fireExposureRuntimeState) {
      _fireGridRuntimeState = fireGridRuntimeState;
      _fireExposureRuntimeState = fireExposureRuntimeState;
    }

    public bool StepFrame(int frame) {
      if (_lastSteppedFrame == frame) {
        return false;
      }

      _lastSteppedFrame = frame;
#if PROMETHEUS_TESTS
      _fireExposureRuntimeState.TickSuppression(0f);
#else
      _fireExposureRuntimeState.TickSuppression(Time.deltaTime);
#endif
      _fireGridRuntimeState.Step(FireGridKernel.Full27);
      ApplySuppressionAreas();
      LogRuntimeState();
      return true;
    }

    private void ApplySuppressionAreas() {
      var zones = _fireExposureRuntimeState.GetSuppressionZones();
      if (zones.Length == 0) {
        return;
      }

      var dampedCells = 0;
      for (var i = 0; i < zones.Length; i++) {
        dampedCells += _fireGridRuntimeState.ApplySuppressionArea(zones[i]);
      }

      LogSuppressionApplied(zones.Length, dampedCells);
    }

    private void LogRuntimeState() {
#if PROMETHEUS_TESTS
      return;
#else
      if (_fireGridRuntimeState.ActiveCellCount <= 0) {
        return;
      }

      if (Time.realtimeSinceStartup - _lastRuntimeTelemetryTime < RuntimeTelemetryIntervalInSeconds) {
        return;
      }

      _lastRuntimeTelemetryTime = Time.realtimeSinceStartup;
      FireTelemetry.Log($"event={FireTelemetryEvents.GridRuntimeState} activeCells={_fireGridRuntimeState.ActiveCellCount} activeChunks={_fireGridRuntimeState.ActiveChunkCount} totalChunks={_fireGridRuntimeState.TotalChunkCount}");
#endif
    }

    private void LogSuppressionApplied(int zoneCount, int dampedCells) {
#if PROMETHEUS_TESTS
      return;
#else
      if (Time.realtimeSinceStartup - _lastSuppressionTelemetryTime < RuntimeTelemetryIntervalInSeconds) {
        return;
      }

      _lastSuppressionTelemetryTime = Time.realtimeSinceStartup;
      FireTelemetry.Log($"event={FireTelemetryEvents.FireSuppressionAreaApplied} activeZones={zoneCount} dampedCells={dampedCells}");
#endif
    }

  }
}
