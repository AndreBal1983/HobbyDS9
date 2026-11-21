#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    // Presentation of detached values only; no affordability, support or queue policy.
    public static class DenseConstructionPresentation
    {
        public static string Number(long? value)=>value.HasValue?value.Value.ToString("N0",CultureInfo.InvariantCulture):"?";
        public static string Delta(long value)=>(value>0?"+":"")+Number(value);
        public static string Arms(ActivityBuildComparisonDto? value)=>value?.Weapons==null?"?":value.Weapons.Count==0?"Unarmed":Number(value.Weapons.Count);
        public static string MaterialCode(string id)=>id=="resource.dilithium"?"DIL":id=="resource.settlement-kit"?"KIT":"MAT";
        public static string MaterialName(string id)=>id=="resource.dilithium"?"Dilithium":id=="resource.settlement-kit"?"Settlement kits":"Material";
        public static string Materials(IEnumerable<ResourceAmountDto>? required)
        {
            var values=required?.Where(r=>r.Amount!=0).ToArray();
            return values==null||values.Length==0?"None":string.Join(" · ",values.Select(r=>MaterialCode(r.ResourceDefinitionId)+" "+Number(r.Amount)));
        }
        public static string MaterialDetails(IEnumerable<ResourceAmountDto>? required,IEnumerable<ResourceAmountSnapshot> stocks)
        {
            var values=required?.Where(r=>r.Amount!=0).ToArray();
            if(values==null||values.Length==0)return "No construction materials required.";
            return string.Join("\n",values.Select(r=>MaterialName(r.ResourceDefinitionId)+": required "+Number(r.Amount)+" / local "+Number(stocks.Where(s=>s.ResourceDefinitionId==r.ResourceDefinitionId).Sum(s=>s.Amount))))+
                "\nMaterials are consumed as work advances; a shortage pauses progress.";
        }
    }
}
