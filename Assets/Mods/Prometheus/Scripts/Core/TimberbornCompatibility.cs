using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mods.Prometheus.Scripts {
  internal enum TimberbornCompatibilityProbeStatus {
    Unknown,
    Resolved,
    Missing,
    Deferred,
  }

  internal enum TimberbornCompatibilityArea {
    Damage,
    Recovery,
    Beaver,
    Workplace,
    Cache,
    Focus,
    Operation,
    Environment,
  }

  internal readonly struct TimberbornCompatibilityProbeResult {

    public TimberbornCompatibilityProbeStatus Status { get; }
    public string Detail { get; }

    public TimberbornCompatibilityProbeResult(TimberbornCompatibilityProbeStatus status, string detail) {
      Status = status;
      Detail = string.IsNullOrWhiteSpace(detail) ? "none" : detail.Trim();
    }

  }

  internal readonly struct TimberbornNeedManagerApi {

    public MethodInfo ManagerAddPointsMethod { get; }
    public MethodInfo GetNeedMethod { get; }
    public MethodInfo TryGetNeedMethod { get; }
    public MethodInfo NeedAddPointsMethod { get; }
    public MethodInfo NeedSetPointsMethod { get; }
    public string Description { get; }

    public bool IsResolved =>
      ManagerAddPointsMethod is not null
      || (GetNeedMethod is not null && NeedAddPointsMethod is not null)
      || (TryGetNeedMethod is not null && NeedAddPointsMethod is not null);

    public bool CanSetNeedPoints => NeedSetPointsMethod is not null;

    public TimberbornNeedManagerApi(
      MethodInfo managerAddPointsMethod,
      MethodInfo getNeedMethod,
      MethodInfo tryGetNeedMethod,
      MethodInfo needAddPointsMethod,
      MethodInfo needSetPointsMethod,
      string description) {
      ManagerAddPointsMethod = managerAddPointsMethod;
      GetNeedMethod = getNeedMethod;
      TryGetNeedMethod = tryGetNeedMethod;
      NeedAddPointsMethod = needAddPointsMethod;
      NeedSetPointsMethod = needSetPointsMethod;
      Description = string.IsNullOrWhiteSpace(description) ? "missing" : description;
    }

  }

  internal static class TimberbornCompatibility {

    internal const string ComponentCacheTypeName = "ComponentCache";
    internal const string NeedManagerTypeName = "NeedManager";
    internal const string DeteriorableTypeName = "Deteriorable";
    internal const string GrowableTypeName = "Growable";
    internal const string LivingNaturalResourceTypeName = "LivingNaturalResource";
    internal const string TreeComponentTypeName = "TreeComponent";

    private static readonly BindingFlags InstanceBindingFlags =
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Dictionary<TimberbornCompatibilityArea, TimberbornCompatibilityProbeResult> ProbeResults =
      CreateInitialProbeResults();

    private static readonly HashSet<string> LoggedProbeTransitions = new();
    private static bool _loggedStartupSummary;

    internal static TimberbornCompatibilityProbeResult NormalizeProbeResult(bool resolved, string detail) =>
      new(resolved ? TimberbornCompatibilityProbeStatus.Resolved : TimberbornCompatibilityProbeStatus.Missing, detail);

    internal static bool IsComponentCacheTypeName(string componentTypeName) =>
      string.Equals(componentTypeName, ComponentCacheTypeName, StringComparison.Ordinal);

    internal static bool IsNeedManagerTypeName(string componentTypeName) =>
      string.Equals(componentTypeName, NeedManagerTypeName, StringComparison.Ordinal);

    internal static bool IsTreeComponentName(string componentTypeName) =>
      string.Equals(componentTypeName, TreeComponentTypeName, StringComparison.Ordinal);

    internal static bool IsGrowableComponentName(string componentTypeName) =>
      string.Equals(componentTypeName, GrowableTypeName, StringComparison.Ordinal);

    internal static bool IsDeteriorableComponentName(string componentTypeName) =>
      string.Equals(componentTypeName, DeteriorableTypeName, StringComparison.Ordinal);

    internal static bool IsLivingNaturalResourceComponentName(string componentTypeName) =>
      string.Equals(componentTypeName, LivingNaturalResourceTypeName, StringComparison.Ordinal);

    internal static bool IsBuildingDamageComponentName(string componentTypeName) {
      if (string.IsNullOrWhiteSpace(componentTypeName)) {
        return false;
      }

      return componentTypeName.Contains("Deteriorable", StringComparison.Ordinal)
             || componentTypeName.Contains("WorkplaceBonuses", StringComparison.Ordinal)
             || componentTypeName.Contains("Manufactory", StringComparison.Ordinal)
             || componentTypeName.Contains("Workshop", StringComparison.Ordinal);
    }

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

    internal static FireDamageCategory ClassifyDamageCategory(IEnumerable<string> componentTypeNames, bool hasWorkplaceComponent) {
      if (hasWorkplaceComponent) {
        return FireDamageCategory.Building;
      }

      var hasGrowable = false;
      var hasBuildingComponent = false;
      foreach (var componentTypeName in componentTypeNames ?? Array.Empty<string>()) {
        if (IsTreeComponentName(componentTypeName)) {
          return FireDamageCategory.Tree;
        }

        if (IsGrowableComponentName(componentTypeName)) {
          hasGrowable = true;
          continue;
        }

        if (IsBuildingDamageComponentName(componentTypeName)) {
          hasBuildingComponent = true;
        }
      }

      if (hasGrowable) {
        return FireDamageCategory.Crop;
      }

      return hasBuildingComponent ? FireDamageCategory.Building : FireDamageCategory.Unknown;
    }

    internal static MethodInfo FindMethod(Type type, string name, params Type[] parameterTypes) {
      if (type is null || string.IsNullOrWhiteSpace(name)) {
        return null;
      }

      if (parameterTypes is null || parameterTypes.Length == 0) {
        return type.GetMethod(name, InstanceBindingFlags);
      }

      return type.GetMethod(name, InstanceBindingFlags, null, parameterTypes, null);
    }

    internal static PropertyInfo FindProperty(Type type, string name) =>
      type is null || string.IsNullOrWhiteSpace(name)
        ? null
        : type.GetProperty(name, InstanceBindingFlags);

    internal static TimberbornNeedManagerApi ProbeNeedManagerApi(Type needManagerType) {
      var managerAddPointsCandidate = FindMethod(needManagerType, "AddPoints", typeof(string), typeof(float));
      if (managerAddPointsCandidate is not null) {
        return new TimberbornNeedManagerApi(
          managerAddPointsCandidate,
          null,
          null,
          null,
          null,
          "NeedManager.AddPoints(string,float)");
      }

      var getNeedCandidate = FindMethod(needManagerType, "GetNeed", typeof(string));
      if (getNeedCandidate is not null && TryBindNeedMethods(getNeedCandidate.ReturnType, out var getNeedAddPoints, out var getNeedSetPoints)) {
        return new TimberbornNeedManagerApi(
          null,
          getNeedCandidate,
          null,
          getNeedAddPoints,
          getNeedSetPoints,
          "NeedManager.GetNeed(string) + Need.AddPoints(float)");
      }

      var tryGetNeedCandidate = FindMethod(needManagerType, "TryGetNeed");
      if (tryGetNeedCandidate is null) {
        return new TimberbornNeedManagerApi(null, null, null, null, null, "NeedManager API missing");
      }

      var parameters = tryGetNeedCandidate.GetParameters();
      if (parameters.Length != 2
          || parameters[0].ParameterType != typeof(string)
          || !parameters[1].ParameterType.IsByRef) {
        return new TimberbornNeedManagerApi(null, null, null, null, null, "NeedManager.TryGetNeed signature mismatch");
      }

      var needType = parameters[1].ParameterType.GetElementType();
      if (!TryBindNeedMethods(needType, out var tryGetNeedAddPoints, out var tryGetNeedSetPoints)) {
        return new TimberbornNeedManagerApi(null, null, null, null, null, "Need.AddPoints(float) missing");
      }

      return new TimberbornNeedManagerApi(
        null,
        null,
        tryGetNeedCandidate,
        tryGetNeedAddPoints,
        tryGetNeedSetPoints,
        "NeedManager.TryGetNeed + Need.AddPoints(float)");
    }

    internal static void LogStartupSummary() {
      if (_loggedStartupSummary) {
        return;
      }

      _loggedStartupSummary = true;
      FireTelemetry.Log(CreateSummaryMessage());
    }

    internal static void RecordProbe(TimberbornCompatibilityArea area, bool resolved, string detail) =>
      RecordProbe(area, NormalizeProbeResult(resolved, detail));

    internal static void RecordProbe(TimberbornCompatibilityArea area, TimberbornCompatibilityProbeResult result) {
      ProbeResults[area] = result;
      var transitionKey = $"{area}:{result.Status}:{result.Detail}";
      if (!LoggedProbeTransitions.Add(transitionKey)) {
        return;
      }

      var log = result.Status == TimberbornCompatibilityProbeStatus.Missing
        ? (Action<string>)FireTelemetry.LogWarning
        : FireTelemetry.Log;
      log($"event=timberborn_compatibility_probe area={FormatArea(area)} status={FormatStatus(result.Status)} detail=\"{Escape(result.Detail)}\"");
    }

    private static Dictionary<TimberbornCompatibilityArea, TimberbornCompatibilityProbeResult> CreateInitialProbeResults() =>
      new() {
        { TimberbornCompatibilityArea.Damage, new TimberbornCompatibilityProbeResult(TimberbornCompatibilityProbeStatus.Deferred, "Deteriorable/Growable/LivingNaturalResource runtime probe") },
        { TimberbornCompatibilityArea.Recovery, new TimberbornCompatibilityProbeResult(TimberbornCompatibilityProbeStatus.Deferred, "Growable.GrowthTimeInDays runtime probe") },
        { TimberbornCompatibilityArea.Beaver, new TimberbornCompatibilityProbeResult(TimberbornCompatibilityProbeStatus.Deferred, "NeedManager runtime probe") },
        { TimberbornCompatibilityArea.Workplace, new TimberbornCompatibilityProbeResult(TimberbornCompatibilityProbeStatus.Resolved, "Workplace typed component and Worker speed API") },
        { TimberbornCompatibilityArea.Cache, new TimberbornCompatibilityProbeResult(TimberbornCompatibilityProbeStatus.Deferred, "ComponentCache _components/AllComponents runtime probe") },
        { TimberbornCompatibilityArea.Focus, new TimberbornCompatibilityProbeResult(TimberbornCompatibilityProbeStatus.Resolved, "EntitySelectionService.SelectAndFocusOn") },
        { TimberbornCompatibilityArea.Operation, new TimberbornCompatibilityProbeResult(TimberbornCompatibilityProbeStatus.Resolved, "type-name operation classifier") },
        { TimberbornCompatibilityArea.Environment, new TimberbornCompatibilityProbeResult(TimberbornCompatibilityProbeStatus.Deferred, "terrain/block/water/soil runtime probe") },
      };

    private static bool TryBindNeedMethods(Type needType, out MethodInfo needAddPointsMethod, out MethodInfo needSetPointsMethod) {
      needAddPointsMethod = FindMethod(needType, "AddPoints", typeof(float));
      needSetPointsMethod = FindMethod(needType, "SetPoints", typeof(float));
      return needAddPointsMethod is not null;
    }

    private static string CreateSummaryMessage() =>
      $"event=timberborn_compatibility_summary damage={FormatProbe(TimberbornCompatibilityArea.Damage)} recovery={FormatProbe(TimberbornCompatibilityArea.Recovery)} beaver={FormatProbe(TimberbornCompatibilityArea.Beaver)} workplace={FormatProbe(TimberbornCompatibilityArea.Workplace)} cache={FormatProbe(TimberbornCompatibilityArea.Cache)} focus={FormatProbe(TimberbornCompatibilityArea.Focus)} operation={FormatProbe(TimberbornCompatibilityArea.Operation)} environment={FormatProbe(TimberbornCompatibilityArea.Environment)}";

    private static string FormatProbe(TimberbornCompatibilityArea area) {
      var result = ProbeResults[area];
      return $"{FormatStatus(result.Status)}:{EscapeToken(result.Detail)}";
    }

    private static string FormatArea(TimberbornCompatibilityArea area) =>
      area.ToString().ToLowerInvariant();

    private static string FormatStatus(TimberbornCompatibilityProbeStatus status) =>
      status.ToString().ToLowerInvariant();

    private static string Escape(string value) =>
      (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeToken(string value) =>
      Escape(value).Replace(' ', '_');

  }
}
