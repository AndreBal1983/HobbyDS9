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
        public LcarsPowerSlider(string caption,string fieldName,long value,long maximum,bool invalid,Action<long> commit,long poweredMinimum=0)
        {
            if(maximum<0 || maximum>int.MaxValue)throw new ArgumentOutOfRangeException(nameof(maximum));
            this.maximum=maximum;this.commit=commit;backendInvalid=invalid;Value=committed=value;
            name="slider-"+fieldName;AddToClassList("lcars-power-slider");
            var heading=new VisualElement();heading.AddToClassList("lps-heading");Add(heading);
            heading.Add(new Label(caption.ToUpperInvariant()));readout.AddToClassList("lps-readout");heading.Add(readout);
            var body=new VisualElement();body.AddToClassList("lps-body");Add(body);
            var rail=new VisualElement();rail.AddToClassList("lps-rail");body.Add(rail);
            var bars=new VisualElement {pickingMode=PickingMode.Ignore};bars.AddToClassList("lps-segments");rail.Add(bars);
            // Index zero is the bottom segment. USS column-reverse matches the real
            // non-inverted vertical SliderInt: zero at bottom, full demand at top.
            for(var i=0;i<segments.Length;i++)
            {
                segments[i]=new VisualElement {name="segment-"+i,pickingMode=PickingMode.Ignore};
                segments[i].AddToClassList("lps-segment");
                segments[i].AddToClassList(i<6?"lps-band-low":i<10?"lps-band-middle":"lps-band-high");
                bars.Add(segments[i]);
            }
            Track=new SliderInt(0,(int)maximum) {name="track-"+fieldName,direction=SliderDirection.Vertical,inverted=false};Track.AddToClassList("lps-track");rail.Add(Track);
            Track.SetEnabled(maximum>0);Track.SetValueWithoutNotify((int)Math.Max(0,Math.Min(maximum,value)));
            rail.RegisterCallback<PointerDownEvent>(_=>dragging=true,TrickleDown.TrickleDown);
            Track.RegisterValueChangedCallback(e=>{Value=e.newValue;Paint();if(!dragging)Commit();});
            rail.RegisterCallback<PointerUpEvent>(_=>{dragging=false;Paint();Commit();},TrickleDown.TrickleDown);
            Track.RegisterCallback<PointerCaptureOutEvent>(_=>{if(dragging){dragging=false;Paint();Commit();}});
            RegisterCallback<DetachFromPanelEvent>(_=>dragging=false);
            rail.RegisterCallback<KeyDownEvent>(e=>
            {
                var delta=e.keyCode==KeyCode.LeftArrow || e.keyCode==KeyCode.DownArrow?-1:e.keyCode==KeyCode.RightArrow || e.keyCode==KeyCode.UpArrow?1:0;
                if(delta!=0 || e.keyCode==KeyCode.Home || e.keyCode==KeyCode.End)
                {
                    Value=e.keyCode==KeyCode.Home?0:e.keyCode==KeyCode.End?maximum:Math.Max(0,Math.Min(maximum,Math.Max(0,Math.Min(maximum,Value))+delta));
                    Track.SetValueWithoutNotify((int)Value);Paint();Commit();e.StopImmediatePropagation();
                }
            },TrickleDown.TrickleDown);
            // Editor panels may synthesize navigation from an arrow KeyDown, while the
            // native player sends only KeyDown. Consume navigation so arrows step once
            // in both hosts; keyboard editing belongs to the handler above.
            rail.RegisterCallback<NavigationMoveEvent>(e=>
            {
                var delta=e.direction==NavigationMoveEvent.Direction.Left || e.direction==NavigationMoveEvent.Direction.Down?-1:e.direction==NavigationMoveEvent.Direction.Right || e.direction==NavigationMoveEvent.Direction.Up?1:0;
                if(delta==0)return;
                e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            var bounds=new VisualElement();bounds.AddToClassList("lps-bounds");body.Add(bounds);
            bounds.Add(new Label("FULL "+maximum+" units"));
            bounds.Add(new Label("Powered min "+poweredMinimum));
            Exact=new LongField("Exact units") {name=fieldName,isDelayed=true};Exact.SetValueWithoutNotify(value);bounds.Add(Exact);
            Exact.RegisterValueChangedCallback(e=>{Value=e.newValue;Track.SetValueWithoutNotify((int)Math.Max(0,Math.Min(maximum,Value)));Paint();Commit();});
            bounds.Add(new Label("0 units / bottom"));
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
            notice.text=dragging?"EDIT PREVIEW • release to validate":
                PositionState=="invalid"?"! OUTSIDE RANGE • exact value retained":
                backendInvalid?"! BACKEND ERROR • see validation":
                maximum==0?"NO POWER DEMAND • track disabled":Value==0?"ZERO • backend validation applies":
                "POSITION "+PositionState.ToUpperInvariant()+" • not legality";
        }
    }
}
