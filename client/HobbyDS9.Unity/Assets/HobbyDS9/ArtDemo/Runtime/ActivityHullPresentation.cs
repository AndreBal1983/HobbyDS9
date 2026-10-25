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
                case "hull.constitution-explorer.v1": return new ActivityHullVisualDefinition(exactHullVersionId,"activity.constitution-explorer.v1","Constitution-style Explorer · original hull art","ArtDemo/ActivityHulls/Constitution/Models/ConstitutionExplorer_LOD0",300);
                case "hull.galaxy-cruiser.v1": return new ActivityHullVisualDefinition(exactHullVersionId,"activity.galaxy-cruiser.v1","Galaxy Cruiser · classic hull art",null,620);
                case "hull.defiant-combat.v1": return new ActivityHullVisualDefinition(exactHullVersionId,"activity.defiant-combat.v1","Defiant Combat · owner-provided art, local review only","ArtDemo/ActivityHulls/Defiant/Models/Defiant_LOD0",170);
                default:return null;
            }
        }
    }

    public sealed partial class ShipPreviewStage
    {
        private string activityHullVersionId;
        public bool IsActivityHull => activityHullVersionId!=null;

        public void ShowActivityHull(string exactHullVersionId)
        {
            EnsureAlive();
            var definition=ActivityHullCatalog.Find(exactHullVersionId);
            if(definition!=null && activityHullVersionId==exactHullVersionId && Availability==ShipPreviewAvailability.Model)return;
            Clear();
            if(definition==null){Label="Hull schematic · no exact activity hull art binding";return;}
            var shader=Resources.Load<Shader>("ArtDemo/PreviewStudio");
            if(shader==null || !shader.isSupported){Label="Activity hull unavailable in this renderer";Availability=ShipPreviewAvailability.RenderingUnavailable;return;}
            var prefab=Resources.Load<GameObject>(definition.ResourcePath??(DesignerResourceRoot+"Models/GalaxyHull_LOD0"));
            if(prefab==null){Label=definition.Label+" · model resource missing, schematic only";return;}
            model=Child(definition.Label);
            model.transform.localScale=Vector3.one*(10f/definition.NominalArtLengthMetres);
            if(definition.ResourcePath==null)
            {
                classicNacelles=true;
                AddDesignerPart(DesignerReferenceView.Hull,prefab);
                AddDesignerNacelles();
                AddDesignerDeflector();
                AddDesignerPart(DesignerReferenceView.CoreCutaway,Resources.Load<GameObject>(DesignerResourceRoot+"Models/WarpCore_LOD0"),new Vector3(0,-56.286f,-64.395f));
                ApplyStudioMaterials();
            }
            else
            {
                var instance=InstantiateDesignerPart(prefab);
                if(!ApplyActivityMaterials(instance,definition,shader))
                { Clear();Label="Activity hull material contract missing · schematic only";return; }
            }
            activityHullVersionId=exactHullVersionId;
            designerReference=true;VisualKey=definition.VisualKey;Availability=ShipPreviewAvailability.Model;
            Label=definition.Label+" · fixed hull presentation; fitted equipment remains in facts";
            if(missingDesignerArt.Length>0)Label+=" · missing art: "+missingDesignerArt;
            FrameDesignerReference();Render();
        }

        private bool ApplyActivityMaterials(GameObject instance,ActivityHullVisualDefinition definition,Shader shader)
        {
            bool defiant=definition.HullVersionId=="hull.defiant-combat.v1";
            var roles=defiant?new[]{"Material_0"}:new[]{"CON_Hull_Pearl","CON_Panel_Silver","CON_Panel_Slate","CON_Phaser_Bronze","CON_Drive_Blue","CON_Recess_Graphite","CON_Deflector_Amber","CON_Collector_Red"};
            var colours=new[]{new Color(.52f,.59f,.65f),new Color(.68f,.72f,.75f),new Color(.25f,.33f,.4f),new Color(.28f,.2f,.11f),new Color(.025f,.27f,.8f),new Color(.025f,.035f,.05f),new Color(.75f,.3f,.06f),new Color(.75f,.045f,.025f)};
            var metals=new[]{.28f,.33f,.38f,.6f,.2f,.28f,.32f,.22f};
            var roughness=new[]{.4f,.4f,.43f,.38f,.35f,.45f,.4f,.35f};
            var materials=new Material[roles.Length];
            Texture2D colour=null,normal=null,mr=null;
            if(defiant)
            {
                const string path="ArtDemo/ActivityHulls/Defiant/Textures/Defiant_Image_";
                colour=Resources.Load<Texture2D>(path+"0");mr=Resources.Load<Texture2D>(path+"1");normal=Resources.Load<Texture2D>(path+"2");
                if(colour==null || mr==null || normal==null)return false;
            }
            foreach(var filter in instance.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh==null || filter.sharedMesh.subMeshCount!=roles.Length)return false;
            for(int i=0;i<roles.Length;i++)
            {
                var material=new Material(shader){name=roles[i]+" activity preview",hideFlags=HideFlags.DontSave};
                material.SetColor("_BaseColor",defiant?Color.white:colours[i]);
                material.SetFloat("_Metallic",defiant?1:metals[i]);
                material.SetFloat("_Smoothness",defiant?1:1-roughness[i]);
                if(!defiant && (i==4 || i==6 || i==7))material.SetColor("_EmissionColor",colours[i]*(i==4?1.6f:.8f));
                if(defiant)
                {
                    material.SetTexture("_BaseMap",colour);material.SetTexture("_BumpMap",normal);material.SetFloat("_UseNormalMap",1);
                    material.SetTexture("_MetallicRoughnessMap",mr);material.SetFloat("_UseMetallicRoughnessMap",1);
                }
                previewMaterials.Add(material);materials[i]=material;
            }
            // Received FBX submesh indices preserve source material-slot order.
            foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))renderer.sharedMaterials=materials;
            return true;
        }
    }
}
