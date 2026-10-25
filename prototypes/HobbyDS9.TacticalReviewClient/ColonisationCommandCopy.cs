using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;
namespace HobbyDS9.TacticalReviewClient
{
    internal static class ColonisationCommandCopy
    {
        private static List<ResourceAmountDto>? Resources(List<ResourceAmountDto>? rows)
        {
            if(rows==null)return null;
            var copy=new List<ResourceAmountDto>(rows.Count);
            foreach(var row in rows){if(row==null)throw new ArgumentException("Missing material row.");copy.Add(new ResourceAmountDto{ResourceDefinitionId=row.ResourceDefinitionId,Amount=row.Amount});}
            return copy;
        }
        internal static ColonisationCommandDto Copy(ColonisationCommandDto c)=>new ColonisationCommandDto {
            CommandId=c.CommandId,ActorId=c.ActorId,Turn=c.Turn,Revision=c.Revision,Action=c.Action,
            StartSurvey=c.StartSurvey==null?null:new PlanetSurveyOrderDto{ShipId=c.StartSurvey.ShipId,PlanetId=c.StartSurvey.PlanetId},
            CancelSurvey=c.CancelSurvey==null?null:new CancelPlanetSurveyDto{SurveyId=c.CancelSurvey.SurveyId},
            Embark=c.Embark==null?null:new EmbarkSettlersDto{ShipId=c.Embark.ShipId,ColonyId=c.Embark.ColonyId,CohortId=c.Embark.CohortId,Population=c.Embark.Population,Food=c.Embark.Food,Materials=Resources(c.Embark.Materials)},
            Unload=c.Unload==null?null:new UnloadSettlersDto{ManifestId=c.Unload.ManifestId,ColonyId=c.Unload.ColonyId},
            Deploy=c.Deploy==null?null:new DeploySettlersDto{ManifestId=c.Deploy.ManifestId,PlanetId=c.Deploy.PlanetId},
            Withdraw=c.Withdraw==null?null:new WithdrawDeploymentDto{IntentId=c.Withdraw.IntentId},
            Evacuate=c.Evacuate==null?null:new EvacuateFoundingColonyDto{CampId=c.Evacuate.CampId,ShipId=c.Evacuate.ShipId},
            Deliver=c.Deliver==null?null:new DeliverCarriedSuppliesDto{ManifestId=c.Deliver.ManifestId,ColonyId=c.Deliver.ColonyId,Food=c.Deliver.Food,Materials=Resources(c.Deliver.Materials)},
            EnqueueKit=c.EnqueueKit==null?null:new EnqueueSettlementKitDto{ColonyId=c.EnqueueKit.ColonyId},
            LoadSupplies=c.LoadSupplies==null?null:new LoadCarriedSuppliesDto{ShipId=c.LoadSupplies.ShipId,SourceColonyId=c.LoadSupplies.SourceColonyId,Food=c.LoadSupplies.Food,Materials=Resources(c.LoadSupplies.Materials)}
        };
    }
}
