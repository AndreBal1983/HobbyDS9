#nullable enable
using System;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    public static class ReviewBattleVisualBinding
    {
        public static Tuple<string,string>? Identity(CampaignPlaybackBoundary boundary,string ship)
        {
            // Both disclosure and identity must belong to this exact boundary.
            if(!boundary.OwnShips.Concat(boundary.VisibleEnemies).Any(s=>s.ShipId==ship))return null;
            var identity=boundary.ShipIdentities.SingleOrDefault(s=>s.ShipId==ship);
            return identity==null?null:Tuple.Create(identity.CivilizationDefinitionId,identity.ShipDesignDefinitionId);
        }
    }
}
