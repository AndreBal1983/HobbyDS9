using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    public sealed partial class SimulationSession
    {
        private CampaignBlueprintSnapshot ProjectBlueprint(BlueprintRevisionDto record) => new CampaignBlueprintSnapshot(
            record.OwnerCivilizationId, CampaignBlueprints.Restore(record), record.RulesVersion, record.SourceContentHash,
            !state.Blueprints!.Any(b => b.BlueprintId == record.BlueprintId && b.Revision > record.Revision));

        public IReadOnlyList<CampaignBlueprintSnapshot> CaptureBlueprints()
        {
            lock (gate) return state.Blueprints!.OrderBy(b => b.BlueprintId, StringComparer.Ordinal).ThenBy(b => b.Revision)
                .Select(ProjectBlueprint).ToList().AsReadOnly();
        }

        public IReadOnlyList<BlueprintConstructionSnapshot> CaptureBlueprintConstruction(string actor, string colony)
        {
            lock (gate) return state.Blueprints!.Where(b => b.OwnerCivilizationId == actor).OrderBy(b => b.BlueprintId, StringComparer.Ordinal)
                .ThenBy(b => b.Revision).Select(b => new BlueprintConstructionSnapshot(ProjectBlueprint(b),
                    CampaignBlueprints.Eligibility(state, definitions, actor, colony, b))).ToList().AsReadOnly();
        }

        public CampaignShipDesignCatalogueSnapshot? CaptureShipDesignCatalogue(string actor)
        {
            lock (gate)
            {
                var owner = state.Civilizations!.SingleOrDefault(c => c.CivilizationId == actor);
                if (owner == null || definitions.ShipDesignCatalogue == null) return null;
                return new CampaignShipDesignCatalogueSnapshot(new ShipDesignCatalogueSnapshot(definitions.ShipDesignCatalogue),
                    CampaignBlueprints.Access(definitions, owner).AvailableVersionIds!);
            }
        }
    }
}
