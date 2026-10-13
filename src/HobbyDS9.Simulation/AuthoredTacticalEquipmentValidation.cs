using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidateAuthoredTacticalEquipmentDefinitions(DefinitionCatalogDto definitions,
            HashSet<string> typed, List<ValidationIssue> issues)
        {
            issues.AddRange(AuthoredTacticalEquipment.Validate(definitions).Issues);
            var catalogue = definitions.AuthoredTacticalEquipment;
            if (catalogue == null) return;
            foreach (var id in (catalogue.Fits?.Where(f => f != null).Select(f => f.DefinitionId) ?? Array.Empty<string>())
                .Concat(catalogue.AmmunitionDefinitions?.Where(a => a != null).Select(a => a.DefinitionId) ?? Array.Empty<string>()))
                if (id != null && !typed.Add(id)) Add(issues, ValidationCode.DuplicateId, "$.Definitions.AuthoredTacticalEquipment");
        }

        private static void ValidateAuthoredTacticalEquipmentBindings(GameStateDto? state, DefinitionCatalogDto? definitions,
            bool initial, List<ValidationIssue> issues)
        {
            var catalogue = definitions?.AuthoredTacticalEquipment;
            if (state == null || catalogue == null) return;
            foreach (var provision in catalogue.InitialShipProvisions ?? new List<AuthoredTacticalShipProvisionDto>())
            {
                if (provision == null) continue;
                var path = "$.Definitions.AuthoredTacticalEquipment.InitialShipProvisions";
                var ship = state.Ships?.FirstOrDefault(s => s != null && s.ShipId == provision.ShipId);
                // Destroyed authored ships can disappear; no stocks are recreated for new ships.
                if (ship == null)
                { if (initial) Add(issues, ValidationCode.MissingEntity, path + ".ShipId"); continue; }
                var fit = catalogue.Fits?.FirstOrDefault(f => f != null && f.DefinitionId == provision.FitDefinitionId);
                var design = definitions?.ShipDesigns?.FirstOrDefault(d => d != null && d.DefinitionId == provision.ShipDesignDefinitionId);
                if (ship.Blueprint != null || ship.OwnerCivilizationId != provision.OwnerCivilizationId
                    || ship.ShipDesignDefinitionId != provision.ShipDesignDefinitionId || fit == null || design == null
                    || design.MaxHull != fit.MaxHull || ship.CurrentHull > fit.MaxHull)
                    Add(issues, ValidationCode.InvalidTacticalEquipmentBinding, path);
            }
        }
    }
}
