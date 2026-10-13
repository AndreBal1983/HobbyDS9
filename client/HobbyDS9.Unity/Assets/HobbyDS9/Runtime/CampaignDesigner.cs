#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.UnityClient
{
    // Presentation lifetime and identity only. Application owns availability,
    // compilation, publication, history, payment and construction.
    public sealed class CampaignDesigner
    {
        private readonly StrategicClient client;
        private readonly long generation;
        private DesignerFit? lastPublished;
        public EntityId Actor { get; }
        public CampaignSnapshot Observed { get; private set; }
        public ShipDesignDraft? Draft { get; private set; }
        public string BlueprintId { get; private set; } = "";
        public int NextRevision { get; private set; } = 1;
        public int? EditingRevision { get; private set; }
        public string Notice { get; private set; } = "";
        public bool RefreshRequired { get; private set; }
        public bool SessionReplaced => generation!=client.SessionGeneration;
        public IReadOnlyList<CampaignBlueprintSnapshot> History { get; private set; } = Array.Empty<CampaignBlueprintSnapshot>();
        public bool CanPublish => !SessionReplaced && !RefreshRequired && Draft!=null && NextRevision>0
            && Observed.Campaign.Phase==SessionPhase.Planning && HumanControlled
            && (lastPublished==null || !Draft.Current.SameAs(lastPublished))
            && (Draft.Preview().Status==DesignerStatus.Valid || Draft.Preview().Status==DesignerStatus.Warning);
        private bool HumanControlled => Observed.Campaign.Civilizations.Any(c=>c.CivilizationId==Actor && c.ControllerKind==ControllerKind.Human);
        public CampaignDesigner(StrategicClient client,EntityId actor)
        {
            this.client=client; Actor=actor; generation=client.SessionGeneration; Observed=client.Snapshot;
            Refresh(); NewIdentity();
        }
        public void Refresh()
        {
            if(SessionReplaced) { Notice="Campaign replaced. Old draft retained for review only; discard it to start the loaded campaign workspace."; return; }
            Observed=client.Snapshot; RefreshRequired=false;
            History=client.Blueprints().Where(b=>b.OwnerCivilizationId==Actor).ToList().AsReadOnly();
            var source=client.ShipDesignCatalogue(Actor);
            if(source==null || source.Catalogue.Hulls.Count==0 || source.Catalogue.Roles.Count==0 || !HumanControlled)
            {
                Notice="Ship design catalogue unavailable for a human actor in this campaign.";
                Draft=null; return;
            }
            var designer=ShipDesignApplication.FromCampaignCatalogue(source.Catalogue);
            var hull=source.Catalogue.Hulls.FirstOrDefault(h=>source.AvailableVersionIds.Contains(h.VersionId)) ?? source.Catalogue.Hulls[0];
            var blank=new ShipClassDraftDto { ClassId="class.local",DisplayName="New class",HullVersionId=hull.VersionId,
                RoleId=source.Catalogue.Roles[0].RoleId,EmbarkedCrew=hull.MinimumCrew,
                Installations=new List<ShipInstallationDto>(),Profiles=Enum.GetValues(typeof(DesignerProfile)).Cast<DesignerProfile>()
                    .Select(p=>new ShipOperatingProfileDto { Kind=DesignerFit.Kind(p),Allocations=new List<ProfilePowerAllocationDto>() }).ToList() };
            var provider=new ApplicationShipDesignPreviewProvider(designer,source.AvailableVersionIds,
                new[]{new DesignerTemplate("blank","Empty campaign fit",new DesignerFit(blank))},
                "LIVE CAMPAIGN / Application catalogue • Mature Engineering access");
            if(Draft==null)Draft=new ShipDesignDraft(provider); else Draft.RefreshProvider(provider);
            if(EditingRevision.HasValue)
            {
                var latest=History.Where(b=>b.Blueprint.BlueprintId==BlueprintId).Select(b=>b.Blueprint.Revision).DefaultIfEmpty(0).Max();
                NextRevision=latest==int.MaxValue?0:latest+1;
            }
        }
        private void NewIdentity()
        {
            // Canonical host-generated identity, independent of campaign RNG and
            // entity sequence. Backend still checks global definition collisions.
            BlueprintId="blueprint.player."+Guid.NewGuid().ToString("N");
            _=new DefinitionId(BlueprintId);
            NextRevision=1; EditingRevision=null; lastPublished=null;
            if(Draft!=null)
            {
                var empty=Draft.Current.ToDto();empty.ClassId="class."+BlueprintId;
                Draft.StartFit(new DesignerFit(empty));
            }
        }
        public void NewClass()
        {
            if(SessionReplaced || Draft==null)return;
            Draft.RequestDiscard(()=> { Draft.StartFit(Draft.Provider.Templates[0].Fit); NewIdentity(); Notice="New local class; publication has no credit cost."; });
        }
        public void Edit(CampaignBlueprintSnapshot record)
        {
            if(SessionReplaced || Draft==null || !History.Contains(record))return;
            if(!CanEdit(record)) {Notice="This immutable fit uses separate stacks or quantities beyond this editor's supported range. It can still be reviewed and constructed.";return;}
            var fit=FromSnapshot(record.Blueprint.Fit); // detached immutable revision
            Draft.RequestDiscard(()=> {
                BlueprintId=record.Blueprint.BlueprintId; EditingRevision=record.Blueprint.Revision;
                var latest=History.Where(b=>b.Blueprint.BlueprintId==BlueprintId).Max(b=>b.Blueprint.Revision);
                NextRevision=latest==int.MaxValue?0:latest+1; lastPublished=fit;
                Draft.StartFit(fit); Notice="Editing a local copy of revision "+EditingRevision+"; stored history is immutable.";
            });
        }
        public bool CanEdit(CampaignBlueprintSnapshot record) => record.Blueprint.Fit.Installations.All(i=>i.Quantity<=int.MaxValue)
            && record.Blueprint.Fit.Installations.Select(i=>i.ComponentVersionId).Distinct(StringComparer.Ordinal).Count()==record.Blueprint.Fit.Installations.Count;
        public CommandResult Publish()
        {
            if(SessionReplaced || Draft==null || RefreshRequired)
                throw new InvalidOperationException("Review the current campaign workspace before publishing.");
            if(lastPublished!=null && Draft.Current.SameAs(lastPublished))
                throw new InvalidOperationException("This fit is already published; edit it before publishing another revision.");
            var fit=Draft.Current;
            var result=client.PublishBlueprint(Observed,Actor,BlueprintId,NextRevision,fit.ToDto());
            if(result.Accepted)
            {
                EditingRevision=NextRevision; lastPublished=fit; Draft.MarkPublished(); Refresh();
                Notice="Published "+fit.Name+" / revision "+EditingRevision+". Queue this exact revision in Production.";
            }
            else { RefreshRequired=true; Notice=CommandFeedback.Describe(result)+" Draft retained. Use Refresh campaign before retrying."; }
            return result;
        }
        public static DesignerFit FromSnapshot(ShipClassDraftSnapshot d) => new DesignerFit(new ShipClassDraftDto {
            ClassId=d.ClassId,DisplayName=d.DisplayName,HullVersionId=d.HullVersionId,RoleId=d.RoleId,EmbarkedCrew=d.EmbarkedCrew,
            Installations=d.Installations.Select(i=>new ShipInstallationDto { InstallationId=i.InstallationId,ComponentVersionId=i.ComponentVersionId,Quantity=i.Quantity }).ToList(),
            Profiles=d.Profiles.Select(p=>new ShipOperatingProfileDto { Kind=p.Kind,Allocations=p.Allocations.Select(a=>new ProfilePowerAllocationDto { InstallationId=a.InstallationId,RequestedPower=a.RequestedPower,AllocatedPower=a.AllocatedPower }).ToList() }).ToList()
        });
    }
}
