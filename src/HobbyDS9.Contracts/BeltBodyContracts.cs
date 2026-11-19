using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    // Versioned provisional recipe; values are implementation tuning, not agreed canon.
    public static class BeltMiningPolicy
    {
        public const long Industry=60,Credits=0,WeeklyExtraction=3,WeeklyTransferLimit=3,MineStorage=12;
        public const int SurveyWeeks=2,PowerSupply=3,PowerDemand=3,NaturalHabitabilityBps=0;
        public const string Resource="resource.dilithium";
        public const string InstallationDefinition="infrastructure.belt-mine.v1";
        public const string Tuning="ore120-plus0to4-times60;vacuum-habitability0;survey2;industry60;credits0;materials0;extract3;storage12;transfer3;closed-uncrewed-selfpower3-demand3;one-live-mine-per-belt;queued-cancel-only;standing-delivery-demand;same-system-shared-residual-freight;no-housing;no-colony";
    }
    public enum BeltSurveyStage { Active=1,Completed=2,Cancelled=3,Interrupted=4 }
    public enum BeltMineStage { Queued=1,Active=2,Cancelled=3,Exhausted=4 }
    public enum BeltBodyAction { StartSurvey=1,CancelSurvey=2,EnqueueMine=3,CancelMine=4 }
    public enum BeltBodyError { None=0,Unavailable=1,ActorNotControlled=2,WrongPhase=3,StaleTurn=4,StaleRevision=5,Duplicate=6,InvalidCommand=7,Capacity=8,UnknownBody=9,BodyNotObserved=10,SurveyRequired=11,AlreadySurveyed=12,ShipUnavailable=13,ShipBusy=14,StudyUnavailable=15,SurveyUnavailable=16,ColonyUnavailable=17,WrongSystem=18,MineExists=19,MineUnavailable=20,InsufficientCredits=21,UnsupportedHabitat=22,DepositExhausted=23 }
    public sealed class BeltBodyDefinitionDto
    {
        public string BodyId {get;set;}="";public string FeatureId {get;set;}="";public string SystemId {get;set;}="";public string DisplayName {get;set;}="";
        public int HabitabilityBps {get;set;}public string ResourceDefinitionId {get;set;}=BeltMiningPolicy.Resource;public long InitialDeposit {get;set;}
    }
    public sealed class BeltBodyDepositDto
    {public string BodyId {get;set;}="";public long Remaining {get;set;}public long LocalStock {get;set;}}
    public sealed class BeltBodyObservationDto
    {public string ActorId {get;set;}="";public string BodyId {get;set;}="";public int SurveyedTurn {get;set;}public int ObservedTurn {get;set;}public long ObservedRemaining {get;set;}}
    public sealed class BeltBodySurveyDto
    {
        public string SurveyId {get;set;}="";public string ActorId {get;set;}="";public string ShipId {get;set;}="";public string BodyId {get;set;}="";
        public BeltSurveyStage Stage {get;set;}public int CompletedWeeks {get;set;}public int StartedTurn {get;set;}public int? ResolvedTurn {get;set;}
    }
    public sealed class BeltMineDto
    {
        public string InstallationId {get;set;}="";public string QueueItemId {get;set;}="";public string ActorId {get;set;}="";public string BodyId {get;set;}="";public string ColonyId {get;set;}="";
        public BeltMineStage Stage {get;set;}public int? CompletedTurn {get;set;}public long TotalExtracted {get;set;}public long TotalDelivered {get;set;}
        public int? LastExtractionTurn {get;set;}public int? LastDeliveryTurn {get;set;}public string LatestReport {get;set;}="";
    }
    public sealed class BeltShipmentDto
    {public string InstallationId {get;set;}="";public string ActorId {get;set;}="";public string BodyId {get;set;}="";public string ColonyId {get;set;}="";public string ResourceDefinitionId {get;set;}=BeltMiningPolicy.Resource;public long Amount {get;set;}public int Turn {get;set;}}
    public sealed class BeltBodyCommandDto
    {
        public string ActorId {get;set;}="";public string CommandId {get;set;}="";public int ExpectedTurn {get;set;}public long ExpectedRevision {get;set;}
        public BeltBodyAction Action {get;set;}public string BodyId {get;set;}="";public string? ShipId {get;set;}public string? ColonyId {get;set;}public string? SurveyId {get;set;}public string? MineId {get;set;}
    }
    public sealed class BeltBodyOperationDto
    {public int BaseJournalOffset {get;set;}public BeltBodyCommandDto? Command {get;set;}}
    public sealed class BeltBodyStateDto
    {
        public string PolicyId {get;set;}=BeltBodiesEditionIdentity.Policy;
        public List<BeltBodyDefinitionDto>? Definitions {get;set;}=new List<BeltBodyDefinitionDto>();
        public List<BeltBodyDepositDto>? Deposits {get;set;}=new List<BeltBodyDepositDto>();
        public List<BeltBodyObservationDto>? Observations {get;set;}=new List<BeltBodyObservationDto>();
        public List<BeltBodySurveyDto>? Surveys {get;set;}=new List<BeltBodySurveyDto>();
        public List<BeltMineDto>? Mines {get;set;}=new List<BeltMineDto>();
        public List<BeltShipmentDto>? LastShipments {get;set;}=new List<BeltShipmentDto>();
        public List<BeltBodyOperationDto>? Operations {get;set;}=new List<BeltBodyOperationDto>();
    }
    public sealed class BeltBodyCommandResult
    {
        public BeltBodyError Error {get;}public bool Accepted=>Error==BeltBodyError.None;public int Turn {get;}public long Revision {get;}public string? TargetId {get;}
        public BeltBodyCommandResult(BeltBodyError error,int turn,long revision,string? target=null){Error=error;Turn=turn;Revision=revision;TargetId=target;}
    }
    public sealed class BeltBodyCommandPreview
    {
        public BeltBodyError Error {get;}public bool CanSubmit=>Error==BeltBodyError.None;public long Industry {get;}public long Credits {get;}public int SurveyWeeks {get;}
        public long WeeklyExtraction=>BeltMiningPolicy.WeeklyExtraction;public long WeeklyTransferLimit=>BeltMiningPolicy.WeeklyTransferLimit;
        public bool SelfPowered=>true;public bool Uncrewed=>true;public int PowerSupply=>BeltMiningPolicy.PowerSupply;public int PowerDemand=>BeltMiningPolicy.PowerDemand;
        public BeltBodyCommandPreview(BeltBodyError error,BeltBodyAction action){Error=error;Industry=action==BeltBodyAction.EnqueueMine?BeltMiningPolicy.Industry:0;Credits=action==BeltBodyAction.EnqueueMine?BeltMiningPolicy.Credits:0;SurveyWeeks=action==BeltBodyAction.StartSurvey?BeltMiningPolicy.SurveyWeeks:0;}
    }
    public sealed class BeltMineSnapshot
    {
        public string InstallationId {get;}public string QueueItemId {get;}public string ColonyId {get;}public BeltMineStage Stage {get;}
        public long TotalExtracted {get;}public long TotalDelivered {get;}public long LocalStock {get;}public long StorageCapacity=>BeltMiningPolicy.MineStorage;public long Work {get;}public long CompletedWork {get;}
        public int? LastExtractionTurn {get;}public int? LastDeliveryTurn {get;}public string LatestReport {get;}
        public BeltMineSnapshot(BeltMineDto m,long localStock,long completedWork){InstallationId=m.InstallationId;QueueItemId=m.QueueItemId;ColonyId=m.ColonyId;Stage=m.Stage;TotalExtracted=m.TotalExtracted;TotalDelivered=m.TotalDelivered;LocalStock=localStock;Work=BeltMiningPolicy.Industry;CompletedWork=completedWork;LastExtractionTurn=m.LastExtractionTurn;LastDeliveryTurn=m.LastDeliveryTurn;LatestReport=m.LatestReport;}
    }
    public sealed class BeltBodySnapshot
    {
        public string BodyId {get;}public string FeatureId {get;}public string SystemId {get;}public string DisplayName {get;}public string BodyKind=>"Asteroid belt aggregate";
        public bool CurrentlyObserved {get;}public bool Surveyed {get;}public int? SurveyedTurn {get;}public int? ResourceObservedTurn {get;}public bool ResourceIsCurrent {get;}
        public int? HabitabilityBps {get;}public string? HabitabilityBasis=>Surveyed?"Unassisted biological habitation in vacuum; no supported orbital habitat.":null;public int? NaturalSettlementCapacity=>Surveyed?(int?)0:null;public string? ResourceDefinitionId {get;}public long? ObservedRemaining {get;}public bool SettlementSupported=>false;
        public string SettlementReason=>"No supported orbital habitat; this uncrewed installation provides no housing.";
        public string? ActiveSurveyId {get;}public int CompletedSurveyWeeks {get;}public string? SurveyShipId {get;}
        public BeltMineSnapshot? Mine {get;}public IReadOnlyList<string> SponsorColonyIds {get;}public IReadOnlyList<string> SurveyShipIds {get;}
        public BeltBodySnapshot(BeltBodyDefinitionDto d,bool current,BeltBodyObservationDto? observation,BeltBodySurveyDto? survey,BeltMineSnapshot? mine,IEnumerable<string> sponsors,IEnumerable<string> ships)
        {BodyId=d.BodyId;FeatureId=d.FeatureId;SystemId=d.SystemId;DisplayName=d.DisplayName;CurrentlyObserved=current;Surveyed=observation!=null;SurveyedTurn=observation?.SurveyedTurn;ResourceObservedTurn=observation?.ObservedTurn;ResourceIsCurrent=observation!=null&&current;HabitabilityBps=observation==null?(int?)null:d.HabitabilityBps;ResourceDefinitionId=observation==null?null:d.ResourceDefinitionId;ObservedRemaining=observation?.ObservedRemaining;ActiveSurveyId=survey?.SurveyId;CompletedSurveyWeeks=survey?.CompletedWeeks??0;SurveyShipId=survey?.ShipId;Mine=mine;SponsorColonyIds=new List<string>(sponsors).AsReadOnly();SurveyShipIds=new List<string>(ships).AsReadOnly();}
    }
    public sealed class BeltBodiesSnapshot
    {
        public string ActorId {get;}public int Turn {get;}public long Revision {get;}public IReadOnlyList<BeltBodySnapshot> Bodies {get;}
        public BeltBodiesSnapshot(string actor,int turn,long revision,IEnumerable<BeltBodySnapshot> bodies){ActorId=actor;Turn=turn;Revision=revision;Bodies=new List<BeltBodySnapshot>(bodies).AsReadOnly();}
    }
}
