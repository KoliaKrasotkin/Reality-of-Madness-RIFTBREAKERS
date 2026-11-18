// Auditoría OFFLINE de solo lectura de Assets. Solo escribe resultados en esta carpeta Docs.
// No abre Unity, no carga escenas en el Editor y no ejecuta generadores del proyecto.
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
const out = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(out, '../../../..');
const base = path.join(root, 'Docs/CiudadDelSol/Interior/Plan_20261117');
const read = p => fs.readFile(path.join(root, p), 'utf8');
const save = (name, value) => fs.writeFile(path.join(out, name), JSON.stringify(value, null, 2));
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
const vec = s => Object.fromEntries([...String(s || '').matchAll(/([xyzw]): ([-+\d.eE]+)/g)].map(m => [m[1], +m[2]]));
const dec = s => String(s || '').replace(/^"|"$/g, '').replace(/\\x([\da-fA-F]{2})/g, (_, h) => String.fromCharCode(parseInt(h, 16)));
const rooms = JSON.parse(await fs.readFile(path.join(base, 'Salas.json'), 'utf8'));
const links = JSON.parse(await fs.readFile(path.join(base, 'Conexiones.json'), 'utf8'));
const levantamiento = JSON.parse(await fs.readFile(path.join(base, 'Levantamiento.json'), 'utf8'));
const catalogue = JSON.parse(await fs.readFile(path.join(base, 'Prefabs_arquitectonicos.json'), 'utf8'));
const sceneBytes = await fs.readFile(path.join(root, levantamiento.scene));
const sceneSHA256 = hash(sceneBytes);
if (sceneSHA256 !== levantamiento.sceneSHA256) throw Error('La escena difiere del dossier: no reutilizar medidas.');
const text = sceneBytes.toString('utf8'), blocks = text.split(/\n--- !u!/);
const objects = new Map(), prefabs = [], mods = new Map();
for (const b of blocks) {
  const h = b.match(/^(\d+) &(-?\d+)/); if (!h) continue;
  const type = +h[1], id = h[2];
  if (type === 1) objects.set(id, {type,id,name:b.match(/\n  m_Name: (.*)/)?.[1],layer:+(b.match(/m_Layer: (\d+)/)?.[1]||0),active:b.match(/\n  m_IsActive: (\d)/)?.[1] !== '0'});
  if (type === 4) objects.set(id,{type,id,go:b.match(/m_GameObject: \{fileID: (-?\d+)/)?.[1],parent:b.match(/m_Father: \{fileID: (-?\d+)/)?.[1],p:vec(b.match(/m_LocalPosition: \{([^}]+)/)?.[1]),q:vec(b.match(/m_LocalRotation: \{([^}]+)/)?.[1]),s:vec(b.match(/m_LocalScale: \{([^}]+)/)?.[1])});
  if (type === 1001) {
    const list=[]; for(const m of b.matchAll(/- target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\s*\n\s*propertyPath: ([^\n]+)\n\s*value: ([^\n]*)/g))list.push({target:m[1],guid:m[2],path:m[3],value:m[4]});
    mods.set(id,{list,removed:[...b.matchAll(/- \{fileID: (-?\d+), guid: (\w+), type: \d+\}/g)].map(m=>m[1])});
    const props={}; for(const m of list)props[m.path]=m.value;
    prefabs.push({id,parent:b.match(/m_TransformParent: \{fileID: (-?\d+)/)?.[1],guid:b.match(/m_SourcePrefab: \{fileID: [^,]+, guid: (\w+)/)?.[1],props});
  }
}
const goTransforms = new Map([...objects.values()].filter(o=>o.type===4&&o.go).map(o=>[o.go,o.id]));
const hierarchy = id => {const o=objects.get(id);if(!o||o.type!==4)return '';return(o.parent&&o.parent!=='0'?hierarchy(o.parent)+'/':'')+(objects.get(o.go)?.name||'?');};
const active = id => {const o=objects.get(id);if(!o||o.type!==4)return true;return objects.get(o.go)?.active!==false&&(!o.parent||o.parent==='0'||active(o.parent));};
const inside = s => /^(Subsuelo_|Nucleo_|Umbral_)/.test(s);
const rotation = (p,q) => {const u=[q.x||0,q.y||0,q.z||0],s=q.w??1,cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]],v=cross(u,p),w=cross(u,v);return p.map((x,i)=>x+2*(s*v[i]+w[i]));};
const world = (p,id) => {if(!id||id==='0')return p;const t=objects.get(id);if(!t?.p||!Object.keys(t.p).length)return null;const v=rotation(p.map((n,i)=>n*([t.s.x,t.s.y,t.s.z][i]??1)),t.q).map((n,i)=>n+([t.p.x,t.p.y,t.p.z][i]??0));return world(v,t.parent);};
const mod = (inst,target,key,def) => mods.get(inst)?.list.find(m=>m.target===target&&m.path===key)?.value??def;
const models = new Map();
for(const cat of catalogue){
  const src=await read(cat.path), bs=src.split(/\n--- !u!/),trs=new Map(),gos=new Map(),colliders=[],renderers=[];
  for(const b of bs){const h=b.match(/^(\d+) &(-?\d+)/);if(!h)continue;const type=+h[1],id=h[2],go=b.match(/m_GameObject: \{fileID: (-?\d+)/)?.[1];
    if(type===1)gos.set(id,{name:b.match(/m_Name: (.*)/)?.[1],layer:+(b.match(/m_Layer: (\d+)/)?.[1]||0),active:b.match(/m_IsActive: (\d)/)?.[1]!=='0'});
    if(type===4)trs.set(id,{id,go,parent:b.match(/m_Father: \{fileID: (-?\d+)/)?.[1],p:vec(b.match(/m_LocalPosition: \{([^}]+)/)?.[1]),q:vec(b.match(/m_LocalRotation: \{([^}]+)/)?.[1]),s:vec(b.match(/m_LocalScale: \{([^}]+)/)?.[1])});
    if([65,64,135].includes(type))colliders.push({id,type,go,enabled:b.match(/m_Enabled: (\d)/)?.[1]!=='0',trigger:b.match(/m_IsTrigger: (\d)/)?.[1]==='1',size:vec(b.match(/m_Size: \{([^}]+)/)?.[1]),center:vec(b.match(/m_Center: \{([^}]+)/)?.[1]),mesh:b.match(/m_Mesh: \{fileID: (-?\d+), guid: (\w+)/)?.slice(1),convex:b.match(/m_Convex: (\d)/)?.[1]==='1'});
    if(type===23)renderers.push({id,go,enabled:b.match(/m_Enabled: (\d)/)?.[1]!=='0'});
  }
  models.set(cat.guid,{...cat,trs,gos,colliders,renderers,rootTransform:[...trs.values()].find(t=>t.parent==='0'),transformOfGO:new Map([...trs.values()].map(t=>[t.go,t.id]))});
}
const prefabActive=(p,m,gid)=>{const go=m.gos.get(gid);if(mod(p.id,gid,'m_IsActive',go?.active?'1':'0')==='0')return false;const t=m.trs.get(m.transformOfGO.get(gid));return t?.parent&&t.parent!=='0'?prefabActive(p,m,m.trs.get(t.parent)?.go):true;};
const prefabWorld=(pt,p,m,tid)=>{const t=m.trs.get(tid);if(!t)return null;const pos=['x','y','z'].map(k=>+mod(p.id,tid,'m_LocalPosition.'+k,t.p[k]??0)),sc=['x','y','z'].map(k=>+mod(p.id,tid,'m_LocalScale.'+k,t.s[k]??1)),q=Object.fromEntries(['x','y','z','w'].map(k=>[k,+mod(p.id,tid,'m_LocalRotation.'+k,t.q[k]??(k==='w'?1:0))]));const v=rotation(pt.map((n,i)=>n*sc[i]),q).map((n,i)=>n+pos[i]);return t.parent&&t.parent!=='0'?prefabWorld(v,p,m,t.parent):world(v,p.parent);};
const boxes=[],meshInstances=[],disabled=[],visualOnly=[],visualBoxesWithoutCollider=[];
function addBox(ref,size,center,transform){
  const corners=[];for(const x of [-1,1])for(const y of [-1,1])for(const z of [-1,1]){const v=transform([center.x+x*size.x/2,center.y+y*size.y/2,center.z+z*size.z/2]);if(v)corners.push(v);}if(corners.length!==8)return;
  const origin=transform([0,0,0]),axes=[[1,0,0],[0,1,0],[0,0,1]].map(a=>transform(a).map((v,i)=>v-origin[i])),lengths=axes.map(a=>Math.hypot(...a)),unitAxes=axes.map((a,i)=>a.map(v=>v/lengths[i]));
  boxes.push({...ref,corners,origin,axes,size,localCenter:center,obbCenter:[0,1,2].map(k=>origin[k]+axes[0][k]*center.x+axes[1][k]*center.y+axes[2][k]*center.z),unitAxes,half:[size.x*lengths[0]/2,size.y*lengths[1]/2,size.z*lengths[2]/2],orthogonal:unitAxes.every((a,i)=>unitAxes.every((b,j)=>i===j||Math.abs(a.reduce((s,v,k)=>s+v*b[k],0))<.00001)),axisAligned:axes.every(a=>a.filter(n=>Math.abs(n)>.0001).length===1),x0:Math.min(...corners.map(v=>v[0])),x1:Math.max(...corners.map(v=>v[0])),y0:Math.min(...corners.map(v=>v[1])),y1:Math.max(...corners.map(v=>v[1])),z0:Math.min(...corners.map(v=>v[2])),z1:Math.max(...corners.map(v=>v[2]))});
}
for(const p of prefabs){p.path=hierarchy(p.parent);if(!inside(p.path)||!active(p.parent))continue;const m=models.get(p.guid);if(!m)continue;
  for(const c of m.colliders){const enabled=mod(p.id,c.id,'m_Enabled',c.enabled?'1':'0')==='1',trigger=mod(p.id,c.id,'m_IsTrigger',c.trigger?'1':'0')==='1',on=prefabActive(p,m,c.go),removed=mods.get(p.id)?.removed.includes(c.id),ref={instance:p.id,collider:c.id,name:m.gos.get(c.go)?.name||p.props.m_Name,path:dec(p.path)+'/'+p.props.m_Name,prefabGUID:p.guid,layer:+mod(p.id,c.go,'m_Layer',m.gos.get(c.go)?.layer||0),source:'Prefab YAML y overrides por ID',isScene:false};
    if(!enabled||trigger||!on||removed){disabled.push({...ref,enabled,trigger,active:on,removed:!!removed});const rr=m.renderers.find(r=>r.go===c.go);if(on&&!enabled&&rr&&mod(p.id,rr.id,'m_Enabled',rr.enabled?'1':'0')==='1'){visualOnly.push(ref);if(c.type===65){const before=boxes.length,tid=m.transformOfGO.get(c.go);addBox(ref,Object.fromEntries(['x','y','z'].map(k=>[k,+mod(p.id,c.id,'m_Size.'+k,c.size[k])])),Object.fromEntries(['x','y','z'].map(k=>[k,+mod(p.id,c.id,'m_Center.'+k,c.center[k])])),point=>prefabWorld(point,p,m,tid));if(boxes.length>before)visualBoxesWithoutCollider.push({...boxes.pop(),colliderEnabled:false});}}continue;}
    const tid=m.transformOfGO.get(c.go);
    if(c.type===65)addBox(ref,Object.fromEntries(['x','y','z'].map(k=>[k,+mod(p.id,c.id,'m_Size.'+k,c.size[k])])),Object.fromEntries(['x','y','z'].map(k=>[k,+mod(p.id,c.id,'m_Center.'+k,c.center[k])])),point=>prefabWorld(point,p,m,tid));
    if(c.type===64)meshInstances.push({...ref,mesh:c.mesh,convex:c.convex,scope:'Prefab'});
  }
}
for(const b of blocks){const h=b.match(/^(65|64) &(-?\d+)/);if(!h)continue;const go=b.match(/m_GameObject: \{fileID: (-?\d+)/)?.[1],tid=goTransforms.get(go),hp=hierarchy(tid);if(!inside(hp)||!active(tid)||b.match(/m_Enabled: (\d)/)?.[1]==='0'||b.match(/m_IsTrigger: (\d)/)?.[1]==='1')continue;const ref={instance:tid,collider:h[2],name:objects.get(go)?.name,path:dec(hp),source:'Collider YAML de escena activo',isScene:true};
  ref.layer=objects.get(go)?.layer??0;
  if(h[1]==='65'){const sm=b.match(/m_Size: \{([^}]+)/),cm=b.match(/m_Center: \{([^}]+)/);if(sm&&cm)addBox(ref,vec(sm[1]),vec(cm[1]),p=>world(p,tid));}
  else meshInstances.push({...ref,mesh:b.match(/m_Mesh: \{fileID: (-?\d+), guid: (\w+)/)?.slice(1),convex:b.match(/m_Convex: (\d)/)?.[1]==='1',scope:'Escena'});
}
await save('Colliders_activos.json',boxes);await save('Colliders_malla.json',meshInstances);await save('Colliders_no_fisicos.json',disabled);await save('Geometria_visible_sin_collider.json',visualOnly);
await save('Cajas_visuales_sin_colision.json',visualBoxesWithoutCollider);
const grid=new Map();for(const b of boxes)for(let x=Math.floor(b.x0/6);x<=Math.floor(b.x1/6);x++)for(let z=Math.floor(b.z0/6);z<=Math.floor(b.z1/6);z++){const k=x+','+z;if(!grid.has(k))grid.set(k,[]);grid.get(k).push(b);}
const nearby=(p,r=.57)=>{const set=new Set();for(let x=Math.floor((p[0]-r)/6);x<=Math.floor((p[0]+r)/6);x++)for(let z=Math.floor((p[2]-r)/6);z<=Math.floor((p[2]+r)/6);z++)for(const b of grid.get(x+','+z)||[])set.add(b);return [...set];};
function segmentBoxDist2(a,b,half){const d=b.map((v,i)=>v-a[i]),cuts=[0,1];for(let i=0;i<3;i++){if(Math.abs(d[i])<1e-12)continue;for(const edge of [-half[i],half[i]]){const t=(edge-a[i])/d[i];if(t>0&&t<1)cuts.push(t);}}cuts.sort((a,b)=>a-b);const dist=t=>a.reduce((s,v,i)=>{const n=v+d[i]*t,z=Math.max(-half[i]-n,0,n-half[i]);return s+z*z;},0);let best=Math.min(dist(0),dist(1));for(let j=1;j<cuts.length;j++){const lo=cuts[j-1],hi=cuts[j],mid=(lo+hi)/2;let aa=0,bb=0;for(let i=0;i<3;i++){const p=a[i]+d[i]*mid;if(p>=-half[i]&&p<=half[i])continue;const edge=p<0?-half[i]:half[i];aa+=d[i]*d[i];bb+=(a[i]-edge)*d[i];}const t=aa?Math.max(lo,Math.min(hi,-bb/aa)):mid;best=Math.min(best,dist(t),dist(lo),dist(hi));}return best;}
function conflicts(p,r=.32,h=2){const hits=[];for(const b of nearby(p,r)){if(b.y1<=p[1]+.0001||b.y0>=p[1]+h-.0001||!b.orthogonal)continue;const toLocal=point=>b.unitAxes.map(a=>a.reduce((s,v,i)=>s+v*(point[i]-b.obbCenter[i]),0));const d2=segmentBoxDist2(toLocal([p[0],p[1]+r,p[2]]),toLocal([p[0],p[1]+h-r,p[2]]),b.half),depth=r-Math.sqrt(d2);if(depth>.0001)hits.push({b,depth});}return hits;}
const support=p=>nearby(p,0).some(b=>b.name==='Platform_Base_01'&&!/Cimentacion|Basamento|Apoyos|Espesor|contrafuertes|Cubierta|moldura/i.test(b.path)&&Math.abs(b.y1-p[1])<.01&&p[0]>=b.x0-.0001&&p[0]<=b.x1+.0001&&p[2]>=b.z0-.0001&&p[2]<=b.z1+.0001);
const connectionAudits=[];
for(const l of links){const collisions=new Map(),navCollisions=new Map(),gaps=[],laneGaps=[],laneBoxes=new Map();let count=0;
  for(let i=1;i<l.points.length;i++){const a=l.points[i-1],b=l.points[i],n=Math.max(1,Math.ceil(Math.hypot(...a.map((v,k)=>b[k]-v))/.5)),hz=Math.hypot(b[0]-a[0],b[2]-a[2]),side=hz?[-(b[2]-a[2])/hz,0,(b[0]-a[0])/hz]:[0,0,0],horizontal=Math.abs(a[1]-b[1])<.001;
    for(let j=0;j<=n;j++){const p=a.map((v,k)=>v+(b[k]-v)*j/n);count++;
      for(const hit of conflicts(p,.32)){const key=hit.b.instance+':'+hit.b.collider;if(!collisions.has(key))collisions.set(key,{instance:hit.b.instance,collider:hit.b.collider,name:hit.b.name,path:hit.b.path,points:[],maxDepth:0});const c=collisions.get(key);if(c.points.length<4)c.points.push({p,segment:i,t:j/n});c.maxDepth=Math.max(c.maxDepth,hit.depth);}
      for(const hit of conflicts(p,.57)){const key=hit.b.instance+':'+hit.b.collider;if(!navCollisions.has(key))navCollisions.set(key,{instance:hit.b.instance,name:hit.b.name,path:hit.b.path,p,maxDepth:hit.depth});else navCollisions.get(key).maxDepth=Math.max(navCollisions.get(key).maxDepth,hit.depth);}
      if(horizontal){if(!support(p)&&gaps.length<20)gaps.push({p,segment:i});if(j%3===0||j===n)for(const offset of [-2.25,-.75,.75,2.25]){const q=p.map((v,k)=>v+side[k]*offset);if(!support(q)&&laneGaps.length<20)laneGaps.push({p:q,segment:i,offset});for(const hit of conflicts(q,.32)){const key=hit.b.instance+':'+hit.b.collider;if(!laneBoxes.has(key))laneBoxes.set(key,{name:hit.b.name,path:hit.b.path,p:q,offset,depth:hit.depth});}}}
    }
  }
  const proposed=/^(H|LA)/.test(l.id),coll=[...collisions.values()],strong=coll.filter(c=>c.maxDepth>.05);
  connectionAudits.push({...l,classification:strong.length?'Parcial o bloqueada: interferencia del eje propuesto con sólido activo':proposed?'Propuesta: paso físico individual no acreditado':'Construida, pero pendiente de validación',classCode:strong.length?'C':proposed?'D':'B',geometryBasis:proposed?'Código de enlace del dossier; no objeto arquitectónico de escena':'Estructuras y descensos asociados presentes en escena y fuentes',checkedSamples:count,warriorBoxConflicts:coll,humanoidBoxConflicts:[...navCollisions.values()],centerlineSupportGaps:gaps,fourLaneBoxWarnings:[...laneBoxes.values()],laneSupportWarnings:laneGaps,strongConflictCount:strong.length,navMeshBidirectional:'NO EJECUTADO EN UNITY',caveat:'C = eje interferido, no prueba de desconexión global. D = no se demuestra inexistencia de un paso alternativo. Contactos pequeños de escalera no se declaran bloqueo: requiere CharacterController y soporte real.'});
}
await save('Conexiones_auditadas.json',connectionAudits);
const roomAudits=[];
for(const r of rooms){const meshGuids=levantamiento.ceilingMeshes.filter(m=>m.path.startsWith(r.source+'/')).map(m=>m.guid),existingMeshes=meshInstances.filter(m=>m.mesh&&meshGuids.includes(m.mesh[1])),bodyBoxes=boxes.filter(b=>b.x1>r.x-r.w/2&&b.x0<r.x+r.w/2&&b.z1>r.z-r.d/2&&b.z0<r.z+r.d/2&&b.y1>r.y+.01&&b.y0<r.y+2);let floorArea=0,clearArea=0,unsupported=0;const obstacles=new Map();
  for(let x=r.x-r.w/2+.5;x<r.x+r.w/2;x+=1)for(let z=r.z-r.d/2+.5;z<r.z+r.d/2;z+=1){const p=[x,r.y,z];if(!support(p)){unsupported++;continue;}floorArea++;const hits=conflicts(p,.32);if(!hits.length)clearArea++;for(const hit of hits){const k=hit.b.instance+':'+hit.b.collider;if(!obstacles.has(k))obstacles.set(k,{name:hit.b.name,path:hit.b.path,instance:hit.b.instance,collider:hit.b.collider,samples:0,p,depth:hit.depth});obstacles.get(k).samples++;}}
  const ceilBoxes=nearby([r.x,r.y,r.z],0).filter(b=>b.axisAligned&&b.x0<=r.x&&b.x1>=r.x&&b.z0<=r.z&&b.z1>=r.z&&b.y0>r.y+.01).sort((a,b)=>a.y0-b.y0),ceil=[r.ceiling,ceilBoxes[0]?.y0].filter(v=>typeof v==='number'&&Number.isFinite(v));
  roomAudits.push({...r,presence:r.category==='P'?'Solo propuesta funcional dentro de una cámara existente':r.category==='A'?'Acceso/atrio construido':r.category==='B'?'Galería, puente o descanso construido':r.category==='D'?'Recinto vertical construido':'Volumen abovedado existente, no habitación cerrada independiente',construction:r.category==='P'?'Propuesta sin recinto independiente':'Volumen estructural construido; completitud funcional no acreditada',completedInUnity:false,measuredCenter:[r.x,r.y,r.z],measuredDimensions:[r.w,r.d],floorSupportedAreaSampled:floorArea,areaWithoutKnownBoxConflictUpperEstimate:clearArea,unsupportedAreaInEnvelopeSampled:unsupported,samplingResolutionM:1,areaCaveat:'Estimación de apoyo por cajas y cota superior de área libre; no área NavMesh ni superficie jugable certificada. No sumar recintos superpuestos.',activeBodyBoxCount:bodyBoxes.length,existingColliderMeshes:existingMeshes.map(m=>({instance:m.instance,collider:m.collider,path:m.path,mesh:m.mesh})),obstacles:[...obstacles.values()].sort((a,b)=>b.samples-a.samples),centerCeilingFromDecodedGeometry:ceil.length?Math.min(...ceil):null,centerCeilingCaveat:'Mallas de nave del dossier y cajas activas; no todos los MeshCollider importados. null no significa cielo abierto.',existingConnections:r.connections.filter(id=>!/^(H|LA)/.test(id)),proposedConnections:r.connections.filter(id=>/^(H|LA)/.test(id)),blockedProposedAccesses:connectionAudits.filter(l=>(l.from===r.id||l.to===r.id)&&l.classCode==='C').map(l=>l.id),missingArchitecture:r.category==='P'?'Falta aprobar subzona del jefe y su despeje tridimensional; no añadir una sala por defecto.':r.category==='A'?'No hay estructura faltante demostrada; pendientes juntas, huecos útiles, protección y recepción del acceso.':r.category==='B'?'No hay tramo faltante demostrado; pendientes anchura efectiva, juntas, protección y recepción.':r.category==='D'?'Pendientes delimitación jugable, huecos efectivos, límites de combate y recepción del recorrido entre apoyos y techos.':'No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.'});
}
const canonical=s=>dec(s).replaceAll('"','');
const hierarchyPaths=new Set([...objects.values()].filter(o=>o.type===4&&o.go).map(o=>canonical(hierarchy(o.id))));
for(const r of roomAudits){
  r.sourceHierarchyExactMatch=hierarchyPaths.has(canonical(r.source));
  r.sourceMeshColliders=meshInstances.filter(m=>canonical(m.path).startsWith(canonical(r.source)+'/')).map(m=>({instance:m.instance,collider:m.collider,name:m.name,path:m.path,mesh:m.mesh,convex:m.convex}));
  r.sourceHierarchyCandidates=r.sourceHierarchyExactMatch?[]:[...hierarchyPaths].filter(p=>p.startsWith(r.source.split('/').slice(0,2).join('/')+'/')&&p.split('/').length<=4).slice(0,30);
}
await save('Espacios_auditados.json',roomAudits);
const candidates=[{for:'H46',points:[[156,-16,99],[180,-16,99],[180,-16,12],[141,-16,12],[141,-16,3]]},{for:'H47',points:[[141,-16,3],[141,-16,12],[183,-16,12],[183,-16,-3]]},{for:'LA3',points:[[-81,-16,-15],[-33,-16,-15],[-33,-16,-39],[63,-16,-39],[63,-16,12],[147,-16,12],[147,-16,-63]]},{for:'LA4',points:[[147,-16,-63],[135,-16,-63],[135,-16,12],[120,-16,12],[120,-16,126],[24,-16,126],[24,-16,153],[21,-16,153]]}];
for(const c of candidates){const hits=new Map(),gaps=[];for(let i=1;i<c.points.length;i++){const a=c.points[i-1],b=c.points[i],n=Math.ceil(Math.hypot(...a.map((v,k)=>b[k]-v))/.5);for(let j=0;j<=n;j++){const p=a.map((v,k)=>v+(b[k]-v)*j/n);for(const hit of conflicts(p,.57)){const key=hit.b.instance+':'+hit.b.collider;if(!hits.has(key))hits.set(key,{name:hit.b.name,path:hit.b.path,p,depth:hit.depth});}if(!support(p)&&gaps.length<20)gaps.push(p);}}c.boxConflicts=[...hits.values()];c.supportGaps=gaps;c.status='Solo propuesta documental: no aplicada; pendiente MeshCollider, ancho y NavMesh.';}
await save('Alternativas_no_aplicadas.json',candidates);
const shared=connectionAudits.find(c=>c.id==='VS01');
const corrected=[];for(let i=0;i<shared.points.length;i++){const p=shared.points[i];corrected.push(p);const next=shared.points[i+1];if(next&&p[0]===135&&p[2]===105&&next[0]===120&&next[2]===105)corrected.push([135,-16,111],[120,-16,111]);}
const sharedCandidate={for:'VC01/VE01/VS01 (tramo común)',points:corrected,boxConflicts:[],supportGaps:[],status:'Solo desvío documental del eje común por Z111; no aplicado ni aprobado.'};
const hitKeys=new Set();for(let i=1;i<corrected.length;i++){const a=corrected[i-1],b=corrected[i],n=Math.ceil(Math.hypot(...a.map((v,k)=>b[k]-v))/.5);for(let j=0;j<=n;j++){const p=a.map((v,k)=>v+(b[k]-v)*j/n);for(const h of conflicts(p,.57)){const k=h.b.instance+':'+h.b.collider;if(!hitKeys.has(k)){hitKeys.add(k);sharedCandidate.boxConflicts.push({name:h.b.name,path:h.b.path,p,depth:h.depth});}}if(a[1]===b[1]&&!support(p)&&sharedCandidate.supportGaps.length<20)sharedCandidate.supportGaps.push(p);}}
candidates.push(sharedCandidate);
await save('Alternativas_no_aplicadas.json',candidates);
const blocked=new Set(connectionAudits.filter(c=>c.classCode==='C').map(c=>c.id)),left=new Set(rooms.map(r=>r.id)),components=[];
while(left.size){const seed=left.values().next().value,q=[seed];left.delete(seed);for(let i=0;i<q.length;i++)for(const l of links.filter(l=>!blocked.has(l.id))){const next=l.from===q[i]?l.to:l.to===q[i]?l.from:null;if(next&&left.delete(next))q.push(next);}components.push(q);}
const originalHashes=JSON.parse(await fs.readFile(path.join(out,'Integridad_inicial.json'),'utf8')),finalHashes=[];
for(const p of originalHashes){const buf=await fs.readFile(path.join(root,p.path));finalHashes.push({...p,sha256:hash(buf),same:hash(buf)===p.sha256});}
await save('Integridad_final.json',finalHashes);
const disabledCounts={};for(const c of disabled){const key=c.name+' / '+(!c.active?'objeto inactivo':!c.enabled?'collider desactivado':c.trigger?'trigger':'retirado');disabledCounts[key]=(disabledCounts[key]||0)+1;}
const tests=[
 {name:'Distancia de segmento que atraviesa caja',pass:segmentBoxDist2([-2,0,0],[2,0,0],[1,1,1])===0},
 {name:'Distancia paralela exterior a caja',pass:Math.abs(segmentBoxDist2([2,-2,0],[2,2,0],[1,1,1])-1)<1e-9},
 {name:'Distancia puntual a esquina',pass:Math.abs(segmentBoxDist2([2,2,2],[2,2,2],[1,1,1])-3)<1e-9},
 {name:'Todas las cajas transformadas tienen ejes ortogonales',pass:boxes.every(b=>b.orthogonal)},
 {name:'54 naves conservan dos MeshCollider asociados',pass:roomAudits.filter(r=>['R','G','M'].includes(r.category)).every(r=>r.existingColliderMeshes.length===2)},
 {name:'Cuatro interferencias de eje documentadas',pass:['H46','H47','LA3','LA4'].every(id=>connectionAudits.find(c=>c.id===id)?.classCode==='C')},
 {name:'Alternativas sin cruces de caja conocidos ni huecos de apoyo',pass:candidates.every(c=>!c.boxConflicts.length&&!c.supportGaps.length)},
 {name:'Escena, NavMeshData y dossier conservados',pass:finalHashes.every(p=>p.same)}
];
await save('Pruebas_offline.json',tests);
const summary={sceneSHA256,sceneBytes:sceneBytes.length,spaces:roomAudits.length,connections:connectionAudits.length,categoryCounts:connectionAudits.reduce((a,c)=>(a[c.classCode]=(a[c.classCode]||0)+1,a),{}),liveUnityTests:0,activeBoxes:boxes.length,activeMeshColliders:meshInstances.length,disabledCounts,visualOnlyCount:visualOnly.length,missingLiteralHierarchy:roomAudits.filter(r=>!r.sourceHierarchyExactMatch).map(r=>({id:r.id,source:r.source})),naveEnvelopeArea:roomAudits.filter(r=>['R','G','M'].includes(r.category)).reduce((s,r)=>s+r.w*r.d,0),designComponentsWithoutBlockedTraces:components,detourCandidates:candidates.map(c=>({for:c.for,boxHits:c.boxConflicts.length,missingSupport:c.supportGaps.length})),protectedUnchanged:finalHashes.every(p=>p.same),offlineTests:tests,notes:['D no demuestra que no exista un paso alternativo.','C corresponde a colisión del eje en archivos; no certifica desconexión NavMesh.','No se declaran salas terminadas sin recepción en Unity.','No se ejecuta ni modifica Unity.']};
await save('Resumen_cajas.json',summary);
console.log(JSON.stringify({spaces:roomAudits.length,connections:connectionAudits.length,classes:summary.categoryCounts,boxes:boxes.length,mesh:meshInstances.length,components:components.map(c=>c.length),unchanged:summary.protectedUnchanged}));
export {conflicts, support};
