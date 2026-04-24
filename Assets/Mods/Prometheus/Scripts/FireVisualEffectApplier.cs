using System.Collections.Generic;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireVisualEffectApplier : BaseComponent,
                                          IAwakableComponent,
                                          IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 0.25f;
    private const int MaxEmissionRate = 48;

    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly Color CharTintColor = new(0.10f, 0.09f, 0.08f, 1f);

    private FireSimulationRuntimeState _fireSimulationRuntimeState;
    private FireWaterContextRuntimeState _fireWaterContextRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireVisualEffectRuntimeState _fireVisualEffectRuntimeState;

    private readonly List<RendererPropertyBlockState> _rendererStates = new();
    private ParticleSystem _emberParticles;
    private ParticleSystem _smokeParticles;
    private ParticleSystem _fireParticles;
    private ParticleSystem _steamParticles;
    private MaterialPropertyBlock _propertyBlock;
    private float _timeSinceLastUpdate;
    private float _effectBaseHeight = 1.25f;
    private bool _initializedRenderers;

    [Inject]
    public void InjectDependencies(
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireVisualEffectRuntimeState fireVisualEffectRuntimeState) {
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireVisualEffectRuntimeState = fireVisualEffectRuntimeState;
    }

    public void Awake() {
      _propertyBlock = new MaterialPropertyBlock();
      CaptureRenderers();
      CreateParticleSystems();
    }

    public void Update() {
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      var simulation = _fireSimulationRuntimeState.TryGetSnapshot(entityId, out var simulationSnapshot)
        ? simulationSnapshot
        : FireSimulationRules.CreateTerminalDeadBuildingSnapshot();
      var waterContext = _fireWaterContextRuntimeState.TryGetSnapshot(entityId, out var waterSnapshot)
        ? waterSnapshot
        : new FireWaterContextSnapshot(false, 0f, false, 0f, 0f, 0f);
      var damageState = _fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageSnapshot)
        ? damageSnapshot
        : new FireDamageStateSnapshot(FireDamageCategory.Unknown, FireDamageState.Healthy, 0f, 0f, 0);

      var intensity = FireVisualEffectRules.ComputeIntensity(simulation, waterContext, damageState, _fireVisualEffectRuntimeState.CurrentTuning);
      ApplyParticleIntensity(_emberParticles, intensity.Embers, 0.75f, 1.4f);
      ApplyParticleIntensity(_smokeParticles, intensity.Smoke, 1.2f, 2.6f);
      ApplyParticleIntensity(_fireParticles, intensity.Fire, 0.45f, 1.0f);
      ApplyParticleIntensity(_steamParticles, intensity.Steam, 0.9f, 2.0f);
      ApplyCharTint(intensity.Char);
    }

    internal void DebugResetVisualEffects() {
      ApplyParticleIntensity(_emberParticles, 0f, 0.75f, 1.4f);
      ApplyParticleIntensity(_smokeParticles, 0f, 1.2f, 2.6f);
      ApplyParticleIntensity(_fireParticles, 0f, 0.45f, 1.0f);
      ApplyParticleIntensity(_steamParticles, 0f, 0.9f, 2.0f);
      ApplyCharTint(0f);
    }

    private void CaptureRenderers() {
      if (_initializedRenderers) {
        return;
      }

      _initializedRenderers = true;
      var renderers = GameObject.GetComponentsInChildren<Renderer>();
      var maxY = float.MinValue;
      for (var i = 0; i < renderers.Length; i++) {
        var renderer = renderers[i];
        if (renderer == null || renderer is ParticleSystemRenderer) {
          continue;
        }

        _rendererStates.Add(new RendererPropertyBlockState(renderer));
        maxY = Mathf.Max(maxY, renderer.bounds.max.y);
      }

      if (maxY > float.MinValue) {
        _effectBaseHeight = Mathf.Clamp((maxY - GameObject.transform.position.y) + 0.15f, 0.8f, 6f);
      }
    }

    private void CreateParticleSystems() {
      _emberParticles = CreateParticleSystem("PrometheusEmbers", new Color(1f, 0.46f, 0.12f, 0.92f), 0.18f, 1.2f, 0.9f, 0.04f);
      _smokeParticles = CreateParticleSystem("PrometheusSmoke", new Color(0.25f, 0.25f, 0.23f, 0.46f), 0.72f, 2.5f, 0.35f, 0.18f);
      _fireParticles = CreateParticleSystem("PrometheusFire", new Color(1f, 0.34f, 0.08f, 0.82f), 0.36f, 0.75f, 0.55f, 0.09f);
      _steamParticles = CreateParticleSystem("PrometheusSteam", new Color(0.78f, 0.88f, 0.90f, 0.40f), 0.62f, 1.7f, 0.42f, 0.16f);
    }

    private ParticleSystem CreateParticleSystem(
      string name,
      Color color,
      float startSize,
      float lifetime,
      float speed,
      float gravityModifier) {
      var particleObject = new GameObject(name);
      particleObject.transform.SetParent(GameObject.transform, false);
      particleObject.transform.localPosition = new Vector3(0f, _effectBaseHeight, 0f);
      particleObject.transform.localRotation = Quaternion.identity;
      particleObject.transform.localScale = Vector3.one;

      var particles = particleObject.AddComponent<ParticleSystem>();
      var main = particles.main;
      main.loop = true;
      main.playOnAwake = true;
      main.startLifetime = lifetime;
      main.startSpeed = speed;
      main.startSize = startSize;
      main.startColor = color;
      main.gravityModifier = gravityModifier;
      main.simulationSpace = ParticleSystemSimulationSpace.World;

      var emission = particles.emission;
      emission.rateOverTime = 0f;

      var shape = particles.shape;
      shape.shapeType = ParticleSystemShapeType.Sphere;
      shape.radius = 0.6f;
      shape.radiusThickness = 0.35f;

      var velocity = particles.velocityOverLifetime;
      velocity.enabled = true;
      velocity.space = ParticleSystemSimulationSpace.World;
      velocity.y = speed;

      var renderer = particles.GetComponent<ParticleSystemRenderer>();
      renderer.renderMode = ParticleSystemRenderMode.Billboard;
      renderer.sortingOrder = 20;
      if (ParticleMaterial != null) {
        renderer.sharedMaterial = ParticleMaterial;
      }

      particles.Play();
      particleObject.SetActive(false);
      return particles;
    }

    private static Material _particleMaterial;

    private static Material ParticleMaterial {
      get {
        if (_particleMaterial != null) {
          return _particleMaterial;
        }

        var shader = Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("UI/Default");
        if (shader == null) {
          return null;
        }

        _particleMaterial = new Material(shader) {
          hideFlags = HideFlags.HideAndDontSave
        };
        return _particleMaterial;
      }
    }

    private static void ApplyParticleIntensity(ParticleSystem particles, float intensity, float minScale, float maxScale) {
      if (particles == null) {
        return;
      }

      var clampedIntensity = Mathf.Clamp01(intensity);
      var particleObject = particles.gameObject;
      if (clampedIntensity <= 0.01f) {
        if (particleObject.activeSelf) {
          particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
          particleObject.SetActive(false);
        }

        return;
      }

      if (!particleObject.activeSelf) {
        particleObject.SetActive(true);
      }

      var emission = particles.emission;
      emission.rateOverTime = Mathf.CeilToInt(Mathf.Lerp(2f, MaxEmissionRate, clampedIntensity));

      var main = particles.main;
      main.startSizeMultiplier = Mathf.Lerp(minScale, maxScale, clampedIntensity);
      if (!particles.isPlaying) {
        particles.Play();
      }
    }

    private void ApplyCharTint(float charIntensity) {
      var clampedIntensity = Mathf.Clamp01(charIntensity);
      for (var i = 0; i < _rendererStates.Count; i++) {
        var rendererState = _rendererStates[i];
        if (rendererState.Renderer == null) {
          continue;
        }

        if (clampedIntensity <= 0.01f) {
          rendererState.Renderer.SetPropertyBlock(rendererState.OriginalPropertyBlock);
          continue;
        }

        rendererState.Renderer.GetPropertyBlock(_propertyBlock);
        var baseColor = rendererState.BaseColor;
        var tintedColor = Color.Lerp(baseColor, CharTintColor, clampedIntensity * 0.85f);
        _propertyBlock.SetColor(ColorPropertyId, tintedColor);
        _propertyBlock.SetColor(BaseColorPropertyId, tintedColor);
        rendererState.Renderer.SetPropertyBlock(_propertyBlock);
      }
    }

    private sealed class RendererPropertyBlockState {

      public Renderer Renderer { get; }
      public Color BaseColor { get; }
      public MaterialPropertyBlock OriginalPropertyBlock { get; }

      public RendererPropertyBlockState(Renderer renderer) {
        Renderer = renderer;
        BaseColor = TryGetRendererBaseColor(renderer, out var color) ? color : Color.white;
        OriginalPropertyBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(OriginalPropertyBlock);
      }

      private static bool TryGetRendererBaseColor(Renderer renderer, out Color color) {
        color = Color.white;
        var sharedMaterial = renderer.sharedMaterial;
        if (sharedMaterial == null) {
          return false;
        }

        if (sharedMaterial.HasProperty(BaseColorPropertyId)) {
          color = sharedMaterial.GetColor(BaseColorPropertyId);
          return true;
        }

        if (sharedMaterial.HasProperty(ColorPropertyId)) {
          color = sharedMaterial.GetColor(ColorPropertyId);
          return true;
        }

        return false;
      }

    }

  }
}
