using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    internal sealed class ValidatedPublishedBuiltSource
    {
        private readonly CampaignPublishedBuiltShipDto record;
        internal CampaignPublishedBuiltShipDto Provenance => PublishedBuiltShipValidation.Copy(record);
        internal CampaignEquipmentSource Equipment { get; }
        internal ValidatedPublishedBuiltSource(CampaignPublishedBuiltShipDto value,CampaignEquipmentSource equipment)
        {record=PublishedBuiltShipValidation.Copy(value);Equipment=equipment;}
    }

    // Detached consistency checks, NOT payment/admission authority or a live profile.
    // A future trusted caller must provide the actual staged ProductionPhase before/after
    // and must reconstruct retained history via full operation replay, not hashes alone.
    internal static class PublishedBuiltShipValidation
    {
        internal static IReadOnlyList<ValidatedPublishedBuiltSource> ValidateRetained(IReadOnlyList<CampaignPublishedBuiltShipDto> records,
            GameStateDto game,TacticalSupportCatalogueDto support,int initialTurn,ActivityTacticalSupportCatalogueDto? activitySupport=null)
        {
            Require(records!=null && records.Count<=4096 && game!=null && game.Ships!=null && game.Blueprints!=null && game.Civilizations!=null
                && initialTurn>=1 && game.TurnNumber>=initialTurn,"Missing or excessive detached inputs.");
            Require(records!.All(r=>r!=null) && records.Select(r=>r.ShipId).Distinct(StringComparer.Ordinal).Count()==records.Count
                && records.Select(r=>r.QueueItemId).Distinct(StringComparer.Ordinal).Count()==records.Count,"Duplicate provenance identity.");
            var sources=new List<ValidatedPublishedBuiltSource>();
            foreach(var supplied in records.OrderBy(r=>r.ShipId,StringComparer.Ordinal))
            {
                var r=Copy(supplied);
                Require(Allocated(r.ShipId,"ship.",out var sequence),"Invalid ship identity.");
                Require(Allocated(r.QueueItemId,"queue-item.",out var queueSequence),"Invalid queue identity.");
                Require(sequence<game!.NextEntitySequence && queueSequence<sequence
                    && EntityId.TryParse(r.FleetIdAtConstruction,out _) && game.Civilizations!.Count(c=>c.CivilizationId==r.OwnerCivilizationId)==1
                    && r.TurnBuilt>=initialTurn && r.TurnBuilt<=game.TurnNumber,"Invalid produced identity or turn.");
                var publications=game.Blueprints!.Where(p=>p.BlueprintId==r.BlueprintId && p.Revision==r.BlueprintRevision).ToList();
                Require(publications.Count==1,"Unknown or ambiguous publication revision.");var publication=publications[0];
                Require(publication.OwnerCivilizationId==r.OwnerCivilizationId && publication.CompilerVersion==ShipDesignVersions.Compiler
                    && !string.IsNullOrWhiteSpace(publication.RulesVersion) && Hash(publication.SourceContentHash)
                    && publication.Fit!=null && publication.Cost!=null && publication.CompiledDesign!=null
                    && ShipDesignCatalogueValidation.Validate(publication.FrozenCatalogue).Count==0,"Invalid frozen publication source.");
                // FrozenAccess is the existing archival-recompilation convention. It validates
                // the recorded source, never current Engineering access or queue permission.
                var frozen=new ShipDesignCatalogueSnapshot(publication.FrozenCatalogue!);
                var restored=CampaignBlueprints.Restore(publication);
                Require(restored!=null,"Frozen publication failed recompilation.");
                var expected=CampaignBlueprints.Design(restored!);
                Require(BlueprintCopies.Same(BlueprintCopies.Copy(expected),BlueprintCopies.Copy(publication.CompiledDesign!))
                    && BlueprintCopies.Same(BlueprintCopies.Copy(publication.Cost!),new ShipDesignCostDto{Credits=restored!.Performance.Cost!.Credits,
                        Industry=restored.Performance.Cost.Industry,Maintenance=restored.Performance.Cost.Maintenance,
                        Resources=restored.Performance.Cost.Resources.Select(x=>new ResourceAmountDto{ResourceDefinitionId=x.ResourceDefinitionId,Amount=x.Amount}).ToList()}),"Forged compiled source or cost.");
                var activity=activitySupport==null?null:PublishedTacticalSupportCompiler.EvaluateActivities(frozen,restored!,CampaignBlueprints.FrozenAccess(publication.FrozenCatalogue!),activitySupport);
                var compiled=activity==null?PublishedTacticalSupportCompiler.Evaluate(frozen,restored!,CampaignBlueprints.FrozenAccess(publication.FrozenCatalogue!),support)
                    :new PublishedTacticalSupportResult(activity.Issues,activity.Ship?.Tactical);
                Require(compiled.IsSupportedByCompiler,"Unsupported frozen tactical source.");var fit=compiled.Fit!;
                Require(r.SourceCompilerId==publication.CompilerVersion && r.SourceRulesVersion==publication.RulesVersion && r.SourceContentHash==publication.SourceContentHash
                    && r.PublicationHash==fit.SourceHash && r.SupportCatalogueId==fit.SupportCatalogueId && r.SupportCatalogueRevision==fit.SupportCatalogueRevision
                    && r.SupportHash==fit.SupportHash && r.SupportCompilerId==fit.CompilerId && r.FitId==fit.FitId && r.FitRevision==fit.FitRevision,"Source/support provenance mismatch.");
                var live=game.Ships!.Where(s=>s.ShipId==r.ShipId).ToList();Require(live.Count<=1,"Ambiguous live ship.");
                if(live.Count==1)Require(live[0].OwnerCivilizationId==r.OwnerCivilizationId && string.IsNullOrEmpty(live[0].ShipDesignDefinitionId)
                    && live[0].Blueprint?.BlueprintId==r.BlueprintId && live[0].Blueprint?.Revision==r.BlueprintRevision
                    && live[0].CurrentHull>0 && live[0].CurrentHull<=fit.MaxHull,"Live ship/source mismatch.");
                var equipment=new CampaignEquipmentSource(CampaignEquipmentSourceKind.PublishedBuilt,r.ShipId!,r.OwnerCivilizationId!,restored!.Fit.DisplayName,
                    restored.BlueprintId,ToFit(fit),fit.ZeroShieldSectors.Select(x=>new AuthoredTacticalShieldStockDto{Sector=x,Current=0}),
                    Array.Empty<AuthoredTacticalAmmoStockDto>(),false,0);
                sources.Add(new ValidatedPublishedBuiltSource(r,equipment));
            }
            return sources.AsReadOnly();
        }

        internal static ValidatedPublishedBuiltSource ValidateCompletion(CampaignPublishedBuiltShipDto record,GameStateDto before,GameStateDto after,
            TacticalSupportCatalogueDto support,int initialTurn,ActivityTacticalSupportCatalogueDto? activitySupport=null)
        {
            Require(record!=null && before?.Colonies!=null && before.Ships!=null && before.Blueprints!=null && after?.Colonies!=null && after.Ships!=null && after.Blueprints!=null,"Missing completion association.");
            var heads=before!.Colonies!.Where(c=>c.ProductionQueue?.Count>0 && c.ProductionQueue[0].QueueItemId==record!.QueueItemId).ToList();
            Require(heads.Count==1,"Completion must name exactly one actual pre-production queue head.");var colony=heads[0];var queue=colony.ProductionQueue![0];
            Require(queue.Blueprint!=null && queue.Blueprint.BlueprintId==record!.BlueprintId && queue.Blueprint.Revision==record.BlueprintRevision
                && colony.OwnerCivilizationId==record.OwnerCivilizationId && record.TurnBuilt==before.TurnNumber
                && !before.Ships!.Any(s=>s.ShipId==record.ShipId) && after!.Ships!.Count(s=>s.ShipId==record.ShipId && s.FleetId==record.FleetIdAtConstruction)==1
                && Allocated(record.ShipId,"ship.",out var sequence) && sequence>=before.NextEntitySequence && sequence<after.NextEntitySequence
                && !after.Colonies!.Any(c=>c.ProductionQueue!.Any(q=>q.QueueItemId==record.QueueItemId)),"Queue/produced-ship association mismatch.");
            var records=before.Blueprints!.Where(p=>p.BlueprintId==record!.BlueprintId && p.Revision==record.BlueprintRevision).ToList();
            Require(records.Count==1,"Missing paid publication.");var source=records[0];
            var current=after!.Blueprints!.Where(p=>p.BlueprintId==record!.BlueprintId && p.Revision==record.BlueprintRevision).ToList();
            Require(current.Count==1 && BlueprintCopies.Same(source,current[0]) && source.CompiledDesign!=null,"Publication changed during completion.");
            var cost=source.CompiledDesign!;
            Require(queue.PaidCredits==cost.CreditCost && queue.RequiredIndustry==cost.IndustryCost && queue.CompletedIndustry>=0 && queue.CompletedIndustry<=queue.RequiredIndustry
                && queue.PaidResources!=null && cost.ResourceCosts!=null && queue.PaidResources.Count==cost.ResourceCosts.Count
                && queue.PaidResources.OrderBy(x=>x.ResourceDefinitionId,StringComparer.Ordinal).Zip(cost.ResourceCosts.OrderBy(x=>x.ResourceDefinitionId,StringComparer.Ordinal),BlueprintCopies.Same).All(x=>x),"Paid receipt/source cost mismatch.");
            return ValidateRetained(new[]{record!},after,support,initialTurn,activitySupport)[0];
        }
        internal static CampaignPublishedBuiltShipDto Copy(CampaignPublishedBuiltShipDto r)=>new CampaignPublishedBuiltShipDto {
            ShipId=r.ShipId,BlueprintId=r.BlueprintId,BlueprintRevision=r.BlueprintRevision,OwnerCivilizationId=r.OwnerCivilizationId,FleetIdAtConstruction=r.FleetIdAtConstruction,
            QueueItemId=r.QueueItemId,TurnBuilt=r.TurnBuilt,SourceCompilerId=r.SourceCompilerId,SourceRulesVersion=r.SourceRulesVersion,SourceContentHash=r.SourceContentHash,
            PublicationHash=r.PublicationHash,SupportCatalogueId=r.SupportCatalogueId,SupportCatalogueRevision=r.SupportCatalogueRevision,SupportHash=r.SupportHash,
            SupportCompilerId=r.SupportCompilerId,FitId=r.FitId,FitRevision=r.FitRevision };
        internal static AuthoredTacticalFitDto ToFit(SupportedPublishedTacticalFit s)=>new AuthoredTacticalFitDto {
            DefinitionId=s.FitId,Revision=s.FitRevision,ProvenanceId=s.ProvenanceId,MaxHull=s.MaxHull,Armor=s.Armor,EvasionBps=s.EvasionBps,Role=s.Role,
            Capabilities=new AuthoredTacticalCapabilitiesDto{Speed=s.Capabilities.Speed,Agility=s.Capabilities.Agility,Sensors=s.Capabilities.Sensors,Command=s.Capabilities.Command,Cohesion=s.Capabilities.Cohesion},
            ReactorOutput=s.ReactorOutput,DistributionLimit=s.DistributionLimit,ShieldRechargePerRound=s.ShieldRechargePerRound,
            ShieldRechargeRequiredPower=s.ShieldRechargeRequiredPower,ShieldRechargeAllocatedPower=s.ShieldRechargeAllocatedPower,
            ShieldLimits=s.ZeroShieldSectors.Select(x=>new AuthoredTacticalShieldLimitDto{Sector=x,Maximum=0}).ToList(),
            EssentialReservations=s.EssentialReservations.Select(x=>new AuthoredTacticalEssentialPowerDto{System=x.System,RequiredPower=x.RequiredPower,AllocatedPower=x.AllocatedPower}).ToList(),
            Mounts=s.Mounts.Select(x=>new AuthoredTacticalMountDto{MountId=x.MountId,AllowedWeaponClasses=x.AllowedWeaponClasses.ToList(),Coverage=x.Coverage,Slots=x.Slots}).ToList(),
            Weapons=s.Weapons.Select(x=>new AuthoredTacticalWeaponDto{WeaponId=x.WeaponId,PackageVersionId=x.PackageVersionId,MountId=x.MountId,SlotsRequired=x.SlotsRequired,Class=x.Class,
                Coverage=x.Coverage,Damage=x.Damage,AccuracyBps=x.AccuracyBps,TrackingBps=x.TrackingBps,OpportunitiesPerRound=x.OpportunitiesPerRound,CadenceRounds=x.CadenceRounds,
                InitialNextFireRound=x.InitialNextFireRound,RequiredPower=x.RequiredPower,AllocatedPower=x.AllocatedPower,AmmunitionDefinitionId=x.AmmunitionDefinitionId,MagazineCapacity=x.MagazineCapacity}).ToList() };
        private static bool Allocated(string? text,string prefix,out ulong sequence){sequence=0;return text!=null && text.StartsWith(prefix,StringComparison.Ordinal) && EntityId.TryParse(text,out var id) && id.TryGetAllocationSequence(out sequence);}
        private static bool Hash(string? s)=>s!=null && s.Length==64 && s.All(c=>c>='0' && c<='9' || c>='a' && c<='f');
        private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool value,string message){if(!value)throw new ArgumentException(message);}
    }
}
