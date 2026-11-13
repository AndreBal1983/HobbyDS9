#nullable enable
using System;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    // One completed-click chain for the mounted System view, shared by scene and list.
    public sealed class PlanetActivationScope
    {
        private VisualElement? first;
        private int pointer;
        public PlanetActivationScope(VisualElement owner)
        {
            owner.RegisterCallback<PointerDownEvent>(e=>{
                var target=e.target as VisualElement;
                while(target!=null&&!target.ClassListContains("planet-activation-target"))target=target.parent;
                if(e.button!=0||!ReferenceEquals(target,first))Cancel();
            },TrickleDown.TrickleDown);
            owner.RegisterCallback<WheelEvent>(_=>Cancel(),TrickleDown.TrickleDown);
            owner.RegisterCallback<PointerCancelEvent>(_=>Cancel(),TrickleDown.TrickleDown);
            owner.RegisterCallback<DetachFromPanelEvent>(e=>{if(ReferenceEquals(e.target,owner))Cancel();});
        }
        internal void Cancel(){first=null;pointer=-1;}
        internal void Cancel(VisualElement element){if(ReferenceEquals(first,element))Cancel();}
        internal void Remember(VisualElement element,int pointerId){first=element;pointer=pointerId;}
        internal bool Complete(VisualElement element,int pointerId)
        {var same=ReferenceEquals(first,element)&&pointer==pointerId;Cancel();return same;}
    }

    // Stable, mounted hit target shared by the compact list and scene markers.
    public sealed class PlanetBodyCard : VisualElement
    {
        private readonly Color colonyRail;
        private bool selected,keyboardFocus;
        public PlanetBodyCard(ColonisationPlanetSnapshot planet,string? ownStage,bool selected,Action focus,Action open,PlanetActivationScope? activation=null)
        {
            name="colonisation-system-body-"+planet.BodyId;focusable=true;tabIndex=0;this.selected=selected;
            colonyRail=ownStage==null?new Color(.40f,.47f,.55f):new Color(.30f,.66f,.50f);
            style.width=184;style.maxWidth=Length.Percent(100);style.minHeight=80;style.flexShrink=0;
            style.marginRight=6;style.marginBottom=6;style.paddingLeft=8;style.paddingRight=6;style.paddingTop=6;style.paddingBottom=6;
            style.borderLeftWidth=4;style.borderLeftColor=colonyRail;
            style.borderTopWidth=style.borderBottomWidth=style.borderRightWidth=2;
            var label=new Label(planet.DisplayName){pickingMode=PickingMode.Ignore};label.style.whiteSpace=WhiteSpace.Normal;label.style.unityFontStyleAndWeight=FontStyle.Bold;Add(label);
            Add(new Label(ownStage==null?"? Colony unknown":"⌂ "+ownStage){pickingMode=PickingMode.Ignore});
            Add(new Label(planet.ActiveSurveyId!=null?"Survey in progress · "+planet.CompletedSurveyWeeks+" weeks":planet.Surveyed?"Environment known":"Not surveyed"){pickingMode=PickingMode.Ignore});
            tooltip="Click / Space to focus · Double-click / Enter to open "+planet.DisplayName;
            BindActivation(this,focus,open,activation);
            RegisterCallback<FocusInEvent>(_=>{keyboardFocus=true;Refresh();});
            RegisterCallback<FocusOutEvent>(_=>{keyboardFocus=false;Refresh();});Refresh();
        }
        internal static void BindActivation(VisualElement element,Action focus,Action open,PlanetActivationScope? activation=null)
        {
            var chain=activation??new PlanetActivationScope(element);
            element.AddToClassList("planet-activation-target");
            element.focusable=true;element.tabIndex=0;
            element.RegisterCallback<ClickEvent>(e=>{
                if(e.button!=0||!element.enabledInHierarchy||element.panel==null)return;
                var complete=e.clickCount==2&&chain.Complete(element,e.pointerId);
                if(e.clickCount!=1)chain.Cancel();
                element.Focus();focus();
                if(e.clickCount==1)chain.Remember(element,e.pointerId);
                else if(complete)open();
                e.StopPropagation();
            });
            element.RegisterCallback<NavigationSubmitEvent>(e=>{if(!element.enabledInHierarchy||element.panel==null)return;chain.Cancel();focus();open();e.StopPropagation();});
            element.RegisterCallback<KeyDownEvent>(e=>{chain.Cancel();if(e.keyCode!=KeyCode.Space||!element.enabledInHierarchy||element.panel==null)return;focus();e.StopPropagation();});
            // Focus transfer can finish after the next target's click callback.
            // A retired target may cancel only its own pending chain.
            element.RegisterCallback<FocusOutEvent>(_=>chain.Cancel(element));
            element.RegisterCallback<DetachFromPanelEvent>(e=>{if(ReferenceEquals(e.target,element))chain.Cancel(element);});
        }
        public void SetSelected(bool value){selected=value;Refresh();}
        private void Refresh()
        {
            var edge=keyboardFocus?new Color(.88f,.84f,1):selected?new Color(.95f,.72f,.39f):new Color(.17f,.25f,.34f);
            style.borderTopColor=style.borderBottomColor=style.borderRightColor=edge;style.borderLeftColor=colonyRail;
            style.backgroundColor=selected?new Color(.12f,.14f,.19f):new Color(.035f,.065f,.09f);
        }
    }
}
