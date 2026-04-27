using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;

namespace Mods.Prometheus.Scripts {
  internal class FireDamageEffectApplier : BaseComponent,
                                          IAwakableComponent,
                                          IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireResetRegistry _fireResetRegistry;
    private FireResetRegistration _resetRegistration = FireResetRegistration.Empty;

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

    [Inject]
    public void InjectDependencies(
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireResetRegistry fireResetRegistry) {
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireResetRegistry = fireResetRegistry;
    }

    public void Awake() {
      BindTargetComponents();
      _lastAppliedState = FireDamageState.Healthy;
      _initialized = true;
    }

    public void Update() {
      EnsureResetRegistration();
      if (!_initialized) {
        return;
      }

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

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

    internal void DebugRestoreHealthyState() {
      if (!_initialized) {
        BindTargetComponents();
        _initialized = true;
      }

      ApplyState(FireDamageState.Healthy);
      _lastAppliedState = FireDamageState.Healthy;
    }

    private void OnDestroy() {
      _resetRegistration.Dispose();
    }

    private void EnsureResetRegistration() {
      if (_resetRegistration != FireResetRegistration.Empty) {
        return;
      }

      _resetRegistration = _fireResetRegistry.RegisterEntity(
        GameObject.GetInstanceID(),
        FireResetHookKind.DamageEffect,
        nameof(FireDamageEffectApplier),
        DebugRestoreHealthyState);
    }

    private void ApplyState(FireDamageState state) {
      switch (state) {
        case FireDamageState.Healthy:
          InvokeIfAvailable(_setDeteriorationToZeroMethod, _deteriorableComponent);
          InvokeIfAvailable(_resumeGrowingMethod, _growableComponent);
          SetBoolIfAvailable(_isDyingProperty, _livingNaturalResourceComponent, false);
          SetBoolIfAvailable(_isDeadProperty, _livingNaturalResourceComponent, false);
          break;
        case FireDamageState.Scorched:
        case FireDamageState.Burning:
          InvokeIfAvailable(_pauseGrowingMethod, _growableComponent);
          SetBoolIfAvailable(_isDyingProperty, _livingNaturalResourceComponent, true);
          break;
        case FireDamageState.Dead:
          InvokeIfAvailable(_setDeteriorationToMaximumMethod, _deteriorableComponent);
          if (_livingNaturalResourceComponent is not null) {
            InvokeIfAvailable(_pauseGrowingMethod, _growableComponent);
          } else if (!InvokeIfAvailable(_removeMethod, _growableComponent)) {
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
        _setDeteriorationToMaximumMethod = TimberbornCompatibility.FindMethod(type, "SetDeteriorationToMaximum");
        _setDeteriorationToZeroMethod = TimberbornCompatibility.FindMethod(type, "SetDeteriorationToZero");
        TimberbornCompatibility.RecordProbe(
          TimberbornCompatibilityArea.Damage,
          _setDeteriorationToMaximumMethod is not null && _setDeteriorationToZeroMethod is not null,
          "Deteriorable.SetDeteriorationToMaximum/SetDeteriorationToZero");
      }

      var growableComponent = GameObject.GetComponent("Growable");
      if (growableComponent is not null) {
        _growableComponent = growableComponent;
        var type = growableComponent.GetType();
        _pauseGrowingMethod = TimberbornCompatibility.FindMethod(type, "PauseGrowing");
        _resumeGrowingMethod = TimberbornCompatibility.FindMethod(type, "ResumeGrowing");
        _removeMethod = TimberbornCompatibility.FindMethod(type, "Remove", System.Type.EmptyTypes);
        TimberbornCompatibility.RecordProbe(
          TimberbornCompatibilityArea.Damage,
          _pauseGrowingMethod is not null && _resumeGrowingMethod is not null,
          "Growable.PauseGrowing/ResumeGrowing/Remove");
      }

      var livingNaturalResourceComponent = GameObject.GetComponent("LivingNaturalResource");
      if (livingNaturalResourceComponent is not null) {
        _livingNaturalResourceComponent = livingNaturalResourceComponent;
        var type = livingNaturalResourceComponent.GetType();
        _isDyingProperty = TimberbornCompatibility.FindProperty(type, "IsDying");
        _isDeadProperty = TimberbornCompatibility.FindProperty(type, "IsDead");
        TimberbornCompatibility.RecordProbe(
          TimberbornCompatibilityArea.Damage,
          _isDyingProperty is not null && _isDeadProperty is not null,
          "LivingNaturalResource.IsDying/IsDead");
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
