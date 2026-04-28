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

    private object _naturalResourceModelComponent;
    private MethodInfo _hideModelsMethod;
    private MethodInfo _showCurrentModelMethod;

    private object _cuttableComponent;
    private MethodInfo _showLeftoverModelMethod;

    private object _livingNaturalResourceComponent;
    private PropertyInfo _isDyingProperty;
    private PropertyInfo _isDeadProperty;
    private MethodInfo _dieMethod;
    private MethodInfo _reverseDeathMethod;

    private object _yielderComponent;
    private PropertyInfo _yielderSpecProperty;
    private PropertyInfo _isYieldRemovedProperty;
    private PropertyInfo _isYieldingProperty;
    private PropertyInfo _yieldProperty;
    private MethodInfo _removeRemainingYieldMethod;
    private MethodInfo _resetYieldMethod;
    private FieldInfo _yielderYieldField;
    private FieldInfo _yielderInitialYieldField;
    private FieldInfo _yielderSpecYieldField;
    private object _originalYield;
    private object _originalInitialYield;
    private object _originalYielderSpecYield;
    private bool _capturedOriginalYield;

    private object _emptyDeadNaturalResourceOverriderComponent;
    private MethodInfo _makeOverridableMethod;

    private TreeModelStateSwitcher _treeModelStateSwitcher;
    private bool _treeReachedStumpVisualStage;
    private bool _treeReachedDeadVisualStage;
    private bool _treeAshYieldApplied;

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
      var treeVisualStage = DetermineEffectiveTreeVisualStage(snapshot, projection.VisualExposure);
      if (snapshot.State == _lastAppliedState
          && treeVisualStage == _lastAppliedTreeVisualStage
          && treeVisualStage != FireNaturalResourceVisualStage.StumpAndCharred) {
        return;
      }

      ApplyState(snapshot, treeVisualStage);
      _lastAppliedState = snapshot.State;
      _lastAppliedTreeVisualStage = treeVisualStage;
    }

    internal void DebugRestoreHealthyState() {
      EnsureInitialized();

      var healthySnapshot = new FireDamageStateSnapshot(FireDamageCategory.Unknown, FireDamageState.Healthy, 0f, 0f, 0);
      _treeReachedDeadVisualStage = false;
      _treeReachedStumpVisualStage = false;
      _treeAshYieldApplied = false;
      InvokeIfAvailable(_reverseDeathMethod, _livingNaturalResourceComponent);
      RestoreOriginalTreeYield();
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
      visualStage = FireNaturalResourceVisualRules.ClampToLatchedTreeStage(
        visualStage,
        _treeReachedDeadVisualStage,
        _treeReachedStumpVisualStage);

      if (visualStage == FireNaturalResourceVisualStage.Healthy) {
        InvokeIfAvailable(_resumeGrowingMethod, _growableComponent);
      } else {
        InvokeIfAvailable(_pauseGrowingMethod, _growableComponent);
      }

      if (visualStage is FireNaturalResourceVisualStage.DeadAndCharred
          or FireNaturalResourceVisualStage.StumpAndCharred) {
        _treeReachedDeadVisualStage = true;
      }

      var usesStumpVisual = FireNaturalResourceVisualRules.UsesStumpVisual(visualStage);
      if (usesStumpVisual) {
        _treeReachedStumpVisualStage = true;
        InvokeIfAvailable(_dieMethod, _livingNaturalResourceComponent);
      }

      SetBoolIfAvailable(
        _isDyingProperty,
        _livingNaturalResourceComponent,
        FireNaturalResourceVisualRules.UsesDriedVisual(visualStage));
      SetBoolIfAvailable(
        _isDeadProperty,
        _livingNaturalResourceComponent,
        FireNaturalResourceVisualRules.UsesDeadVisual(visualStage));
      _treeModelStateSwitcher?.Apply(visualStage);
      if (usesStumpVisual) {
        ForceNativeLeftoverModel();
        ApplyTreeAshYieldOnce();
      }
    }

    private FireNaturalResourceVisualStage DetermineEffectiveTreeVisualStage(
      FireDamageStateSnapshot snapshot,
      FireExposureSnapshot exposure) {
      var stage = FireNaturalResourceVisualRules.DetermineTreeStage(snapshot, exposure);
      if (snapshot.Category != FireDamageCategory.Tree) {
        return stage;
      }

      return FireNaturalResourceVisualRules.ClampToLatchedTreeStage(
        stage,
        _treeReachedDeadVisualStage,
        _treeReachedStumpVisualStage);
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
        TimberbornCompatibility.NaturalResourceModelTypeName,
        out var naturalResourceModelComponent)) {
        _naturalResourceModelComponent = naturalResourceModelComponent;
        var type = naturalResourceModelComponent.GetType();
        _hideModelsMethod = TimberbornCompatibility.FindMethod(type, "HideModels");
        _showCurrentModelMethod = TimberbornCompatibility.FindMethod(type, "ShowCurrentModel");
        TimberbornCompatibility.RecordProbe(
          TimberbornCompatibilityArea.Damage,
          _hideModelsMethod is not null,
          "NaturalResourceModel.HideModels");
      }

      if (TimberbornComponentCacheLookup.TryGetCachedOrDirectComponentByTypeName(
        GameObject,
        TimberbornCompatibility.CuttableTypeName,
        out var cuttableComponent)) {
        _cuttableComponent = cuttableComponent;
        var type = cuttableComponent.GetType();
        _showLeftoverModelMethod = TimberbornCompatibility.FindMethod(type, "ShowLeftoverModel");
        TimberbornCompatibility.RecordProbe(
          TimberbornCompatibilityArea.Damage,
          _showLeftoverModelMethod is not null,
          "Cuttable.ShowLeftoverModel");
      }

      if (TimberbornComponentCacheLookup.TryGetCachedOrDirectComponentByTypeName(
        GameObject,
        TimberbornCompatibility.LivingNaturalResourceTypeName,
        out var livingNaturalResourceComponent)) {
        _livingNaturalResourceComponent = livingNaturalResourceComponent;
        var type = livingNaturalResourceComponent.GetType();
        _isDyingProperty = TimberbornCompatibility.FindProperty(type, "IsDying");
        _isDeadProperty = TimberbornCompatibility.FindProperty(type, "IsDead");
        _dieMethod = TimberbornCompatibility.FindMethod(type, "Die");
        _reverseDeathMethod = TimberbornCompatibility.FindMethod(type, "ReverseDeath");
        TimberbornCompatibility.RecordProbe(
          TimberbornCompatibilityArea.Damage,
          _isDeadProperty is not null && _dieMethod is not null,
          "LivingNaturalResource.IsDead/Die");
      }

      if (TimberbornComponentCacheLookup.TryGetCachedOrDirectComponentByTypeName(
        GameObject,
        TimberbornCompatibility.YielderTypeName,
        out var yielderComponent)) {
        _yielderComponent = yielderComponent;
        var type = yielderComponent.GetType();
        _yielderSpecProperty = TimberbornCompatibility.FindProperty(type, "YielderSpec");
        _isYieldRemovedProperty = TimberbornCompatibility.FindProperty(type, "IsYieldRemoved");
        _isYieldingProperty = TimberbornCompatibility.FindProperty(type, "IsYielding");
        _yieldProperty = TimberbornCompatibility.FindProperty(type, "Yield");
        _removeRemainingYieldMethod = TimberbornCompatibility.FindMethod(type, "RemoveRemainingYield");
        _resetYieldMethod = TimberbornCompatibility.FindMethod(type, "ResetYield");
        _yielderYieldField = type.GetField("_yield", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _yielderInitialYieldField = type.GetField("_initialYield", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var yielderSpec = _yielderSpecProperty?.GetValue(yielderComponent);
        _yielderSpecYieldField = yielderSpec?.GetType().GetField("<Yield>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        CaptureOriginalTreeYield();
        TimberbornCompatibility.RecordProbe(
          TimberbornCompatibilityArea.Damage,
          _resetYieldMethod is not null && _yielderYieldField is not null && _yielderSpecYieldField is not null,
          "Yielder.ResetYield/_yield/YielderSpec.Yield");
      }

      if (TimberbornComponentCacheLookup.TryGetCachedOrDirectComponentByTypeName(
        GameObject,
        TimberbornCompatibility.EmptyDeadNaturalResourceOverriderTypeName,
        out var emptyDeadNaturalResourceOverriderComponent)) {
        _emptyDeadNaturalResourceOverriderComponent = emptyDeadNaturalResourceOverriderComponent;
        var type = emptyDeadNaturalResourceOverriderComponent.GetType();
        _makeOverridableMethod = TimberbornCompatibility.FindMethod(type, "MakeOverridable");
        TimberbornCompatibility.RecordProbe(
          TimberbornCompatibilityArea.Damage,
          _makeOverridableMethod is not null,
          "EmptyDeadNaturalResourceOverrider.MakeOverridable");
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

    private void CaptureOriginalTreeYield() {
      if (_capturedOriginalYield || _yielderComponent is null) {
        return;
      }

      _originalYield = _yielderYieldField?.GetValue(_yielderComponent);
      _originalInitialYield = _yielderInitialYieldField?.GetValue(_yielderComponent);
      var yielderSpec = _yielderSpecProperty?.GetValue(_yielderComponent);
      _originalYielderSpecYield = _yielderSpecYieldField?.GetValue(yielderSpec);
      _capturedOriginalYield = true;
    }

    private void RestoreOriginalTreeYield() {
      if (!_capturedOriginalYield || _yielderComponent is null) {
        return;
      }

      if (_yielderInitialYieldField is not null && _originalInitialYield is not null) {
        _yielderInitialYieldField.SetValue(_yielderComponent, _originalInitialYield);
      }

      var yielderSpec = _yielderSpecProperty?.GetValue(_yielderComponent);
      if (_yielderSpecYieldField is not null && yielderSpec is not null && _originalYielderSpecYield is not null) {
        _yielderSpecYieldField.SetValue(yielderSpec, _originalYielderSpecYield);
      }

      if (_resetYieldMethod is not null) {
        _resetYieldMethod.Invoke(_yielderComponent, null);
        return;
      }

      if (_yielderYieldField is not null && _originalYield is not null) {
        _yielderYieldField.SetValue(_yielderComponent, _originalYield);
      }
    }

    private void ForceNativeLeftoverModel() {
      InvokeIfAvailable(_hideModelsMethod, _naturalResourceModelComponent);
      InvokeIfAvailable(_showLeftoverModelMethod, _cuttableComponent);
    }

    private bool TryApplyTreeAshYield(out string reason) {
      reason = "unknown";
      if (_yielderComponent is null) {
        reason = "yielder_missing";
        return false;
      }

      var yielderSpec = _yielderSpecProperty?.GetValue(_yielderComponent);
      var appliedSpecYield = TryApplyTreeAshSpecYield(yielderSpec);
      var goodAmountType = _yielderInitialYieldField?.FieldType ?? _yielderYieldField?.FieldType;
      if (goodAmountType is null) {
        reason = "good_amount_type_missing";
        return false;
      }

      var constructor = goodAmountType.GetConstructor(new[] { typeof(string), typeof(int) });
      if (constructor is null) {
        reason = "good_amount_constructor_missing";
        return false;
      }

      var ashYield = constructor.Invoke(new object[] {
        FertileAshRecoveredGoodStackRules.FertileAshGoodId,
        FertileAshSpawnPolicy.CharredTreeAmount,
      });

      if (_yielderInitialYieldField is not null) {
        _yielderInitialYieldField.SetValue(_yielderComponent, ashYield);
      }

      if (_resetYieldMethod is not null) {
        _resetYieldMethod.Invoke(_yielderComponent, null);
        reason = appliedSpecYield ? "reset_spec_and_yield_to_stump_ash" : "reset_yield_to_stump_ash";
        return true;
      }

      if (_yielderYieldField is null) {
        reason = "yield_field_missing";
        return false;
      }

      _yielderYieldField.SetValue(_yielderComponent, ashYield);
      reason = appliedSpecYield ? "set_spec_and_yield_field_to_stump_ash" : "set_yield_field_to_stump_ash";
      return true;
    }

    private void ApplyTreeAshYieldOnce() {
      if (_treeAshYieldApplied) {
        return;
      }

      _treeAshYieldApplied = true;
      if (TryApplyTreeAshYield(out var reason)) {
        FireTelemetry.Log(
          $"event={FireTelemetryEvents.FertileAshTreeRemnantYieldApplied} entity={GameObject.name} id={GameObject.GetInstanceID()} good={FertileAshRecoveredGoodStackRules.FertileAshGoodId} amount={FertileAshSpawnPolicy.CharredTreeAmount} reason={reason} {DescribeYielderForTelemetry()}");
        return;
      }

      FireTelemetry.LogWarning(
        $"event={FireTelemetryEvents.FertileAshTreeRemnantYieldFailed} entity={GameObject.name} id={GameObject.GetInstanceID()} good={FertileAshRecoveredGoodStackRules.FertileAshGoodId} amount={FertileAshSpawnPolicy.CharredTreeAmount} reason={reason} {DescribeYielderForTelemetry()}");
    }

    private bool TryApplyTreeAshSpecYield(object yielderSpec) {
      if (yielderSpec is null || _yielderSpecYieldField is null) {
        return false;
      }

      var goodAmountSpecType = _yielderSpecYieldField.FieldType;
      var ashSpec = System.Activator.CreateInstance(goodAmountSpecType);
      if (ashSpec is null) {
        return false;
      }

      var idProperty = TimberbornCompatibility.FindProperty(goodAmountSpecType, "Id");
      var amountProperty = TimberbornCompatibility.FindProperty(goodAmountSpecType, "Amount");
      if (idProperty?.CanWrite == true) {
        idProperty.SetValue(ashSpec, FertileAshRecoveredGoodStackRules.FertileAshGoodId);
      } else {
        goodAmountSpecType.GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
          ?.SetValue(ashSpec, FertileAshRecoveredGoodStackRules.FertileAshGoodId);
      }

      if (amountProperty?.CanWrite == true) {
        amountProperty.SetValue(ashSpec, FertileAshSpawnPolicy.CharredTreeAmount);
      } else {
        goodAmountSpecType.GetField("<Amount>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
          ?.SetValue(ashSpec, FertileAshSpawnPolicy.CharredTreeAmount);
      }

      _yielderSpecYieldField.SetValue(yielderSpec, ashSpec);
      return true;
    }

    private string DescribeYielderForTelemetry() {
      var yield = _yieldProperty?.GetValue(_yielderComponent) ?? _yielderYieldField?.GetValue(_yielderComponent);
      var yielderSpec = _yielderSpecProperty?.GetValue(_yielderComponent);
      var specYield = _yielderSpecYieldField?.GetValue(yielderSpec);
      return $"yield={DescribeGoodAmountForTelemetry(yield)} specYield={DescribeGoodAmountSpecForTelemetry(specYield)} component={DescribePropertyForTelemetry(yielderSpec, "YielderComponentName")} resourceGroup={DescribePropertyForTelemetry(yielderSpec, "ResourceGroup")} isYielding={GetBoolIfAvailable(_isYieldingProperty, _yielderComponent).ToString().ToLowerInvariant()}";
    }

    private static string DescribeGoodAmountForTelemetry(object goodAmount) =>
      goodAmount is null
        ? "none"
        : $"{FireResetRegistry.EscapeToken(GetPropertyString(goodAmount, "GoodId"))}:{GetPropertyInt(goodAmount, "Amount")}";

    private static string DescribeGoodAmountSpecForTelemetry(object goodAmountSpec) =>
      goodAmountSpec is null
        ? "none"
        : $"{FireResetRegistry.EscapeToken(GetPropertyString(goodAmountSpec, "Id"))}:{GetPropertyInt(goodAmountSpec, "Amount")}";

    private static string DescribePropertyForTelemetry(object target, string propertyName) =>
      FireResetRegistry.EscapeToken(GetPropertyString(target, propertyName));

    private static string GetPropertyString(object target, string propertyName) =>
      target is null
        ? "none"
        : target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target)?.ToString() ?? "none";

    private static int GetPropertyInt(object target, string propertyName) {
      if (target is null) {
        return 0;
      }

      var value = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);
      return value is int intValue ? intValue : 0;
    }

    private static bool GetBoolIfAvailable(PropertyInfo property, object target) =>
      property is not null
      && target is not null
      && property.PropertyType == typeof(bool)
      && property.GetValue(target) is true;

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
        if (stage == _lastAppliedStage && stage != FireNaturalResourceVisualStage.StumpAndCharred) {
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
