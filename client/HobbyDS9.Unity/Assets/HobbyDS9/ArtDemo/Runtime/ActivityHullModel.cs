using System;
using System.Collections.Generic;
using UnityEngine;

namespace HobbyDS9.ArtDemo
{
    // Bare owned model only. Caller supplies disclosed identity and owns lifetime;
    // no scene, camera, render texture, input, statistics or gameplay authority.
    public sealed class ActivityHullModel : IDisposable
    {
        private readonly long shutdownLabel=ArtShutdownLifecycle.NextId();
        private readonly List<Material> materialsOwned=new List<Material>();
        public GameObject Root { get; }
        public ActivityHullVisualDefinition Definition { get; }
        public string MissingArt { get; private set; }="";
        private bool disposed;
        private ActivityHullModel(Transform parent,ActivityHullVisualDefinition definition)
        {Definition=definition;Root=new GameObject(definition.Label){hideFlags=HideFlags.DontSave,layer=parent.gameObject.layer};Root.transform.SetParent(parent,false);}
        public static ActivityHullModel Create(Transform parent,string exactHullVersionId)
            =>Create(parent,exactHullVersionId,out _);
        public static ActivityHullModel Create(Transform parent,string exactHullVersionId,out ShipPreviewAvailability availability)
        {
            availability=ShipPreviewAvailability.Empty;
            if(parent==null)throw new ArgumentNullException(nameof(parent));
            ActivityHullModel value=null;
            try
            {
                var definition=ActivityHullCatalog.Find(exactHullVersionId);if(definition==null)return null;
                var shader=Resources.Load<Shader>("ArtDemo/PreviewStudio");if(shader==null||!shader.isSupported){availability=ShipPreviewAvailability.RenderingUnavailable;return null;}
                var prefab=Resources.Load<GameObject>(definition.ResourcePath??(ShipPreviewStage.DesignerResourceRoot+"Models/GalaxyHull_LOD0"));if(prefab==null)return null;
                value=new ActivityHullModel(parent,definition);
                if(definition.ResourcePath!=null)
                {if(!value.ApplyActivityMaterials(value.Socket(prefab),definition,shader)){value.Dispose();return null;}}
                else
                {
                    value.Socket(prefab);
                    foreach(var side in new[]{"Port","Starboard"})
                    {
                        var part=Resources.Load<GameObject>(ShipPreviewStage.DesignerResourceRoot+"Models/GalaxyNacelle_"+side+"_LOD1");
                        value.Part(part,new Vector3(side=="Port"?-125.6418f:125.4987f,-49.131f,-214.65f),side+" nacelle");
                    }
                    value.Part(Resources.Load<GameObject>(ShipPreviewStage.DesignerResourceRoot+"Models/Deflector_Blue_concentric_LOD0"),new Vector3(0,-64.395f,75.366f),"Deflector");
                    value.Part(Resources.Load<GameObject>(ShipPreviewStage.DesignerResourceRoot+"Models/WarpCore_LOD0"),new Vector3(0,-56.286f,-64.395f),"CoreCutaway");
                    value.StudioMaterials(shader);
                }
                availability=ShipPreviewAvailability.Model;return value;
            }
            catch(UnityException){value?.Dispose();return null;}
            catch(ArgumentException){value?.Dispose();return null;}
            catch{value?.Dispose();throw;}
        }
        private void Part(GameObject prefab,Vector3 position,string name)
        {if(prefab==null){MissingArt+=(MissingArt.Length==0?"":", ")+name;return;}Socket(prefab).transform.localPosition=position;}
        private GameObject Socket(GameObject prefab)
        {
            // Preserve imported FBX TRS; only the new socket is identity.
            var socket=new GameObject(prefab.name+" socket"){hideFlags=HideFlags.DontSave,layer=Root.layer};socket.transform.SetParent(Root.transform,false);
            var imported=UnityEngine.Object.Instantiate(prefab,socket.transform,false);
            foreach(var part in imported.GetComponentsInChildren<Transform>(true))part.gameObject.layer=Root.layer;
            return socket;
        }
        private void StudioMaterials(Shader shader)
        {
            var cache=new Dictionary<Material,Material>();
            foreach(var renderer in Root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];
                    if(!cache.TryGetValue(source,out var target))
                    {
                        target=new Material(shader){name=source.name+" preview",hideFlags=HideFlags.DontSave};materialsOwned.Add(target);
                        target.SetColor("_BaseColor",source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);
                        target.SetColor("_EmissionColor",source.HasProperty("_EmissionColor")?source.GetColor("_EmissionColor"):Color.black);
                        target.SetFloat("_Metallic",source.HasProperty("_Metallic")?source.GetFloat("_Metallic"):.2f);
                        target.SetFloat("_Smoothness",source.HasProperty("_Smoothness")?source.GetFloat("_Smoothness"):.35f);
                        CopyTexture(source,target,source.HasProperty("_BaseMap")?"_BaseMap":"_MainTex","_BaseMap");CopyTexture(source,target,"_BumpMap","_BumpMap");CopyTexture(source,target,"_EmissionMap","_EmissionMap");
                        target.SetFloat("_BumpScale",source.HasProperty("_BumpScale")?source.GetFloat("_BumpScale"):1);target.SetFloat("_UseNormalMap",source.HasProperty("_BumpMap")&&source.GetTexture("_BumpMap")!=null?1:0);
                        cache.Add(source,target);
                    }
                    materials[i]=target;
                }
                renderer.sharedMaterials=materials;
            }
        }
        private static void CopyTexture(Material source,Material target,string from,string to)
        {if(!source.HasProperty(from)||source.GetTexture(from)==null)return;target.SetTexture(to,source.GetTexture(from));target.SetTextureScale(to,source.GetTextureScale(from));target.SetTextureOffset(to,source.GetTextureOffset(from));}
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
                var material=new Material(shader){name=roles[i]+" activity preview",hideFlags=HideFlags.DontSave};materialsOwned.Add(material);
                material.SetColor("_BaseColor",defiant?Color.white:colours[i]);
                material.SetFloat("_Metallic",defiant?1:metals[i]);
                material.SetFloat("_Smoothness",defiant?1:1-roughness[i]);
                if(!defiant && (i==4 || i==6 || i==7))material.SetColor("_EmissionColor",colours[i]*(i==4?1.6f:.8f));
                if(defiant)
                {
                    material.SetTexture("_BaseMap",colour);material.SetTexture("_BumpMap",normal);material.SetFloat("_UseNormalMap",1);
                    material.SetTexture("_MetallicRoughnessMap",mr);material.SetFloat("_UseMetallicRoughnessMap",1);
                }
                materials[i]=material;
            }
            // Received FBX submesh indices preserve source material-slot order.
            foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))renderer.sharedMaterials=materials;
            return true;
        }

        private static void Destroy(UnityEngine.Object value)
        {if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        public void Dispose()
        {ArtShutdownLifecycle.Mark("ActivityHullModel",shutdownLabel,"dispose.enter");if(disposed){ArtShutdownLifecycle.Mark("ActivityHullModel",shutdownLabel,"dispose.already-return");return;}disposed=true;if(Root!=null){Root.SetActive(false);Destroy(Root);}foreach(var material in materialsOwned)Destroy(material);materialsOwned.Clear();ArtShutdownLifecycle.Mark("ActivityHullModel",shutdownLabel,"dispose.return");}
    }
}
