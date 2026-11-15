#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
namespace HobbyDS9.Application
{
    public enum EngineeringOutputKind { Module,Hull,ShipDesign }
    // Immutable public catalogue relation, not execution, chosen experiment parent or equipment maturity.
    public sealed class EngineeringOutputSnapshot
    {
        public EngineeringOutputKind Kind {get;} public string Identity {get;} public string DisplayName {get;}
        public ComponentVersionSnapshot? Component {get;} public ComponentVersionSnapshot? ParentComponent {get;}
        public HullVersionSnapshot? Hull {get;} public HullVersionSnapshot? ParentHull {get;}
        public IReadOnlyList<DefinitionId> RequiredProjects {get;}
        private EngineeringOutputSnapshot(EngineeringOutputKind kind,string id,string name,IEnumerable<string> required,
            ComponentVersionSnapshot? component=null,ComponentVersionSnapshot? parentComponent=null,HullVersionSnapshot? hull=null,HullVersionSnapshot? parentHull=null)
        {Kind=kind;Identity=id;DisplayName=name;Component=component;ParentComponent=parentComponent;Hull=hull;ParentHull=parentHull;RequiredProjects=required.OrderBy(v=>v,StringComparer.Ordinal).Select(v=>new DefinitionId(v)).ToList().AsReadOnly();}
        internal static IReadOnlyList<EngineeringOutputSnapshot> For(string project,DefinitionCatalogDto content)
        {
            var result=new List<EngineeringOutputSnapshot>();var catalogue=content.ShipDesignCatalogue;
            if(catalogue!=null)
            foreach(var gate in (content.ShipDesignVersionAccess??new List<ShipDesignVersionAccessDto>()).Where(g=>g.RequiredEngineeringApplicationDefinitionIds?.Contains(project)==true).OrderBy(g=>g.VersionId,StringComparer.Ordinal))
            {
                var required=gate.RequiredEngineeringApplicationDefinitionIds!;
                var component=catalogue.Components!.FirstOrDefault(c=>c.VersionId==gate.VersionId);
                if(component!=null)
                {
                    var parent=catalogue.Components.FirstOrDefault(c=>c.VersionId==component.ParentVersionId);
                    result.Add(new EngineeringOutputSnapshot(EngineeringOutputKind.Module,component.VersionId,component.DisplayName,required,new ComponentVersionSnapshot(component),parent==null?null:new ComponentVersionSnapshot(parent)));continue;
                }
                var hull=catalogue.Hulls!.FirstOrDefault(h=>h.VersionId==gate.VersionId);
                if(hull!=null)
                {
                    var parent=catalogue.Hulls.FirstOrDefault(h=>h.VersionId==hull.ParentVersionId);
                    result.Add(new EngineeringOutputSnapshot(EngineeringOutputKind.Hull,hull.VersionId,hull.DisplayName,required,hull:new HullVersionSnapshot(hull),parentHull:parent==null?null:new HullVersionSnapshot(parent)));
                }
            }
            foreach(var design in (content.ShipDesigns??new List<ShipDesignDefinitionDto>()).Where(d=>d.RequiredEngineeringApplicationDefinitionIds?.Contains(project)==true))
                result.Add(new EngineeringOutputSnapshot(EngineeringOutputKind.ShipDesign,design.DefinitionId,design.DisplayName,design.RequiredEngineeringApplicationDefinitionIds!));
            return result.OrderBy(r=>r.Identity,StringComparer.Ordinal).ToList().AsReadOnly();
        }
    }
}
