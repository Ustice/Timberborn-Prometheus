namespace Mods.Prometheus.Scripts {
  internal sealed class FireGridSimulationCoordinator {

    private readonly FireGridRuntimeState _fireGridRuntimeState;
    private int _lastSteppedFrame = -1;

    public FireGridSimulationCoordinator(FireGridRuntimeState fireGridRuntimeState) {
      _fireGridRuntimeState = fireGridRuntimeState;
    }

    public bool StepFrame(int frame) {
      if (_lastSteppedFrame == frame) {
        return false;
      }

      _lastSteppedFrame = frame;
      _fireGridRuntimeState.Step(FireGridKernel.Full27);
      return true;
    }

  }
}
