#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts.ShipDesign;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient
{
    // Reuses the existing editor with a scoped catalogue. No campaign factory or v2 conversion.
    public static class IntegratedDesignerPanelBridge
    {
        private sealed class State
        {
            public CampaignIntegratedDesignLibrarySnapshot Library=null!;
            public ShipDesignDraft? Draft;
            public ShipDesignerView? View;
            public Action<bool> Changed=null!;
            public Action Back=null!;
            public Action? PreviewCaption;
        }
        public static object Create(CampaignIntegratedDesignLibrarySnapshot library,Action<bool> changed,Action back)
        {
            var state=new State{Library=library,Changed=changed,Back=back};var source=library.Catalogue;
            if(source==null||source.Catalogue.Hulls.Count==0||source.Catalogue.Roles.Count==0)return state;
            var hull=source.Catalogue.Hulls.FirstOrDefault(h=>source.AvailableVersionIds.Contains(h.VersionId));
            if(hull==null)return state;
            var blank=new ShipClassDraftDto{ClassId="class.local",DisplayName="New preview class",HullVersionId=hull.VersionId,
                RoleId=source.Catalogue.Roles[0].RoleId,EmbarkedCrew=hull.MinimumCrew,Installations=new List<ShipInstallationDto>(),
                Profiles=Enum.GetValues(typeof(DesignerProfile)).Cast<DesignerProfile>().Select(p=>new ShipOperatingProfileDto{Kind=DesignerFit.Kind(p),Allocations=new List<ProfilePowerAllocationDto>()}).ToList()};
            var provider=new ApplicationShipDesignPreviewProvider(ShipDesignApplication.FromCampaignCatalogue(source.Catalogue),source.AvailableVersionIds,
                new[]{new DesignerTemplate("blank","Empty available hull",new DesignerFit(blank))},"OWN CAMPAIGN CATALOGUE · v1 preview · custom v2 construction unavailable");
            state.Draft=new ShipDesignDraft(provider);state.Draft.Changed+=()=>state.Changed(state.Draft.Modified);
            return state;
        }
        public static void Present(VisualElement root,object context)
        {
            var state=(State)context;root.Clear();
            var note=new Label("Local v1 design preview. Start with Hull, then add required Power, Crew, Command and Propulsion systems; Cost / Validation explains remaining requirements. Publication from this page is pending; custom v2 construction is unsupported."){name="integrated-design-scope"};note.style.whiteSpace=WhiteSpace.Normal;root.Add(note);
            if(state.Draft==null){root.Add(new Label("No available ship-design hull for this actor."));return;}
            if(state.PreviewCaption!=null)state.Draft.Changed-=state.PreviewCaption;
            state.View?.Dispose();state.View=new ShipDesignerView(state.Draft,state.Back);state.View.style.flexGrow=1;state.View.style.minHeight=0;
            root.Add(state.View);
            state.PreviewCaption=()=>{if(state.Draft.Current.Installed.Count==0){var caption=state.View.Q<Label>("designer-state");caption.text="Incomplete local preview";caption.tooltip="This empty fit has actual backend validation errors. Add required systems and inspect Cost / Validation; it is not publishable.";}};
            state.Draft.Changed+=state.PreviewCaption;state.PreviewCaption();
            var history=new Foldout{text="Your published revisions (read only)",value=false};root.Add(history);
            if(state.Library.Published.Count==0)history.Add(new Label("No published revisions."));
            foreach(var record in state.Library.Published)history.Add(new Label(record.Blueprint.Fit.DisplayName+" · revision "+record.Blueprint.Revision));
        }
        public static void Reset(object context)
        {
            var state=(State)context;if(state.Draft==null)return;
            state.Draft.StartFit(state.Draft.Starting);
        }
    }
}
