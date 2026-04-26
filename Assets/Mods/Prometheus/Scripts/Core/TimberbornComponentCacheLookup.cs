using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal static class TimberbornComponentCacheLookup {

    private const string ComponentCacheTypeName = "ComponentCache";

    private static readonly BindingFlags CacheMemberBindingFlags =
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static IEnumerable<CachedComponentCache> FindLoadedComponentCaches() {
      var unityComponents = Object.FindObjectsByType<Component>(FindObjectsSortMode.None);
      for (var i = 0; i < unityComponents.Length; i++) {
        var unityComponent = unityComponents[i];
        if (!IsComponentCache(unityComponent)) {
          continue;
        }

        yield return new CachedComponentCache(
          unityComponent,
          TryGetCachedComponents(unityComponent, out var cachedComponents)
            ? cachedComponents
            : null);
      }
    }

    internal static IEnumerable<FireExposureController> FindLoadedFireExposureControllers() {
      foreach (var componentCache in FindLoadedComponentCaches()) {
        if (!componentCache.HasCachedComponents) {
          continue;
        }

        foreach (var component in componentCache.CachedComponents) {
          if (component is FireExposureController fireExposureController) {
            yield return fireExposureController;
          }
        }
      }
    }

    internal static IEnumerable<GameObject> FindLoadedPrometheusFireEntityGameObjects() {
      var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
      for (var i = 0; i < allObjects.Length; i++) {
        var gameObject = allObjects[i];
        if (gameObject == null || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded) {
          continue;
        }

        if (HasPrometheusFireComponent(gameObject)) {
          yield return gameObject;
        }
      }
    }

    internal static bool HasPrometheusFireComponent(GameObject gameObject) {
      if (gameObject == null) {
        return false;
      }

      if (gameObject.GetComponent<FireExposureController>() is not null
          || gameObject.GetComponent<FireDamageStateController>() is not null
          || gameObject.GetComponent<FireDamageEffectApplier>() is not null
          || gameObject.GetComponent<FireWorkplaceEffectApplier>() is not null
          || gameObject.GetComponent<FireRecoveryController>() is not null
          || gameObject.GetComponent<FireRecoveryEffectApplier>() is not null) {
        return true;
      }

      var componentCache = gameObject.GetComponent<ComponentCache>();
      return componentCache is not null
             && (componentCache.TryGetCachedComponent<FireExposureController>(out _)
                 || componentCache.TryGetCachedComponent<FireDamageStateController>(out _)
                 || componentCache.TryGetCachedComponent<FireDamageEffectApplier>(out _)
                 || componentCache.TryGetCachedComponent<FireVisualEffectApplier>(out _)
                 || componentCache.TryGetCachedComponent<FireWorkplaceEffectApplier>(out _)
                 || componentCache.TryGetCachedComponent<FireRecoveryController>(out _)
                 || componentCache.TryGetCachedComponent<FireRecoveryEffectApplier>(out _));
    }

    internal static bool TryGetPrometheusFireComponent<TComponent>(
      GameObject gameObject,
      out TComponent component)
      where TComponent : BaseComponent =>
      TryGetPrometheusFireComponent(gameObject, out component, out _);

    internal static bool TryGetPrometheusFireComponent<TComponent>(
      GameObject gameObject,
      out TComponent component,
      out bool fromComponentCache)
      where TComponent : BaseComponent {
      component = null;
      fromComponentCache = false;
      if (gameObject == null) {
        return false;
      }

      var componentCache = gameObject.GetComponent<ComponentCache>();
      if (componentCache is not null && componentCache.TryGetCachedComponent<TComponent>(out var cachedComponent)) {
        component = cachedComponent;
        fromComponentCache = true;
        return true;
      }

      var directComponent = gameObject.GetComponent<TComponent>();
      if (directComponent is null) {
        return false;
      }

      component = directComponent;
      return true;
    }

    internal static bool TryGetCachedComponents(Component componentCache, out IEnumerable cachedComponents) {
      if (!IsComponentCache(componentCache)) {
        cachedComponents = null;
        return false;
      }

      // Timberborn has moved this surface before; keep both known cache shapes searchable here.
      var componentCacheType = componentCache.GetType();
      var componentsField = componentCacheType.GetField("_components", CacheMemberBindingFlags);
      if (componentsField?.GetValue(componentCache) is IEnumerable components) {
        cachedComponents = components;
        return true;
      }

      var allComponentsProperty = componentCacheType.GetProperty("AllComponents", CacheMemberBindingFlags);
      if (allComponentsProperty?.GetValue(componentCache) is IEnumerable allComponents) {
        cachedComponents = allComponents;
        return true;
      }

      cachedComponents = null;
      return false;
    }

    private static bool IsComponentCache(Component component) =>
      component is not null && component.GetType().Name == ComponentCacheTypeName;

    internal readonly struct CachedComponentCache {

      public readonly Component ComponentCache;
      public readonly IEnumerable CachedComponents;

      public bool HasCachedComponents => CachedComponents is not null;

      public CachedComponentCache(Component componentCache, IEnumerable cachedComponents) {
        ComponentCache = componentCache;
        CachedComponents = cachedComponents;
      }

    }

  }
}
