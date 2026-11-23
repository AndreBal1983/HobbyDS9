using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class BeltBodyCopies
    {
        internal static BeltBodyDefinitionDto Copy(BeltBodyDefinitionDto d)=>new BeltBodyDefinitionDto{BodyId=d.BodyId,FeatureId=d.FeatureId,SystemId=d.SystemId,DisplayName=d.DisplayName,HabitabilityBps=d.HabitabilityBps,ResourceDefinitionId=d.ResourceDefinitionId,InitialDeposit=d.InitialDeposit};
        internal static BeltBodyDepositDto Copy(BeltBodyDepositDto d)=>new BeltBodyDepositDto{BodyId=d.BodyId,Remaining=d.Remaining,LocalStock=d.LocalStock};
        internal static BeltBodyObservationDto Copy(BeltBodyObservationDto d)=>new BeltBodyObservationDto{ActorId=d.ActorId,BodyId=d.BodyId,SurveyedTurn=d.SurveyedTurn,ObservedTurn=d.ObservedTurn,ObservedRemaining=d.ObservedRemaining};
        internal static BeltBodySurveyDto Copy(BeltBodySurveyDto d)=>new BeltBodySurveyDto{SurveyId=d.SurveyId,ActorId=d.ActorId,ShipId=d.ShipId,BodyId=d.BodyId,Stage=d.Stage,CompletedWeeks=d.CompletedWeeks,StartedTurn=d.StartedTurn,ResolvedTurn=d.ResolvedTurn};
        internal static BeltMineDto Copy(BeltMineDto d)=>new BeltMineDto{InstallationId=d.InstallationId,QueueItemId=d.QueueItemId,ActorId=d.ActorId,BodyId=d.BodyId,ColonyId=d.ColonyId,Stage=d.Stage,CompletedTurn=d.CompletedTurn,TotalExtracted=d.TotalExtracted,TotalDelivered=d.TotalDelivered,LastExtractionTurn=d.LastExtractionTurn,LastDeliveryTurn=d.LastDeliveryTurn,LatestReport=d.LatestReport};
        internal static BeltShipmentDto Copy(BeltShipmentDto d)=>new BeltShipmentDto{InstallationId=d.InstallationId,ActorId=d.ActorId,BodyId=d.BodyId,ColonyId=d.ColonyId,ResourceDefinitionId=d.ResourceDefinitionId,Amount=d.Amount,Turn=d.Turn};
        internal static BeltBodyCommandDto Copy(BeltBodyCommandDto c)=>new BeltBodyCommandDto{ActorId=c.ActorId,CommandId=c.CommandId,ExpectedTurn=c.ExpectedTurn,ExpectedRevision=c.ExpectedRevision,Action=c.Action,BodyId=c.BodyId,ShipId=c.ShipId,ColonyId=c.ColonyId,SurveyId=c.SurveyId,MineId=c.MineId};
        internal static BeltBodyStateDto Copy(BeltBodyStateDto s)=>new BeltBodyStateDto{PolicyId=s.PolicyId,Definitions=s.Definitions?.Select(Copy).ToList(),Deposits=s.Deposits?.Select(Copy).ToList(),Observations=s.Observations?.Select(Copy).ToList(),Surveys=s.Surveys?.Select(Copy).ToList(),Mines=s.Mines?.Select(Copy).ToList(),LastShipments=s.LastShipments?.Select(Copy).ToList(),Operations=s.Operations?.Select(o=>new BeltBodyOperationDto{BaseJournalOffset=o.BaseJournalOffset,Command=o.Command==null?null:Copy(o.Command)}).ToList()};
    }
}
