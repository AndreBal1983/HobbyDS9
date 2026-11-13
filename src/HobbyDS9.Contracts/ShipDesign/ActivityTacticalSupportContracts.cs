using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.Contracts.ShipDesign
{
    public static class ActivityTacticalSupportIdentity
    {
        public const int Format=3;
        public const string Schema="published-tactical-activity-support-v3",Compiler="published-tactical-activity-compiler-v3";
    }
    public enum ActivityModuleKind { Laboratory=1, DiplomaticSuite=2 }
    // Dedicated v3 composition, never admitted as old tactical-support-v2 metadata.
    public sealed class ActivityTacticalSupportCatalogueDto
    {
        public int FormatVersion {get;set;}
        public string SchemaId {get;set;}="";
        public string CompilerId {get;set;}="";
        public string CatalogueId {get;set;}="";
        public int Revision {get;set;}
        public TacticalSupportCatalogueDto? Tactical {get;set;}
        public List<ActivityModuleSupportDto>? Modules {get;set;}
    }
    public sealed class ActivityModuleSupportDto
    {
        public string ComponentVersionId {get;set;}="";
        public ActivityModuleKind Kind {get;set;}
        public long SurveyPower {get;set;}
        public int StudyWork {get;set;}
        public int ContactCapacity {get;set;}
    }
    public sealed class ActivitySupportedShip
    {
        private readonly ActivityTacticalSupportCatalogueDto metadata;
        public SupportedPublishedTacticalFit Tactical {get;}
        public string CompilerId=>ActivityTacticalSupportIdentity.Compiler;
        public string SupportCatalogueId=>metadata.CatalogueId;
        public int SupportCatalogueRevision=>metadata.Revision;
        public int StudyWork {get;}
        public int ContactCapacity {get;}
        public string SupportHash=>Tactical.SupportHash;
        // Advisory public snapshot only. Future admission/load must independently rederive
        // source/capabilities from the complete externally pinned package, never trust this constructor.
        public ActivitySupportedShip(SupportedPublishedTacticalFit tactical,int studyWork,int contactCapacity,ActivityTacticalSupportCatalogueDto support)
        {Tactical=tactical;StudyWork=studyWork;ContactCapacity=contactCapacity;metadata=ActivitySupportCopies.Copy(support);}
        public ActivityTacticalSupportCatalogueDto CopySupport()=>ActivitySupportCopies.Copy(metadata);
    }
    public sealed class ActivitySupportResult
    {
        public ActivitySupportedShip? Ship {get;}
        public IReadOnlyList<TacticalSupportIssue> Issues {get;}
        public bool Supported=>Ship!=null&&Issues.Count==0;
        public ActivitySupportResult(ActivitySupportedShip? ship,IReadOnlyList<TacticalSupportIssue> issues)
        {Ship=ship;Issues=new List<TacticalSupportIssue>(issues).AsReadOnly();}
    }
    public static class ActivitySupportCopies
    {
        public static ActivityTacticalSupportCatalogueDto Copy(ActivityTacticalSupportCatalogueDto s)=>new ActivityTacticalSupportCatalogueDto{
            FormatVersion=s.FormatVersion,SchemaId=s.SchemaId,CompilerId=s.CompilerId,CatalogueId=s.CatalogueId,Revision=s.Revision,
            Modules=s.Modules!.Select(m=>new ActivityModuleSupportDto{ComponentVersionId=m.ComponentVersionId,Kind=m.Kind,SurveyPower=m.SurveyPower,StudyWork=m.StudyWork,ContactCapacity=m.ContactCapacity}).ToList(),
            Tactical=new TacticalSupportCatalogueDto{FormatVersion=s.Tactical!.FormatVersion,SchemaId=s.Tactical.SchemaId,CatalogueId=s.Tactical.CatalogueId,Revision=s.Tactical.Revision,
                SourceCompilerId=s.Tactical.SourceCompilerId,BalanceStatus=s.Tactical.BalanceStatus,CloakPolicyId=s.Tactical.CloakPolicyId,
                Hulls=s.Tactical.Hulls!.Select(h=>new TacticalHullSupportDto{HullVersionId=h.HullVersionId,SupportedRoleIds=h.SupportedRoleIds!.ToList(),Armor=h.Armor,TacticalRole=h.TacticalRole,
                    Capabilities=new AuthoredTacticalCapabilitiesDto{Speed=h.Capabilities!.Speed,Agility=h.Capabilities.Agility,Sensors=h.Capabilities.Sensors,Command=h.Capabilities.Command,Cohesion=h.Capabilities.Cohesion},
                    Mounts=h.Mounts!.Select(m=>new TacticalMountSupportDto{SourceMountId=m.SourceMountId,SourceCoverageTag=m.SourceCoverageTag,Coverage=m.Coverage,Slots=m.Slots}).ToList()}).ToList(),
                Components=s.Tactical.Components!.Select(c=>new TacticalComponentSupportDto{ComponentVersionId=c.ComponentVersionId,Kind=c.Kind,TacticalSensorRating=c.TacticalSensorRating,
                    Beam=c.Beam==null?null:new TacticalBeamSupportDto{SourceMountId=c.Beam.SourceMountId,SlotsRequired=c.Beam.SlotsRequired,Coverage=c.Beam.Coverage,Damage=c.Beam.Damage,
                        AccuracyBps=c.Beam.AccuracyBps,TrackingBps=c.Beam.TrackingBps,OpportunitiesPerRound=c.Beam.OpportunitiesPerRound,CadenceRounds=c.Beam.CadenceRounds,InitialNextFireRound=c.Beam.InitialNextFireRound}}).ToList()}};
    }
}
