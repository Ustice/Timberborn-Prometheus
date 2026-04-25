using System;

namespace Mods.Prometheus.Scripts {
  internal static class FireWorkplaceRules {

    internal static bool IsWorkplaceSupportComponentName(string componentTypeName) {
      if (string.IsNullOrWhiteSpace(componentTypeName)) {
        return false;
      }

      return componentTypeName.Contains("Workplace", StringComparison.Ordinal)
             && !componentTypeName.Contains("Bonus", StringComparison.Ordinal);
    }

    internal static bool IsOperationalComponentName(string componentTypeName) {
      if (string.IsNullOrWhiteSpace(componentTypeName)) {
        return false;
      }

      if (componentTypeName.Contains("Fire", StringComparison.Ordinal)
          || componentTypeName.Contains("Workplace", StringComparison.Ordinal)
          || componentTypeName.Contains("Deteriorable", StringComparison.Ordinal)) {
        return false;
      }

      return componentTypeName.Contains("Manufactory", StringComparison.Ordinal)
             || componentTypeName.Contains("Workshop", StringComparison.Ordinal)
             || componentTypeName.Contains("Recipe", StringComparison.Ordinal);
    }

  }
}
