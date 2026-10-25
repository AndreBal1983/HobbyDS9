using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public sealed class ColonisationPlanetSnapshot
    {
        public string PlanetId {get;}public string BodyId {get;}public string SystemId {get;}public string DisplayName {get;}public bool Surveyed {get;}
        public string? PlanetClass {get;}public string? Atmosphere {get;}public int Gravity {get;}public int Pressure {get;}public int Radiation {get;}
        public IReadOnlyList<ResourceAmountSnapshot> InitialDeposits {get;}public PlanetControllerKind? KnownControl {get;}public string? KnownActor {get;}public int? ControlObservedTurn {get;}
        public string? ActiveSurveyId {get;}public int CompletedSurveyWeeks {get;}
        public ColonisationPlanetSnapshot(ColonisationPlanetObservationDto value)
        {PlanetId=value.PlanetId;BodyId=value.BodyId;SystemId=value.SystemId;DisplayName=value.DisplayName;Surveyed=value.Surveyed;
         PlanetClass=value.Environment?.PlanetClass;Atmosphere=value.Environment?.Atmosphere;Gravity=value.Environment?.Gravity??0;Pressure=value.Environment?.Pressure??0;Radiation=value.Environment?.Radiation??0;
         InitialDeposits=ResourceAmountSnapshot.Copy(value.Environment?.Deposits??new List<ResourceAmountDto>());KnownControl=value.KnownControl?.Kind;KnownActor=value.KnownControl?.ActorId;ControlObservedTurn=value.ControlObservedTurn;
         ActiveSurveyId=value.ActiveSurveyId;CompletedSurveyWeeks=value.CompletedSurveyWeeks;}
    }
    public sealed class ColonisationCohortSnapshot
    {
        public string Id {get;}public string SpeciesId {get;}public string SpeciesName {get;}public long Population {get;}
        public ColonisationCohortSnapshot(string id,string species,long population,string? name=null){Id=id;SpeciesId=species;SpeciesName=name??species;Population=population;}
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
    public sealed class ColonisationOwnShipSourceSnapshot
    {
        public string ShipId {get;}public string BlueprintId {get;}public int Revision {get;}
        public ColonisationOwnShipSourceSnapshot(string ship,string blueprint,int revision){ShipId=ship;BlueprintId=blueprint;Revision=revision;}
    }
    public sealed class ColonisationCampaignSnapshot
    {
        public string Actor {get;}public int Turn {get;}public long Revision {get;}public string PackageHash {get;}
        public IReadOnlyList<ColonisationWorldSnapshot> Worlds {get;}public IReadOnlyList<ColonisationTransportSnapshot> Transports {get;}
        public IReadOnlyList<ColonisationOwnShipSourceSnapshot> OwnShipSources {get;}
        public IReadOnlyList<ColonisationQueueSnapshot> Production {get;}public IReadOnlyList<ColonisationPresenceSnapshot> Presence {get;}
        public IReadOnlyList<ColonisationManifestReport> Reports {get;}public IReadOnlyList<ColonisationSurveySnapshot> Surveys {get;}public IReadOnlyList<ColonisationIntentSnapshot> Intents {get;}
        public long KitIndustry {get;}public IReadOnlyList<ResourceAmountSnapshot> KitResources {get;}
        public ColonisationCampaignSnapshot(string actor,int turn,long revision,string hash,IEnumerable<ColonisationWorldSnapshot> worlds,IEnumerable<ColonisationTransportSnapshot> ships,
            IEnumerable<ColonisationQueueSnapshot> queue,IEnumerable<ColonisationPresenceSnapshot> presence,IEnumerable<ColonisationManifestReport> reports,IEnumerable<ColonisationSurveySnapshot> surveys,
            IEnumerable<ColonisationIntentSnapshot> intents,long kitIndustry,IEnumerable<ResourceAmountSnapshot> kitResources,IEnumerable<ColonisationOwnShipSourceSnapshot>? ownShipSources=null)
        {Actor=actor;Turn=turn;Revision=revision;PackageHash=hash;Worlds=new List<ColonisationWorldSnapshot>(worlds).AsReadOnly();Transports=new List<ColonisationTransportSnapshot>(ships).AsReadOnly();
         Production=new List<ColonisationQueueSnapshot>(queue).AsReadOnly();Presence=new List<ColonisationPresenceSnapshot>(presence).AsReadOnly();Reports=new List<ColonisationManifestReport>(reports).AsReadOnly();
         Surveys=new List<ColonisationSurveySnapshot>(surveys).AsReadOnly();Intents=new List<ColonisationIntentSnapshot>(intents).AsReadOnly();OwnShipSources=new List<ColonisationOwnShipSourceSnapshot>(ownShipSources??System.Array.Empty<ColonisationOwnShipSourceSnapshot>()).AsReadOnly();KitIndustry=kitIndustry;KitResources=new List<ResourceAmountSnapshot>(kitResources).AsReadOnly();}
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
        public string Reason=>Describe(Error);
        public static string Describe(CampaignTacticalError error)
        {
            switch(error)
            {
                case CampaignTacticalError.None:return "Available at this observed campaign state.";
                case CampaignTacticalError.StaleTurn:case CampaignTacticalError.StaleRevision:case CampaignTacticalError.StaleBoundary:return "The campaign changed. Refresh before reviewing this order again.";
                case CampaignTacticalError.WrongPhase:return "Resolve the pending combat before issuing this order.";
                case CampaignTacticalError.Overflow:return "The campaign has reached a technical recording or arithmetic limit. This order was not applied; the last valid campaign is unchanged.";
                case CampaignTacticalError.UnsupportedSource:return "This ship or construction option is outside the supported playtest content.";
                case CampaignTacticalError.DuplicateCommand:return "This exact order was already submitted.";
                default:return "This order is unavailable here. Check current position, survey, ownership, local supplies, capacity and receiving-world stage.";
            }
        }
        public ColonisationEligibilitySnapshot(ColonisationCommandDto command,CampaignTacticalError error){Actor=command.ActorId;Turn=command.Turn;Revision=command.Revision;Action=command.Action;Error=error;}
    }
}
