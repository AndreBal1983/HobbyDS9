using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private sealed class ColonisationContext
        {
            internal readonly string PackageHash;
            internal readonly ColonisationPolicyDto Policy;
            internal readonly ColonisationSupportCatalogueDto Support;
            internal readonly FrozenColonisationPlanetsDto FrozenPlanets;
            internal CampaignColonisationStateDto State;
            internal List<ColonisationProductionItemDto> Production=new List<ColonisationProductionItemDto>();
            internal List<ColonisationProductionReceiptDto> History=new List<ColonisationProductionReceiptDto>();
            internal List<ColonisationOperationDto> Journal=new List<ColonisationOperationDto>();
            internal ColonisationMovementOwnershipDto? MovementOwnership;
            internal ColonisationSettlementProofDto? SettlementProof;
            internal ColonisationContext(string hash,ColonisationPolicyDto policy,ColonisationSupportCatalogueDto support,CampaignColonisationStateDto state,FrozenColonisationPlanetsDto frozen)
            {PackageHash=hash;Policy=ColonisationData.Copy(policy);Support=ColonisationSupportCopies.Copy(support);State=ColonisationData.Copy(state);FrozenPlanets=ColonisationData.Copy(frozen);}
            internal ColonisationContext Copy()=>new ColonisationContext(PackageHash,Policy,Support,State,FrozenPlanets){
                Production=Production.Select(ColonisationData.Copy).ToList(),History=History.Select(ColonisationData.Copy).ToList(),
                Journal=Journal.Select(o=>new ColonisationOperationDto{Colonisation=o.Colonisation==null?null:ColonisationData.Copy(o.Colonisation),Existing=o.Existing==null?null:CopyActivityOperation(o.Existing)}).ToList(),
                MovementOwnership=MovementOwnership==null?null:ColonisationData.Copy(MovementOwnership),SettlementProof=SettlementProof==null?null:ColonisationData.Copy(SettlementProof)};
        }
        private ColonisationContext? colonisation;
        private bool EstablishedColony(string colonyId)=>!colonisation!.State.CampSources!.Any(c=>c.ColonyId==colonyId&&c.Stage!=ColonyEstablishmentStage.Established);
        public static CampaignColonisationPackageDto CreateColonisationPackage(CampaignActivitiesPackageDto activitiesPackage,ShipDesignCatalogueDto catalogue,
            IReadOnlyList<ShipClassDraftDto> ready,ColonisationSupportCatalogueDto support,ColonisationPolicyDto policy)
        {
            var package=new CampaignColonisationPackageDto{Activities=activitiesPackage,Catalogue=BlueprintCopies.Copy(catalogue),ReadyClasses=ready.Select(BlueprintCopies.Copy).ToList(),Support=ColonisationSupportCopies.Copy(support),Settlement=ColonisationData.Copy(policy)};
            package.PackageHash=ColonisationData.PackageHash(package);return package;
        }
        public static CampaignTacticalReviewSession StartColonisation(CampaignColonisationPackageDto package,NaturalGalaxySettingsDto settings)
        {
            CampaignTacticalReviewData.Require(package!=null&&package.PackageHash==ColonisationData.PackageHash(package),"Different colonisation package.");
            var definitions=package!.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            var map=NaturalGalaxyGenerator.Generate(settings,definitions);
            return StartActivitiesFrozen(package.Activities,GeneratedCampaignFactory.Frozen(map,definitions),null,package);
        }
        private PlanetaryOwnershipContext PlanetaryOwner(GameStateDto game)=>PlanetaryOwnershipContext.CreateForColonisation(definitions,game,
            colonisation!.State,activities!.State.Minors!);
        private WorldEconomySnapshot ProjectOwnedWorld(ColonyStateDto colony,GameStateDto game)=>WorldEconomy.Project(colony,game,definitions,colonisation==null?null:PlanetaryOwner(game));
        private PlanetaryOwnershipContext MovementOwner(GameStateDto start)
        {
            var era=colonisation!.MovementOwnership;
            CampaignTacticalReviewData.Require(era!=null&&era.Turn==start.TurnNumber&&era.Revision==start.StateRevision&&era.PlanetControls!=null&&era.ColonyStages!=null,"Exact movement planetary ownership era required.");
            return PlanetaryOwnershipContext.CreateForColonisationEra(definitions,start,era!.PlanetControls!,activities!.State.Minors!,era.ColonyStages!);
        }
        private void ValidateOwnedGame(GameStateDto game)
        {
            if(colonisation==null)FoundationValidator.ValidateGameState(game,definitions).ThrowIfInvalid(nameof(game));
            else FoundationValidator.ValidatePlanetaryGameState(game,definitions,PlanetaryOwner(game)).ThrowIfInvalid(nameof(game));
        }
        private void RequireOwnedChanges(GameStateDto before,GameStateDto after,bool economy,bool research=false,bool production=false,bool movement=false)
        {
            if(colonisation==null)SimulationSession.RequirePhaseChanges(before,after,economy,research,production,movement);
            else SimulationSession.RequirePlanetaryPhaseChanges(before,after,definitions,PlanetaryOwner(before),PlanetaryOwner(after),economy,research,production,movement);
        }
    }
}
