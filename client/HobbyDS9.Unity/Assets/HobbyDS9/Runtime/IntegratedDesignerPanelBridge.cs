#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient
{
    // Cross-assembly callbacks carry only catalogue facts, input drafts and outcomes.
    public static class IntegratedDesignerPanelBridge
    {
        private sealed class State
        {
            public CampaignIntegratedDesignLibrarySnapshot Library=null!;
            public ShipDesignDraft? Draft; public ShipDesignerView? View;
            public Action<bool> Changed=null!; public Action Back=null!;
            public Func<ShipClassDraftDto,bool> CanPublish=null!,Publish=null!;
            public Func<CampaignIntegratedDesignLibrarySnapshot?> Refresh=null!;
            public Func<CampaignBlueprintSnapshot,ShipClassDraftDto> Edit=null!;
            public Action New=null!; public Func<string> Notice=null!;
            public Func<string> ClassId=null!;
            public Action? Update;
        }
        public static object Create(CampaignIntegratedDesignLibrarySnapshot library,Action<bool> changed,Action back,
            Func<ShipClassDraftDto,bool> canPublish,Func<ShipClassDraftDto,bool> publish,
            Func<CampaignIntegratedDesignLibrarySnapshot?> refresh,Func<CampaignBlueprintSnapshot,ShipClassDraftDto> edit,Action newClass,
            Func<string> notice,ShipClassDraftDto? starter,Func<string> classId)
        {
            var state=new State{Library=library,Changed=changed,Back=back,CanPublish=canPublish,Publish=publish,Refresh=refresh,Edit=edit,New=newClass,Notice=notice,ClassId=classId};
            var source=library.Catalogue;if(source==null||source.Catalogue.Hulls.Count==0||source.Catalogue.Roles.Count==0)return state;
            var hull=source.Catalogue.Hulls.FirstOrDefault(h=>source.AvailableVersionIds.Contains(h.VersionId));if(hull==null)return state;
            var blank=new ShipClassDraftDto{ClassId="class.local",DisplayName="New class",HullVersionId=hull.VersionId,RoleId=source.Catalogue.Roles[0].RoleId,
                EmbarkedCrew=hull.MinimumCrew,Installations=new List<ShipInstallationDto>(),Profiles=Enum.GetValues(typeof(DesignerProfile)).Cast<DesignerProfile>()
                    .Select(p=>new ShipOperatingProfileDto{Kind=DesignerFit.Kind(p),Allocations=new List<ProfilePowerAllocationDto>()}).ToList()};
            var templates=new List<DesignerTemplate>();if(starter!=null)templates.Add(new DesignerTemplate("starter","Validated utility starter",new DesignerFit(starter)));
            templates.Add(new DesignerTemplate("blank","Empty available hull (advanced)",new DesignerFit(blank)));
            foreach(var record in library.Published.Where(CanEdit))templates.Add(new DesignerTemplate("published."+record.Blueprint.BlueprintId+"."+record.Blueprint.Revision,
                record.Blueprint.Fit.DisplayName+" / revision "+record.Blueprint.Revision,CampaignDesigner.FromSnapshot(record.Blueprint.Fit)));
            var provider=new ApplicationShipDesignPreviewProvider(ShipDesignApplication.FromCampaignCatalogue(source.Catalogue),source.AvailableVersionIds,templates,
                "OWN CAMPAIGN CATALOGUE · immutable publication · custom construction unavailable");
            state.Draft=new ShipDesignDraft(provider);
            var first=state.Draft.Current.ToDto();first.ClassId=state.ClassId();state.Draft.StartFit(new DesignerFit(first));
            state.Draft.Changed+=()=>{if(state.Draft.Current.ToDto().ClassId!=state.ClassId()){var fit=state.Draft.Current.ToDto();fit.ClassId=state.ClassId();state.Draft.StartFit(new DesignerFit(fit));return;}state.Changed(state.Draft.Modified);};return state;
        }
        private static bool CanEdit(CampaignBlueprintSnapshot r)=>r.Blueprint.Fit.Installations.All(i=>i.Quantity<=int.MaxValue)
            && r.Blueprint.Fit.Installations.Select(i=>i.ComponentVersionId).Distinct(StringComparer.Ordinal).Count()==r.Blueprint.Fit.Installations.Count;
        public static void Present(VisualElement root,object context)
        {
            var s=(State)context;root.Clear();var scope=new Label("Design and publish immutable class revisions. Custom class construction is not supported in this campaign."){name="integrated-design-scope"};scope.style.whiteSpace=WhiteSpace.Normal;root.Add(scope);
            if(s.Draft==null){root.Add(new Label("No available ship-design hull for this actor."));return;}
            if(s.Update!=null)s.Draft.Changed-=s.Update;s.View?.Dispose();
            var actions=new VisualElement();actions.style.flexDirection=FlexDirection.Row;actions.style.flexShrink=0;root.Add(actions);
            var feedback=new Label(){name="integrated-design-feedback"};feedback.style.whiteSpace=WhiteSpace.Normal;root.Add(feedback);
            var publish=new Button(()=>{if(s.Publish(s.Draft.Current.ToDto())){s.Draft.MarkPublished();var library=s.Refresh();if(library!=null)s.Library=library;Present(root,s);}else s.Update?.Invoke();}){name="integrated-design-publish",text="Publish class revision"};actions.Add(publish);
            actions.Add(new Button(()=>s.Draft.RequestDiscard(()=>{s.New();s.Draft.StartFit(s.Draft.Provider.Templates[0].Fit);s.Update?.Invoke();})){name="integrated-design-new",text="New class"});
            actions.Add(new Button(()=>{var library=s.Refresh();if(library!=null){s.Library=library;
                var provider=new ApplicationShipDesignPreviewProvider(ShipDesignApplication.FromCampaignCatalogue(library.Catalogue!.Catalogue),library.Catalogue.AvailableVersionIds,s.Draft.Provider.Templates,"OWN CAMPAIGN CATALOGUE · custom construction unavailable");s.Draft.RefreshProvider(provider);Present(root,s);}else s.Update?.Invoke();}){name="integrated-design-refresh",text="Refresh campaign"});
            s.View=new ShipDesignerView(s.Draft,s.Back);s.View.style.flexGrow=1;s.View.style.minHeight=0;root.Add(s.View);
            var history=new Foldout{text="Your immutable revisions — edit a local copy",value=false,name="integrated-design-history"};root.Add(history);
            if(s.Library.Published.Count==0)history.Add(new Label("No published revisions. Refresh after publishing to review history."));
            foreach(var record in s.Library.Published){var r=record;var button=new Button(()=>s.Draft.RequestDiscard(()=>{s.Draft.StartFit(new DesignerFit(s.Edit(r)));s.Update?.Invoke();}))
                {name="integrated-design-edit-"+r.Blueprint.BlueprintId+"-"+r.Blueprint.Revision,text=r.Blueprint.Fit.DisplayName+" · revision "+r.Blueprint.Revision};button.SetEnabled(CanEdit(r));history.Add(button);}
            s.Update=()=>{publish.SetEnabled(s.CanPublish(s.Draft.Current.ToDto()));feedback.text=s.Notice();};s.Draft.Changed+=s.Update;s.Update();
        }
        public static void Reset(object context){var s=(State)context;if(s.Draft!=null)s.Draft.StartFit(s.Draft.Starting);}
    }
}
