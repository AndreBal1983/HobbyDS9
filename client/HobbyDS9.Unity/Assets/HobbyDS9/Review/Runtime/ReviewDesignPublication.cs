#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.TacticalReviewClient;
using Newtonsoft.Json;
namespace HobbyDS9.UnityReview
{
    // Local publication workspace only. Every preview and command is portable authority.
    public sealed class ReviewDesignPublication
    {
        private readonly ReviewUiModel model;
        private readonly ReviewController owner;
        private ReviewFrame observed;
        private string? publishedFit;
        public CampaignIntegratedDesignLibrarySnapshot Library { get; private set; }
        public string BlueprintId { get; private set; } = "";
        public string ClassId { get; private set; } = "";
        public int NextRevision { get; private set; }
        public string Notice { get; private set; } = "";
        public bool RefreshRequired { get; private set; }
        public bool Current => !model.Closed && ReferenceEquals(model.Current,owner) && ReferenceEquals(model.Current.Frame,observed);
        public ReviewDesignPublication(ReviewUiModel model)
        { this.model=model;owner=model.Current;observed=owner.Frame;Library=model.DesignLibrary();NewIdentity(); }
        public void NewIdentity()
        { BlueprintId="blueprint.player."+Guid.NewGuid().ToString("N");ClassId="class."+BlueprintId;NextRevision=1;publishedFit=null; }
        public bool Refresh(bool announce=true)
        {
            if(model.Closed || model.Busy || !ReferenceEquals(model.Current,owner)) { Notice="Campaign replaced. Open the current campaign designer.";return false; }
            if(!model.Refresh()){Notice=model.Message;return false;}
            Library=model.DesignLibrary();observed=model.Current.Frame;RefreshRequired=false;
            var latest=Library.Published.Where(p=>p.Blueprint.BlueprintId==BlueprintId).Select(p=>p.Blueprint.Revision).DefaultIfEmpty(0).Max();
            NextRevision=latest==int.MaxValue?0:latest+1;
            if(announce)Notice="Current campaign catalogue refreshed; local fit retained.";return true;
        }
        public bool CanPublish(ShipClassDraftDto draft) => Current && !model.Busy && !RefreshRequired && Library.CanPublish && NextRevision>0
            && !owner.HasPendingWork && !owner.Dirty && !owner.RefreshRequired && draft.ClassId==ClassId && publishedFit!=JsonConvert.SerializeObject(draft) && Valid(Library,draft);
        public bool Publish(ShipClassDraftDto draft)
        {
            if(!CanPublish(draft)) { Notice="Publication unavailable. Review validation and refresh the current campaign; your draft is retained.";return false; }
            var accepted=model.PublishDesign(observed,BlueprintId,NextRevision,draft);
            if(!accepted) { RefreshRequired=true;Notice=model.Message+" Local fit retained; refresh before retrying.";return false; }
            publishedFit=JsonConvert.SerializeObject(draft);Library=model.DesignLibrary();observed=model.Current.Frame;
            Notice="Published revision "+NextRevision+".";
            NextRevision=NextRevision==int.MaxValue?0:NextRevision+1;return true;
        }
        public ShipClassDraftDto Edit(CampaignBlueprintSnapshot record)
        {
            if(!Current || !Library.Published.Contains(record))throw new InvalidOperationException("Only this actor's current library may be edited.");
            var d=FromSnapshot(record.Blueprint.Fit);BlueprintId=record.Blueprint.BlueprintId;ClassId=d.ClassId;
            var latest=Library.Published.Where(p=>p.Blueprint.BlueprintId==BlueprintId).Max(p=>p.Blueprint.Revision);
            NextRevision=latest==int.MaxValue?0:latest+1;publishedFit=JsonConvert.SerializeObject(d);return d;
        }
        public static bool Valid(CampaignIntegratedDesignLibrarySnapshot library,ShipClassDraftDto draft) => library.Catalogue!=null &&
            ShipDesignApplication.FromCampaignCatalogue(library.Catalogue.Catalogue).Preview(draft,new ShipDesignAccessDto { AvailableVersionIds=library.Catalogue.AvailableVersionIds.ToList() }).IsValid;
        public static ShipClassDraftDto? Starter(CampaignIntegratedDesignLibrarySnapshot library)
        {
            var source=library.Catalogue;if(source==null)return null;
            var ids=new[]{"component.command.v1","component.life-support.v1","component.propulsion.v1","component.reactor.v1"};
            if(!source.AvailableVersionIds.Contains("hull.utility.v1") || ids.Any(id=>!source.AvailableVersionIds.Contains(id)))return null;
            var draft=new ShipClassDraftDto { ClassId="class.local",DisplayName="Utility class",HullVersionId="hull.utility.v1",RoleId=source.Catalogue.Roles[0].RoleId,EmbarkedCrew=30,
                Installations=ids.Select(id=>new ShipInstallationDto {InstallationId="fit.local."+id,ComponentVersionId=id,Quantity=1}).ToList(),
                Profiles=Enum.GetValues(typeof(OperatingProfileKind)).Cast<OperatingProfileKind>().Select(p=>new ShipOperatingProfileDto {Kind=p,Allocations=ids.Select(id=>{
                    long power=id=="component.command.v1"?4:id=="component.life-support.v1"?6:id=="component.propulsion.v1"?(p==OperatingProfileKind.Cruise?30:p==OperatingProfileKind.Combat?12:8):0;
                    return new ProfilePowerAllocationDto {InstallationId="fit.local."+id,RequestedPower=power,AllocatedPower=power}; }).ToList()}).ToList() };
            return Valid(library,draft)?draft:null;
        }
        public static ShipClassDraftDto FromSnapshot(ShipClassDraftSnapshot d) => new ShipClassDraftDto {ClassId=d.ClassId,DisplayName=d.DisplayName,HullVersionId=d.HullVersionId,RoleId=d.RoleId,EmbarkedCrew=d.EmbarkedCrew,
            Installations=d.Installations.Select(i=>new ShipInstallationDto {InstallationId=i.InstallationId,ComponentVersionId=i.ComponentVersionId,Quantity=i.Quantity}).ToList(),
            Profiles=d.Profiles.Select(p=>new ShipOperatingProfileDto {Kind=p.Kind,Allocations=p.Allocations.Select(a=>new ProfilePowerAllocationDto {InstallationId=a.InstallationId,RequestedPower=a.RequestedPower,AllocatedPower=a.AllocatedPower}).ToList()}).ToList()};
    }
    public sealed partial class ReviewUiModel
    {
        public bool PublishDesign(ReviewFrame observed,string blueprint,int revision,ShipClassDraftDto draft)
        {
            if(Busy || Closed || !ReferenceEquals(Current.Frame,observed)) { Message="Designer observation is stale or unavailable; local fit retained.";return false; }
            var c=StrategicEnvelope(CommandKind.PublishShipBlueprint,NewCommandId());
            c.PublishShipBlueprint=new PublishShipBlueprintPayload {BlueprintId=blueprint,Revision=revision,Draft=draft};return Strategic(c);
        }
    }
}
