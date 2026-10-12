#nullable enable
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    public sealed class LcarsPowerSlider : VisualElement
    {
        public SliderInt Track { get; }
        public LongField Exact { get; }
        public long Value { get; private set; }
        public string PositionState { get; private set; }="zero";
        private readonly Label readout=new Label(), notice=new Label();
        private readonly VisualElement[] segments=new VisualElement[12];
        private readonly Action<long> commit;
        private readonly long maximum;
        private readonly bool backendInvalid;
        private bool dragging;
        private long committed;
        public LcarsPowerSlider(string caption,string fieldName,long value,long maximum,bool invalid,Action<long> commit)
        {
            if(maximum<0 || maximum>int.MaxValue)throw new ArgumentOutOfRangeException(nameof(maximum));
            this.maximum=maximum;this.commit=commit;backendInvalid=invalid;Value=committed=value;
            name="slider-"+fieldName;AddToClassList("lcars-power-slider");
            var heading=new VisualElement();heading.AddToClassList("lps-heading");Add(heading);
            heading.Add(new Label(caption.ToUpperInvariant()+" / POWER"));heading.Add(readout);
            var rail=new VisualElement();rail.AddToClassList("lps-rail");Add(rail);
            var bars=new VisualElement {pickingMode=PickingMode.Ignore};bars.AddToClassList("lps-segments");rail.Add(bars);
            for(var i=0;i<segments.Length;i++){segments[i]=new VisualElement {pickingMode=PickingMode.Ignore};segments[i].AddToClassList("lps-segment");bars.Add(segments[i]);}
            Track=new SliderInt(0,(int)maximum) {name="track-"+fieldName};Track.AddToClassList("lps-track");rail.Add(Track);
            Track.SetEnabled(maximum>0);Track.SetValueWithoutNotify((int)Math.Max(0,Math.Min(maximum,value)));
            rail.RegisterCallback<PointerDownEvent>(_=>dragging=true,TrickleDown.TrickleDown);
            Track.RegisterValueChangedCallback(e=>{Value=e.newValue;Paint();if(!dragging)Commit();});
            rail.RegisterCallback<PointerUpEvent>(_=>{dragging=false;Commit();},TrickleDown.TrickleDown);
            Track.RegisterCallback<PointerCaptureOutEvent>(_=>{if(dragging){dragging=false;Commit();}});
            RegisterCallback<DetachFromPanelEvent>(_=>dragging=false);
            rail.RegisterCallback<KeyDownEvent>(e=>
            {
                if(e.keyCode==KeyCode.Home || e.keyCode==KeyCode.End)
                {
                    Value=e.keyCode==KeyCode.Home?0:maximum;Track.SetValueWithoutNotify((int)Value);Paint();Commit();e.StopImmediatePropagation();
                }
            },TrickleDown.TrickleDown);
            // Arrow keys become navigation events in UI Toolkit. Handle that event once,
            // rather than also adjusting KeyDown and accidentally stepping twice.
            rail.RegisterCallback<NavigationMoveEvent>(e=>
            {
                var delta=e.direction==NavigationMoveEvent.Direction.Left || e.direction==NavigationMoveEvent.Direction.Down?-1:e.direction==NavigationMoveEvent.Direction.Right || e.direction==NavigationMoveEvent.Direction.Up?1:0;
                if(delta==0)return;
                Value=Math.Max(0,Math.Min(maximum,Math.Max(0,Math.Min(maximum,Value))+delta));
                Track.SetValueWithoutNotify((int)Value);Paint();Commit();e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            var bounds=new VisualElement();bounds.AddToClassList("lps-bounds");Add(bounds);
            bounds.Add(new Label("0 units"));bounds.Add(new Label(maximum+" units • step 1"));
            Exact=new LongField("Exact units") {name=fieldName,isDelayed=true};Exact.SetValueWithoutNotify(value);Add(Exact);
            Exact.RegisterValueChangedCallback(e=>{Value=e.newValue;Track.SetValueWithoutNotify((int)Math.Max(0,Math.Min(maximum,Value)));Paint();Commit();});
            notice.AddToClassList("lps-notice");Add(notice);Paint();
        }
        private void Commit(){if(Value==committed)return;committed=Value;commit(Value);}
        private void Paint()
        {
            var fraction=maximum==0?0:Math.Max(0,Math.Min(1,(double)Value/maximum));
            PositionState=Value<0 || Value>maximum?"invalid":maximum==0?"disabled":Value==0?"zero":fraction>=.85?"high":fraction>=.5?"medium":"low";
            foreach(var state in new[]{"invalid","disabled","zero","high","medium","low"})EnableInClassList("lps-"+state,state==PositionState);
            EnableInClassList("lps-backend-invalid",backendInvalid && !dragging);
            for(var i=0;i<segments.Length;i++)segments[i].EnableInClassList("lps-filled",fraction>=(double)(i+1)/segments.Length);
            readout.text=Value.ToString(CultureInfo.InvariantCulture)+" units";
            Exact.SetValueWithoutNotify(Value);
            notice.text=dragging?"EDIT PREVIEW • release to validate; results below show the committed draft":
                PositionState=="invalid"?"OUTSIDE TRACK RANGE • exact value retained; backend validation applies":
                backendInvalid?"BACKEND ERROR • review allocation / profile messages below":
                maximum==0?"NO POWER DEMAND • track disabled; exact input retained":Value==0?"ZERO • backend determines whether this system may be unpowered":
                "POSITION "+PositionState.ToUpperInvariant()+" • fill is demand fraction, not legality";
        }
    }
}
