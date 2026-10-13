#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.UnityClient
{
    // Presentation-local editing; DTOs cross only the Application boundary.
    public enum DesignerProfile { Cruise, Combat, Survey }
    public enum DesignerStatus { Valid, Warning, HardError, NotEvaluated }
    public sealed class DesignerRole
    {
        public string Id { get; }
        public string Caption { get; }
        public DesignerRole(string id,string caption) { Id=id; Caption=caption; }
    }
    public sealed class DesignerHull
    {
        public string Id { get; }
        public string Caption { get; }
        public string Facts { get; }
        public DesignerHull(string id,string caption,string facts) { Id=id; Caption=caption; Facts=facts; }
    }
    public sealed class DesignerComponent
    {
        public string Id { get; }
        public string Name { get; }
        public string Group { get; }
        public string Description { get; }
        public string Facts { get; }
        public string SystemId { get; }
        public DesignerComponent(string id,string name,string group,string description,string facts)
            : this(id,name,group,description,facts,group) { }
        public DesignerComponent(string id,string name,string group,string description,string facts,string systemId)
        { Id=id; Name=name; Group=group; Description=description; Facts=facts; SystemId=systemId; }
    }
    public sealed class DesignerFit
    {
        private readonly ShipClassDraftDto value;
        public string Name => value.DisplayName;
        public string HullId => value.HullVersionId;
        public string Role => value.RoleId;
        public long EmbarkedCrew => value.EmbarkedCrew;
        public IReadOnlyDictionary<string,int> Installed { get; }
        public DesignerFit(ShipClassDraftDto dto)
        {
            value=Copy(dto);
            Installed=new System.Collections.ObjectModel.ReadOnlyDictionary<string,int>(value.Installations!.ToDictionary(i=>i.ComponentVersionId,i=>checked((int)i.Quantity),StringComparer.Ordinal));
        }
        public ShipClassDraftDto ToDto() => Copy(value);
        public static OperatingProfileKind Kind(DesignerProfile profile) => (OperatingProfileKind)((int)profile+1);
        public ProfilePowerAllocationDto Power(string component,DesignerProfile profile)
        {
            var id=value.Installations!.Single(i=>i.ComponentVersionId==component).InstallationId;
            var p=value.Profiles!.Single(x=>x.Kind==Kind(profile)).Allocations!.Single(x=>x.InstallationId==id);
            return new ProfilePowerAllocationDto { InstallationId=id,RequestedPower=p.RequestedPower,AllocatedPower=p.AllocatedPower };
        }
        public bool SameAs(DesignerFit other)
        {
            var b=other.value;
            return value.ClassId==b.ClassId && Name==other.Name && HullId==other.HullId && Role==other.Role && EmbarkedCrew==other.EmbarkedCrew
                && value.Installations!.Count==b.Installations!.Count && value.Installations.All(i=>b.Installations.Any(x=>x.InstallationId==i.InstallationId && x.ComponentVersionId==i.ComponentVersionId && x.Quantity==i.Quantity))
                && value.Profiles!.All(p=>p.Allocations!.Count==b.Profiles!.Single(x=>x.Kind==p.Kind).Allocations!.Count && p.Allocations.All(a=>b.Profiles!.Single(x=>x.Kind==p.Kind).Allocations!.Any(x=>x.InstallationId==a.InstallationId && x.RequestedPower==a.RequestedPower && x.AllocatedPower==a.AllocatedPower)));
        }
        private static ShipClassDraftDto Copy(ShipClassDraftDto d) => new ShipClassDraftDto
        {
            ClassId=d.ClassId,DisplayName=d.DisplayName,RoleId=d.RoleId,HullVersionId=d.HullVersionId,EmbarkedCrew=d.EmbarkedCrew,
            Installations=d.Installations!.Select(i=>new ShipInstallationDto { InstallationId=i.InstallationId,ComponentVersionId=i.ComponentVersionId,Quantity=i.Quantity }).ToList(),
            Profiles=d.Profiles!.Select(p=>new ShipOperatingProfileDto { Kind=p.Kind,Allocations=p.Allocations!.Select(a=>new ProfilePowerAllocationDto { InstallationId=a.InstallationId,RequestedPower=a.RequestedPower,AllocatedPower=a.AllocatedPower }).ToList() }).ToList()
        };
    }
    public sealed class DesignerTemplate
    {
        public string Id { get; }
        public string Caption { get; }
        public DesignerFit Fit { get; }
        public DesignerTemplate(string id,string caption,DesignerFit fit) { Id=id; Caption=caption; Fit=fit; }
    }
    public sealed class DesignerIssue
    {
        public string SystemId { get; }
        public DesignerStatus Severity { get; }
        public string Explanation { get; }
        public DesignerProfile? Profile { get; }
        public DesignerIssue(string systemId,DesignerStatus severity,string explanation,DesignerProfile? profile=null) { SystemId=systemId; Severity=severity; Explanation=explanation; Profile=profile; }
    }
    public sealed class DesignerMetric
    {
        public string Caption { get; }
        public string Value { get; }
        public DesignerMetric(string caption,string value) { Caption=caption; Value=value; }
    }
    // Detached presentation range: full/minimum power comes from Application's stack preview.
    public sealed class DesignerPowerRange
    {
        public string ComponentId { get; }
        public long Maximum { get; }
        public long PoweredMinimum { get; }
        public DesignerPowerRange(string componentId,long maximum,long poweredMinimum)
        { ComponentId=componentId; Maximum=maximum; PoweredMinimum=poweredMinimum; }
    }
    public sealed class DesignerPreview
    {
        public IReadOnlyList<DesignerPowerRange> PowerRanges { get; }
        public DesignerStatus Status { get; }
        public string Notice { get; }
        public long? UsedCapacity { get; }
        public long? TotalCapacity { get; }
        public long? FreeCapacity { get; }
        public IReadOnlyList<DesignerMetric> Power { get; }
        public IReadOnlyList<DesignerMetric> Capabilities { get; }
        public IReadOnlyList<DesignerMetric> Costs { get; }
        public IReadOnlyList<DesignerIssue> Issues { get; }
        public DesignerPreview(DesignerStatus status,string notice,long? used=null,long? total=null,long? free=null,
            IEnumerable<DesignerMetric>? power=null,IEnumerable<DesignerMetric>? capabilities=null,IEnumerable<DesignerMetric>? costs=null,IEnumerable<DesignerIssue>? issues=null,IEnumerable<DesignerPowerRange>? powerRanges=null)
        {
            PowerRanges=(powerRanges??Array.Empty<DesignerPowerRange>()).ToList().AsReadOnly();
            Status=status; Notice=notice; UsedCapacity=used; TotalCapacity=total; FreeCapacity=free;
            Power=(power??Array.Empty<DesignerMetric>()).ToList().AsReadOnly(); Capabilities=(capabilities??Array.Empty<DesignerMetric>()).ToList().AsReadOnly();
            Costs=(costs??Array.Empty<DesignerMetric>()).ToList().AsReadOnly(); Issues=(issues??Array.Empty<DesignerIssue>()).ToList().AsReadOnly();
        }
    }
    public interface IShipDesignPreviewProvider
    {
        string StatusLabel { get; }
        bool FixtureResults { get; }
        IReadOnlyList<DesignerHull> Hulls { get; }
        IReadOnlyList<DesignerRole> Roles { get; }
        IReadOnlyList<DesignerComponent> Catalogue { get; }
        IReadOnlyList<DesignerTemplate> Templates { get; }
        DesignerPreview Preview(DesignerFit fit,DesignerProfile profile);
    }
    public interface IShipDesignCatalogueFacts
    {
        bool Available(string id);
        IReadOnlyList<DesignerMetric> ComponentFacts(string id,DesignerProfile profile);
    }
    public sealed class ShipDesignDraft
    {
        public IShipDesignPreviewProvider Provider { get; private set; }
        public DesignerFit Starting { get; private set; } = null!;
        public string TemplateId { get; private set; } = "";
        public DesignerProfile Profile { get; private set; } = DesignerProfile.Cruise;
        public DesignerFit Current => new DesignerFit(edit);
        public bool Modified => !Current.SameAs(Starting);
        public bool ConfirmationPending => pending!=null;
        public event Action? Changed;
        private ShipClassDraftDto edit=null!;
        private readonly Dictionary<string,string> knownIds=new Dictionary<string,string>(StringComparer.Ordinal);
        private Action? pending;
        public ShipDesignDraft(IShipDesignPreviewProvider provider) { Provider=provider; StartTemplate(provider.Templates.First().Id); }
        private void Notify() => Changed?.Invoke();
        private void CopyStarting() { edit=Starting.ToDto(); }
        public void RefreshProvider(IShipDesignPreviewProvider provider) { Provider=provider; Notify(); }
        public void StartFit(DesignerFit fit)
        {
            Starting=new DesignerFit(fit.ToDto()); CopyStarting(); TemplateId="";
            knownIds.Clear(); foreach(var i in edit.Installations!)knownIds.Add(i.ComponentVersionId,i.InstallationId);
            pending=null; Notify();
        }
        public void SetClassId(string id) { edit.ClassId=id; Notify(); }
        public void MarkPublished() { Starting=Current; Notify(); }
        private void StartTemplate(string id)
        {
            TemplateId=id; Starting=new DesignerFit(Provider.Templates.Single(t=>t.Id==id).Fit.ToDto()); CopyStarting();
            knownIds.Clear(); foreach(var i in edit.Installations!) knownIds.Add(i.ComponentVersionId,i.InstallationId);
            Profile=DesignerProfile.Cruise; pending=null; Notify();
        }
        public void Rename(string value) { if(edit.DisplayName==value)return; edit.DisplayName=value; Notify(); }
        public void SetRole(string value) { if(edit.RoleId==value)return; edit.RoleId=value; Notify(); }
        public void SetCrew(long value) { edit.EmbarkedCrew=value; Notify(); }
        public void SetHull(string value)
        {
            if(!Provider.Hulls.Any(h=>h.Id==value))throw new ArgumentException("Unknown hull version.",nameof(value));
            edit.HullVersionId=value; Notify();
        }
        public void SelectProfile(DesignerProfile value) { Profile=value; Notify(); }
        public void SetPower(string component,long requested,long allocated)
        {
            var id=edit.Installations!.Single(i=>i.ComponentVersionId==component).InstallationId;
            var a=edit.Profiles!.Single(p=>p.Kind==DesignerFit.Kind(Profile)).Allocations!.Single(x=>x.InstallationId==id);
            a.RequestedPower=requested; a.AllocatedPower=allocated; Notify(); // Never clamp or auto-allocate.
        }
        public void SetQuantity(string componentId,int quantity)
        {
            if(quantity<0)throw new ArgumentOutOfRangeException(nameof(quantity));
            if(!Provider.Catalogue.Any(c=>c.Id==componentId))throw new ArgumentException("Unknown catalogue item.",nameof(componentId));
            var item=edit.Installations!.SingleOrDefault(i=>i.ComponentVersionId==componentId);
            if(quantity==0)
            {
                if(item!=null) { edit.Installations!.Remove(item); foreach(var p in edit.Profiles!)p.Allocations!.RemoveAll(a=>a.InstallationId==item.InstallationId); }
            }
            else if(item!=null)item.Quantity=quantity;
            else
            {
                if(!knownIds.TryGetValue(componentId,out var id) || edit.Installations!.Any(i=>i.InstallationId==id)) { id="fit.local."+componentId; var suffix=1; while(edit.Installations!.Any(i=>i.InstallationId==id)) id="fit.local."+componentId+".n"+suffix++; knownIds[componentId]=id; }
                edit.Installations!.Add(new ShipInstallationDto { InstallationId=id,ComponentVersionId=componentId,Quantity=quantity });
                foreach(var p in edit.Profiles!)p.Allocations!.Add(new ProfilePowerAllocationDto { InstallationId=id,RequestedPower=0,AllocatedPower=0 });
            }
            Notify();
        }
        public void Add(string id) => SetQuantity(id,checked((Current.Installed.TryGetValue(id,out var count)?count:0)+1));
        public void Remove(string id) => SetQuantity(id,0);
        public void Replace(string oldId,string replacementId)
        {
            if(oldId==replacementId)return;
            if(!Provider.Catalogue.Any(c=>c.Id==replacementId))throw new ArgumentException("Unknown replacement.",nameof(replacementId));
            var item=edit.Installations!.SingleOrDefault(i=>i.ComponentVersionId==oldId) ?? throw new ArgumentException("Select an installed item first.",nameof(oldId));
            var existing=edit.Installations!.SingleOrDefault(i=>i.ComponentVersionId==replacementId);
            if(existing!=null) { var total=checked((int)(existing.Quantity+item.Quantity)); Remove(oldId); SetQuantity(replacementId,total); }
            else
            {
                item.ComponentVersionId=replacementId; knownIds[replacementId]=item.InstallationId;
                foreach(var p in edit.Profiles!) { var a=p.Allocations!.Single(x=>x.InstallationId==item.InstallationId); a.RequestedPower=0; a.AllocatedPower=0; }
                Notify();
            }
        }
        public void RequestTemplate(string id)
        {
            if(!Provider.Templates.Any(t=>t.Id==id))throw new ArgumentException("Unknown template.",nameof(id));
            RequestDiscard(()=>StartTemplate(id));
        }
        public void RequestReset() => RequestDiscard(()=> { CopyStarting(); Notify(); });
        public void RequestDiscard(Action afterDiscard) { if(Modified) { pending=afterDiscard; Notify(); } else afterDiscard(); }
        public void CancelDiscard() { pending=null; Notify(); }
        public void ConfirmDiscard() { var action=pending; if(action==null)return; pending=null; CopyStarting(); action(); Notify(); }
        public DesignerPreview Preview(bool starting=false)
        {
            try { return Provider.Preview(starting?Starting:Current,Profile); }
            catch { return new DesignerPreview(DesignerStatus.NotEvaluated,"Preview provider unavailable. Your local draft is retained; no campaign order was sent."); }
        }
        // Detached local copy for proposed-edit previews; uses these same edit APIs.
        public ShipDesignDraft PreviewCopy()
        {
            var copy=new ShipDesignDraft(Provider);
            copy.TemplateId=TemplateId; copy.Starting=Starting; copy.edit=Current.ToDto(); copy.Profile=Profile;
            copy.knownIds.Clear(); foreach(var pair in knownIds) copy.knownIds.Add(pair.Key,pair.Value);
            return copy;
        }
    }
}
