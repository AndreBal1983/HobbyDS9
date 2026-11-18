#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    internal sealed class ReviewSolOverview : VisualElement,IDisposable
    {
        private readonly Dictionary<string,VisualElement> markers=new Dictionary<string,VisualElement>(StringComparer.Ordinal);
        private readonly Dictionary<string,VisualElement> bodyHits=new Dictionary<string,VisualElement>(StringComparer.Ordinal);
        private readonly List<(Vector2 body,Vector2 label)> leaderSegments=new List<(Vector2,Vector2)>();
        private readonly VisualElement leaders;
        private readonly Label sunLabel;
        private readonly Dictionary<string,float> markerHoldUntil=new Dictionary<string,float>(StringComparer.Ordinal);
        private readonly ColonisationPlanetSnapshot[] bodies;
        private readonly VisualElement viewport,controls;
        private readonly Foldout viewControls;
        private readonly Image image;
        private readonly Label unavailable;
        private readonly Label beltUnavailable;
        private Image? focusedPortrait;
        private Label? portraitUnavailable;
        private readonly Action<float,float> onView;
        private readonly PlanetActivationScope activation;
        private readonly IReadOnlyDictionary<string,long>? meanDistances;
        private readonly IReadOnlyList<BeltVisualSlot> belts;
        private readonly Action<bool,bool>? onFrame;
        private bool innerFrame,focusFrame;
        private ReviewSolStage? stage;
        private string selected;
        private float yaw,zoom,startYaw,targetYaw,animationStart;
        private bool disposed,failed,queued,animating,reducedMotion,pendingFocus;
        internal static bool HasReference(ColonisationPlanetSnapshot body)=>PlanetVisualProfile.From(body).AuthoredMap!=null;
        internal ReviewSolOverview(IEnumerable<ColonisationPlanetSnapshot> bodies,string selectedPlanet,float yaw,float zoom,
            Action<ColonisationPlanetSnapshot> onSelect,Action<float,float> onView,Action<ColonisationPlanetSnapshot> onOpen,
            bool reducedMotion=false,Action<bool>? onReducedMotion=null,PlanetActivationScope? activation=null)
            :this(bodies,selectedPlanet,yaw,zoom,onSelect,onView,onOpen,reducedMotion,onReducedMotion,activation,null,false,false,null){}
        internal ReviewSolOverview(IEnumerable<ColonisationPlanetSnapshot> bodies,string selectedPlanet,float yaw,float zoom,
            Action<ColonisationPlanetSnapshot> onSelect,Action<float,float> onView,
            Action<ColonisationPlanetSnapshot> onOpen,bool reducedMotion,Action<bool>? onReducedMotion,PlanetActivationScope? activation,IReadOnlyDictionary<string,long>? meanDistances,bool innerFrame,bool focusFrame,Action<bool,bool>? onFrame,IReadOnlyList<BeltVisualSlot>? belts=null)
        {
            this.belts=belts??Array.Empty<BeltVisualSlot>();
            this.activation=activation??new PlanetActivationScope(this);this.meanDistances=meanDistances;this.innerFrame=innerFrame;this.focusFrame=focusFrame;this.onFrame=onFrame;
            this.bodies=bodies.ToArray();this.yaw=yaw;this.zoom=zoom;selected=selectedPlanet;this.onView=onView;this.reducedMotion=reducedMotion;
            name="sol-overview";style.flexGrow=0;style.flexShrink=0;style.minWidth=0;
            var title=new Label("System overview");title.AddToClassList("console-section");Add(title);
            var scale=new Label(meanDistances==null?"Schematic spacing · Exaggerated body sizes · Only disclosed bodies":"Schematic spacing · Exaggerated body sizes · AU readouts: mean Sun distance"){name="system-scale-caption"};
            scale.style.whiteSpace=WhiteSpace.Normal;scale.style.flexShrink=0;Add(scale);
            foreach(var belt in this.belts){var fact=new Label("Asteroid belt · "+belt.Feature.FeatureId+" · procedural reference "+belt.Feature.InnerRadiusMilliAu+"–"+belt.Feature.OuterRadiusMilliAu+" milli-AU · schematic spacing"){name="system-belt-"+belt.Feature.FeatureId};fact.style.whiteSpace=WhiteSpace.Normal;fact.style.fontSize=14;Add(fact);}
            beltUnavailable=new Label("Belt rendering unavailable; the disclosed feature is listed above."){name="system-belt-render-unavailable"};beltUnavailable.style.whiteSpace=WhiteSpace.Normal;beltUnavailable.style.display=DisplayStyle.None;Add(beltUnavailable);
            viewControls=new Foldout{text="View controls",value=false,name="system-view-controls"};Add(viewControls);
            controls=new VisualElement();controls.style.flexDirection=FlexDirection.Row;controls.style.flexWrap=Wrap.Wrap;viewControls.Add(controls);
            Control("Rotate left",()=>SetView(this.yaw-25,this.zoom));Control("Rotate right",()=>SetView(this.yaw+25,this.zoom));
            Control("Zoom out",()=>SetView(this.yaw,this.zoom/1.3f));Control("Zoom in",()=>SetView(this.yaw,this.zoom*1.3f));
            void Frame(bool inner,bool focus){this.innerFrame=inner;this.focusFrame=focus;this.onFrame?.Invoke(inner,focus);SetView(this.yaw,1);if(focus)pendingFocus=true;}
            var framing=new VisualElement{name="system-framing-controls"};framing.style.flexDirection=FlexDirection.Row;framing.style.flexWrap=Wrap.Wrap;framing.style.flexShrink=0;Add(framing);
            void FrameControl(string text,Action action){var button=new Button(()=>{if(!disposed&&!failed)action();}){text=text};button.style.minHeight=44;button.style.marginRight=4;framing.Add(button);}
            FrameControl("Overview",()=>Frame(false,false));
            if(meanDistances!=null)FrameControl("Inner system",()=>Frame(true,false));
            FrameControl("Focus selected",()=>Frame(false,true));
            var motion=new Toggle("Reduce motion"){value=reducedMotion,name="system-reduce-motion"};controls.Add(motion);
            motion.RegisterValueChangedCallback(e=>{this.activation.Cancel();this.reducedMotion=e.newValue;onReducedMotion?.Invoke(e.newValue);if(e.newValue&&animating){this.yaw=targetYaw;animating=false;onView(this.yaw,this.zoom);QueueDraw();}});
            viewport=new VisualElement{name="sol-viewport"};viewport.style.height=340;viewport.style.flexShrink=0;viewport.style.overflow=Overflow.Hidden;
            viewport.style.backgroundColor=new Color(.019f,.027f,.049f);Add(viewport);
            image=new Image{scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Ignore};image.StretchToParentSize();viewport.Add(image);
            leaders=new VisualElement{pickingMode=PickingMode.Ignore};leaders.StretchToParentSize();viewport.Add(leaders);
            leaders.generateVisualContent+=context=>{var painter=context.painter2D;painter.strokeColor=new Color(.35f,.45f,.58f);painter.lineWidth=1;foreach(var line in leaderSegments){painter.BeginPath();painter.MoveTo(line.body);painter.LineTo(line.label);painter.Stroke();}};
            sunLabel=new Label(meanDistances==null?"Star":"Sun"){name="sol-sun-label",pickingMode=PickingMode.Ignore};sunLabel.style.position=Position.Absolute;sunLabel.style.color=new Color(1,.77f,.4f);sunLabel.style.fontSize=15;viewport.Add(sunLabel);
            foreach(var body in this.bodies)
            {
                var marker=new VisualElement{name="sol-body-"+body.PlanetId,tooltip="Click / Space to focus · Double-click / Enter to open "+body.DisplayName};
                marker.style.position=Position.Absolute;marker.style.width=108;marker.style.height=44;marker.style.paddingTop=2;marker.style.backgroundColor=new Color(.04f,.07f,.11f,.95f);
                marker.style.color=new Color(.81f,.9f,1);marker.style.display=DisplayStyle.None;
                var distance=meanDistances!=null&&meanDistances.TryGetValue(body.PlanetId,out var km)?"\n"+SolSchematicLayout.Au(km).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" AU":"";
                var label=new Label(body.DisplayName+distance){pickingMode=PickingMode.Ignore};
                label.style.whiteSpace=WhiteSpace.Normal;label.style.unityTextAlign=TextAnchor.MiddleCenter;label.style.fontSize=16;marker.Add(label);
                if(body.ActiveSurveyId!=null)
                {
                    var bar=new VisualElement{pickingMode=PickingMode.Ignore,tooltip="Survey in progress · duration unavailable"};
                    bar.style.height=4;bar.style.backgroundColor=new Color(.16f,.24f,.32f);
                    var segment=new VisualElement{pickingMode=PickingMode.Ignore};segment.style.width=18;segment.style.height=4;
                    segment.style.backgroundColor=new Color(.86f,.65f,.35f);bar.Add(segment);marker.Add(bar);
                }
                PlanetBodyCard.BindActivation(marker,()=>{if(!disposed&&!failed){markerHoldUntil[body.PlanetId]=Time.realtimeSinceStartup+.6f;onSelect(body);schedule.Execute(QueueDraw).StartingIn(610);}},()=>{if(!disposed&&!failed)onOpen(body);},this.activation);
                viewport.Add(marker);markers.Add(body.PlanetId,marker);
                var hit=new VisualElement{name="sol-body-hit-"+body.PlanetId,tooltip=body.DisplayName+" · "+(body.Surveyed?"Surveyed":"UNSURVEYED · cosmetic appearance only")};
                hit.style.position=Position.Absolute;hit.style.width=hit.style.height=28;hit.style.display=DisplayStyle.None;
                PlanetBodyCard.BindActivation(hit,()=>{if(!disposed&&!failed)onSelect(body);},()=>{if(!disposed&&!failed)onOpen(body);},this.activation);hit.focusable=false;hit.tabIndex=-1;viewport.Add(hit);bodyHits.Add(body.PlanetId,hit);
            }
            unavailable=new Label("3D overview unavailable. Use the disclosed body list."){name="sol-overview-unavailable"};unavailable.style.display=DisplayStyle.None;Add(unavailable);
            var credits=new Foldout{text="Surface map credits",value=false};
            credits.Add(new Label("Surface maps: Solar System Scope · CC BY 4.0. Other terrain is decorative."));Add(credits);
            viewport.RegisterCallback<GeometryChangedEvent>(e=>{if(e.oldRect.size!=e.newRect.size)QueueDraw();});
            RegisterCallback<GeometryChangedEvent>(e=>{
                if(ReferenceEquals(e.target,this))viewport.style.height=Mathf.Clamp(e.newRect.width/2.1f,260,420);
            });
            RegisterCallback<AttachToPanelEvent>(_=>QueueDraw());
            RegisterCallback<DetachFromPanelEvent>(e=>{if(ReferenceEquals(e.target,this))Dispose();});
        }
        internal void BindPortrait(Image portrait,Label? unavailable=null)
        {
            if(focusedPortrait!=null)focusedPortrait.image=null;
            focusedPortrait=portrait;portraitUnavailable=unavailable;
            if(failed&&portraitUnavailable!=null)portraitUnavailable.style.display=DisplayStyle.Flex;
            QueueDraw();
        }
        internal void Select(string id)
        {
            if(disposed||!markers.ContainsKey(id))return;
            if(selected!=id){selected=id;pendingFocus=true;}QueueDraw();
        }
        private void Control(string text,Action action)
        {var b=new Button(()=>{if(!disposed&&!failed)action();}){text=text};b.style.minHeight=44;b.style.marginRight=4;controls.Add(b);}
        private void SetView(float angle,float scale)
        {activation.Cancel();animating=false;pendingFocus=false;yaw=Mathf.Repeat(angle+180,360)-180;zoom=Mathf.Clamp(scale,.5f,64f);onView(yaw,zoom);QueueDraw();}
        private void QueueDraw()
        {
            if(disposed||failed||queued)return;queued=true;
            schedule.Execute(()=>{queued=false;Draw();}).StartingIn(animating?16:0);
        }
        private void Draw()
        {
            if(disposed||failed||panel==null)return;
            float width=viewport.layout.width,height=viewport.layout.height;if(!(width>=80&&height>=80))return;
            var shader=Resources.Load<Shader>("SystemBodyArt/ProceduralSurface");
            if(stage==null&&(shader==null||!shader.isSupported||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)){Fallback();return;}
            try {stage??=new ReviewSolStage(shader!,bodies,meanDistances,belts);}
            catch(Exception){Fallback();return;}
            beltUnavailable.style.display=stage.AssetFallbacks.Any(f=>f.StartsWith("Asteroid belt:",StringComparison.Ordinal))?DisplayStyle.Flex:DisplayStyle.None;
            if(pendingFocus)
            {
                // Interrupt from the actual displayed pose, never the previous destination.
                startYaw=yaw;targetYaw=yaw+Mathf.DeltaAngle(yaw,stage.ForegroundYaw(selected));animationStart=Time.realtimeSinceStartup;
                animating=!reducedMotion;pendingFocus=false;if(reducedMotion)yaw=targetYaw;
            }
            if(animating)
            {
                var t=Mathf.Clamp01((Time.realtimeSinceStartup-animationStart)/1.2f);yaw=Mathf.Lerp(startYaw,targetYaw,t*t*(3-2*t));
                if(t>=1)animating=false;
            }
            onView(yaw,zoom);
            stage.FrameRadius=focusFrame?Mathf.Max(stage.Radius(selected)*.22f,3.5f):innerFrame&&stage.KnownSol?stage.InnerRadius:null;
            stage.FocusSelected=focusFrame;
            stage.HorizontalPaddingPixels=bodies.Length<=8?120:0;stage.ViewportWidthPixels=width;
            try {if(!stage.Render(width/height,yaw,zoom,selected)){Fallback();return;}}
            catch(Exception){Fallback();return;}
            image.image=stage.Texture;
            var sunVisible=stage.TryGetSunPosition(out var sunPoint);sunLabel.style.display=sunVisible?DisplayStyle.Flex:DisplayStyle.None;
            sunLabel.style.left=sunPoint.x*width-14;sunLabel.style.top=sunPoint.y*height+Mathf.Max(12,height*.0425f);
            if(focusedPortrait!=null&&focusedPortrait.panel!=null)
            {
                bool ready;
                try{ready=stage.RenderPortrait(selected);}catch(Exception){ready=false;}
                focusedPortrait.image=ready?stage.PortraitTexture:null;
                if(portraitUnavailable!=null)portraitUnavailable.style.display=ready?DisplayStyle.None:DisplayStyle.Flex;
            }
            leaderSegments.Clear();
            var points=new Dictionary<string,Vector2>(StringComparer.Ordinal);
            foreach(var body in bodies)
            {
                var hit=bodyHits[body.PlanetId];var visible=stage.TryGetPosition(body.PlanetId,out var point);
                hit.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;
                if(visible){points.Add(body.PlanetId,new Vector2(point.x*width,point.y*height));hit.style.left=point.x*width-14;hit.style.top=point.y*height-14;}
            }
            var railLabels=bodies.Length<=8?SolSchematicLayout.Labels(points.Select(p=>new SolSchematicLayout.Point(p.Key,p.Value.x,p.Value.y)),width,height).ToDictionary(p=>p.Id):null;
            var occupied=new List<Rect>();
            foreach(var entry in markers.OrderBy(x=>x.Key==selected?0:bodies.First(b=>b.PlanetId==x.Key).ActiveSurveyId!=null?1:2))
            {
                var marker=entry.Value;bool visible=stage.TryGetPosition(entry.Key,out var point);
                var rectangle=railLabels!=null&&railLabels.TryGetValue(entry.Key,out var placed)?new Rect((float)placed.X,(float)placed.Y,108,44):new Rect(Mathf.Clamp(point.x*width-54,0,Mathf.Max(0,width-108)),Mathf.Clamp(point.y*height-25,0,height-44),108,44);
                // Keep a just-clicked label hit target stationary through the double-click window.
                if(markerHoldUntil.TryGetValue(entry.Key,out var until)&&Time.realtimeSinceStartup<until&&marker.style.display.value!=DisplayStyle.None)
                {rectangle=new Rect(marker.resolvedStyle.left,marker.resolvedStyle.top,108,44);visible=true;}
                if(railLabels==null&&visible&&entry.Key!=selected&&occupied.Any(r=>r.Overlaps(rectangle)))visible=false;
                marker.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;if(!visible)continue;
                occupied.Add(rectangle);marker.style.left=rectangle.x;marker.style.top=rectangle.y;
                marker.style.color=entry.Key==selected?new Color(.98f,.76f,.48f):new Color(.81f,.9f,1);
                if(railLabels!=null&&points.TryGetValue(entry.Key,out var centre))leaderSegments.Add((centre,new Vector2(rectangle.center.x<width/2?rectangle.xMax:rectangle.xMin,rectangle.center.y)));
            }
            leaders.MarkDirtyRepaint();
            if(animating)QueueDraw();
        }
        private void Fallback()
        {failed=true;image.image=null;if(focusedPortrait!=null)focusedPortrait.image=null;if(portraitUnavailable!=null)portraitUnavailable.style.display=DisplayStyle.Flex;stage?.Dispose();stage=null;viewport.style.display=viewControls.style.display=DisplayStyle.None;unavailable.style.display=DisplayStyle.Flex;}
        public void Dispose(){if(disposed)return;disposed=true;image.image=null;if(focusedPortrait!=null)focusedPortrait.image=null;focusedPortrait=null;portraitUnavailable=null;stage?.Dispose();stage=null;}
    }
}
