namespace Mods.Prometheus.Scripts {
  internal static class TickGate {

    internal static bool ShouldRun(ref float timeSinceLastUpdate, float intervalInSeconds) {
      timeSinceLastUpdate += UnityEngine.Time.deltaTime;
      if (timeSinceLastUpdate < intervalInSeconds) {
        return false;
      }

      timeSinceLastUpdate = 0f;
      return true;
    }

  }
}
