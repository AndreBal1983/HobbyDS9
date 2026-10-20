#nullable enable
using System;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed class ReviewBattlePanel : VisualElement
    {
        private ReviewBattleStage? stage;
        private readonly Image image;
        private readonly Label caption;
        private float lastTime;
        public ReviewBattlePanel(ReviewCombatFlow flow,Func<VisualElement,string,Action,Button> button,Action<Action> perform)
        {
            name="resolved-battle-presentation";AddToClassList("battle-presentation");
            var playback=flow.Playback!;var pending=flow.PendingPlayback;
            var boundary=pending?.Before??playback.Rounds.LastOrDefault()?.After??playback.Initial;
            image=new Image{name="battle-3d-image",scaleMode=ScaleMode.ScaleToFit};image.AddToClassList("battle-viewport");Add(image);
            caption=new Label{name="battle-playback-caption"};caption.AddToClassList("console-copy");Add(caption);
            try
            {
                // Exact actor-filtered identities are supplied by the receiving projection.
                stage=new ReviewBattleStage(boundary,pending,ReviewBattleVisualBinding.Identity);image.image=stage.Texture;caption.text=stage.Caption;
            }
            catch(Exception)
            {stage?.Dispose();stage=null;caption.text="3D presentation unavailable. The resolved report is still available; playback can be skipped.";}
            var controls=new VisualElement();controls.AddToClassList("review-field-row");Add(controls);
            var pause=button(controls,"Pause / play",()=>{if(stage!=null)stage.Paused=!stage.Paused;});pause.name="battle-playback-pause";pause.SetEnabled(stage!=null&&pending!=null);
            foreach(var speed in new[]{1,2,4})
                button(controls,speed+"×",()=>{if(stage!=null)stage.Speed=speed;}).SetEnabled(stage!=null&&pending!=null);
            button(controls,"Overview / cinematic",()=>stage?.ToggleCamera()).SetEnabled(stage!=null);
            if(pending!=null)
            {
                var skip=button(controls,"Skip round playback",()=>{stage?.Skip();perform(()=>flow.AcknowledgePlayback(playback,pending.Round));});
                skip.name="battle-playback-skip";
                var done=button(controls,"Review round "+pending.Round+" result",()=>perform(()=>flow.AcknowledgePlayback(playback,pending.Round)));
                done.name="battle-playback-done";done.SetEnabled(stage?.Complete??false);
                lastTime=Time.realtimeSinceStartup;
                schedule.Execute(()=>{
                    if(stage==null||panel==null)return;
                    var now=Time.realtimeSinceStartup;stage.Tick(now-lastTime);lastTime=now;
                    caption.text=stage.Caption+(stage.GenericMarkers>0?" · Some models unavailable; generic markers shown":"");
                    done.SetEnabled(stage.Complete);
                }).Every(33);
            }
            Add(new Label("Presentation spacing and timing are schematic. Only resolved events are played; camera, pause and skip do not change combat."){name="battle-presentation-boundary"});
            RegisterCallback<DetachFromPanelEvent>(_=>{image.image=null;stage?.Dispose();stage=null;});
        }
        public static void Statistics(VisualElement parent,CampaignBattlePlayback playback)
        {
            var known=new[]{playback.Initial}.Concat(playback.Rounds.Select(r=>r.After)).SelectMany(b=>b.OwnShips.Concat(b.VisibleEnemies))
                .GroupBy(c=>c.ShipId).Select(g=>g.First()).OrderBy(c=>c.ShipId,StringComparer.Ordinal).ToList();
            string Ship(string id){var index=known.FindIndex(c=>c.ShipId==id);return index<0?"Observed ship":known[index].Type+" "+(index+1);}
            var title=new Label("Battle statistics · "+(playback.Complete?"complete":"resolved rounds only")+" · turn "+playback.Turn);
            title.AddToClassList("console-section");parent.Add(title);
            parent.Add(new Label("Damage columns show damage received. Enemy figures include observed events only; reserves remain private."));
            foreach(var item in playback.Statistics)
            {
                var row=new Label((item.Own?"Own":"Observed enemy")+" · "+Ship(item.ShipId)+"\n"+
                    "Shots "+item.ShotsObserved+" · hits "+item.HitsObserved+" · misses "+item.MissesObserved+
                    "\nHull damage received "+item.HullDamageObserved+" · shield damage received "+item.ShieldDamageObserved+
                    (item.DestroyedObserved?" · destroyed":""));
                row.AddToClassList("console-queue");parent.Add(row);
            }
            foreach(var ship in playback.OwnFinalCondition)
            {
                var c=ship.Condition;var row=new Label(Ship(c.ShipId)+" · retained hull "+c.Hull+" / "+c.MaxHull+
                    "\nFore / Port / Starboard / Aft: "+string.Join(" / ",c.Shields)+
                    "\n"+string.Join(" · ",ship.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo).Select(w=>"Torpedoes "+w.Ammunition+" / "+w.MagazineCapacity)));
                row.AddToClassList("console-queue");parent.Add(row);
            }
        }
    }
}
