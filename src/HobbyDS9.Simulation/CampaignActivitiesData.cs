using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    internal static class CampaignActivitiesData
    {
        internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition,string message){if(!condition)throw new ArgumentException(message);}
        internal static string Identity(params string[] parts)
        {using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream,Encoding.UTF8,true))
         {foreach(var p in parts){Require(p!=null&&p.Length<=65536,"Activity identity length.");var b=Encoding.UTF8.GetBytes(p);w.Write(b.Length);w.Write(b);}w.Flush();
          Require(stream.Length<=1024*1024,"Activity identity budget.");using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(stream.ToArray()).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)));}}
        internal static string ReadyId(ShipClassDraftDto draft)=>"blueprint.ready."+draft.ClassId.Substring(6,draft.ClassId.Length-9);
        internal static Dictionary<string,ActivitySupportedShip> Ready(CampaignActivitiesPackageDto p)
        {
            Require(p.Catalogue!=null&&ShipDesignCatalogueValidation.Validate(p.Catalogue).Count==0&&p.Support!=null&&p.ReadyClasses!=null&&p.ReadyClasses.Count==3,"Three legal activity classes required.");
            Require(p.ReadyClasses!.All(d=>d!=null&&d.ClassId.StartsWith("class.",StringComparison.Ordinal)&&d.ClassId.EndsWith(".v1",StringComparison.Ordinal))
                &&p.ReadyClasses.Select(d=>d.ClassId).Distinct(StringComparer.Ordinal).Count()==3&&p.ReadyClasses.Select(d=>d.HullVersionId).Distinct(StringComparer.Ordinal).Count()==3,"Distinct ready class/hull identities required.");
            var catalogue=new ShipDesignCatalogueSnapshot(p.Catalogue!);var compiler=new ShipDesignCompiler(catalogue);var access=CampaignBlueprints.FrozenAccess(p.Catalogue!);
            var result=new Dictionary<string,ActivitySupportedShip>(StringComparer.Ordinal);
            foreach(var draft in p.ReadyClasses)
            {var publication=compiler.Publish(draft,access,ReadyId(draft),1);Require(publication.Published,"Illegal ready class.");
             var supported=PublishedTacticalSupportCompiler.EvaluateActivities(catalogue,publication.Blueprint!,access,p.Support!);Require(supported.Supported,"Unsupported ready activity class.");result.Add(ReadyId(draft),supported.Ship!);}
            return result;
        }
        private static bool Id(string? id)=>DefinitionId.TryParse(id,out _)&&id!.Length<=128;
        private static bool Text(string? text,int max)=>text!=null&&text.Length<=max;
        internal static void ValidateContent(CampaignActivityContentDto? c)
        {
            Require(c!=null&&Id(c.ContentId)&&c.Revision==1&&c.Structures!=null&&c.Structures.Count==3&&c.Sites!=null&&c.Sites.Count==4
                &&c.Minors!=null&&c.Minors.Count==2&&c.Species!=null&&c.Species.Count==2,"Explicit first activity content required.");
            Require(c!.Structures!.All(s=>s!=null&&Id(s.DefinitionId)&&Text(s.DisplayName,128)&&s.Cost!=null&&s.Cost.Credits>=0&&s.Cost.Industry>0
                &&s.Cost.Resources!=null&&s.Cost.Resources.Count<=8&&s.Cost.Resources.All(r=>r!=null&&Id(r.ResourceDefinitionId)&&r.Amount>=0)
                &&s.Industry>=0&&s.Research>=0&&s.FoodOutput>=0&&s.PowerDemand>=0&&s.PowerDemand<=100),"Invalid structure content.");
            Require(c.Structures.Select(s=>s.DefinitionId).Distinct(StringComparer.Ordinal).Count()==3,"Duplicate structure identity.");
            Require(c.Species!.All(s=>s!=null&&Id(s.SpeciesId)&&Text(s.DisplayName,128))&&c.Species.Select(s=>s.SpeciesId).Distinct(StringComparer.Ordinal).Count()==2,"Distinct minor species required.");
            Require(c.Minors!.All(m=>m!=null&&Id(m.DefinitionId)&&Text(m.DisplayName,128)&&Text(m.SpeciesName,128)&&c.Species.Any(s=>s.SpeciesId==m.SpeciesId)&&m.Encounter!=null)
                &&c.Minors.Select(m=>m.DefinitionId).Distinct(StringComparer.Ordinal).Count()==2,"Invalid minor governments.");
            var sites=c.Sites!.Concat(c.Minors.Select(m=>m.Encounter!)).ToList();
            Require(sites.All(s=>s!=null&&Id(s.DefinitionId)&&Text(s.Title,256)&&Text(s.DiscoveryText,4096)&&s.Choices!=null&&s.Choices.Count>0&&s.Choices.Count<=4&&s.Choices.All(x=>x!=null))
                &&sites.Select(s=>s.DefinitionId).Distinct(StringComparer.Ordinal).Count()==6,"Invalid activity site content.");
            foreach(var s in sites)
            {
                Require(s.Choices!.Select(x=>x.ChoiceId).Distinct(StringComparer.Ordinal).Count()==s.Choices!.Count,"Duplicate choice identity.");
                Require(s.Choices!.All(x=>x!=null&&Id(x.ChoiceId)&&Id(x.FindingId)&&Text(x.Caption,256)&&Text(x.Report,4096)&&Text(x.ContactOutcome,128)
                    &&x.RequiredStudy>=0&&x.RequiredStudy<=4&&x.RequiredContact>=0&&x.RequiredContact<=3&&x.Work>=0&&x.Work<=12
                    &&x.Deadline>=0&&x.Deadline<=3&&(x.Work==0||x.Deadline>0)&&x.Credits>=0&&x.Credits<=1000
                    &&ScienceRules.ValidDiscipline(x.Discipline)&&x.Insight>=0&&x.Insight<=1000&&x.HullRiskBps>=0&&x.HullRiskBps<=10000
                    &&x.HullDamage>=0&&x.HullDamage<=10&&x.ConsentChanceBps>=0&&x.ConsentChanceBps<=10000
                    &&(!x.RequiredRelation.HasValue||Enum.IsDefined(typeof(ActivityMinorRelation),x.RequiredRelation.Value))
                    &&(!x.AcceptedRelation.HasValue||Enum.IsDefined(typeof(ActivityMinorRelation),x.AcceptedRelation.Value))
                    &&(!x.RefusedRelation.HasValue||Enum.IsDefined(typeof(ActivityMinorRelation),x.RefusedRelation.Value))),"Invalid choice rule.");
            }
        }
        internal static string PackageHash(CampaignActivitiesPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==1&&p.ProfileId==CampaignActivitiesIdentity.PackageProfile&&p.BaseTemplate!=null
                &&p.BaseTemplate.ReplayOperationLimit>0&&p.BaseTemplate.ReplayOperationLimit<=65536&&p.RecordBudget>=32&&p.RecordBudget<=65536,"Invalid activity package.");
            ValidateContent(p!.Content);var ready=Ready(p);
            var parts=new List<string>{CampaignActivitiesIdentity.Profile,CampaignActivitiesIdentity.Policy,"activity-placement-frontier-v2",p.BaseTemplate!.PackageHash!,
                p.BaseTemplate.ReplayOperationLimit!.Value.ToString(CultureInfo.InvariantCulture),p.RecordBudget.ToString(CultureInfo.InvariantCulture),
                PublishedTacticalSupportEncoding.ActivityCatalogue(new ShipDesignCatalogueSnapshot(p.Catalogue!)),p.Content!.ContentId,p.Content.Revision.ToString(CultureInfo.InvariantCulture)};
            foreach(var pair in ready.OrderBy(x=>x.Key,StringComparer.Ordinal)){parts.Add(pair.Key);parts.Add(pair.Value.SupportHash);parts.Add(pair.Value.Tactical.SourceHash);}
            using(var bytes=new MemoryStream())using(var w=new BinaryWriter(bytes,Encoding.UTF8,true))
            {
                void S(string s){var data=Encoding.UTF8.GetBytes(s);w.Write(data.Length);w.Write(data);}
                foreach(var s in p.Content.Structures!.OrderBy(x=>x.DefinitionId,StringComparer.Ordinal))
                {S(s.DefinitionId);S(s.DisplayName);w.Write(s.Cost!.Credits);w.Write(s.Cost.Industry);w.Write(s.Cost.Maintenance);w.Write(s.Cost.Resources!.Count);
                 foreach(var r in s.Cost.Resources.OrderBy(x=>x.ResourceDefinitionId,StringComparer.Ordinal)){S(r.ResourceDefinitionId);w.Write(r.Amount);}
                 w.Write(s.Industry);w.Write(s.Research);w.Write(s.FoodOutput);w.Write(s.PowerDemand);}
                foreach(var s in p.Content.Sites!.Concat(p.Content.Minors!.Select(m=>m.Encounter!)).OrderBy(x=>x.DefinitionId,StringComparer.Ordinal))
                {S(s.DefinitionId);S(s.Title);S(s.DiscoveryText);w.Write(s.Choices!.Count);
                 foreach(var c in s.Choices.OrderBy(x=>x.ChoiceId,StringComparer.Ordinal))
                 {S(c.ChoiceId);S(c.FindingId);S(c.Caption);S(c.Report);w.Write(c.RequiredStudy);w.Write(c.RequiredContact);w.Write(c.Work);w.Write(c.Deadline);w.Write(c.Credits);
                  w.Write((int)c.Discipline);w.Write(c.Insight);w.Write(c.HullRiskBps);w.Write(c.HullDamage);S(c.ContactOutcome);
                  w.Write(c.RequiredRelation.HasValue);if(c.RequiredRelation.HasValue)w.Write((int)c.RequiredRelation.Value);
                  w.Write(c.AcceptedRelation.HasValue);if(c.AcceptedRelation.HasValue)w.Write((int)c.AcceptedRelation.Value);
                  w.Write(c.RefusedRelation.HasValue);if(c.RefusedRelation.HasValue)w.Write((int)c.RefusedRelation.Value);w.Write(c.ConsentChanceBps);}}
                foreach(var m in p.Content.Minors.OrderBy(x=>x.DefinitionId,StringComparer.Ordinal)){S(m.DefinitionId);S(m.DisplayName);S(m.SpeciesId);S(m.SpeciesName);w.Write(m.WarpCapable);S(m.Encounter!.DefinitionId);}
                foreach(var s in p.Content.Species!.OrderBy(x=>x.SpeciesId,StringComparer.Ordinal)){S(s.SpeciesId);S(s.DisplayName);}
                w.Flush();Require(bytes.Length<=128*1024,"Activity content encoding budget.");parts.Add(Convert.ToBase64String(bytes.ToArray()));
            }
            return Identity(parts.ToArray());
        }
        internal static CampaignActivityContentDto Copy(CampaignActivityContentDto c)=>new CampaignActivityContentDto{ContentId=c.ContentId,Revision=c.Revision,
            Structures=c.Structures!.Select(s=>new ActivityStructureDefinitionDto{DefinitionId=s.DefinitionId,DisplayName=s.DisplayName,Cost=BlueprintCopies.Copy(s.Cost!),Industry=s.Industry,Research=s.Research,FoodOutput=s.FoodOutput,PowerDemand=s.PowerDemand}).ToList(),
            Sites=c.Sites!.Select(Copy).ToList(),Minors=c.Minors!.Select(m=>new ActivityMinorDefinitionDto{DefinitionId=m.DefinitionId,DisplayName=m.DisplayName,SpeciesId=m.SpeciesId,SpeciesName=m.SpeciesName,WarpCapable=m.WarpCapable,Encounter=Copy(m.Encounter!)}).ToList(),
            Species=c.Species!.Select(s=>new ActivitySpeciesDto{SpeciesId=s.SpeciesId,DisplayName=s.DisplayName}).ToList()};
        internal static ActivitySiteDefinitionDto Copy(ActivitySiteDefinitionDto s)=>new ActivitySiteDefinitionDto{DefinitionId=s.DefinitionId,Title=s.Title,DiscoveryText=s.DiscoveryText,Choices=s.Choices!.Select(c=>new ActivityChoiceDefinitionDto{
            ChoiceId=c.ChoiceId,FindingId=c.FindingId,Caption=c.Caption,Report=c.Report,RequiredStudy=c.RequiredStudy,RequiredContact=c.RequiredContact,Work=c.Work,Deadline=c.Deadline,Credits=c.Credits,Discipline=c.Discipline,Insight=c.Insight,
            HullRiskBps=c.HullRiskBps,HullDamage=c.HullDamage,ContactOutcome=c.ContactOutcome,RequiredRelation=c.RequiredRelation,AcceptedRelation=c.AcceptedRelation,RefusedRelation=c.RefusedRelation,ConsentChanceBps=c.ConsentChanceBps}).ToList()};
        internal static CampaignActivityCommandDto Copy(CampaignActivityCommandDto c)=>new CampaignActivityCommandDto{ActorId=c.ActorId,CommandId=c.CommandId,ExpectedTurn=c.ExpectedTurn,ExpectedRevision=c.ExpectedRevision,Kind=c.Kind,TargetId=c.TargetId,DefinitionId=c.DefinitionId,BlueprintRevision=c.BlueprintRevision,ShipId=c.ShipId};
        internal static CampaignActivityStateDto Copy(CampaignActivityStateDto s)=>new CampaignActivityStateDto{
            Observations=s.Observations!.Select(o=>new ActivitySiteObservationDto{ActorId=o.ActorId,SiteId=o.SiteId,FirstObservedTurn=o.FirstObservedTurn}).ToList(),
            Production=s.Production!.Select(q=>new ActivityProductionItemDto{ColonyId=q.ColonyId,Kind=q.Kind,DefinitionId=q.DefinitionId,Paid=Copy(q.Paid!)}).ToList(),
            Structures=s.Structures!.Select(r=>new ActivityStructureReceiptDto{QueueItemId=r.QueueItemId,ColonyId=r.ColonyId,DefinitionId=r.DefinitionId,OwnerId=r.OwnerId,CompletedTurn=r.CompletedTurn}).ToList(),
            Sites=s.Sites!.Select(r=>new ActivitySiteStateDto{SiteId=r.SiteId,DefinitionId=r.DefinitionId,NodeId=r.NodeId,SystemId=r.SystemId,BodyId=r.BodyId,MinorActorId=r.MinorActorId}).ToList(),
            Minors=s.Minors!.Select(m=>new ActivityMinorStateDto{ActorId=m.ActorId,DefinitionId=m.DefinitionId,BodyId=m.BodyId,Population=m.Population,SpeciesId=m.SpeciesId}).ToList(),
            Bodies=s.Bodies!.Select(b=>new ActivityBodyDto{BodyId=b.BodyId,SystemId=b.SystemId,DisplayName=b.DisplayName,GovernmentActorId=b.GovernmentActorId}).ToList(),
            Relations=s.Relations!.Select(r=>new ActivityMinorRelationDto{ActorId=r.ActorId,MinorActorId=r.MinorActorId,Relation=r.Relation,ChangedTurn=r.ChangedTurn}).ToList(),
            Findings=s.Findings!.Select(f=>new ActivityFindingDto{ActorId=f.ActorId,SiteId=f.SiteId,ChoiceId=f.ChoiceId,FindingId=f.FindingId,FindingBudget=f.FindingBudget,ShipId=f.ShipId,FleetId=f.FleetId,Stage=f.Stage,WorkDone=f.WorkDone,RemainingAdvances=f.RemainingAdvances,StartedTurn=f.StartedTurn,ResolvedTurn=f.ResolvedTurn,PaidCredits=f.PaidCredits,InsightApplied=f.InsightApplied,InsightUnlearnable=f.InsightUnlearnable,ResearchReceipts=f.ResearchReceipts!.Select(r=>new ResearchReceiptDto{CivilizationId=r.CivilizationId,Category=r.Category,Discipline=r.Discipline,IntervalStartLevel=r.IntervalStartLevel,BreakthroughOrdinal=r.BreakthroughOrdinal,ApplicationDefinitionId=r.ApplicationDefinitionId,Amount=r.Amount}).ToList(),Report=f.Report,ContactOutcome=f.ContactOutcome}).ToList()};
        internal static ProductionQueueItemDto Copy(ProductionQueueItemDto q)=>new ProductionQueueItemDto{QueueItemId=q.QueueItemId,ShipDesignDefinitionId=q.ShipDesignDefinitionId,
            Blueprint=q.Blueprint==null?null:BlueprintCopies.Copy(q.Blueprint),RequiredIndustry=q.RequiredIndustry,CompletedIndustry=q.CompletedIndustry,PaidCredits=q.PaidCredits,PaidResources=q.PaidResources!.Select(PopulationData.Copy).ToList()};
    }
}
