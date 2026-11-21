using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal enum CampaignEquipmentSourceKind { AuthoredInitial, StaticBuilt, PublishedBuilt }

    // Private detached equipment facts, not a source-admission capability or registry.
    internal sealed class CampaignEquipmentSource
    {
        private readonly AuthoredTacticalFitDto fit;
        private readonly AuthoredTacticalShipProvisionDto? provision;
        private readonly List<AuthoredTacticalShieldStockDto> shields;
        private readonly List<AuthoredTacticalAmmoStockDto> ammunition;
        internal CampaignEquipmentSourceKind Kind { get; }
        internal string ShipId { get; }
        internal string Owner { get; }
        internal string DisplayName { get; }
        internal string PlaybackDesignId { get; }
        internal bool CloakCapable { get; }
        internal long CloakPower { get; }
        internal AuthoredTacticalFitDto Fit => CopyFit(fit);
        internal List<AuthoredTacticalAmmoStockDto> InitialAmmunition => CopyAmmo(ammunition);
        internal AuthoredTacticalShipProvisionDto AuthoredProvision => provision==null
            ? throw new InvalidOperationException("Published sources are not authored provisions.") : CopyProvision(provision);
        internal CampaignEquipmentSource(CampaignEquipmentSourceKind kind,string ship,string owner,string name,string playback,
            AuthoredTacticalFitDto equipment,IEnumerable<AuthoredTacticalShieldStockDto> initialShields,
            IEnumerable<AuthoredTacticalAmmoStockDto> initialAmmunition,bool cloak,long cloakPower,AuthoredTacticalShipProvisionDto? authored=null)
        {
            Kind=kind;ShipId=ship;Owner=owner;DisplayName=name;PlaybackDesignId=playback;fit=CopyFit(equipment);
            shields=CopyShields(initialShields);ammunition=CopyAmmo(initialAmmunition);CloakCapable=cloak;CloakPower=cloakPower;
            provision=authored==null?null:CopyProvision(authored);
        }
        internal CampaignTacticalPoolDto InitialPool() => new CampaignTacticalPoolDto {
            ShipId=ShipId,FitId=fit.DefinitionId!,FitRevision=fit.Revision!.Value,ProvenanceId=fit.ProvenanceId!,
            Shields=CopyShields(shields),Ammunition=CopyAmmo(ammunition) };
        private static List<AuthoredTacticalShieldStockDto> CopyShields(IEnumerable<AuthoredTacticalShieldStockDto> values)
            =>values.OrderBy(x=>x.Sector).Select(x=>new AuthoredTacticalShieldStockDto{Sector=x.Sector,Current=x.Current}).ToList();
        private static List<AuthoredTacticalAmmoStockDto> CopyAmmo(IEnumerable<AuthoredTacticalAmmoStockDto> values)
            =>values.OrderBy(x=>x.WeaponId,StringComparer.Ordinal).Select(x=>new AuthoredTacticalAmmoStockDto{WeaponId=x.WeaponId,AmmunitionDefinitionId=x.AmmunitionDefinitionId,Stock=x.Stock}).ToList();
        private static AuthoredTacticalShipProvisionDto CopyProvision(AuthoredTacticalShipProvisionDto p)=>new AuthoredTacticalShipProvisionDto {
            ShipId=p.ShipId,OwnerCivilizationId=p.OwnerCivilizationId,ShipDesignDefinitionId=p.ShipDesignDefinitionId,FitDefinitionId=p.FitDefinitionId,
            FitRevision=p.FitRevision,InitialShields=CopyShields(p.InitialShields!),InitialAmmunition=CopyAmmo(p.InitialAmmunition!) };
        private static AuthoredTacticalFitDto CopyFit(AuthoredTacticalFitDto f)
            =>AuthoredTacticalEquipmentCopies.Copy(new AuthoredTacticalEquipmentCatalogueDto {
                AmmunitionDefinitions=new List<AuthoredTacticalAmmoDefinitionDto>(),Fits=new List<AuthoredTacticalFitDto>{f},
                InitialShipProvisions=new List<AuthoredTacticalShipProvisionDto>() }).Fits![0];
    }

    internal static class CampaignEquipmentSourceResolver
    {
        internal static CampaignEquipmentSource ResolveAuthored(DefinitionCatalogDto definitions,IEnumerable<CampaignTacticalCloakSourceDto> cloak,
            IEnumerable<CampaignIntegratedConstructionTemplateDto>? templates,IEnumerable<CampaignIntegratedBuiltShipDto> built,string shipId)
        {
            var initial=definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Where(p=>p.ShipId==shipId).ToList();
            var produced=built.Where(p=>p.ShipId==shipId).ToList();
            Require(initial.Count+produced.Count==1,"Unknown or ambiguous equipment source.");
            AuthoredTacticalShipProvisionDto provision;
            if(initial.Count==1)provision=initial[0];
            else
            {
                var record=produced[0];var matches=templates?.Where(t=>t.ShipDesignDefinitionId==record.ShipDesignDefinitionId && t.SourceProvisionShipId==record.SourceProvisionShipId).ToList();
                Require(matches?.Count==1,"Unknown or ambiguous construction template.");
                var originals=definitions.AuthoredTacticalEquipment.InitialShipProvisions.Where(p=>p.ShipId==matches![0].SourceProvisionShipId).ToList();
                Require(originals.Count==1,"Unknown or ambiguous authored provision.");var original=originals[0];
                provision=new AuthoredTacticalShipProvisionDto{ShipId=shipId,OwnerCivilizationId=record.OwnerCivilizationId,ShipDesignDefinitionId=original.ShipDesignDefinitionId,
                    FitDefinitionId=original.FitDefinitionId,FitRevision=original.FitRevision,InitialShields=original.InitialShields,InitialAmmunition=original.InitialAmmunition};
            }
            var fits=definitions.AuthoredTacticalEquipment.Fits!.Where(f=>f.DefinitionId==provision.FitDefinitionId && f.Revision==provision.FitRevision).ToList();
            Require(fits.Count==1,"Unknown or ambiguous authored fit.");var fit=fits[0];
            var policies=cloak.Where(c=>c.FitId==fit.DefinitionId && c.FitRevision==fit.Revision).ToList();
            Require(policies.Count==1 && policies[0].Capable.HasValue && policies[0].ConcurrentPowerRequired>=0,"Missing or ambiguous authored cloak policy.");
            var designs=definitions.ShipDesigns!.Where(d=>d.DefinitionId==provision.ShipDesignDefinitionId).ToList();
            Require(designs.Count==1,"Unknown or ambiguous authored design.");
            return new CampaignEquipmentSource(initial.Count==1?CampaignEquipmentSourceKind.AuthoredInitial:CampaignEquipmentSourceKind.StaticBuilt,
                shipId,provision.OwnerCivilizationId!,designs[0].DisplayName,provision.ShipDesignDefinitionId!,fit,provision.InitialShields!,provision.InitialAmmunition!,
                policies[0].Capable!.Value,policies[0].ConcurrentPowerRequired!.Value,provision);
        }
        private static void Require(bool condition,string message){if(!condition)throw new ArgumentException(message);}
    }
}
