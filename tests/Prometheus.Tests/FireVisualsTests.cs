using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireVisualsTests
    {

        [Fact]
        public void FireVisualEffectRules_DryBurningFireProducesReadableEffects_Test()
        {
            var intensity = FireVisualEffectRules.ComputeIntensity(
              TestSupport.CreateExposureSnapshot(burning: true, intensity: 0.8f),
              new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Burning, 0.7f, 0.5f, 4),
              FireVisualEffectTuning.Default);

            TestSupport.True(intensity.HasAnyVisibleEffect);
            TestSupport.NearlyEqual(0f, intensity.Embers);
            TestSupport.True(intensity.Smoke > 0.4f);
            TestSupport.True(intensity.Fire > 0.7f);
            TestSupport.True(intensity.Char > 0.4f);
            TestSupport.NearlyEqual(0f, intensity.Steam);
        }

        [Fact]
        public void FireVisualEffectRules_EmberPressureShowsFieldPressureWithoutLocalBurningSparks_Test()
        {
            var exposed = FireVisualEffectRules.ComputeIntensity(
              new FireExposureSnapshot(false, 0.15f, 0.2f, 0.65f, 0.18f, 0.25f, 0f, 0.35f, 0.9f, "Grid"),
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Healthy, 0f, 0f, 0),
              FireVisualEffectTuning.Default);
            var burning = FireVisualEffectRules.ComputeIntensity(
              new FireExposureSnapshot(true, 0.7f, 0.75f, 0.9f, 0.45f, 1f, 0.2f, 0f, 1f, "Grid"),
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Burning, 0.45f, 0.2f, 1),
              FireVisualEffectTuning.Default);

            TestSupport.True(exposed.Embers > 0.4f);
            TestSupport.NearlyEqual(0f, exposed.Smoke);
            TestSupport.NearlyEqual(0f, exposed.Fire);
            TestSupport.NearlyEqual(0f, burning.Embers);
            TestSupport.True(burning.Fire > 0.6f);
        }

        [Fact]
        public void FireVisualEffectRules_LowEmberNoiseDoesNotCreateVisualSpam_Test()
        {
            var intensity = FireVisualEffectRules.ComputeIntensity(
              new FireExposureSnapshot(false, 0.05f, 0.08f, 0.12f, 0.05f, 0.1f, 0f, 0.6f, 1f, "Grid"),
              new FireDamageStateSnapshot(FireDamageCategory.Crop, FireDamageState.Healthy, 0f, 0f, 0),
              FireVisualEffectTuning.Default);

            TestSupport.NearlyEqual(0f, intensity.Embers);
            TestSupport.NearlyEqual(0f, intensity.Fire);
        }

        [Fact]
        public void FireVisualEffectRules_HealthySmokeFieldDoesNotRenderAsFireWave_Test()
        {
            var intensity = FireVisualEffectRules.ComputeIntensity(
              new FireExposureSnapshot(false, 0f, 0f, 0f, 0.9f, 0f, 0f, 0.2f, 1f, "Grid"),
              new FireDamageStateSnapshot(FireDamageCategory.Crop, FireDamageState.Healthy, 0f, 0f, 0),
              FireVisualEffectTuning.Default);

            TestSupport.NearlyEqual(0f, intensity.Smoke);
            TestSupport.NearlyEqual(0f, intensity.Fire);
        }

        [Fact]
        public void FireVisualEffectRules_CropBurnsDoNotEmitSmokePlumes_Test()
        {
            var intensity = FireVisualEffectRules.ComputeIntensity(
              new FireExposureSnapshot(true, 1f, 0.75f, 0.8f, 0.4f, 1f, 0.2f, 0f, 1f, "Grid"),
              new FireDamageStateSnapshot(FireDamageCategory.Crop, FireDamageState.Burning, 0.75f, 0.3f, 2),
              FireVisualEffectTuning.Default);

            TestSupport.NearlyEqual(0f, intensity.Smoke);
            TestSupport.True(intensity.Fire > 0.8f);
        }

        [Fact]
        public void FireVisualEffectRules_MoistureTradesFireForSteam_Test()
        {
            var dryExposure = TestSupport.CreateExposureSnapshot(burning: true, intensity: 0.8f, moistureDampening: 0f);
            var wetExposure = TestSupport.CreateExposureSnapshot(burning: true, intensity: 0.8f, moistureDampening: 0.9f);
            var damage = new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Burning, 0.65f, 0.5f, 3);
            var dry = FireVisualEffectRules.ComputeIntensity(dryExposure, damage, FireVisualEffectTuning.Default);
            var wet = FireVisualEffectRules.ComputeIntensity(wetExposure, damage, FireVisualEffectTuning.Default);

            TestSupport.True(wet.Steam > dry.Steam);
            TestSupport.True(wet.Fire < dry.Fire);
            TestSupport.True(wet.Smoke < dry.Smoke);
        }

        [Fact]
        public void FireVisualEffectRules_DeadDamageStateKeepsCharWithoutFire_Test()
        {
            var intensity = FireVisualEffectRules.ComputeIntensity(
              TestSupport.CreateExposureSnapshot(burning: true, intensity: 1f),
              new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Dead, 1f, 1f, 12),
              FireVisualEffectTuning.Default);

            TestSupport.NearlyEqual(0f, intensity.Fire);
            TestSupport.True(intensity.Char > 0.95f);
            TestSupport.True(intensity.Smoke > 0f);
        }

        [Fact]
        public void FireVisualEffectRules_DeadTreeStopsFlameButKeepsTemporarySmoke_Test()
        {
            var deadTree = FireVisualEffectRules.ComputeIntensity(
              new FireExposureSnapshot(true, 1f, 0.7f, 0.4f, 0.5f, 1f, 0.32f, 0f, 1f, "Grid"),
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Dead, 1f, 1f, 12),
              FireVisualEffectTuning.Default);
            var stumpStage = FireVisualEffectRules.ComputeIntensity(
              new FireExposureSnapshot(true, 1f, 0.7f, 0.4f, 0.5f, 1f, 0.45f, 0f, 1f, "Grid"),
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Dead, 1f, 1f, 12),
              FireVisualEffectTuning.Default);
            var lateStump = FireVisualEffectRules.ComputeIntensity(
              new FireExposureSnapshot(true, 1f, 0.7f, 0.4f, 0.5f, 1f, 0.62f, 0f, 1f, "Grid"),
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Dead, 1f, 1f, 12),
              FireVisualEffectTuning.Default);

            TestSupport.NearlyEqual(0f, deadTree.Fire);
            TestSupport.NearlyEqual(0f, stumpStage.Fire);
            TestSupport.True(deadTree.Smoke > 0.25f);
            TestSupport.True(stumpStage.Smoke > 0f);
            TestSupport.NearlyEqual(0f, lateStump.Smoke);
            TestSupport.True(deadTree.Char > 0.95f);
        }

        [Fact]
        public void FireVisualEffectRules_EvaporatedMoistureBrownsVegetation_Test()
        {
            var intensity = FireVisualEffectRules.ComputeIntensity(
              TestSupport.CreateExposureSnapshot(burning: false, intensity: 0f, moistureDampening: 0f),
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Healthy, 0f, 0f, 0),
              FireVisualEffectTuning.Default);

            TestSupport.True(intensity.Desiccation > 0.95f);
            TestSupport.NearlyEqual(0f, intensity.Fire);
            TestSupport.NearlyEqual(0f, intensity.Char);
        }

        [Fact]
        public void FireVisualEffectRules_BurnedOutFuelKeepsCharredRemnant_Test()
        {
            var intensity = FireVisualEffectRules.ComputeIntensity(
              new FireExposureSnapshot(false, 0f, 0f, 0f, 0.08f, 0f, 1f, 0f, 1f, "BurnedOut"),
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Dead, 1f, 1f, 4),
              FireVisualEffectTuning.Default);

            TestSupport.True(intensity.Char > 0.95f);
            TestSupport.True(intensity.Desiccation > 0.95f);
            TestSupport.NearlyEqual(0f, intensity.Fire);
        }

        [Fact]
        public void FireNaturalResourceVisualRules_TreesProgressFromDryToCharredStump_Test()
        {
            var healthy = FireNaturalResourceVisualRules.DetermineTreeStage(
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Healthy, 0f, 0f, 0),
              FireExposureRules.CreateColdSnapshot());
            var dried = FireNaturalResourceVisualRules.DetermineTreeStage(
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Healthy, 0f, 0f, 0),
              new FireExposureSnapshot(false, 0f, 0f, 0f, 0f, 0f, 0f, 0.2f, 1f, "Grid"));
            var driedAndCharred = FireNaturalResourceVisualRules.DetermineTreeStage(
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Scorched, 0.35f, 0.2f, 1),
              new FireExposureSnapshot(true, 0.5f, 0.45f, 0.25f, 0.2f, 0.8f, 0.12f, 0f, 1f, "Grid"));
            var deadAndCharred = FireNaturalResourceVisualRules.DetermineTreeStage(
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Burning, 0.7f, 0.6f, 3),
              new FireExposureSnapshot(true, 0.75f, 0.6f, 0.5f, 0.25f, 1f, 0.2f, 0f, 1f, "Grid"));
            var stumpByVisualFuel = FireNaturalResourceVisualRules.DetermineTreeStage(
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Burning, 0.7f, 0.6f, 3),
              new FireExposureSnapshot(true, 0.75f, 0.6f, 0.5f, 0.25f, 1f, 0.45f, 0f, 1f, "Grid"));
            var stumpAndCharred = FireNaturalResourceVisualRules.DetermineTreeStage(
              new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Dead, 1f, 1f, 4),
              FireExposureRules.CreateBurnedOutSnapshot());

            TestSupport.Equal(FireNaturalResourceVisualStage.Healthy, healthy);
            TestSupport.Equal(FireNaturalResourceVisualStage.Dried, dried);
            TestSupport.Equal(FireNaturalResourceVisualStage.DriedAndCharred, driedAndCharred);
            TestSupport.Equal(FireNaturalResourceVisualStage.DeadAndCharred, deadAndCharred);
            TestSupport.Equal(FireNaturalResourceVisualStage.StumpAndCharred, stumpByVisualFuel);
            TestSupport.Equal(FireNaturalResourceVisualStage.StumpAndCharred, stumpAndCharred);
            TestSupport.True(FireNaturalResourceVisualRules.UsesDriedVisual(deadAndCharred));
            TestSupport.False(FireNaturalResourceVisualRules.UsesStumpVisual(deadAndCharred));
            TestSupport.True(FireNaturalResourceVisualRules.UsesStumpVisual(stumpAndCharred));
        }

        [Fact]
        public void FireNaturalResourceVisualRules_MapTreeStagesToNativeModelChildren_Test()
        {
            TestSupport.Equal("#Alive", FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.Healthy));
            TestSupport.Equal("#Dying", FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.Dried));
            TestSupport.Equal("#Dying", FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.DriedAndCharred));
            TestSupport.Equal("#Dead", FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.DeadAndCharred));
            TestSupport.Equal("#Leftover", FireNaturalResourceVisualRules.ModelStateName(FireNaturalResourceVisualStage.StumpAndCharred));
        }

        [Fact]
        public void FireVisualPreset_DefaultsUsePromotedAuthoringValues_Test()
        {
            var preset = new FireVisualPreset();
            var smoke = preset.GetParticle(FireVisualEffectKind.Smoke);
            TestSupport.Equal("FoodFactorySmoke", smoke.SourceName);
            TestSupport.NearlyEqual(2.3f, smoke.Lifetime);
            TestSupport.NearlyEqual(0f, smoke.Spread);

            var ash = preset.GetParticle(FireVisualEffectKind.Ash);
            TestSupport.Equal("BadwaterRigSmoke", ash.SourceName);
            TestSupport.NearlyEqual(0.55f, ash.Intensity);
            TestSupport.NearlyEqual(0.4f, ash.Emission);
            TestSupport.NearlyEqual(0.9f, ash.Position.y);
            TestSupport.NearlyEqual(0.2f, ash.Size);
            TestSupport.NearlyEqual(0.75f, ash.Lifetime);
            TestSupport.NearlyEqual(0.25f, ash.Spread);

            var steam = preset.GetParticle(FireVisualEffectKind.Steam);
            TestSupport.Equal("CoffeeBrewerySmoke", steam.SourceName);
            TestSupport.NearlyEqual(0.35f, steam.Position.y);
            TestSupport.NearlyEqual(0.7f, steam.Velocity.y);
            TestSupport.NearlyEqual(0.8f, steam.Spread);

            var fire = preset.GetParticle(FireVisualEffectKind.Fire);
            TestSupport.Equal("CampfireFire", fire.SourceName);
            TestSupport.NearlyEqual(0.25f, fire.Position.x);
            TestSupport.NearlyEqual(0.15f, fire.Position.z);
            TestSupport.NearlyEqual(1.2f, fire.Lifetime);
            TestSupport.NearlyEqual(0f, fire.Spread);
            TestSupport.NearlyEqual(-0.15f, fire.Gravity);
            TestSupport.Equal(FireVisualSizeOverLifetimePreset.Swell, fire.SizeOverLifetime);

            var sparks = preset.GetParticle(FireVisualEffectKind.Sparks);
            TestSupport.Equal("Sparks_Trail", sparks.SourceName);
            TestSupport.NearlyEqual(0.7f, sparks.Intensity);
            TestSupport.NearlyEqual(0.55f, sparks.Emission);
            TestSupport.NearlyEqual(1.4f, sparks.Spread);
            TestSupport.NearlyEqual(-0.25f, sparks.Gravity);
            TestSupport.NearlyEqual(0.4f, sparks.NoiseStrength);
        }

        [Fact]
        public void FireNativeParticleSourceCatalog_ScoresRuntimePreferredSources_Test()
        {
            TestSupport.True(FireNativeParticleSourceCatalog.ScoreSearchableText(FireVisualEffectKind.Sparks, "Root/Sparks_Trail") > FireNativeParticleSourceCatalog.ScoreSearchableText(FireVisualEffectKind.Sparks, "Root/Common_Trail_Sparks"));
            TestSupport.True(FireNativeParticleSourceCatalog.ScoreSearchableText(FireVisualEffectKind.Smoke, "Root/SmelterSmoke") > FireNativeParticleSourceCatalog.ScoreSearchableText(FireVisualEffectKind.Smoke, "Root/BakerySmoke"));
            TestSupport.True(FireNativeParticleSourceCatalog.ScoreSearchableText(FireVisualEffectKind.Fire, "Root/CampfireFire") > FireNativeParticleSourceCatalog.ScoreSearchableText(FireVisualEffectKind.Fire, "Root/BrazierFire"));
            TestSupport.True(FireNativeParticleSourceCatalog.ScoreSearchableText(FireVisualEffectKind.Steam, "Root/SteamEngineSmoke") > FireNativeParticleSourceCatalog.ScoreSearchableText(FireVisualEffectKind.Steam, "Root/Smoke", firstParticleAlphaIsSoft: true));
        }

    }
}
