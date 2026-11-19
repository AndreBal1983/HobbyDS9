using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts.ShipDesign;
namespace HobbyDS9.Simulation
{
    public static partial class PublishedTacticalSupportCompiler
    {
        private const string RefinementComponentPrefix="component.refinement.";
        public static ColonisationSupportCatalogueDto CreateRefinementSupport(ColonisationSupportCatalogueDto support)
        {
            if(support==null||support.FormatVersion!=ColonisationSupportIdentity.Format||support.SchemaId!=ColonisationSupportIdentity.Schema
                ||support.CompilerId!=ColonisationSupportIdentity.Compiler||support.Activity?.Tactical?.Components==null||support.Activity.Modules==null||support.SettlerModules==null)
                throw new ArgumentException("A complete explicit v4 support source is required.",nameof(support));
            var copy=ColonisationSupportCopies.Copy(support);
            copy.FormatVersion=ColonisationRefinementSupportIdentity.Format;copy.SchemaId=ColonisationRefinementSupportIdentity.Schema;copy.CompilerId=ColonisationRefinementSupportIdentity.Compiler;
            return copy;
        }
        private static bool ColonisationSupportVersion(ColonisationSupportCatalogueDto s)
            =>s.FormatVersion==ColonisationSupportIdentity.Format&&s.SchemaId==ColonisationSupportIdentity.Schema&&s.CompilerId==ColonisationSupportIdentity.Compiler
                ||s.FormatVersion==ColonisationRefinementSupportIdentity.Format&&s.SchemaId==ColonisationRefinementSupportIdentity.Schema&&s.CompilerId==ColonisationRefinementSupportIdentity.Compiler;
        private static ColonisationSupportCatalogueDto RefinementReactorMappings(ShipDesignCatalogueSnapshot catalogue,PublishedShipBlueprint publication,ColonisationSupportCatalogueDto support)
        {
            // A source map cannot assert support for a generated ID. Only actual installed,
            // frozen ancestry may derive it; unrelated newer catalogue entries are ignored.
            if(support.Activity!.Tactical!.Components!.Any(c=>c.ComponentVersionId?.StartsWith(RefinementComponentPrefix,StringComparison.Ordinal)==true)
                ||support.Activity.Modules!.Any(m=>m.ComponentVersionId?.StartsWith(RefinementComponentPrefix,StringComparison.Ordinal)==true)
                ||support.SettlerModules!.Any(m=>m.ComponentVersionId?.StartsWith(RefinementComponentPrefix,StringComparison.Ordinal)==true))
                throw new ArgumentException("Generated refinement support must derive from frozen reactor ancestry.");
            var source=catalogue.Components.ToDictionary(c=>c.VersionId,StringComparer.Ordinal);
            var roots=support.Activity.Tactical.Components.ToDictionary(c=>c.ComponentVersionId!,StringComparer.Ordinal);
            var copy=ColonisationSupportCopies.Copy(support);
            foreach(var id in publication.Fit.Installations.Select(i=>i.ComponentVersionId).Distinct(StringComparer.Ordinal).OrderBy(v=>v,StringComparer.Ordinal))
            {
                if(id==null)throw new ArgumentException("Missing installed component identity.");
                if(!id.StartsWith(RefinementComponentPrefix,StringComparison.Ordinal))continue;
                if(!source.TryGetValue(id,out var current))throw new ArgumentException("Missing frozen refinement source.");
                var visited=new HashSet<string>(StringComparer.Ordinal);
                while(!roots.ContainsKey(current.VersionId))
                {
                    if(!visited.Add(current.VersionId)||!current.VersionId.StartsWith(RefinementComponentPrefix,StringComparison.Ordinal)
                        ||current.ParentVersionId==null||!source.TryGetValue(current.ParentVersionId,out var parent)||!SameRefinementReactor(parent,current))
                        throw new ArgumentException("Unsupported frozen reactor refinement path.");
                    current=parent;
                }
                if(current.Kind!=ComponentKind.Reactor||roots[current.VersionId].Kind!=TacticalComponentSupportKind.Reactor)
                    throw new ArgumentException("Refinement root is not an explicitly supported reactor.");
                copy.Activity!.Tactical!.Components!.Add(new TacticalComponentSupportDto{ComponentVersionId=id,Kind=TacticalComponentSupportKind.Reactor});
            }
            return copy;
        }
        private static bool SameRefinementReactor(ComponentVersionSnapshot parent,ComponentVersionSnapshot child)
        {
            // Identity/name and the three authorized quantitative attributes may change.
            // Every functional family, compatibility, demand, tag, mount and mode fact stays exact.
            return parent.Kind==ComponentKind.Reactor&&child.Kind==ComponentKind.Reactor&&parent.FamilyId==child.FamilyId
                &&child.VersionNumber>parent.VersionNumber&&child.ParentVersionId==parent.VersionId&&child.Capacity>0&&child.PowerSupply>0&&child.Cost.Industry>0
                &&parent.CrewRequired==child.CrewRequired&&parent.ComputerDemand==child.ComputerDemand&&parent.EnvelopeSize==child.EnvelopeSize
                &&parent.CompatibleHullFamilyIds.SequenceEqual(child.CompatibleHullFamilyIds,StringComparer.Ordinal)
                &&parent.RequiredHullTags.SequenceEqual(child.RequiredHullTags,StringComparer.Ordinal)&&parent.Tags.SequenceEqual(child.Tags,StringComparer.Ordinal)
                &&parent.RequiredComponentTags.SequenceEqual(child.RequiredComponentTags,StringComparer.Ordinal)
                &&parent.Mounts.Select(m=>(m.Kind,m.SizeRating,m.Count,m.CoverageTag)).SequenceEqual(child.Mounts.Select(m=>(m.Kind,m.SizeRating,m.Count,m.CoverageTag)))
                &&parent.Dependencies.Select(d=>(d.Kind,d.Amount)).SequenceEqual(child.Dependencies.Select(d=>(d.Kind,d.Amount)))
                &&parent.Cost.Credits==child.Cost.Credits&&parent.Cost.Maintenance==child.Cost.Maintenance
                &&parent.Cost.Resources.Select(r=>(r.ResourceDefinitionId,r.Amount)).SequenceEqual(child.Cost.Resources.Select(r=>(r.ResourceDefinitionId,r.Amount)))
                &&parent.Modes.Count==child.Modes.Count&&parent.Modes.Zip(child.Modes,(a,b)=>a.Profile==b.Profile&&a.FullPower==b.FullPower&&a.MinimumPower==b.MinimumPower
                    &&a.Scaling==b.Scaling&&a.Essential==b.Essential&&a.Outputs.Select(o=>(o.Kind,o.Amount)).SequenceEqual(b.Outputs.Select(o=>(o.Kind,o.Amount)))).All(same=>same);
        }
    }
}
