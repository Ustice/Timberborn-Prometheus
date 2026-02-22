using Bindito.Core;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace Mods.Prometheus.Scripts {
  [Context("Game")]
  public class PrometheusConfigurator : Configurator {

    protected override void Configure() {
      Bind<FireSuppressionRuntimeState>().AsSingleton();
      Bind<FireTuningRuntimeState>().AsSingleton();
      Bind<FireSimulationRuntimeState>().AsSingleton();
      Bind<FireDispatchScoringRuntimeState>().AsSingleton();
      Bind<FireEntityRegistryRuntimeState>().AsSingleton();
      Bind<FireImpactRuntimeState>().AsSingleton();
      Bind<FireDamageStateRuntimeState>().AsSingleton();
      Bind<FireWaterContextRuntimeState>().AsSingleton();
      Bind<FireRecoveryRuntimeState>().AsSingleton();
      Bind<FireFestivalRuntimeState>().AsSingleton();

      Bind<FireResponseProfile>().AsTransient();
      Bind<FireFestivalRiskController>().AsTransient();
      Bind<FireWaterContextProbe>().AsTransient();
      Bind<FireSuppressionProfileApplier>().AsTransient();
      Bind<FireSimulationController>().AsTransient();
      Bind<FireImpactController>().AsTransient();
      Bind<FireDamageStateController>().AsTransient();
      Bind<FireDamageEffectApplier>().AsTransient();
      Bind<FireRecoveryController>().AsTransient();
      Bind<FireRecoveryEffectApplier>().AsTransient();
      Bind<FireWorkplaceEffectApplier>().AsTransient();
      Bind<FireBeaverEffectApplier>().AsTransient();
      Bind<PrometheusFireDebugFragment>().AsSingleton();

      MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();

      MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule() {
      var builder = new TemplateModule.Builder();
      builder.AddDecorator<FireResponseProfileSpec, FireResponseProfile>();
      builder.AddDecorator<FireResponseProfileSpec, FireFestivalRiskController>();
      builder.AddDecorator<FireResponseProfileSpec, FireWaterContextProbe>();
      builder.AddDecorator<FireResponseProfileSpec, FireSuppressionProfileApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireSimulationController>();
      builder.AddDecorator<FireResponseProfileSpec, FireImpactController>();
      builder.AddDecorator<FireResponseProfileSpec, FireDamageStateController>();
      builder.AddDecorator<FireResponseProfileSpec, FireDamageEffectApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireRecoveryController>();
      builder.AddDecorator<FireResponseProfileSpec, FireRecoveryEffectApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireWorkplaceEffectApplier>();
      builder.AddDecorator<FireResponseProfileSpec, FireBeaverEffectApplier>();
      return builder.Build();
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

  }
}