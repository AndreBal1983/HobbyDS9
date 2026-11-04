#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    internal sealed class ReviewSolOverview : VisualElement, IDisposable
    {
        private readonly Dictionary<string,Button> markers = new Dictionary<string,Button>(StringComparer.Ordinal);
        private readonly VisualElement controls;
        private readonly VisualElement viewport;
        private readonly Image image;
        private readonly Label unavailable;
        private readonly string selected;
        private readonly Action<float,float> onView;
        private ReviewSolStage? stage;
        private float yaw, zoom;
        private bool disposed, failed, redrawQueued;

        internal static bool HasReference(ColonisationPlanetSnapshot body)
            =>body.PlanetId=="planet.sol"||body.PlanetId=="planet.mars";

        internal ReviewSolOverview(IEnumerable<ColonisationPlanetSnapshot> bodies,string selectedPlanet,float yaw,float zoom,
            Action<ColonisationPlanetSnapshot> onSelect,Action<float,float> onView)
        {
            this.yaw=yaw;this.zoom=zoom;selected=selectedPlanet;this.onView=onView;
            name="sol-overview";
            AddToClassList("console-queue");
            style.maxWidth=1180;style.flexShrink=0;
            var title=new Label("Sol overview") { name="sol-overview-heading" };
            title.AddToClassList("console-section");
            Add(title);
            var caption=new Label("Illustrative orbits and sizes. Labeled planets are disclosed campaign bodies.")
                { name="sol-overview-disclosure" };
            caption.AddToClassList("console-copy");
            Add(caption);
            controls=new VisualElement();
            controls.style.flexDirection=FlexDirection.Row;
            controls.style.flexWrap=Wrap.Wrap;
            Add(controls);
            Control(controls,"Rotate left","sol-rotate-left",()=>ChangeView(yaw:-25));
            Control(controls,"Rotate right","sol-rotate-right",()=>ChangeView(yaw:25));
            Control(controls,"Zoom out","sol-zoom-out",()=>ChangeView(zoom:-.14f));
            Control(controls,"Zoom in","sol-zoom-in",()=>ChangeView(zoom:.14f));
            Control(controls,"Reset view","sol-reset",()=>SetView(0,1));
            viewport=new VisualElement { name="sol-viewport" };
            viewport.style.height=310;viewport.style.flexShrink=0;viewport.style.overflow=Overflow.Hidden;
            viewport.style.backgroundColor=new Color(.019f,.027f,.049f,1);
            Add(viewport);
            image=new Image { scaleMode=ScaleMode.StretchToFill, pickingMode=PickingMode.Ignore };
            image.StretchToParentSize();
            viewport.Add(image);
            foreach(var body in bodies.Where(HasReference))
            {
                var marker=new Button(()=>{if(!disposed&&!failed)onSelect(body);})
                    { name="sol-body-"+body.PlanetId, text=body.DisplayName, tooltip="Inspect "+body.DisplayName };
                marker.style.position=Position.Absolute;
                marker.style.width=82;marker.style.height=60;
                marker.style.marginLeft=0;marker.style.marginTop=0;marker.style.marginRight=0;marker.style.marginBottom=0;
                marker.style.paddingTop=34;marker.style.paddingBottom=0;
                marker.style.color=body.PlanetId==selected?new Color(.98f,.76f,.48f):new Color(.81f,.9f,1);
                marker.style.backgroundColor=Color.clear;
                marker.style.borderLeftColor=Color.clear;marker.style.borderRightColor=Color.clear;
                marker.style.borderTopColor=Color.clear;marker.style.borderBottomColor=Color.clear;
                marker.style.display=DisplayStyle.None;
                viewport.Add(marker);
                markers.Add(body.PlanetId,marker);
            }
            unavailable=new Label("3D overview unavailable. Use the planet and ship cards above.")
                { name="sol-overview-unavailable" };
            unavailable.AddToClassList("console-copy");
            unavailable.style.display=DisplayStyle.None;
            Add(unavailable);
            var credit=new Label("Surface maps: Solar System Scope · CC BY 4.0") { name="sol-overview-credit" };
            credit.AddToClassList("console-copy");
            Add(credit);
            viewport.RegisterCallback<GeometryChangedEvent>(e=>{
                if(e.oldRect.size!=e.newRect.size)QueueDraw();
            });
            RegisterCallback<DetachFromPanelEvent>(e=>{if(ReferenceEquals(e.target,this))Dispose();});
        }

        private static void Control(VisualElement parent,string label,string id,Action apply)
        {
            var button=new Button(apply) { text=label, name=id };
            button.style.minHeight=40;button.style.marginRight=5;
            parent.Add(button);
        }

        private void ChangeView(float yaw=0,float zoom=0)=>SetView(this.yaw+yaw,this.zoom+zoom);

        private void SetView(float yaw,float zoom)
        {
            if(disposed||failed)return;
            this.yaw=Mathf.Repeat(yaw+180,360)-180;
            this.zoom=Mathf.Clamp(zoom,.76f,1.42f);
            onView(this.yaw,this.zoom);
            QueueDraw();
        }

        private void QueueDraw()
        {
            if(disposed||failed||redrawQueued)return;
            redrawQueued=true;
            schedule.Execute(()=>{
                redrawQueued=false;
                Draw();
            });
        }

        private void Draw()
        {
            if(disposed||failed||panel==null)return;
            float width=viewport.layout.width,height=viewport.layout.height;
            if(!(width>=80&&height>=80))return;
            var shader=Resources.Load<Shader>("SystemBodyArt/SolSurface");
            if(stage==null&&(shader==null||!shader.isSupported||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null))
            {
                ShowFallback();
                return;
            }
            stage??=new ReviewSolStage(shader);
            if(!stage.Render(width/height,yaw,zoom,selected))
            {
                ShowFallback();
                return;
            }
            image.image=stage.Texture;
            foreach(var entry in markers)
            {
                bool visible=stage.TryGetPosition(entry.Key,out var position);
                var marker=entry.Value;
                marker.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;
                if(!visible)continue;
                marker.style.left=Mathf.Clamp(position.x*width-41,0,width-82);
                marker.style.top=Mathf.Clamp(position.y*height-24,0,height-60);
            }
        }

        private void ShowFallback()
        {
            failed=true;
            image.image=null;
            stage?.Dispose();stage=null;
            controls.style.display=DisplayStyle.None;
            viewport.style.display=DisplayStyle.None;
            unavailable.style.display=DisplayStyle.Flex;
        }

        public void Dispose()
        {
            if(disposed)return;
            disposed=true;
            image.image=null;
            stage?.Dispose();stage=null;
        }
    }
}
