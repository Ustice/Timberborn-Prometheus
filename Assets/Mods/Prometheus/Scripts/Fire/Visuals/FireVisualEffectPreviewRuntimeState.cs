using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal sealed class FireVisualEffectPreviewRuntimeState {

    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int CutoffPropertyId = Shader.PropertyToID("_Cutoff");
    private static readonly int AlphaClipPropertyId = Shader.PropertyToID("_AlphaClip");

    private readonly Dictionary<int, PreviewInstance> _previewsByTargetId = new();

    public bool TryApplyEffect(GameObject target, FireVisualPreset preset, FireVisualEffectKind effectKind, out string message) {
      if (!TryPrepareTarget(target, out var preview, out message)) {
        return false;
      }

      if (effectKind == FireVisualEffectKind.Char) {
        preview.ApplyChar(preset.Char);
      } else {
        preview.ApplyParticle(preset.GetParticle(effectKind));
      }

      message = $"Applied {effectKind} preview to {FireVisualPreviewTarget.CleanObjectName(target.name)}.";
      return true;
    }

    public bool TryApplyPreset(GameObject target, FireVisualPreset preset, out string message) {
      if (!TryPrepareTarget(target, out var preview, out message)) {
        return false;
      }

      foreach (var tuning in preset.ParticleEffects.Values) {
        preview.ApplyParticle(tuning);
      }

      preview.ApplyChar(preset.Char);
      message = $"Applied full preview preset to {FireVisualPreviewTarget.CleanObjectName(target.name)}.";
      return true;
    }

    public bool ClearPreview(GameObject target, out string message) {
      if (target == null) {
        message = "No selected target.";
        return false;
      }

      var targetId = target.GetInstanceID();
      if (!_previewsByTargetId.TryGetValue(targetId, out var preview)) {
        message = "No preview on selected target.";
        return false;
      }

      preview.Clear();
      _previewsByTargetId.Remove(targetId);
      message = $"Cleared preview on {FireVisualPreviewTarget.CleanObjectName(target.name)}.";
      return true;
    }

    public void ClearAllPreviews() {
      foreach (var preview in _previewsByTargetId.Values.ToArray()) {
        preview.Clear();
      }

      _previewsByTargetId.Clear();
    }

    private bool TryPrepareTarget(GameObject target, out PreviewInstance preview, out string message) {
      preview = null;
      if (target == null) {
        message = "No selected target.";
        return false;
      }

      if (!target.scene.IsValid() || !target.scene.isLoaded) {
        message = "Selected target is not loaded.";
        return false;
      }

      var targetId = target.GetInstanceID();
      if (!_previewsByTargetId.TryGetValue(targetId, out preview)) {
        preview = new PreviewInstance(target);
        _previewsByTargetId[targetId] = preview;
      }

      if (!preview.IsSupported) {
        message = "Selected target has no usable transform or renderers.";
        return false;
      }

      message = string.Empty;
      return true;
    }

    private sealed class PreviewInstance {

      private readonly GameObject _target;
      private readonly GameObject _root;
      private readonly Dictionary<FireVisualEffectKind, GameObject> _particleRootsByKind = new();
      private readonly List<RendererPreviewState> _rendererStates;

      public bool IsSupported => _target != null && (_target.transform != null || _rendererStates.Count > 0);

      public PreviewInstance(GameObject target) {
        _target = target;
        _root = new GameObject("PrometheusVisualPreview");
        _root.transform.SetParent(target.transform, false);
        _root.transform.localPosition = Vector3.zero;
        _root.transform.localRotation = Quaternion.identity;
        _root.transform.localScale = Vector3.one;
        _rendererStates = target.GetComponentsInChildren<Renderer>(true)
          .Where(renderer => renderer != null && renderer is not ParticleSystemRenderer)
          .Select(renderer => new RendererPreviewState(renderer))
          .ToList();
      }

      public void ApplyParticle(FireParticleEffectTuning tuning) {
        ClearParticle(tuning.Kind);
        if (!tuning.Enabled) {
          return;
        }

        var source = FireNativeParticleSourceCatalog.TryGetSource(tuning.SourceName)
                     ?? FireNativeParticleSourceCatalog.TryGetRecommendedSource(tuning.Kind);
        if (source == null) {
          return;
        }

        var clone = Object.Instantiate(source);
        clone.name = $"PrometheusPreview{tuning.Kind}";
        clone.transform.SetParent(_root.transform, false);
        clone.transform.localPosition = tuning.Position;
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.localScale = Vector3.one;
        clone.SetActive(true);
        _particleRootsByKind[tuning.Kind] = clone;

        foreach (var particleSystem in clone.GetComponentsInChildren<ParticleSystem>(true)) {
          ApplyParticleTuning(particleSystem, tuning);
        }
      }

      public void ApplyChar(FireCharEffectTuning tuning) {
        if (!tuning.Enabled) {
          ClearChar();
          return;
        }

        foreach (var rendererState in _rendererStates) {
          rendererState.ApplyChar(tuning);
        }
      }

      public void Clear() {
        foreach (var kind in _particleRootsByKind.Keys.ToArray()) {
          ClearParticle(kind);
        }

        ClearChar();
        if (_root != null) {
          Object.Destroy(_root);
        }
      }

      private void ClearParticle(FireVisualEffectKind kind) {
        if (!_particleRootsByKind.TryGetValue(kind, out var root)) {
          return;
        }

        if (root != null) {
          Object.Destroy(root);
        }

        _particleRootsByKind.Remove(kind);
      }

      private void ClearChar() {
        foreach (var rendererState in _rendererStates) {
          rendererState.Restore();
        }
      }

      private static void ApplyParticleTuning(ParticleSystem particleSystem, FireParticleEffectTuning tuning) {
        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetimeMultiplier = Mathf.Max(0.05f, main.startLifetimeMultiplier * tuning.Lifetime);
        main.startSpeedMultiplier = Mathf.Max(0f, main.startSpeedMultiplier * tuning.Speed);
        main.startSizeMultiplier = Mathf.Max(0.01f, main.startSizeMultiplier * tuning.Size);
        main.startColor = ApplyAlpha(tuning.Color, tuning.Alpha);
        main.gravityModifierMultiplier = tuning.Gravity;

        var emission = particleSystem.emission;
        emission.enabled = tuning.Emission > 0f && tuning.Intensity > 0f;
        emission.rateOverTimeMultiplier = Mathf.Max(0f, emission.rateOverTimeMultiplier * tuning.Emission * tuning.Intensity);

        var shape = particleSystem.shape;
        if (tuning.ShapeMode != FireVisualShapeMode.Native || tuning.Spread > 0f) {
          shape.enabled = true;
          shape.shapeType = tuning.ShapeMode switch {
            FireVisualShapeMode.Box => ParticleSystemShapeType.Box,
            FireVisualShapeMode.Cone => ParticleSystemShapeType.Cone,
            _ => ParticleSystemShapeType.Sphere,
          };
          shape.radius = Mathf.Max(0f, tuning.Spread);
          shape.radiusThickness = 1f;
          shape.randomDirectionAmount = 0.35f;
        }

        var velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = tuning.Velocity.sqrMagnitude > 0.0001f;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = tuning.Velocity.x;
        velocity.y = tuning.Velocity.y;
        velocity.z = tuning.Velocity.z;

        var noise = particleSystem.noise;
        noise.enabled = tuning.NoiseStrength > 0.01f;
        noise.strength = tuning.NoiseStrength;

        var rotation = particleSystem.rotationOverLifetime;
        rotation.enabled = Mathf.Abs(tuning.RotationSpeed) > 0.01f;
        rotation.z = tuning.RotationSpeed;

        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        ConfigureSizeOverLifetime(sizeOverLifetime, tuning.SizeOverLifetime);

        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null) {
          renderer.sortingOrder = tuning.SortingOrder;
        }

        particleSystem.Play();
      }

      private static void ConfigureSizeOverLifetime(ParticleSystem.SizeOverLifetimeModule module, FireVisualSizeOverLifetimePreset preset) {
        module.enabled = preset != FireVisualSizeOverLifetimePreset.Constant;
        if (!module.enabled) {
          return;
        }

        module.size = preset switch {
          FireVisualSizeOverLifetimePreset.Grow => new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(1f, 1f))),
          FireVisualSizeOverLifetimePreset.Shrink => new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.1f))),
          FireVisualSizeOverLifetimePreset.Swell => new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.55f, 1.25f),
            new Keyframe(1f, 0.35f))),
          FireVisualSizeOverLifetimePreset.Pop => new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.1f),
            new Keyframe(0.2f, 1.2f),
            new Keyframe(1f, 0.05f))),
          _ => new ParticleSystem.MinMaxCurve(1f),
        };
      }

      private static Color ApplyAlpha(Color color, float alpha) => new(color.r, color.g, color.b, Mathf.Clamp01(color.a * alpha));

    }

    private sealed class RendererPreviewState {

      private readonly Renderer _renderer;
      private readonly MaterialPropertyBlock _originalBlock;
      private readonly MaterialPropertyBlock _workingBlock = new();
      private readonly Color _baseColor;
      private readonly bool _supportsCutoff;

      public RendererPreviewState(Renderer renderer) {
        _renderer = renderer;
        _originalBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_originalBlock);
        _baseColor = TryGetBaseColor(renderer, out var color) ? color : Color.white;
        _supportsCutoff = RendererSupportsCutoff(renderer);
      }

      public void ApplyChar(FireCharEffectTuning tuning) {
        if (_renderer == null) {
          return;
        }

        _renderer.GetPropertyBlock(_workingBlock);
        var rendererNoise = Mathf.Abs(Mathf.Sin((_renderer.GetInstanceID() * 12.9898f + tuning.Seed * 78.233f) * tuning.NoiseScale));
        rendererNoise = Mathf.Pow(rendererNoise, Mathf.Max(0.1f, tuning.NoiseContrast));
        var mottledStrength = Mathf.Clamp01(tuning.TintStrength * Mathf.Lerp(1f - tuning.EdgeDepth, 1f, rendererNoise));
        var charColor = Color.Lerp(_baseColor, tuning.TintColor, mottledStrength);
        charColor = Color.Lerp(charColor, Color.black, Mathf.Clamp01(tuning.BlackInteriorStrength * tuning.CutAmount * 0.5f));
        var ashEdge = Color.Lerp(charColor, Color.white, Mathf.Clamp01(tuning.AshEdgeBrightness * tuning.EdgeWidth));
        var finalColor = Color.Lerp(charColor, ashEdge, Mathf.Clamp01(tuning.EdgeDepth * rendererNoise));

        _workingBlock.SetColor(ColorPropertyId, finalColor);
        _workingBlock.SetColor(BaseColorPropertyId, finalColor);
        if (_supportsCutoff) {
          _workingBlock.SetFloat(CutoffPropertyId, Mathf.Clamp01(tuning.CutAmount));
          _workingBlock.SetFloat(AlphaClipPropertyId, 1f);
        }

        _renderer.SetPropertyBlock(_workingBlock);
      }

      public void Restore() {
        if (_renderer != null) {
          _renderer.SetPropertyBlock(_originalBlock);
        }
      }

      private static bool TryGetBaseColor(Renderer renderer, out Color color) {
        color = Color.white;
        var material = renderer.sharedMaterial;
        if (material == null) {
          return false;
        }

        if (material.HasProperty(BaseColorPropertyId)) {
          color = material.GetColor(BaseColorPropertyId);
          return true;
        }

        if (material.HasProperty(ColorPropertyId)) {
          color = material.GetColor(ColorPropertyId);
          return true;
        }

        return false;
      }

      private static bool RendererSupportsCutoff(Renderer renderer) {
        var material = renderer.sharedMaterial;
        return material != null && (material.HasProperty(CutoffPropertyId) || material.HasProperty(AlphaClipPropertyId));
      }

    }

  }

}
