using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal static class FireNativeParticleSourceCatalog {

    private static readonly Dictionary<string, GameObject> SourcesByName = new();
    private static bool _searched;

    public static string[] GetRecommendedSources(FireVisualEffectKind kind) => kind switch {
      FireVisualEffectKind.Smoke => new[] { "FoodFactorySmoke", "SmelterSmoke", "BakerySmoke", "CoffeeBrewerySmoke", "DeconstructionSmoke", "GrillSmoke", "RefinerySmoke" },
      FireVisualEffectKind.Ash => new[] { "BadwaterRigSmoke", "Building Dust", "Dust", "BakerySmoke", "DeconstructionSmoke", "DirtExcavationGlobalDust" },
      FireVisualEffectKind.Steam => new[] { "CoffeeBrewerySmoke", "SteamEngineSmoke", "GeothermalEngineSmoke", "GeothermalFieldSmoke", "Smoke" },
      FireVisualEffectKind.Fire => new[] { "CampfireFire", "BrazierFire", "BrazierOfBondingFire", "FlameOfUnityFire", "Flames", "Fire" },
      FireVisualEffectKind.Sparks => new[] { "Sparks_Trail", "Sparks_Burst", "BotAssemblerSparks", "Common_Trail_Sparks", "TeethGrindstoneSparks.Folktails", "TeethGrindstoneSparks.IronTeeth" },
      _ => System.Array.Empty<string>(),
    };

    public static string[] GetAllSourceNames() {
      EnsureSearched();
      return SourcesByName.Keys.OrderBy(name => name).ToArray();
    }

    public static GameObject TryGetSource(string sourceName) {
      EnsureSearched();
      return !string.IsNullOrWhiteSpace(sourceName) && SourcesByName.TryGetValue(sourceName, out var source) ? source : null;
    }

    public static GameObject TryGetRecommendedSource(FireVisualEffectKind kind) {
      EnsureSearched();
      return SourcesByName.Values
        .Select(source => new NativeParticleCandidate(source, ScoreParticleObject(kind, source)))
        .Where(candidate => candidate.Score > 0)
        .OrderByDescending(candidate => candidate.Score)
        .ThenBy(candidate => candidate.SourceRoot.name)
        .Select(candidate => candidate.SourceRoot)
        .FirstOrDefault();
    }

    internal static int ScoreSearchableText(FireVisualEffectKind kind, string searchableText, bool firstParticleAlphaIsSoft = false) {
      if (string.IsNullOrWhiteSpace(searchableText)) {
        return 0;
      }

      var searchable = searchableText.ToLowerInvariant();
      return kind switch {
        FireVisualEffectKind.Sparks => Score(searchable, "sparks_trail", 120, "common_trail_sparks", 110, "spark", 80, "firework", 35),
        FireVisualEffectKind.Smoke => Score(searchable, "smeltersmoke", 130, "bakerysmoke", 120, "smoke", 100, "exhaust", 35) - Score(searchable, "explosion", 55, "steam", 35),
        FireVisualEffectKind.Fire => Score(searchable, "campfirefire", 140, "brazierfire", 130, "fire", 100, "flame", 95) - Score(searchable, "firework", 70, "spark", 25),
        FireVisualEffectKind.Steam => Score(searchable, "steamenginesmoke", 130, "geothermal", 95, "steam", 90, "smoke_soft", 45) + (firstParticleAlphaIsSoft ? 5 : 0),
        FireVisualEffectKind.Ash => Score(searchable, "badwaterrigsmoke", 130, "building dust", 120, "dust", 100, "dirt", 55) + Score(searchable, "deconstructionsmoke", 45, "bakerysmoke", 30),
        _ => 0,
      };
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
        .Where(particleSystem => particleSystem != null && IsNativeCandidate(particleSystem.gameObject))
        .Select(particleSystem => particleSystem.gameObject)
        .ToArray();
      var particleObjects = resourceParticlePrefabs
        .Concat(loadedParticleObjects)
        .GroupBy(gameObject => gameObject.GetInstanceID())
        .Select(group => group.First());

      foreach (var particleObject in particleObjects) {
        if (!SourcesByName.ContainsKey(particleObject.name)) {
          SourcesByName[particleObject.name] = particleObject;
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

    private static int ScoreParticleObject(FireVisualEffectKind kind, GameObject gameObject) {
      var particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>(true);
      var names = particleSystems
        .Select(particleSystem => GetHierarchyName(particleSystem.transform))
        .Concat(new[] { GetHierarchyName(gameObject.transform) });
      var materialNames = particleSystems
        .Select(particleSystem => particleSystem.GetComponent<ParticleSystemRenderer>())
        .Where(renderer => renderer != null && renderer.sharedMaterial != null)
        .Select(renderer => renderer.sharedMaterial.name);
      var searchable = string.Join(" ", names.Concat(materialNames));
      var main = particleSystems.Length == 0 ? default : particleSystems[0].main;
      return ScoreSearchableText(kind, searchable, main.startColor.color.a < 0.65f);
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
