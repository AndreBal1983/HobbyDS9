using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    // No private battle/save/commitment/equipment/RNG input can enter this pure policy.
    internal static class DemoAiPolicy
    {
        internal static List<CampaignTacticalFleetOrderDto> Orders(IReadOnlyList<CampaignTacticalCondition> own,IReadOnlyList<CampaignTacticalCondition> visible)
        {
            var focus=visible.OrderBy(s=>s.Hull).ThenBy(s=>s.FleetId,StringComparer.Ordinal).ThenBy(s=>s.ShipId,StringComparer.Ordinal).FirstOrDefault()?.FleetId;
            return own.GroupBy(s=>s.FleetId).OrderBy(f=>f.Key,StringComparer.Ordinal).Select(f=>new CampaignTacticalFleetOrderDto {FleetId=f.Key,
                Maneuver=focus==null||f.All(s=>s.Hull<=s.MaxHull/3)?CampaignFleetManeuver.Screen:CampaignFleetManeuver.Charge,FocusFleetId=focus}).ToList();
        }
    }
}
