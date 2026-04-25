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
      CurrentTuning = CurrentTuning.With(emberScale: scale);
    }

    public void SetSmokeScale(float scale) {
      CurrentTuning = CurrentTuning.With(smokeScale: scale);
    }

    public void SetFireScale(float scale) {
      CurrentTuning = CurrentTuning.With(fireScale: scale);
    }

    public void SetSteamScale(float scale) {
      CurrentTuning = CurrentTuning.With(steamScale: scale);
    }

    public void SetCharScale(float scale) {
      CurrentTuning = CurrentTuning.With(charScale: scale);
    }

    public void SetHeightOffset(float offset) {
      CurrentTuning = CurrentTuning.With(heightOffset: offset);
    }

    public void SetDepthOffset(float offset) {
      CurrentTuning = CurrentTuning.With(depthOffset: offset);
    }

    public void SetEffectSize(float size) {
      CurrentTuning = CurrentTuning.With(effectSize: size);
    }

    public void SetEmberSpread(float spread) {
      CurrentTuning = CurrentTuning.With(emberSpread: spread);
    }

  }
}
