using System.Collections.Generic;

namespace Mods.Prometheus.Scripts {
  internal static class FireFieldAmendmentGrowthRules {

    internal const float GrowthSpeedBonus = 0.1f;

    internal static bool IsEligibleCropGrowable(IEnumerable<string> componentTypeNames) {
      var hasGrowable = false;
      var hasCrop = false;
      foreach (var componentTypeName in componentTypeNames ?? System.Array.Empty<string>()) {
        if (TimberbornCompatibility.IsTreeComponentName(componentTypeName)
            || TimberbornCompatibility.IsBushComponentName(componentTypeName)) {
          return false;
        }

        if (TimberbornCompatibility.IsGrowableComponentName(componentTypeName)) {
          hasGrowable = true;
        }

        if (TimberbornCompatibility.IsCropComponentName(componentTypeName)) {
          hasCrop = true;
        }
      }

      return hasGrowable && hasCrop;
    }

    internal static float ComputeBoostedGrowthTimeInDays(float baseGrowthTimeInDays) =>
      baseGrowthTimeInDays <= 0f
        ? baseGrowthTimeInDays
        : baseGrowthTimeInDays / (1f + GrowthSpeedBonus);

  }
}
