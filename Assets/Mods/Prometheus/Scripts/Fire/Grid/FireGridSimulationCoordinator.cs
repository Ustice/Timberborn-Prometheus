using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal sealed class FireGridSimulationCoordinator {

#if !PROMETHEUS_TESTS
    private const float RuntimeTelemetryIntervalInSeconds = 1f;
#endif

    private readonly FireGridRuntimeState _fireGridRuntimeState;
    private int _lastSteppedFrame = -1;
#if !PROMETHEUS_TESTS
    private float _lastRuntimeTelemetryTime = -999f;
#endif

    public FireGridSimulationCoordinator(FireGridRuntimeState fireGridRuntimeState) {
      _fireGridRuntimeState = fireGridRuntimeState;
    }

    public bool StepFrame(int frame) {
      if (_lastSteppedFrame == frame) {
        return false;
      }

      _lastSteppedFrame = frame;
      _fireGridRuntimeState.Step(FireGridKernel.Full27);
      LogRuntimeState();
      return true;
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

  }
}
