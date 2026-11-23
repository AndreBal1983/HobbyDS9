#nullable enable
using System;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // One disclosed globe, fixed framing and local pose. No navigation or campaign commands.
    internal sealed class ReviewPlanetPortrait : VisualElement,IDisposable
    {
        private readonly ColonisationPlanetSnapshot body;
        private readonly Image image;
        private readonly VisualElement viewport;
        private readonly Label unavailable,hint;
        private readonly Toggle motionToggle;
        private readonly PlanetPortraitMotion motion;
        private readonly Action<bool>? preferenceChanged;
        private ReviewSolStage? stage;
        private IVisualElementScheduledItem? ticker;
        private bool disposed,failed,applicationFocused,renderQueued;
        private float previousTick;
        internal bool ReducedMotion
        {
            get=>motion.ReducedMotion;
            set
            {
                if(disposed||motion.ReducedMotion==value)return;
                motion.ReducedMotion=value;motionToggle.SetValueWithoutNotify(value);UpdateHint();previousTick=Time.realtimeSinceStartup;
                preferenceChanged?.Invoke(value);
            }
        }
        internal ReviewPlanetPortrait(ColonisationPlanetSnapshot body):this(body,false,null){}
        internal ReviewPlanetPortrait(ColonisationPlanetSnapshot body,bool reducedMotion,Action<bool>? preferenceChanged=null)
        {
            this.body=body;this.preferenceChanged=preferenceChanged;motion=new PlanetPortraitMotion(reducedMotion);
            name="planet-detail-visual";AddToClassList("planet-detail-visual");
            viewport=new VisualElement{name="planet-portrait-drag-surface",focusable=true,tabIndex=0,
                tooltip="Drag to rotate. Arrow keys rotate; Home restores the starting view. Reduce motion pauses automatic rotation."};
            viewport.AddToClassList("planet-detail-portrait");viewport.style.minHeight=180;viewport.style.height=320;viewport.style.maxHeight=320;viewport.style.minWidth=0;viewport.style.flexShrink=0;
            viewport.style.borderLeftWidth=viewport.style.borderRightWidth=viewport.style.borderTopWidth=viewport.style.borderBottomWidth=2;FocusBorder(false);Add(viewport);
            image=new Image{name="planet-detail-portrait",scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
            image.StretchToParentSize();viewport.Add(image);
            unavailable=new Label("Planet preview unavailable");unavailable.style.display=DisplayStyle.None;Add(unavailable);
            var caption=new Label(PlanetVisualProfile.From(body).Caption);caption.AddToClassList("console-copy");Add(caption);
            hint=new Label{name="planet-portrait-motion-hint",pickingMode=PickingMode.Ignore};hint.style.fontSize=14;hint.style.whiteSpace=WhiteSpace.Normal;Add(hint);
            motionToggle=new Toggle("Reduce motion"){name="planet-portrait-reduce-motion",value=reducedMotion};motionToggle.style.minHeight=44;Add(motionToggle);
            motionToggle.RegisterValueChangedCallback(e=>ReducedMotion=e.newValue);UpdateHint();
            viewport.RegisterCallback<GeometryChangedEvent>(e=>{if(e.oldRect.size!=e.newRect.size&&motion.Dragging)CancelDrag();if(Mathf.Abs(e.newRect.width-e.oldRect.width)>.1f&&e.newRect.width>0)viewport.style.height=Mathf.Clamp(e.newRect.width,180,320);});
            viewport.RegisterCallback<PointerDownEvent>(PointerDown);viewport.RegisterCallback<PointerMoveEvent>(PointerMove);viewport.RegisterCallback<PointerUpEvent>(PointerUp);
            viewport.RegisterCallback<PointerCancelEvent>(e=>{if(motion.PointerId==e.pointerId)CancelDrag();});viewport.RegisterCallback<PointerCaptureOutEvent>(e=>{if(motion.PointerId==e.pointerId)CancelDrag();});
            viewport.RegisterCallback<FocusInEvent>(_=>FocusBorder(true));viewport.RegisterCallback<FocusOutEvent>(_=>{FocusBorder(false);CancelDrag();});viewport.RegisterCallback<KeyDownEvent>(KeyDown);
            viewport.RegisterCallback<ClickEvent>(e=>e.StopImmediatePropagation());viewport.RegisterCallback<NavigationSubmitEvent>(e=>e.StopImmediatePropagation());
            viewport.RegisterCallback<NavigationMoveEvent>(e=>e.StopImmediatePropagation());
            RegisterCallback<AttachToPanelEvent>(e=>{if(ReferenceEquals(e.target,this))Attach();});
            RegisterCallback<DetachFromPanelEvent>(e=>{if(ReferenceEquals(e.target,this))Dispose();});
        }
        private void FocusBorder(bool focused)
        {var color=focused?new Color(.94f,.69f,.42f):Color.clear;viewport.style.borderLeftColor=viewport.style.borderRightColor=viewport.style.borderTopColor=viewport.style.borderBottomColor=color;}
        private void UpdateHint()=>hint.text=failed?"Rotation unavailable":motion.Dragging?"Drag to rotate · Auto-rotation paused":motion.ReducedMotion?"Drag to rotate · Motion reduced":"Drag to rotate · Slow auto-rotation";
        private void Attach()
        {
            if(disposed)return;applicationFocused=UnityEngine.Application.isFocused;previousTick=Time.realtimeSinceStartup;
            UnityEngine.Application.focusChanged-=FocusChanged;UnityEngine.Application.focusChanged+=FocusChanged;
            ticker??=schedule.Execute(Tick).Every(64);ticker.Resume();Render();
        }
        private void FocusChanged(bool focused)
        {applicationFocused=focused;previousTick=Time.realtimeSinceStartup;if(!focused)CancelDrag();}
        private bool Visible()
        {
            if(disposed||failed||panel==null||!enabledInHierarchy||!applicationFocused)return false;
            var bounds=viewport.worldBound;if(bounds.width<=0||bounds.height<=0)return false;
            for(VisualElement? current=this;current!=null;current=current.parent)
            {
                if(current.resolvedStyle.display==DisplayStyle.None||current.resolvedStyle.visibility!=Visibility.Visible||current.resolvedStyle.opacity<=0)return false;
                if(current is ScrollView scroll&&!bounds.Overlaps(scroll.contentViewport.worldBound))return false;
            }
            return true;
        }
        private void Tick()
        {
            if(disposed)return;var now=Time.realtimeSinceStartup;var elapsed=Math.Max(0,now-previousTick);previousTick=now;
            if(motion.Advance(elapsed,Visible()))QueueRender();
        }
        private bool Interact()=>!disposed&&!failed&&panel!=null&&enabledInHierarchy&&applicationFocused;
        private void PointerDown(PointerDownEvent e)
        {
            if(e.button!=0||e.altKey||e.ctrlKey||e.shiftKey||e.commandKey||!Interact())return;
            if(!motion.BeginDrag(e.pointerId,e.localPosition.x,e.localPosition.y))return;
            viewport.Focus();viewport.CapturePointer(e.pointerId);UpdateHint();e.StopImmediatePropagation();
        }
        private void PointerMove(PointerMoveEvent e)
        {
            if(motion.PointerId!=e.pointerId)return;
            if(!Interact()||!viewport.HasPointerCapture(e.pointerId)){CancelDrag();return;}
            if(motion.MoveDrag(e.pointerId,e.localPosition.x,e.localPosition.y))QueueRender();e.StopImmediatePropagation();
        }
        private void PointerUp(PointerUpEvent e)
        {
            if(e.button!=0||motion.PointerId!=e.pointerId)return;
            if(Interact()&&motion.MoveDrag(e.pointerId,e.localPosition.x,e.localPosition.y))QueueRender();
            motion.EndDrag(e.pointerId);if(viewport.HasPointerCapture(e.pointerId))viewport.ReleasePointer(e.pointerId);UpdateHint();previousTick=Time.realtimeSinceStartup;e.StopImmediatePropagation();
        }
        private void CancelDrag()
        {
            var pointer=motion.PointerId;motion.CancelDrag();if(pointer.HasValue&&viewport.HasPointerCapture(pointer.Value))viewport.ReleasePointer(pointer.Value);
            UpdateHint();previousTick=Time.realtimeSinceStartup;
        }
        private void KeyDown(KeyDownEvent e)
        {
            if(!Interact()||e.altKey||e.ctrlKey||e.shiftKey||e.commandKey)return;
            bool changed;
            switch(e.keyCode)
            {
                case KeyCode.LeftArrow:changed=motion.Nudge(-4,0);break;
                case KeyCode.RightArrow:changed=motion.Nudge(4,0);break;
                case KeyCode.UpArrow:changed=motion.Nudge(0,-4);break;
                case KeyCode.DownArrow:changed=motion.Nudge(0,4);break;
                case KeyCode.Home:changed=motion.Reset();break;
                case KeyCode.Escape:CancelDrag();e.StopImmediatePropagation();return;
                default:return;
            }
            if(changed)QueueRender();e.StopImmediatePropagation();
        }
        private void QueueRender()
        {
            if(disposed||failed||renderQueued)return;renderQueued=true;
            schedule.Execute(()=>{renderQueued=false;Render();});
        }
        private void Render()
        {
            if(disposed||failed||panel==null)return;
            try
            {
                var shader=Resources.Load<Shader>("SystemBodyArt/ProceduralSurface");
                if(shader==null||!shader.isSupported||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
                    throw new InvalidOperationException();
                stage??=new ReviewSolStage(shader,new[]{body});
                if(!stage.RenderPortraitPose(body.PlanetId,(float)motion.Yaw,(float)motion.Pitch))throw new InvalidOperationException();
                image.image=stage.PortraitTexture;
            }
            catch(Exception){failed=true;CancelDrag();ticker?.Pause();image.image=null;stage?.Dispose();stage=null;viewport.SetEnabled(false);unavailable.style.display=DisplayStyle.Flex;}
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;ticker?.Pause();ticker=null;UnityEngine.Application.focusChanged-=FocusChanged;CancelDrag();image.image=null;stage?.Dispose();stage=null;
        }
    }
}
