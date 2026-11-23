using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        // Publications and their frozen catalogues are immutable. Current access/support
        // is checked by the caller on every capture; only immutable compiled facts cache.
        private readonly Dictionary<(string Actor,string Blueprint,int Revision),ActivityBuildComparisonDto> productionComparisons
            =new Dictionary<(string,string,int),ActivityBuildComparisonDto>();
        private ActivityBuildComparisonDto? ProductionComparison(GameStateDto game,BlueprintRevisionDto record,bool supported)
        {
            if(!supported)return null;
            var key=(record.OwnerCivilizationId,record.BlueprintId,record.Revision);
            if(productionComparisons.TryGetValue(key,out var cached))return CopyProductionComparison(cached);
            var catalogue=new ShipDesignCatalogueSnapshot(record.FrozenCatalogue!);
            var publication=CampaignBlueprints.Restore(record);
            var access=CampaignBlueprints.Access(definitions,game.Civilizations!.Single(c=>c.CivilizationId==record.OwnerCivilizationId));
            SupportedPublishedTacticalFit? tactical=null;int? work=null,contact=null;
            if(colonisation!=null)
            {
                var ship=PublishedTacticalSupportCompiler.EvaluateColonisation(catalogue,publication,access,colonisation.Support).Ship;
                if(ship!=null){tactical=ship.Tactical;work=ship.StudyWork;contact=ship.ContactCapacity;}
            }
            else if(activities!=null)
            {
                var ship=PublishedTacticalSupportCompiler.EvaluateActivities(catalogue,publication,access,activities.Support).Ship;
                if(ship!=null){tactical=ship.Tactical;work=ship.StudyWork;contact=ship.ContactCapacity;}
            }
            if(tactical==null)return null;
            var result=new ActivityBuildComparisonDto{StudyWork=work,ContactCapacity=contact,MaxHull=tactical.MaxHull,Armor=tactical.Armor,
                // The current supported-publication compiler explicitly emits zero shield sectors.
                ShieldCapacity=tactical.ZeroShieldSectors.Count==4?0:(long?)null,Speed=tactical.Capabilities.Speed,MovementBudget=tactical.MovementBudget,
                Weapons=tactical.Weapons.Select(w=>new ActivityBuildWeaponComparisonDto{Class=w.Class,Coverage=w.Coverage,Damage=w.Damage,
                    OpportunitiesPerRound=w.OpportunitiesPerRound,CadenceRounds=w.CadenceRounds,AccuracyBps=w.AccuracyBps,TrackingBps=w.TrackingBps,
                    RequiredPower=w.RequiredPower,AllocatedPower=w.AllocatedPower,Powered=w.Powered}).ToList()};
            if(productionComparisons.Count<2048)productionComparisons.Add(key,result);
            return CopyProductionComparison(result);
        }
        private static ActivityBuildComparisonDto CopyProductionComparison(ActivityBuildComparisonDto c)=>new ActivityBuildComparisonDto{
            StudyWork=c.StudyWork,ContactCapacity=c.ContactCapacity,MaxHull=c.MaxHull,Armor=c.Armor,ShieldCapacity=c.ShieldCapacity,Speed=c.Speed,MovementBudget=c.MovementBudget,
            Weapons=c.Weapons?.Select(w=>new ActivityBuildWeaponComparisonDto{Class=w.Class,Coverage=w.Coverage,Damage=w.Damage,
                OpportunitiesPerRound=w.OpportunitiesPerRound,CadenceRounds=w.CadenceRounds,AccuracyBps=w.AccuracyBps,TrackingBps=w.TrackingBps,
                RequiredPower=w.RequiredPower,AllocatedPower=w.AllocatedPower,Powered=w.Powered}).ToList()};
    }
}
