namespace Mods.Prometheus.Scripts {
  internal class FireVisualEffectRuntimeState {

    public FireVisualEffectTuning CurrentTuning { get; private set; } = FireVisualEffectTuning.Default;
    public bool TextMarkersEnabled { get; private set; }
    public float TextMarkerScale { get; private set; } = 1f;

    public void ResetDefaults() {
      CurrentTuning = FireVisualEffectTuning.Default;
      TextMarkersEnabled = false;
      TextMarkerScale = 1f;
    }

    public void SetTextMarkersEnabled(bool enabled) {
      TextMarkersEnabled = enabled;
    }

    public void SetTextMarkerScale(float scale) {
      TextMarkerScale = UnityEngine.Mathf.Clamp(scale, 0f, 3f);
    }

    public void SetEmberScale(float scale) {
      CurrentTuning = new FireVisualEffectTuning(
        scale,
        CurrentTuning.SmokeScale,
        CurrentTuning.FireScale,
        CurrentTuning.SteamScale,
        CurrentTuning.CharScale);
    }

    public void SetSmokeScale(float scale) {
      CurrentTuning = new FireVisualEffectTuning(
        CurrentTuning.EmberScale,
        scale,
        CurrentTuning.FireScale,
        CurrentTuning.SteamScale,
        CurrentTuning.CharScale);
    }

    public void SetFireScale(float scale) {
      CurrentTuning = new FireVisualEffectTuning(
        CurrentTuning.EmberScale,
        CurrentTuning.SmokeScale,
        scale,
        CurrentTuning.SteamScale,
        CurrentTuning.CharScale);
    }

    public void SetSteamScale(float scale) {
      CurrentTuning = new FireVisualEffectTuning(
        CurrentTuning.EmberScale,
        CurrentTuning.SmokeScale,
        CurrentTuning.FireScale,
        scale,
        CurrentTuning.CharScale);
    }

    public void SetCharScale(float scale) {
      CurrentTuning = new FireVisualEffectTuning(
        CurrentTuning.EmberScale,
        CurrentTuning.SmokeScale,
        CurrentTuning.FireScale,
        CurrentTuning.SteamScale,
        scale);
    }

  }
}
