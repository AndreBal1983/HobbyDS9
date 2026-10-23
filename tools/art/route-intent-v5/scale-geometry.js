/* Presentation extraction only: exact supplied edges, no generator or pathfinder. */
(()=>{'use strict';
const pointKey=p=>`${p.X},${p.Y}`,edgeKey=(a,b)=>[pointKey(a),pointKey(b)].sort().join('|'),pairKey=(a,b)=>[a,b].sort().join('|');
function index(raw){
 const d=raw.Layout,nodes=new Map(d.Regions.map(r=>[r.NodeId,r])),systems=new Map(d.Systems.map(s=>[s.SystemId,s])),features=new Map(raw.Features.map(f=>[f.FeatureId,f])),stars=new Map(raw.Features.filter(f=>f.Kind===1).map(f=>[f.SystemId,f])),candidates=new Map(d.HomeCandidates.map(h=>[h.NodeId,h])),bounds=new Map(),allEdges=new Map(),shared=new Map();
 for(const r of d.Regions){bounds.set(r.NodeId,{minX:Math.min(...r.Boundary.map(p=>p.X)),maxX:Math.max(...r.Boundary.map(p=>p.X)),minY:Math.min(...r.Boundary.map(p=>p.Y)),maxY:Math.max(...r.Boundary.map(p=>p.Y))});for(let i=0;i<r.Boundary.length;i++){const a=r.Boundary[i],b=r.Boundary[(i+1)%r.Boundary.length],key=edgeKey(a,b);if(!allEdges.has(key))allEdges.set(key,[]);allEdges.get(key).push({node:r.NodeId,a,b,key});}}
 for(const rows of allEdges.values()){if(rows.length===2){const[a,b]=rows,key=pairKey(a.node,b.node);if(!shared.has(key))shared.set(key,[]);shared.get(key).push(a);}}
 for(const rows of shared.values())rows.sort((a,b)=>a.key<b.key?-1:a.key>b.key?1:0);
 function route(ids){const vertices=[nodes.get(ids[0]).Center],portals=[];for(let i=1;i<ids.length;i++){const edges=shared.get(pairKey(ids[i-1],ids[i]));if(!edges?.length)throw Error('Accepted hop lacks exact positive shared edge');const e=edges[0];if(e.a.X===e.b.X&&e.a.Y===e.b.Y)throw Error('Zero-length route edge');const p2={X:e.a.X+e.b.X,Y:e.a.Y+e.b.Y};portals.push({from:ids[i-1],to:ids[i],a:e.a,b:e.b,midpoint2:p2});vertices.push({X:p2.X/2,Y:p2.Y/2},nodes.get(ids[i]).Center);}return{vertices,portals};}
 return{nodes,systems,features,stars,candidates,bounds,shared,route};
}
function visible(b,c,w,h,margin=24){return b.maxX*c.scale+c.tx>=-margin&&b.minX*c.scale+c.tx<=w+margin&&b.maxY*c.scale+c.ty>=-margin&&b.minY*c.scale+c.ty<=h+margin;}
function grid(points,cell=32){const bins=new Map();for(const p of points){const k=`${Math.floor(p.x/cell)},${Math.floor(p.y/cell)}`;if(!bins.has(k))bins.set(k,[]);bins.get(k).push(p);}return{xcell:cell,bins};}
function nearest(g,x,y,r=13){let best=null,bestD=Infinity;const{bins,xcell:c}=g;for(let i=Math.floor((x-r)/c);i<=Math.floor((x+r)/c);i++)for(let j=Math.floor((y-r)/c);j<=Math.floor((y+r)/c);j++)for(const p of bins.get(`${i},${j}`)||[]){const d=(p.x-x)**2+(p.y-y)**2;if(d<=r*r&&(d<bestD||(d===bestD&&p.id<best.id))){best=p;bestD=d;}}return best;}
window.SCALE_GEOMETRY={index,visible,grid,nearest};
})();
