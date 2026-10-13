using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class TacticalWeaponOwnConstraints
    {
        // Exactly the existing battery-admission predicate. No target, geometry, RNG or opponent input.
        internal static CampaignWeaponOwnBlockers Blockers(TacticalWeaponV2Dto weapon,int? round)
        {
            var value=CampaignWeaponOwnBlockers.None;
            if(!weapon.Equipped)value|=CampaignWeaponOwnBlockers.NotEquipped;
            if(weapon.AllocatedPower!=weapon.RequiredPower)value|=CampaignWeaponOwnBlockers.PowerIncomplete;
            if(weapon.Damage<=0)value|=CampaignWeaponOwnBlockers.NoDamage;
            if(weapon.Class==TacticalWeaponClass.Torpedo&&weapon.Ammunition<=0)value|=CampaignWeaponOwnBlockers.NoAmmunition;
            if(round.HasValue&&weapon.NextFireRound>round.Value)value|=CampaignWeaponOwnBlockers.CadenceWait;
            return value;
        }
        internal static IReadOnlyDictionary<string,CampaignWeaponAvailability> Project(TacticalShipV2Dto ship,int? round,
            TacticalFormationStatus formation=TacticalFormationStatus.Active,bool complete=false)
            =>ship.Weapons.ToDictionary(w=>w.WeaponId,w=>{
                var blockers=Blockers(w,round);
                if(ship.Hull<=0)blockers|=CampaignWeaponOwnBlockers.ShipDestroyed;
                if(formation!=TacticalFormationStatus.Active)blockers|=CampaignWeaponOwnBlockers.FormationInactive;
                if(complete)blockers|=CampaignWeaponOwnBlockers.EncounterComplete;
                var status=!round.HasValue?CampaignWeaponAvailabilityStatus.OutsideEncounter:
                    blockers!=CampaignWeaponOwnBlockers.None?CampaignWeaponAvailabilityStatus.OwnStateBlocked:CampaignWeaponAvailabilityStatus.TargetDependent;
                return new CampaignWeaponAvailability(status,blockers,round);
            });
    }
}
