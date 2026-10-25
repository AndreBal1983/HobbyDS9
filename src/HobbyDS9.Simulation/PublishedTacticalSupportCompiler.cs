using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
namespace HobbyDS9.Simulation
{
    // Deterministic derivation used by detached inspection and the explicit portable
    // published-construction profile. Metadata itself never grants admission.
    public static class PublishedTacticalSupportCompiler
    {
        private sealed class SupportMappings
        {
            internal readonly Dictionary<string,TacticalHullSupportDto> Hulls;
            internal readonly Dictionary<string,TacticalComponentSupportDto> Components;
            internal SupportMappings(Dictionary<string,TacticalHullSupportDto> hulls,Dictionary<string,TacticalComponentSupportDto> components)
            { Hulls=hulls;Components=components; }
        }
        // Same metadata validation used by standalone derivation and explicit package ingress.
        internal static string ValidateMetadata(ShipDesignCatalogueSnapshot catalogue,TacticalSupportCatalogueDto support)
        {
            var issues=new List<TacticalSupportIssue>();
            var mappings=MapMetadata(catalogue,support,(c,p)=>issues.Add(new TacticalSupportIssue(c,p)));
            if(mappings==null||issues.Count!=0)throw new ArgumentException("Unsupported tactical support metadata.");
            return PublishedTacticalSupportEncoding.Support(support,new PublishedTacticalSupportEncoding.Budget());
        }
        private static SupportMappings? MapMetadata(ShipDesignCatalogueSnapshot catalogue,TacticalSupportCatalogueDto support,Action<TacticalSupportIssueCode,string> Error)
        {
                if(support==null||support.FormatVersion!=PublishedTacticalSupportIdentity.Format||support.SchemaId!=PublishedTacticalSupportIdentity.Schema
                    ||!DefinitionId.TryParse(support.CatalogueId,out _)||support.Revision==null||support.Revision<1
                    ||support.SourceCompilerId!=ShipDesignVersions.Compiler||support.BalanceStatus!="provisional-test-only"
                    ||support.CloakPolicyId!=PublishedTacticalSupportIdentity.NoCloak
                    ||support.Hulls==null||support.Hulls.Count==0||support.Hulls.Count>64||support.Components==null||support.Components.Count==0||support.Components.Count>256)
                {Error(TacticalSupportIssueCode.InvalidMetadata,"$.Support");return null;}
                var hulls=new Dictionary<string,TacticalHullSupportDto>(StringComparer.Ordinal);
                foreach(var h in support.Hulls)
                {
                    if(h==null||h.HullVersionId==null||!hulls.TryAdd(h.HullVersionId,h)){Error(TacticalSupportIssueCode.InvalidMetadata,"$.Support.Hulls");continue;}
                    var source=catalogue.Hulls.SingleOrDefault(x=>x.VersionId==h.HullVersionId);
                    if(source==null||h.Armor==null||h.Armor<0||h.TacticalRole==null||!Enum.IsDefined(typeof(TacticalShipRole),h.TacticalRole.Value)
                        ||h.SupportedRoleIds==null||h.SupportedRoleIds.Count==0||h.SupportedRoleIds.Distinct(StringComparer.Ordinal).Count()!=h.SupportedRoleIds.Count
                        ||h.SupportedRoleIds.Any(x=>!catalogue.Roles.Any(r=>r.RoleId==x))||!Capabilities(h.Capabilities)||h.Mounts==null||h.Mounts.Count>64)
                    {Error(TacticalSupportIssueCode.InvalidMetadata,"$.Support.Hulls."+h.HullVersionId);continue;}
                    var mountIds=new HashSet<string>(StringComparer.Ordinal);
                    foreach(var m in h.Mounts)
                    {
                        var physical=source.Mounts.SingleOrDefault(x=>x.Id==m?.SourceMountId);
                        if(m==null||physical==null||!mountIds.Add(m.SourceMountId!)||physical.Kind!=MountKind.EnergyBank
                            ||m.SourceCoverageTag!=physical.CoverageTag||m.Slots==null||m.Slots<1||m.Slots>physical.Count||!Arc(m.Coverage))
                            Error(TacticalSupportIssueCode.MountBinding,"$.Support.Hulls."+h.HullVersionId+".Mounts");
                    }
                }
                var components=new Dictionary<string,TacticalComponentSupportDto>(StringComparer.Ordinal);
                foreach(var c in support.Components)
                {
                    if(c==null||c.ComponentVersionId==null||!components.TryAdd(c.ComponentVersionId,c)){Error(TacticalSupportIssueCode.InvalidMetadata,"$.Support.Components");continue;}
                    var source=catalogue.Components.SingleOrDefault(x=>x.VersionId==c.ComponentVersionId);
                    if(source==null||!Matches(c.Kind,source.Kind)||(c.Kind!=TacticalComponentSupportKind.Beam&&c.Beam!=null)
                        ||(c.Kind==TacticalComponentSupportKind.TargetingSensors?!Rating(c.TacticalSensorRating):c.TacticalSensorRating!=null)
                        ||(c.Kind==TacticalComponentSupportKind.Beam&&!Beam(c.Beam)))
                        Error(TacticalSupportIssueCode.InvalidMetadata,"$.Support.Components."+c.ComponentVersionId);
                }
                return new SupportMappings(hulls,components);

        }
        public static PublishedTacticalSupportResult Evaluate(ShipDesignCatalogueSnapshot catalogue,PublishedShipBlueprint publication,
            ShipDesignAccessDto access,TacticalSupportCatalogueDto support)
        {
            var issues=new List<TacticalSupportIssue>();
            void Error(TacticalSupportIssueCode c,string p)=>issues.Add(new TacticalSupportIssue(c,p));
            PublishedTacticalSupportResult Rejected()=>new PublishedTacticalSupportResult(issues);
            try
            {
                if(catalogue==null||catalogue.FormatVersion!=ShipDesignVersions.CatalogueFormat||publication==null||access==null||publication.Fit==null||publication.Hull==null||publication.Components==null||publication.Performance==null||publication.Performance.Cost==null||publication.Performance.Compiled==null){Error(TacticalSupportIssueCode.InvalidPublication,"$.Publication");return Rejected();}
                ShipBlueprintPublicationResult ordinary;
                try{ordinary=new ShipDesignCompiler(catalogue).Publish(Draft(publication.Fit),access,publication.BlueprintId,publication.Revision);}
                catch(ArgumentException){Error(TacticalSupportIssueCode.InvalidPublication,"$.Publication");return Rejected();}
                catch(InvalidOperationException){Error(TacticalSupportIssueCode.InvalidPublication,"$.Publication");return Rejected();}
                if(!ordinary.Published){Error(TacticalSupportIssueCode.OrdinaryDesignRejected,"$.Publication");return new PublishedTacticalSupportResult(issues,null,ordinary.Preview.Issues);}
                // Public snapshot constructors are not an authority bypass: independently recompile.
                var encodingBudget=new PublishedTacticalSupportEncoding.Budget();
                var suppliedSourceHash=PublishedTacticalSupportEncoding.Publication(publication,encodingBudget);
                var sourceHash=PublishedTacticalSupportEncoding.Publication(ordinary.Blueprint!,encodingBudget);
                if(suppliedSourceHash!=sourceHash){Error(TacticalSupportIssueCode.IdentityMismatch,"$.Publication");return Rejected();}
                var mappings=MapMetadata(catalogue,support,Error);
                if(mappings==null||issues.Count>0)return Rejected();
                var hulls=mappings.Hulls;var components=mappings.Components;
                if(!hulls.TryGetValue(publication.Fit.HullVersionId,out var hull)){Error(TacticalSupportIssueCode.MissingMapping,"$.Publication.Hull");return Rejected();}
                if(!hull.SupportedRoleIds!.Contains(publication.Fit.RoleId,StringComparer.Ordinal))Error(TacticalSupportIssueCode.UnsupportedRole,"$.Publication.Role");
                var actual=ordinary.Blueprint!;var combat=actual.Performance.Profiles.Single(p=>p.Kind==OperatingProfileKind.Combat);
                var power=new Dictionary<TacticalComponentSupportKind,long>();
                var weapons=new List<AuthoredTacticalWeaponDto>();var usedMounts=new Dictionary<string,AuthoredTacticalMountDto>(StringComparer.Ordinal);
                foreach(var install in actual.Fit.Installations)
                {
                    var path="$.Publication.Installations."+install.InstallationId;
                    if(!components.TryGetValue(install.ComponentVersionId,out var mapped)){Error(TacticalSupportIssueCode.MissingMapping,path);continue;}
                    if(install.Quantity!=1){Error(TacticalSupportIssueCode.UnsupportedQuantity,path);continue;}
                    var source=actual.Components.Single(c=>c.VersionId==install.ComponentVersionId);
                    var mode=source.Modes.Single(m=>m.Profile==OperatingProfileKind.Combat);
                    var allocated=combat.Installations.Single(i=>i.InstallationId==install.InstallationId);
                    var requested=actual.Fit.Profiles.Single(p=>p.Kind==OperatingProfileKind.Combat).Allocations.Single(a=>a.InstallationId==install.InstallationId);
                    if(requested.RequestedPower!=mode.FullPower||allocated.AllocatedPower!=mode.FullPower||(mapped.Kind!=TacticalComponentSupportKind.Reactor&&mode.FullPower<=0))
                    {Error(TacticalSupportIssueCode.CombatPower,path);continue;}
                    if(mapped.Kind!=TacticalComponentSupportKind.Beam)
                    {
                        if(!power.TryAdd(mapped.Kind!.Value,allocated.AllocatedPower))Error(TacticalSupportIssueCode.UnsupportedComponent,path);
                        continue;
                    }
                    var b=mapped.Beam!;var mount=hull.Mounts!.SingleOrDefault(m=>m.SourceMountId==b.SourceMountId);
                    var physical=actual.Hull.Mounts.SingleOrDefault(m=>m.Id==b.SourceMountId);
                    var requirement=source.Mounts.Count==1?source.Mounts[0]:null;
                    if(mount==null||physical==null||requirement==null||requirement.Kind!=MountKind.EnergyBank||requirement.Count!=b.SlotsRequired
                        ||requirement.SizeRating>physical.SizeRating||requirement.CoverageTag!=mount.SourceCoverageTag
                        ||(b.Coverage!.Value&mount.Coverage!.Value)!=b.Coverage.Value)
                    {Error(TacticalSupportIssueCode.MountBinding,path);continue;}
                    usedMounts[mount.SourceMountId!]=new AuthoredTacticalMountDto {MountId=mount.SourceMountId,Slots=mount.Slots,Coverage=mount.Coverage,AllowedWeaponClasses=new List<TacticalWeaponClass>{TacticalWeaponClass.Beam}};
                    weapons.Add(new AuthoredTacticalWeaponDto {WeaponId="weapon."+install.InstallationId,PackageVersionId=install.ComponentVersionId,MountId=b.SourceMountId,
                        SlotsRequired=b.SlotsRequired,Class=TacticalWeaponClass.Beam,Coverage=b.Coverage,Damage=b.Damage,AccuracyBps=b.AccuracyBps,TrackingBps=b.TrackingBps,
                        OpportunitiesPerRound=b.OpportunitiesPerRound,CadenceRounds=b.CadenceRounds,InitialNextFireRound=b.InitialNextFireRound,
                        RequiredPower=mode.FullPower,AllocatedPower=allocated.AllocatedPower,AmmunitionDefinitionId="",MagazineCapacity=0});
                }
                foreach(var kind in new[]{TacticalComponentSupportKind.Reactor,TacticalComponentSupportKind.LifeSupport,TacticalComponentSupportKind.Command,TacticalComponentSupportKind.Propulsion})
                    if(!power.ContainsKey(kind))Error(TacticalSupportIssueCode.UnsupportedComponent,"$.Publication.Core."+kind);
                if(actual.Performance.Compiled!.Combat.ShieldCapacity!=0)Error(TacticalSupportIssueCode.UnsupportedComponent,"$.Publication.Shields");
                if(issues.Count>0)return Rejected();
                var command=power[TacticalComponentSupportKind.Command];
                power.TryGetValue(TacticalComponentSupportKind.TargetingSensors,out var targeting);
                if(weapons.Count>0&&targeting<=0){Error(TacticalSupportIssueCode.CombatPower,"$.Publication.TargetingSensors");return Rejected();}
                int sensorRating=hull.Capabilities!.Sensors!.Value;
                foreach(var i in actual.Fit.Installations.Where(i=>components[i.ComponentVersionId].Kind==TacticalComponentSupportKind.TargetingSensors))
                    sensorRating=Math.Max(sensorRating,components[i.ComponentVersionId].TacticalSensorRating!.Value);
                var supportHash=PublishedTacticalSupportEncoding.Support(support,encodingBudget);var fitId="fit.support."+HashText(PublishedTacticalSupportIdentity.Compiler+":"+supportHash+":"+sourceHash);
                AuthoredTacticalEssentialPowerDto Reserve(TacticalEssentialSystem system,long amount)=>new AuthoredTacticalEssentialPowerDto {System=system,RequiredPower=amount,AllocatedPower=amount};
                var fit=new AuthoredTacticalFitDto {DefinitionId=fitId,Revision=publication.Revision,ProvenanceId=support.CatalogueId,
                    MaxHull=actual.Performance.Compiled.MaxHull,Armor=hull.Armor,EvasionBps=actual.Performance.Compiled.Combat.EvasionBps,Role=hull.TacticalRole,
                    Capabilities=new AuthoredTacticalCapabilitiesDto {Speed=hull.Capabilities.Speed,Agility=hull.Capabilities.Agility,Sensors=sensorRating,Command=hull.Capabilities.Command,Cohesion=hull.Capabilities.Cohesion},
                    ReactorOutput=actual.Performance.ReactorPower,DistributionLimit=actual.Performance.DistributionLimit,
                    EssentialReservations=new List<AuthoredTacticalEssentialPowerDto>{Reserve(TacticalEssentialSystem.LifeSupport,power[TacticalComponentSupportKind.LifeSupport]),
                        Reserve(TacticalEssentialSystem.Command,command),Reserve(TacticalEssentialSystem.Propulsion,power[TacticalComponentSupportKind.Propulsion]),Reserve(TacticalEssentialSystem.Targeting,targeting)},
                    ShieldRechargeRequiredPower=0,ShieldRechargeAllocatedPower=0,ShieldRechargePerRound=0,
                    ShieldLimits=Enum.GetValues(typeof(TacticalSector)).Cast<TacticalSector>().Select(s=>new AuthoredTacticalShieldLimitDto {Sector=s,Maximum=0}).ToList(),Mounts=usedMounts.Values.ToList(),Weapons=weapons};
                // Reuse all actual tactical field/power/mount validators without any ship/provision.
                var validation=AuthoredTacticalEquipment.Validate(new DefinitionCatalogDto {ContentFormatVersion=AuthoredTacticalEquipmentIdentity.ContentFormatVersion,DefinitionIds=new List<string>{fitId},
                    AuthoredTacticalEquipment=new AuthoredTacticalEquipmentCatalogueDto {FormatVersion=AuthoredTacticalEquipmentIdentity.FormatVersion,SchemaId=AuthoredTacticalEquipmentIdentity.SchemaId,
                        ProvenanceId=support.CatalogueId,BalanceStatus="provisional-test-scenario",AmmunitionDefinitions=new List<AuthoredTacticalAmmoDefinitionDto>(),
                        Fits=new List<AuthoredTacticalFitDto>{fit},InitialShipProvisions=new List<AuthoredTacticalShipProvisionDto>()}});
                foreach(var failure in validation.Issues)Error(TacticalSupportIssueCode.TacticalValidation,failure.Path);
                if(issues.Count>0)return Rejected();
                return new PublishedTacticalSupportResult(issues,new SupportedPublishedTacticalFit(actual,support,supportHash,sourceHash,fit));
            }
            catch(OverflowException){Error(TacticalSupportIssueCode.InvalidMetadata,"$.Arithmetic");return Rejected();}
            catch(ArgumentException){Error(TacticalSupportIssueCode.InvalidPublication,"$.Publication");return Rejected();}
        }
        private static bool Capabilities(AuthoredTacticalCapabilitiesDto? c)=>c!=null&&Rating(c.Speed)&&Rating(c.Agility)&&Rating(c.Sensors)&&Rating(c.Command)&&Rating(c.Cohesion);
        private static bool Rating(int? n)=>n.HasValue&&n>=0&&n<=100;
        private static bool Arc(TacticalArc? a)=>a.HasValue&&a!=TacticalArc.None&&(a&~TacticalArc.All)==0;
        private static bool Beam(TacticalBeamSupportDto? b)=>b!=null&&DefinitionId.TryParse(b.SourceMountId,out _)&&b.SlotsRequired>0&&Arc(b.Coverage)
            &&b.Damage.HasValue&&b.Damage>=0&&b.AccuracyBps.HasValue&&b.AccuracyBps>=0&&b.AccuracyBps<=10000&&b.TrackingBps.HasValue&&b.TrackingBps>=0&&b.TrackingBps<=10000
            &&b.OpportunitiesPerRound.HasValue&&b.OpportunitiesPerRound>=1&&b.OpportunitiesPerRound<=64&&b.CadenceRounds.HasValue&&b.CadenceRounds>=1&&b.CadenceRounds<=3
            &&b.InitialNextFireRound.HasValue&&b.InitialNextFireRound>=1&&b.InitialNextFireRound<=b.CadenceRounds;
        private static bool Matches(TacticalComponentSupportKind? support,ComponentKind source)=>(support==TacticalComponentSupportKind.Reactor&&source==ComponentKind.Reactor)
            ||(support==TacticalComponentSupportKind.LifeSupport&&source==ComponentKind.LifeSupport)||(support==TacticalComponentSupportKind.Command&&source==ComponentKind.Command)
            ||(support==TacticalComponentSupportKind.Propulsion&&source==ComponentKind.Propulsion)||(support==TacticalComponentSupportKind.Beam&&source==ComponentKind.EnergyWeapon)
            ||(support==TacticalComponentSupportKind.TargetingSensors&&source==ComponentKind.Sensors);
        private static ShipClassDraftDto Draft(ShipClassDraftSnapshot d)=>new ShipClassDraftDto {ClassId=d.ClassId,DisplayName=d.DisplayName,HullVersionId=d.HullVersionId,RoleId=d.RoleId,EmbarkedCrew=d.EmbarkedCrew,
            Installations=d.Installations.Select(i=>new ShipInstallationDto {InstallationId=i.InstallationId,ComponentVersionId=i.ComponentVersionId,Quantity=i.Quantity}).ToList(),
            Profiles=d.Profiles.Select(p=>new ShipOperatingProfileDto {Kind=p.Kind,Allocations=p.Allocations.Select(a=>new ProfilePowerAllocationDto {InstallationId=a.InstallationId,RequestedPower=a.RequestedPower,AllocatedPower=a.AllocatedPower}).ToList()}).ToList()};
        private static string HashText(string text){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)));}
    }
}
