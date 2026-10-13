#nullable enable
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    // Rendering scale only, never gameplay budgets or validation.
    public sealed class LcarsPowerScale
    {
        public const int SafeSegmentLimit=48;
        public const float Pitch=6, ZeroHeight=14;
        public float SegmentPitch { get; }
        public bool Paired { get; }
        public long UnitsPerBar { get; }
        public int TallestCount { get; }
        public string Legend=>Paired?"2 bars / power":"1 bar = "+UnitsPerBar+" power • approximate; exact values below";
        public LcarsPowerScale(long largestMaximum,float availableHeight=288,float segmentPitch=Pitch)
        {
            if(largestMaximum<0)throw new ArgumentOutOfRangeException(nameof(largestMaximum));
            if(float.IsNaN(segmentPitch)||float.IsInfinity(segmentPitch)||segmentPitch<4||segmentPitch>12)throw new ArgumentOutOfRangeException(nameof(segmentPitch));
            SegmentPitch=segmentPitch;
            var limit=Math.Max(16,Math.Min(SafeSegmentLimit,(int)(Math.Max(96,Math.Min(288,availableHeight))/SegmentPitch)));
            Paired=largestMaximum<=limit/2;UnitsPerBar=Paired?1:Ceiling(largestMaximum,limit);
            TallestCount=Count(largestMaximum);
        }
        private static long Ceiling(long value,long divisor)=>value/divisor+(value%divisor==0?0:1);
        public int Count(long maximum)
        {
            if(maximum<0)throw new ArgumentOutOfRangeException(nameof(maximum));
            var count=Paired?(maximum>SafeSegmentLimit/2?(long)SafeSegmentLimit+1:maximum*2):Ceiling(maximum,UnitsPerBar);
            if(count>SafeSegmentLimit)throw new ArgumentException("Range must belong to this bank's common scale",nameof(maximum));
            return (int)count;
        }
        public long SegmentValue(int index,long maximum)
        {
            if(maximum==0)return 0;
            index=Math.Max(0,Math.Min(Count(maximum)-1,index));
            if(Paired)return Math.Min(maximum,index/2+1);
            var bars=(long)index+1;
            return bars>maximum/UnitsPerBar?maximum:bars*UnitsPerBar;
        }
        public int Filled(long value,long maximum)
        {
            value=Math.Max(0,Math.Min(maximum,value));
            if(value==maximum)return Count(maximum);
            return Paired?(int)(value*2):(int)(value/UnitsPerBar);
        }
    }

    // The lights capture pointer input directly; no SliderInt or hidden fader.
    public sealed class LcarsPowerSlider : VisualElement
    {
        public VisualElement Track { get; }
        public LongField Exact { get; }
        public long Value { get; private set; }
        public long Maximum { get; }
        public LcarsPowerScale Scale { get; }
        public string PositionState { get; private set; }="zero";
        public bool Pending=>pointer>=0;
        private readonly Label readout=new Label(), notice=new Label();
        private readonly VisualElement[] segments;
        private readonly Action<long> commit;
        private readonly bool backendInvalid;
        private int pointer=-1;
        private float pointerOriginY;
        private long committed;
        public LcarsPowerSlider(string caption,string fieldName,long value,long maximum,bool invalid,Action<long> commit,long poweredMinimum=0,LcarsPowerScale? scale=null)
        {
            if(maximum<0)throw new ArgumentOutOfRangeException(nameof(maximum));
            Maximum=maximum;this.commit=commit;backendInvalid=invalid;Value=committed=value;Scale=scale??new LcarsPowerScale(maximum);
            name="slider-"+fieldName;AddToClassList("lcars-power-slider");
            Add(new Label(caption.StartsWith("Requested",StringComparison.Ordinal)?"R":"A") {tooltip=caption});
            var stage=new VisualElement();stage.AddToClassList("lps-stage");stage.style.height=Scale.TallestCount*Scale.SegmentPitch+LcarsPowerScale.ZeroHeight;Add(stage);
            Track=new VisualElement {name="track-"+fieldName,focusable=true,tabIndex=0,tooltip=caption+" power. Tap lights; arrows ±1, Home zero, End full. Powered minimum "+poweredMinimum};
            Track.AddToClassList("lps-track");Track.style.height=Scale.Count(maximum)*Scale.SegmentPitch+LcarsPowerScale.ZeroHeight;stage.Add(Track);Track.SetEnabled(maximum>0);
            var bars=new VisualElement {pickingMode=PickingMode.Ignore};bars.AddToClassList("lps-segments");Track.Add(bars);
            segments=new VisualElement[Scale.Count(maximum)];
            for(var i=0;i<segments.Length;i++)
            {
                var segment=new VisualElement {name="segment-"+i,pickingMode=PickingMode.Ignore};segments[i]=segment;segment.AddToClassList("lps-segment");
                segment.style.height=Scale.SegmentPitch-2;segment.style.minHeight=Scale.SegmentPitch-2;
                segment.AddToClassList(i<segments.Length*.42?"lps-band-low":i<segments.Length*.75?"lps-band-middle":"lps-band-high");bars.Add(segment);
            }
            var zero=new Label("0") {pickingMode=PickingMode.Ignore};zero.AddToClassList("lps-zero-strip");Track.Add(zero);
            Track.RegisterCallback<PointerDownEvent>(e=>
            {
                if(e.button!=0 || pointer>=0 || Maximum==0)return;
                // Freeze the panel-space origin before FocusIn reparents the
                // exact editor. Native event-local coordinates can change as
                // dispatch/focus dirties transforms; panel positions stay stable.
                pointerOriginY=Track.worldBound.y;
                var y=e.position.y-pointerOriginY;
                Track.Focus();pointer=e.pointerId;Track.CapturePointer(pointer);Scrub(y);e.StopPropagation();
            });
            Track.RegisterCallback<PointerMoveEvent>(e=>{if(e.pointerId==pointer){Scrub(e.position.y-pointerOriginY);e.StopPropagation();}});
            Track.RegisterCallback<PointerUpEvent>(e=>
            {
                if(e.pointerId!=pointer)return;
                Scrub(e.position.y-pointerOriginY);Finish();e.StopPropagation();
            });
            Track.RegisterCallback<PointerCaptureOutEvent>(e=>{if(e.pointerId==pointer)Finish();});
            Track.RegisterCallback<PointerCancelEvent>(e=>{if(e.pointerId==pointer)Finish();});
            RegisterCallback<DetachFromPanelEvent>(_=>{pointer=-1;Value=committed;Paint();});
            Track.RegisterCallback<KeyDownEvent>(e=>
            {
                if(Pending)return;
                var delta=e.keyCode==KeyCode.LeftArrow || e.keyCode==KeyCode.DownArrow?-1:e.keyCode==KeyCode.RightArrow || e.keyCode==KeyCode.UpArrow?1:0;
                if(delta==0 && e.keyCode!=KeyCode.Home && e.keyCode!=KeyCode.End)return;
                var bounded=Math.Max(0,Math.Min(Maximum,Value));
                Value=e.keyCode==KeyCode.Home?0:e.keyCode==KeyCode.End?Maximum:delta>0?(bounded<Maximum?bounded+1:Maximum):(bounded>0?bounded-1:0);
                Paint();Commit();e.StopImmediatePropagation();
            });
            Track.RegisterCallback<NavigationMoveEvent>(e=>{if(e.direction!=NavigationMoveEvent.Direction.None)e.StopImmediatePropagation();});
            readout.AddToClassList("lps-readout");Add(readout);notice.AddToClassList("lps-notice");Add(notice);
            var storage=new VisualElement();storage.AddToClassList("lps-exact-store");Add(storage);
            Exact=new LongField(caption+" / exact power") {name=fieldName,isDelayed=true};Exact.SetValueWithoutNotify(value);storage.Add(Exact);
            Exact.RegisterValueChangedCallback(e=>{Value=e.newValue;Paint();Commit();});Paint();
        }
        public long ValueAtLocalY(float y)
        {
            if(Maximum==0)return 0;
            // Matches the explicitly sized stack. Do not query a layout rect
            // while focus/detail/preview updates have invalidated that layout.
            var fromBottom=Scale.Count(Maximum)*Scale.SegmentPitch-y;
            if(fromBottom<0)return 0;
            return Scale.SegmentValue((int)Math.Min(Scale.Count(Maximum)-1,Math.Max(0,Math.Floor(fromBottom/Scale.SegmentPitch))),Maximum);
        }
        private void Scrub(float localY){Value=ValueAtLocalY(localY);Paint();}
        private void Finish()
        {
            var id=pointer;pointer=-1;
            if(id>=0 && Track.HasPointerCapture(id))Track.ReleasePointer(id);
            Paint();Commit();
        }
        private void Commit(){if(Value==committed)return;committed=Value;commit(Value);}
        private void Paint()
        {
            var fraction=Maximum==0?0:Math.Max(0,Math.Min(1,(double)Value/Maximum));
            PositionState=Value<0 || Value>Maximum?"invalid":Maximum==0?"disabled":Value==0?"zero":fraction>=.85?"high":fraction>=.5?"medium":"low";
            foreach(var state in new[]{"invalid","disabled","zero","high","medium","low"})EnableInClassList("lps-"+state,state==PositionState);
            EnableInClassList("lps-backend-invalid",backendInvalid);EnableInClassList("lps-pending",Pending);
            for(var i=0;i<segments.Length;i++)segments[i].EnableInClassList("lps-filled",i<Scale.Filled(Value,Maximum));
            readout.text=Value.ToString(CultureInfo.InvariantCulture);Exact.SetValueWithoutNotify(Value);
            notice.text=Pending?"PENDING":PositionState=="invalid"?"! RANGE":backendInvalid?"! BACKEND ERROR":Maximum==0?"NO DEMAND":!Scale.Paired?"≈ SCALE":"";
            notice.style.display=notice.text.Length==0?DisplayStyle.None:DisplayStyle.Flex;
            tooltip="Exact "+Value+" / maximum "+Maximum+". "+notice.text;
        }
    }
}
