using System.Collections.Generic;
using System.Linq;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mods.Prometheus.Scripts {
  internal class FireVisualEffectApplier : BaseComponent,
                                          IAwakableComponent,
                                          IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 0.25f;
    private const float VisualTelemetryIntervalInSeconds = 2f;
    private const int MaxEmissionRate = 48;
    private const int VisualTelemetryBudget = 180;
    private const int SurfaceMaterialTelemetryBudget = 48;
    private const int SurfaceMaterialTextureTelemetryBudget = 48;
    private static readonly int MainTexturePropertyId = Shader.PropertyToID("_MainTex");
    private static int _visualTelemetryCount;
    private static int _surfaceMaterialTelemetryCount;
    private static int _surfaceMaterialTextureTelemetryCount;
    private static Texture2D _charredPineTexture;

    private FireRuntimeProjectionRuntimeState _fireRuntimeProjectionRuntimeState;
    private FireVisualEffectRuntimeState _fireVisualEffectRuntimeState;
    private PrometheusWorldLoadState _prometheusWorldLoadState;

    private readonly List<TreeSurfaceTextureState> _treeSurfaceStates = new();
    private ParticleEffectGroup _emberEffect;
    private ParticleEffectGroup _smokeEffect;
    private ParticleEffectGroup _fireEffect;
    private ParticleEffectGroup _steamEffect;
    private float _timeSinceLastUpdate;
    private float _lastVisualTelemetryTime = -999f;
    private float _effectBaseHeight = 1.25f;
    private bool _initializedRenderers;
    private bool _isCropProfile;
    private bool _isTreeProfile;
    private bool _loggedSurfaceMaterials;
    private FireNaturalResourceVisualStage _lastAppliedSurfaceStage = FireNaturalResourceVisualStage.Healthy;

    [Inject]
    public void InjectDependencies(
      FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState,
      FireVisualEffectRuntimeState fireVisualEffectRuntimeState,
      PrometheusWorldLoadState prometheusWorldLoadState) {
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
      _fireVisualEffectRuntimeState = fireVisualEffectRuntimeState;
      _prometheusWorldLoadState = prometheusWorldLoadState;
    }

    public void Awake() {
      var fireProfile = GetComponent<FireProfile>();
      _isCropProfile = fireProfile != null
                       && fireProfile.StructureKind.IndexOf("crop", System.StringComparison.OrdinalIgnoreCase) >= 0;
      _isTreeProfile = fireProfile != null
                       && fireProfile.StructureKind.IndexOf("tree", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public void Update() {
      if (!EnsureWorldReadyAndInitialized()) {
        return;
      }

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      var projection = _fireRuntimeProjectionRuntimeState.TryGetSnapshot(entityId, out var projectionSnapshot)
        ? projectionSnapshot
        : FireRuntimeProjectionRules.EmptyProjection;

      var intensity = FireVisualEffectRules.ComputeIntensity(projection, _fireVisualEffectRuntimeState.CurrentTuning);
      LogVisualIntensity(entityId, projection, intensity);
      LogSurfaceMaterialDiscovery(intensity);
      ApplyTreeSurfaceTexture(projection);
      var tuning = _fireVisualEffectRuntimeState.CurrentTuning;
      _emberEffect.ApplyTuning(tuning, _effectBaseHeight);
      _smokeEffect.ApplyTuning(tuning, _effectBaseHeight);
      _fireEffect.ApplyTuning(tuning, _effectBaseHeight);
      _steamEffect.ApplyTuning(tuning, _effectBaseHeight);
      _emberEffect.ApplyIntensity(intensity.Embers, 0.75f, 1.4f, tuning.EffectSize);
      _smokeEffect.ApplyIntensity(_isCropProfile ? 0f : intensity.Smoke, 1.2f, 2.6f, tuning.EffectSize);
      _fireEffect.ApplyIntensity(intensity.Fire, 0.45f, 1.0f, tuning.EffectSize);
      _steamEffect.ApplyIntensity(intensity.Steam, 0.9f, 2.0f, tuning.EffectSize);
    }

    private void LogVisualIntensity(
      int entityId,
      FireRuntimeProjectionSnapshot projection,
      FireVisualEffectIntensity intensity) {
      if (_visualTelemetryCount >= VisualTelemetryBudget) {
        return;
      }

      if (Time.realtimeSinceStartup - _lastVisualTelemetryTime < VisualTelemetryIntervalInSeconds) {
        return;
      }

      if (intensity.Smoke <= 0.01f
          && intensity.Embers <= 0.01f
          && intensity.Steam <= 0.01f
          && intensity.Fire <= 0.01f) {
        return;
      }

      _lastVisualTelemetryTime = Time.realtimeSinceStartup;
      _visualTelemetryCount++;
      var damage = projection.VisualDamageState;
      var exposure = projection.VisualExposure;
      FireTelemetry.Log($"event={FireTelemetryEvents.VisualRuntimeIntensity} entity={GameObject.name} id={entityId} isCropProfile={_isCropProfile} damageCategory={damage.Category.ToString().ToLowerInvariant()} damageState={damage.State.ToString().ToLowerInvariant()} burning={exposure.Burning} visualEmbers={intensity.Embers:0.000} visualSmoke={intensity.Smoke:0.000} visualFire={intensity.Fire:0.000} visualSteam={intensity.Steam:0.000} visualChar={intensity.Char:0.000} visualDesiccation={intensity.Desiccation:0.000} exposureHeat={exposure.HeatExposure:0.000} exposureEmber={exposure.EmberPressure:0.000} exposureSmoke={exposure.Smoke:0.000} exposureIgnition={exposure.IgnitionProgress:0.000}");
    }

    internal void DebugResetVisualEffects() {
      var tuning = _fireVisualEffectRuntimeState.CurrentTuning;
      _emberEffect?.ApplyIntensity(0f, 0.75f, 1.4f, tuning.EffectSize);
      _smokeEffect?.ApplyIntensity(0f, 1.2f, 2.6f, tuning.EffectSize);
      _fireEffect?.ApplyIntensity(0f, 0.45f, 1.0f, tuning.EffectSize);
      _steamEffect?.ApplyIntensity(0f, 0.9f, 2.0f, tuning.EffectSize);
      ApplyTreeSurfaceTexture(FireRuntimeProjectionRules.EmptyProjection);
    }

    private void LogSurfaceMaterialDiscovery(FireVisualEffectIntensity intensity) {
      if (!_isTreeProfile
          || _loggedSurfaceMaterials
          || _surfaceMaterialTelemetryCount >= SurfaceMaterialTelemetryBudget
          || intensity.Desiccation <= 0.1f && intensity.Char <= 0.1f) {
        return;
      }

      _loggedSurfaceMaterials = true;
      var renderers = GameObject.GetComponentsInChildren<Renderer>(true);
      for (var i = 0; i < renderers.Length && _surfaceMaterialTelemetryCount < SurfaceMaterialTelemetryBudget; i++) {
        var renderer = renderers[i];
        if (renderer == null || renderer is ParticleSystemRenderer) {
          continue;
        }

        var sharedMaterials = renderer.sharedMaterials;
        for (var materialIndex = 0; materialIndex < sharedMaterials.Length && _surfaceMaterialTelemetryCount < SurfaceMaterialTelemetryBudget; materialIndex++) {
          var material = sharedMaterials[materialIndex];
          if (material == null) {
            continue;
          }

          _surfaceMaterialTelemetryCount++;
          FireTelemetry.Log($"event=visual_surface_material_discovered entity={GameObject.name} renderer=\"{FireResetRegistry.EscapeToken(renderer.name)}\" materialIndex={materialIndex} material=\"{FireResetRegistry.EscapeToken(material.name)}\" shader=\"{FireResetRegistry.EscapeToken(material.shader == null ? "none" : material.shader.name)}\" shaderProps=\"{DiscoverShaderProperties(material)}\" textureProps=\"{DiscoverTextureProperties(material)}\" renderQueue={material.renderQueue} desiccation={intensity.Desiccation:0.000} char={intensity.Char:0.000}");
        }
      }
    }

    private static string DiscoverShaderProperties(Material material) {
      if (material.shader == null) {
        return string.Empty;
      }

      var propertyCount = material.shader.GetPropertyCount();
      return string.Join(
        ",",
        Enumerable.Range(0, propertyCount)
          .Select(index => DiscoverShaderProperty(material, index)));
    }

    private static string DiscoverShaderProperty(Material material, int index) {
      var shader = material.shader;
      var name = shader.GetPropertyName(index);
      var propertyId = shader.GetPropertyNameId(index);
      var type = shader.GetPropertyType(index);
      var value = type switch {
        ShaderPropertyType.Color => ColorToTelemetry(material.GetColor(propertyId)),
        ShaderPropertyType.Float => material.GetFloat(propertyId).ToString("0.###"),
        ShaderPropertyType.Range => material.GetFloat(propertyId).ToString("0.###"),
        ShaderPropertyType.Int => material.GetInt(propertyId).ToString(),
        ShaderPropertyType.Vector => VectorToTelemetry(material.GetVector(propertyId)),
        ShaderPropertyType.Texture => TextureToTelemetry(material.GetTexture(propertyId)),
        _ => "unsupported",
      };
      return $"{name}:{type.ToString().ToLowerInvariant()}={value}";
    }

    private static string DiscoverTextureProperties(Material material) =>
      string.Join(
        ",",
        new[] { "_BaseMap", "_MainTex", "_Albedo", "_MaskMap", "_BumpMap", "_EmissionMap", "_Palette", "_ColorRamp" }
          .Where(name => material.HasProperty(name))
          .Select(name => $"{name}:{TextureToTelemetry(material.GetTexture(name))}"));

    private static string ColorToTelemetry(Color color) =>
      $"{color.r:0.###}/{color.g:0.###}/{color.b:0.###}/{color.a:0.###}";

    private static string VectorToTelemetry(Vector4 vector) =>
      $"{vector.x:0.###}/{vector.y:0.###}/{vector.z:0.###}/{vector.w:0.###}";

    private static string TextureToTelemetry(Texture texture) =>
      texture == null
        ? "none"
        : $"{FireResetRegistry.EscapeToken(texture.name)}:{texture.width}x{texture.height}";

    private void ApplyTreeSurfaceTexture(FireRuntimeProjectionSnapshot projection) {
      if (!_isTreeProfile || _treeSurfaceStates.Count == 0) {
        return;
      }

      var stage = projection.HasDamageState
        ? FireNaturalResourceVisualRules.DetermineTreeStage(projection.VisualDamageState, projection.VisualExposure)
        : FireNaturalResourceVisualStage.Healthy;
      if (stage == _lastAppliedSurfaceStage) {
        return;
      }

      _lastAppliedSurfaceStage = stage;
      var useBlackTexture = stage is FireNaturalResourceVisualStage.DeadAndCharred
        or FireNaturalResourceVisualStage.StumpAndCharred;
      var targetTexture = useBlackTexture ? CharredPineTexture : null;
      _treeSurfaceStates.ForEach(state => state.Apply(targetTexture));
      LogSurfaceTextureSwap(stage, useBlackTexture);
    }

    private void LogSurfaceTextureSwap(FireNaturalResourceVisualStage stage, bool useBlackTexture) {
      if (_surfaceMaterialTextureTelemetryCount >= SurfaceMaterialTextureTelemetryBudget) {
        return;
      }

      _surfaceMaterialTextureTelemetryCount++;
      FireTelemetry.Log($"event=visual_surface_material_texture_applied entity={GameObject.name} stage={stage.ToString().ToLowerInvariant()} renderers={_treeSurfaceStates.Count} mainTex=\"{(useBlackTexture ? TextureToTelemetry(CharredPineTexture) : "original")}\"");
    }

    private static Texture2D CharredPineTexture {
      get {
        if (_charredPineTexture != null) {
          return _charredPineTexture;
        }

        const int textureSize = 128;
        _charredPineTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false) {
          name = "PrometheusCharredPineBark",
          hideFlags = HideFlags.HideAndDontSave
        };
        for (var y = 0; y < textureSize; y++) {
          for (var x = 0; x < textureSize; x++) {
            _charredPineTexture.SetPixel(x, y, ComputeCharredPinePixel(x, y));
          }
        }

        _charredPineTexture.wrapMode = TextureWrapMode.Repeat;
        _charredPineTexture.filterMode = FilterMode.Bilinear;
        _charredPineTexture.Apply(false, true);
        return _charredPineTexture;
      }
    }

    private static Color ComputeCharredPinePixel(int x, int y) {
      var verticalGrain = Mathf.PerlinNoise(x * 0.075f, y * 0.015f);
      var barkGrooves = Mathf.PerlinNoise(x * 0.18f, y * 0.045f);
      var ashNoise = Mathf.PerlinNoise((x + 97) * 0.42f, (y + 31) * 0.42f);
      var exposedBark = Mathf.PerlinNoise((x + 211) * 0.12f, y * 0.035f);

      var charcoal = Mathf.Clamp01(0.055f + verticalGrain * 0.055f - barkGrooves * 0.04f);
      var color = new Color(charcoal, charcoal * 0.92f, charcoal * 0.78f, 1f);
      if (exposedBark > 0.72f && barkGrooves > 0.45f) {
        var exposed = Mathf.InverseLerp(0.72f, 1f, exposedBark);
        color = Color.Lerp(color, new Color(0.34f, 0.17f, 0.07f, 1f), exposed * 0.55f);
      }

      if (ashNoise > 0.86f) {
        var ash = Mathf.InverseLerp(0.86f, 1f, ashNoise);
        color = Color.Lerp(color, new Color(0.36f, 0.35f, 0.32f, 1f), ash * 0.45f);
      }

      return color;
    }

    private bool EnsureWorldReadyAndInitialized() {
      if (_prometheusWorldLoadState?.WorldReady != true) {
        return false;
      }

      if (_emberEffect is not null) {
        return true;
      }

      CaptureRenderers();
      CreateParticleSystems();
      return true;
    }

    private void CaptureRenderers() {
      if (_initializedRenderers) {
        return;
      }

      _initializedRenderers = true;
      var renderers = GameObject.GetComponentsInChildren<Renderer>(true);
      var maxY = float.MinValue;
      for (var i = 0; i < renderers.Length; i++) {
        var renderer = renderers[i];
        if (renderer == null || renderer is ParticleSystemRenderer) {
          continue;
        }

        if (_isTreeProfile) {
          var surfaceState = TreeSurfaceTextureState.TryCreate(renderer);
          if (surfaceState != null) {
            _treeSurfaceStates.Add(surfaceState);
          }
        }

        if (renderer.gameObject.activeInHierarchy) {
          maxY = Mathf.Max(maxY, renderer.bounds.max.y);
        }
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

    private sealed class TreeSurfaceTextureState {

      private readonly MaterialTextureState[] _materialStates;

      private TreeSurfaceTextureState(MaterialTextureState[] materialStates) {
        _materialStates = materialStates;
      }

      public static TreeSurfaceTextureState TryCreate(Renderer renderer) {
        var materialStates = renderer.materials
          .Select(MaterialTextureState.TryCreate)
          .Where(state => state != null)
          .ToArray();
        return materialStates.Length == 0 ? null : new TreeSurfaceTextureState(materialStates);
      }

      public void Apply(Texture targetTexture) =>
        System.Array.ForEach(_materialStates, state => state.Apply(targetTexture));

    }

    private sealed class MaterialTextureState {

      private readonly Material _material;
      private readonly Texture _originalMainTexture;

      private MaterialTextureState(Material material, Texture originalMainTexture) {
        _material = material;
        _originalMainTexture = originalMainTexture;
      }

      public static MaterialTextureState TryCreate(Material material) {
        if (material == null
            || material.shader == null
            || material.shader.name != "Shader Graphs/VegetationURP"
            || !material.HasProperty(MainTexturePropertyId)) {
          return null;
        }

        return new MaterialTextureState(material, material.GetTexture(MainTexturePropertyId));
      }

      public void Apply(Texture targetTexture) {
        if (_material == null) {
          return;
        }

        _material.SetTexture(MainTexturePropertyId, targetTexture ?? _originalMainTexture);
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
        if (SourcesByKind.TryGetValue(kind, out var source)) {
          return source;
        }

        source = FireNativeParticleSourceCatalog.TryGetRecommendedSource(ToVisualEffectKind(kind));
        if (source != null) {
          SourcesByKind[kind] = source;
        }

        return source;
      }

      private static FireVisualEffectKind ToVisualEffectKind(NativeParticleEffectKind kind) {
        return kind switch {
          NativeParticleEffectKind.Embers => FireVisualEffectKind.Sparks,
          NativeParticleEffectKind.Smoke => FireVisualEffectKind.Smoke,
          NativeParticleEffectKind.Fire => FireVisualEffectKind.Fire,
          NativeParticleEffectKind.Steam => FireVisualEffectKind.Steam,
          _ => FireVisualEffectKind.Smoke,
        };
      }

    }

  }
}
