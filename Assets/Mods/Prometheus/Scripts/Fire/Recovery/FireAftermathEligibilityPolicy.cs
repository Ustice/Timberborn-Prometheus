namespace Mods.Prometheus.Scripts {
  internal enum FireAftermathSourceKind {
    Unknown,
    CharredTree,
    CharredBuilding,
    Terrain,
    TopSurface,
    ExcludedObject,
  }

  internal enum FireAftermathEligibilityStatus {
    Ineligible,
    Eligible,
    Placeholder,
  }

  internal readonly struct FireAftermathEligibilityCandidate {

    public FireGridStructureKind StructureKind { get; }
    public FireDamageCategory DamageCategory { get; }
    public FireDamageState DamageState { get; }
    public bool BurnedOut { get; }
    public bool TerrainSampleAvailable { get; }
    public bool TopSurfaceSampleAvailable { get; }

    public FireAftermathEligibilityCandidate(
      FireGridStructureKind structureKind,
      FireDamageCategory damageCategory,
      FireDamageState damageState,
      bool burnedOut,
      bool terrainSampleAvailable = false,
      bool topSurfaceSampleAvailable = false) {
      StructureKind = structureKind;
      DamageCategory = damageCategory;
      DamageState = damageState;
      BurnedOut = burnedOut;
      TerrainSampleAvailable = terrainSampleAvailable;
      TopSurfaceSampleAvailable = topSurfaceSampleAvailable;
    }

  }

  internal readonly struct FireAftermathEligibilityResult {

    public FireAftermathEligibilityStatus Status { get; }
    public FireAftermathSourceKind SourceKind { get; }
    public string Reason { get; }

    public bool CanProduceFertileAsh => Status == FireAftermathEligibilityStatus.Eligible;
    public bool IsPlaceholder => Status == FireAftermathEligibilityStatus.Placeholder;

    public FireAftermathEligibilityResult(
      FireAftermathEligibilityStatus status,
      FireAftermathSourceKind sourceKind,
      string reason) {
      Status = status;
      SourceKind = sourceKind;
      Reason = string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason;
    }

  }

  internal static class FireAftermathEligibilityPolicy {

    internal static FireAftermathEligibilityResult Evaluate(FireAftermathEligibilityCandidate candidate) {
      if (candidate.TopSurfaceSampleAvailable) {
        return Placeholder(FireAftermathSourceKind.TopSurface, "top_surface_adapter_pending");
      }

      if (candidate.TerrainSampleAvailable || candidate.StructureKind == FireGridStructureKind.Terrain) {
        return Placeholder(FireAftermathSourceKind.Terrain, "terrain_adapter_pending");
      }

      if (!candidate.BurnedOut || candidate.DamageState != FireDamageState.Dead) {
        return Ineligible(FireAftermathSourceKind.ExcludedObject, "not_charred");
      }

      if (candidate.StructureKind == FireGridStructureKind.Vegetation
          && candidate.DamageCategory == FireDamageCategory.Tree) {
        return Eligible(FireAftermathSourceKind.CharredTree, "charred_tree");
      }

      if (candidate.StructureKind == FireGridStructureKind.Building
          && candidate.DamageCategory == FireDamageCategory.Building) {
        return Eligible(FireAftermathSourceKind.CharredBuilding, "charred_building");
      }

      return Ineligible(FireAftermathSourceKind.ExcludedObject, "excluded_source");
    }

    private static FireAftermathEligibilityResult Eligible(
      FireAftermathSourceKind sourceKind,
      string reason) =>
      new(FireAftermathEligibilityStatus.Eligible, sourceKind, reason);

    private static FireAftermathEligibilityResult Placeholder(
      FireAftermathSourceKind sourceKind,
      string reason) =>
      new(FireAftermathEligibilityStatus.Placeholder, sourceKind, reason);

    private static FireAftermathEligibilityResult Ineligible(
      FireAftermathSourceKind sourceKind,
      string reason) =>
      new(FireAftermathEligibilityStatus.Ineligible, sourceKind, reason);

  }
}
