using Bindito.Core;
using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.SingletonSystem;
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
      Bind<PrometheusDebugQaTool>().AsSingleton();
      Bind<PrometheusDebugLogTool>().AsSingleton();
      this.MultiBindCustomTool<PrometheusDebugToolGroupElement>();
      MultiBind<ILoadableSingleton>().ToProvider<PrometheusDebugPanelLoadableProvider>().AsSingleton();
      MultiBind<ILoadableSingleton>().ToProvider<PrometheusWorldLoadStateLoadableProvider>().AsSingleton();
      MultiBind<IPostLoadableSingleton>().ToProvider<PrometheusWorldLoadStatePostLoadableProvider>().AsSingleton();
      MultiBind<IUnloadableSingleton>().ToProvider<PrometheusWorldLoadStateUnloadableProvider>().AsSingleton();
      MultiBind<IUpdatableSingleton>().To<FireFieldAmendmentRuntimeTicker>().AsSingleton();

      RegisterEntityPanelModule();
      RegisterTemplateModule();
    }

    private void BindRuntimeStates() {
      Bind<FireTuningRuntimeState>().AsSingleton();
      Bind<FireGridRuntimeState>().AsSingleton();
      Bind<FireGridSimulationCoordinator>().AsSingleton();
      Bind<TimberbornEnvironmentAdapter>().AsSingleton();
      Bind<FireExposureRuntimeState>().AsSingleton();
      Bind<FireImpactRuntimeState>().AsSingleton();
      Bind<FireDamageStateRuntimeState>().AsSingleton();
      Bind<FireRuntimeProjectionRuntimeState>().AsSingleton();
      Bind<FireRecoveryRuntimeState>().AsSingleton();
      Bind<FertileAshRecoveredGoodStackTelemetryState>().AsSingleton();
      Bind<FertileAshRecoveredGoodStackSpawner>().AsSingleton();
      Bind<FireFieldAmendmentRuntimeState>().AsSingleton();
      Bind<FireVisualEffectRuntimeState>().AsSingleton();
      Bind<FireVisualEffectPreviewRuntimeState>().AsSingleton();
      Bind<FireResetRegistry>().AsSingleton();
      Bind<PrometheusWorldLoadState>().AsSingleton();
    }

    private void BindFireComponents() {
      Bind<FireProfile>().AsTransient();
      Bind<FireExposureController>().AsTransient();
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
      builder.AddDecorator<FireProfileSpec, FireExposureController>();
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

    private class PrometheusWorldLoadStateLoadableProvider : IProvider<ILoadableSingleton> {

      private readonly PrometheusWorldLoadState _worldLoadState;

      public PrometheusWorldLoadStateLoadableProvider(PrometheusWorldLoadState worldLoadState) {
        _worldLoadState = worldLoadState;
      }

      public ILoadableSingleton Get() {
        return _worldLoadState;
      }

    }

    private class PrometheusWorldLoadStatePostLoadableProvider : IProvider<IPostLoadableSingleton> {

      private readonly PrometheusWorldLoadState _worldLoadState;

      public PrometheusWorldLoadStatePostLoadableProvider(PrometheusWorldLoadState worldLoadState) {
        _worldLoadState = worldLoadState;
      }

      public IPostLoadableSingleton Get() {
        return _worldLoadState;
      }

    }

    private class PrometheusWorldLoadStateUnloadableProvider : IProvider<IUnloadableSingleton> {

      private readonly PrometheusWorldLoadState _worldLoadState;

      public PrometheusWorldLoadStateUnloadableProvider(PrometheusWorldLoadState worldLoadState) {
        _worldLoadState = worldLoadState;
      }

      public IUnloadableSingleton Get() {
        return _worldLoadState;
      }

    }

  }

  internal class FireEntityLifecycleCleanup : BaseComponent {

    private FireExposureRuntimeState _fireExposureRuntimeState;
    private FireGridRuntimeState _fireGridRuntimeState;
    private FireImpactRuntimeState _fireImpactRuntimeState;
    private FireDamageStateRuntimeState _fireDamageStateRuntimeState;
    private FireRuntimeProjectionRuntimeState _fireRuntimeProjectionRuntimeState;
    private FireRecoveryRuntimeState _fireRecoveryRuntimeState;
    private FireFieldAmendmentRuntimeState _fireFieldAmendmentRuntimeState;

    [Inject]
    public void InjectDependencies(
      FireExposureRuntimeState fireExposureRuntimeState,
      FireGridRuntimeState fireGridRuntimeState,
      FireImpactRuntimeState fireImpactRuntimeState,
      FireDamageStateRuntimeState fireDamageStateRuntimeState,
      FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState,
      FireRecoveryRuntimeState fireRecoveryRuntimeState,
      FireFieldAmendmentRuntimeState fireFieldAmendmentRuntimeState) {
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _fireGridRuntimeState = fireGridRuntimeState;
      _fireImpactRuntimeState = fireImpactRuntimeState;
      _fireDamageStateRuntimeState = fireDamageStateRuntimeState;
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
      _fireRecoveryRuntimeState = fireRecoveryRuntimeState;
      _fireFieldAmendmentRuntimeState = fireFieldAmendmentRuntimeState;
    }

    private void OnDestroy() {
      if (_fireExposureRuntimeState == null
          || _fireGridRuntimeState == null
          || _fireImpactRuntimeState == null
          || _fireDamageStateRuntimeState == null
          || _fireRuntimeProjectionRuntimeState == null
          || _fireRecoveryRuntimeState == null
          || _fireFieldAmendmentRuntimeState == null) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      _fireExposureRuntimeState.RemoveSnapshot(entityId);
      _fireImpactRuntimeState.RemoveSnapshot(entityId);
      _fireDamageStateRuntimeState.RemoveSnapshot(entityId);
      _fireRuntimeProjectionRuntimeState.RemoveSnapshot(entityId);
      _fireRecoveryRuntimeState.RemoveSnapshot(entityId);

      FireTelemetry.Log($"event={FireTelemetryEvents.EntityDestroyCleanup} entity={GameObject.name} id={entityId}");
    }

  }
}
