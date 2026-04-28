using System.Linq;
using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

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
    private TreeModelStateSwitcher _treeModelStateSwitcher;

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
      if (snapshot.Category == FireDamageCategory.Tree) {
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
      _treeModelStateSwitcher?.Apply(visualStage);
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

      _treeModelStateSwitcher = TreeModelStateSwitcher.TryCreate(GameObject);
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

    private sealed class TreeModelStateSwitcher {

      private const string SeedlingRootName = "Seedling";
      private const string MatureRootName = "Mature";
      private const string LeftoverRootName = "#Leftover";

      private readonly TreeAgeModelState[] _ageStates;
      private readonly Transform _leftoverRoot;
      private readonly bool _hasAgeLocalLeftover;
      private FireNaturalResourceVisualStage _lastAppliedStage = FireNaturalResourceVisualStage.Healthy;

      private TreeModelStateSwitcher(
        TreeAgeModelState[] ageStates,
        Transform leftoverRoot) {
        _ageStates = ageStates;
        _leftoverRoot = leftoverRoot;
        _hasAgeLocalLeftover = ageStates.Any(state => state.HasLeftoverRoot);
      }

      public static TreeModelStateSwitcher TryCreate(GameObject target) {
        var ageStates = new[] {
            TreeAgeModelState.TryCreate(FindChildRecursive(target.transform, SeedlingRootName)),
            TreeAgeModelState.TryCreate(FindChildRecursive(target.transform, MatureRootName)),
          }
          .Where(state => state is not null)
          .ToArray();
        var leftoverRoot = FindChildRecursive(target.transform, LeftoverRootName);
        if (ageStates.Length == 0 && leftoverRoot == null) {
          return null;
        }

        return new TreeModelStateSwitcher(ageStates, leftoverRoot);
      }

      public void Apply(FireNaturalResourceVisualStage stage) {
        if (stage == _lastAppliedStage) {
          return;
        }

        _lastAppliedStage = stage;
        var showLeftover = FireNaturalResourceVisualRules.UsesStumpVisual(stage);
        if (_leftoverRoot != null && !_hasAgeLocalLeftover) {
          _leftoverRoot.gameObject.SetActive(showLeftover);
        }

        for (var i = 0; i < _ageStates.Length; i++) {
          _ageStates[i].Apply(stage, showLeftover, _hasAgeLocalLeftover);
        }
      }

      private static Transform FindChildRecursive(Transform root, string childName) {
        if (root == null) {
          return null;
        }

        if (root.name == childName) {
          return root;
        }

        for (var i = 0; i < root.childCount; i++) {
          var result = FindChildRecursive(root.GetChild(i), childName);
          if (result != null) {
            return result;
          }
        }

        return null;
      }

      private sealed class TreeAgeModelState {

        private readonly Transform _ageRoot;
        private readonly Transform _aliveRoot;
        private readonly Transform _dyingRoot;
        private readonly Transform _deadRoot;
        private readonly Transform _leftoverRoot;
        private readonly bool _ageRootOriginalActive;

        public bool HasLeftoverRoot => _leftoverRoot != null;

        private TreeAgeModelState(
          Transform ageRoot,
          Transform aliveRoot,
          Transform dyingRoot,
          Transform deadRoot,
          Transform leftoverRoot) {
          _ageRoot = ageRoot;
          _aliveRoot = aliveRoot;
          _dyingRoot = dyingRoot;
          _deadRoot = deadRoot;
          _leftoverRoot = leftoverRoot;
          _ageRootOriginalActive = ageRoot.gameObject.activeSelf;
        }

        public static TreeAgeModelState TryCreate(Transform ageRoot) {
          if (ageRoot == null) {
            return null;
          }

          var aliveRoot = FindChildRecursive(ageRoot, FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.Healthy));
          var dyingRoot = FindChildRecursive(ageRoot, FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.Dried));
          var deadRoot = FindChildRecursive(ageRoot, FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.DeadAndCharred));
          var leftoverRoot = FindChildRecursive(ageRoot, FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.StumpAndCharred));
          if (aliveRoot == null && dyingRoot == null && deadRoot == null && leftoverRoot == null) {
            return null;
          }

          return new TreeAgeModelState(ageRoot, aliveRoot, dyingRoot, deadRoot, leftoverRoot);
        }

        public void Apply(
          FireNaturalResourceVisualStage stage,
          bool showLeftover,
          bool useAgeLocalLeftover) {
          _ageRoot.gameObject.SetActive((!showLeftover || useAgeLocalLeftover) && _ageRootOriginalActive);
          if (!_ageRootOriginalActive) {
            return;
          }

          var targetName = FireNaturalResourceVisualRules.ModelStateName(stage);
          SetActiveIfPresent(_aliveRoot, targetName == FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.Healthy));
          SetActiveIfPresent(_dyingRoot, targetName == FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.Dried));
          SetActiveIfPresent(_deadRoot, targetName == FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.DeadAndCharred));
          SetActiveIfPresent(_leftoverRoot, showLeftover && useAgeLocalLeftover);
        }

        private static void SetActiveIfPresent(Transform transform, bool active) {
          if (transform == null) {
            return;
          }

          transform.gameObject.SetActive(active);
        }

      }

    }

  }
}
