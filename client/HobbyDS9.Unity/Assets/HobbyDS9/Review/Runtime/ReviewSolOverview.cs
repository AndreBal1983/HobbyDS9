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
        private readonly Dictionary<string,float> markerHoldUntil=new Dictionary<string,float>(StringComparer.Ordinal);
        private readonly ColonisationPlanetSnapshot[] bodies;
        private readonly VisualElement viewport,controls;
        private readonly Foldout viewControls;
        private readonly Image image;
        private readonly Label unavailable;
        private Image? focusedPortrait;
        private Label? portraitUnavailable;
        private readonly Action<float,float> onView;
        private readonly PlanetActivationScope activation;
        private ReviewSolStage? stage;
        private string selected;
        private float yaw,zoom,startYaw,targetYaw,animationStart;
        private bool disposed,failed,queued,animating,reducedMotion,pendingFocus;
        internal static bool HasReference(ColonisationPlanetSnapshot body)=>PlanetVisualProfile.From(body).AuthoredMap!=null;
        internal ReviewSolOverview(IEnumerable<ColonisationPlanetSnapshot> bodies,string selectedPlanet,float yaw,float zoom,
            Action<ColonisationPlanetSnapshot> onSelect,Action<float,float> onView,
            Action<ColonisationPlanetSnapshot> onOpen,bool reducedMotion=false,Action<bool>? onReducedMotion=null,PlanetActivationScope? activation=null)
        {
            this.activation=activation??new PlanetActivationScope(this);
            this.bodies=bodies.ToArray();this.yaw=yaw;this.zoom=zoom;selected=selectedPlanet;this.onView=onView;this.reducedMotion=reducedMotion;
            name="sol-overview";style.flexGrow=1;style.flexBasis=520;style.minWidth=0;
            var title=new Label("System overview");title.AddToClassList("console-section");Add(title);
            Add(new Label("Schematic orbits and sizes · Only disclosed bodies"));
            viewControls=new Foldout{text="View controls",value=false,name="system-view-controls"};Add(viewControls);
            controls=new VisualElement();controls.style.flexDirection=FlexDirection.Row;controls.style.flexWrap=Wrap.Wrap;viewControls.Add(controls);
            Control("Rotate left",()=>SetView(yaw-25,zoom));Control("Rotate right",()=>SetView(yaw+25,zoom));
            Control("Zoom out",()=>SetView(yaw,zoom-.14f));Control("Zoom in",()=>SetView(yaw,zoom+.14f));
            Control("Overview",()=>SetView(0,1));
            var motion=new Toggle("Reduce motion"){value=reducedMotion,name="system-reduce-motion"};controls.Add(motion);
            motion.RegisterValueChangedCallback(e=>{this.activation.Cancel();this.reducedMotion=e.newValue;onReducedMotion?.Invoke(e.newValue);if(e.newValue&&animating){yaw=targetYaw;animating=false;onView(yaw,zoom);QueueDraw();}});
            viewport=new VisualElement{name="sol-viewport"};viewport.style.height=340;viewport.style.flexShrink=0;viewport.style.overflow=Overflow.Hidden;
            viewport.style.backgroundColor=new Color(.019f,.027f,.049f);Add(viewport);
            image=new Image{scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Ignore};image.StretchToParentSize();viewport.Add(image);
            foreach(var body in this.bodies)
            {
                var marker=new VisualElement{name="sol-body-"+body.PlanetId,tooltip="Click / Space to focus · Double-click / Enter to open "+body.DisplayName};
                marker.style.position=Position.Absolute;marker.style.width=108;marker.style.height=58;marker.style.paddingTop=24;
                marker.style.color=new Color(.81f,.9f,1);marker.style.display=DisplayStyle.None;
                var label=new Label(body.DisplayName+(body.ActiveSurveyId!=null?"\nSurvey in progress":"")){pickingMode=PickingMode.Ignore};
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
            }
            unavailable=new Label("3D overview unavailable. Use the disclosed body list."){name="sol-overview-unavailable"};unavailable.style.display=DisplayStyle.None;Add(unavailable);
            var credits=new Foldout{text="Surface map credits",value=false};
            credits.Add(new Label("Earth/Mars surface maps: Solar System Scope · CC BY 4.0. Other terrain is decorative."));Add(credits);
            viewport.RegisterCallback<GeometryChangedEvent>(e=>{if(e.oldRect.size!=e.newRect.size)QueueDraw();});
            RegisterCallback<GeometryChangedEvent>(e=>{
                if(ReferenceEquals(e.target,this))viewport.style.height=Mathf.Clamp(e.newRect.width/2.1f,260,420);
            });
            RegisterCallback<AttachToPanelEvent>(_=>{pendingFocus=true;QueueDraw();});
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
            selected=id;pendingFocus=true;QueueDraw();
        }
        private void Control(string text,Action action)
        {var b=new Button(()=>{if(!disposed&&!failed)action();}){text=text};b.style.minHeight=44;b.style.marginRight=4;controls.Add(b);}
        private void SetView(float angle,float scale)
        {activation.Cancel();animating=false;pendingFocus=false;yaw=Mathf.Repeat(angle+180,360)-180;zoom=Mathf.Clamp(scale,.76f,1.42f);onView(yaw,zoom);QueueDraw();}
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
            try {stage??=new ReviewSolStage(shader!,bodies);}
            catch(Exception){Fallback();return;}
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
            try {if(!stage.Render(width/height,yaw,zoom,selected)){Fallback();return;}}
            catch(Exception){Fallback();return;}
            image.image=stage.Texture;
            if(focusedPortrait!=null&&focusedPortrait.panel!=null)
            {
                bool ready;
                try{ready=stage.RenderPortrait(selected);}catch(Exception){ready=false;}
                focusedPortrait.image=ready?stage.PortraitTexture:null;
                if(portraitUnavailable!=null)portraitUnavailable.style.display=ready?DisplayStyle.None:DisplayStyle.Flex;
            }
            var occupied=new List<Rect>();
            foreach(var entry in markers.OrderBy(x=>x.Key==selected?0:bodies.First(b=>b.PlanetId==x.Key).ActiveSurveyId!=null?1:2))
            {
                var marker=entry.Value;bool visible=stage.TryGetPosition(entry.Key,out var point);
                var rectangle=new Rect(Mathf.Clamp(point.x*width-54,0,Mathf.Max(0,width-108)),Mathf.Clamp(point.y*height-25,0,height-58),108,58);
                // Keep a just-clicked label hit target stationary through the double-click window.
                if(markerHoldUntil.TryGetValue(entry.Key,out var until)&&Time.realtimeSinceStartup<until&&marker.style.display.value!=DisplayStyle.None)
                {rectangle=new Rect(marker.resolvedStyle.left,marker.resolvedStyle.top,108,58);visible=true;}
                if(visible&&entry.Key!=selected&&occupied.Any(r=>r.Overlaps(rectangle)))visible=false;
                marker.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;if(!visible)continue;
                occupied.Add(rectangle);marker.style.left=rectangle.x;marker.style.top=rectangle.y;
                marker.style.color=entry.Key==selected?new Color(.98f,.76f,.48f):new Color(.81f,.9f,1);
            }
            if(animating)QueueDraw();
        }
        private void Fallback()
        {failed=true;image.image=null;if(focusedPortrait!=null)focusedPortrait.image=null;if(portraitUnavailable!=null)portraitUnavailable.style.display=DisplayStyle.Flex;stage?.Dispose();stage=null;viewport.style.display=viewControls.style.display=DisplayStyle.None;unavailable.style.display=DisplayStyle.Flex;}
        public void Dispose(){if(disposed)return;disposed=true;image.image=null;if(focusedPortrait!=null)focusedPortrait.image=null;focusedPortrait=null;portraitUnavailable=null;stage?.Dispose();stage=null;}
    }
}
