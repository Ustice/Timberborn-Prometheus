namespace Mods.Prometheus.Scripts {
  internal static class FireWorkplaceRules {

    internal static bool IsWorkplaceSupportComponentName(string componentTypeName) =>
      TimberbornCompatibility.IsWorkplaceSupportComponentName(componentTypeName);

    internal static bool IsOperationalComponentName(string componentTypeName) =>
      TimberbornCompatibility.IsOperationalComponentName(componentTypeName);

  }
}
