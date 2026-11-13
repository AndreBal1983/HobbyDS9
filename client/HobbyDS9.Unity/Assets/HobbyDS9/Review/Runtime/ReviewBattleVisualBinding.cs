#nullable enable
using System;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    public static class ReviewBattleVisualBinding
    {
        public static string? OwnHullVersion(CampaignPlaybackBoundary boundary,string ship)
        {
            if(!boundary.OwnShips.Any(s=>s.ShipId==ship))return null;
            return boundary.ShipIdentities.SingleOrDefault(s=>s.ShipId==ship)?.OwnHullVersionId;
        }
        public static string? VisibleHullFamily(CampaignPlaybackBoundary boundary,string ship)
        {
            if(!boundary.OwnShips.Concat(boundary.VisibleEnemies).Any(s=>s.ShipId==ship))return null;
            return boundary.ShipIdentities.SingleOrDefault(s=>s.ShipId==ship)?.VisibleHullFamilyId;
        }
        // Representative existing artwork only: this never discloses an engineered foreign hull version.
        public static string? FamilyArtHull(string? family)
        {
            switch(family)
            {
                case "hull-family.constitution-explorer":return "hull.constitution-explorer.v1";
                case "hull-family.galaxy-cruiser":return "hull.galaxy-cruiser.v1";
                case "hull-family.defiant-combat":return "hull.defiant-combat.v1";
                default:return null;
            }
        }
        public static Tuple<string,string>? Identity(CampaignPlaybackBoundary boundary,string ship)
        {
            // Both disclosure and identity must belong to this exact boundary.
            if(!boundary.OwnShips.Concat(boundary.VisibleEnemies).Any(s=>s.ShipId==ship))return null;
            var identity=boundary.ShipIdentities.SingleOrDefault(s=>s.ShipId==ship);
            return identity==null?null:Tuple.Create(identity.CivilizationDefinitionId,identity.ShipDesignDefinitionId);
        }
    }
}
