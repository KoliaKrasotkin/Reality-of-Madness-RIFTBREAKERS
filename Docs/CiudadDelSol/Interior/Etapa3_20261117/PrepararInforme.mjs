// Documentation only. Reads native evidence; never edits Assets, scenes or NavMesh.
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const folder=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(folder,'../../../..');
const backup=path.join(root,'Backups/CiudadDelSol/Interior/Etapa3_20261117');
const load=async p=>JSON.parse(await fs.readFile(p,'utf8'));
const exists=async p=>fs.access(p).then(()=>true,()=>false);
const hash=b=>crypto.createHash('sha256').update(b).digest('hex').toUpperCase();
const source=path.join(backup,'Recepcion.json');
const native=await exists(source)?await load(source):null;
const batchMarker=path.join(backup,'Ejecucion_batch.txt');
const batchStarted=await exists(batchMarker)?(await fs.readFile(batchMarker,'utf8')).split('\n')[0].trim():null;
const nativeBatch=!!(native&&batchStarted&&Date.parse(native.started)>=Date.parse(batchStarted));
const regressionSource=path.join(backup,'RegresionEtapa2/Recepcion.json');
const regression=await exists(regressionSource)?await load(regressionSource):null;
const plans=(await load(path.join(root,'Docs/CiudadDelSol/Interior/Plan_20261117/Conexiones.json'))).filter(p=>['VE01','VE02','VD01'].includes(p.id));
const audit=(await load(path.join(root,'Docs/CiudadDelSol/Interior/Auditoria_20261117/Conexiones_con_mallas.json'))).filter(p=>plans.some(q=>q.id===p.id));
const before=await load(path.join(backup,'Antes_SHA256.json'));
const testsPath=path.join(backup,'Regresiones.json');
const tests=await exists(testsPath)?await load(testsPath):null;
const integrity=await Promise.all(before.map(async p=>({...p,after:hash(await fs.readFile(path.join(root,p.path)))})));
for(const p of integrity)p.unchanged=p.sha256===p.after;
const protectedIntact=integrity.filter(p=>!p.path.endsWith('CiudadDelSolInteriorStageTwo.cs')).every(p=>p.unchanged);
if(!protectedIntact)throw Error('Cambió un archivo protegido: no publicar aprobaciones.');
const regressionOK=regression&&!regression.error&&regression.filesUnchanged&&regression.connections.length===6&&regression.connections.every(c=>c.technicalIndividual);
const evidenceOK=native&&!native.error&&native.filesUnchanged&&!native.playMode&&!native.simulation&&native.negativeControlsRejectObstaclesAndOtherLevels&&regressionOK;
const approved=evidenceOK?native.connections.filter(c=>c.technicalIndividual).map(c=>c.plan.id):[];
const completed=approved.length===3;
const xyz=p=>`(${p.map(v=>Number(v).toFixed(2)).join(', ')})`;
const point=p=>Array.isArray(p)?p:[p.x,p.y,p.z];
const minWidth=c=>c?.guide?.widths?.length?Math.min(...c.guide.widths.map(w=>w.usefulWidthLowerBound)):null;
const fmt=v=>v==null?'Pendiente':Number(v).toFixed(2);
await fs.writeFile(path.join(folder,'Integridad.json'),JSON.stringify(integrity,null,2));
const summary={generatedUTC:new Date().toISOString(),source:native?source:null,sourceSHA256:native?hash(await fs.readFile(source)):null,
  approved,pending:plans.filter(p=>!approved.includes(p.id)).map(p=>p.id),complete:completed,realPlayersTested:false,nativeBatch,
  regressionStageTwo:regressionOK?'6/6 nativas':'Pendiente de ejecución nativa',protectedIntact,
  connections:plans.map(p=>{const c=native?.connections.find(c=>c.plan.id===p.id);return {id:p.id,from:p.from,to:p.to,approved:approved.includes(p.id),chosen:c?.chosen??null,minWidth:minWidth(c),forward:c?.forward.status??null,reverse:c?.reverse.status??null};})};
await fs.writeFile(path.join(folder,'Resumen.json'),JSON.stringify(summary,null,2));
if(native)await fs.copyFile(source,path.join(folder,'Recepcion_Unity.json'));
const lines=[
'# Ciudad del Sol — Etapa 3: acceso Sureste y conexión inferior','',
`**Estado: ${completed?'recepción técnica individual completa, 3/3':'recepción técnica NO completada'}.** ${native?'Existe evidencia nativa; ver resultados individuales.':'Unity no importó aún la nueva selección. No se ejecutaron ni se simularon estas tres conexiones.'} Cuatro jugadores reales: pendiente.`,
'','## Alcance e inventario comprobado en archivos','',
'Se consultaron Plan_20261117, Auditoria_20261117, Etapa1_20261117, RecepcionIndividual_20261117 y la recepción 6/6 de Etapa2_20261117. Se conserva el mismo archivo de escena de 96.932.348 bytes y su SHA-256; no se ejecutaron los generadores de construcción.',
'',
'| Conexión | Extremos documentales originales | Desnivel | Recorrido documental | Dependencias existentes | Resultado |',
'|---|---|---:|---:|---|---|',
...plans.map(p=>`| ${p.id} · ${p.from} ↔ ${p.to} | ${xyz(p.points[0])} ↔ ${xyz(p.points.at(-1))} | ${p.deltaY} m | ${p.length3D.toFixed(2)} m | ${p.id==='VE01'?'A03; galerías a −16; cuatro y tres escaleras 2/6; descanso a −24; B06':p.id==='VE02'?'B06 a −30; giro de terraza; dos escaleras 2/6; D02 a −34':'D02 y D01 a −34; galerías inferiores y pilares de arcadas'} | ${approved.includes(p.id)?'Recibida técnicamente':'Pendiente'} |`),
'',
'B06 es la terraza centrada en (120, −30, −42), con huella documental 24 × 12 m. D02 está centrada en (99, −34, −9), huella 66 × 78 m; D01 en (105, −34, 63), huella 54 × 66 m. A03 tiene centro documental (156, −16, 102), pero el extremo de circulación usado es (156, −16, 99). Esas envolventes no acreditan que todas sus áreas sean jugables.',
'',
'La secuencia es coherente en cotas: A03 −16 → descanso −24 → B06 −30 → D02 −34 → D01 −34. VD01 cierra un lazo con el acceso VC02 de la Etapa 2; no lleva a la cámara final D03. En las escaleras la selección añade los extremos de cada módulo 2/6 que el dossier resumía en tramos largos: no crea escaleras ni cambia trazados.',
...(completed?['','**Ancla recibida en B06: (123, −30, −45).** Sustituye al centro documental (120, −30, −42) como punto de unión de VE01 y VE02, sin mover la sala ni la baranda. VE01 utiliza el rodeo norte de A03. Los ejes rechazados siguen archivados y no se presentan como transitables.']:[]),
'','## Problemas y alternativas existentes','',
'La auditoría de mallas, cuya firma de escena coincide con la actual, detectó una intersección del eje VE01 en (132, −16, 105) con la pilastra de `Subsuelo_34_Naves_Sin_Reticula / Nave 102/102 (30×42) / Nervaduras, pilastras y transiciones`, recurso `Assets/ROM/CiudadDelSol34Naves/Nave_08_2.asset`. Es evidencia de archivos de geometría, no un resultado nuevo de PhysX. No se elimina ni recorta esa pilastra.',
'',
'El receptor comprueba primero los tres ejes originales. Solo si VE01 falla, comprueba también un rodeo existente: (156,−16,99) → (174,−16,99) → (174,−16,117) → (120,−16,117) → (120,−16,105), y desde allí la continuación original hasta B06. El tramo norte procede de Etapa 1; toda la continuación se vuelve a evaluar con física y NavMesh. Proponerlo no lo acredita. Los resultados del eje original se guardan en EjesOriginales y los del rodeo en AlternativaNorteVE01.',
'',
'Las advertencias laterales de la auditoría señalan Railing_01 del Centro Oriental junto a X≈132,75, Z=63…90; barandas del descanso a Y=−24; protecciones del borde sur de B06 a Y=−30; y Column_Stackable_01 de las arcadas a Y=−34, X≈90,75. No se confunde una advertencia de cuatro carriles de cajas con un bloqueo del Warrior ni se retira una protección por esa advertencia.',
'',
...(native?['### Hallazgo real de Unity: ancla documental de B06','',
'La primera recepción nativa confirmó el obstáculo de la pilastra y otro problema compartido: el punto documental (120, −30, −42) corta lateralmente una baranda de la escalera. El MeshCollider activo es Nucleo_Monumental_Conjunto/02 - Sureste arcadas aprobadas/07 - Protecciones/Railing_Staircase_01, GlobalObjectId terminado en 1916801645-0. Hubo seis estaciones obstruidas cerca del pie y falta de proyección local del extremo. El rodeo norte evita la pilastra, pero no corrige ese extremo: tampoco se aprueba por sí solo.','',
`El generador existente registra la salida física (123, −30, −42) → (123, −30, −45) → (111, −30, −45). ${completed?'Unity confirmó la alternativa completa, con ancla común (123, −30, −45), en ambos sentidos y sin atravesar la baranda.':'La selección alternativa sigue pendiente de recepción.'} Se conserva el centro documental de la sala; solo se replantea el punto de paso. Las primeras pruebas se conservan en AntesDescansoB06 y los ensayos nuevos en DescansoRealB06.`,'']:[]),
'## Resultados nativos individuales','',
...plans.flatMap(p=>{
 const c=native?.connections.find(c=>c.plan.id===p.id);
 if(!c)return [`### ${p.id}`,'','Sin recepción nativa disponible. Apoyo, huella, cápsula, barridos, pendientes, escalones, obstáculos, bordes y NavMesh bidireccional siguen pendientes.',''];
 return [`### ${p.id} — ${approved.includes(p.id)?'recibida técnicamente':'pendiente'}`,'',
 `Extremos realmente evaluados: ${xyz(point(c.plan.points[0]))} ↔ ${xyz(point(c.plan.points.at(-1)))}.`,
 `Puntos de paso evaluados: ${c.plan.points.map(q=>xyz(point(q))).join(' → ')}.`,
 `Elección: ${c.chosen}. Anchura útil inferior muestreada: ${fmt(minWidth(c))} m. Cuatro cápsulas estáticas: ${c.geometryFourLanes}; protección de bordes: ${c.edgeProtectionClear}.`,
 `Guía física: ${c.guide.samples} muestras, ${c.guide.failures} fallos de estación, ${c.guide.sweepFailures} barridos fallidos, ${c.guide.missingNav} sin NavMesh; pendiente máxima ${fmt(c.guide.maximumSlope)}°.`,
 ...[['Ida',c.forward],['Regreso',c.reverse]].map(([label,r])=>`${label}: ${r.status}, físico=${r.physical}, impactos NavMesh sin resolver=${r.blockedRaycasts}, muestras=${r.trace?.samples??0}, fallos=${r.trace?.failures??'no evaluado'}, barridos=${r.trace?.sweepFailures??'no evaluado'}, sin NavMesh=${r.trace?.missingNav??'no evaluado'}, discontinuidades=${r.trace?.disconnectedSurfaceSteps??'no evaluado'}.`),
 ...[...c.guide.problems,...(c.forward.trace?.problems??[]),...(c.reverse.trace?.problems??[])].map(q=>`- ${q.kind} en ${xyz(point(q.position))}: ${q.hierarchy||'sin collider identificado'} (${q.occurrences} observaciones).`),
 ''];
}),
'## Herramienta, compilación y protecciones','',
'Se reutiliza el motor de CiudadDelSolInteriorStageTwo.cs mediante una entrada parametrizada por selección y carpeta de resultados. No se duplicaron los métodos de física, cápsula, barridos, pendientes, raycasts o navegación. CiudadDelSolInteriorStageThree.cs es una selección de conexiones, un menú y una solicitud explícita de recepción; no es un sistema de gameplay.',
'',
'La compilación C# de Editor pasó con el compilador instalado de Unity 6000.3.23f1. Las advertencias CS0618 pertenecen al diagnóstico experimental antiguo, que no se invoca. No se utiliza NavMeshQuery experimental ni se crean enlaces, instancias de Warrior o objetos de prueba.',
'',
...(nativeBatch?[`La recepción actual se ejecutó con Unity ${native.unity} en modo batch/nographics, después de verificar fuera del sandbox que no había otro Editor del proyecto. Se cargó la escena guardada mediante OpenScene y se invocó el mismo receptor en Edit Mode. Son consultas reales de PhysX y NavMesh; no renderizado, simulación de movimiento ni una sesión Host. Inicio: ${native.started}; cierre de consultas: ${native.ended}. El registro completo está en Unity_recepcion_batch.log.`,'']:[]),
`Comprobaciones de código e integridad: ${tests?.testsPassed??'pendientes'}. Incluyen comparación literal del motor recibido, conservación de los seis planes anteriores, extremos y puntos originales de VE01/VE02/VD01 y nueve escaleras 2/6. ${tests?.nativeAvailable?'También se contrastó la recepción nativa nueva.':'No son consultas PhysX/NavMesh: la recepción nativa nueva sigue pendiente.'}`,
'',
`Regresión nativa de Etapa 2: ${regressionOK?'seis conexiones satisfactorias, sin sobrescribir la evidencia anterior':'pendiente de ejecutar; el resultado previo 6/6 se conserva sin reinterpretarlo'}. Integridad de escena, NavMeshData, población y Etapa 1: ${protectedIntact?'SHA-256 conservados':'REVISAR'}. La modificación del receptor Editor está respaldada y declarada; no es una modificación arquitectónica.`,
'',
'Las consultas conservan el Warrior original (radio 0,32; altura 2; escalón 0,30; pendiente 48°), Humanoid 0 (radio 0,57; escalón 0,36), proyección local y comprobación de nivel. No se amplían tolerancias ni se cambian colliders. Las cuatro cápsulas estáticas no equivalen a cuatro jugadores reales, evitación dinámica o movimiento del CharacterController.',
'',
'**Obras realizadas: ninguna.** Se aprovechó el trazado físico existente; no se construyó, eliminó arquitectura, movió barandas ni actualizó NavMeshData. Se preservan los siete sectores, las Etapas 1–2, D03, combate, Mirror, HUD, población y progresión.',
'',completed?'## Recepción completada / reproducción':'## Recepción pendiente / reproducción','',
'Fuera de Play, con Dungeon_Isla_del_Sol cargada y sin cambios pendientes: ROM → Dungeon Ciudad del Sol → Interior - Etapa 3 → Recibir VE01 VE02 VD01 (sin obras). La solicitud GUID Recepcion.pending permite la misma ejecución una sola vez al importar los scripts. No abre, guarda ni reconstruye escenas; no inicia Host ni mueve al Warrior. Si la escena está sucia o hay Play, se informa el bloqueo y no se fuerza la prueba.',
'',
nativeBatch?'La solicitud pendiente fue consumida por la entrada batch del receptor existente. No hace falta un recorrido manual para repetir estas consultas. El bloqueo de la automatización de escritorio no se evitó: se usó la instalación local de Unity únicamente después de confirmar que el Editor estaba cerrado.':native?'La recepción inicial sí se ejecutó desde el menú del Editor en Edit Mode. La prueba del descanso alternativo continúa pendiente hasta disponer de nueva evidencia en DescansoRealB06/Recepcion.json.':'La recepción nativa sigue pendiente de ejecución. No se interpreta la ausencia de errores como aprobación.',
'',
'Después de ejecutarla, correr `node Docs/CiudadDelSol/Interior/Etapa3_20261117/PrepararInforme.mjs` actualiza este dossier exclusivamente desde los resultados guardados.',
'','## Etapa 4 — recomendación condicionada, sin construcción','',
completed?'Con la recepción individual de estas tres conexiones puede planificarse la siguiente auditoría.':'No se cierra Etapa 3 ni se recomienda ejecutar obras de Etapa 4 sin su recepción. Puede prepararse su análisis documental.',
'',
'La siguiente selección lógica para explorar el descenso restante es VS01 (A03→B01, −16→−24) y VS02 (B01→B02, −24→−30), con VS04/VS05 solo como alternativas documentales de los anillos. VS03 entra en D03 y requiere autorización específica por afectar el acceso a la cámara final; no se interviene ahora. La circulación simultánea de cuatro humanos y el último tramo sin telemetría manual de Etapa 1 siguen pendientes.',
'','## Archivos','',
'- Assets/ROM/Editor/CiudadDelSolInteriorStageTwo.cs: reutilización parametrizada del receptor; misma lógica de seguridad.',
'- Assets/ROM/Editor/CiudadDelSolInteriorStageThree.cs y .meta: selección, alternativa existente, menú y solicitud de Etapa 3.',
'- Docs/CiudadDelSol/Interior/Etapa3_20261117/: informe, resumen, integridad, ejes y plano por niveles.',
'- Backups/CiudadDelSol/Interior/Etapa3_20261117/: respaldo completo, compilación, solicitud y resultados nativos cuando estén disponibles.',
'','Sin cambios de escena ni NavMesh, sin commits ni pushes.',''];
await fs.writeFile(path.join(folder,'Informe.md'),lines.join('\n'));
await fs.writeFile(path.join(folder,'Ejes_documentales.json'),JSON.stringify(plans,null,2));
await fs.writeFile(path.join(folder,'Auditoria_previa_seleccionada.json'),JSON.stringify(audit,null,2));
let svg=`<svg xmlns="http://www.w3.org/2000/svg" width="1500" height="1080" viewBox="0 0 1500 1080"><rect width="1500" height="1080" fill="#101925"/><g font-family="Segoe UI,Arial" fill="#e7eff6"><text x="28" y="36" font-size="25">Ciudad del Sol · Etapa 3 · A03 → B06 → D02 → D01</text><text x="28" y="64" font-size="15">${native?approved.length+'/3 recibidas técnicamente':'Recepción nativa pendiente'} · cuatro jugadores reales sin certificar · sin obras</text><text x="28" y="88" font-size="13">Ejes documentales y envolventes existentes. Norte +Z arriba; Este +X derecha. Ámbar: pendiente; verde: recibido.</text>`;
const rooms=[['A03',156,-16,102,24,24],['B06',120,-30,-42,24,12],['D02',99,-34,-9,66,78],['D01',105,-34,63,54,66]];
for(const [panel,level] of [-16,-24,-30,-34].entries()){
 const left=25+panel*370,top=140,k=2.5,X=x=>left+(x-58)*k,Y=z=>top+(127-z)*k;
 svg+=`<rect x="${left-7}" y="110" width="354" height="545" fill="none" stroke="#51667c"/><text x="${left+7}" y="136" font-size="20">Piso Y=${level} m</text>`;
 for(const [id,x,y,z,w,d] of rooms.filter(r=>r[2]===level))svg+=`<rect x="${X(x-w/2)}" y="${Y(z+d/2)}" width="${w*k}" height="${d*k}" fill="#23374d" stroke="#91a4b8" stroke-dasharray="5 4"/><text x="${X(x-w/2)+4}" y="${Y(z+d/2)+17}" font-size="12">${id} · ${w}×${d}</text>`;
 for(const p of plans){const c=native?.connections.find(c=>c.plan.id===p.id),points=c?c.plan.points.map(point):p.points,color=approved.includes(p.id)?'#54ddba':'#ffbd66';
   for(let i=1;i<points.length;i++){const a=points[i-1],b=points[i];if(a[1]!==level&&b[1]!==level)continue;svg+=`<path d="M${X(a[0])} ${Y(a[2])}L${X(b[0])} ${Y(b[2])}" fill="none" stroke="${color}" stroke-width="3" ${a[1]!==b[1]?'stroke-dasharray="6 4"':''}/>`;}
 }
 if(level===-16)svg+=`<circle cx="${X(132)}" cy="${Y(105)}" r="5" fill="#ff7373"/><text x="${X(132)-65}" y="${Y(105)-10}" font-size="12">Pilastra conservada</text><path d="M${X(156)} ${Y(99)}L${X(174)} ${Y(99)}L${X(174)} ${Y(117)}L${X(120)} ${Y(117)}L${X(120)} ${Y(105)}" fill="none" stroke="${completed?'#54ddba':'#75bfff'}" stroke-width="2"/><text x="${left+10}" y="610" font-size="12">${completed?'Rodeo norte recibido':'Azul: rodeo existente candidato'}</text>`;
 if(level===-30)svg+=`<circle cx="${X(120)}" cy="${Y(-42)}" r="4" fill="#ff7373"/><circle cx="${X(123)}" cy="${Y(-45)}" r="4" fill="#75bfff"/><text x="${left+10}" y="610" font-size="11">Rojo: centro documental bloqueado</text><text x="${left+10}" y="624" font-size="11">Azul: descanso real (123,−30,−45)</text>`;
 svg+=`<path d="M${left+10} 626h25" stroke="#e7eff6"/><text x="${left+10}" y="645" font-size="12">10 m · ↑+Z</text>`;
}
svg+=`<text x="28" y="710" font-size="20">Perfil ${completed?'recibido':'documental'} VE01–VE02 · descenso −16 → −34 m</text><text x="28" y="735" font-size="13">Nueve tramos 2/6 existentes; descansos −24 y −30. No es una simulación de movimiento.</text>`;
const profilePlans=plans.filter(p=>p.id!=='VD01').map(p=>native?.connections.find(c=>c.plan.id===p.id)?.plan??p);
let distance=0,profile=[];for(const p of profilePlans)for(const v of p.points){const q=point(v);if(profile.length){const prev=profile.at(-1);distance+=Math.hypot(q[0]-prev.q[0],q[2]-prev.q[2]);}profile.push({q,s:distance});}
const px=s=>70+s/distance*1340,py=y=>770+(-16-y)*11;
for(const y of [-16,-24,-30,-34])svg+=`<path d="M70 ${py(y)}H1410" stroke="#3b5065"/><text x="28" y="${py(y)+4}" font-size="12">${y}</text>`;
svg+=`<polyline points="${profile.map(p=>`${px(p.s)},${py(p.q[1])}`).join(' ')}" fill="none" stroke="${completed?'#54ddba':'#ffbd66'}" stroke-width="3"/><text x="28" y="1025" font-size="14">Cámara final D03, exterior y etapas recibidas protegidos. Ninguna geometría nueva.</text></g></svg>`;
await fs.writeFile(path.join(folder,'Recorrido_por_niveles.svg'),svg);
console.log(JSON.stringify(summary,null,2));
