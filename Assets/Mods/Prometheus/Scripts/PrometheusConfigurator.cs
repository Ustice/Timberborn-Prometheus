using Bindito.Core;
using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.SingletonSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;
using Timberborn.ToolSystem;

namespace Mods.Prometheus.Scripts {
  [Context("Game")]
  public class PrometheusConfigurator : Configurator {

    protected override void Configure() {
      BindRuntimeStates();
      BindFireComponents();
      Bind<PrometheusFireDebugFragment>().AsSingleton();
      Bind<PrometheusDebugPanel>().AsSingleton();
      Bind<PrometheusDebugActionsTool>().AsSingleton();
      Bind<PrometheusDebugVisualsTool>().AsSingleton();
      Bind<PrometheusDebugSelectionTool>().AsSingleton();
      Bind<PrometheusDebugLogTool>().AsSingleton();
      this.MultiBindCustomTool<PrometheusDebugToolGroupElement>();
      MultiBind<ILoadableSingleton>().ToProvider<PrometheusDebugPanelLoadableProvider>().AsSingleton();

      RegisterEntityPanelModule();
      RegisterTemplateModule();
    }

    private void BindRuntimeStates() {
      Bind<FireTuningRuntimeState>().AsSingleton();
      Bind<FireGridRuntimeState>().AsSingleton();
      Bind<FireSimulationRuntimeState>().AsSingleton();
      Bind<FireImpactRuntimeState>().AsSingleton();
      Bind<FireDamageStateRuntimeState>().AsSingleton();
      Bind<FireRecoveryRuntimeState>().AsSingleton();
      Bind<FireVisualEffectRuntimeState>().AsSingleton();
      Bind<FireVisualEffectPreviewRuntimeState>().AsSingleton();
    }

    private void BindFireComponents() {
      Bind<FireProfile>().AsTransient();
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

    private void RegisterTemplateModule() {
      MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule() {
      var builder = new TemplateModule.Builder();
      AddFireDecorators(builder);
      return builder.Build();
    }

    private static void AddFireDecorators(TemplateModule.Builder builder) {
      builder.AddDecorator<FireProfileSpec, FireProfile>();
      builder.AddDecorator<FireProfileSpec, FireSimulationController>();
      builder.AddDecorator<FireProfileSpec, FireImpactController>();
      builder.AddDecorator<FireProfileSpec, FireDamageStateController>();
      builder.AddDecorator<FireProfileSpec, FireDamageEffectApplier>();
      builder.AddDecorator<FireProfileSpec, FireVisualEffectApplier>();
      builder.AddDecorator<FireProfileSpec, FireRecoveryController>();
      builder.AddDecorator<FireProfileSpec, FireRecoveryEffectApplier>();
      builder.AddDecorator<FireProfileSpec, FireWorkplaceEffectApplier>();
      builder.AddDecorator<FireProfileSpec, FireBeaverEffectApplier>();
      builder.AddDecorator<FireProfileSpec, FireEntityLifecycleCleanup>();
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

  internal class FireEntityLifecycleCleanup : BaseComponent {

    private FireSimulationRuntimeState _fireSimulationRuntimeState;
    private FireGridRuntimeState _fireGridRuntimeState;
    private FireImpactRuntimeState _fireImpactRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireRecoveryRuntimeState _fireRecoveryRuntimeState;

    [Inject]
    public void InjectDependencies(
      FireSimulationRuntimeState fireSimulationRuntimeState,
      FireGridRuntimeState fireGridRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState) {
      _fireSimulationRuntimeState = fireSimulationRuntimeState;
      _fireGridRuntimeState = fireGridRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
    }

    private void OnDestroy() {
      if (_fireSimulationRuntimeState == null
          || _fireGridRuntimeState == null
          || _fireImpactRuntimeState == null
          || _fireDamageStateRuntimeState == null
          || _fireRecoveryRuntimeState == null) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      _fireSimulationRuntimeState.RemoveSnapshot(entityId);
      _fireImpactRuntimeState.RemoveSnapshot(entityId);
      _fireDamageStateRuntimeState.RemoveSnapshot(entityId);
      _fireRecoveryRuntimeState.RemoveSnapshot(entityId);

      FireTelemetry.Log($"event={FireTelemetryEvents.EntityDestroyCleanup} entity={GameObject.name} id={entityId}");
    }

  }
}
