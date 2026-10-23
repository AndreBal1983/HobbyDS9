#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HobbyDS9.Contracts;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec
    {
        public byte[] EncodeRouteDraft(GeneratedGalaxyRouteDraftDocument document)
        {
            JObject Anchor(GeneratedGalaxyRouteDraftAnchorDto row)=>new JObject{{"NodeId",row.NodeId},{"Kind",(int)row.Kind}};
            var root=new JObject{{"FormatVersion",document.FormatVersion},{"MapContentHash",document.MapContentHash},{"PolicyVersion",document.PolicyVersion},
                {"Start",Anchor(document.Start)},{"Targets",new JArray(document.Targets.Select(Anchor))}};
            var bytes=Utf8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None));DecodeRouteDraft(bytes);return bytes;
        }
        public GeneratedGalaxyRouteDraftDocument DecodeRouteDraft(byte[] bytes)
        {
            try
            {
                var root=GeneratedRoot(bytes,GeneratedGalaxyRouteDraftStore.MaximumBytes);
                GeneratedObject(root,"FormatVersion","MapContentHash","PolicyVersion","Start","Targets");
                if(root["FormatVersion"]?.Type!=JTokenType.Integer||root["FormatVersion"]!.Value<int>()!=1)throw Invalid();
                if(root["MapContentHash"]?.Type!=JTokenType.String||!Regex.IsMatch(root["MapContentHash"]!.Value<string>()!,@"\A[0-9a-f]{64}\z"))throw Invalid();
                if(root["PolicyVersion"]?.Type!=JTokenType.String||root["PolicyVersion"]!.Value<string>()!=GalaxyReviewRoutePolicy.MinimumCrossingsDirectV1)throw Invalid();
                void Anchor(JToken? anchor)
                {
                    GeneratedObject(anchor,"NodeId","Kind");
                    if(anchor!["NodeId"]?.Type!=JTokenType.String||!Regex.IsMatch(anchor["NodeId"]!.Value<string>()!,@"\A[a-z][a-z0-9._-]{0,127}\z"))throw Invalid();
                    if(anchor["Kind"]?.Type!=JTokenType.Integer||anchor["Kind"]!.Value<int>()<0||anchor["Kind"]!.Value<int>()>1)throw Invalid();
                }
                Anchor(root["Start"]);foreach(var target in GeneratedRows(root["Targets"],GalaxyReviewRoutePolicy.MaximumTargets))Anchor(target);
                GeneratedGalaxyRouteDraftAnchorDto Copy(JToken row)=>new GeneratedGalaxyRouteDraftAnchorDto{
                    NodeId=row["NodeId"]!.Value<string>()!,Kind=(GeneratedGalaxyDraftAnchorKind)row["Kind"]!.Value<int>()};
                // Explicit host DTO construction; the generic physical codec intentionally admits Contracts types only.
                return new GeneratedGalaxyRouteDraftDocument{MapContentHash=root["MapContentHash"]!.Value<string>()!,
                    PolicyVersion=root["PolicyVersion"]!.Value<string>()!,Start=Copy(root["Start"]!),Targets=root["Targets"]!.Select(Copy).ToList()};
            }
            catch(Exception){throw new InvalidDataException("Invalid route-draft companion.");}
        }
    }
}
