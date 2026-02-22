using System.Reflection;
using Timberborn.BaseComponentSystem;

namespace Mods.Prometheus.Scripts {
  internal class FireDamageEffectApplier : BaseComponent,
                                          IAwakableComponent,
                                          IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;

    private float _timeSinceLastUpdate;
    private FireDamageState _lastAppliedState;
    private bool _initialized;

    private object _deteriorableComponent;
    private MethodInfo _setDeteriorationToMaximumMethod;
    private MethodInfo _setDeteriorationToZeroMethod;

    private object _growableComponent;
    private MethodInfo _pauseGrowingMethod;
    private MethodInfo _resumeGrowingMethod;
    private MethodInfo _removeMethod;

    private object _livingNaturalResourceComponent;
    private PropertyInfo _isDyingProperty;
    private PropertyInfo _isDeadProperty;

    public void InjectDependencies(FireDamageStateRuntimeState fireDamageStateRuntimeState) {
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
    }

    public void Awake() {
      BindTargetComponents();
      _lastAppliedState = FireDamageState.Healthy;
      _initialized = true;
    }

    public void Update() {
      if (!_initialized) {
        return;
      }

      _timeSinceLastUpdate += UnityEngine.Time.deltaTime;
      if (_timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      var entityId = GameObject.GetInstanceID();
      if (!_fireDamageStateRuntimeState.TryGetSnapshot(entityId, out var snapshot)) {
        return;
      }

      if (snapshot.State == _lastAppliedState) {
        return;
      }

      ApplyState(snapshot.State);
      _lastAppliedState = snapshot.State;
    }

    private void ApplyState(FireDamageState state) {
      switch (state) {
        case FireDamageState.Healthy:
          InvokeIfAvailable(_setDeteriorationToZeroMethod, _deteriorableComponent);
          InvokeIfAvailable(_resumeGrowingMethod, _growableComponent);
          SetBoolIfAvailable(_isDyingProperty, _livingNaturalResourceComponent, false);
          break;
        case FireDamageState.Scorched:
          InvokeIfAvailable(_pauseGrowingMethod, _growableComponent);
          SetBoolIfAvailable(_isDyingProperty, _livingNaturalResourceComponent, true);
          break;
        case FireDamageState.Burning:
          InvokeIfAvailable(_pauseGrowingMethod, _growableComponent);
          SetBoolIfAvailable(_isDyingProperty, _livingNaturalResourceComponent, true);
          break;
        case FireDamageState.Dead:
          InvokeIfAvailable(_setDeteriorationToMaximumMethod, _deteriorableComponent);
          if (!InvokeIfAvailable(_removeMethod, _growableComponent)) {
            InvokeIfAvailable(_pauseGrowingMethod, _growableComponent);
          }
          SetBoolIfAvailable(_isDyingProperty, _livingNaturalResourceComponent, true);
          SetBoolIfAvailable(_isDeadProperty, _livingNaturalResourceComponent, true);
          break;
      }
    }

    private void BindTargetComponents() {
      var deteriorableComponent = GameObject.GetComponent("Deteriorable");
      if (deteriorableComponent is not null) {
        _deteriorableComponent = deteriorableComponent;
        var type = deteriorableComponent.GetType();
        _setDeteriorationToMaximumMethod = type.GetMethod("SetDeteriorationToMaximum", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _setDeteriorationToZeroMethod = type.GetMethod("SetDeteriorationToZero", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      }

      var growableComponent = GameObject.GetComponent("Growable");
      if (growableComponent is not null) {
        _growableComponent = growableComponent;
        var type = growableComponent.GetType();
        _pauseGrowingMethod = type.GetMethod("PauseGrowing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _resumeGrowingMethod = type.GetMethod("ResumeGrowing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _removeMethod = type.GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
      }

      var livingNaturalResourceComponent = GameObject.GetComponent("LivingNaturalResource");
      if (livingNaturalResourceComponent is not null) {
        _livingNaturalResourceComponent = livingNaturalResourceComponent;
        var type = livingNaturalResourceComponent.GetType();
        _isDyingProperty = type.GetProperty("IsDying", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _isDeadProperty = type.GetProperty("IsDead", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      }
    }

    private static bool InvokeIfAvailable(MethodInfo method, object target) {
      if (method is null || target is null) {
        return false;
      }

      method.Invoke(target, null);
      return true;
    }

    private static void SetBoolIfAvailable(PropertyInfo property, object target, bool value) {
      if (property is null || target is null || !property.CanWrite || property.PropertyType != typeof(bool)) {
        return;
      }

      property.SetValue(target, value);
    }

  }
}