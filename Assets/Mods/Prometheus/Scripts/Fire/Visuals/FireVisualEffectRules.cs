using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireVisualEffectTuning {

    public static FireVisualEffectTuning Default { get; } = new(
      1.0f,
      1.0f,
      1.0f,
      1.0f,
      1.0f,
      0f,
      0f,
      1.0f,
      1.25f);

    public float EmberScale { get; }
    public float SmokeScale { get; }
    public float FireScale { get; }
    public float SteamScale { get; }
    public float CharScale { get; }
    public float HeightOffset { get; }
    public float DepthOffset { get; }
    public float EffectSize { get; }
    public float EmberSpread { get; }

    public FireVisualEffectTuning(
      float emberScale,
      float smokeScale,
      float fireScale,
      float steamScale,
      float charScale,
      float heightOffset,
      float depthOffset,
      float effectSize,
      float emberSpread) {
      EmberScale = Mathf.Max(0f, emberScale);
      SmokeScale = Mathf.Max(0f, smokeScale);
      FireScale = Mathf.Max(0f, fireScale);
      SteamScale = Mathf.Max(0f, steamScale);
      CharScale = Mathf.Max(0f, charScale);
      HeightOffset = Mathf.Clamp(heightOffset, -2f, 4f);
      DepthOffset = Mathf.Clamp(depthOffset, -3f, 3f);
      EffectSize = Mathf.Clamp(effectSize, 0.25f, 4f);
      EmberSpread = Mathf.Clamp(emberSpread, 0f, 4f);
    }

    public FireVisualEffectTuning With(
      float? emberScale = null,
      float? smokeScale = null,
      float? fireScale = null,
      float? steamScale = null,
      float? charScale = null,
      float? heightOffset = null,
      float? depthOffset = null,
      float? effectSize = null,
      float? emberSpread = null) {
      return new FireVisualEffectTuning(
        emberScale ?? EmberScale,
        smokeScale ?? SmokeScale,
        fireScale ?? FireScale,
        steamScale ?? SteamScale,
        charScale ?? CharScale,
        heightOffset ?? HeightOffset,
        depthOffset ?? DepthOffset,
        effectSize ?? EffectSize,
        emberSpread ?? EmberSpread);
    }

  }

  internal readonly struct FireVisualEffectIntensity {

    public float Embers { get; }
    public float Smoke { get; }
    public float Fire { get; }
    public float Steam { get; }
    public float Char { get; }
    public float Desiccation { get; }
    public bool HasAnyVisibleEffect => Embers > 0f || Smoke > 0f || Fire > 0f || Steam > 0f || Char > 0f || Desiccation > 0f;

    public FireVisualEffectIntensity(
      float embers,
      float smoke,
      float fire,
      float steam,
      float charAmount,
      float desiccation) {
      Embers = Mathf.Clamp01(embers);
      Smoke = Mathf.Clamp01(smoke);
      Fire = Mathf.Clamp01(fire);
      Steam = Mathf.Clamp01(steam);
      Char = Mathf.Clamp01(charAmount);
      Desiccation = Mathf.Clamp01(desiccation);
    }

  }

  internal static class FireVisualEffectRules {

    internal static FireVisualEffectIntensity ComputeIntensity(
      FireRuntimeProjectionSnapshot projection,
      FireVisualEffectTuning tuning) {
      return ComputeIntensity(projection.VisualExposure, projection.VisualDamageState, tuning);
    }

    internal static FireVisualEffectIntensity ComputeIntensity(
      FireExposureSnapshot exposure,
      FireDamageStateSnapshot damageState,
      FireVisualEffectTuning tuning) {
      var heatPressure = Mathf.Clamp01(Mathf.Max(
        exposure.Intensity,
        exposure.EmberPressure,
        exposure.HeatExposure));
      var moistureDampening = Mathf.Clamp01(exposure.MoistureDampening);
      var severity = Mathf.Clamp01(damageState.Severity);
      var fuelConsumed = Mathf.Clamp01(exposure.FuelConsumed);
      var desiccation = 1f - Mathf.Clamp01(exposure.MoistureDampening);

      // Local object fire progression is smoke/fire/smoke+ash. Sparks belong to the separate ember-field spread visual.
      var embers = 0f;
      var smokeBase = damageState.State switch {
        FireDamageState.Scorched => Mathf.Max(0.25f, severity),
        FireDamageState.Burning => Mathf.Max(0.45f, Mathf.Max(exposure.Intensity, exposure.Smoke)),
        FireDamageState.Dead => 0.15f,
        _ when fuelConsumed >= 0.95f => 0.12f,
        _ => 0f,
      };
      var smoke = Mathf.Clamp01(smokeBase * (1f - (moistureDampening * 0.25f)) * tuning.SmokeScale);
      var fire = exposure.Burning && damageState.State != FireDamageState.Dead
        ? Mathf.Clamp01(Mathf.Max(0.15f, exposure.Intensity) * (1f - (moistureDampening * 0.35f)) * tuning.FireScale)
        : 0f;
      var steam = Mathf.Clamp01(heatPressure * moistureDampening * tuning.SteamScale);
      var charAmount = damageState.State switch {
        FireDamageState.Scorched => Mathf.Clamp01(severity * 0.35f * tuning.CharScale),
        FireDamageState.Burning => Mathf.Clamp01((0.2f + (severity * 0.45f)) * tuning.CharScale),
        FireDamageState.Dead => Mathf.Clamp01(Mathf.Max(0.75f, severity) * tuning.CharScale),
        _ => Mathf.Clamp01(fuelConsumed * tuning.CharScale),
      };

      return new FireVisualEffectIntensity(embers, smoke, fire, steam, charAmount, desiccation);
    }

  }
}
