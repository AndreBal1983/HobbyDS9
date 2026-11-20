#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewColonisationPanel
    {
        internal enum BodySelectionKind { None,Planet,Belt }
        internal readonly struct BodySelectionKey
        {
            internal readonly BodySelectionKind Kind;
            private readonly string? id;
            internal string Id=>id??"";
            internal BodySelectionKey(BodySelectionKind kind,string id){Kind=kind;this.id=id;}
        }
        private BodySelectionKey selectedBody;
        private string? inspectedSystemId;
        private readonly BeltBodiesSnapshot? beltBodies;
        private readonly IReadOnlyDictionary<string,string>? disclosedSystemNames;
        private readonly bool supportsBeltBodies;
        private readonly Action<BeltBodyCommandDto,string>? reviewBelt;
        private string beltSurveyShip="",beltSponsor="";
        private IEnumerable<BeltBodySnapshot> KnownBelts=>beltBodies?.Bodies??Array.Empty<BeltBodySnapshot>();
        private bool KnownBody(BodySelectionKey key)=>key.Kind==BodySelectionKind.Planet?planets.Any(p=>p.PlanetId==key.Id)
            :key.Kind==BodySelectionKind.Belt&&KnownBelts.Any(b=>b.BodyId==key.Id);
        private string[] DisclosedBodySystems()=>planets.Select(p=>p.SystemId).Concat(KnownBelts.Select(b=>b.SystemId)).Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        private string BodySystemName(string id)=>disclosedSystemNames!=null&&disclosedSystemNames.TryGetValue(id,out var name)?name:
            "System containing "+(planets.FirstOrDefault(p=>p.SystemId==id)?.DisplayName??KnownBelts.FirstOrDefault(b=>b.SystemId==id)?.DisplayName??"disclosed body");
        private BeltBodyCommandDto BeltCommand(BeltBodyAction action,BeltBodySnapshot target)=>new BeltBodyCommandDto
        {ActorId=observation.Actor,ExpectedTurn=observation.Turn,ExpectedRevision=observation.Revision,CommandId="command.belt.ui."+Guid.NewGuid().ToString("N"),Action=action,BodyId=target.BodyId};
        private void AskBelt(BeltBodyCommandDto command,string caption){if(Live)reviewBelt?.Invoke(command,caption);}
        private void FocusBelt(BeltBodySnapshot target,VisualElement details,int epoch)
        {
            if(!Live||epoch!=controlEpoch||!Contains(details)||!KnownBelts.Any(b=>ReferenceEquals(b,target)))return;
            selectedBody=new BodySelectionKey(BodySelectionKind.Belt,target.BodyId);inspectedSystemId=target.SystemId;
            foreach(var card in this.Query<PlanetBodyCard>().ToList())card.SetSelected(false);
            solOverview?.ClearSelection();
            foreach(var row in this.Query<Button>(className:"system-belt-body").ToList())row.EnableInClassList("selected-nav",row.name=="belt-body-card-"+target.BodyId);
            details.Clear();BeltDetails(details,target);
        }
        private void BeltButtons(VisualElement parent,IEnumerable<BeltBodySnapshot> rows,VisualElement details)
        {
            var epoch=controlEpoch;
            foreach(var target in rows.OrderBy(b=>b.BodyId,StringComparer.Ordinal))
            {
                var captured=target;
                var button=Button(parent,target.DisplayName+"\nAsteroid belt · "+(target.Surveyed?"Surveyed":"Survey required"),"belt-body-card-"+target.BodyId,()=>FocusBelt(captured,details,epoch));
                button.AddToClassList("system-belt-body");button.style.minHeight=64;button.style.minWidth=170;button.style.maxWidth=280;
                button.style.whiteSpace=WhiteSpace.Normal;
                button.EnableInClassList("selected-nav",selectedBody.Kind==BodySelectionKind.Belt&&selectedBody.Id==target.BodyId);
                button.tooltip="One belt aggregate. Selecting it changes no orders, work or resources.";
            }
        }
        private void BeltOnlySystem(VisualElement parent,string system)
        {
            var details=new VisualElement{name="colonisation-belt-focus"};
            var choices=Row(parent);choices.name="colonisation-system-bodies";
            var belts=KnownBelts.Where(b=>b.SystemId==system).ToArray();
            var selected=belts.FirstOrDefault(b=>selectedBody.Kind==BodySelectionKind.Belt&&b.BodyId==selectedBody.Id)??belts.FirstOrDefault();
            if(selected!=null)selectedBody=new BodySelectionKey(BodySelectionKind.Belt,selected.BodyId);
            BeltButtons(choices,belts,details);parent.Add(details);
            if(selected!=null)BeltDetails(details,selected);
            else Text(parent,supportsBeltBodies?"Body information is temporarily unavailable.":"No celestial bodies have been disclosed here.");
        }
        private void BeltDetails(VisualElement parent,BeltBodySnapshot target)
        {
            var root=new VisualElement{name="belt-body-details"};root.AddToClassList("world-info-compact");parent.Add(root);
            var sheet=Resources.Load<StyleSheet>("WorldInfoCompact");if(sheet!=null)root.styleSheets.Add(sheet);
            WorldLabel(root,target.DisplayName,"belt-body-name","world-info-title");
            var identity=WorldGrid(root,"belt-body-identity");
            WorldChip(identity,target.BodyKind,"belt-body-kind","One aggregate belt; decorative rock instances have no gameplay identity.");
            WorldChip(identity,target.CurrentlyObserved?"Current presence":"Retained observation","belt-body-presence","Existence does not reveal unobserved deposits.",!target.CurrentlyObserved);
            WorldChip(identity,BeltBodyPresentation.SurveyStamp(target),"belt-body-survey-stamp","Survey work belongs to an actual assigned ship.",!target.Surveyed);
            var resources=WorldGrid(root,"belt-body-resources");
            var habitat=new VisualElement{name="belt-habitability"};habitat.AddToClassList("world-info-stat");resources.Add(habitat);
            WorldLabel(habitat,"Unassisted habitability","belt-habitability-label","world-info-stat-label");
            WorldLabel(habitat,BeltBodyPresentation.Habitability(target),"belt-habitability-value","world-info-stat-value");
            habitat.tooltip=target.HabitabilityBasis??"Habitability is undisclosed until survey. This is not settlement permission.";
            WorldStat(resources,"belt-ore","Observed remaining · "+BeltBodyPresentation.Resource(target),target.ObservedRemaining,"units",ReviewIconKind.Construction);
            WorldChip(root,BeltBodyPresentation.ResourceStamp(target),"belt-resource-stamp","Initial deposits are not inferred from an old remaining-resource report.",!target.ResourceIsCurrent);
            WorldChip(root,"Settlement unavailable","belt-settlement-status",target.SettlementReason,true);
            var epoch=controlEpoch;
            bool Current()=>Live&&epoch==controlEpoch&&Contains(root)&&selectedBody.Kind==BodySelectionKind.Belt
                &&selectedBody.Id==target.BodyId&&KnownBelts.Any(b=>ReferenceEquals(b,target));
            if(target.Mine is BeltMineSnapshot mine)
            {
                var sponsor=observation.Worlds.FirstOrDefault(w=>w.Id==mine.ColonyId)?.Name??"Original sponsor unavailable";
                var summary=WorldGrid(root,"belt-mining-status");
                WorldChip(summary,"Mine · "+mine.Stage,"belt-mine-stage","Owned mining record · week "+observation.Turn);
                WorldChip(summary,"Recipient · "+sponsor,"belt-mine-recipient","Standing delivery remains bound to this sponsor.");
                WorldStat(summary,"belt-mine-work","Construction work",mine.CompletedWork,"/ "+mine.Work+" Industry",ReviewIconKind.Construction);
                WorldStat(summary,"belt-mine-stock","Mine storage",mine.LocalStock,"/ "+mine.StorageCapacity+" units",ReviewIconKind.Construction);
                WorldStat(summary,"belt-mine-extracted","Total extracted",mine.TotalExtracted,"units",ReviewIconKind.Construction);
                WorldStat(summary,"belt-mine-delivered","Total delivered",mine.TotalDelivered,"units",ReviewIconKind.Construction);
                var history=new Foldout{text="Mining operation details",value=false,name="belt-operation-details"};root.Add(history);
                Text(history,"Standing delivery up to "+BeltMiningPolicy.WeeklyTransferLimit+" units/week to "+sponsor+" through remaining shared freight capacity.");
                Text(history,"Last extraction · "+(mine.LastExtractionTurn.HasValue?"week "+mine.LastExtractionTurn.Value:"none recorded")+" · Last delivery · "+(mine.LastDeliveryTurn.HasValue?"week "+mine.LastDeliveryTurn.Value:"none recorded"));
                if(!string.IsNullOrWhiteSpace(mine.LatestReport))Text(history,mine.LatestReport);
                if(mine.Stage==BeltMineStage.Queued)
                    Button(root,"Review cancel mine","belt-mine-cancel",()=>{if(Current()){var c=BeltCommand(BeltBodyAction.CancelMine,target);c.MineId=mine.InstallationId;AskBelt(c,"Cancel mining at "+target.DisplayName);}}).SetEnabled(reviewBelt!=null&&Live);
            }
            else if(target.Surveyed)
            {
                var sponsors=observation.Worlds.Where(w=>target.SponsorColonyIds.Contains(w.Id,StringComparer.Ordinal)).OrderBy(w=>w.Name,StringComparer.Ordinal).ToArray();
                if(!sponsors.Any(w=>w.Id==beltSponsor))beltSponsor=sponsors.FirstOrDefault()?.Id??"";
                if(sponsors.Length>0)Picker(root,"Mining sponsor",sponsors,w=>w.Id,w=>w.Name,beltSponsor,id=>{if(Current())beltSponsor=id;},"belt-mine-sponsor");
                var enqueue=Button(root,"Review mining installation","belt-mine-enqueue",()=>{if(Current()){var c=BeltCommand(BeltBodyAction.EnqueueMine,target);c.ColonyId=beltSponsor;AskBelt(c,"Build a mining installation at "+target.DisplayName);}});
                enqueue.SetEnabled(reviewBelt!=null&&Live&&sponsors.Length>0);
                if(sponsors.Length==0)Text(root,"No eligible established sponsor in this system.");
            }
            if(target.ActiveSurveyId!=null)
            {
                Button(root,"Review cancel belt survey","belt-survey-cancel",()=>{if(Current()){var c=BeltCommand(BeltBodyAction.CancelSurvey,target);c.SurveyId=target.ActiveSurveyId;AskBelt(c,"Cancel survey of "+target.DisplayName);}}).SetEnabled(reviewBelt!=null&&Live);
            }
            else if(!target.Surveyed)
            {
                var ships=activities.Ships.Where(s=>target.SurveyShipIds.Contains(s.ShipId,StringComparer.Ordinal)).OrderBy(s=>s.DisplayName,StringComparer.Ordinal).ToArray();
                if(!ships.Any(s=>s.ShipId==beltSurveyShip))beltSurveyShip=ships.FirstOrDefault()?.ShipId??"";
                if(ships.Length>0)Picker(root,"Survey ship",ships,s=>s.ShipId,s=>s.DisplayName,beltSurveyShip,id=>{if(Current())beltSurveyShip=id;},"belt-survey-ship");
                var survey=Button(root,"Review belt survey","belt-survey-start",()=>{if(Current()){var c=BeltCommand(BeltBodyAction.StartSurvey,target);c.ShipId=beltSurveyShip;AskBelt(c,"Survey "+target.DisplayName);}});
                survey.SetEnabled(reviewBelt!=null&&Live&&ships.Length>0);
                if(ships.Length==0)Text(root,"No eligible stationary study ship in this system.");
            }
        }
    }
}
