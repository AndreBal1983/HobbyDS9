#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.UnityClient
{
    // Host-loaded prototype content, separate from campaign content and authority.
    public sealed class DesignerFixture
    {
        public ShipDesignCatalogueDto Catalogue { get; set; } = null!;
        public ShipDesignAccessDto Access { get; set; } = null!;
        public List<ShipClassDraftDto> Drafts { get; set; } = null!;
    }
    // Presentation adapter only. The portable Application owns every computed fact.
    public sealed class FixtureShipDesignPreviewProvider : IShipDesignPreviewProvider
    {
        public string StatusLabel => "Backend validated prototype • Construction integration pending";
        public bool FixtureResults => false;
        public IReadOnlyList<DesignerHull> Hulls { get; }
        public IReadOnlyList<DesignerComponent> Catalogue { get; }
        public IReadOnlyList<DesignerTemplate> Templates { get; }
        public IReadOnlyList<DesignerRole> Roles { get; }
        public ShipDesigner Designer { get; }
        private readonly string[] available;
        public FixtureShipDesignPreviewProvider(DesignerFixture fixture)
        {
            var creation = ShipDesignApplication.Create(fixture.Catalogue);
            Designer = creation.Designer ?? throw new ArgumentException(string.Join("; ",creation.Issues.Select(i=>i.Message)));
            available = fixture.Access.AvailableVersionIds!.ToArray();
            Hulls = Designer.Catalogue.Hulls.Select(h=>new DesignerHull(h.VersionId,h.DisplayName,
                $"{h.Capacity} capacity • {h.PowerDistributionLimit} distribution • crew minimum {h.MinimumCrew}")).ToList().AsReadOnly();
            Roles = Designer.Catalogue.Roles.Select(r=>new DesignerRole(r.RoleId,r.DisplayName)).ToList().AsReadOnly();
            Catalogue = Designer.Catalogue.Components.Select(c=>new DesignerComponent(c.VersionId,c.DisplayName,
                c.Kind==ComponentKind.Shields || c.Kind==ComponentKind.EnergyWeapon || c.Kind==ComponentKind.TorpedoWeapon ? "Defence" :
                c.Kind==ComponentKind.Sensors || c.Kind==ComponentKind.Science || c.Kind==ComponentKind.Support ? "Mission" : "Core",
                $"{c.Kind} • {c.VersionId}\n"+string.Join(" • ",c.Modes.Select(m=>$"{m.Profile}: full {m.FullPower}, minimum {m.MinimumPower}, {m.Scaling}")),
                $"{c.Capacity} capacity • crew {c.CrewRequired} • computer demand {c.ComputerDemand} • supply {c.PowerSupply} • envelope {c.EnvelopeSize}\n"+
                $"{c.Cost.Credits} credits / {c.Cost.Industry} industry / upkeep {c.Cost.Maintenance}\n"+
                string.Join(" • ",c.Mounts.Select(m=>$"{m.Kind}: {m.Count} mounts, size {m.SizeRating}")))).ToList().AsReadOnly();
            Templates = fixture.Drafts.Select(d=>new DesignerTemplate(d.ClassId,d.DisplayName,new DesignerFit(d))).ToList().AsReadOnly();
        }
        public ShipDesignAccessDto Access() => new ShipDesignAccessDto { AvailableVersionIds=available.ToList() };
        public ShipDesignPreview Evaluate(DesignerFit fit) => Designer.Preview(fit.ToDto(),Access());
        private static DesignerMetric M(string key,object value) => new DesignerMetric(key,Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture) ?? "—");
        public DesignerPreview Preview(DesignerFit fit,DesignerProfile profile)
        {
            var result=Evaluate(fit);
            var selected=result.Profiles.SingleOrDefault(p=>p.Kind==DesignerFit.Kind(profile));
            var power=new List<DesignerMetric>(); var capabilities=new List<DesignerMetric>(); var costs=new List<DesignerMetric>();
            if(selected!=null)
            {
                power.Add(M("Available / requested",$"{selected.AvailablePower} / {selected.RequestedPower}"));
                power.Add(M("Actual allocation",selected.AllocatedPower));
                power.Add(M("Installed peak",selected.InstalledPeakPower));
                foreach(var output in selected.Outputs) capabilities.Add(M(output.Kind.ToString(),output.Amount));
                foreach(var installation in selected.Installations)
                    capabilities.Add(M(installation.InstallationId,string.Join(" • ",installation.Outputs.Select(o=>$"{o.Kind} {o.Amount}"))));
            }
            capabilities.Add(M("Required / embarked crew",$"{result.RequiredCrew} / {fit.EmbarkedCrew}"));
            foreach(var mount in result.Mounts) capabilities.Add(M(mount.MountId,$"{mount.Used}/{mount.Limit} {mount.Kind}, size {mount.SizeRating}"));
            foreach(var envelope in result.Envelopes) capabilities.Add(M(envelope.Kind+" envelope",$"{envelope.PackagesUsed}/{envelope.PackageLimit} packages • size {envelope.LargestSizeUsed}/{envelope.SizeLimit}"));
            if(result.Compiled!=null) capabilities.Add(M("Compiled hull / movement",$"{result.Compiled.MaxHull} / {result.Compiled.MovementBudget}"));
            if(result.Cost!=null)
            {
                costs.Add(M("Construction",$"{result.Cost.Credits} credits / {result.Cost.Industry} industry"));
                costs.Add(M("Maintenance (informational)",result.Cost.Maintenance));
                foreach(var resource in result.Cost.Resources) costs.Add(M(resource.ResourceDefinitionId,resource.Amount));
            }
            var dto=fit.ToDto();
            var issues=result.Issues.Select(i=>new DesignerIssue(
                dto.Installations!.FirstOrDefault(x=>i.Path.Contains(x.InstallationId))?.ComponentVersionId ?? "",
                i.Severity==DesignIssueSeverity.Error?DesignerStatus.HardError:DesignerStatus.Warning,
                i.Message+"\n"+i.Code+" • "+i.Path));
            return new DesignerPreview(!result.IsValid?DesignerStatus.HardError:result.Issues.Count>0?DesignerStatus.Warning:DesignerStatus.Valid,
                "Portable backend preview • all profiles validated • no campaign order", result.CapacityUsed,result.CapacityLimit,
                result.CapacityLimit.HasValue && result.CapacityUsed.HasValue ? result.CapacityLimit-result.CapacityUsed : null,
                power,capabilities,costs,issues);
        }
    }
}
