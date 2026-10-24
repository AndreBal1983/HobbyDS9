using System;
using System.Collections.Generic;
using UnityEngine;

namespace HobbyDS9.ArtDemo
{
    public enum DesignerReferenceView { Hull, Nacelles, Deflector, CoreCutaway }
    public enum DesignerStudyVariant { SovereignNacelles, ClassicNacelles, BlueDeflector, AmberDeflector, SurveyDeflector }
    public sealed class DesignerArtPartSnapshot
    {
        public string Name;
        public float[] SocketPosition,ImportedRootPosition,ImportedRootScale,ImportedRootRotation;
    }
    public sealed class DesignerArtSnapshot
    {
        public string View;
        public float[] AssembledBoundsSize;
        public float CameraOrthographicSize,CameraNear,CameraFar,VisibleMinDepth,VisibleMaxDepth;
        public List<DesignerArtPartSnapshot> Parts;
    }

    public sealed partial class ShipPreviewStage
    {
        public const string DesignerGalaxyKey="reference.galaxy.precision.v3";
        public const string DesignerResourceRoot="ArtDemo/Designer/Galaxy/";
        private readonly Dictionary<DesignerReferenceView,GameObject> designerParts=new Dictionary<DesignerReferenceView,GameObject>();
        private readonly Dictionary<string,int> designerCounts=new Dictionary<string,int>(StringComparer.Ordinal);
        private GameObject designerCutawayHull;
        private bool designerReference;
        private DesignerReferenceView designerView;
        private float referenceYaw=37.57f,referencePitch=26f,referenceZoom=1;
        private string missingDesignerArt="";
        private bool classicNacelles;
        private DesignerStudyVariant deflectorVariant=DesignerStudyVariant.BlueDeflector;
        private DesignerArtSnapshot lastArtSnapshot;
        public bool IsDesignerReference => designerReference;
        public DesignerReferenceView ReferenceView => designerView;

        public static string DesignerReferenceKey(string hullVersionId) =>
            string.Equals(hullVersionId,"hull.utility.v1",StringComparison.Ordinal)?DesignerGalaxyKey:null;

        public static int DesignerModuleQuantity(IReadOnlyDictionary<string,int> counts,DesignerReferenceView view)
        {
            var id=view==DesignerReferenceView.Nacelles?"component.propulsion.v1":view==DesignerReferenceView.CoreCutaway?"component.reactor.v1":null;
            return id!=null && counts!=null && counts.TryGetValue(id,out var count) && count>0?count:0;
        }

        public void ShowDesignerReference(string hullVersionId,IReadOnlyDictionary<string,int> componentQuantities)
        {
            EnsureAlive();
            var key=DesignerReferenceKey(hullVersionId);
            var same=designerReference && key==VisualKey;
            if(!same)
            {
                Clear();
                if(key==null){Label="Hull schematic · no mapped Galaxy reference";return;}
                var shader=Resources.Load<Shader>("ArtDemo/PreviewStudio");
                if(shader==null || !shader.isSupported){Label="Galaxy reference unavailable in this renderer";Availability=ShipPreviewAvailability.RenderingUnavailable;return;}
                var hull=Resources.Load<GameObject>(DesignerResourceRoot+"Models/GalaxyHull_LOD0");
                if(hull==null){Label="Galaxy fitting art unavailable · schematic only";return;}
                model=Child("Illustrative Galaxy fitting reference");
                model.transform.localScale=Vector3.one*(10f/620f);
                AddDesignerPart(DesignerReferenceView.Hull,hull);
                AddDesignerNacelles();
                AddDesignerDeflector();
                AddDesignerPart(DesignerReferenceView.CoreCutaway,Resources.Load<GameObject>(DesignerResourceRoot+"Models/WarpCore_LOD0"),new Vector3(0,-56.286f,-64.395f));
                var cutaway=Resources.Load<GameObject>(DesignerResourceRoot+"Models/GalaxyHullCutaway_LOD1");
                if(cutaway!=null)designerCutawayHull=InstantiateDesignerPart(cutaway);
                else missingDesignerArt+=(missingDesignerArt.Length==0?"":", ")+"cutaway hull";
                ApplyStudioMaterials();designerReference=true;VisualKey=key;Availability=ShipPreviewAvailability.Model;
            }
            bool changed=!same || designerCounts.Count!=(componentQuantities?.Count??0);
            if(componentQuantities!=null)
            {
                if(componentQuantities.Count>256)throw new ArgumentOutOfRangeException(nameof(componentQuantities));
                foreach(var pair in componentQuantities)
                {
                    if(pair.Key==null || pair.Value<0)throw new ArgumentException("Invalid illustrative fit quantity.");
                    if(!designerCounts.TryGetValue(pair.Key,out var before) || before!=pair.Value)changed=true;
                }
            }
            designerCounts.Clear();if(componentQuantities!=null)foreach(var pair in componentQuantities)designerCounts.Add(pair.Key,pair.Value);
            UpdateDesignerVisibility();
            if(changed){FrameDesignerReference();Render();}
        }

        private void AddDesignerPart(DesignerReferenceView kind,GameObject prefab,Vector3 position=default(Vector3))
        {
            if(prefab==null){missingDesignerArt+=(missingDesignerArt.Length==0?"":", ")+kind;return;}
            var instance=InstantiateDesignerPart(prefab);
            instance.transform.localPosition=position;
            designerParts.Add(kind,instance);
        }

        private void AddDesignerNacelles()
        {
            var pair=new GameObject("Sovereign-style nacelles · illustrative reference"){hideFlags=HideFlags.DontSave,layer=PreviewLayer};
            pair.transform.SetParent(model.transform,false);
            foreach(var side in new[]{"Port","Starboard"})
            {
                var prefab=Resources.Load<GameObject>(DesignerResourceRoot+"Models/"+(classicNacelles?"GalaxyNacelle_":"SovereignNacelle_")+side+(classicNacelles?"_LOD1":"_LOD0"));
                if(prefab==null){missingDesignerArt+=(missingDesignerArt.Length==0?"":", ")+side+" nacelle";continue;}
                var instance=InstantiateDesignerPart(prefab);instance.transform.SetParent(pair.transform,false);
                instance.transform.localPosition=new Vector3(side=="Port"?-125.6418f:125.4987f,-49.131f,-214.65f);
            }
            designerParts.Add(DesignerReferenceView.Nacelles,pair);
        }

        private void AddDesignerDeflector()
        {
            var file=deflectorVariant==DesignerStudyVariant.AmberDeflector?"Deflector_Amber_oval_LOD1":deflectorVariant==DesignerStudyVariant.SurveyDeflector?"Deflector_Segmented_survey_LOD1":"Deflector_Blue_concentric_LOD0";
            AddDesignerPart(DesignerReferenceView.Deflector,Resources.Load<GameObject>(DesignerResourceRoot+"Models/"+file),new Vector3(0,-64.395f,75.366f));
        }

        public void SetStudyVariant(DesignerStudyVariant variant)
        {
            EnsureAlive();if(!Enum.IsDefined(typeof(DesignerStudyVariant),variant))throw new ArgumentOutOfRangeException(nameof(variant));
            if(!designerReference)return;
            bool nacelles=variant==DesignerStudyVariant.ClassicNacelles || variant==DesignerStudyVariant.SovereignNacelles;
            if(nacelles && classicNacelles==(variant==DesignerStudyVariant.ClassicNacelles))return;
            if(!nacelles && deflectorVariant==variant)return;
            var kind=nacelles?DesignerReferenceView.Nacelles:DesignerReferenceView.Deflector;
            if(designerParts.TryGetValue(kind,out var previous)){previous.SetActive(false);Destroy(previous);designerParts.Remove(kind);}
            if(nacelles){classicNacelles=variant==DesignerStudyVariant.ClassicNacelles;AddDesignerNacelles();}
            else{deflectorVariant=variant;AddDesignerDeflector();}
            if(designerParts.TryGetValue(kind,out var replacement))ApplyStudioMaterials(replacement);
            UpdateDesignerVisibility();FrameDesignerReference();Render();
        }

        private GameObject InstantiateDesignerPart(GameObject prefab)
        {
            // Identity applies to the SOCKET, not the imported FBX root. Unity's
            // root TRS may carry mandatory file-unit and axis conversion.
            var socket=new GameObject(prefab.name+" socket"){hideFlags=HideFlags.DontSave,layer=PreviewLayer};
            socket.transform.SetParent(model.transform,false);
            var imported=UnityEngine.Object.Instantiate(prefab,socket.transform,false);
            foreach(var part in imported.GetComponentsInChildren<Transform>(true))part.gameObject.layer=PreviewLayer;
            return socket;
        }

        private void UpdateDesignerVisibility()
        {
            foreach(var pair in designerParts)
            {
                bool visible=designerView==DesignerReferenceView.Hull
                    ? pair.Key==DesignerReferenceView.Hull || pair.Key==DesignerReferenceView.Deflector || DesignerModuleQuantity(designerCounts,pair.Key)>0
                    : pair.Key==designerView;
                pair.Value.SetActive(visible);
            }
            if(designerCutawayHull!=null)designerCutawayHull.SetActive(designerView==DesignerReferenceView.CoreCutaway);
            var propulsion=DesignerModuleQuantity(designerCounts,DesignerReferenceView.Nacelles);
            var reactor=DesignerModuleQuantity(designerCounts,DesignerReferenceView.CoreCutaway);
            int unmapped=0;foreach(var pair in designerCounts)
                if(pair.Value>0 && pair.Key!="component.propulsion.v1" && pair.Key!="component.reactor.v1")unmapped++;
            Label="Illustrative Galaxy reference · "+propulsion+" propulsion / "+reactor+" reactor packages; one representative per type; deflector is unbound art";
            if(unmapped>0)Label+=" · "+unmapped+" other package types use schematic facts";
            if(designerView==DesignerReferenceView.Deflector)Label="Uninstalled deflector art study · no catalogue deflector binding";
            if(designerView==DesignerReferenceView.Nacelles)Label="Nacelle art study only · "+(classicNacelles?"Classic":"Sovereign-style")+" · installed propulsion packages "+propulsion;
            if(designerView==DesignerReferenceView.CoreCutaway)Label=(reactor==0?"Uninstalled internal core art study":"Internal core art study")+" · cutaway hull · installed reactors "+reactor;
            if(missingDesignerArt.Length>0)Label+=" · missing art: "+missingDesignerArt;
        }

        public void SetReferenceView(DesignerReferenceView view)
        {
            EnsureAlive();if(!Enum.IsDefined(typeof(DesignerReferenceView),view))throw new ArgumentOutOfRangeException(nameof(view));
            if(!designerReference || designerView==view)return;
            designerView=view;UpdateDesignerVisibility();FrameDesignerReference();Render();
        }

        public void OrbitReference(float yawDegrees,float pitchDegrees)
        {
            EnsureAlive();
            if(float.IsNaN(yawDegrees)||float.IsInfinity(yawDegrees)||float.IsNaN(pitchDegrees)||float.IsInfinity(pitchDegrees))throw new ArgumentOutOfRangeException(nameof(yawDegrees));
            if(!designerReference)return;
            var yaw=Mathf.Repeat(yawDegrees,360);var pitch=Mathf.Clamp(pitchDegrees,-75,75);
            if(referenceYaw==yaw && referencePitch==pitch)return;
            referenceYaw=yaw;referencePitch=pitch;FrameDesignerReference();Render();
        }

        public void ZoomReference(float zoom)
        {
            EnsureAlive();if(float.IsNaN(zoom)||float.IsInfinity(zoom))throw new ArgumentOutOfRangeException(nameof(zoom));
            if(!designerReference)return;zoom=Mathf.Clamp(zoom,.6f,2.2f);
            if(referenceZoom==zoom)return;referenceZoom=zoom;FrameDesignerReference();Render();
        }

        private bool ReferencePartVisible(Transform value)
        {
            for(var part=value;part!=null && part!=root.transform;part=part.parent)if(!part.gameObject.activeSelf)return false;
            return true;
        }

        private void FrameDesignerReference()
        {
            var points=new List<Vector3>();
            var filters=model.GetComponentsInChildren<MeshFilter>(true);
            if(filters.Length>512)throw new InvalidOperationException("Designer reference mesh budget exceeded.");
            foreach(var filter in filters)
            {
                if(filter.sharedMesh==null || !ReferencePartVisible(filter.transform))continue;
                var b=filter.sharedMesh.bounds;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    points.Add(filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
            }
            if(points.Count==0){ClearTexture();Label+=" · selected reference art unavailable";return;}
            var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
            var centre=bounds.center;var yaw=referenceYaw*Mathf.Deg2Rad;var pitch=referencePitch*Mathf.Deg2Rad;
            var distance=Mathf.Max(20,bounds.extents.magnitude*2+1);
            camera.transform.position=centre+new Vector3(Mathf.Sin(yaw)*Mathf.Cos(pitch),Mathf.Sin(pitch),Mathf.Cos(yaw)*Mathf.Cos(pitch))*distance;
            camera.nearClipPlane=Mathf.Max(.01f,distance-bounds.extents.magnitude*1.5f);
            camera.farClipPlane=distance+bounds.extents.magnitude*1.5f+1;
            camera.transform.LookAt(centre,Vector3.up);camera.aspect=(float)Texture.width/Texture.height;
            float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
            foreach(var point in points)
            {
                var delta=point-centre;var x=Vector3.Dot(delta,camera.transform.right);var y=Vector3.Dot(delta,camera.transform.up);
                minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
            }
            camera.orthographicSize=ReferenceFrameSize(new Vector2((maxX-minX)*.5f,(maxY-minY)*.5f),camera.aspect)/referenceZoom;
            camera.transform.position+=camera.transform.right*((minX+maxX)*.5f)+camera.transform.up*((minY+maxY)*.5f);
            float minDepth=float.PositiveInfinity,maxDepth=float.NegativeInfinity;
            foreach(var point in points){var depth=Vector3.Dot(point-camera.transform.position,camera.transform.forward);minDepth=Mathf.Min(minDepth,depth);maxDepth=Mathf.Max(maxDepth,depth);}
            var parts=new List<DesignerArtPartSnapshot>();
            foreach(var transform in model.GetComponentsInChildren<Transform>(true))
            {
                if(!transform.name.EndsWith(" socket",StringComparison.Ordinal) || transform.childCount==0)continue;
                var imported=transform.GetChild(0);
                parts.Add(new DesignerArtPartSnapshot{Name=transform.name,SocketPosition=ArtVector(transform.localPosition),ImportedRootPosition=ArtVector(imported.localPosition),ImportedRootScale=ArtVector(imported.localScale),ImportedRootRotation=ArtVector(imported.localEulerAngles)});
            }
            lastArtSnapshot=new DesignerArtSnapshot{View=designerView.ToString(),AssembledBoundsSize=ArtVector(bounds.size),CameraOrthographicSize=camera.orthographicSize,CameraNear=camera.nearClipPlane,CameraFar=camera.farClipPlane,VisibleMinDepth=minDepth,VisibleMaxDepth=maxDepth,Parts=parts};
        }

        private static float[] ArtVector(Vector3 value)=>new[]{value.x,value.y,value.z};
        public DesignerArtSnapshot CaptureArtDiagnostics()=>lastArtSnapshot;

        private void ResetDesignerReference()
        {
            designerParts.Clear();designerCounts.Clear();designerCutawayHull=null;designerReference=false;designerView=DesignerReferenceView.Hull;
            missingDesignerArt="";referenceYaw=37.57f;referencePitch=26f;referenceZoom=1;
            classicNacelles=false;deflectorVariant=DesignerStudyVariant.BlueDeflector;
            lastArtSnapshot=null;
            // A later lawful Show(selection) on the same stage keeps its
            // original clipping contract after leaving a reference view.
            camera.nearClipPlane=.1f;camera.farClipPlane=80;
        }
    }
}
