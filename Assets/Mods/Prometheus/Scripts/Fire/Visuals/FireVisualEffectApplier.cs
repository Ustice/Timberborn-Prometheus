using System.Collections.Generic;
using System.Linq;
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
    private static readonly Color DesiccatedTintColor = new(0.45f, 0.30f, 0.14f, 1f);
    private static readonly Color CharTintColor = new(0.10f, 0.09f, 0.08f, 1f);

    private FireExposureRuntimeState _fireExposureRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireVisualEffectRuntimeState _fireVisualEffectRuntimeState;
    private FireResetRegistry _fireResetRegistry;
    private FireResetRegistration _resetRegistration = FireResetRegistration.Empty;

    private readonly List<RendererPropertyBlockState> _rendererStates = new();
    private ParticleEffectGroup _emberEffect;
    private ParticleEffectGroup _smokeEffect;
    private ParticleEffectGroup _fireEffect;
    private ParticleEffectGroup _steamEffect;
    private MaterialPropertyBlock _propertyBlock;
    private float _timeSinceLastUpdate;
    private float _effectBaseHeight = 1.25f;
    private bool _initializedRenderers;

    [Inject]
    public void InjectDependencies(
      FireExposureRuntimeState fireExposureRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireVisualEffectRuntimeState fireVisualEffectRuntimeState,
      FireResetRegistry fireResetRegistry) {
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireVisualEffectRuntimeState = fireVisualEffectRuntimeState;
      _fireResetRegistry = fireResetRegistry;
    }

    public void Awake() {
      _propertyBlock = new MaterialPropertyBlock();
      CaptureRenderers();
      CreateParticleSystems();
    }

    public void Update() {
      EnsureResetRegistration();
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      var exposure = _fireExposureRuntimeState.TryGetSnapshot(entityId, out var exposureSnapshot)
        ? exposureSnapshot
        : FireExposureRules.CreateTerminalDeadBuildingSnapshot();
      var damageState = _fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var damageSnapshot)
        ? damageSnapshot
        : new FireDamageStateSnapshot(FireDamageCategory.Unknown, FireDamageState.Healthy, 0f, 0f, 0);

      var intensity = FireVisualEffectRules.ComputeIntensity(exposure, damageState, _fireVisualEffectRuntimeState.CurrentTuning);
      var tuning = _fireVisualEffectRuntimeState.CurrentTuning;
      _emberEffect.ApplyTuning(tuning, _effectBaseHeight);
      _smokeEffect.ApplyTuning(tuning, _effectBaseHeight);
      _fireEffect.ApplyTuning(tuning, _effectBaseHeight);
      _steamEffect.ApplyTuning(tuning, _effectBaseHeight);
      _emberEffect.ApplyIntensity(intensity.Embers, 0.75f, 1.4f, tuning.EffectSize);
      _smokeEffect.ApplyIntensity(intensity.Smoke, 1.2f, 2.6f, tuning.EffectSize);
      _fireEffect.ApplyIntensity(intensity.Fire, 0.45f, 1.0f, tuning.EffectSize);
      _steamEffect.ApplyIntensity(intensity.Steam, 0.9f, 2.0f, tuning.EffectSize);
      ApplySurfaceTint(intensity.Desiccation, intensity.Char);
    }

    internal void DebugResetVisualEffects() {
      var tuning = _fireVisualEffectRuntimeState.CurrentTuning;
      _emberEffect?.ApplyIntensity(0f, 0.75f, 1.4f, tuning.EffectSize);
      _smokeEffect?.ApplyIntensity(0f, 1.2f, 2.6f, tuning.EffectSize);
      _fireEffect?.ApplyIntensity(0f, 0.45f, 1.0f, tuning.EffectSize);
      _steamEffect?.ApplyIntensity(0f, 0.9f, 2.0f, tuning.EffectSize);
      ApplySurfaceTint(0f, 0f);
    }

    private void OnDestroy() {
      _resetRegistration.Dispose();
    }

    private void EnsureResetRegistration() {
      if (_resetRegistration != FireResetRegistration.Empty) {
        return;
      }

      _resetRegistration = _fireResetRegistry.RegisterEntity(
        GameObject.GetInstanceID(),
        FireResetHookKind.VisualEffect,
        nameof(FireVisualEffectApplier),
        DebugResetVisualEffects);
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
      _emberEffect = CreateNativeOrFallbackEffect(
        NativeParticleEffectKind.Embers,
        "PrometheusEmbers",
        () => CreateGeneratedParticleEffect(NativeParticleEffectKind.Embers, "PrometheusEmbers", new Color(1f, 0.46f, 0.12f, 0.92f), 0.18f, 1.2f, 0.9f, 0.04f));
      _smokeEffect = CreateNativeOrFallbackEffect(
        NativeParticleEffectKind.Smoke,
        "PrometheusSmoke",
        () => CreateGeneratedParticleEffect(NativeParticleEffectKind.Smoke, "PrometheusSmoke", new Color(0.25f, 0.25f, 0.23f, 0.46f), 0.72f, 2.5f, 0.35f, 0.18f));
      _fireEffect = CreateNativeOrFallbackEffect(
        NativeParticleEffectKind.Fire,
        "PrometheusFire",
        () => CreateGeneratedParticleEffect(NativeParticleEffectKind.Fire, "PrometheusFire", new Color(1f, 0.34f, 0.08f, 0.82f), 0.36f, 0.75f, 0.55f, 0.09f));
      _steamEffect = CreateNativeOrFallbackEffect(
        NativeParticleEffectKind.Steam,
        "PrometheusSteam",
        () => CreateGeneratedParticleEffect(NativeParticleEffectKind.Steam, "PrometheusSteam", new Color(0.78f, 0.88f, 0.90f, 0.40f), 0.62f, 1.7f, 0.42f, 0.16f));
    }

    private ParticleEffectGroup CreateNativeOrFallbackEffect(
      NativeParticleEffectKind kind,
      string name,
      System.Func<ParticleEffectGroup> fallbackFactory) {
      var nativeEffect = NativeParticleEffectLibrary.TryCreateEffect(kind, name, GameObject.transform, new Vector3(0f, _effectBaseHeight, 0f));
      return nativeEffect ?? fallbackFactory();
    }

    private ParticleEffectGroup CreateGeneratedParticleEffect(
      NativeParticleEffectKind kind,
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
      return new ParticleEffectGroup(particleObject, kind, particles);
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

    private void ApplySurfaceTint(float desiccationIntensity, float charIntensity) {
      var clampedDesiccation = Mathf.Clamp01(desiccationIntensity);
      var clampedIntensity = Mathf.Clamp01(charIntensity);
      for (var i = 0; i < _rendererStates.Count; i++) {
        var rendererState = _rendererStates[i];
        if (rendererState.Renderer == null) {
          continue;
        }

        if (clampedDesiccation <= 0.01f && clampedIntensity <= 0.01f) {
          rendererState.Renderer.SetPropertyBlock(rendererState.OriginalPropertyBlock);
          continue;
        }

        rendererState.Renderer.GetPropertyBlock(_propertyBlock);
        var baseColor = rendererState.BaseColor;
        var dryColor = Color.Lerp(baseColor, DesiccatedTintColor, clampedDesiccation * 0.7f);
        var tintedColor = Color.Lerp(dryColor, CharTintColor, clampedIntensity * 0.9f);
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

    private enum NativeParticleEffectKind {
      Embers,
      Smoke,
      Fire,
      Steam,
    }

    private sealed class ParticleEffectGroup {

      private readonly GameObject _root;
      private readonly NativeParticleEffectKind _kind;
      private readonly List<ParticleSystemState> _particleStates;

      public ParticleEffectGroup(GameObject root, NativeParticleEffectKind kind, ParticleSystem particleSystem)
        : this(root, kind, new[] { particleSystem }) {
      }

      public ParticleEffectGroup(GameObject root, NativeParticleEffectKind kind, IEnumerable<ParticleSystem> particleSystems) {
        _root = root;
        _kind = kind;
        _particleStates = particleSystems
          .Where(particleSystem => particleSystem != null)
          .Select(particleSystem => new ParticleSystemState(particleSystem))
          .ToList();
      }

      public void ApplyTuning(FireVisualEffectTuning tuning, float effectBaseHeight) {
        if (_root == null) {
          return;
        }

        _root.transform.localPosition = new Vector3(0f, effectBaseHeight + tuning.HeightOffset, tuning.DepthOffset);
        if (_kind == NativeParticleEffectKind.Embers) {
          _particleStates.ForEach(state => state.ApplyShapeSpread(tuning.EmberSpread));
        }
      }

      public void ApplyIntensity(float intensity, float minScale, float maxScale, float effectSize) {
        if (_root == null || _particleStates.Count == 0) {
          return;
        }

        var clampedIntensity = Mathf.Clamp01(intensity);
        if (clampedIntensity <= 0.01f) {
          if (_root.activeSelf) {
            _particleStates.ForEach(state => state.ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear));
            _root.SetActive(false);
          }

          return;
        }

        if (!_root.activeSelf) {
          _root.SetActive(true);
        }

        _particleStates.ForEach(state => state.ApplyIntensity(clampedIntensity, minScale, maxScale, effectSize));
      }

    }

    private sealed class ParticleSystemState {

      public ParticleSystem ParticleSystem { get; }

      private readonly float _baseEmissionRateMultiplier;
      private readonly float _baseStartSizeMultiplier;
      private readonly bool _baseShapeEnabled;
      private readonly ParticleSystemShapeType _baseShapeType;
      private readonly float _baseShapeRadius;

      public ParticleSystemState(ParticleSystem particleSystem) {
        ParticleSystem = particleSystem;
        var emission = particleSystem.emission;
        var main = particleSystem.main;
        var shape = particleSystem.shape;
        _baseEmissionRateMultiplier = Mathf.Max(1f, emission.rateOverTimeMultiplier);
        _baseStartSizeMultiplier = Mathf.Max(0.01f, main.startSizeMultiplier);
        _baseShapeEnabled = shape.enabled;
        _baseShapeType = shape.shapeType;
        _baseShapeRadius = Mathf.Max(0f, shape.radius);

        main.loop = true;
        main.playOnAwake = true;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null) {
          renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 20);
        }
      }

      public void ApplyIntensity(float intensity, float minScale, float maxScale, float effectSize) {
        var emission = ParticleSystem.emission;
        emission.enabled = true;
        emission.rateOverTimeMultiplier = Mathf.Max(
          1f,
          Mathf.Lerp(_baseEmissionRateMultiplier * 0.25f, _baseEmissionRateMultiplier, intensity),
          Mathf.Lerp(2f, MaxEmissionRate, intensity));

        var main = ParticleSystem.main;
        main.startSizeMultiplier = _baseStartSizeMultiplier * Mathf.Lerp(minScale, maxScale, intensity) * effectSize;

        if (!ParticleSystem.isPlaying) {
          ParticleSystem.Play();
        }
      }

      public void ApplyShapeSpread(float spread) {
        var shape = ParticleSystem.shape;
        if (spread <= 0.01f) {
          shape.enabled = _baseShapeEnabled;
          shape.shapeType = _baseShapeType;
          shape.radius = _baseShapeRadius;
          return;
        }

        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = spread;
        shape.radiusThickness = 1f;
        shape.randomDirectionAmount = 0.35f;
      }

    }

    private static class NativeParticleEffectLibrary {

      private static readonly Dictionary<NativeParticleEffectKind, GameObject> SourcesByKind = new();
      private static readonly HashSet<NativeParticleEffectKind> LoggedResolvedKinds = new();
      private static readonly HashSet<NativeParticleEffectKind> LoggedUnavailableKinds = new();
      private static bool _searched;

      public static ParticleEffectGroup TryCreateEffect(
        NativeParticleEffectKind kind,
        string name,
        Transform parent,
        Vector3 localPosition) {
        var source = FindSource(kind);
        if (source == null) {
          if (LoggedUnavailableKinds.Add(kind)) {
            FireTelemetry.LogWarning($"event={FireTelemetryEvents.NativeVisualEffectUnavailable} kind={kind}");
          }

          return null;
        }

        var clone = Object.Instantiate(source);
        clone.name = name;
        clone.transform.SetParent(parent, false);
        clone.transform.localPosition = localPosition;
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.localScale = Vector3.one;
        clone.SetActive(false);

        var particleSystems = clone.GetComponentsInChildren<ParticleSystem>(true);
        if (LoggedResolvedKinds.Add(kind)) {
          FireTelemetry.Log($"event={FireTelemetryEvents.NativeVisualEffectResolved} kind={kind} source=\"{source.name}\" systems={particleSystems.Length}");
        }

        return new ParticleEffectGroup(clone, kind, particleSystems);
      }

      private static GameObject FindSource(NativeParticleEffectKind kind) {
        EnsureSearched();
        return SourcesByKind.TryGetValue(kind, out var source) ? source : null;
      }

      private static void EnsureSearched() {
        if (_searched) {
          return;
        }

        _searched = true;
        var resourceParticlePrefabs = Resources.LoadAll<GameObject>(string.Empty)
          .Where(IsNativeCandidate)
          .Where(gameObject => gameObject.GetComponentsInChildren<ParticleSystem>(true).Length > 0)
          .ToArray();
        var loadedParticleObjects = Resources.FindObjectsOfTypeAll<ParticleSystem>()
          .Where(IsNativeCandidate)
          .Select(particleSystem => particleSystem.gameObject)
          .ToArray();
        var particleObjects = resourceParticlePrefabs
          .Concat(loadedParticleObjects)
          .GroupBy(gameObject => gameObject.GetInstanceID())
          .Select(group => group.First())
          .ToArray();

        foreach (var kind in new[] {
                   NativeParticleEffectKind.Embers,
                   NativeParticleEffectKind.Smoke,
                   NativeParticleEffectKind.Fire,
                   NativeParticleEffectKind.Steam,
                 }) {
          var source = particleObjects
            .Select(gameObject => new NativeParticleCandidate(gameObject, ScoreParticleObject(kind, gameObject)))
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .FirstOrDefault();

          if (source.SourceRoot != null) {
            SourcesByKind[kind] = source.SourceRoot;
          }
        }
      }

      private static bool IsNativeCandidate(GameObject gameObject) {
        if (gameObject == null) {
          return false;
        }

        var hierarchyName = GetHierarchyName(gameObject.transform);
        return !hierarchyName.Contains("Prometheus")
               && !hierarchyName.Contains("Preview")
               && !hierarchyName.Contains("UnityEngine");
      }

      private static bool IsNativeCandidate(ParticleSystem particleSystem) {
        return particleSystem != null && IsNativeCandidate(particleSystem.gameObject);
      }

      private static int ScoreParticleObject(NativeParticleEffectKind kind, GameObject gameObject) {
        var particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>(true);
        var names = particleSystems
          .Select(particleSystem => GetHierarchyName(particleSystem.transform))
          .Concat(new[] { GetHierarchyName(gameObject.transform) });
        var materialNames = particleSystems
          .Select(particleSystem => particleSystem.GetComponent<ParticleSystemRenderer>())
          .Where(renderer => renderer != null && renderer.sharedMaterial != null)
          .Select(renderer => renderer.sharedMaterial.name);
        var searchable = string.Join(" ", names.Concat(materialNames)).ToLowerInvariant();
        var main = particleSystems.Length == 0 ? default : particleSystems[0].main;

        return kind switch {
          NativeParticleEffectKind.Embers => Score(searchable, "sparks_trail", 120, "common_trail_sparks", 110, "spark", 80, "firework", 35),
          NativeParticleEffectKind.Smoke => Score(searchable, "smeltersmoke", 130, "bakerysmoke", 120, "smoke", 100, "exhaust", 35) - Score(searchable, "explosion", 55, "steam", 35),
          NativeParticleEffectKind.Fire => Score(searchable, "campfirefire", 140, "brazierfire", 130, "fire", 100, "flame", 95) - Score(searchable, "firework", 70, "spark", 25),
          NativeParticleEffectKind.Steam => Score(searchable, "steamenginesmoke", 130, "geothermal", 95, "steam", 90, "smoke_soft", 45) + (main.startColor.color.a < 0.65f ? 5 : 0),
          _ => 0,
        };
      }

      private static int Score(string text, string firstKeyword, int firstScore, string secondKeyword, int secondScore, string thirdKeyword, int thirdScore, string fourthKeyword = "", int fourthScore = 0) {
        return Score(text, firstKeyword, firstScore)
               + Score(text, secondKeyword, secondScore)
               + Score(text, thirdKeyword, thirdScore)
               + Score(text, fourthKeyword, fourthScore);
      }

      private static int Score(string text, string firstKeyword, int firstScore, string secondKeyword, int secondScore) {
        return Score(text, firstKeyword, firstScore)
               + Score(text, secondKeyword, secondScore);
      }

      private static int Score(string text, string keyword, int score) {
        return !string.IsNullOrEmpty(keyword) && text.Contains(keyword) ? score : 0;
      }

      private static string GetHierarchyName(Transform transform) {
        var names = new List<string>();
        var current = transform;
        while (current != null) {
          names.Add(current.name);
          current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
      }

    }

    private readonly struct NativeParticleCandidate {

      public GameObject SourceRoot { get; }
      public int Score { get; }

      public NativeParticleCandidate(GameObject sourceRoot, int score) {
        SourceRoot = sourceRoot;
        Score = score;
      }

    }

  }
}
