#nullable enable
using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient
{
    [Preserve]
    public sealed class LcarsCapacityMeter : VisualElement
    {
        public long UnitsPerBlock {get;} public int BlockCount {get;}
        private string compactLegend="";
        public void Compact()
        {
            style.marginBottom=8;var legend=this.Q<Label>("capacity-legend");if(legend!=null)legend.text=compactLegend;
        }
        public LcarsCapacityMeter(string label,long value,long budget):this(label,value,budget,null,false){}
        [Preserve]public LcarsCapacityMeter(string label,long value,long budget,long? previous,bool higherBetter)
        {
            AddToClassList("lcars-capacity");style.minWidth=0;style.flexShrink=0;style.marginBottom=12;
            var comparison=previous.HasValue;
            string number=comparison?previous+" → "+value:value+" / "+budget;
            var caption=new Label(label+" · "+number){name="capacity-exact"};caption.style.whiteSpace=WhiteSpace.Normal;Add(caption);
            if(value<0||budget<0||previous<0){Add(new Label("Invalid negative value; blocks unavailable."));return;}
            var delta=comparison?value-previous!.Value:0;
            var extent=Math.Max(Math.Max(value,budget),previous??0);UnitsPerBlock=extent<=120?1:extent/120+(extent%120==0?0:1);
            BlockCount=(int)(extent/UnitsPerBlock+(extent%UnitsPerBlock==0?0:1));
            var strip=new VisualElement{name="capacity-blocks"};strip.style.flexDirection=FlexDirection.Row;strip.style.flexWrap=Wrap.Wrap;strip.style.minWidth=0;Add(strip);
            var benefit=comparison&&delta!=0&&(higherBetter?delta>0:delta<0);
            var color=benefit?new Color(.36f,.76f,.53f):comparison&&delta!=0?new Color(.91f,.47f,.39f):new Color(.94f,.69f,.42f);
            VisualElement? group=null;
            for(int i=0;i<BlockCount;i++)
            {
                if(i%10==0){group=new VisualElement();group.AddToClassList("capacity-ten");group.style.flexDirection=FlexDirection.Row;group.style.flexShrink=0;group.style.marginRight=6;group.style.marginBottom=5;strip.Add(group);}
                var start=(long)i*UnitsPerBlock;var units=Math.Min(UnitsPerBlock,extent-start);
                var cell=new VisualElement{tooltip="Capacity units "+(start+1)+"–"+(start+units)};cell.AddToClassList("capacity-cell");
                cell.style.width=8*(float)((double)units/UnitsPerBlock);cell.style.height=11;cell.style.marginRight=2;cell.style.borderLeftWidth=cell.style.borderRightWidth=cell.style.borderTopWidth=cell.style.borderBottomWidth=1;
                var edge=new Color(.34f,.42f,.50f);cell.style.borderLeftColor=cell.style.borderRightColor=cell.style.borderTopColor=cell.style.borderBottomColor=edge;
                var filled=value>start?Math.Min(units,value-start):0;
                cell.AddToClassList(filled>0?"capacity-filled":"capacity-hollow");
                if(filled>0){var fill=new VisualElement{pickingMode=PickingMode.Ignore};fill.style.height=Length.Percent(100);fill.style.width=Length.Percent((float)((double)filled/units*100));fill.style.backgroundColor=!comparison&&start+filled>budget?new Color(.91f,.47f,.39f):color;cell.Add(fill);}
                group!.Add(cell);
            }
            string meaning=comparison?(delta<0?(-delta)+" less":delta>0?delta+" more":"Unchanged")+(benefit?" · beneficial for this measure":""):
                value>budget?(value-budget)+" over budget":(budget-value)+" unused";
            compactLegend=meaning+" · 1 block = "+UnitsPerBlock+(UnitsPerBlock==1?" unit":" units; partial end");
            var legend=new Label(meaning+" · filled: current value · hollow: "+(comparison&&value<previous?(higherBetter?"lost capacity":"freed positions"):"unused reference")+" · 1 block = "+UnitsPerBlock+" unit"+(UnitsPerBlock==1?"":"s; final positions may be partial")){name="capacity-legend"};legend.style.whiteSpace=WhiteSpace.Normal;legend.style.fontSize=14;Add(legend);
            tooltip=caption.text+". "+legend.text;
        }
    }
}
