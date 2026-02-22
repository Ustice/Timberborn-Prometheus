using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireWaterContextProbe : BaseComponent,
                                        IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireWaterContextRuntimeState _fireWaterContextRuntimeState;

    private float _timeSinceLastUpdate;

    [Inject]
    public void InjectDependencies(FireWaterContextRuntimeState fireWaterContextRuntimeState) {
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
    }

    public void Update() {
      _timeSinceLastUpdate += Time.deltaTime;
      if (_timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      var floodableObject = GameObject.GetComponent("FloodableObject");
      var wateredNaturalResource = GameObject.GetComponent("WateredNaturalResource");
      var livingWaterObject = GameObject.GetComponent("LivingWaterObject");

      var isFlooded = ReadBool(floodableObject, "IsFlooded");
      var waterAboveBase = ReadFloat(floodableObject, "WaterAboveBase");
      if (waterAboveBase <= 0f) {
        waterAboveBase = ReadFloat(livingWaterObject, "WaterAboveBase");
      }

      var waterNeedsMet = ReadBool(wateredNaturalResource, "WaterNeedsAreMet");

      var localWaterExposure = Mathf.Clamp01(
        (waterAboveBase / 0.65f)
        + (isFlooded ? 0.5f : 0f)
        + (waterNeedsMet ? 0.35f : 0f));

      var quenchingBonus = localWaterExposure * 0.08f;
      var spreadReduction = localWaterExposure * 0.05f;

      var snapshot = new FireWaterContextSnapshot(
        isFlooded,
        waterAboveBase,
        waterNeedsMet,
        localWaterExposure,
        quenchingBonus,
        spreadReduction);

      _fireWaterContextRuntimeState.SetSnapshot(GameObject.GetInstanceID(), snapshot);
    }

    private static bool ReadBool(object target, string propertyName) {
      if (target is null) {
        return false;
      }

      var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (property is null || property.PropertyType != typeof(bool)) {
        return false;
      }

      return (bool)property.GetValue(target);
    }

    private static float ReadFloat(object target, string propertyName) {
      if (target is null) {
        return 0f;
      }

      var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (property is null || property.PropertyType != typeof(float)) {
        return 0f;
      }

      return (float)property.GetValue(target);
    }

  }
}