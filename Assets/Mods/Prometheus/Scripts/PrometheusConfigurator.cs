using Bindito.Core;
using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.BottomBarSystem;
using Timberborn.SingletonSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mods.Prometheus.Scripts {
  [Context("Game")]
  public class PrometheusConfigurator : Configurator {

    protected override void Configure() {
      BindRuntimeStates();
      BindFireResponseComponents();
      Bind<PrometheusFireDebugFragment>().AsSingleton();
      Bind<PrometheusDebugPanel>().AsSingleton();
      Bind<PrometheusDebugBottomBarButton>().AsSingleton();
      MultiBind<ILoadableSingleton>().ToProvider<PrometheusDebugPanelLoadableProvider>().AsSingleton();

      RegisterBottomBarModule();
      RegisterEntityPanelModule();
      RegisterTemplateModule();
    }

    private void BindRuntimeStates() {
      Bind<FireSuppressionRuntimeState>().AsSingleton();
      Bind<FireTuningRuntimeState>().AsSingleton();
      Bind<FireSimulationRuntimeState>().AsSingleton();
      Bind<FireDispatchScoringRuntimeState>().AsSingleton();
      Bind<FireEntityRegistryRuntimeState>().AsSingleton();
      Bind<FireImpactRuntimeState>().AsSingleton();
      Bind<FireDamageStateRuntimeState>().AsSingleton();
      Bind<FireWaterContextRuntimeState>().AsSingleton();
      Bind<FireRecoveryRuntimeState>().AsSingleton();
      Bind<FireVisualEffectRuntimeState>().AsSingleton();
    }

    private void BindFireResponseComponents() {
      Bind<FireResponseProfile>().AsTransient();
      Bind<FireWaterContextProbe>().AsTransient();
      Bind<FireSuppressionProfileApplier>().AsTransient();
      Bind<FireSimulationController>().AsTransient();
      Bind<FireImpactController>().AsTransient();
      Bind<FireDamageStateController>().AsTransient();
      Bind<FireDamageEffectApplier>().AsTransient();
      Bind<FireVisualEffectApplier>().AsTransient();
      Bind<FireRecoveryController>().AsTransient();
      Bind<FireRecoveryEffectApplier>().AsTransient();
      Bind<FireWorkplaceEffectApplier>().AsTransient();
      Bind<FireBeaverEffectApplier>().AsTransient();
      Bind<FireEntityLifecycleCleanup>().AsTransient();
    }

    private void RegisterEntityPanelModule() {
      MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
    }

    private void RegisterBottomBarModule() {
      MultiBind<BottomBarModule>().ToProvider<BottomBarModuleProvider>().AsSingleton();
    }

    private void RegisterTemplateModule() {
      MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule() {
      var builder = new TemplateModule.Builder();
      AddFireResponseDecorators(builder);
      return builder.Build();
    }

    private static void AddFireResponseDecorators(TemplateModule.Builder builder) {
      builder.AddDecorator<FireResponseProfileSpec, FireResponseProfile>();
      builder.AddDecorator<FireResponseProfileSpec, FireWaterContextProbe>();
      builder.AddDecorator<FireResponseProfileSpec, FireSuppressionProfileApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireSimulationController>();
      builder.AddDecorator<FireResponseProfileSpec, FireImpactController>();
      builder.AddDecorator<FireResponseProfileSpec, FireDamageStateController>();
      builder.AddDecorator<FireResponseProfileSpec, FireDamageEffectApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireVisualEffectApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireRecoveryController>();
      builder.AddDecorator<FireResponseProfileSpec, FireRecoveryEffectApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireWorkplaceEffectApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireBeaverEffectApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireEntityLifecycleCleanup>();
    }

    private class EntityPanelModuleProvider : IProvider<EntityPanelModule> {

      private readonly PrometheusFireDebugFragment _prometheusFireDebugFragment;

      public EntityPanelModuleProvider(PrometheusFireDebugFragment prometheusFireDebugFragment) {
        _prometheusFireDebugFragment = prometheusFireDebugFragment;
      }

      public EntityPanelModule Get() {
        var builder = new EntityPanelModule.Builder();
        builder.AddTopFragment(_prometheusFireDebugFragment, -80);
        return builder.Build();
      }

    }

    private class BottomBarModuleProvider : IProvider<BottomBarModule> {

      private readonly PrometheusDebugBottomBarButton _prometheusDebugBottomBarButton;

      public BottomBarModuleProvider(PrometheusDebugBottomBarButton prometheusDebugBottomBarButton) {
        _prometheusDebugBottomBarButton = prometheusDebugBottomBarButton;
      }

      public BottomBarModule Get() {
        var builder = new BottomBarModule.Builder();
        builder.AddLeftSectionElement(_prometheusDebugBottomBarButton, 180);
        return builder.Build();
      }

    }

    private class PrometheusDebugPanelLoadableProvider : IProvider<ILoadableSingleton> {

      private readonly PrometheusDebugPanel _prometheusDebugPanel;

      public PrometheusDebugPanelLoadableProvider(PrometheusDebugPanel prometheusDebugPanel) {
        _prometheusDebugPanel = prometheusDebugPanel;
      }

      public ILoadableSingleton Get() {
        return _prometheusDebugPanel;
      }

    }

  }

  internal class PrometheusDebugBottomBarButton : IBottomBarElementsProvider {

    private readonly PrometheusDebugPanel _prometheusDebugPanel;
    private Button _button;

    public PrometheusDebugBottomBarButton(PrometheusDebugPanel prometheusDebugPanel) {
      _prometheusDebugPanel = prometheusDebugPanel;
      _prometheusDebugPanel.OpenStateChanged += UpdateButtonState;
      _prometheusDebugPanel.UnreadCountChanged += _ => UpdateButtonState(_prometheusDebugPanel.IsOpen);
    }

    public IEnumerable<BottomBarElement> GetElements() {
      _button = new Button(_prometheusDebugPanel.ToggleOpenClose) {
        text = "Debug"
      };
      _button.style.height = 24;
      _button.style.minWidth = 92;
      _button.style.unityFontStyleAndWeight = FontStyle.Bold;
      _button.style.unityTextAlign = TextAnchor.MiddleCenter;
      _button.tooltip = "Toggle the standalone Prometheus Debug panel.";

      UpdateButtonState(_prometheusDebugPanel.IsOpen);

      yield return BottomBarElement.CreateSingleLevel(_button);
    }

    private void UpdateButtonState(bool isOpen) {
      if (_button == null) {
        return;
      }

      var unreadCount = _prometheusDebugPanel.UnreadCount;
      var unreadSuffix = unreadCount <= 0 ? string.Empty : unreadCount > 99 ? " (99+)" : $" ({unreadCount})";

      _button.text = isOpen ? "Debug ●" : $"Debug{unreadSuffix}";
      _button.style.unityBackgroundImageTintColor = isOpen
        ? PrometheusDebugPalette.ButtonSelected
        : unreadCount > 0
          ? PrometheusDebugPalette.ButtonUnread
        : PrometheusDebugPalette.ButtonNeutral;
    }

  }

  internal class FireEntityLifecycleCleanup : BaseComponent {

    private FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private FireSimulationRuntimeState _fireSimulationRuntimeState;
    private FireDispatchScoringRuntimeState _fireDispatchScoringRuntimeState;
    private FireEntityRegistryRuntimeState _fireEntityRegistryRuntimeState;
    private FireImpactRuntimeState _fireImpactRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireWaterContextRuntimeState _fireWaterContextRuntimeState;
    private FireRecoveryRuntimeState _fireRecoveryRuntimeState;

    [Inject]
    public void InjectDependencies(
      FireSuppressionRuntimeState fireSuppressionRuntimeState,
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireDispatchScoringRuntimeState fireDispatchScoringRuntimeState,
      FireEntityRegistryRuntimeState fireEntityRegistryRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState) {
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireDispatchScoringRuntimeState = fireDispatchScoringRuntimeState;
      _fireEntityRegistryRuntimeState = fireEntityRegistryRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
    }

    private void OnDestroy() {
      if (_fireSuppressionRuntimeState == null
          || _fireSimulationRuntimeState == null
          || _fireDispatchScoringRuntimeState == null
          || _fireEntityRegistryRuntimeState == null
          || _fireImpactRuntimeState == null
          || _fireDamageStateRuntimeState == null
          || _fireWaterContextRuntimeState == null
          || _fireRecoveryRuntimeState == null) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      _fireSuppressionRuntimeState.RemoveSnapshot(entityId);
      _fireSimulationRuntimeState.RemoveSnapshot(entityId);
      _fireDispatchScoringRuntimeState.RemoveSnapshot(entityId);
      _fireEntityRegistryRuntimeState.RemoveSnapshot(entityId);
      _fireImpactRuntimeState.RemoveSnapshot(entityId);
      _fireDamageStateRuntimeState.RemoveSnapshot(entityId);
      _fireWaterContextRuntimeState.RemoveSnapshot(entityId);
      _fireRecoveryRuntimeState.RemoveSnapshot(entityId);

      FireTelemetry.Log($"event={FireTelemetryEvents.EntityDestroyCleanup} entity={GameObject.name} id={entityId}");
    }

  }
}
