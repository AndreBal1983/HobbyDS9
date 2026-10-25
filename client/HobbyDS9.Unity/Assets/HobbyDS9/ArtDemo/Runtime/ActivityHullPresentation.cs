using System;
using UnityEngine;

namespace HobbyDS9.ArtDemo
{
    public sealed class ActivityHullVisualDefinition
    {
        public string HullVersionId { get; }
        public string VisualKey { get; }
        public string Label { get; }
        public string ResourcePath { get; }
        public float NominalArtLengthMetres { get; }
        internal ActivityHullVisualDefinition(string hull,string key,string label,string path,float length)
        { HullVersionId=hull;VisualKey=key;Label=label;ResourcePath=path;NominalArtLengthMetres=length; }
    }

    // Caller supplies already disclosed hull identity. No hidden lookup or
    // capability, equipment, owner, role or substring inference is performed.
    public static class ActivityHullCatalog
    {
        public static ActivityHullVisualDefinition Find(string exactHullVersionId)
        {
            switch(exactHullVersionId)
            {
                case "hull.constitution-explorer.v1": return new ActivityHullVisualDefinition(exactHullVersionId,"activity.constitution-explorer.v1","Constitution-style Explorer Â· original hull art","ArtDemo/ActivityHulls/Constitution/Models/ConstitutionExplorer_LOD0",300);
                case "hull.galaxy-cruiser.v1": return new ActivityHullVisualDefinition(exactHullVersionId,"activity.galaxy-cruiser.v1","Galaxy Cruiser Â· classic hull art",null,620);
                case "hull.defiant-combat.v1": return new ActivityHullVisualDefinition(exactHullVersionId,"activity.defiant-combat.v1","Defiant Combat Â· owner-provided art, local review only","ArtDemo/ActivityHulls/Defiant/Models/Defiant_LOD0",170);
                default:return null;
            }
        }
    }

    public sealed partial class ShipPreviewStage
    {
        private string activityHullVersionId;
        private ActivityHullModel activityModel;
        public bool IsActivityHull => activityHullVersionId!=null;

        public void ShowActivityHull(string exactHullVersionId)
        {
            EnsureAlive();
            var definition=ActivityHullCatalog.Find(exactHullVersionId);
            if(definition!=null && activityHullVersionId==exactHullVersionId && Availability==ShipPreviewAvailability.Model)return;
            Clear();
            if(definition==null){Label="Hull schematic Â· no exact activity hull art binding";return;}
            activityModel=ActivityHullModel.Create(root.transform,exactHullVersionId);
            if(activityModel==null){Label=definition.Label+" Â· model or material unavailable, schematic only";return;}
            model=activityModel.Root;model.transform.localScale=Vector3.one*(10f/definition.NominalArtLengthMetres);
            missingDesignerArt=activityModel.MissingArt;
            activityHullVersionId=exactHullVersionId;
            designerReference=true;VisualKey=definition.VisualKey;Availability=ShipPreviewAvailability.Model;
            Label=definition.Label+" Â· fixed hull presentation; fitted equipment remains in facts";
            if(missingDesignerArt.Length>0)Label+=" Â· missing art: "+missingDesignerArt;
            FrameDesignerReference();Render();
        }

    }
}
