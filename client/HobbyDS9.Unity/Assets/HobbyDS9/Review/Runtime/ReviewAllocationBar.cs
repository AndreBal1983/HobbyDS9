#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // Presentation-only local editor. A release edits the draft; it never submits research.
    public sealed class ReviewAllocationBar : VisualElement
    {
        private readonly Action<int> change;
        private readonly Color colour;
        private int value,preview,pointer=-1;
        private float origin,width;
        public ReviewAllocationBar(string id,int units,Color colour,Action<int> change)
        {
            name=id;value=preview=units;this.colour=colour;this.change=change;
            focusable=true;tabIndex=0;AddToClassList("science-allocation-track");
            tooltip="Science share. Left / Right: 1%. Page Up / Down: 5%. Home: zero. End: available budget. Release pointer to edit; Escape cancels the drag.";
            generateVisualContent+=Paint;
            RegisterCallback<PointerDownEvent>(e=>{
                if(e.button!=0||pointer>=0||!enabledInHierarchy)return;
                origin=worldBound.x;width=worldBound.width;pointer=e.pointerId;
                Focus();this.CapturePointer(pointer);Scrub(e.position.x);e.StopPropagation();
            });
            RegisterCallback<PointerMoveEvent>(e=>{if(pointer==e.pointerId){Scrub(e.position.x);e.StopPropagation();}});
            RegisterCallback<PointerUpEvent>(e=>{
                if(pointer!=e.pointerId)return;
                Scrub(e.position.x);var next=preview;CancelDrag();e.StopPropagation();Commit(next);
            });
            RegisterCallback<PointerCancelEvent>(_=>CancelDrag());
            RegisterCallback<PointerCaptureOutEvent>(_=>{pointer=-1;preview=value;MarkDirtyRepaint();});
            RegisterCallback<DetachFromPanelEvent>(_=>{pointer=-1;});
            RegisterCallback<KeyDownEvent>(e=>{
                if(!enabledInHierarchy)return;
                var next=value;
                switch(e.keyCode)
                {
                    case KeyCode.LeftArrow:case KeyCode.DownArrow:next-=6;break;
                    case KeyCode.RightArrow:case KeyCode.UpArrow:next+=6;break;
                    case KeyCode.PageDown:next-=30;break;
                    case KeyCode.PageUp:next+=30;break;
                    case KeyCode.Home:next=0;break;
                    case KeyCode.End:next=600;break;
                    case KeyCode.Escape:CancelDrag();e.StopPropagation();return;
                    case KeyCode.Return:case KeyCode.KeypadEnter:case KeyCode.Space:e.StopPropagation();return;
                    default:return;
                }
                e.StopPropagation();Commit(Math.Max(0,Math.Min(600,next)));
            });
            RegisterCallback<NavigationSubmitEvent>(e=>e.StopPropagation());
        }
        private void Scrub(float x){preview=Mathf.RoundToInt(Mathf.Clamp01((x-origin)/Math.Max(1,width))*100)*6;MarkDirtyRepaint();}
        private void CancelDrag()
        {
            var old=pointer;pointer=-1;preview=value;
            if(old>=0&&this.HasPointerCapture(old))this.ReleasePointer(old);
            MarkDirtyRepaint();
        }
        private void Commit(int next)
        {
            if(next==value)return;
            var tree=panel?.visualTree;var id=name;
            change(next);
            // The panel redraw retains the draft; restore the editor rather than moving focus to a submit action.
            tree?.Q<ReviewAllocationBar>(id)?.Focus();
        }
        private void Paint(MeshGenerationContext context)
        {
            var p=context.painter2D;var r=contentRect;var pitch=r.width/20;
            var fill=r.width*preview/600f;
            void Block(float x,float w,Color c)
            {
                if(w<=0)return;p.fillColor=c;p.BeginPath();p.MoveTo(new Vector2(x,4));p.LineTo(new Vector2(x+w,4));
                p.LineTo(new Vector2(x+w,r.height-4));p.LineTo(new Vector2(x,r.height-4));p.ClosePath();p.Fill();
            }
            for(int i=0;i<20;i++)
            {
                var x=i*pitch;var w=Math.Max(1,pitch-3);
                Block(x,w,new Color(.14f,.2f,.27f));
                Block(x,Math.Max(0,Math.Min(w,fill-x)),enabledInHierarchy?colour:colour*.6f);
            }
            p.strokeColor=new Color(.98f,.94f,.84f);p.lineWidth=3;p.BeginPath();
            p.MoveTo(new Vector2(fill,1));p.LineTo(new Vector2(fill,r.height-1));p.Stroke();
        }
    }
}
