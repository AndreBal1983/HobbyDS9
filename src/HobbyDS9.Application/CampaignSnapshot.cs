using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Application
{
    public sealed class CampaignSnapshot
    {
        public SessionSnapshot Campaign { get; }
        public ScenarioContentSnapshot Content { get; }
        public NavigationMapSnapshot Navigation { get; }
        // Current planning ledger only, not an action history. AI IDs are reserved.
        public IReadOnlyList<EntityId> AcceptedPlayerCommandIds { get; }
        internal CampaignSnapshot(SessionSnapshot campaign, ScenarioContentSnapshot content, NavigationMapSnapshot navigation)
        {
            Campaign = campaign; Content = content; Navigation = navigation;
            AcceptedPlayerCommandIds = campaign.AcceptedCommandIds
                .Where(id => !id.Value.StartsWith("ai-command.", StringComparison.Ordinal)).ToList().AsReadOnly();
        }
    }

    public sealed class NamedDefinitionSnapshot
    {
        public DefinitionId DefinitionId { get; }
        public string DisplayName { get; }
        internal NamedDefinitionSnapshot(string id, string name) { DefinitionId = new DefinitionId(id); DisplayName = name; }
    }

    public sealed class ResearchDefinitionSnapshot
    {
        public DefinitionId DefinitionId { get; }
        public string DisplayName { get; }
        public long ResearchCost { get; }
        public IReadOnlyList<DefinitionId> Prerequisites { get; }
        internal ResearchDefinitionSnapshot(ResearchNodeDefinitionDto dto)
        {
            DefinitionId = new DefinitionId(dto.DefinitionId); DisplayName = dto.DisplayName; ResearchCost = dto.ResearchCost;
            Prerequisites = dto.PrerequisiteDefinitionIds!.OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => new DefinitionId(id)).ToList().AsReadOnly();
        }
    }

    // Content labels are separate from rules and copied once at session creation.
    // Ship design costs/profiles are already available in Campaign.ShipDesigns.
    public sealed class ScenarioContentSnapshot
    {
        public IReadOnlyList<NamedDefinitionSnapshot> Civilizations { get; }
        public IReadOnlyList<NamedDefinitionSnapshot> StarSystems { get; }
        public IReadOnlyList<GovernancePolicyDefinitionSnapshot> GovernancePolicies { get; }
        public IReadOnlyList<PoliticalMemoryEffectSnapshot> PoliticalMemoryEffects { get; }
        public IReadOnlyList<SpeciesDefinitionSnapshot> Species { get; }
        public IReadOnlyList<ResourceDefinitionSnapshot> Resources { get; }
        public IReadOnlyList<InfrastructureDefinitionSnapshot> Infrastructure { get; }
        public IReadOnlyList<ResearchDefinitionSnapshot> Research { get; }
        internal ScenarioContentSnapshot(DefinitionCatalogDto definitions)
        {
            GovernancePolicies = definitions.GovernancePolicies!.OrderBy(p=>p.DefinitionId,StringComparer.Ordinal).Select(p=>new GovernancePolicyDefinitionSnapshot(p)).ToList().AsReadOnly();
            PoliticalMemoryEffects = definitions.PoliticalMemoryEffects!.OrderBy(p=>p.DefinitionId,StringComparer.Ordinal).Select(p=>new PoliticalMemoryEffectSnapshot(p)).ToList().AsReadOnly();
            Species = definitions.Species!.OrderBy(s => s.DefinitionId, StringComparer.Ordinal).Select(s => new SpeciesDefinitionSnapshot(s)).ToList().AsReadOnly();
            Resources = definitions.Resources!.OrderBy(r => r.DefinitionId, StringComparer.Ordinal).Select(r => new ResourceDefinitionSnapshot(r)).ToList().AsReadOnly();
            Infrastructure = definitions.Infrastructure!.OrderBy(i => i.DefinitionId, StringComparer.Ordinal).Select(i => new InfrastructureDefinitionSnapshot(i)).ToList().AsReadOnly();
            Civilizations = definitions.Civilizations!.OrderBy(d => d.DefinitionId, StringComparer.Ordinal)
                .Select(d => new NamedDefinitionSnapshot(d.DefinitionId, d.DisplayName)).ToList().AsReadOnly();
            StarSystems = definitions.StarSystems!.OrderBy(d => d.DefinitionId, StringComparer.Ordinal)
                .Select(d => new NamedDefinitionSnapshot(d.DefinitionId, d.DisplayName)).ToList().AsReadOnly();
            Research = definitions.ResearchNodes!.OrderBy(d => d.DefinitionId, StringComparer.Ordinal)
                .Select(d => new ResearchDefinitionSnapshot(d)).ToList().AsReadOnly();
        }
    }
}
