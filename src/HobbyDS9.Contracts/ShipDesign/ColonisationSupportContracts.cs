using System.Collections.Generic;

namespace HobbyDS9.Contracts.ShipDesign
{
    public static class ColonisationSupportIdentity
    {
        public const int Format=4;
        public const string Schema="published-colonisation-support-v4",Compiler="published-colonisation-compiler-v4";
    }
    public sealed class ColonisationSupportCatalogueDto
    {
        public int FormatVersion {get;set;}
        public string SchemaId {get;set;}="";
        public string CompilerId {get;set;}="";
        public string CatalogueId {get;set;}="";
        public int Revision {get;set;}
        public ActivityTacticalSupportCatalogueDto? Activity {get;set;}
        public List<SettlerModuleSupportDto>? SettlerModules {get;set;}
    }
    public sealed class SettlerModuleSupportDto
    {
        public string ComponentVersionId {get;set;}="";
        public long RequiredPower {get;set;}
        public int Passengers {get;set;}
        public int Food {get;set;}
        public int Equipment {get;set;}
        public int Materials {get;set;}
    }
    // Advisory immutable projection. Admission rederives from the approved frozen full package.
    public sealed class ColonisationSupportedShip
    {
        private readonly ColonisationSupportCatalogueDto metadata;
        public SupportedPublishedTacticalFit Tactical {get;}
        public string CompilerId=>ColonisationSupportIdentity.Compiler;
        public int StudyWork {get;}
        public int ContactCapacity {get;}
        public int Passengers {get;}
        public int Food {get;}
        public int Equipment {get;}
        public int Materials {get;}
        public long PassengerSupportPower {get;}
        public ColonisationSupportedShip(SupportedPublishedTacticalFit tactical,int work,int contact,SettlerModuleSupportDto? installed,ColonisationSupportCatalogueDto support)
        {Tactical=tactical;StudyWork=work;ContactCapacity=contact;Passengers=installed?.Passengers??0;Food=installed?.Food??0;Equipment=installed?.Equipment??0;Materials=installed?.Materials??0;PassengerSupportPower=installed?.RequiredPower??0;metadata=ColonisationSupportCopies.Copy(support);}
        public ColonisationSupportCatalogueDto CopySupport()=>ColonisationSupportCopies.Copy(metadata);
    }
    public sealed class ColonisationSupportResult
    {
        public ColonisationSupportedShip? Ship {get;}
        public IReadOnlyList<TacticalSupportIssue> Issues {get;}
        public bool Supported=>Ship!=null&&Issues.Count==0;
        public ColonisationSupportResult(ColonisationSupportedShip? ship,IReadOnlyList<TacticalSupportIssue> issues)
        {Ship=ship;Issues=new List<TacticalSupportIssue>(issues).AsReadOnly();}
    }
    public static class ColonisationSupportCopies
    {
        public static ColonisationSupportCatalogueDto Copy(ColonisationSupportCatalogueDto s)
        {
            var modules=new List<SettlerModuleSupportDto>();foreach(var m in s.SettlerModules!)modules.Add(new SettlerModuleSupportDto{ComponentVersionId=m.ComponentVersionId,RequiredPower=m.RequiredPower,Passengers=m.Passengers,Food=m.Food,Equipment=m.Equipment,Materials=m.Materials});
            return new ColonisationSupportCatalogueDto{FormatVersion=s.FormatVersion,SchemaId=s.SchemaId,CompilerId=s.CompilerId,CatalogueId=s.CatalogueId,Revision=s.Revision,Activity=ActivitySupportCopies.Copy(s.Activity!),SettlerModules=modules};
        }
    }
}
