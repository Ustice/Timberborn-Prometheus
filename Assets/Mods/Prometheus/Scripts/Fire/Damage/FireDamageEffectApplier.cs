using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;

namespace Mods.Prometheus.Scripts {
  internal class FireDamageEffectApplier : BaseComponent,
                                          IAwakableComponent,
                                          IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireRuntimeProjectionRuntimeState _fireRuntimeProjectionRuntimeState;
    private PrometheusWorldLoadState _prometheusWorldLoadState;
    private float _timeSinceLastUpdate;
    private FireDamageState _lastAppliedState;
    private FireNaturalResourceVisualStage _lastAppliedTreeVisualStage;
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
      FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState,
      PrometheusWorldLoadState prometheusWorldLoadState) {
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
      _prometheusWorldLoadState = prometheusWorldLoadState;
    }

    public void Awake() {
    }

    public void Update() {
      if (!EnsureWorldReadyAndInitialized()) {
        return;
      }

      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      if (!_fireRuntimeProjectionRuntimeState.TryGetSnapshot(entityId, out var projection) || !projection.HasDamageState) {
        return;
      }

      var snapshot = projection.DamageState;
      var treeVisualStage = FireNaturalResourceVisualRules.DetermineTreeStage(snapshot, projection.VisualExposure);
      if (snapshot.State == _lastAppliedState && treeVisualStage == _lastAppliedTreeVisualStage) {
        return;
      }

      ApplyState(snapshot, treeVisualStage);
      _lastAppliedState = snapshot.State;
      _lastAppliedTreeVisualStage = treeVisualStage;
    }

    internal void DebugRestoreHealthyState() {
      EnsureInitialized();

      var healthySnapshot = new FireDamageStateSnapshot(FireDamageCategory.Unknown, FireDamageState.Healthy, 0f, 0f, 0);
      ApplyState(healthySnapshot, FireNaturalResourceVisualStage.Healthy);
      _lastAppliedState = FireDamageState.Healthy;
      _lastAppliedTreeVisualStage = FireNaturalResourceVisualStage.Healthy;
    }

    private bool EnsureWorldReadyAndInitialized() {
      if (_prometheusWorldLoadState?.WorldReady != true) {
        return false;
      }

      EnsureInitialized();
      return _initialized;
    }

    private void EnsureInitialized() {
      if (_initialized) {
        return;
      }

      BindTargetComponents();
      _lastAppliedState = FireDamageState.Healthy;
      _lastAppliedTreeVisualStage = FireNaturalResourceVisualStage.Healthy;
      _initialized = true;
    }

    private void ApplyState(
      FireDamageStateSnapshot snapshot,
      FireNaturalResourceVisualStage treeVisualStage) {
      if (snapshot.Category == FireDamageCategory.Tree && _livingNaturalResourceComponent is not null) {
        ApplyTreeNaturalResourceState(treeVisualStage);
        return;
      }

      switch (snapshot.State) {
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

    private void ApplyTreeNaturalResourceState(FireNaturalResourceVisualStage visualStage) {
      if (visualStage == FireNaturalResourceVisualStage.Healthy) {
        InvokeIfAvailable(_resumeGrowingMethod, _growableComponent);
      } else {
        InvokeIfAvailable(_pauseGrowingMethod, _growableComponent);
      }

      SetBoolIfAvailable(
        _isDyingProperty,
        _livingNaturalResourceComponent,
        FireNaturalResourceVisualRules.UsesDriedVisual(visualStage));
      SetBoolIfAvailable(
        _isDeadProperty,
        _livingNaturalResourceComponent,
        FireNaturalResourceVisualRules.UsesStumpVisual(visualStage));
    }

    private void BindTargetComponents() {
      if (TimberbornComponentCacheLookup.TryGetCachedOrDirectComponentByTypeName(
        GameObject,
        TimberbornCompatibility.DeteriorableTypeName,
        out var deteriorableComponent)) {
        _deteriorableComponent = deteriorableComponent;
        var type = deteriorableComponent.GetType();
        _setDeteriorationToMaximumMethod = TimberbornCompatibility.FindMethod(type, "SetDeteriorationToMaximum");
        _setDeteriorationToZeroMethod = TimberbornCompatibility.FindMethod(type, "SetDeteriorationToZero");
        TimberbornCompatibility.RecordProbe(
          TimberbornCompatibilityArea.Damage,
          _setDeteriorationToMaximumMethod is not null && _setDeteriorationToZeroMethod is not null,
          "Deteriorable.SetDeteriorationToMaximum/SetDeteriorationToZero");
      }

      if (TimberbornComponentCacheLookup.TryGetCachedOrDirectComponentByTypeName(
        GameObject,
        TimberbornCompatibility.GrowableTypeName,
        out var growableComponent)) {
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

      if (TimberbornComponentCacheLookup.TryGetCachedOrDirectComponentByTypeName(
        GameObject,
        TimberbornCompatibility.LivingNaturalResourceTypeName,
        out var livingNaturalResourceComponent)) {
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
