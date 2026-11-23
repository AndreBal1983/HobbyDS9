#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // Mounted UI-only selector. No native OS menu, gameplay command or authoritative state.
    public sealed class ReviewLcarsSelector : VisualElement
    {
        private static ReviewLcarsSelector? open;
        private readonly Button trigger;
        private readonly Action<string> changed;
        private readonly Func<bool> current;
        private VisualElement? overlay,popup,host;
        private ScrollView? scroll;
        private readonly List<Button> items=new List<Button>();
        private int highlighted,dismissPointer=-1;
        public IReadOnlyList<string> Choices {get;}
        public string Value {get;private set;}
        public ReviewLcarsSelector(string label,IEnumerable<string> choices,string value,Action<string> changed,Func<bool> current)
        {
            Choices=choices.ToList().AsReadOnly();if(Choices.Count==0)throw new ArgumentException("Selector needs a choice.");
            Value=Choices.Contains(value)?value:Choices[0];this.changed=changed;this.current=current;
            AddToClassList("lcars-selector");Add(new Label(label));
            trigger=new Button(Open){text=Value+"  ▾",tooltip=label+": "+Value};trigger.AddToClassList("lcars-selector-trigger");Add(trigger);
            trigger.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.DownArrow||e.keyCode==KeyCode.UpArrow||e.keyCode==KeyCode.Home||e.keyCode==KeyCode.End){Open();if(e.keyCode==KeyCode.Home)Highlight(0);if(e.keyCode==KeyCode.End)Highlight(Choices.Count-1);e.StopImmediatePropagation();}});
            RegisterCallback<DetachFromPanelEvent>(e=>{if(ReferenceEquals(e.target,this))Close(false);});
            RegisterCallback<GeometryChangedEvent>(_=>Place());
        }
        public static void CloseOpen()=>open?.Close(true);
        private void Open()
        {
            if(overlay!=null||panel==null||!enabledInHierarchy||!current())return;
            CloseOpen();open=this;host=panel.visualTree;
            for(var parent=this.parent;parent!=null;parent=parent.parent)if(parent.ClassListContains("player-console")){host=parent;break;}
            overlay=new VisualElement{name="lcars-selector-overlay"};overlay.AddToClassList("lcars-selector-overlay");overlay.StretchToParentSize();
            var sheet=Resources.Load<StyleSheet>("PlayerConsole");if(sheet!=null)overlay.styleSheets.Add(sheet);overlay.style.fontSize=16;
            popup=new VisualElement{name="lcars-selector-menu"};popup.AddToClassList("lcars-selector-menu");overlay.Add(popup);
            var backdrop=overlay;var menu=popup;
            scroll=new ScrollView(ScrollViewMode.Vertical){horizontalScrollerVisibility=ScrollerVisibility.Hidden,verticalScrollerVisibility=ScrollerVisibility.Auto};popup.Add(scroll);
            for(int i=0;i<Choices.Count;i++)
            {
                var index=i;var value=Choices[i];var item=new Button(()=>Choose(index)){text=(value==Value?"Selected · ":"")+value,tooltip=value,name="lcars-selector-option-"+i};
                item.AddToClassList("lcars-selector-option");item.EnableInClassList("lcars-selector-checked",value==Value);
                var check=new VisualElement{pickingMode=PickingMode.Ignore};check.AddToClassList("lcars-selector-check");
                if(value==Value)check.generateVisualContent+=c=>{var p=c.painter2D;p.strokeColor=new Color(.98f,.76f,.48f);p.lineWidth=2;p.BeginPath();p.MoveTo(new Vector2(1,8));p.LineTo(new Vector2(5,12));p.LineTo(new Vector2(13,3));p.Stroke();};
                item.Insert(0,check);items.Add(item);scroll.Add(item);
            }
            overlay.RegisterCallback<PointerDownEvent>(e=>{
                if(!ReferenceEquals(overlay,backdrop)||menu.Contains(e.target as VisualElement))return;
                dismissPointer=e.pointerId;backdrop.CapturePointer(e.pointerId);e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            overlay.RegisterCallback<PointerUpEvent>(e=>{if(!ReferenceEquals(overlay,backdrop)||dismissPointer!=e.pointerId)return;e.StopImmediatePropagation();backdrop.ReleasePointer(e.pointerId);Close(true);},TrickleDown.TrickleDown);
            overlay.RegisterCallback<PointerCancelEvent>(e=>{e.StopImmediatePropagation();Close(true);});
            overlay.RegisterCallback<ClickEvent>(e=>e.StopPropagation());overlay.RegisterCallback<WheelEvent>(e=>e.StopPropagation());
            overlay.RegisterCallback<KeyDownEvent>(e=>{
                if(e.keyCode==KeyCode.Escape){Close(true);e.StopImmediatePropagation();return;}
                if(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter||e.keyCode==KeyCode.Space){Choose(highlighted);e.StopImmediatePropagation();return;}
                var next=highlighted;
                if(e.keyCode==KeyCode.DownArrow)next++;
                else if(e.keyCode==KeyCode.UpArrow)next--;
                else if(e.keyCode==KeyCode.Home)next=0;
                else if(e.keyCode==KeyCode.End)next=Choices.Count-1;
                else if(e.keyCode==KeyCode.Tab){host?.panel?.focusController.IgnoreEvent(e);next+=e.shiftKey?-1:1;}
                else return;
                Highlight(next);e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            overlay.RegisterCallback<NavigationMoveEvent>(e=>{if(e.direction==NavigationMoveEvent.Direction.Down||e.direction==NavigationMoveEvent.Direction.Up){Highlight(highlighted+(e.direction==NavigationMoveEvent.Direction.Down?1:-1));e.StopImmediatePropagation();}});
            host.Add(overlay);host.RegisterCallback<GeometryChangedEvent>(HostGeometry);Place();Highlight(Choices.ToList().IndexOf(Value));
        }
        private void HostGeometry(GeometryChangedEvent _)=>Place();
        private void Place()
        {
            if(host==null||popup==null||overlay==null)return;
            var bounds=host.worldBound;var screen=host.panel.visualTree.worldBound;
            var min=host.WorldToLocal(new Vector2(Mathf.Max(bounds.xMin,screen.xMin),Mathf.Max(bounds.yMin,screen.yMin)));
            var max=host.WorldToLocal(new Vector2(Mathf.Min(bounds.xMax,screen.xMax),Mathf.Min(bounds.yMax,screen.yMax)));
            var area=new Rect(min,max-min);var origin=host.WorldToLocal(trigger.worldBound.position);
            var width=Mathf.Min(Mathf.Max(80,area.width-16),Mathf.Max(trigger.worldBound.width,Mathf.Min(420,Choices.Max(v=>v.Length)*8+64)));
            var below=area.yMax-(origin.y+trigger.worldBound.height)-8;var above=origin.y-area.yMin-8;
            var height=Mathf.Min(Choices.Count*48+12,Mathf.Max(below,above));height=Mathf.Max(44,Mathf.Min(area.height-16,height));
            popup.style.width=width;popup.style.height=height;popup.style.left=Mathf.Clamp(origin.x,area.xMin+8,Mathf.Max(area.xMin+8,area.xMax-width-8));
            popup.style.top=Mathf.Clamp(below>=height?origin.y+trigger.worldBound.height:origin.y-height,area.yMin+8,Mathf.Max(area.yMin+8,area.yMax-height-8));
        }
        private void Highlight(int index)
        {if(items.Count==0)return;highlighted=(index%items.Count+items.Count)%items.Count;for(int i=0;i<items.Count;i++)items[i].EnableInClassList("lcars-selector-highlight",i==highlighted);items[highlighted].Focus();scroll?.ScrollTo(items[highlighted]);}
        private void Choose(int index)
        {
            if(overlay==null)return;var valid=panel!=null&&enabledInHierarchy&&current();var next=Choices[index];Close(true);
            if(!valid||next==Value)return;Value=next;trigger.text=next+"  ▾";trigger.tooltip=next;changed(next);
        }
        private void Close(bool restore)
        {
            if(ReferenceEquals(open,this))open=null;
            host?.UnregisterCallback<GeometryChangedEvent>(HostGeometry);overlay?.RemoveFromHierarchy();overlay=popup=host=null;scroll=null;items.Clear();dismissPointer=-1;
            if(restore&&panel!=null&&enabledInHierarchy)trigger.Focus();
        }
    }
}
