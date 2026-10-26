#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    public sealed partial class GeneratedGalaxyEngineTests
    {
        private sealed class ColonyHold
        {
            internal readonly ManualResetEventSlim Entered=new ManualResetEventSlim(),Release=new ManualResetEventSlim();
            internal ColonisationInteractionWork? Work;internal CheckpointContext? Context;internal StreamedContinuationHold? Continuation;
            internal int Hits,Thread;internal long Epoch;
        }
        private sealed class ColonyCase
        {
            internal bool RosterV2;
            internal string MenuPrefix=>RosterV2?"colonisation-roster-campaign":"colonisation-campaign";
            internal string SlotPrefix=>RosterV2?"unity-campaign-colonisation-v2-":"unity-campaign-colonisation-v1-";
            internal readonly PublishedReceivingState Shared=new PublishedReceivingState();
            internal readonly List<ColonyHold> Holds=new List<ColonyHold>();internal readonly List<PhysicalReviewHost> Hosts=new List<PhysicalReviewHost>();
            internal readonly List<CheckpointContext> Contexts=new List<CheckpointContext>();
            internal string Home="",Yard="",HomeSystem="",HomeNode="",Explorer="",Escort="",Transport="",SecondTransport="",TargetPlanet="",TargetNode="",TargetWorld="";
            internal byte[]? Founding,Established;internal string? FoundingMission,EstablishedMission;
            internal string[] ManufacturedOrders=Array.Empty<string>();
            internal bool PriorDirectoryCaptured,SurveyCancelled,SurveyCaptured,LandingCompleted;
            internal int SurveyAttempts;
            internal readonly HashSet<string> SurveyVisited=new HashSet<string>(StringComparer.Ordinal),AttemptedPlanets=new HashSet<string>(StringComparer.Ordinal),AttemptedNodes=new HashSet<string>(StringComparer.Ordinal);
            internal bool Pending=>Contexts.Any(c=>Volatile.Read(ref c.Finished)==0)||Holds.Any(h=>h.Work!=null&&(!h.Work.Settled||h.Context!=null&&Volatile.Read(ref h.Context.Finished)==0));
            internal Exception? ReleaseAll(){foreach(var h in Holds)h.Release.Set();Exception? error=null;foreach(var h in Holds)try{h.Continuation?.Forward();}catch(Exception e){error=error??e;}return error;}
        }
        private static ReviewUiModel ColonyModel(ColonyCase s)=>CampaignModel(s.Shared.Entry!);
        // Root access is audit ONLY. No command, route, source/destination ID or target selection uses it.
        private static CampaignColonisationSaveDto ColonyAudit(ReviewUiModel m)=>( (ICampaignColonisationSession)typeof(PhysicalReviewHost).GetField("session",HullPrivate)!.GetValue(StreamHost(m))!).ExportColonisationSave();
        private static byte[] ColonyBytes(ReviewUiModel m)=>new ReviewNewtonsoftCodec().EncodeColonisationRoot(ColonyAudit(m));
        private static GameStateDto ColonyGame(CampaignColonisationSaveDto root)=>root.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
        private static void ColonyProbe(PhysicalReviewHost host,Action<string>? probe)=>typeof(PhysicalReviewHost).GetProperty("ColonisationInteractionProbe",HullPrivate)!.SetValue(host,probe);
        private static bool ColonyPending(ReviewEntry entry)=>(bool)CheckpointField(entry,"generatedCampaignBusy")!||CampaignModel(entry).Busy||CheckpointField(entry,"checkpointUiOperation")!=null||CheckpointField(entry,"colonisationUiWork")!=null;
        private static string ColonySelection(ReviewColonisationPanel? panel)
        {if(panel==null)return "none";var selected=typeof(ReviewColonisationPanel).GetMethod("CaptureSelection",HullPrivate)!.Invoke(panel,null);return selected==null?"none":JsonConvert.SerializeObject(selected.GetType().GetFields(HullPrivate).Where(f=>f.Name!="Science").ToDictionary(f=>f.Name,f=>f.GetValue(selected)));}
        private static string ColonyDraft(UIDocument doc)
        {var food=doc.rootVisualElement.Q<IntegerField>("colonisation-food");return food==null?"none":JsonConvert.SerializeObject(new{food=food.value,cohort=doc.rootVisualElement.Q<DropdownField>("colonisation-cohort")?.value,materials=doc.rootVisualElement.Query<IntegerField>().ToList().Where(f=>f.name.StartsWith("colonisation-material-",StringComparison.Ordinal)).OrderBy(f=>f.name,StringComparer.Ordinal).ToDictionary(f=>f.name,f=>f.value)});}
        private static IEnumerator ColonyIdle(ColonyCase s,string phase)
        {var clock=System.Diagnostics.Stopwatch.StartNew();int frames=0;while(ColonyPending(s.Shared.Entry!)){Assert.That(clock.Elapsed.TotalSeconds,Is.LessThan(90),phase);frames++;yield return null;}s.Shared.Timings.Add(new{phase,observedWallMs=clock.Elapsed.TotalMilliseconds,yieldedFrames=frames,notFps=true});yield return null;yield return null;}
        private static IEnumerator ColonyReach(UIDocument doc,VisualElement item,bool enabled=true,string target="actor control")
        {
            Assert.That(item,Is.Not.Null,target+" exists on the CURRENT view");var parents=new List<ScrollView>();for(var at=item.parent;at!=null;at=at.parent)if(at is ScrollView v)parents.Add(v);
            foreach(var v in parents){v.ScrollTo(item);yield return null;yield return null;}
            Assert.That(item.panel,Is.SameAs(doc.rootVisualElement.panel));if(enabled)Assert.That(item.enabledInHierarchy,Is.True,item.name);
            var hit=doc.rootVisualElement.panel.Pick(item.worldBound.center);Assert.That(hit==item||hit!=null&&item.Contains(hit),Is.True,item.name+" actual pick");
            foreach(var v in parents)PublishedReceivingContained(v.contentViewport.worldBound,item,item.name);
            Assert.That(item.worldBound.yMax,Is.LessThanOrEqualTo(doc.rootVisualElement.Q("player-footer").worldBound.yMin));
        }
        private static IEnumerator ColonyClick(ColonyCase s,UIDocument doc,string id)
        {var b=doc.rootVisualElement.Q<Button>(id);yield return ColonyReach(doc,b,target:id);var clock=System.Diagnostics.Stopwatch.StartNew();ColonyActivate(s,()=>Click(b));s.Shared.Timings.Add(new{phase="activate-"+id,owningThreadMs=clock.Elapsed.TotalMilliseconds,notTotalLatency=true});yield return ColonyIdle(s,id);if(id=="colonisation-order-confirm")Assert.That(doc.rootVisualElement.Q<Button>(id),Is.Null,"accepted/rejected submission discards the prior review");}
        private static void ColonyActivate(ColonyCase s,Action activate)
        {
            object?[] Operations()=>new[]{CheckpointField(s.Shared.Entry!,"colonisationUiWork"),CheckpointField(s.Shared.Entry!,"streamedUiWork"),CheckpointField(s.Shared.Entry!,"checkpointUiOperation"),CheckpointField(s.Shared.Entry!,"generatedCampaignOperationToken")};
            var before=Operations();
            var prior=SynchronizationContext.Current;Assert.That(prior,Is.Not.Null);var context=new CheckpointContext(prior!);SynchronizationContext.SetSynchronizationContext(context);
            try{activate();}finally{SynchronizationContext.SetSynchronizationContext(prior);}
            var after=Operations();
            if(after.Where((operation,index)=>operation!=null&&!ReferenceEquals(operation,before[index])).Any())s.Contexts.Add(context);
            else{context.Inline=true;context.Finished=1;} // No new async owner; an older held UI field is not this activation's continuation.
        }
        private static IEnumerator ColonyPage(ColonyCase s,UIDocument doc,string tab)
        {CampaignPage(doc,ReviewWorkspacePage.Production);yield return null;yield return null;yield return ColonyClick(s,doc,"colonisation-tab-"+tab);}
        private static IEnumerator ColonyPick<T>(UIDocument doc,string id,IReadOnlyList<T> rows,Func<T,string> key,string selected)
        {var field=doc.rootVisualElement.Q<DropdownField>(id);Assert.That(field,Is.Not.Null,id);int index=rows.ToList().FindIndex(x=>key(x)==selected);Assert.That(index,Is.GreaterThanOrEqualTo(0),id+" actor-observed selection");yield return ColonyReach(doc,field);field.index=index;yield return null;yield return null;}
        private static IEnumerator ColonyReview(ColonyCase s,UIDocument doc,string id,bool available=true)
        {var before=ColonyBytes(ColonyModel(s));yield return ColonyClick(s,doc,id);Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(before),"preview is pure");Assert.That(doc.rootVisualElement.Q<Button>("colonisation-order-confirm")!=null,Is.EqualTo(available));if(!available){Assert.That(doc.rootVisualElement.Q("colonisation-order-review").Query<Label>().ToList().Any(l=>l.text.Contains("unavailable",StringComparison.OrdinalIgnoreCase)||l.text.Contains("outside",StringComparison.OrdinalIgnoreCase)),Is.True);yield return ColonyClick(s,doc,"colonisation-order-back");}}
        private static IEnumerator ColonyOrder(ColonyCase s,UIDocument doc,string id)
        {yield return ColonyReview(s,doc,id);yield return ColonyClick(s,doc,"colonisation-order-confirm");}
        private static int ColonyBuildIndex(ReviewUiModel m,string definition)=>m.ColonisationActivities().BuildOptions.FindIndex(x=>x.DefinitionId==definition&&x.BlueprintRevision<=1);
        private static IEnumerator ColonyResolveCombat(ColonyCase s,UIDocument doc)
        {
            int encounters=0;while(ColonyModel(s).Current.Frame.Status.Phase==SessionPhase.AwaitingCombat)
            {
                Assert.That(++encounters,Is.LessThanOrEqualTo(8));CampaignPage(doc,ReviewWorkspacePage.Combat);yield return null;yield return null;
                int rounds=0;while(!ColonyModel(s).Current.Frame.Battle!.Complete)
                {Assert.That(++rounds,Is.LessThanOrEqualTo(16));int round=ColonyModel(s).Current.Frame.Battle!.Round;Click(CampaignButton(doc,"Review / commit round "+round));Confirm(doc);yield return ColonyIdle(s,"actual-colony-combat-round");Click(doc.rootVisualElement.Q<Button>("combat-skip-pending-playback"));yield return null;yield return null;if(!ColonyModel(s).Current.Frame.Battle!.Complete){Click(CampaignButton(doc,"Plan next round"));yield return null;}}
                Click(CampaignButton(doc,"Confirm campaign aftermath"));Confirm(doc);yield return ColonyIdle(s,"actual-colony-aftermath");
            }
        }
        private static IEnumerator ColonyAdvance(ColonyCase s,UIDocument doc)
        {yield return ColonyResolveCombat(s,doc);CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);int turn=ColonyModel(s).Current.Frame.Status.Turn;ColonyActivate(s,()=>{Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);});yield return ColonyIdle(s,"colony-advance");yield return ColonyResolveCombat(s,doc);Assert.That(ColonyModel(s).Current.Frame.Status.Turn,Is.EqualTo(turn+1),"settlement and aftermath share ONE week");}
        private static IEnumerator ColonyMove(ColonyCase s,UIDocument doc,string ship,string node)
        {
            var own=ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==ship);if(own.NodeId==node&&own.Stationary)yield break;
            var route=ColonyModel(s).Current.FindKnownRoute(own.FleetId,node);Assert.That(route?.Route.Success,Is.True,"one selected actor-known route");
            yield return StreamRoute(doc,s.Shared.Entry!,own.FleetId,node);for(int i=0;i<24&&!ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==ship).Stationary;i++)yield return ColonyAdvance(s,doc);
            own=ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==ship);Assert.That(own.NodeId,Is.EqualTo(node));Assert.That(own.Stationary,Is.True);
        }
        private static IEnumerator ColonyJoinEscort(ColonyCase s,UIDocument doc)
        {
            var wanted=new[]{s.Explorer,s.Escort,s.Transport,s.SecondTransport};
            foreach(var id in wanted)yield return ColonyMove(s,doc,id,s.HomeNode);
            string Fleet(string id)=>ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==id).FleetId;
            var target=Fleet(s.Explorer);var transfer=wanted.Where(id=>Fleet(id)!=target).ToArray();
            if(transfer.Length>0)
            {
                foreach(var fleet in wanted.Select(Fleet).Distinct(StringComparer.Ordinal).ToArray())
                {
                    CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet));yield return V3Settled(doc);
                    CampaignPage(doc,ReviewWorkspacePage.FleetOrders);yield return null;yield return null;var clear=doc.rootVisualElement.Q<Button>("fleet-clear-route");yield return ColonyReach(doc,clear,target:"normal fleet Remain");
                    ColonyActivate(s,()=>{Click(clear);Confirm(doc);});yield return ColonyIdle(s,"normal-colocated-fleet-remain");
                }
                CampaignPage(doc,ReviewWorkspacePage.FleetOrders);yield return null;yield return null;
                foreach(var id in transfer)
                {
                    if(doc.rootVisualElement.Q<Toggle>("fleet-member-"+id)==null){Click(doc.rootVisualElement.Q<Button>("fleet-roster-"+Fleet(id)));yield return null;yield return null;}
                    var member=doc.rootVisualElement.Q<Toggle>("fleet-member-"+id);yield return ColonyReach(doc,member,target:"own transfer member");member.value=true;yield return null;yield return null;
                }
                var button=doc.rootVisualElement.Q<Button>("fleet-transfer-"+target);yield return ColonyReach(doc,button,target:"normal own colocated transfer");
                ColonyActivate(s,()=>{Click(button);Confirm(doc);});yield return ColonyIdle(s,"normal-colocated-escort-transfer");
            }
            Assert.That(wanted.All(id=>Fleet(id)==target),Is.True,"actual normal completion autojoin or accepted UI transfer joins the own escort and transports");
            Assert.That(ColonyModel(s).Current.Frame.OwnShips.Single(x=>x.Condition.ShipId==s.Escort).Condition.Hull,Is.GreaterThan(0));
            s.Shared.Timings.Add(new{phase="actual-own-escort-assembly",targetFleet=target,ships=wanted,ordinaryTransferExecuted=transfer.Length>0});
        }
        private static IEnumerator ColonyCover(ColonyCase s,UIDocument doc,string phase)
        {
            var own=ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==s.Explorer);var fleet=own.FleetId;
            Assert.That(own.Stationary,Is.True);Assert.That(ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==s.Escort).FleetId,Is.EqualTo(fleet));
            Assert.That(ColonyModel(s).Current.Frame.OwnShips.Single(x=>x.Condition.ShipId==s.Escort).Condition.Hull,Is.GreaterThan(0));
            var historyBefore=ColonyModel(s).History().Count;var before=ColonyModel(s).Current.Frame.Status;
            CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet));yield return V3Settled(doc);
            CampaignPage(doc,ReviewWorkspacePage.FleetOrders);yield return null;yield return null;
            var engage=doc.rootVisualElement.Q<Button>("player-stay-engage");yield return ColonyReach(doc,engage,target:"public standing own StayEngage");
            ColonyActivate(s,()=>{Click(engage);Confirm(doc);});yield return ColonyIdle(s,phase+"-standing-order");
            yield return ColonyAdvance(s,doc);
            Assert.That(ColonyModel(s).Current.Frame.Status.Turn,Is.EqualTo(before.Turn+1));
            s.Shared.Timings.Add(new{phase,fleet,ownNode=own.NodeId,normalAdvance=true,actualCompletedEncounterDelta=ColonyModel(s).History().Count-historyBefore,qualification="A standing order and ordinary week; zero encounters is not a battle-win claim."});
        }
        private static HashSet<string> ColonyObservedRisk(ColonyCase s,string phase,string? chosen=null)
        {
            var model=ColonyModel(s);var map=model.GeneratedMap();
            var contacts=model.Current.Frame.Travel!.KnownContacts.OrderBy(c=>c.FleetId,StringComparer.Ordinal).ToArray();
            var seen=new HashSet<string>(contacts.Select(c=>c.LastSeenNodeId),StringComparer.Ordinal);
            var excluded=new HashSet<string>(seen,StringComparer.Ordinal);
            foreach(var edge in map.Connections){if(seen.Contains(edge.EndpointA))excluded.Add(edge.EndpointB);if(seen.Contains(edge.EndpointB))excluded.Add(edge.EndpointA);}
            excluded.UnionWith(s.AttemptedNodes);
            s.Shared.Timings.Add(new{phase="observed-contact-risk",check=phase,turn=model.Current.Frame.Status.Turn,
                contacts=contacts.Select(c=>new{c.FleetId,c.LastSeenNodeId,c.LastSeenTurn,c.Fresh}).ToArray(),
                excluded=excluded.OrderBy(x=>x,StringComparer.Ordinal).ToArray(),ownAttemptedNodes=s.AttemptedNodes.OrderBy(x=>x,StringComparer.Ordinal).ToArray(),ownAttemptedPlanets=s.AttemptedPlanets.OrderBy(x=>x,StringComparer.Ordinal).ToArray(),chosen,
                qualification="Conservative last-seen contact adjacency and own failed-attempt exclusion; not enemy destination knowledge or a safety guarantee."});
            return excluded;
        }
        private static string ColonyNextKnown(CampaignGeneratedMapDto map,string origin,HashSet<string> visited,HashSet<string> excluded)
        {
            // One BFS over the ACTOR projection; no private map and no per-node full route recompilation.
            var distance=new Dictionary<string,int>(StringComparer.Ordinal){{origin,0}};var queue=new Queue<string>();queue.Enqueue(origin);
            while(queue.Count>0){var n=queue.Dequeue();foreach(var edge in map.Connections.Where(c=>c.EndpointA==n||c.EndpointB==n)){var next=edge.EndpointA==n?edge.EndpointB:edge.EndpointA;if(distance.ContainsKey(next))continue;distance.Add(next,distance[n]+1);queue.Enqueue(next);}}
            var candidates=map.Regions.Where(r=>!visited.Contains(r.NodeId)&&!excluded.Contains(r.NodeId)&&distance.ContainsKey(r.NodeId)).OrderBy(r=>r.SystemId==null?1:0).ThenBy(r=>distance[r.NodeId]).ThenBy(r=>r.NodeId,StringComparer.Ordinal).ToList();Assert.That(candidates,Is.Not.Empty,"an unexcluded lawful known exploration frontier exists; no risky fallback");return candidates[0].NodeId;
        }
        private static ColonyHold ColonyStartHold(ColonyCase s,Action activate,bool continuation=false)
        {
            var h=new ColonyHold();s.Holds.Add(h);var host=StreamHost(ColonyModel(s));if(!s.Hosts.Contains(host))s.Hosts.Add(host);
            ColonyProbe(host,p=>{if(p!="before-action")return;Interlocked.Increment(ref h.Hits);h.Thread=Thread.CurrentThread.ManagedThreadId;h.Entered.Set();if(!h.Release.Wait(TimeSpan.FromSeconds(90)))throw new TimeoutException("Colonisation receiving hold");});
            var prior=SynchronizationContext.Current;Assert.That(prior,Is.Not.Null);h.Context=new CheckpointContext(prior!);if(continuation){h.Continuation=new StreamedContinuationHold(h.Context);SynchronizationContext.SetSynchronizationContext(h.Continuation);}else SynchronizationContext.SetSynchronizationContext(h.Context);
            try{activate();}finally{SynchronizationContext.SetSynchronizationContext(prior);}h.Epoch=(long)CheckpointField(s.Shared.Entry!,"viewEpoch")!;h.Work=(ColonisationInteractionWork?)CheckpointField(s.Shared.Entry!,"colonisationUiWork");Assert.That(h.Work,Is.Not.Null);return h;
        }
        private static IEnumerator ColonyEntered(ColonyHold h)
        {var clock=System.Diagnostics.Stopwatch.StartNew();while(!h.Entered.IsSet){Assert.That(clock.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}Assert.That(h.Hits,Is.EqualTo(1));Assert.That(h.Thread,Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId));}
        private static IEnumerator ColonyBusy(ColonyCase s,UIDocument doc,ColonyHold h,byte[] before,ReviewFrame frame)
        {
            CheckpointBusy(doc,h.Work!.Kind==ColonisationInteractionKind.Preview?"Checking":h.Work.Kind==ColonisationInteractionKind.Submission?"Applying":"Advancing");int ticks=0;var positions=new HashSet<float>();var heartbeat=doc.rootVisualElement.schedule.Execute(()=>ticks++).Every(10);var clock=System.Diagnostics.Stopwatch.StartNew();
            try{while(clock.Elapsed.TotalSeconds<.3){positions.Add(doc.rootVisualElement.Q("campaign-operation-indicator").style.left.value.value);yield return null;}Assert.That(ticks,Is.GreaterThanOrEqualTo(3));Assert.That(positions.Count,Is.GreaterThan(1));Assert.That(ColonyModel(s).Current.Frame,Is.SameAs(frame));Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(before),"audit only after source fork has released its lock");Assert.That((long)CheckpointField(s.Shared.Entry!,"viewEpoch")!,Is.EqualTo(h.Epoch));var page=s.Shared.Entry!.Workspace!.Page;Click(doc.rootVisualElement.Q<Button>("review-nav-Map"));Assert.That(s.Shared.Entry.Workspace.Page,Is.EqualTo(page));Assert.That(ColonyModel(s).BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance),Is.Null);}finally{heartbeat.Pause();}
        }
        private static IEnumerator ColonySave(ColonyCase s,UIDocument doc,string label,int slot=1)
        {
            var expected=ColonyBytes(ColonyModel(s));CampaignPage(doc,ReviewWorkspacePage.Saves);yield return null;Click(doc.rootVisualElement.Q<Button>("save-slot-generated-"+slot));ColonyActivate(s,()=>{Click(doc.rootVisualElement.Q<Button>("player-save"));if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});yield return ColonyIdle(s,label+"-save");
            Assert.That(File.ReadAllBytes(Path.Combine(directoryFor(s),s.SlotPrefix+"generated-"+slot+".json")),Is.EqualTo(expected));File.WriteAllBytes(Path.Combine(CampaignOutput(),"colony-"+label+"-saved-private-audit.json"),expected);
        }
        private static string directoryFor(ColonyCase s)=>(string)CheckpointField(s.Shared.Entry!,"generatedCampaignTestDirectory")!;
        private static IEnumerator ColonyContinue(ColonyCase s,UIDocument doc,string label,byte[] expected,int slot=1,string? mission=null)
        {
            var old=ColonyModel(s);Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;yield return null;doc.rootVisualElement.Q<Foldout>(s.MenuPrefix+"-menu").value=true;yield return null;
            ColonyActivate(s,()=>{Click(doc.rootVisualElement.Q<Button>(s.MenuPrefix+"-continue-generated-"+slot));Confirm(doc);});yield return ColonyIdle(s,label+"-continue");var current=ColonyModel(s);Assert.That(current,Is.Not.SameAs(old));Assert.That(current.ColonisationCampaign,Is.True);Assert.That(current.ColonisationRosterCampaign,Is.EqualTo(s.RosterV2));Assert.That(ColonyBytes(current),Is.EqualTo(expected));if(mission!=null)Assert.That(JsonConvert.SerializeObject(current.FrontierCommission()),Is.EqualTo(mission));
            Assert.That(doc.rootVisualElement.Q<Button>("colonisation-order-confirm"),Is.Null,"new controller never inherits an earlier review");Assert.That(doc.rootVisualElement.Q<Button>("activity-order-confirm"),Is.Null);
        }
        private static IEnumerator ColonySaveContinue(ColonyCase s,UIDocument doc,string label,int slot=1)
        {var expected=ColonyBytes(ColonyModel(s));var mission=JsonConvert.SerializeObject(ColonyModel(s).FrontierCommission());yield return ColonySave(s,doc,label,slot);yield return ColonyContinue(s,doc,label,expected,slot,mission);}
        private static IEnumerator ColonyWorld(ColonyCase s,UIDocument doc,string world,string tab="Worlds")
        {yield return ColonyPage(s,doc,tab);yield return ColonyPick(doc,tab=="Worlds"?"colonisation-world-selector":"colonisation-transport-world",ColonyModel(s).Colonisation().Worlds,x=>x.Id,world);}
        private static IEnumerator ColonyTransport(ColonyCase s,UIDocument doc,string world,string? ship=null)
        {yield return ColonyWorld(s,doc,world,"Transports");yield return ColonyPick(doc,"colonisation-transport-selector",ColonyModel(s).Colonisation().Transports,x=>x.ShipId,ship??s.Transport);}
        private static IEnumerator ColonyCapture(ColonyCase s,UIDocument doc,Vector2Int size,string filename,bool inspector=false)
        {
            var source=ColonyBytes(ColonyModel(s));var selected=ColonySelection(doc.rootVisualElement.Q<ReviewColonisationPanel>());
            var draft=ColonyDraft(doc);
            var oldConfirm=doc.rootVisualElement.Q<Button>("colonisation-order-confirm");
            var prior=doc.panelSettings.targetTexture;var width=doc.rootVisualElement.style.width;var height=doc.rootVisualElement.style.height;var active=RenderTexture.active;var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Texture2D? image=null;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;yield return null;yield return null;yield return null;
                var panel=doc.rootVisualElement.Q<ReviewColonisationPanel>();var scroll=panel?.Q<ScrollView>("colonisation-scroll");var item=inspector?panel?.Q("colonisation-hull-image"):panel?.Q("colonisation-world-facts");if(scroll!=null&&item!=null){scroll.ScrollTo(item);yield return null;yield return null;}
                RenderTexture.active=target;image=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(Path.Combine(CampaignOutput(),filename),image.EncodeToPNG());
                File.WriteAllText(Path.Combine(CampaignOutput(),filename.Replace(".png",".layout.json")),JsonConvert.SerializeObject(new{root=doc.rootVisualElement.worldBound.ToString(),footer=doc.rootVisualElement.Q("player-footer")?.worldBound.ToString(),panel=panel?.worldBound.ToString(),viewport=scroll?.contentViewport.worldBound.ToString(),scroll=scroll?.scrollOffset.ToString(),status=panel?.Q<Label>("colonisation-command-status")?.text,selection=ColonySelection(panel),draft=ColonyDraft(doc)}));
                if(panel!=null)
                {
                    if(oldConfirm!=null)Assert.That(doc.rootVisualElement.Q<Button>("colonisation-order-confirm"),Is.Not.Null,"same-frame resize retains the displayed review with fresh controls");
                    Assert.That(doc.rootVisualElement.Query<ReviewColonisationPanel>().ToList().Count,Is.EqualTo(1));Assert.That(scroll!.contentViewport.worldBound.height,Is.GreaterThanOrEqualTo(100));
                    if(inspector){Assert.That(item,Is.Not.Null);Assert.That(item!.worldBound.height,Is.GreaterThanOrEqualTo(180));PublishedReceivingContained(scroll.contentViewport.worldBound,item,"whole180px hull image");Assert.That(((Image)item).image,Is.Not.Null);}
                    var footer=doc.rootVisualElement.Q("player-footer");Assert.That(panel.worldBound.yMax,Is.LessThanOrEqualTo(footer.worldBound.yMin));
                    foreach(var button in panel.Query<Button>().ToList().Where(b=>b.enabledInHierarchy&&b.resolvedStyle.display!=DisplayStyle.None&&b.worldBound.height>0).Take(4)){Assert.That(button.worldBound.height,Is.GreaterThanOrEqualTo(44));Assert.That(button.resolvedStyle.fontSize,Is.GreaterThanOrEqualTo(16));}
                    if(panel.Q<ScrollView>("colonisation-catalogue")!=null)
                    {
                        foreach(var id in new[]{"colonisation-section-PublishedShip","colonisation-section-Infrastructure","colonisation-section-projects","colonisation-section-system"}){var section=panel.Q(id);Assert.That(section,Is.Not.Null);panel.Q<ScrollView>("colonisation-catalogue").ScrollTo(section);scroll.ScrollTo(section);yield return null;yield return null;PublishedReceivingContained(panel.Q<ScrollView>("colonisation-catalogue").contentViewport.worldBound,section,id);}
                        Assert.That(panel.Q<ScrollView>("colonisation-queue").contentViewport.worldBound.height,Is.GreaterThanOrEqualTo(100));
                    }
                }
            }
            finally{doc.panelSettings.targetTexture=prior;doc.rootVisualElement.style.width=width;doc.rootVisualElement.style.height=height;RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}yield return null;yield return null;
            Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(source),"layout, inspection and queries never mutate authoritative campaign");Assert.That(ColonySelection(doc.rootVisualElement.Q<ReviewColonisationPanel>()),Is.EqualTo(selected),"same-controller redraw retains only view selection");
            Assert.That(ColonyDraft(doc),Is.EqualTo(draft),"same transport context retains user food, cohort and material inputs through resize");
            if(oldConfirm!=null){var current=doc.rootVisualElement.Q<Button>("colonisation-order-confirm");Assert.That(current,Is.Not.Null,"review also survives restoration of the original size");if(!ReferenceEquals(oldConfirm,current)){StreamCapturedEvent(oldConfirm);yield return null;Assert.That(ColonyPending(s.Shared.Entry!),Is.False);Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(source),"removed pre-resize Confirm is inert");}}
        }
        private static IEnumerator ColonyManufacture(ColonyCase s,UIDocument doc,int transports,IReadOnlyList<string>? captured=null)
        {
            var orders=captured?.ToArray()??ColonyModel(s).Colonisation().Production.Where(x=>x.ColonyId==s.Home).Select(x=>x.Id).ToArray();
            bool shipOrder=ColonyModel(s).Colonisation().Production.Any(x=>orders.Contains(x.Id)&&x.Kind==ColonisationProductionKind.PublishedShip);
            Assert.That(orders,Is.Not.Empty);bool consumed=false;var consumedWorlds=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<96&&ColonyModel(s).Colonisation().Production.Any(x=>orders.Contains(x.Id));i++)
            {
                yield return ColonyAdvance(s,doc);
                var progress=ColonyModel(s).Colonisation().Production.Where(x=>orders.Contains(x.Id)&&x.CompletedIndustry>0&&x.Consumed.Any(r=>r.Amount>0)).ToArray();
                consumed|=progress.Length>0;foreach(var order in progress)consumedWorlds.Add(order.ColonyId);
            }
            Assert.That(ColonyModel(s).Colonisation().Production.Where(x=>orders.Contains(x.Id)),Is.Empty,"normal FIFO finishes the actual captured orders");
            Assert.That(ColonyModel(s).Colonisation().Transports.Count,Is.GreaterThanOrEqualTo(transports));
            if(shipOrder)Assert.That(consumed,Is.True,"actual positive partial ship work and material consumption are observed before completion");
            if(s.RosterV2&&captured!=null){Assert.That(consumedWorlds.Contains(s.Home),Is.True,"Home orders consume real delivered/local materials");Assert.That(consumedWorlds.Contains(s.Yard),Is.True,"parallel Yard orders consume real delivered/local materials");}
            var history=ColonyAudit(ColonyModel(s)).ProductionHistory!;foreach(var id in orders)Assert.That(history.Count(x=>x.Completed&&x.Order!.QueueItemId==id),Is.EqualTo(1),"each captured own order completes once");
            s.Shared.Timings.Add(new{phase="manufacture",capturedOrders=orders,positiveConsumedWorkObserved=consumed,consumedWorlds=consumedWorlds.ToArray(),boundWeeks=96});
        }
        private static IEnumerator ColonySurveyTarget(ColonyCase s,UIDocument doc)
        {
            var home=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);
            var homeEnvironment=StreamHost(ColonyModel(s)).CaptureColonisationPlanets(ColonyModel(s).Current.Frame).Single(x=>x.PlanetId==home.PlanetId).Environment!;
            var visited=s.SurveyVisited;visited.Add(s.HomeNode);
            for(;s.SurveyAttempts<20&&s.TargetPlanet=="";)
            {
                s.SurveyAttempts++;var own=ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==s.Explorer);visited.Add(own.NodeId);
                var excluded=ColonyObservedRisk(s,"before-survey");
                // Discovery and candidate selection use ONLY the detached actor projection.
                foreach(var planet in ColonyModel(s).ColonisationPlanets().Where(x=>x.SystemId!=s.HomeSystem&&!x.Surveyed&&!s.AttemptedPlanets.Contains(x.PlanetId)&&!excluded.Contains(own.NodeId)&&ColonyModel(s).GeneratedMap().Regions.Any(r=>r.NodeId==own.NodeId&&r.SystemId==x.SystemId)).ToArray())
                {
                    if(ColonyObservedRisk(s,"before-next-body",own.NodeId).Contains(own.NodeId))break;
                    yield return ColonyPage(s,doc,"Planets");yield return ColonyPick(doc,"colonisation-planet-selector",ColonyModel(s).ColonisationPlanets(),x=>x.PlanetId,planet.PlanetId);
                    yield return ColonyPick(doc,"colonisation-survey-ship",ColonyModel(s).ColonisationActivities().Ships,x=>x.ShipId,s.Explorer);
                    yield return ColonyReview(s,doc,"colonisation-survey-review");
                    if(!s.SurveyCaptured){yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),"colony-02-survey-review-1920.png");s.SurveyCaptured=true;}
                    yield return ColonyClick(s,doc,"colonisation-order-confirm");
                    if(!s.SurveyCancelled)
                    {
                        yield return ColonyPage(s,doc,"Planets");yield return ColonyOrder(s,doc,"colonisation-survey-cancel");
                        Assert.That(ColonyModel(s).Colonisation().Surveys.Last(x=>x.PlanetId==planet.PlanetId).Stage,Is.EqualTo(PlanetSurveyStage.Cancelled));
                        Assert.That(ColonyModel(s).ColonisationPlanets().Single(x=>x.PlanetId==planet.PlanetId).Surveyed,Is.False);
                        yield return ColonyOrder(s,doc,"colonisation-survey-review");s.SurveyCancelled=true;
                    }
                    for(int week=0;week<4&&!ColonyModel(s).ColonisationPlanets().Single(x=>x.PlanetId==planet.PlanetId).Surveyed;week++)yield return ColonyAdvance(s,doc);
                    var observed=StreamHost(ColonyModel(s)).CaptureColonisationPlanets(ColonyModel(s).Current.Frame).Single(x=>x.PlanetId==planet.PlanetId);
                    Assert.That(observed.Surveyed,Is.True);var e=observed.Environment!;
                    // A conservative choice from publicly surveyed facts, not a replacement eligibility rule.
                    if(!ColonyObservedRisk(s,"post-survey",own.NodeId).Contains(own.NodeId)&&observed.KnownControl?.Kind==PlanetControllerKind.Unclaimed&&e.Deposits!.Any(r=>r.Amount>0)&&e.Gravity==homeEnvironment.Gravity&&e.Pressure==homeEnvironment.Pressure&&e.Atmosphere==homeEnvironment.Atmosphere&&e.Radiation<=homeEnvironment.Radiation&&e.Habitats!.Any(h=>h.NaturalCapacity>=100&&homeEnvironment.Habitats!.Any(k=>k.Kind==h.Kind&&k.ShareBps>0)))
                    {s.TargetPlanet=planet.PlanetId;s.TargetNode=own.NodeId;break;}
                }
                if(s.TargetPlanet!="")
                {
                    // V2 exercises the real own standing order before return; no unseen contact is a command target.
                    if(s.RosterV2)yield return ColonyCover(s,doc,"surveyed-target-cover");
                    // Return the shared fleet normally before loading; risk is rechecked against CURRENT observations.
                    yield return ColonyMove(s,doc,s.Explorer,s.HomeNode);
                    if(ColonyObservedRisk(s,"returned-home-before-load",s.TargetNode).Contains(s.TargetNode)){s.TargetPlanet="";s.TargetNode="";}
                }
                if(s.TargetPlanet=="")
                {
                    own=ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==s.Explorer);
                    yield return ColonyMove(s,doc,s.Explorer,ColonyNextKnown(ColonyModel(s).GeneratedMap(),own.NodeId,visited,ColonyObservedRisk(s,"next-frontier")));
                }
            }
            Assert.That(s.TargetPlanet,Is.Not.Empty,"a surveyed lawful nearby suitable resource opportunity");Assert.That(s.SurveyCancelled,Is.True);
            Assert.That(ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==s.Explorer).NodeId,Is.EqualTo(s.HomeNode));
        }
        private static IEnumerator ColonyConfirmNegatives(ColonyCase s,UIDocument doc)
        {
            var old=doc.rootVisualElement.Q<Button>("colonisation-order-confirm");Assert.That(old,Is.Not.Null);var before=ColonyBytes(ColonyModel(s));
            yield return ColonyClick(s,doc,"colonisation-order-back");StreamCapturedEvent(old);yield return null;
            Assert.That(ColonyPending(s.Shared.Entry!),Is.False);Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(before),"old Confirm after Back is inert");
            yield return ColonyReview(s,doc,"colonisation-embark");var replaced=doc.rootVisualElement.Q<Button>("colonisation-order-confirm");
            // A replacement review on the SAME live panel exercises the independent review token.
            var panel=doc.rootVisualElement.Q<ReviewColonisationPanel>();var workFrame=ColonyModel(s).Current.Frame;var world=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);
            var cohort=doc.rootVisualElement.Q<DropdownField>("colonisation-cohort");var command=new ColonisationCommandDto{ActorId=ColonyModel(s).Colonisation().Actor,Turn=workFrame.Status.Turn,Revision=workFrame.Status.CampaignRevision,CommandId="command.receiving.replacement."+Guid.NewGuid().ToString("N"),Action=ColonisationAction.EmbarkSettlers,Embark=new EmbarkSettlersDto{ShipId=s.Transport,ColonyId=s.Home,CohortId=world.Cohorts[cohort.index].Id,Population=100,Food=doc.rootVisualElement.Q<IntegerField>("colonisation-food").value,Materials=world.Stocks.Where(r=>doc.rootVisualElement.Q<IntegerField>("colonisation-material-"+r.ResourceDefinitionId)?.value>0).Select(r=>new ResourceAmountDto{ResourceDefinitionId=r.ResourceDefinitionId,Amount=doc.rootVisualElement.Q<IntegerField>("colonisation-material-"+r.ResourceDefinitionId).value}).ToList()}};
            // Test-only same-panel lifetime probe uses genuine public eligibility; it never submits this replacement.
            var eligibility=StreamHost(ColonyModel(s)).EvaluateColonisation(workFrame,command);Assert.That(eligibility.Available,Is.True);
            typeof(ReviewColonisationPanel).GetMethod("PresentReviewedOrder",HullPrivate)!.Invoke(panel,new object[]{command,"Replacement of the same actual reviewed embark payload (view lifetime probe only).",eligibility});
            StreamCapturedEvent(replaced);yield return null;Assert.That(ColonyPending(s.Shared.Entry!),Is.False);Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(before),"removed Confirm after replacement is inert");
            yield return ColonyClick(s,doc,"colonisation-order-back");yield return ColonyReview(s,doc,"colonisation-embark");
        }
        private static IEnumerator ColonyInspectorNegatives(ColonyCase s,UIDocument doc)
        {
            var options=ColonyModel(s).ColonisationActivities().BuildOptions;var ships=options.Select((v,i)=>(v,i)).Where(x=>x.v.Kind==ActivityProductionKind.PublishedShip&&x.v.Supported).Take(2).ToArray();Assert.That(ships.Length,Is.EqualTo(2));
            yield return ColonyClick(s,doc,"colonisation-inspect-"+(ships[0].i+1));var old=doc.rootVisualElement.Q<Foldout>("colonisation-hull-inspector");var orbit=doc.rootVisualElement.Q<Button>("colonisation-hull-orbit");
            yield return ColonyClick(s,doc,"colonisation-inspect-"+(ships[1].i+1));var panel=doc.rootVisualElement.Q<ReviewColonisationPanel>();var field=typeof(ReviewColonisationPanel).GetField("hullPreview",HullPrivate)!;var current=field.GetValue(panel);Assert.That(current,Is.Not.Null);
            var texture=(RenderTexture)doc.rootVisualElement.Q<Image>("colonisation-hull-image").image;var pixels=HullTextureBytes(texture);
            old.value=false;old.value=true;StreamCapturedEvent(orbit);yield return null;yield return null;
            Assert.That(field.GetValue(panel),Is.SameAs(current));Assert.That(texture.IsCreated(),Is.True);Assert.That(HullTextureBytes(texture),Is.EqualTo(pixels),"removed inspector foldout/orbit cannot touch the current stage");
        }
        private static byte[] HullTextureBytes(RenderTexture texture)
        {var previous=RenderTexture.active;Texture2D? read=null;try{RenderTexture.active=texture;read=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);read.Apply();return read.EncodeToPNG();}finally{RenderTexture.active=previous;if(read!=null)UnityEngine.Object.DestroyImmediate(read);}}
        private static IEnumerator ColonyLoadOne(ColonyCase s,UIDocument doc,string ship,bool lifecycle=false)
        {
            yield return ColonyTransport(s,doc,s.Home,ship);var source=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);
            yield return ColonyPick(doc,"colonisation-cohort",source.Cohorts,x=>x.Id,source.Cohorts.Where(x=>x.Population>=400).OrderByDescending(x=>x.Id,StringComparer.Ordinal).First().Id);
            if(lifecycle)
            {
                var oldFood=doc.rootVisualElement.Q<IntegerField>("colonisation-food");var oldMaterial=doc.rootVisualElement.Q<IntegerField>("colonisation-material-resource.dilithium");Assert.That(oldMaterial,Is.Not.Null);Assert.That(source.Stocks.Single(x=>x.ResourceDefinitionId=="resource.dilithium").Amount,Is.GreaterThan(0));oldFood.value=19;oldMaterial.value=1;var drafted=ColonyDraft(doc);
                var root=ColonyBytes(ColonyModel(s));var sourceFrame=ColonyModel(s).Current.Frame;var selected=ColonySelection(doc.rootVisualElement.Q<ReviewColonisationPanel>());var button=doc.rootVisualElement.Q<Button>("colonisation-embark");yield return ColonyReach(doc,button);
                var cancelled=ColonyStartHold(s,()=>Click(button),true);yield return ColonyEntered(cancelled);yield return ColonyBusy(s,doc,cancelled,root,sourceFrame);
                yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),"colony-04-held-busy-1920.png");
                s.Shared.Entry!.enabled=false;yield return null;s.Shared.Entry.enabled=true;yield return null;Assert.That(ColonyPending(s.Shared.Entry),Is.True,"cancelled source-reading worker must drain before UI re-admits");
                cancelled.Release.Set();var wait=System.Diagnostics.Stopwatch.StartNew();while(!cancelled.Work!.Settled||cancelled.Continuation!.Queued==0){Assert.That(wait.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                s.Shared.Entry.enabled=false;yield return null;s.Shared.Entry.enabled=true;yield return null;yield return null;
                Assert.That(ColonyModel(s).Busy,Is.False);Assert.That(ColonyModel(s).Current.Frame,Is.SameAs(sourceFrame));Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(root));
                Assert.That(ColonySelection(doc.rootVisualElement.Q<ReviewColonisationPanel>()),Is.EqualTo(selected),"fresh view retains the already selected source world and transport");
                Assert.That(ColonyDraft(doc),Is.EqualTo(drafted),"cancelled preview/drain retains the actual nondefault form draft");oldFood.value=8;oldMaterial.value=0;yield return null;Assert.That(ColonyDraft(doc),Is.EqualTo(drafted),"disposed input callbacks cannot edit the current draft");
                var tracked=s.Contexts.Count;var oldUi=CheckpointField(s.Shared.Entry,"colonisationUiWork");ColonyActivate(s,()=>{});
                Assert.That(s.Contexts.Count,Is.EqualTo(tracked),"synchronous activation cannot inherit an older held continuation");Assert.That(CheckpointField(s.Shared.Entry,"colonisationUiWork"),Is.SameAs(oldUi));
                // Do not globally await the deliberately held OLD finally before creating its replacement lease.
                button=doc.rootVisualElement.Q<Button>("colonisation-embark");yield return ColonyReach(doc,button,target:"fresh selected Embark while old finally is held");
                var next=ColonyStartHold(s,()=>Click(button));yield return ColonyEntered(next);var token=CheckpointField(s.Shared.Entry,"campaignBusyToken");cancelled.Continuation.Forward();
                wait.Restart();while(Volatile.Read(ref cancelled.Context!.Finished)==0){Assert.That(wait.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                Assert.That(CheckpointField(s.Shared.Entry,"colonisationUiWork"),Is.SameAs(next.Work));Assert.That(CheckpointField(s.Shared.Entry,"campaignBusyToken"),Is.SameAs(token));Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(root));
                next.Release.Set();yield return ColonyIdle(s,"new-preview-after-cancel-drain");ColonyProbe(StreamHost(ColonyModel(s)),null);
                Assert.That(ColonyDraft(doc),Is.EqualTo(drafted),"pure preview completion preserves food/cohort/material draft");
                yield return ColonyConfirmNegatives(s,doc);yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),"colony-03-embark-review-1920.png");
                var oldFrame=ColonyModel(s).Current.Frame;var oldConfirm=doc.rootVisualElement.Q<Button>("colonisation-order-confirm");var unchanged=ColonyBytes(ColonyModel(s));
                Assert.That(ColonyModel(s).Refresh(),Is.True);Assert.That(ColonyModel(s).Current.Frame,Is.Not.SameAs(oldFrame),"explicit refresh replaces the frame even at the same stamp");typeof(ReviewEntry).GetMethod("Draw",HullPrivate)!.Invoke(s.Shared.Entry,null);yield return null;yield return null;
                Assert.That(doc.rootVisualElement.Q<Button>("colonisation-order-confirm"),Is.Null,"a replaced frame discards eligibility presentation");StreamCapturedEvent(oldConfirm);yield return null;Assert.That(ColonyPending(s.Shared.Entry!),Is.False);Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(unchanged));
                // This re-review follows an explicit frame replacement, never a workaround for resize loss.
                Assert.That(ColonyDraft(doc),Is.EqualTo(drafted));doc.rootVisualElement.Q<IntegerField>("colonisation-food").value=20;doc.rootVisualElement.Q<IntegerField>("colonisation-material-resource.dilithium").value=0;
                yield return ColonyReview(s,doc,"colonisation-embark");
            }
            else{Assert.That(doc.rootVisualElement.Q<IntegerField>("colonisation-food").value,Is.EqualTo(20));Assert.That(doc.rootVisualElement.Query<IntegerField>().ToList().Where(f=>f.name.StartsWith("colonisation-material-",StringComparison.Ordinal)).All(f=>f.value==0),Is.True,"different transport/manifest context resets material drafts");yield return ColonyReview(s,doc,"colonisation-embark");}
            var before=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);var credits=ColonyModel(s).ColonisationActivities().Credits;
            yield return ColonyClick(s,doc,"colonisation-order-confirm");var after=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);var loaded=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==ship);
            Assert.That(after.Population,Is.EqualTo(before.Population-100));Assert.That(after.Food,Is.EqualTo(before.Food-20));Assert.That(after.Stocks.Where(x=>x.ResourceDefinitionId=="resource.settlement-kit").Sum(x=>x.Amount),Is.EqualTo(before.Stocks.Where(x=>x.ResourceDefinitionId=="resource.settlement-kit").Sum(x=>x.Amount)-1));
            Assert.That(loaded.Population,Is.EqualTo(100));Assert.That(loaded.Food,Is.EqualTo(20));Assert.That(loaded.Kind,Is.EqualTo(SettlerManifestKind.Settlement));Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(credits));
            Assert.That(loaded.Cargo.Where(x=>x.ResourceDefinitionId=="resource.settlement-kit").Sum(x=>x.Amount),Is.EqualTo(1));Assert.That(loaded.Cargo.Where(x=>x.ResourceDefinitionId!="resource.settlement-kit").Sum(x=>x.Amount),Is.Zero,"distinctive draft material is explicitly reset and re-reviewed before actual embark");
        }
        private static IEnumerator ColonyEstablished(ColonyCase s,UIDocument doc)
        {
            for(int i=0;i<12&&ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld).Stage!=ColonyEstablishmentStage.Established;i++)yield return ColonyAdvance(s,doc);
            var world=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld);Assert.That(world.Stage,Is.EqualTo(ColonyEstablishmentStage.Established));Assert.That(world.FedWeeks,Is.GreaterThanOrEqualTo(4));Assert.That(world.Industry,Is.GreaterThan(0));Assert.That(world.Population,Is.GreaterThan(0));
        }
        private static IEnumerator ColonyIncomingRejected(ColonyCase s,UIDocument doc,string ship)
        {yield return ColonyTransport(s,doc,s.TargetWorld,ship);Assert.That(ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==ship).Population,Is.GreaterThan(0));yield return ColonyReview(s,doc,"colonisation-unload",false);}
        private static IEnumerator ColonyDeploy(ColonyCase s,UIDocument doc,bool allowInterrupted=false)
        {
            s.LandingCompleted=false;yield return ColonyTransport(s,doc,s.Home);yield return ColonyPick(doc,"colonisation-deploy-planet",ColonyModel(s).ColonisationPlanets().Where(x=>x.Surveyed).ToArray(),x=>x.PlanetId,s.TargetPlanet);
            var manifest=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport).ManifestId;
            yield return ColonyOrder(s,doc,"colonisation-deploy");var intent=ColonyModel(s).Colonisation().Intents.Single(x=>x.PlanetId==s.TargetPlanet&&x.ManifestId==manifest);
            yield return ColonyAdvance(s,doc);var result=ColonyModel(s).Colonisation().LatestDeployments.Single(x=>x.IntentId==intent.Id&&x.ManifestId==manifest&&x.PlanetId==s.TargetPlanet);
            s.Shared.Timings.Add(new{phase="actual-own-deployment-result",result.IntentId,result.ManifestId,result.PlanetId,result.Turn,result.Outcome,result.StillAboard});
            if(result.Outcome==DeploymentBoundaryOutcome.Interrupted&&allowInterrupted)
            {
                Assert.That(result.StillAboard,Is.True);Assert.That(ColonyModel(s).Colonisation().Intents.Any(x=>x.Id==intent.Id),Is.False);
                foreach(var id in new[]{s.Transport,s.SecondTransport}){var t=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==id);Assert.That(t.Living,Is.True);Assert.That(t.ManifestId,Is.Not.Null);Assert.That(t.Population,Is.EqualTo(100));Assert.That(t.Food,Is.GreaterThan(0));Assert.That(t.Cargo.Where(x=>x.ResourceDefinitionId=="resource.settlement-kit").Sum(x=>x.Amount),Is.EqualTo(1));}
                yield return ColonyTransport(s,doc,s.Home,s.Transport);var label=doc.rootVisualElement.Q<Label>("colonisation-deployment-result-"+intent.Id);Assert.That(label,Is.Not.Null);Assert.That(label.text,Does.Contain("Landing did not complete"));Assert.That(label.text,Does.Contain("Settlers remain aboard"));
                var parents=new List<ScrollView>();for(var at=label.parent;at!=null;at=at.parent)if(at is ScrollView v)parents.Add(v);
                foreach(var view in parents){view.ScrollTo(label);yield return null;yield return null;PublishedReceivingContained(view.contentViewport.worldBound,label,"actual own interrupted landing feedback");}
                Assert.That(label.panel,Is.SameAs(doc.rootVisualElement.panel));Assert.That(label.worldBound.height,Is.GreaterThan(0));Assert.That(label.worldBound.yMax,Is.LessThanOrEqualTo(doc.rootVisualElement.Q("player-footer").worldBound.yMin));
                yield break;
            }
            Assert.That(result.Outcome,Is.EqualTo(DeploymentBoundaryOutcome.Deployed),"no silent repeated recovery or changed landing rules");
            var world=ColonyModel(s).Colonisation().Worlds.Single(x=>x.PlanetId==s.TargetPlanet);s.TargetWorld=world.Id;s.LandingCompleted=true;
            Assert.That(world.Stage,Is.EqualTo(ColonyEstablishmentStage.Founding));Assert.That(world.Population,Is.EqualTo(100));Assert.That(ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport).ManifestId,Is.Null);
        }
        private static IEnumerator ColonyRecoverLanding(ColonyCase s,UIDocument doc)
        {
            // Only our own submitted target/action is excluded; no hidden reason, enemy position or private route is consulted.
            Assert.That(s.AttemptedPlanets.Add(s.TargetPlanet),Is.True);Assert.That(s.AttemptedNodes.Add(s.TargetNode),Is.True);
            foreach(var id in new[]{s.Transport,s.SecondTransport})
            {
                yield return ColonyMove(s,doc,id,s.HomeNode);yield return ColonyTransport(s,doc,s.Home,id);
                var before=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);var cargo=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==id);Assert.That(cargo.Population,Is.EqualTo(100));Assert.That(cargo.Food,Is.GreaterThan(0));
                var kit=cargo.Cargo.Where(x=>x.ResourceDefinitionId=="resource.settlement-kit").Sum(x=>x.Amount);Assert.That(kit,Is.EqualTo(1));
                yield return ColonyOrder(s,doc,"colonisation-unload");var after=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);
                Assert.That(after.Population,Is.EqualTo(before.Population+cargo.Population));Assert.That(after.Food,Is.EqualTo(before.Food+cargo.Food));
                Assert.That(after.Stocks.Where(x=>x.ResourceDefinitionId=="resource.settlement-kit").Sum(x=>x.Amount),Is.EqualTo(before.Stocks.Where(x=>x.ResourceDefinitionId=="resource.settlement-kit").Sum(x=>x.Amount)+kit));
                Assert.That(ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==id).ManifestId,Is.Null);Assert.That(ColonyModel(s).Colonisation().Reports.Single(x=>x.ManifestId==cargo.ManifestId).Stage,Is.EqualTo(SettlerManifestStage.Unloaded));
                s.Shared.Timings.Add(new{phase="actual-interrupted-expedition-return-unload",ship=id,cargo.ManifestId,cargo.Population,cargo.Food,recoveredKits=kit});
            }
            s.TargetPlanet="";s.TargetNode="";s.TargetWorld="";yield return ColonySurveyTarget(s,doc);
            Assert.That(s.AttemptedPlanets.Contains(s.TargetPlanet)||s.AttemptedNodes.Contains(s.TargetNode),Is.False,"ONE different publicly discovered target/system");
            yield return ColonyLoadOne(s,doc,s.Transport);yield return ColonyLoadOne(s,doc,s.SecondTransport);
            // No extra SaveContinue, repeated captures or held races: first loaded checkpoint already covers the stage.
            yield return ColonyMove(s,doc,s.Transport,s.TargetNode);yield return ColonyMove(s,doc,s.SecondTransport,s.TargetNode);yield return ColonyDeploy(s,doc);
            Assert.That(s.LandingCompleted,Is.True,"the one allowed replanned attempt must actually land");
        }
        private static IEnumerator ColonyNoCohort(ColonyCase s,UIDocument doc)
        {
            yield return ColonyTransport(s,doc,s.TargetWorld);var w=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld);Assert.That(w.Stage,Is.EqualTo(ColonyEstablishmentStage.EmptyOutpost));Assert.That(w.Cohorts,Is.Empty);
            var before=ColonyBytes(ColonyModel(s));var embark=doc.rootVisualElement.Q<Button>("colonisation-embark");Assert.That(embark,Is.Not.Null);Assert.That(embark.enabledSelf,Is.False);Assert.DoesNotThrow(()=>StreamCapturedEvent(embark));yield return null;
            Assert.That(ColonyPending(s.Shared.Entry!),Is.False);Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(before));Assert.That(doc.rootVisualElement.Q<Button>("colonisation-load-supplies"),Is.Not.Null,"independent supply control remains available");
        }
        private static IEnumerator ColonyHistoryComponent(ColonyCase s,UIDocument doc)
        {
            var o=ColonyModel(s).Colonisation();Assert.That(o.Reports,Is.Not.Empty);var before=ColonyBytes(ColonyModel(s));
            var onlyView=new ColonisationCampaignSnapshot(o.Actor,o.Turn,o.Revision,o.PackageHash,o.Worlds,Array.Empty<ColonisationTransportSnapshot>(),o.Production,o.Presence,o.Reports,o.Surveys,o.Intents,o.KitIndustry,o.KitResources,o.OwnShipSources);
            // View-only no-live-transport component assertion; no campaign destruction is claimed.
            using(var view=new ReviewColonisationPanel(onlyView,ColonyModel(s).ColonisationPlanets(),ColonyModel(s).ColonisationActivities(),(_,__)=>Assert.Fail("history cannot issue orders"),_=>Assert.Fail("history cannot submit"),(_,__)=>Assert.Fail("history cannot preview"),_=>Assert.Fail("history cannot submit")))
            {
                view.style.position=Position.Absolute;view.style.left=0;view.style.top=0;view.style.width=800;view.style.height=600;doc.rootVisualElement.Add(view);Click(view.Q<Button>("colonisation-tab-Transports"));yield return null;
                var history=view.Q<Foldout>("colonisation-expedition-history");Assert.That(history.value,Is.True);Assert.That(history.Query<Label>().ToList().Count,Is.GreaterThanOrEqualTo(o.Reports.Count));view.RemoveFromHierarchy();
            }
            Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(before));
        }
        [UnityTest,Timeout(600000)]
        public IEnumerator ColonisationCampaignUsesConservedSettlersAndTypedContinue()
            =>ColonyReceiving(false);
        [UnityTest,Timeout(600000)]
        public IEnumerator ColonisationRosterV2UsesConservedSettlersAndTypedContinue()
            =>ColonyReceiving(true);
        private IEnumerator ColonyReceiving(bool rosterV2)
        {
            var s=new ColonyCase{RosterV2=rosterV2};var stack=new Stack<IEnumerator>();stack.Push(ColonyReceivingBody(s));Exception? failure=null;
            while(stack.Count>0)
            {
                var current=stack.Peek();bool next=false;object? value=null;try{next=current.MoveNext();if(next)value=current.Current;}catch(Exception e){failure=e;}
                if(failure!=null)break;if(!next){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception e){failure=e;break;}continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
            }
            void Cleanup(string phase,Action action){try{action();}catch(Exception e){failure=failure??e;try{CampaignDiagnosticError(phase,e);}catch{/* Diagnostic failure must not skip scene exit. */}}}
            var releaseFailure=s.ReleaseAll();failure=failure??releaseFailure;Cleanup("colonisation-release",()=>s.Shared.Release.Set());while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){failure=failure??e;}
            // Cancel immediately on exit/failure, before waiting. Every held continuation is forwarding now.
            if(s.Shared.Entry!=null)Cleanup("colonisation-disable",()=>s.Shared.Entry.enabled=false);
            bool settled=true;
            bool PendingCleanup(){if(s.Pending||s.Shared.Holder?.IsCompleted==false)return true;bool pending=false;Cleanup("colonisation-drain-status",()=>pending=s.Shared.Entry!=null&&ColonyPending(s.Shared.Entry));return pending;}
            var wait=System.Diagnostics.Stopwatch.StartNew();while(PendingCleanup())
            {if(wait.Elapsed.TotalSeconds>=90){settled=false;directory=null;failure=failure??new TimeoutException("Unsettled colonisation worker; temporary namespace and its scene override retained until scene exit.");break;}yield return null;}
            Cleanup("colonisation-latency-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"colony-latency.json"),JsonConvert.SerializeObject(s.Shared.Timings)));
            if(settled)Cleanup("colonisation-final-audit",()=>{if(s.Shared.Entry!=null&&ColonyModel(s).ColonisationCampaign&&!ColonyPending(s.Shared.Entry))File.WriteAllBytes(Path.Combine(CampaignOutput(),failure==null?"colony-final-private-audit.json":"colony-failed-private-audit.json"),ColonyBytes(ColonyModel(s)));});
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured)Cleanup("colonisation-restore-override",()=>typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory));
            Cleanup("colonisation-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"colony-cleanup.json"),JsonConvert.SerializeObject(new{settled,allReleasesSet=s.Holds.All(h=>h.Release.IsSet),priorOverrideCaptured=s.PriorDirectoryCaptured,overrideRestored=settled&&s.PriorDirectoryCaptured,directoryRetained=!settled,entryDisabled=true,cleanupMs=wait.Elapsed.TotalMilliseconds})));
            if(settled)foreach(var host in s.Hosts)Cleanup("colonisation-remove-probe",()=>ColonyProbe(host,null));if(!s.Pending)foreach(var h in s.Holds){Cleanup("colonisation-dispose-entered",()=>h.Entered.Dispose());Cleanup("colonisation-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted){Cleanup("colonisation-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("colonisation-dispose-shared-release",()=>s.Shared.Release.Dispose());}yield return new ExitPlayMode();if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        private IEnumerator ColonyReceivingBody(ColonyCase s)
        {
            yield return new EnterPlayMode();yield return null;yield return null;s.Shared.Entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var entry=s.Shared.Entry;
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Colonisation-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            field.SetValue(entry,Path.Combine(directory,"campaign"));
            using(var surface=new GalaxyInputSurface(doc))
            {
                doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").value="2.00×";
                doc.rootVisualElement.Q<Foldout>(s.MenuPrefix+"-menu").value=true;yield return null;ColonyActivate(s,()=>{Click(doc.rootVisualElement.Q<Button>(s.MenuPrefix+"-start"));if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});yield return ColonyIdle(s,"new-colonisation250");
                Assert.That(ColonyModel(s).ColonisationCampaign,Is.True);
                Assert.That(ColonyModel(s).ColonisationRosterCampaign,Is.EqualTo(s.RosterV2),"explicit menu retains the trusted starting-content source");
                File.WriteAllText(Path.Combine(CampaignOutput(),"colony-profile.json"),JsonConvert.SerializeObject(new{rosterV2=s.RosterV2,menu=s.MenuPrefix,slotPrefix=s.SlotPrefix,templatePackageHash=ColonyAudit(ColonyModel(s)).TemplatePackageHash}));
                var home=ColonyModel(s).Colonisation().Worlds.Where(x=>x.Stage==ColonyEstablishmentStage.Established&&x.Population>=700&&(x.Cohorts.Any(c=>c.Population>=500)||x.Cohorts.Count(c=>c.Population>=400)>=2)).OrderByDescending(x=>x.Population).ThenBy(x=>x.Id,StringComparer.Ordinal).First();s.Home=home.Id;s.HomeSystem=home.SystemId;
                s.HomeNode=ColonyModel(s).GeneratedMap().Regions.Single(x=>x.SystemId==s.HomeSystem).NodeId;s.Explorer=ColonyModel(s).ColonisationActivities().Ships.First(x=>x.StudyWork>0).ShipId;
                s.Yard=s.Home;
                if(s.RosterV2)
                {
                    var shipyards=ColonyModel(s).Current.Frame.Strategy!.Colonies.Where(x=>x.Economy.Shipbuilding).Select(x=>x.ColonyId.Value).ToHashSet(StringComparer.Ordinal);
                    var yard=ColonyModel(s).Colonisation().Worlds.Where(x=>x.Id!=s.Home&&x.SystemId==s.HomeSystem&&x.Stage==ColonyEstablishmentStage.Established&&shipyards.Contains(x.Id)&&x.Industry>home.Industry).OrderByDescending(x=>x.Industry).ThenBy(x=>x.Id,StringComparer.Ordinal).First();s.Yard=yard.Id;
                    s.Shared.Timings.Add(new{phase="actor-observed-parallel-production-allocation",home,yard,qualification="Home keeps physical kits/cohorts; the higher-output owned shipyard builds transports. No ETA is promised."});
                }
                yield return ColonyMove(s,doc,s.Explorer,s.HomeNode);
                yield return ColonyPage(s,doc,"Science");Assert.That(doc.rootVisualElement.Q<DropdownField>("activity-ship-selector"),Is.Not.Null,"existing real expedition controls remain reachable on the new profile");
                var options=ColonyModel(s).ColonisationActivities().BuildOptions;var transport=options.Single(x=>x.Kind==ActivityProductionKind.PublishedShip&&x.Supported&&x.DefinitionId=="blueprint.ready.galaxy-colony-transport"&&x.BlueprintRevision==1);var blueprint=transport.DefinitionId;
                yield return ColonyWorld(s,doc,s.Home);yield return ColonyInspectorNegatives(s,doc);yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),"colony-01-catalogue-1920.png",true);
                var root=ColonyBytes(ColonyModel(s));var frame=ColonyModel(s).Current.Frame;var kit=doc.rootVisualElement.Q<Button>("colonisation-build-kit");yield return ColonyReach(doc,kit);
                yield return ColonyClick(s,doc,"colonisation-tab-Planets");StreamCapturedEvent(kit);yield return null;Assert.That(ColonyPending(entry),Is.False);Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(root),"inactive-page + callback is inert");yield return ColonyWorld(s,doc,s.Home);kit=doc.rootVisualElement.Q<Button>("colonisation-build-kit");yield return ColonyReach(doc,kit);
                var fleet=ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==s.Explorer).FleetId;
                Assert.That(ColonyModel(s).Current.EditTravel(fleet,CampaignTravelIntent.Stay),Is.True,"real unsent local travel draft blocks submission");Click(kit);yield return null;
                Assert.That(ColonyPending(entry),Is.False);Assert.That(doc.rootVisualElement.Q<Label>("colonisation-command-status").text,Does.Contain("unavailable"));Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(root));Assert.That(ColonyModel(s).Current.ResetTravelDraft(),Is.True);
                // Retry the SAME still-attached control only after explicitly discarding the draft.
                var submit=ColonyStartHold(s,()=>Click(kit));yield return ColonyEntered(submit);yield return ColonyBusy(s,doc,submit,root,frame);StreamCapturedEvent(kit);Assert.That(CheckpointField(entry,"colonisationUiWork"),Is.SameAs(submit.Work));
                submit.Release.Set();yield return ColonyIdle(s,"direct-kit-single-submit");ColonyProbe(StreamHost(ColonyModel(s)),null);Assert.That(ColonyModel(s).Colonisation().Production.Count(x=>x.ColonyId==s.Home),Is.EqualTo(1));Assert.That(doc.rootVisualElement.Q("colonisation-order-confirm"),Is.Null);var accepted=ColonyBytes(ColonyModel(s));StreamCapturedEvent(kit);yield return null;Assert.That(ColonyPending(entry),Is.False);Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(accepted),"disposed prior + cannot enqueue again");
                yield return ColonyWorld(s,doc,s.Home);var queued=ColonyModel(s).Colonisation().Production.Single(x=>x.ColonyId==s.Home).Id;yield return ColonyReview(s,doc,"colonisation-queue-cancel-"+queued);
                var oldCancelConfirm=doc.rootVisualElement.Q<Button>("colonisation-order-confirm");var cancelFrame=ColonyModel(s).Current.Frame;typeof(ReviewEntry).GetMethod("Draw",HullPrivate)!.Invoke(entry,null);yield return null;yield return null;
                Assert.That(ColonyModel(s).Current.Frame,Is.SameAs(cancelFrame));Assert.That(doc.rootVisualElement.Q<Button>("colonisation-order-confirm"),Is.Not.Null,"activity review in the colonisation container survives same-frame redraw");StreamCapturedEvent(oldCancelConfirm);yield return null;Assert.That(ColonyPending(entry),Is.False);Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(accepted));yield return ColonyClick(s,doc,"colonisation-order-back");
                if(s.RosterV2)
                {
                    var unworked=ColonyModel(s).Colonisation().Production.Single(x=>x.Id==queued);Assert.That(unworked.CompletedIndustry,Is.EqualTo(0));
                    var stocks=JsonConvert.SerializeObject(ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home).Stocks);var credits=ColonyModel(s).ColonisationActivities().Credits;
                    yield return ColonyWorld(s,doc,s.Home);yield return ColonyOrder(s,doc,"colonisation-queue-cancel-"+queued);
                    Assert.That(ColonyModel(s).Colonisation().Production.Any(x=>x.Id==queued),Is.False);Assert.That(JsonConvert.SerializeObject(ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home).Stocks),Is.EqualTo(stocks));Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(credits));
                    var combat=ColonyModel(s).ColonisationActivities().BuildOptions.Single(x=>x.Supported&&x.BlueprintRevision==1&&x.HullVersionId=="hull.defiant-combat.v1");
                    yield return ColonyWorld(s,doc,s.Home);yield return ColonyClick(s,doc,"colonisation-build-"+ColonyBuildIndex(ColonyModel(s),combat.DefinitionId));
                    yield return ColonyWorld(s,doc,s.Home);yield return ColonyClick(s,doc,"colonisation-build-kit");Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(credits),"Defiant and kit use ordinary zero-normal-credit construction");
                }
                var kitCredits=ColonyModel(s).ColonisationActivities().Credits;yield return ColonyWorld(s,doc,s.Home);yield return ColonyClick(s,doc,"colonisation-build-kit");Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(kitCredits));
                var constructionCredits=ColonyModel(s).ColonisationActivities().Credits;
                for(int ship=0;ship<2;ship++){yield return ColonyWorld(s,doc,s.Yard);yield return ColonyClick(s,doc,"colonisation-build-"+ColonyBuildIndex(ColonyModel(s),blueprint));}
                Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(constructionCredits));
                Assert.That(ColonyModel(s).Colonisation().Production.Count(x=>x.ColonyId==s.Home),Is.EqualTo(s.RosterV2?3:4));if(s.RosterV2)Assert.That(ColonyModel(s).Colonisation().Production.Count(x=>x.ColonyId==s.Yard),Is.EqualTo(2));
                s.ManufacturedOrders=ColonyModel(s).Colonisation().Production.Where(x=>x.ColonyId==s.Home||x.ColonyId==s.Yard).Select(x=>x.Id).ToArray();Assert.That(s.ManufacturedOrders.Length,Is.EqualTo(s.RosterV2?5:4));yield return ColonySaveContinue(s,doc,"manufacturing-pending");
                CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);root=ColonyBytes(ColonyModel(s));frame=ColonyModel(s).Current.Frame;
                var advance=ColonyStartHold(s,()=>{Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);});yield return ColonyEntered(advance);yield return ColonyBusy(s,doc,advance,root,frame);advance.Release.Set();yield return ColonyIdle(s,"held-advance");ColonyProbe(StreamHost(ColonyModel(s)),null);yield return ColonyResolveCombat(s,doc);Assert.That(ColonyModel(s).Current.Frame.Status.Turn,Is.EqualTo(frame.Status.Turn+1));
                yield return ColonyManufacture(s,doc,2,s.ManufacturedOrders);foreach(var id in s.ManufacturedOrders)Assert.That(ColonyAudit(ColonyModel(s)).ProductionHistory!.Count(x=>x.Completed&&x.Order!.QueueItemId==id),Is.EqualTo(1));var ships=ColonyModel(s).Colonisation().Transports.OrderBy(x=>x.ShipId,StringComparer.Ordinal).ToArray();Assert.That(ships.Length,Is.EqualTo(2));s.Transport=ships[0].ShipId;s.SecondTransport=ships[1].ShipId;Assert.That(ships.All(x=>x.BlueprintRevision==1&&x.PassengerCapacity==100&&x.FoodCapacity==20),Is.True);
                if(s.RosterV2)
                {
                    var combat=ColonyModel(s).ColonisationActivities().BuildOptions.Single(x=>x.Supported&&x.BlueprintRevision==1&&x.HullVersionId=="hull.defiant-combat.v1");
                    s.Escort=ColonyModel(s).Colonisation().OwnShipSources.Single(x=>x.BlueprintId==combat.DefinitionId&&x.Revision==1).ShipId;
                    var produced=ColonyAudit(ColonyModel(s)).PublishedBuiltShips!.Where(x=>x.OwnerCivilizationId==ColonyModel(s).Current.Frame.Actor).ToArray();Assert.That(produced.Length,Is.EqualTo(3));Assert.That(produced.All(x=>x.BlueprintRevision==1),Is.True);
                    yield return ColonyJoinEscort(s,doc);
                }
                yield return ColonySurveyTarget(s,doc);
                yield return ColonyLoadOne(s,doc,s.Transport,true);yield return ColonyLoadOne(s,doc,s.SecondTransport);yield return ColonySaveContinue(s,doc,"loaded-two-conserved-manifests");
                yield return ColonyMove(s,doc,s.Transport,s.TargetNode);yield return ColonyMove(s,doc,s.SecondTransport,s.TargetNode);if(s.RosterV2)yield return ColonyCover(s,doc,"loaded-approach-cover");yield return ColonyDeploy(s,doc,true);
                if(!s.LandingCompleted)yield return ColonyRecoverLanding(s,doc);
                yield return ColonyIncomingRejected(s,doc,s.SecondTransport);s.Founding=ColonyBytes(ColonyModel(s));s.FoundingMission=JsonConvert.SerializeObject(ColonyModel(s).FrontierCommission());yield return ColonySave(s,doc,"founding-branch-point",2);
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)}){yield return ColonyWorld(s,doc,s.TargetWorld);yield return ColonyClick(s,doc,"colonisation-inspect-"+(ColonyBuildIndex(ColonyModel(s),blueprint)+1));yield return ColonyCapture(s,doc,size,"colony-0"+(size.x==1280?5:size.x==1920?6:7)+"-founding-"+size.x+".png",true);}
                yield return ColonyEstablished(s,doc);yield return ColonyTransport(s,doc,s.TargetWorld,s.SecondTransport);var receiving=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld).Population;var incoming=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.SecondTransport).Population;
                yield return ColonyOrder(s,doc,"colonisation-unload");Assert.That(ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld).Population,Is.EqualTo(receiving+incoming));Assert.That(ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.SecondTransport).ManifestId,Is.Null);
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)}){yield return ColonyWorld(s,doc,s.TargetWorld);yield return ColonyCapture(s,doc,size,"colony-"+(size.x==1280?"08":size.x==1920?"09":"10")+"-established-"+size.x+".png");}
                yield return ColonyMove(s,doc,s.Transport,s.HomeNode);yield return ColonyTransport(s,doc,s.Home);doc.rootVisualElement.Q<IntegerField>("colonisation-food").value=5;yield return ColonyReview(s,doc,"colonisation-load-supplies");yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),"colony-11-supply-only-load-1920.png");
                var people=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home).Population;yield return ColonyClick(s,doc,"colonisation-order-confirm");Assert.That(ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport).Kind,Is.EqualTo(SettlerManifestKind.SupplyOnly));Assert.That(ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home).Population,Is.EqualTo(people));
                yield return ColonyTransport(s,doc,s.TargetWorld);yield return ColonyReview(s,doc,"colonisation-deliver",false);
                yield return ColonyMove(s,doc,s.Transport,s.TargetNode);yield return ColonyTransport(s,doc,s.TargetWorld);yield return ColonyOrder(s,doc,"colonisation-deliver");Assert.That(ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport).ManifestId,Is.Null);
                yield return ColonyWorld(s,doc,s.TargetWorld);var credit=ColonyModel(s).ColonisationActivities().Credits;yield return ColonyClick(s,doc,"colonisation-build-"+ColonyBuildIndex(ColonyModel(s),"infrastructure.frontier-mine.v1"));Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(credit));
                var mine=ColonyModel(s).Colonisation().Production.Single(x=>x.ColonyId==s.TargetWorld&&x.DefinitionId=="infrastructure.frontier-mine.v1").Id;
                for(int i=0;i<64&&ColonyModel(s).Colonisation().Production.Any(x=>x.Id==mine);i++)yield return ColonyAdvance(s,doc);Assert.That(ColonyModel(s).Colonisation().Production.Any(x=>x.Id==mine),Is.False);
                var beforeMine=ColonyAudit(ColonyModel(s));yield return ColonyAdvance(s,doc);var afterMine=ColonyAudit(ColonyModel(s));
                long Deposit(CampaignColonisationSaveDto r)=>ColonyGame(r).Planets!.Single(x=>x.PlanetId==s.TargetPlanet).RemainingDeposits!.Sum(x=>x.Amount);
                long Stock(CampaignColonisationSaveDto r)=>ColonyGame(r).Colonies!.Single(x=>x.ColonyId==s.TargetWorld).Stockpiles!.Where(x=>x.ResourceDefinitionId=="resource.dilithium").Sum(x=>x.Amount);
                var extracted=Deposit(beforeMine)-Deposit(afterMine);Assert.That(extracted,Is.InRange(1L,3L));Assert.That(Stock(afterMine)-Stock(beforeMine),Is.EqualTo(extracted));
                yield return ColonyWorld(s,doc,s.TargetWorld);yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),"colony-12-mine-output-1920.png");yield return ColonySaveContinue(s,doc,"supplied-mining-main-branch");s.Established=ColonyBytes(ColonyModel(s));s.EstablishedMission=JsonConvert.SerializeObject(ColonyModel(s).FrontierCommission());
                // Official alternate branch restore, never an injected or combined continuous history.
                yield return ColonyContinue(s,doc,"founding-evacuation-branch-restore",s.Founding!,2,s.FoundingMission);yield return ColonyTransport(s,doc,s.TargetWorld);yield return ColonyOrder(s,doc,"colonisation-evacuate");Assert.That(ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport).Kind,Is.EqualTo(SettlerManifestKind.ReturnOnly));
                yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),"colony-13-restored-founding-evacuation-1920.png");yield return ColonyIncomingRejected(s,doc,s.Transport);yield return ColonyIncomingRejected(s,doc,s.SecondTransport);
                foreach(var ship in new[]{s.Transport,s.SecondTransport}){yield return ColonyMove(s,doc,ship,s.HomeNode);yield return ColonyTransport(s,doc,s.Home,ship);yield return ColonyOrder(s,doc,"colonisation-unload");Assert.That(ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==ship).ManifestId,Is.Null);}
                yield return ColonyNoCohort(s,doc);yield return ColonyWorld(s,doc,s.Home);yield return ColonyClick(s,doc,"colonisation-build-kit");yield return ColonyManufacture(s,doc,2);yield return ColonyLoadOne(s,doc,s.Transport);yield return ColonyMove(s,doc,s.Transport,s.TargetNode);yield return ColonyIncomingRejected(s,doc,s.Transport);yield return ColonyDeploy(s,doc);yield return ColonyEstablished(s,doc);
                var origin=ColonyAudit(ColonyModel(s)).Colonisation!.ColonyOrigins!.Where(x=>x.ColonyId==s.TargetWorld).ToArray();Assert.That(origin.Length,Is.EqualTo(1),"recommission retains original colony identity");yield return ColonySaveContinue(s,doc,"recommissioned-alternate-branch",3);yield return ColonyWorld(s,doc,s.TargetWorld);yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),"colony-14-recommissioned-world-1920.png");yield return ColonyHistoryComponent(s,doc);
                yield return ColonyContinue(s,doc,"main-established-branch-return",s.Established!,1,s.EstablishedMission);yield return ColonyWorld(s,doc,s.TargetWorld);yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),"colony-15-completed-continue-1920.png");
                // Historical Format10 remains its own typed store and loader, never a Format11 fallback.
                var codec=new ReviewNewtonsoftCodec();var fixture=V3Fixture("HOBBYDS9_ACTIVITY_HULL_COMBAT",ActivityHullFixtureSha);var old=codec.DecodeStreamedActivitiesRoot(fixture);var expected=codec.EncodeStreamedActivitiesRoot(old);new ReviewStreamedActivitiesSlotStore(directoryFor(s),codec).Write("generated-3",old,false);
                Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;doc.rootVisualElement.Q<Foldout>("streamed-activities-campaign-menu").value=true;yield return null;ColonyActivate(s,()=>{Click(doc.rootVisualElement.Q<Button>("streamed-activities-campaign-continue-generated-3"));Confirm(doc);});yield return ColonyIdle(s,"separate-historical-format10-continue");Assert.That(ColonyModel(s).ColonisationCampaign,Is.False);Assert.That(StreamBytes(ColonyModel(s)),Is.EqualTo(expected));
                var captures=Directory.GetFiles(CampaignOutput(),"colony-*.png");var names=new[]{"colony-01-catalogue-1920.png","colony-02-survey-review-1920.png","colony-03-embark-review-1920.png","colony-04-held-busy-1920.png","colony-05-founding-1280.png","colony-06-founding-1920.png","colony-07-founding-3440.png","colony-08-established-1280.png","colony-09-established-1920.png","colony-10-established-3440.png","colony-11-supply-only-load-1920.png","colony-12-mine-output-1920.png","colony-13-restored-founding-evacuation-1920.png","colony-14-recommissioned-world-1920.png","colony-15-completed-continue-1920.png"};Assert.That(captures.Select(Path.GetFileName),Is.EquivalentTo(names));File.WriteAllText(Path.Combine(CampaignOutput(),"colony-results.json"),JsonConvert.SerializeObject(new{passed=true,captures=names,realTransportCount=2,realInitialKits=2,conservedPassengers=200,foundingRejectsIncomingPassengers=true,establishedAllowsImmigration=true,supplyOnlyReload=true,finiteMineExtraction=true,mainAndEvacuationAreSeparateOfficialSavedBranches=true,continuousBothBranches=false,emptyOutpostRecommission=true,historyNoLiveTransportComponentOnly=true,destroyedTransportCampaignProof=false,oldFormat10CanonicalWholeRoot=true,aiColonisationPlanner=false,nativeInput=false}));
            }
        }
    }
}
