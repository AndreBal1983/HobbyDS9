using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private sealed class PublishedConstructionContext
        {
            internal readonly CampaignPlayableFleetPackageDto Package;
            internal readonly TacticalSupportCatalogueDto Support;
            internal readonly string WrapperHash,SupportHash;
            internal readonly int RecordBudget;
            internal List<CampaignPublishedBuiltShipDto> Records=new List<CampaignPublishedBuiltShipDto>();
            internal Dictionary<string,CampaignEquipmentSource> Sources=new Dictionary<string,CampaignEquipmentSource>(StringComparer.Ordinal);
            internal PublishedConstructionContext(CampaignPlayableFleetPackageDto package,TacticalSupportCatalogueDto support,string wrapper,string supportHash,int budget)
            {Package=package;Support=support;WrapperHash=wrapper;SupportHash=supportHash;RecordBudget=budget;}
            internal PublishedConstructionContext Empty()=>new PublishedConstructionContext(Package,Support,WrapperHash,SupportHash,RecordBudget);
            internal PublishedConstructionContext Copy()
            {var c=Empty();c.Records=Records.Select(PublishedBuiltShipValidation.Copy).ToList();c.Sources=new Dictionary<string,CampaignEquipmentSource>(Sources,StringComparer.Ordinal);return c;}
        }
        private PublishedConstructionContext? published;
        private sealed class PublishedRecordCapacityException : ArgumentException { }
        private bool publishedExecuting;
        internal Action<string>? PublishedTransactionProbe { get; set; }

        public static CampaignPublishedConstructionPackageDto CreatePublishedConstructionPackage(CampaignPlayableFleetPackageDto template,TacticalSupportCatalogueDto support,int recordBudget)
        {
            var hash=CampaignPublishedConstructionData.WrapperHash(template,support,recordBudget);
            return new CampaignPublishedConstructionPackageDto{FormatVersion=CampaignPublishedConstructionIdentity.PackageFormat,
                ProfileId=CampaignPublishedConstructionIdentity.PackageProfile,PackageHash=hash,PolicyStatus="proposed",DefaultActivationAllowed=false,
                BaseTemplate=template,Support=CampaignPublishedConstructionData.CopySupport(support),RetainedRecordBudget=recordBudget};
        }
        private static void RequirePublishedPackage(CampaignPublishedConstructionPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==CampaignPublishedConstructionIdentity.PackageFormat&&p.ProfileId==CampaignPublishedConstructionIdentity.PackageProfile
                &&p.PolicyStatus=="proposed"&&p.DefaultActivationAllowed==false&&p.BaseTemplate!=null&&p.Support!=null&&p.RetainedRecordBudget.HasValue,
                "Explicit complete published-construction package required.");
            Require(p!.PackageHash==CampaignPublishedConstructionData.WrapperHash(p.BaseTemplate!,p.Support!,p.RetainedRecordBudget!.Value),"Published wrapper identity mismatch.");
        }
        public static CampaignTacticalReviewSession StartPublishedConstruction(CampaignPublishedConstructionPackageDto p,NaturalGalaxySettingsDto settings)
        {
            RequirePublishedPackage(p);
            var map=NaturalGalaxyGenerator.Generate(settings,p.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!);
            return StartPublishedFrozen(p,GeneratedCampaignFactory.Frozen(map,p.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!));
        }
        private static CampaignTacticalReviewSession StartPublishedFrozen(CampaignPublishedConstructionPackageDto p,NaturalGalaxyDocumentDto map)
        {
            var package=GeneratedCampaignFactory.BuildPublished(p.BaseTemplate!,map,p.PackageHash!);
            ValidatePlayablePackage(package,generated:true);
            var support=CampaignPublishedConstructionData.CopySupport(p.Support!);
            var supportHash=PublishedTacticalSupportCompiler.ValidateMetadata(new ShipDesignCatalogueSnapshot(package.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.ShipDesignCatalogue!),support);
            var context=new PublishedConstructionContext(package,support,p.PackageHash!,supportHash,p.RetainedRecordBudget!.Value);
            return NewPublished(context,map);
        }
        private static CampaignTacticalReviewSession NewPublished(PublishedConstructionContext context,NaturalGalaxyDocumentDto map)
        {
            var i=context.Package.Integrated!;
            return new CampaignTacticalReviewSession(i.Physical!.CorePackage!,null,i.Physical,null,i,null,null,null,context.Package,map,context.WrapperHash,true,context);
        }
        public CampaignPublishedConstructionSaveDto ExportPublishedConstructionSave()
        {
            lock(gate)
            {
                Require(published!=null,"Not a published-construction profile.");
                return new CampaignPublishedConstructionSaveDto{FormatVersion=CampaignPublishedConstructionIdentity.Format,
                    ProfileId=CampaignPublishedConstructionIdentity.Profile,PolicyId=CampaignPublishedConstructionIdentity.Policy,
                    TemplatePackageHash=published!.WrapperHash,PackageHash=playableHash,
                    SupportIdentity=CampaignPublishedConstructionData.SupportIdentity(published.Support,published.SupportHash),
                    Map=NaturalGalaxyData.Copy(generatedMap!),Campaign=ExportPlayableCore(),Discoveries=discoveries.Select(CopyDiscovery).ToList(),
                    PublishedBuiltShips=published.Records.OrderBy(r=>r.ShipId,StringComparer.Ordinal).Select(PublishedBuiltShipValidation.Copy).ToList()};
            }
        }
        public static CampaignTacticalReviewSession LoadPublishedConstruction(CampaignPublishedConstructionPackageDto p,CampaignPublishedConstructionSaveDto save)
        {
            RequirePublishedPackage(p);
            Require(save!=null&&save.FormatVersion==CampaignPublishedConstructionIdentity.Format&&save.ProfileId==CampaignPublishedConstructionIdentity.Profile
                &&save.PolicyId==CampaignPublishedConstructionIdentity.Policy&&save.TemplatePackageHash==p.PackageHash&&save.Map!=null&&save.Campaign!=null
                &&save.Discoveries!=null&&save.Discoveries.All(r=>r!=null&&r.ChartedNodeIds!=null&&r.ChartedLaneIds!=null)
                &&save.PublishedBuiltShips!=null&&save.PublishedBuiltShips.All(r=>r!=null)&&save.PublishedBuiltShips.Count<=p.RetainedRecordBudget,"Incomplete/new-profile save required; no conversion.");
            var map=GeneratedCampaignFactory.Frozen(save!.Map!,p.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!);
            var initial=StartPublishedFrozen(p,map);
            Require(save.PackageHash==initial.playableHash&&CampaignPublishedConstructionData.Same(save.SupportIdentity,
                CampaignPublishedConstructionData.SupportIdentity(initial.published!.Support,initial.published.SupportHash)),"Different pinned support/generated package.");
            var replay=ReplayPlayable(initial.published.Package,save.Campaign!,initial);
            var actual=replay.ExportPublishedConstructionSave();
            Require(actual.Discoveries!.Count==save.Discoveries!.Count&&actual.Discoveries.Zip(save.Discoveries,(a,b)=>
                a.ActorId==b.ActorId&&a.FleetId==b.FleetId&&a.ArrivalNodeId==b.ArrivalNodeId&&a.Turn==b.Turn
                &&a.ChartedNodeIds.SequenceEqual(b.ChartedNodeIds)&&a.ChartedLaneIds.SequenceEqual(b.ChartedLaneIds)).All(x=>x),"Discovery replay mismatch.");
            Require(actual.PublishedBuiltShips!.Count==save.PublishedBuiltShips!.Count&&actual.PublishedBuiltShips.Zip(save.PublishedBuiltShips,SamePublishedRecord).All(x=>x),"Published provenance replay mismatch.");
            return replay;
        }
        private static bool SamePublishedRecord(CampaignPublishedBuiltShipDto a,CampaignPublishedBuiltShipDto b)=>
            (a.ShipId,a.BlueprintId,a.BlueprintRevision,a.OwnerCivilizationId,a.FleetIdAtConstruction,a.QueueItemId,a.TurnBuilt,a.SourceCompilerId,a.SourceRulesVersion,a.SourceContentHash,
                a.PublicationHash,a.SupportCatalogueId,a.SupportCatalogueRevision,a.SupportHash,a.SupportCompilerId,a.FitId,a.FitRevision)
            ==(b.ShipId,b.BlueprintId,b.BlueprintRevision,b.OwnerCivilizationId,b.FleetIdAtConstruction,b.QueueItemId,b.TurnBuilt,b.SourceCompilerId,b.SourceRulesVersion,b.SourceContentHash,
                b.PublicationHash,b.SupportCatalogueId,b.SupportCatalogueRevision,b.SupportHash,b.SupportCompilerId,b.FitId,b.FitRevision);

        private CampaignTacticalReviewSession(CampaignTacticalReviewSession source)
        {
            Require(source.published!=null,"Published fork only.");
            // These are privately owned fixed metadata, never returned as mutable DTOs
            // and never modified by commands. No caller-owned package/map is shared.
            definitions=source.definitions;compatibility=source.compatibility;rules=source.rules;cloak=source.cloak;controllers=source.controllers;
            initialTurn=source.initialTurn;generatedMap=source.generatedMap;generatedTemplateHash=source.generatedTemplateHash;astronomyIndex=source.astronomyIndex;
            declaredHostilities=source.declaredHostilities;authoredContactGrids=source.authoredContactGrids;contactFreshness=source.contactFreshness;authoredFleetOwners=source.authoredFleetOwners;
            playableHash=source.playableHash;playableOperationLimit=source.playableOperationLimit;playableOpponent=source.playableOpponent;playableDefenseNode=source.playableDefenseNode;
            integratedHash=source.integratedHash;initialProductionCompleted=source.initialProductionCompleted;constructionTemplates=source.constructionTemplates;
            // Every mutable authority/source/cache-container/organisation/transcript is
            // detached. Playback objects are already immutable snapshot values.
            travel=source.TravelCopy();state=travel.Core;builtShips=source.builtShips.Select(CopyBuild).ToList();published=source.published!.Copy();
            discoveries=source.discoveries.Select(CopyDiscovery).ToList();
            playableNames=new Dictionary<string,string>(source.playableNames,StringComparer.Ordinal);
            playableCreatedOwners=new Dictionary<string,string>(source.playableCreatedOwners,StringComparer.Ordinal);
            playableRetiredFleets.UnionWith(source.playableRetiredFleets);
            playableTranscript.AddRange(source.playableTranscript.Select(CopyPlayableOperation));playableHistory.AddRange(source.playableHistory);
            PublishedTransactionProbe=source.PublishedTransactionProbe;PhysicalPhaseProbe=source.PhysicalPhaseProbe;FinalizationProbe=source.FinalizationProbe;
            publishedExecuting=true;
        }
        private CampaignTacticalReviewSession ForkPublished()
        {
            var copy=new CampaignTacticalReviewSession(this);copy.ValidateTravelSave(copy.travel!);
            return copy;
        }
        private void AdoptPublished(CampaignTacticalReviewSession c)
        {
            // All potentially throwing copies/validation/probes precede these assignments.
            state=c.state;travel=c.travel;builtShips=c.builtShips;published=c.published;discoveries=c.discoveries;
            playableNames=c.playableNames;playableCreatedOwners=c.playableCreatedOwners;
            playableTranscript=c.playableTranscript;playableRetiredFleets=c.playableRetiredFleets;playableHistory=c.playableHistory;
        }
        private CampaignTacticalMutationResult PublishedMutation(Func<CampaignTacticalReviewSession,CampaignTacticalMutationResult> action)
        {
            lock(gate)
            {
                try
                {
                    var candidate=ForkPublished();var result=action(candidate);
                    if(!result.Accepted){LastPhysicalFailure=candidate.LastPhysicalFailure;return Result(result.Error);}
                    PublishedTransactionProbe?.Invoke("after-command");candidate.ValidateTravelSave(candidate.travel!);
                    var facts=candidate.PublishedCapacity(ConstructionJournalOperation.AlreadyStagedBoundary,candidate.playableTranscript.Count);
                    if(!facts.Certified){LastPhysicalFailure=facts.UncertifiableReason;return Result(CampaignTacticalError.Overflow);}
                    PublishedTransactionProbe?.Invoke("before-copy");
                    var ready=candidate.ForkPublished();ready.publishedExecuting=false;
                    PublishedTransactionProbe?.Invoke("after-transcript-copy");PublishedTransactionProbe?.Invoke("after-cache-copy");
                    AdoptPublished(ready);return result;
                }
                catch(PublishedRecordCapacityException){return Result(CampaignTacticalError.Overflow);}
                catch(OverflowException){return Result(CampaignTacticalError.Overflow);}
                catch(ArgumentException){return Result(CampaignTacticalError.UnsupportedSource);}
                catch(InvalidOperationException){return Result(CampaignTacticalError.InvalidBatch);}
            }
        }
        private CampaignFleetOrganizationResult PublishedOrganization(SubmitCampaignFleetOrganizationDto? command)
        {
            CampaignFleetOrganizationResult? value=null;
            var result=PublishedMutation(c=>{value=c.SubmitFleetOrganization(command);return c.Result(value.Accepted?CampaignTacticalError.None:CampaignTacticalError.InvalidOrder);});
            return result.Accepted?value!:value!=null&&!value.Accepted?value:new CampaignFleetOrganizationResult(result.Error==CampaignTacticalError.Overflow?CampaignFleetOrganizationError.Overflow:CampaignFleetOrganizationError.InvalidOrder,state.Campaign.GameState.StateRevision);
        }
        private ConstructionJournalFacts PublishedCapacity(ConstructionJournalOperation operation,long used)=>ConstructionJournalReserve.Evaluate(
            published!.Package,published.Package.PackageHash!,travel!,BuildRegistry.ToList(),published.Records,used,operation,published.RecordBudget);
        private PublishedTacticalSupportResult PublishedFit(GameStateDto game,BlueprintReferenceDto reference,bool currentAccess)
        {
            var record=game.Blueprints!.Single(r=>r.BlueprintId==reference.BlueprintId&&r.Revision==reference.Revision);
            var restored=CampaignBlueprints.Restore(record);
            return PublishedTacticalSupportCompiler.Evaluate(new ShipDesignCatalogueSnapshot(record.FrozenCatalogue!),restored!,
                currentAccess?CampaignBlueprints.Access(definitions,game.Civilizations!.Single(c=>c.CivilizationId==record.OwnerCivilizationId))
                    :CampaignBlueprints.FrozenAccess(record.FrozenCatalogue!),published!.Support);
        }
        private void ValidatePublishedLedger(GameStateDto game)
        {
            var ctx=published!;var combined=BuildRegistry.Select(r=>(r.ShipId,r.QueueItemId,r.FleetIdAtConstruction,r.OwnerCivilizationId))
                .Concat(ctx.Records.Select(r=>(r.ShipId,r.QueueItemId,r.FleetIdAtConstruction,r.OwnerCivilizationId))).ToList();
            Require(combined.Select(r=>r.ShipId).Distinct(StringComparer.Ordinal).Count()==combined.Count
                &&combined.Select(r=>r.QueueItemId).Distinct(StringComparer.Ordinal).Count()==combined.Count
                &&combined.All(r=>!definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Any(p=>p.ShipId==r.ShipId)
                    &&!game.Colonies!.SelectMany(c=>c.ProductionQueue!).Any(q=>q.QueueItemId==r.QueueItemId)),"Mixed source/queue identity overlap.");
            if(combined.Count+game.Colonies!.Sum(c=>c.ProductionQueue!.Count)>ctx.RecordBudget)throw new PublishedRecordCapacityException();
            foreach(var group in combined.GroupBy(r=>r.FleetIdAtConstruction,StringComparer.Ordinal))
            {
                Require(group.Select(r=>r.OwnerCivilizationId).Distinct(StringComparer.Ordinal).Count()==1,"Mixed construction fleet ownership.");
                if(authoredFleetOwners!.ContainsKey(group.Key!)||playableCreatedOwners.ContainsKey(group.Key!))continue;
                Require(new EntityId(group.Key!).TryGetAllocationSequence(out var fleetSequence)&&fleetSequence<game.NextEntitySequence,"Unallocated mixed construction fleet.");
                var first=group.Select(r=>{Require(new EntityId(r.ShipId!).TryGetAllocationSequence(out var n),"Unallocated mixed ship.");return n;}).Min();
                Require(first<ulong.MaxValue&&first+1==fleetSequence,"Mixed fleet/first constructed ship allocation mismatch.");
            }
            var sources=new Dictionary<string,CampaignEquipmentSource>(StringComparer.Ordinal);
            for(int i=0;i<ctx.Records.Count;i+=4096)foreach(var source in PublishedBuiltShipValidation.ValidateRetained(ctx.Records.Skip(i).Take(4096).ToList(),game,ctx.Support,initialTurn))
                sources.Add(source.Equipment.ShipId,source.Equipment);
            ctx.Sources=sources;
        }
        private void ProvisionPublished(CampaignTacticalTravelSaveDto next,GameStateDto before,ShipStateDto ship,ColonyStateDto colony)
        {
            var game=next.Core.Campaign.GameState;var q=colony.ProductionQueue![0];
            var result=PublishedFit(before,q.Blueprint!,false);Require(result.IsSupportedByCompiler,"Unsupported paid tactical source.");var fit=result.Fit!;
            var source=before.Blueprints!.Single(r=>r.BlueprintId==q.Blueprint!.BlueprintId&&r.Revision==q.Blueprint.Revision);
            var record=new CampaignPublishedBuiltShipDto{ShipId=ship.ShipId,BlueprintId=q.Blueprint!.BlueprintId,BlueprintRevision=q.Blueprint.Revision,
                OwnerCivilizationId=ship.OwnerCivilizationId,FleetIdAtConstruction=ship.FleetId,QueueItemId=q.QueueItemId,TurnBuilt=game.TurnNumber,
                SourceCompilerId=source.CompilerVersion,SourceRulesVersion=source.RulesVersion,SourceContentHash=source.SourceContentHash,
                PublicationHash=fit.SourceHash,SupportCatalogueId=fit.SupportCatalogueId,SupportCatalogueRevision=fit.SupportCatalogueRevision,SupportHash=fit.SupportHash,
                SupportCompilerId=fit.CompilerId,FitId=fit.FitId,FitRevision=fit.FitRevision};
            var validated=PublishedBuiltShipValidation.ValidateCompletion(record,before,game,published!.Support,initialTurn);
            published.Records.Add(PublishedBuiltShipValidation.Copy(record));published.Sources.Add(ship.ShipId,validated.Equipment);
            next.Core.Pools.Add(validated.Equipment.InitialPool());PublishedTransactionProbe?.Invoke("after-provision");
        }
    }
}
