using System.Collections.Generic;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal static class PrometheusLoadedSceneObjectLookup {

    internal static IEnumerable<GameObject> FindLoadedSceneGameObjects() {
      var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
      for (var i = 0; i < allObjects.Length; i++) {
        var gameObject = allObjects[i];
        if (!IsLoadedSceneObject(gameObject)) {
          continue;
        }

        yield return gameObject;
      }
    }

    internal static bool TryFindLoadedGameObject(int entityId, out GameObject loadedGameObject) {
      foreach (var gameObject in FindLoadedSceneGameObjects()) {
        if (gameObject.GetInstanceID() != entityId) {
          continue;
        }

        loadedGameObject = gameObject;
        return true;
      }

      loadedGameObject = null;
      return false;
    }

    internal static Dictionary<int, GameObject> BuildIndexByEntityId() {
      var loadedObjectsByEntityId = new Dictionary<int, GameObject>();
      foreach (var gameObject in FindLoadedSceneGameObjects()) {
        var entityId = gameObject.GetInstanceID();
        if (entityId == 0 || loadedObjectsByEntityId.ContainsKey(entityId)) {
          continue;
        }

        loadedObjectsByEntityId[entityId] = gameObject;
      }

      return loadedObjectsByEntityId;
    }

    private static bool IsLoadedSceneObject(GameObject gameObject) =>
      gameObject != null
      && gameObject.scene.IsValid()
      && gameObject.scene.isLoaded;

  }
}
