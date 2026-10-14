using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public interface ICampaignIntegratedPlaytestSession : ICampaignTacticalTravelSession
    {
        CampaignIntegratedStrategySnapshot CaptureStrategy(string actor);
        CampaignIntegratedPlaytestSaveDto ExportIntegratedSave();
        CampaignIntegratedDesignLibrarySnapshot CaptureDesignLibrary(string actor);
    }
    public sealed class CampaignIntegratedConstructionOption
    {
        public EntityId ColonyId { get; }
        public ShipDesignSnapshot Design { get; }
        public string FitId { get; }
        public int FitRevision { get; }
        public string ProvenanceId { get; }
        public CommandErrorCode UnavailableReason { get; }
        public bool CanEnqueue => UnavailableReason == CommandErrorCode.None;
        internal CampaignIntegratedConstructionOption(EntityId colony, ShipDesignSnapshot design, AuthoredTacticalFitDto fit, CommandErrorCode reason)
        { ColonyId=colony;Design=design;FitId=fit.DefinitionId!;FitRevision=fit.Revision!.Value;ProvenanceId=fit.ProvenanceId!;UnavailableReason=reason; }
    }
    public sealed class CampaignIntegratedStrategySnapshot
    {
        public CampaignTacticalReviewStatus Status { get; }
        public CivilizationSnapshot Civilization { get; }
        public ResearchOverviewSnapshot Research { get; }
        public IReadOnlyList<ColonySnapshot> Colonies { get; }
        public IReadOnlyList<ShipSnapshot> Ships { get; }
        public IReadOnlyList<FleetSnapshot> Fleets { get; }
        public NavigationMapSnapshot Navigation { get; }
        public IReadOnlyList<CampaignIntegratedConstructionOption> Construction { get; }
        public CampaignIntegratedReferenceSnapshot References { get; }
        internal CampaignIntegratedStrategySnapshot(CampaignSnapshot privateFacts,CampaignTacticalReviewStatus status, EntityId actor,
            IEnumerable<CampaignIntegratedConstructionOption> construction)
        {
            Status=status;Civilization=privateFacts.Campaign.Civilizations.Single(c=>c.CivilizationId==actor);
            Research=ResearchOverviewSnapshot.For(privateFacts,actor,status.Phase==SessionPhase.Planning);
            Colonies=privateFacts.Campaign.Colonies.Where(c=>c.OwnerCivilizationId==actor).ToList().AsReadOnly();
            Ships=privateFacts.Campaign.Ships.Where(s=>s.OwnerCivilizationId==actor).ToList().AsReadOnly();
            Fleets=privateFacts.Campaign.Fleets.Where(f=>f.OwnerCivilizationId==actor).ToList().AsReadOnly();
            Navigation=privateFacts.Navigation;Construction=construction.ToList().AsReadOnly();
            References=new CampaignIntegratedReferenceSnapshot(privateFacts,actor);
        }
    }
    public sealed class CampaignIntegratedPlaytestApplication : ICampaignIntegratedPlaytestSession
    {
        private readonly CampaignTacticalReviewSession session;
        private CampaignIntegratedPlaytestApplication(CampaignTacticalReviewSession session) { this.session=session; }
        public static ICampaignIntegratedPlaytestSession StartPackage(CampaignIntegratedPlaytestPackageDto p)
            => new CampaignIntegratedPlaytestApplication(CampaignTacticalReviewSession.StartIntegrated(p));
        public static ICampaignIntegratedPlaytestSession LoadSave(CampaignIntegratedPlaytestPackageDto p,CampaignIntegratedPlaytestSaveDto s)
            => new CampaignIntegratedPlaytestApplication(CampaignTacticalReviewSession.LoadIntegrated(p,s));
        public CampaignIntegratedPlaytestSaveDto ExportIntegratedSave()=>session.ExportIntegratedSave();
        public CampaignIntegratedDesignLibrarySnapshot CaptureDesignLibrary(string actor)
        {var facts=session.CaptureIntegratedLibrary(actor);return new CampaignIntegratedDesignLibrarySnapshot(actor,facts.Status,facts.Catalogue,facts.Published);}
        // Inherited legacy-shaped export is not a complete integrated checkpoint. Fail closed.
        public CampaignTacticalTravelSaveDto ExportSave()=>throw new InvalidOperationException("Use ExportIntegratedSave: bare physical format2 omits construction provenance.");
        public CampaignIntegratedStrategySnapshot CaptureStrategy(string actor)
        {
            var facts=session.CaptureIntegratedProjection(actor);
            var view=facts.Navigation;
            var navigation=new NavigationMapSnapshot(view.Nodes.Select(n=>new NavigationNodeSnapshot(new NavigationNodeId(n.NodeId),
                n.AssociatedSystemId==null?(EntityId?)null:new EntityId(n.AssociatedSystemId),n.DisplayName,n.PhenomenonDefinitionIds)),
                view.Connections.Select(e=>new NavigationLinkSnapshot(new NavigationNodeId(e.FromNodeId),new NavigationNodeId(e.ToNodeId),e.Kind,e.EffectiveCost,e.BaseCost,e.PhenomenonCost,e.ArtificialCost)),
                view.Phenomena.Where(p=>view.Nodes.Any(n=>n.PhenomenonDefinitionIds.Contains(p.DefinitionId))));
            var projection=new CampaignSnapshot(facts.Campaign,new ScenarioContentSnapshot(facts.Definitions),navigation,Array.Empty<FleetTravelSnapshot>(),null);
            var id=new EntityId(actor);var options=new List<CampaignIntegratedConstructionOption>();
            var templates=session.CaptureConstructionTemplates();
            foreach(var colony in facts.Campaign.Colonies.Where(c=>c.OwnerCivilizationId==id))
            foreach(var option in StrategicOptions.Production(projection,id,colony.ColonyId).Where(o=>templates.Any(t=>t.ShipDesignDefinitionId==o.Design.DefinitionId.Value)))
            {
                var template=templates.Single(t=>t.ShipDesignDefinitionId==option.Design.DefinitionId.Value);
                var source=facts.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Single(p=>p.ShipId==template.SourceProvisionShipId);
                var fit=facts.Definitions.AuthoredTacticalEquipment.Fits!.Single(f=>f.DefinitionId==source.FitDefinitionId&&f.Revision==source.FitRevision);
                options.Add(new CampaignIntegratedConstructionOption(colony.ColonyId,option.Design,fit,
                    facts.Status.Phase==SessionPhase.Planning?option.UnavailableReason:CommandErrorCode.CombatPending));
            }
            return new CampaignIntegratedStrategySnapshot(projection,facts.Status,id,options);
        }
        public CampaignTacticalReviewStatus CaptureStatus()=>session.CaptureStatus();
        public IReadOnlyList<FleetSnapshot> GetOwnStrategicFleets(string actor)=>session.OwnStrategicFleets(actor);
        public IReadOnlyList<CampaignTacticalFleetCloakSetting> GetFleetCloakSettings(string actor)=>session.FleetCloakSettings(actor);
        public IReadOnlyList<CampaignTacticalOwnShip> InspectOwnShips(string actor)=>session.InspectOwnShips(actor);
        public IReadOnlyList<string> GetEncounters(string actor)=>session.Encounters(actor);
        public CampaignTacticalObservation Observe(string encounter,string actor)=>session.Observe(encounter,actor);
        public IReadOnlyList<CampaignKnownContact> GetKnownContacts(string actor)=>session.KnownContacts(actor);
        public IReadOnlyList<CampaignOwnTravelReceipt> GetOwnTravelReceipts(string actor)=>session.OwnTravelReceipts(actor);
        public IReadOnlyList<CampaignOwnTravelOrder> GetOwnTravelOrders(string actor)=>session.OwnTravelOrders(actor);
        public StrategicRouteSnapshot FindKnownRoute(string actor,string origin,string destination)=>session.FindKnownTravelRoute(actor,origin,destination);
        public CampaignTacticalMutationResult SubmitTravelOrder(SubmitCampaignTravelOrderDto? command)=>session.SubmitTravelOrder(command);
        public CampaignTacticalMutationResult SetApproachCloak(string actor,string command,string fleet,bool enabled,int turn,long revision)=>session.SetApproachCloak(actor,command,fleet,enabled,turn,revision);
        public CampaignTacticalMutationResult SubmitStrategicCommand(CommandEnvelope? command)=>session.SubmitStrategicCommand(command);
        public CampaignTacticalMutationResult AdvanceTurn(int turn,long revision)=>session.AdvanceTurn(turn,revision);
        public CampaignTacticalMutationResult CommitRound(CommitCampaignTacticalRoundDto? command)=>session.CommitRound(command);
        public CampaignTacticalMutationResult FinalizeBatch(int turn,long revision)=>session.FinalizeBatch(turn,revision);
    }
}
