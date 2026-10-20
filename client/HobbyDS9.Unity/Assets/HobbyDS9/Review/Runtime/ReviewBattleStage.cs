#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.Rendering;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    // Isolated manual-render stage. Inputs are lawful resolved projections only.
    // No campaign, RNG, input physics, command submission or outcome calculation.
    public sealed class ReviewBattleStage : IDisposable
    {
        private const int Layer=30;
        private readonly GameObject root;
        private readonly Camera camera;
        private readonly Dictionary<string,GameObject> ships=new Dictionary<string,GameObject>(StringComparer.Ordinal);
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private readonly List<GameObject> effects=new List<GameObject>();
        private readonly Dictionary<Tuple<Color,bool>,Material> materialCache=new Dictionary<Tuple<Color,bool>,Material>();
        private readonly Dictionary<string,Tuple<Vector3,Quaternion,Vector3,Quaternion>> poses=new Dictionary<string,Tuple<Vector3,Quaternion,Vector3,Quaternion>>(StringComparer.Ordinal);
        private Mesh? markerMesh;
        private GameObject? projectile;
        private Vector3 projectileStart,projectileEnd;
        private readonly Func<CampaignPlaybackBoundary,string,Tuple<string,string>?> identity;
        private CampaignPlaybackBoundary boundary;
        private CampaignRoundPlayback? round;
        private int nextEvent;
        private float clock,maneuverTime;
        private bool disposed,overview;
        public RenderTexture Texture { get; }
        public string Caption { get; private set; }="Awaiting orders";
        public bool Complete=>round==null||maneuverTime>=1&&nextEvent>=round.Events.Count&&clock>=.8f;
        public bool Paused { get; set; }
        public float Speed { get; set; }=1;
        public int GenericMarkers { get; private set; }
        private static readonly Color Space=new Color(.012f,.022f,.042f,1);
        public ReviewBattleStage(CampaignPlaybackBoundary boundary,CampaignRoundPlayback? round,
            Func<CampaignPlaybackBoundary,string,Tuple<string,string>?> identity)
        {
            this.boundary=boundary;this.round=round;this.identity=identity;
            root=new GameObject("Resolved battle presentation"){hideFlags=HideFlags.DontSave,layer=Layer};
            root.transform.position=new Vector3(0,-24000,0);
            camera=Child(root,"Battle camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<Layer;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Space;camera.orthographic=true;
            camera.nearClipPlane=.1f;camera.farClipPlane=400;camera.allowHDR=false;camera.allowMSAA=false;
            Texture=new RenderTexture(1280,600,24,RenderTextureFormat.ARGB32){name="Resolved battle viewport",hideFlags=HideFlags.DontSave,antiAliasing=1};
            Texture.Create();camera.targetTexture=Texture;root.SetActive(false);
            try
            {
            Populate(boundary);Pose(boundary);Frame();
            if(round!=null)
            {
                var before=ships.ToDictionary(p=>p.Key,p=>Tuple.Create(p.Value.transform.localPosition,p.Value.transform.localRotation),StringComparer.Ordinal);
                Pose(round.After);
                foreach(var pair in ships)
                {
                    poses[pair.Key]=Tuple.Create(before[pair.Key].Item1,before[pair.Key].Item2,pair.Value.transform.localPosition,pair.Value.transform.localRotation);
                    pair.Value.transform.localPosition=before[pair.Key].Item1;pair.Value.transform.localRotation=before[pair.Key].Item2;
                }
            }
            Caption=round==null?"Current authoritative boundary · awaiting orders":"Resolved round "+round.Round+" · playback";
            Render();
            }
            catch{Dispose();throw;}
        }
        private static GameObject Child(GameObject parent,string name)
        {var child=new GameObject(name){hideFlags=HideFlags.DontSave,layer=Layer};child.transform.SetParent(parent.transform,false);return child;}
        private Material Material(Color color,bool emissive=false)
        {
            var key=Tuple.Create(color,emissive);if(materialCache.TryGetValue(key,out var cached))return cached;
            var shader=Resources.Load<Shader>(emissive?"BattlePresentation/Effect":"ArtDemo/PreviewStudio");
            if(shader==null)throw new InvalidOperationException("Battle presentation shader unavailable.");
            var m=new Material(shader){hideFlags=HideFlags.DontSave};m.SetColor("_BaseColor",color);
            if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",emissive?color:Color.black);
            owned.Add(m);materialCache.Add(key,m);return m;
        }
        private static Color CardColor(string name)
        {
            if(name.Contains("Plate_Gold"))return new Color(.73f,.55f,.29f);
            if(name.Contains("Inset"))return new Color(.16f,.115f,.065f);
            if(name.Contains("RedBrown"))return new Color(.34f,.105f,.065f);
            if(name.Contains("Drive_Amber"))return new Color(1,.26f,.035f);
            if(name.Contains("Windows"))return new Color(.58f,.73f,.8f);
            return new Color(.52f,.34f,.16f);
        }
        private void Materials(GameObject model,bool cardassian)
        {
            var cache=new Dictionary<Material,Material>();
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var result=renderer.sharedMaterials.Select(original=>{
                    if(original!=null&&cache.TryGetValue(original,out var prior))return prior;
                    var color=cardassian?CardColor(original?.name??""):original!=null&&original.HasProperty("_BaseColor")?
                        original.GetColor("_BaseColor"):original!=null&&original.HasProperty("_Color")?original.color:new Color(.62f,.67f,.72f);
                    var clone=Material(color);
                    if(original!=null)cache[original]=clone;
                    return clone;
                }).ToArray();
                renderer.sharedMaterials=result;
            }
        }
        private GameObject Spawn(CampaignPlaybackBoundary source,CampaignTacticalCondition condition)
        {
            var group=Child(root,"Visible ship "+condition.ShipId);var pair=identity(source,condition.ShipId);
            GameObject? model=null;
            if(pair?.Item1=="civilization.federation"&&pair.Item2=="ship-design.source-cruiser")
            {
                var prefab=Resources.Load<GameObject>("ArtDemo/Federation/Prefabs/exploration-cruiser");
                if(prefab!=null){model=UnityEngine.Object.Instantiate(prefab,group.transform,false);model.transform.localScale=Vector3.one*(4f/620f);Materials(model,false);}
            }
            else if(pair?.Item1=="civilization.cardassian"&&pair.Item2=="ship-design.travel-target")
            {
                model=Child(group,"Cardassian prototype");model.transform.localScale=Vector3.one*(4f/480.95f);
                var lods=new List<LOD>();
                for(int n=0;n<3;n++)
                {
                    var asset=Resources.Load<GameObject>("BattlePresentation/Cardassian/cardassian-cruiser_LOD"+n);
                    if(asset==null)continue;
                    var part=UnityEngine.Object.Instantiate(asset,model.transform,false);Materials(part,true);
                    lods.Add(new LOD(n==0?.45f:n==1?.18f:.02f,part.GetComponentsInChildren<Renderer>(true)));
                }
                if(lods.Count>0)
                {
                    var lod=model.AddComponent<LODGroup>();lod.SetLODs(lods.ToArray());lod.RecalculateBounds();
                    var asset=Resources.Load<TextAsset>("BattlePresentation/Cardassian/manifest");
                    if(asset!=null)
                    {
                        var manifest=JObject.Parse(asset.text);
                        foreach(var anchor in manifest["anchors"]??new JArray())
                        {
                            var point=anchor["position"]!;
                            Child(model,(string)anchor["name"]!).transform.localPosition=new Vector3((float)point[0]!, (float)point[1]!, (float)point[2]!);
                        }
                    }
                }
                else{Destroy(model);model=null;}
            }
            if(model==null)
            {
                GenericMarkers++;
                model=Marker(group,"Model unavailable",new Color(.5f,.55f,.65f));model.transform.localScale=new Vector3(1,.3f,2);
            }
            foreach(var child in group.GetComponentsInChildren<Transform>(true))child.gameObject.layer=Layer;
            return group;
        }
        private GameObject Marker(GameObject parent,string name,Color color)
        {
            var marker=Child(parent,name);
            if(markerMesh==null)
            {
                markerMesh=new Mesh{name="Presentation marker",hideFlags=HideFlags.DontSave};
                markerMesh.vertices=new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back};
                markerMesh.triangles=new[]{0,4,3,0,3,5,0,5,2,0,2,4,1,3,4,1,5,3,1,2,5,1,4,2};
                markerMesh.RecalculateNormals();markerMesh.RecalculateBounds();owned.Add(markerMesh);
            }
            marker.AddComponent<MeshFilter>().sharedMesh=markerMesh;marker.AddComponent<MeshRenderer>().sharedMaterial=Material(color,true);return marker;
        }
        private void Populate(CampaignPlaybackBoundary value)
        {
            var lawful=value.OwnShips.Concat(value.VisibleEnemies).Where(c=>c.Hull>0).OrderBy(c=>c.ShipId,StringComparer.Ordinal).ToList();
            var ids=new HashSet<string>(lawful.Select(c=>c.ShipId),StringComparer.Ordinal);
            foreach(var gone in ships.Keys.Where(id=>!ids.Contains(id)).ToArray()){Destroy(ships[gone]);ships.Remove(gone);}
            foreach(var c in lawful)if(!ships.ContainsKey(c.ShipId))ships.Add(c.ShipId,Spawn(value,c));
        }
        private void Pose(CampaignPlaybackBoundary value)
        {
            // Deterministic display slots; abstract pairwise range is not physical distance.
            void Side(IEnumerable<CampaignTacticalCondition> conditions,bool own)
            {
                var fleets=conditions.Where(c=>c.Hull>0).GroupBy(c=>c.FleetId).OrderBy(g=>g.Key,StringComparer.Ordinal).ToList();
                for(int f=0;f<fleets.Count;f++)
                {
                    var members=fleets[f].OrderBy(c=>c.ShipId,StringComparer.Ordinal).ToList();
                    var relation=value.Relations.FirstOrDefault(r=>(own?r.OwnFleetId:r.VisibleEnemyFleetId)==fleets[f].Key);
                    float gap=relation?.Range==TacticalRange.Intermixed?3:relation?.Range==TacticalRange.Close?5:relation?.Range==TacticalRange.Engagement?8:12;
                    var facing=own?relation?.OwnFacing:relation?.EnemyFacing;
                    float angle=facing==TacticalSector.Port?90:facing==TacticalSector.Starboard?-90:facing==TacticalSector.Aft?180:0;
                    for(int i=0;i<members.Count;i++)if(ships.TryGetValue(members[i].ShipId,out var ship))
                    {
                        ship.transform.localPosition=new Vector3(own?-gap:gap,0,(f-(fleets.Count-1)*.5f)*12+(i-(members.Count-1)*.5f)*5);
                        ship.transform.localRotation=Quaternion.Euler(0,(own?90:-90)+angle,0);
                    }
                }
            }
            Side(value.OwnShips,true);Side(value.VisibleEnemies,false);
        }
        private void Frame()
        {
            var positions=ships.Values.Select(s=>s.transform.localPosition).ToList();
            var extent=positions.Count==0?15:Mathf.Max(15,positions.Max(p=>Mathf.Abs(p.z))+7);
            camera.aspect=(float)Texture.width/Texture.height;camera.orthographicSize=extent*.75f;
            camera.transform.localPosition=overview?new Vector3(0,80,-.1f):new Vector3(-22,32,-30);
            camera.transform.LookAt(root.transform.position,Vector3.up);
        }
        public void ToggleCamera(){overview=!overview;Frame();Render();}
        private Vector3? Position(string? id)=>id!=null&&ships.TryGetValue(id,out var model)?model.transform.localPosition:(Vector3?)null;
        private Vector3? Origin(string? id,TacticalWeaponClass? weapon)
        {
            if(id==null||!ships.TryGetValue(id,out var model))return null;
            var names=weapon==TacticalWeaponClass.Torpedo?new[]{"Effect_Torpedo","Effect_Ordnance_Fore"}:new[]{"Effect_Beam_Fore"};
            var anchor=model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>names.Contains(t.name));
            return anchor==null?model.transform.localPosition:root.transform.InverseTransformPoint(anchor.position);
        }
        private void ClearEffects(){foreach(var effect in effects)Destroy(effect);effects.Clear();projectile=null;}
        private void Beam(Vector3 start,Vector3 end,Color color,float width)
        {
            var effect=Child(root,"Recorded weapon effect");effects.Add(effect);var line=effect.AddComponent<LineRenderer>();
            line.useWorldSpace=false;line.positionCount=2;line.SetPositions(new[]{start,end});line.startWidth=line.endWidth=width;
            line.sharedMaterial=Material(color,true);
        }
        private void Pulse(Vector3 at,Color color,float scale)
        {var fx=Marker(root,"Recorded impact",color);fx.transform.localPosition=at;fx.transform.localScale=Vector3.one*scale;effects.Add(fx);}
        private void Projectile(Vector3 start,Vector3 end)
        {
            projectile=Marker(root,"Recorded ordnance",new Color(1,.45f,.1f));projectile.transform.localScale=Vector3.one*.2f;
            projectile.transform.localPosition=start;projectileStart=start;projectileEnd=end;effects.Add(projectile);
        }
        private void Event(CampaignPlaybackEvent e)
        {
            ClearEffects();var source=Origin(e.SourceShipId,e.WeaponClass);var target=Position(e.TargetShipId);
            Caption="Round "+e.Round+" · "+e.Kind+(e.Amount>0?" · "+e.Amount:"")+(e.Sector.HasValue?" · "+e.Sector:"");
            switch(e.Kind)
            {
                case CampaignPlaybackEventKind.Maneuver:
                    break; // Choreography already used actual before/after relations.
                case CampaignPlaybackEventKind.WeaponFired:
                    if(source.HasValue&&target.HasValue)
                    {if(e.WeaponClass==TacticalWeaponClass.Torpedo||e.WeaponClass==TacticalWeaponClass.Cannon)Projectile(source.Value,target.Value);
                        else Beam(source.Value,target.Value,new Color(1,.78f,.38f),.07f);}
                    else if(source.HasValue)Pulse(source.Value,new Color(1,.78f,.38f),.3f);
                    break;
                case CampaignPlaybackEventKind.IncomingTorpedo:
                    // The hidden source remains absent: the edge cue is not a guessed launcher.
                    if(!source.HasValue&&target.HasValue)Projectile(new Vector3(24,2,target.Value.z+5),target.Value);
                    break;
                case CampaignPlaybackEventKind.InterceptionAttempt:
                    if(source.HasValue)Pulse(source.Value,new Color(.8f,.7f,1),.45f);break;
                case CampaignPlaybackEventKind.TorpedoIntercepted:
                    if(target.HasValue)Pulse(target.Value+Vector3.up,new Color(1,.8f,.5f),.7f);break;
                case CampaignPlaybackEventKind.ShieldDamage:
                    if(target.HasValue)Pulse(target.Value,new Color(.35f,.65f,1),1.8f);break;
                case CampaignPlaybackEventKind.HullDamage:
                    if(target.HasValue)Pulse(target.Value,new Color(1,.3f,.06f),.65f);break;
                case CampaignPlaybackEventKind.ShipDestroyed:
                    if(target.HasValue)
                    {
                        Pulse(target.Value,new Color(1,.55f,.15f),2.3f);
                        if(e.TargetShipId!=null){Destroy(ships[e.TargetShipId]);ships.Remove(e.TargetShipId);}
                    }
                    break;
                case CampaignPlaybackEventKind.ShieldRecharged:
                    if(target.HasValue)Pulse(target.Value,new Color(.3f,.8f,1),.7f);break;
            }
        }
        public void Tick(float seconds)
        {
            if(disposed||Paused||round==null||Complete)return;
            var elapsed=Mathf.Min(seconds,.1f)*Speed;
            if(maneuverTime<1)
            {
                maneuverTime=Mathf.Min(1,maneuverTime+elapsed);
                foreach(var pair in poses)if(ships.TryGetValue(pair.Key,out var ship))
                {var p=pair.Value;ship.transform.localPosition=Vector3.Lerp(p.Item1,p.Item3,maneuverTime);ship.transform.localRotation=Quaternion.Slerp(p.Item2,p.Item4,maneuverTime);}
                Render();return;
            }
            clock+=elapsed;
            if(projectile!=null)projectile.transform.localPosition=Vector3.Lerp(projectileStart,projectileEnd,Mathf.Clamp01(clock/.24f));
            if(nextEvent<round.Events.Count&&clock>=.24f)
            {clock=0;Event(round.Events[nextEvent++]);}
            if(nextEvent>=round.Events.Count&&clock>=.8f)
            {
                ClearEffects();boundary=round.After;Populate(boundary);Pose(boundary);Frame();
                Caption="Round "+round.Round+" complete · authoritative result";
            }
            Render();
        }
        public void Skip()
        {
            if(disposed||round==null)return;
            nextEvent=round.Events.Count;maneuverTime=1;clock=.8f;ClearEffects();boundary=round.After;Populate(boundary);Pose(boundary);Frame();
            Caption="Round "+round.Round+" result · playback skipped";Render();
        }
        private void Render()
        {
            if(disposed)return;
            root.SetActive(true);
            try
            {
                var request=new RenderPipeline.StandardRequest{destination=Texture};
                if(GraphicsSettings.currentRenderPipeline==null)camera.Render();
                else if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);
                else Caption="3D rendering unavailable; authoritative reports remain available";
            }
            finally{root.SetActive(false);}
        }
        private static void Destroy(UnityEngine.Object? value)
        {
            if(value==null)return;
            if(value is GameObject game)game.SetActive(false);
            if(UnityEngine.Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;camera.targetTexture=null;root.SetActive(false);
            Destroy(root);foreach(var item in owned)Destroy(item);owned.Clear();Texture.Release();Destroy(Texture);
        }
    }
}
