using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Timberborn.Localization;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal enum FireVisualEffectKind {
    Smoke,
    Ash,
    Steam,
    Fire,
    Sparks,
    Char,
  }

  internal enum FireVisualSizeOverLifetimePreset {
    Constant,
    Grow,
    Shrink,
    Swell,
    Pop,
  }

  internal enum FireVisualShapeMode {
    Native,
    Sphere,
    Box,
    Cone,
  }

  internal sealed class FireVisualPreset {

    public const int Version = 1;

    public Dictionary<FireVisualEffectKind, FireParticleEffectTuning> ParticleEffects { get; } = new() {
      [FireVisualEffectKind.Smoke] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Smoke, "FoodFactorySmoke"),
      [FireVisualEffectKind.Ash] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Ash, "BadwaterRigSmoke"),
      [FireVisualEffectKind.Steam] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Steam, "CoffeeBrewerySmoke"),
      [FireVisualEffectKind.Fire] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Fire, "CampfireFire"),
      [FireVisualEffectKind.Sparks] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Sparks, "Sparks_Trail"),
    };

    public FireCharEffectTuning Char { get; } = FireCharEffectTuning.Default;

    public FireParticleEffectTuning GetParticle(FireVisualEffectKind kind) => ParticleEffects[kind];

    public void ResetDefaults() {
      ParticleEffects[FireVisualEffectKind.Smoke] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Smoke, "FoodFactorySmoke");
      ParticleEffects[FireVisualEffectKind.Ash] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Ash, "BadwaterRigSmoke");
      ParticleEffects[FireVisualEffectKind.Steam] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Steam, "CoffeeBrewerySmoke");
      ParticleEffects[FireVisualEffectKind.Fire] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Fire, "CampfireFire");
      ParticleEffects[FireVisualEffectKind.Sparks] = FireParticleEffectTuning.CreateDefault(FireVisualEffectKind.Sparks, "Sparks_Trail");
      Char.ResetDefaults();
    }

  }

  internal sealed class FireParticleEffectTuning {

    public FireVisualEffectKind Kind { get; }
    public bool Enabled { get; set; } = true;
    public string SourceName { get; set; }
    public float Intensity { get; set; } = 1f;
    public float Emission { get; set; } = 1f;
    public Vector3 Position { get; set; } = Vector3.zero;
    public Vector3 Velocity { get; set; } = Vector3.zero;
    public float Size { get; set; } = 1f;
    public float Lifetime { get; set; } = 1f;
    public float Speed { get; set; } = 1f;
    public float Alpha { get; set; } = 1f;
    public Color Color { get; set; } = Color.white;
    public float Spread { get; set; } = 1f;
    public float Gravity { get; set; }
    public float NoiseStrength { get; set; }
    public float RotationSpeed { get; set; }
    public int SortingOrder { get; set; } = 20;
    public FireVisualShapeMode ShapeMode { get; set; } = FireVisualShapeMode.Native;
    public FireVisualSizeOverLifetimePreset SizeOverLifetime { get; set; } = FireVisualSizeOverLifetimePreset.Constant;

    private FireParticleEffectTuning(FireVisualEffectKind kind, string sourceName) {
      Kind = kind;
      SourceName = sourceName;
    }

    public static FireParticleEffectTuning CreateDefault(FireVisualEffectKind kind, string sourceName) {
      var tuning = new FireParticleEffectTuning(kind, sourceName);
      switch (kind) {
        case FireVisualEffectKind.Smoke:
          tuning.Intensity = 1f;
          tuning.Emission = 1f;
          tuning.Position = new Vector3(0f, 0f, 0f);
          tuning.Velocity = Vector3.zero;
          tuning.Size = 1.4f;
          tuning.Lifetime = 2.3f;
          tuning.Speed = 0.85f;
          tuning.Alpha = 0.8f;
          tuning.Spread = 0f;
          tuning.Gravity = 0f;
          tuning.NoiseStrength = 0.25f;
          break;
        case FireVisualEffectKind.Ash:
          tuning.Intensity = 0.55f;
          tuning.Emission = 0.4f;
          tuning.Position = new Vector3(0f, 0.9f, 0f);
          tuning.Velocity = Vector3.zero;
          tuning.Size = 0.2f;
          tuning.Lifetime = 0.75f;
          tuning.Speed = 0.35f;
          tuning.Alpha = 1f;
          tuning.Spread = 0.25f;
          tuning.Gravity = 0.25f;
          break;
        case FireVisualEffectKind.Steam:
          tuning.Intensity = 1f;
          tuning.Emission = 1f;
          tuning.Position = new Vector3(0f, 0.35f, 0f);
          tuning.Velocity = new Vector3(0f, 0.7f, 0f);
          tuning.Lifetime = 1.0f;
          tuning.Speed = 0.7f;
          tuning.Size = 1.15f;
          tuning.Alpha = 0.6f;
          tuning.Spread = 0.8f;
          break;
        case FireVisualEffectKind.Fire:
          tuning.Intensity = 1f;
          tuning.Emission = 1f;
          tuning.Position = new Vector3(0.25f, 0f, 0.15f);
          tuning.Velocity = Vector3.zero;
          tuning.Lifetime = 1.2f;
          tuning.Speed = 0.9f;
          tuning.Size = 1.0f;
          tuning.Alpha = 1f;
          tuning.Spread = 0f;
          tuning.Gravity = -0.15f;
          tuning.SizeOverLifetime = FireVisualSizeOverLifetimePreset.Swell;
          break;
        case FireVisualEffectKind.Sparks:
          tuning.Intensity = 0.7f;
          tuning.Emission = 0.55f;
          tuning.Position = Vector3.zero;
          tuning.Velocity = Vector3.zero;
          tuning.Lifetime = 0.85f;
          tuning.Speed = 1.35f;
          tuning.Size = 0.7f;
          tuning.Alpha = 1f;
          tuning.Spread = 1.4f;
          tuning.Gravity = -0.25f;
          tuning.NoiseStrength = 0.4f;
          break;
      }

      return tuning;
    }

  }

  internal sealed class FireCharEffectTuning {

    public bool Enabled { get; set; } = true;
    public Color TintColor { get; set; } = new(0.02f, 0.018f, 0.015f, 1f);
    public float TintStrength { get; set; } = 0.85f;
    public float Darkening { get; set; } = 0.85f;
    public float CutAmount { get; set; } = 0.45f;
    public float NoiseScale { get; set; } = 1.5f;
    public float NoiseContrast { get; set; } = 1.0f;
    public float EdgeWidth { get; set; } = 0.15f;
    public float EdgeDepth { get; set; } = 0.35f;
    public float ActiveGlow { get; set; } = 0.6f;
    public float AshEdgeBrightness { get; set; } = 0.75f;
    public float BlackInteriorStrength { get; set; } = 1.0f;
    public float Seed { get; set; } = 0.37f;

    public static FireCharEffectTuning Default => new();

    public void ResetDefaults() {
      var defaults = Default;
      Enabled = defaults.Enabled;
      TintColor = defaults.TintColor;
      TintStrength = defaults.TintStrength;
      Darkening = defaults.Darkening;
      CutAmount = defaults.CutAmount;
      NoiseScale = defaults.NoiseScale;
      NoiseContrast = defaults.NoiseContrast;
      EdgeWidth = defaults.EdgeWidth;
      EdgeDepth = defaults.EdgeDepth;
      ActiveGlow = defaults.ActiveGlow;
      AshEdgeBrightness = defaults.AshEdgeBrightness;
      BlackInteriorStrength = defaults.BlackInteriorStrength;
      Seed = defaults.Seed;
    }

  }

  internal readonly struct FireVisualPreviewTarget {

    public static FireVisualPreviewTarget None { get; } = new(0, string.Empty, "No selected target", false);

    public int Id { get; }
    public string RawName { get; }
    public string Kind { get; }
    public bool Supported { get; }

    public FireVisualPreviewTarget(int id, string rawName, string kind, bool supported) {
      Id = id;
      RawName = rawName ?? string.Empty;
      Kind = string.IsNullOrWhiteSpace(kind) ? CleanObjectName(rawName) : kind;
      Supported = supported;
    }

    public static FireVisualPreviewTarget FromGameObject(GameObject gameObject, ILoc loc) {
      if (gameObject == null) {
        return None;
      }

      var supported = gameObject.GetComponentsInChildren<Renderer>(true).Any(renderer => renderer is not ParticleSystemRenderer)
                      || gameObject.transform != null;
      return new FireVisualPreviewTarget(
        gameObject.GetInstanceID(),
        gameObject.name,
        ResolveKind(gameObject, loc),
        supported);
    }

    private static string ResolveKind(GameObject gameObject, ILoc loc) {
      var localized = TryResolveLocalizedKind(gameObject, loc);
      return string.IsNullOrWhiteSpace(localized) ? CleanObjectName(gameObject.name) : localized;
    }

    private static string TryResolveLocalizedKind(GameObject gameObject, ILoc loc) {
      var components = gameObject.GetComponents<Component>();
      foreach (var component in components) {
        if (component == null) {
          continue;
        }

        var componentType = component.GetType();
        foreach (var propertyName in new[] { "DisplayNameLocKey", "NameLocKey", "DisplayLocKey", "LocKey", "Id" }) {
          var property = componentType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
          if (property?.PropertyType != typeof(string) || property.GetValue(component) is not string value || string.IsNullOrWhiteSpace(value)) {
            continue;
          }

          var translated = TryTranslate(loc, value);
          if (!string.IsNullOrWhiteSpace(translated) && translated != value) {
            return translated;
          }
        }
      }

      return string.Empty;
    }

    private static string TryTranslate(ILoc loc, string locKey) {
      if (loc == null) {
        return string.Empty;
      }

      try {
        return loc.T(locKey);
      } catch {
        return string.Empty;
      }
    }

    public static string CleanObjectName(string rawName) {
      if (string.IsNullOrWhiteSpace(rawName)) {
        return "Unknown object";
      }

      var name = rawName
        .Replace("(Clone)", string.Empty)
        .Replace(".Folktails", string.Empty)
        .Replace(".IronTeeth", string.Empty)
        .Replace(".Model", string.Empty)
        .Replace("ConstructionStage0", "Construction Stage 0")
        .Replace("ConstructionStage1", "Construction Stage 1")
        .Trim('.', ' ', '_', '-');
      var spaced = new StringBuilder();
      for (var i = 0; i < name.Length; i++) {
        var character = name[i];
        if (character is '.' or '_' or '-') {
          spaced.Append(' ');
          continue;
        }

        if (i > 0 && char.IsUpper(character) && !char.IsWhiteSpace(name[i - 1]) && !char.IsUpper(name[i - 1])) {
          spaced.Append(' ');
        }

        spaced.Append(character);
      }

      return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.ToString().Trim());
    }

  }

  internal static class FireVisualPresetJson {

    public static string Create(FireVisualPreset preset, FireVisualEffectKind selectedEffect, bool advancedEnabled, FireVisualPreviewTarget target) {
      var builder = new StringBuilder();
      builder.Append('{');
      AppendProperty(builder, "version", FireVisualPreset.Version).Append(',');
      AppendProperty(builder, "selectedEffect", selectedEffect.ToString()).Append(',');
      AppendProperty(builder, "advancedEnabled", advancedEnabled).Append(',');
      AppendTarget(builder, target).Append(',');
      builder.Append("\"effects\":{");
      var first = true;
      foreach (var pair in preset.ParticleEffects.OrderBy(pair => pair.Key.ToString())) {
        if (!first) {
          builder.Append(',');
        }

        first = false;
        AppendQuoted(builder, pair.Key.ToString()).Append(':');
        AppendParticle(builder, pair.Value);
      }

      builder.Append("},");
      builder.Append("\"char\":");
      AppendChar(builder, preset.Char);
      builder.Append('}');
      return builder.ToString();
    }

    private static StringBuilder AppendTarget(StringBuilder builder, FireVisualPreviewTarget target) {
      builder.Append("\"target\":{");
      AppendProperty(builder, "id", target.Id).Append(',');
      AppendProperty(builder, "rawName", target.RawName).Append(',');
      AppendProperty(builder, "kind", target.Kind).Append(',');
      AppendProperty(builder, "supported", target.Supported);
      return builder.Append('}');
    }

    private static void AppendParticle(StringBuilder builder, FireParticleEffectTuning tuning) {
      builder.Append('{');
      AppendProperty(builder, "enabled", tuning.Enabled).Append(',');
      AppendProperty(builder, "source", tuning.SourceName).Append(',');
      AppendProperty(builder, "intensity", tuning.Intensity).Append(',');
      AppendProperty(builder, "emission", tuning.Emission).Append(',');
      AppendVector(builder, "position", tuning.Position).Append(',');
      AppendVector(builder, "velocity", tuning.Velocity).Append(',');
      AppendProperty(builder, "size", tuning.Size).Append(',');
      AppendProperty(builder, "lifetime", tuning.Lifetime).Append(',');
      AppendProperty(builder, "speed", tuning.Speed).Append(',');
      AppendProperty(builder, "alpha", tuning.Alpha).Append(',');
      AppendColor(builder, "color", tuning.Color).Append(',');
      AppendProperty(builder, "spread", tuning.Spread).Append(',');
      AppendProperty(builder, "gravity", tuning.Gravity).Append(',');
      AppendProperty(builder, "noiseStrength", tuning.NoiseStrength).Append(',');
      AppendProperty(builder, "rotationSpeed", tuning.RotationSpeed).Append(',');
      AppendProperty(builder, "sortingOrder", tuning.SortingOrder).Append(',');
      AppendProperty(builder, "shapeMode", tuning.ShapeMode.ToString()).Append(',');
      AppendProperty(builder, "sizeOverLifetime", tuning.SizeOverLifetime.ToString());
      builder.Append('}');
    }

    private static void AppendChar(StringBuilder builder, FireCharEffectTuning tuning) {
      builder.Append('{');
      AppendProperty(builder, "enabled", tuning.Enabled).Append(',');
      AppendColor(builder, "tintColor", tuning.TintColor).Append(',');
      AppendProperty(builder, "tintStrength", tuning.TintStrength).Append(',');
      AppendProperty(builder, "darkening", tuning.Darkening).Append(',');
      AppendProperty(builder, "cutAmount", tuning.CutAmount).Append(',');
      AppendProperty(builder, "noiseScale", tuning.NoiseScale).Append(',');
      AppendProperty(builder, "noiseContrast", tuning.NoiseContrast).Append(',');
      AppendProperty(builder, "edgeWidth", tuning.EdgeWidth).Append(',');
      AppendProperty(builder, "edgeDepth", tuning.EdgeDepth).Append(',');
      AppendProperty(builder, "activeGlow", tuning.ActiveGlow).Append(',');
      AppendProperty(builder, "ashEdgeBrightness", tuning.AshEdgeBrightness).Append(',');
      AppendProperty(builder, "blackInteriorStrength", tuning.BlackInteriorStrength).Append(',');
      AppendProperty(builder, "seed", tuning.Seed);
      builder.Append('}');
    }

    private static StringBuilder AppendVector(StringBuilder builder, string name, Vector3 vector) {
      AppendQuoted(builder, name).Append(":{");
      AppendProperty(builder, "x", vector.x).Append(',');
      AppendProperty(builder, "y", vector.y).Append(',');
      AppendProperty(builder, "z", vector.z);
      return builder.Append('}');
    }

    private static StringBuilder AppendColor(StringBuilder builder, string name, Color color) {
      AppendQuoted(builder, name).Append(":{");
      AppendProperty(builder, "r", color.r).Append(',');
      AppendProperty(builder, "g", color.g).Append(',');
      AppendProperty(builder, "b", color.b).Append(',');
      AppendProperty(builder, "a", color.a);
      return builder.Append('}');
    }

    private static StringBuilder AppendProperty(StringBuilder builder, string name, string value) {
      AppendQuoted(builder, name).Append(':');
      return AppendQuoted(builder, value);
    }

    private static StringBuilder AppendProperty(StringBuilder builder, string name, bool value) {
      AppendQuoted(builder, name).Append(':').Append(value ? "true" : "false");
      return builder;
    }

    private static StringBuilder AppendProperty(StringBuilder builder, string name, int value) {
      AppendQuoted(builder, name).Append(':').Append(value.ToString(CultureInfo.InvariantCulture));
      return builder;
    }

    private static StringBuilder AppendProperty(StringBuilder builder, string name, float value) {
      AppendQuoted(builder, name).Append(':').Append(value.ToString("0.###", CultureInfo.InvariantCulture));
      return builder;
    }

    private static StringBuilder AppendQuoted(StringBuilder builder, string value) {
      builder.Append('"');
      if (!string.IsNullOrEmpty(value)) {
        builder.Append(value.Replace("\\", "\\\\").Replace("\"", "\\\""));
      }

      return builder.Append('"');
    }

  }

}
