#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public enum ReviewIconKind { LockOpen,LockClosed,Biotech,Computing,Construction,Energy,Propulsion,Weapons,Engineering,Building }
    // Owned vector strokes, independent of emoji/font coverage. Meaning is also supplied as text.
    public sealed class ReviewIcon : VisualElement
    {
        public ReviewIconKind Kind {get;}
        public ReviewIcon(ReviewIconKind kind,string label)
        {
            Kind=kind;tooltip=label;pickingMode=PickingMode.Ignore;name="lcars-icon-"+kind;style.width=style.height=30;style.flexShrink=0;style.color=new Color(.73f,.67f,.9f);
            generateVisualContent+=context=>{
                var p=context.painter2D;p.strokeColor=resolvedStyle.color;p.lineWidth=2;
                void Line(params float[] xy){p.BeginPath();for(int i=0;i<xy.Length;i+=2){var point=new Vector2(xy[i],xy[i+1]);if(i==0)p.MoveTo(point);else p.LineTo(point);}p.Stroke();}
                void Box(float x,float y,float w,float h)=>Line(x,y,x+w,y,x+w,y+h,x,y+h,x,y);
                switch(Kind)
                {
                    case ReviewIconKind.LockOpen:case ReviewIconKind.LockClosed:
                        Box(7,14,18,13);Line(10,14,10,8,13,4,19,4,22,8,22,Kind==ReviewIconKind.LockClosed?14:9);Line(16,19,16,24);break;
                    case ReviewIconKind.Biotech:Line(8,3,19,9,8,19,19,27);Line(21,3,10,9,21,19,10,27);Line(10,6,19,6);Line(10,23,19,23);break;
                    case ReviewIconKind.Computing:Box(8,8,14,14);for(int i=10;i<=20;i+=5){Line(i,3,i,8);Line(i,22,i,27);Line(3,i,8,i);Line(22,i,27,i);}break;
                    case ReviewIconKind.Construction:Box(4,18,10,9);Box(16,18,10,9);Box(10,7,10,9);break;
                    case ReviewIconKind.Energy:Line(18,3,7,17,15,17,11,28,25,11,17,11,18,3);break;
                    case ReviewIconKind.Propulsion:Line(5,24,15,4,25,24,15,18,5,24);Line(11,26,15,21,19,26);break;
                    case ReviewIconKind.Weapons:Box(9,9,12,12);Line(15,2,15,11);Line(15,19,15,28);Line(2,15,11,15);Line(19,15,28,15);break;
                    case ReviewIconKind.Building:Line(3,10,15,3,27,10,3,10);Line(5,12,5,26,25,26,25,12);Line(11,13,11,24);Line(19,13,19,24);break;
                    default:Box(10,10,10,10);Line(15,2,15,10);Line(15,20,15,28);Line(2,15,10,15);Line(20,15,28,15);Line(5,5,10,10);Line(20,20,25,25);Line(5,25,10,20);Line(20,10,25,5);break;
                }
            };
        }
    }
}
