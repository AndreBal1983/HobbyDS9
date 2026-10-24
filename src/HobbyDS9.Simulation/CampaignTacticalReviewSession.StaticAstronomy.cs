using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private readonly GeneratedStaticAstronomyIndex? astronomyIndex;
        public static CampaignTacticalReviewSession StartStaticAstronomy(CampaignPlayableFleetPackageDto template, NaturalGalaxySettingsDto settings)
        {
            ValidatePlayablePackage(template);
            var catalog = template.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            return StartGeneratedFrozen(template, GeneratedCampaignFactory.Frozen(NaturalGalaxyGenerator.Generate(settings, catalog), catalog), staticAstronomy: true);
        }
        public static CampaignTacticalReviewSession LoadStaticAstronomy(CampaignPlayableFleetPackageDto template, CampaignGeneratedSaveDto save)
        {
#if HOBBYDS9_CPU_PROFILE
            using var cpuProfileScope = CpuProfile.Measure("load.total");
#endif

            ValidatePlayablePackage(template);
            Require(save != null && save.FormatVersion == CampaignStaticAstronomyIdentity.Format && save.ProfileId == CampaignStaticAstronomyIdentity.Profile
                && save.PolicyId == CampaignStaticAstronomyIdentity.Policy && save.TemplatePackageHash == template.PackageHash
                && save.Map != null && save.Campaign != null && save.Discoveries != null, "Unsupported/incomplete static-astronomy save; no migration from v1.");
            var map = GeneratedCampaignFactory.Frozen(save.Map, template.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!);
            var package = GeneratedCampaignFactory.Build(template, map, staticAstronomy: true);
            Require(save.PackageHash == package.PackageHash, "Different static-astronomy content/template identity.");
            var replay = ReplayPlayable(package, save.Campaign, StartGeneratedFrozen(template, map, staticAstronomy: true));
            #if HOBBYDS9_CPU_PROFILE
            Require(CpuProfile.Compare("equality.generated-root", () => FrontierSame(replay.ExportGeneratedSave(), save)), "Static-astronomy checkpoint differs from complete accepted-operation replay.");
#else
            Require(FrontierSame(replay.ExportGeneratedSave(), save), "Static-astronomy checkpoint differs from complete accepted-operation replay.");
#endif
            return replay;
        }
        public CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor)
        {
#if HOBBYDS9_CPU_PROFILE
            using var cpuProfileScope = CpuProfile.Measure("actor.astronomy");
#endif

            lock (gate)
            {
                Require(astronomyIndex != null && Actor(actor) == CampaignTacticalError.None, "Only a controlled static-astronomy actor may capture charted phenomena.");
                var game = state.Campaign.GameState;
                var known = game.NavigationKnowledge!.Single(k => k.CivilizationId == actor).Nodes!;
                return new CampaignStaticAstronomySnapshot(actor, generatedMap!.ContentHash, game.TurnNumber, game.StateRevision, astronomyIndex.VisibleKnown(known));
            }
        }
    }
}
