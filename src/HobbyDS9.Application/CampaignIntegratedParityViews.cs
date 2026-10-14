using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Application
{
    // Immutable reference definitions for owned view facts, not another campaign or private root.
    public sealed class CampaignIntegratedReferenceSnapshot
    {
        public IReadOnlyList<SpeciesDefinitionSnapshot> Species { get; }
        public IReadOnlyList<InfrastructureDefinitionSnapshot> Infrastructure { get; }
        public IReadOnlyList<ResourceDefinitionSnapshot> Resources { get; }
        public IReadOnlyList<GovernancePolicyDefinitionSnapshot> GovernancePolicies { get; }
        public IReadOnlyList<PoliticalMemoryEffectSnapshot> PoliticalMemoryEffects { get; }
        internal CampaignIntegratedReferenceSnapshot(CampaignSnapshot facts,EntityId actor)
        {
            var worlds=facts.Campaign.Colonies.Where(c=>c.OwnerCivilizationId==actor).ToList();
            var species=worlds.SelectMany(c=>c.State.Cohorts).Select(c=>c.SpeciesDefinitionId).ToHashSet();
            var infrastructure=worlds.SelectMany(c=>c.State.Infrastructure).Select(i=>i.DefinitionId).ToHashSet();
            var resources=worlds.SelectMany(c=>c.State.Stockpiles.Concat(c.State.DesiredStockpiles)).Select(r=>r.ResourceDefinitionId)
                .Concat(facts.Campaign.ShipDesigns.SelectMany(d=>d.ResourceCosts).Select(r=>r.ResourceDefinitionId)).ToHashSet();
            Species=facts.Content.Species.Where(s=>species.Contains(s.DefinitionId)).ToList().AsReadOnly();
            Infrastructure=facts.Content.Infrastructure.Where(i=>infrastructure.Contains(i.DefinitionId)).ToList().AsReadOnly();
            Resources=facts.Content.Resources.Where(r=>resources.Contains(r.DefinitionId)).ToList().AsReadOnly();
            // Public content rules, never opponent political state or intentions.
            GovernancePolicies=facts.Content.GovernancePolicies;PoliticalMemoryEffects=facts.Content.PoliticalMemoryEffects;
        }
    }
    public sealed class CampaignIntegratedDesignLibrarySnapshot
    {
        public string Actor { get; }
        public CampaignTacticalReviewStatus Status { get; }
        public CampaignShipDesignCatalogueSnapshot? Catalogue { get; }
        public IReadOnlyList<CampaignBlueprintSnapshot> Published { get; }
        public bool CanPublish=>Catalogue!=null&&Status.Phase==SessionPhase.Planning;
        public bool CanConstructPublishedBlueprints=>false;
        internal CampaignIntegratedDesignLibrarySnapshot(string actor,CampaignTacticalReviewStatus status,
            CampaignShipDesignCatalogueSnapshot? catalogue,IReadOnlyList<CampaignBlueprintSnapshot> published)
        {Actor=actor;Status=status;Catalogue=catalogue;Published=published.ToList().AsReadOnly();}
    }
}
