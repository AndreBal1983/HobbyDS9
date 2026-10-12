using System.Collections.Generic;
using System;
using System.Linq;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Contracts
{
    public sealed class CampaignShipDesignCatalogueSnapshot
    {
        public ShipDesignCatalogueSnapshot Catalogue { get; }
        public IReadOnlyList<string> AvailableVersionIds { get; }
        public CampaignShipDesignCatalogueSnapshot(ShipDesignCatalogueSnapshot catalogue, IEnumerable<string> available)
        { Catalogue = catalogue; AvailableVersionIds = available.OrderBy(id => id, StringComparer.Ordinal).ToList().AsReadOnly(); }
    }
    // Content-owned deployment gates. No client whitelist enters a campaign command.
    public sealed class ShipDesignVersionAccessDto
    {
        public string VersionId { get; set; } = string.Empty;
        public List<string>? RequiredEngineeringApplicationDefinitionIds { get; set; }
    }

    public sealed class BlueprintReferenceDto
    {
        public string BlueprintId { get; set; } = string.Empty;
        public int Revision { get; set; }
    }

    // Serialized frozen publication. Load validates it with the pinned compiler;
    // runtime authority never accepts this record as a player-command payload.
    public sealed class BlueprintRevisionDto
    {
        public string OwnerCivilizationId { get; set; } = string.Empty;
        public string BlueprintId { get; set; } = string.Empty;
        public int Revision { get; set; }
        public string CompilerVersion { get; set; } = string.Empty;
        public string RulesVersion { get; set; } = string.Empty;
        public string SourceContentHash { get; set; } = string.Empty;
        public ShipDesignCatalogueDto? FrozenCatalogue { get; set; }
        public ShipClassDraftDto? Fit { get; set; }
        public ShipDesignDefinitionDto? CompiledDesign { get; set; }
        public ShipDesignCostDto? Cost { get; set; }
    }

    public sealed class PublishShipBlueprintPayload
    {
        public string BlueprintId { get; set; } = string.Empty;
        public int Revision { get; set; }
        public ShipClassDraftDto? Draft { get; set; }
    }

    public sealed class EnqueueBlueprintConstructionPayload
    {
        public string ColonyId { get; set; } = string.Empty;
        public string BlueprintId { get; set; } = string.Empty;
        public int Revision { get; set; }
    }

    public sealed class BlueprintReferenceSnapshot
    {
        public string BlueprintId { get; }
        public int Revision { get; }
        public BlueprintReferenceSnapshot(BlueprintReferenceDto reference)
        { BlueprintId = reference.BlueprintId; Revision = reference.Revision; }
    }

    public sealed class CampaignBlueprintSnapshot
    {
        public EntityId OwnerCivilizationId { get; }
        public PublishedShipBlueprint Blueprint { get; }
        public string RulesVersion { get; }
        public string SourceContentHash { get; }
        public bool IsLatestRevision { get; }
        public CampaignBlueprintSnapshot(string owner, PublishedShipBlueprint blueprint, string rules, string hash, bool latest)
        { OwnerCivilizationId = new EntityId(owner); Blueprint = blueprint; RulesVersion = rules; SourceContentHash = hash; IsLatestRevision = latest; }
    }

    public sealed class BlueprintConstructionSnapshot
    {
        public CampaignBlueprintSnapshot Blueprint { get; }
        public CommandErrorCode ErrorCode { get; }
        public bool CanBuild => ErrorCode == CommandErrorCode.None;
        public BlueprintConstructionSnapshot(CampaignBlueprintSnapshot blueprint, CommandErrorCode error)
        { Blueprint = blueprint; ErrorCode = error; }
    }
}
