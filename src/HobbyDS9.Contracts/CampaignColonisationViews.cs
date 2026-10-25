using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public sealed class ColonisationCohortSnapshot
    {
        public string Id {get;}public string SpeciesId {get;}public long Population {get;}
        public ColonisationCohortSnapshot(string id,string species,long population){Id=id;SpeciesId=species;Population=population;}
    }
    public sealed class ColonisationWorldSnapshot
    {
        public string Id {get;}public string PlanetId {get;}public string SystemId {get;}public string Name {get;}
        public ColonyEstablishmentStage Stage {get;}public string? CampId {get;}public long Population {get;}public long Food {get;}
        public long Industry {get;}public long Research {get;}public long FoodOutput {get;}
        public long EstablishmentWork {get;}public int FedWeeks {get;}public int ShortageWeeks {get;}public long Deaths {get;}public string Report {get;}
        public IReadOnlyList<ResourceAmountSnapshot> Stocks {get;}public IReadOnlyList<ColonisationCohortSnapshot> Cohorts {get;}
        public ColonisationWorldSnapshot(string id,string planet,string system,string name,ColonyEstablishmentStage stage,string? camp,long people,long food,
            long industry,long research,long output,long work,int fed,int shortage,long deaths,string report,IEnumerable<ResourceAmountSnapshot> stocks,IEnumerable<ColonisationCohortSnapshot> cohorts)
        {Id=id;PlanetId=planet;SystemId=system;Name=name;Stage=stage;CampId=camp;Population=people;Food=food;Industry=industry;Research=research;FoodOutput=output;
         EstablishmentWork=work;FedWeeks=fed;ShortageWeeks=shortage;Deaths=deaths;Report=report;Stocks=new List<ResourceAmountSnapshot>(stocks).AsReadOnly();Cohorts=new List<ColonisationCohortSnapshot>(cohorts).AsReadOnly();}
    }
    public sealed class ColonisationTransportSnapshot
    {
        public string ShipId {get;}public string FleetId {get;}public string NodeId {get;}public string Name {get;}public string HullVersionId {get;}
        public string BlueprintId {get;}public int BlueprintRevision {get;}public bool Stationary {get;}public bool Living {get;}
        public int PassengerCapacity {get;}public int FoodCapacity {get;}public int MaterialCapacity {get;}
        public string? ManifestId {get;}public SettlerManifestKind? Kind {get;}public long Population {get;}public long Food {get;}public long Deaths {get;}public int ShortageWeeks {get;}public string Report {get;}
        public IReadOnlyList<ResourceAmountSnapshot> Cargo {get;}
        public ColonisationTransportSnapshot(string ship,string fleet,string node,string name,string hull,string blueprint,int revision,bool stationary,bool living,
            int peopleCapacity,int foodCapacity,int materialCapacity,string? manifest,SettlerManifestKind? kind,long people,long food,long deaths,int shortage,string report,IEnumerable<ResourceAmountSnapshot> cargo)
        {ShipId=ship;FleetId=fleet;NodeId=node;Name=name;HullVersionId=hull;BlueprintId=blueprint;BlueprintRevision=revision;Stationary=stationary;Living=living;PassengerCapacity=peopleCapacity;
         FoodCapacity=foodCapacity;MaterialCapacity=materialCapacity;ManifestId=manifest;Kind=kind;Population=people;Food=food;Deaths=deaths;ShortageWeeks=shortage;Report=report;Cargo=new List<ResourceAmountSnapshot>(cargo).AsReadOnly();}
    }
    public sealed class ColonisationQueueSnapshot
    {
        public string Id {get;}public string ColonyId {get;}public ColonisationProductionKind Kind {get;}public string DefinitionId {get;}public string Name {get;}
        public int BlueprintRevision {get;}public long RequiredIndustry {get;}public long CompletedIndustry {get;}public string Status {get;}
        public IReadOnlyList<ResourceAmountSnapshot> Consumed {get;}public IReadOnlyList<ResourceAmountSnapshot> Required {get;}
        public ColonisationQueueSnapshot(string id,string colony,ColonisationProductionKind kind,string definition,string name,int revision,long required,long done,string status,
            IEnumerable<ResourceAmountSnapshot> consumed,IEnumerable<ResourceAmountSnapshot> materials)
        {Id=id;ColonyId=colony;Kind=kind;DefinitionId=definition;Name=name;BlueprintRevision=revision;RequiredIndustry=required;CompletedIndustry=done;Status=status;
         Consumed=new List<ResourceAmountSnapshot>(consumed).AsReadOnly();Required=new List<ResourceAmountSnapshot>(materials).AsReadOnly();}
    }
    public sealed class ColonisationPresenceSnapshot
    {
        public string SystemId {get;}public int ObservationTurn {get;}public IReadOnlyList<string> KnownActors {get;}
        // Presence never asserts that an unobserved body has no other government.
        public string Caption=>KnownActors.Count==0?"Unknown presence":KnownActors.Count>1?"Known mixed presence":"Known presence";
        public ColonisationPresenceSnapshot(string system,int turn,IEnumerable<string> actors){SystemId=system;ObservationTurn=turn;KnownActors=new List<string>(actors).AsReadOnly();}
    }
    public sealed class ColonisationCampaignSnapshot
    {
        public string Actor {get;}public int Turn {get;}public long Revision {get;}public string PackageHash {get;}
        public IReadOnlyList<ColonisationWorldSnapshot> Worlds {get;}public IReadOnlyList<ColonisationTransportSnapshot> Transports {get;}
        public IReadOnlyList<ColonisationQueueSnapshot> Production {get;}public IReadOnlyList<ColonisationPresenceSnapshot> Presence {get;}
        public IReadOnlyList<ColonisationManifestReport> Reports {get;}public IReadOnlyList<ColonisationSurveySnapshot> Surveys {get;}public IReadOnlyList<ColonisationIntentSnapshot> Intents {get;}
        public long KitIndustry {get;}public IReadOnlyList<ResourceAmountSnapshot> KitResources {get;}
        public ColonisationCampaignSnapshot(string actor,int turn,long revision,string hash,IEnumerable<ColonisationWorldSnapshot> worlds,IEnumerable<ColonisationTransportSnapshot> ships,
            IEnumerable<ColonisationQueueSnapshot> queue,IEnumerable<ColonisationPresenceSnapshot> presence,IEnumerable<ColonisationManifestReport> reports,IEnumerable<ColonisationSurveySnapshot> surveys,
            IEnumerable<ColonisationIntentSnapshot> intents,long kitIndustry,IEnumerable<ResourceAmountSnapshot> kitResources)
        {Actor=actor;Turn=turn;Revision=revision;PackageHash=hash;Worlds=new List<ColonisationWorldSnapshot>(worlds).AsReadOnly();Transports=new List<ColonisationTransportSnapshot>(ships).AsReadOnly();
         Production=new List<ColonisationQueueSnapshot>(queue).AsReadOnly();Presence=new List<ColonisationPresenceSnapshot>(presence).AsReadOnly();Reports=new List<ColonisationManifestReport>(reports).AsReadOnly();
         Surveys=new List<ColonisationSurveySnapshot>(surveys).AsReadOnly();Intents=new List<ColonisationIntentSnapshot>(intents).AsReadOnly();KitIndustry=kitIndustry;KitResources=new List<ResourceAmountSnapshot>(kitResources).AsReadOnly();}
    }
    public sealed class ColonisationManifestReport
    {
        public string ManifestId {get;}public string ShipId {get;}public SettlerManifestKind Kind {get;}public SettlerManifestStage Stage {get;}
        public long PopulationTransferred {get;}public long Deaths {get;}public long FoodConsumed {get;}public long FoodLost {get;}public string Report {get;}
        public ColonisationManifestReport(string id,string ship,SettlerManifestKind kind,SettlerManifestStage stage,long transferred,long deaths,long consumed,long lost,string report)
        {ManifestId=id;ShipId=ship;Kind=kind;Stage=stage;PopulationTransferred=transferred;Deaths=deaths;FoodConsumed=consumed;FoodLost=lost;Report=report;}
    }
    public sealed class ColonisationSurveySnapshot
    {
        public string Id {get;}public string ShipId {get;}public string PlanetId {get;}public PlanetSurveyStage Stage {get;}public int Weeks {get;}
        public ColonisationSurveySnapshot(string id,string ship,string planet,PlanetSurveyStage stage,int weeks){Id=id;ShipId=ship;PlanetId=planet;Stage=stage;Weeks=weeks;}
    }
    public sealed class ColonisationIntentSnapshot
    {
        public string Id {get;}public string ManifestId {get;}public string PlanetId {get;}public bool Disputed {get;}
        public ColonisationIntentSnapshot(string id,string manifest,string planet,bool disputed){Id=id;ManifestId=manifest;PlanetId=planet;Disputed=disputed;}
    }
    public sealed class ColonisationEligibilitySnapshot
    {
        public string Actor {get;}public int Turn {get;}public long Revision {get;}public ColonisationAction Action {get;}public CampaignTacticalError Error {get;}
        public bool Available=>Error==CampaignTacticalError.None;
        public ColonisationEligibilitySnapshot(ColonisationCommandDto command,CampaignTacticalError error){Actor=command.ActorId;Turn=command.Turn;Revision=command.Revision;Action=command.Action;Error=error;}
    }
}
