using System.Collections.Generic;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal enum TimberbornOperationState {
    Unknown,
    Active,
    Inactive,
  }

  internal readonly struct TimberbornOperationalComponentState {

    public string ComponentTypeName { get; }
    public bool Enabled { get; }
    public bool SupportsEnabledState { get; }

    public TimberbornOperationalComponentState(
      string componentTypeName,
      bool enabled,
      bool supportsEnabledState = true) {
      ComponentTypeName = string.IsNullOrWhiteSpace(componentTypeName) ? "unknown" : componentTypeName;
      Enabled = enabled;
      SupportsEnabledState = supportsEnabledState;
    }

  }

  internal readonly struct TimberbornOperationStateSnapshot {

    public TimberbornOperationState State { get; }
    public int ComponentCount { get; }
    public int EnabledComponentCount { get; }
    public int UnknownComponentCount { get; }
    public string Detail { get; }

    public TimberbornOperationStateSnapshot(
      TimberbornOperationState state,
      int componentCount,
      int enabledComponentCount,
      int unknownComponentCount,
      string detail) {
      State = state;
      ComponentCount = componentCount;
      EnabledComponentCount = enabledComponentCount;
      UnknownComponentCount = unknownComponentCount;
      Detail = string.IsNullOrWhiteSpace(detail) ? "none" : detail;
    }

  }

  internal static class TimberbornOperationStateAdapter {

    internal static TimberbornOperationStateSnapshot Evaluate(GameObject gameObject) {
      if (gameObject == null) {
        return new TimberbornOperationStateSnapshot(TimberbornOperationState.Unknown, 0, 0, 0, "missing GameObject");
      }

      var componentStates = new List<TimberbornOperationalComponentState>();
      var unityComponents = gameObject.GetComponents<Component>();
      for (var i = 0; i < unityComponents.Length; i++) {
        AddComponentState(unityComponents[i], componentStates);
      }

#if !PROMETHEUS_TESTS
      foreach (var unityComponent in unityComponents) {
        if (!TimberbornComponentCacheLookup.TryGetCachedComponents(unityComponent, out var cachedComponents)) {
          continue;
        }

        foreach (var cachedComponent in cachedComponents) {
          AddObjectState(cachedComponent, componentStates);
        }
      }
#endif

      return Evaluate(componentStates);
    }

    internal static TimberbornOperationStateSnapshot Evaluate(IEnumerable<TimberbornOperationalComponentState> componentStates) {
      var componentCount = 0;
      var enabledComponentCount = 0;
      var unknownComponentCount = 0;
      foreach (var componentState in componentStates ?? new TimberbornOperationalComponentState[0]) {
        if (!TimberbornCompatibility.IsOperationalComponentName(componentState.ComponentTypeName)) {
          continue;
        }

        componentCount++;
        if (!componentState.SupportsEnabledState) {
          unknownComponentCount++;
          continue;
        }

        if (componentState.Enabled) {
          enabledComponentCount++;
        }
      }

      if (unknownComponentCount > 0) {
        return new TimberbornOperationStateSnapshot(
          TimberbornOperationState.Unknown,
          componentCount,
          enabledComponentCount,
          unknownComponentCount,
          "operational component state API missing");
      }

      if (componentCount == 0) {
        return new TimberbornOperationStateSnapshot(
          TimberbornOperationState.Unknown,
          0,
          0,
          0,
          "no operational components found");
      }

      return new TimberbornOperationStateSnapshot(
        enabledComponentCount > 0 ? TimberbornOperationState.Active : TimberbornOperationState.Inactive,
        componentCount,
        enabledComponentCount,
        0,
        enabledComponentCount > 0 ? "enabled operational component" : "all operational components disabled");
    }

    private static void AddComponentState(Component component, ICollection<TimberbornOperationalComponentState> componentStates) {
      if (component == null) {
        return;
      }

      AddObjectState(component, componentStates);
    }

    private static void AddObjectState(object component, ICollection<TimberbornOperationalComponentState> componentStates) {
      if (component == null) {
        return;
      }

      var componentTypeName = component.GetType().Name;
      if (!TimberbornCompatibility.IsOperationalComponentName(componentTypeName)) {
        return;
      }

      if (component is Behaviour behaviour) {
        componentStates.Add(new TimberbornOperationalComponentState(componentTypeName, behaviour.enabled));
        return;
      }

      componentStates.Add(new TimberbornOperationalComponentState(componentTypeName, false, false));
    }

  }
}
