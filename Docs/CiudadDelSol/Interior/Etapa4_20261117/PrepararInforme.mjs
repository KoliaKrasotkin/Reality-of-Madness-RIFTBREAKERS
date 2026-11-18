// Documentation from actual native receipts; never edits Unity assets or scenes.
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const folder=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(folder,'../../../..');
const backup=path.join(root,'Backups/CiudadDelSol/Interior/Etapa4_20261117');
const load=async p=>JSON.parse(await fs.readFile(p,'utf8'));
const optional=async p=>fs.readFile(p,'utf8').then(s=>JSON.parse(s),()=>null);
const sha=b=>crypto.createHash('sha256').update(b).digest('hex').toUpperCase();
const original=await load(path.join(backup,'EjesOriginales/Recepcion.json'));
const native=await load(path.join(backup,'Recepcion.json'));
const north=await optional(path.join(backup,'RodeoNorteVS01/Recepcion.json'));
const edges=await optional(path.join(backup,'BordesEscalerasEtapa4.json'));
const proposal=await optional(path.join(backup,'PropuestaBarandas_NavMesh.json'));
const regressions=await Promise.all([1,2,3].map(i=>load(path.join(backup,`RegresionEtapa${i}/Recepcion.json`))));
const expected=[1,6,3];
const healthy=(r,n)=>r&&r.filesUnchanged&&!r.error&&!r.sceneDirty&&!r.playMode&&!r.simulation&&r.negativeControlsRejectObstaclesAndOtherLevels&&r.connections.length===n&&r.connections.every(c=>c.technicalIndividual);
const before=await load(path.join(backup,'Antes_SHA256.json'));
const integrity=await Promise.all(before.map(async p=>({...p,after:sha(await fs.readFile(path.join(root,p.path)))})));
for(const p of integrity)p.unchanged=p.sha256===p.after;
if(integrity.some(p=>!p.unchanged))throw Error('Cambió un archivo protegido: revisar antes de publicar recepción.');
const prereqs=regressions.every((r,i)=>healthy(r,expected[i]));
const approved=prereqs&&native.filesUnchanged&&!native.error?native.connections.filter(c=>c.technicalIndividual).map(c=>c.plan.id):[];
const complete=approved.length===2;
const xyz=p=>`(${[p.x,p.y,p.z].map(v=>Number(v).toFixed(3)).join(', ')})`;
const minWidth=c=>Math.min(...c.guide.widths.map(w=>w.usefulWidthLowerBound));
const candidate=north?.connections[0];
const unsafe=candidate?.guide.widths.filter(w=>w.leftUnsupportedUnprotected||w.rightUnsupportedUnprotected)??[];
const drops=edges?.probes.filter(p=>p.supportBelow&&p.drop>.30&&!p.barrier&&!p.capsuleBlocked)??[];
const selected=native.connections.map(c=>c.plan.id==='VS01'&&!c.technicalIndividual&&candidate?candidate:c);
const summary={generatedUTC:new Date().toISOString(),unity:native.unity,approved,pending:['VS01','VS02'].filter(id=>!approved.includes(id)),complete,
  realPlayersTested:false,allProtectedFilesIntact:true,regressions:regressions.map((r,i)=>({stage:i+1,approved:r.connections.filter(c=>c.technicalIndividual).length,total:r.connections.length,healthy:healthy(r,expected[i])})),
  sourceSHA256:sha(await fs.readFile(path.join(backup,'Recepcion.json'))),edgeSamples:edges?.probes.length??0,unprotectedDropSamples:drops.length,
  proposal:proposal?{persisted:proposal.geometryPersisted,pieces:proposal.draftPositions.length,navFootprintConflicts:proposal.samples.filter(p=>p.navFound&&p.originalCapsuleClear&&p.temporaryGuardCollision).length}:null,
  connections:selected.map(c=>({id:c.plan.id,approved:approved.includes(c.plan.id),start:c.plan.points[0],end:c.plan.points.at(-1),guideSamples:c.guide.samples,
    guideFailures:c.guide.failures,guideSweeps:c.guide.sweepFailures,guideMissingNav:c.guide.missingNav,forward:c.forward.status,reverse:c.reverse.status,
    physicalForward:c.forward.physical,physicalReverse:c.reverse.physical,minWidth:minWidth(c),maxSlope:c.guide.maximumSlope,fourStaticCapsules:c.geometryFourLanes,edges:c.edgeProtectionClear}))};
await fs.writeFile(path.join(folder,'Resumen.json'),JSON.stringify(summary,null,2));
await fs.writeFile(path.join(folder,'Integridad.json'),JSON.stringify(integrity,null,2));
await fs.copyFile(path.join(backup,'Recepcion.json'),path.join(folder,'Recepcion_Unity.json'));
if(edges)await fs.copyFile(path.join(backup,'BordesEscalerasEtapa4.json'),path.join(folder,'Bordes_Unity.json'));
if(proposal)await fs.copyFile(path.join(backup,'PropuestaBarandas_NavMesh.json'),path.join(folder,'PropuestaBarandas_Unity.json'));
const lines=['# Ciudad del Sol — Etapa 4: VS01 y VS02','',
`**Recepción técnica individual: ${approved.length}/2. ${complete?'Completa.':'NO completa.'}** Cuatro jugadores reales: pendiente. No se construyó ni guardó arquitectura para forzar una aprobación.`,
'','## Alcance, fuentes y extremos','',
'Se contrastaron Plan_20261117, Auditoria_20261117, Etapa1_20261117, RecepcionIndividual_20261117 y las recepciones completas de Etapa2_20261117 y Etapa3_20261117. También se leyeron UpperRoute del generador NucleoConjunto y Build/Flight/Ring del generador SolInvertido, sin ejecutarlos.',
'',
'| Conexión | Espacios y extremos de prueba | Cotas | Elementos existentes | Estado |',
'|---|---|---|---|---|',
`| VS01 | A03 (156,−16,99) ↔ B01 (45,−24,15) | −16 → −24 | Acceso superior, galerías G04/G03, descanso y cuatro escaleras 2/6 | ${approved.includes('VS01')?'Recibida':'Pendiente de protección lateral'} |`,
`| VS02 | B01 (45,−24,15) ↔ B02 (24,−30,45) | −24 → −30 | Anillo superior, descanso (45,−24,45) y tres escaleras 2/6 | ${approved.includes('VS02')?'Recibida':'Pendiente'} |`,
'',
'B01 es un anillo de seis metros, envolvente 72×72 m centrada en X/Z=(18,18), a Y=−24, con vacío central de 60×60 m. B02 es un anillo de seis metros, envolvente 60×60 m al mismo centro, a Y=−30, con vacío 48×48 m y apertura de escalera. Los puntos de conexión no son el centro de esos anillos: el centro no es un piso transitable. El plano representa los vacíos y separa los niveles.',
'','## Pruebas realmente ejecutadas','',
`Unity ${native.unity}, batch/nographics, PhysX y NavMesh nativos en Edit Mode. Recepción principal: ${native.started} → ${native.ended}. La instalación se inició solo tras comprobar fuera del sandbox que no existía un Editor de este proyecto. No hubo Play, Host, movimiento del Warrior ni simulación de cuatro personajes.`,
'',
'Se utilizó el mismo motor de CiudadDelSolInteriorStageTwo: SamplePosition local con control de planta, CalculatePath en ambos sentidos, raycasts recíprocos/locales, apoyo y huella, cápsula del prefab Warrior, barridos, pendientes/escalones, anchura y bordes. No se aumentaron tolerancias ni se modificó el motor recibido. Se conservan radio Warrior 0,32 m, altura 2 m, escalón 0,30 m y pendiente 48°; Humanoid 0 conserva radio 0,57 m y escalón 0,36 m.',
'',
...selected.flatMap(c=>[
`### ${c.plan.id} — ${approved.includes(c.plan.id)?'recibida técnicamente':'pendiente'}`,'',
`Recorrido evaluado: ${c.plan.points.map(xyz).join(' → ')}.`,
`Guía: ${c.guide.samples} muestras; apoyo/cápsula/huella fallidos: ${c.guide.failures}; barridos fallidos: ${c.guide.sweepFailures}; sin NavMesh: ${c.guide.missingNav}. Pendiente máxima de apoyo: ${c.guide.maximumSlope.toFixed(2)}°. Anchura útil inferior muestreada: ${minWidth(c).toFixed(2)} m; no es certificación de ancho uniforme.`,
`Ida: ${c.forward.status}, física=${c.forward.physical}, impactos sin resolver=${c.forward.blockedRaycasts}. Regreso: ${c.reverse.status}, física=${c.reverse.physical}, impactos sin resolver=${c.reverse.blockedRaycasts}.`,
`Cuatro cápsulas estáticas despejadas: ${c.geometryFourLanes}. Protección de bordes: ${c.edgeProtectionClear}. No equivale a circulación simultánea real ni evitación dinámica.`,
'']),
'## Obstáculos y causas','',
'El eje documental de VS01 cruza la pilastra de Nave 102/102, recurso Nave_08_2.asset, cerca de (132,−16,105). Unity confirmó siete muestras de cápsula obstruida y un barrido bloqueado. No se eliminó ni recortó esa estructura. El rodeo norte existente de A03 evita la pilastra y conserva el resto de la continuación documentada.',
'',
`En el rodeo, el eje completo y las rutas nativas son físicamente continuos, pero ${unsafe.length} secciones de anchura señalan borde sin protección en las cuatro escaleras centradas en X=45, de Z=−12 a Z=12 y Y=−16 a Y=−24. Los colliders Staircase_01 activos tienen seis metros de ancho; no figuran Railing_Staircase_01 laterales en ese vuelo. Flight del generador crea escaleras y recorridos, pero no esas barandas.`,
'',
edges?`La auditoría lateral adicional ejecutó ${edges.probes.length} sondas nativas en los SIETE módulos: X=41,5 y X=48,5 para VS01; Z=41,5 y Z=48,5 para VS02. ${drops.length} sondas encontraron una caída mayor que el escalón permitido sin barrera horizontal ni bloqueo de cápsula. Ejecución: ${edges.started} → ${edges.ended}. Esto acredita un riesgo real, no solo una imprecisión de la huella.`:'Sondas adicionales pendientes.',
'',
...(edges?['| Conexión / escalera | Lado | Suelo bajo el exterior | Desnivel medido | Barrera | Barrido de cápsula bloqueado |','|---|---|---|---:|---|---|',
...edges.probes.map(p=>`| ${p.connection} / ${p.flight} | ${p.connection==='VS01'?(p.side<0?'Oeste':'Este'):(p.side<0?'Sur':'Norte')} | ${p.supportBelow?xyz(p.firstFloor):'Sin apoyo en 40 m'} | ${p.supportBelow?p.drop.toFixed(3)+' m':'No medido'} | ${p.barrier} | ${p.capsuleBlocked} |`),'']:[]),
'**VS02: aprobación provisional retirada.** El motor heredado aprobó el camino central y el muestreo limitado de anchura. La auditoría lateral reveló una caída de aproximadamente 9,119 m sin barrera en ambos lados del tercer módulo (30,−28,45) → (24,−30,45). EjesOriginales conserva el resultado inicial; la recepción consolidada registra el riesgo, sin ocultar este falso negativo de la comprobación anterior.',
'',
'## Correcciones y propuesta acotada','',
'**Corrección de trazado preparada y comprobada:** usar el rodeo norte existente en VS01, preservando la pilastra. **Obras efectivamente realizadas: ninguna.** No se acepta la conexión mientras falte acreditar su protección lateral.',
'',
'Propuesta acotada: ocho Railing_Staircase_01 en ambos flancos de VS01, adaptadas a 2/6 como en Centro Oriental. Ámbito X≈42…48, Z≈−12…12, Y≈−24…−16. Ejes ensayados X≈42,45 y X≈47,55. En VS02, proteger el tercer módulo X=30→24, Z≈42…48, Y=−28→−30 y sus juntas. Las sondas de los dos primeros módulos encuentran bloqueo de la arquitectura existente; no se proponen demoliciones ni protecciones redundantes sin comprobarlas.',
'',
...(proposal?[`Se ensayaron ${proposal.draftPositions.length} barandas TEMPORALES de VS01 con los colliders del prefab original. No se guardaron y se destruyeron al terminar. ${proposal.samples.filter(p=>p.navFound&&p.originalCapsuleClear&&p.temporaryGuardCollision).length}/${proposal.samples.length} muestras con NavMesh local y cápsula Humanoid 0 originalmente despejada resultaron obstruidas por esas barandas. La escena conservó su SHA-256. Son pruebas reales de una propuesta hipotética en memoria, no arquitectura existente ni una recepción aprobada.`,'']:[]),
'',
'La colocación estándar ensayada NO puede guardarse como solución final manteniendo sin más la huella navegable actual. Hay que resolver una ubicación/espesor y apoyo constructivo compatibles con esa huella, o autorizar un ajuste local de navegación que conserve el exterior. El dato manual actual es compartido y está protegido: no se reemplaza ni rehace. Tampoco se añaden superficies flotantes, colliders invisibles, enlaces o navegación paralela. Se conservan todos los peldaños, descansos, apoyos y arquitectura monumental.',
'',
'Los recorridos comprobados, incluido el camino global, siguen usando esos vuelos. No se ha acreditado otro descenso seguro dentro del alcance VS01–VS02. La siguiente escalera del anillo conduce a VS03/D03 y queda fuera de esta etapa: no se usa la cámara final como rodeo no autorizado.',
'','## Regresiones e integridad','',
...regressions.map((r,i)=>`- Etapa ${i+1}: ${r.connections.filter(c=>c.technicalIndividual).length}/${r.connections.length} en consultas nativas nuevas; error=${r.error||'ninguno'}; escena intacta=${r.filesUnchanged}.`),
'- Los resultados nuevos se guardaron dentro de Etapa4_20261117; no se sobrescribieron los dossiers ni recepciones originales.',
`- Los ${integrity.length} archivos protegidos conservan sus SHA-256, incluidos escena, NavMeshData, población y receptores previos.`,
'- Compilación offline de Editor y compilación real de Unity superadas. Advertencias de API experimental proceden del diagnóstico anterior y no se ejecuta NavMeshQuery experimental.',
'- Se mantiene pendiente la telemetría manual del último regreso de Etapa 1. Estas consultas no completan esa captura.',
'','## Archivos y próximo paso','',
'Complemento nuevo: Assets/ROM/Editor/CiudadDelSolInteriorStageFour.Guards.cs y su .meta. Ensaya barandas únicamente en memoria y solo en batch con la opción explícita; no guarda geometría ni modifica prefabs.',
'',
'Nuevo código: Assets/ROM/Editor/CiudadDelSolInteriorStageFour.cs y su .meta; selección de rutas y diagnóstico suplementario dentro del receptor parcial existente. No se modificaron scripts compartidos de gameplay ni el motor de validación de Etapa 2.',
'',
'Documentación: esta carpeta contiene informe, resumen, evidencia Unity, integridad y plano por niveles. Respaldos, compilación y registros completos: Backups/CiudadDelSol/Interior/Etapa4_20261117/.',
'',
complete?'Las dos conexiones quedan recibidas técnicamente. La siguiente selección requiere autorización propia.':'La Etapa 4 NO se declara completa. Los caminos centrales tienen respaldo físico/NavMesh, pero VS01 y VS02 requieren resolver las protecciones laterales y su compatibilidad con navegación. Las pruebas iniciales se conservan; no hace falta repetir recorridos humanos completos.',
'',
'La próxima zona documentada sería VS03 (B02→D03, −30→−38), pero implica el acceso a la cámara final y necesita autorización específica. VS04/VS05 pueden evaluarse como ramales de exploración en una etapa separada. No se construyen ahora. Cuatro jugadores reales siguen pendientes. Sin commits ni pushes.',''];
await fs.writeFile(path.join(folder,'Informe.md'),lines.join('\n'));
await fs.writeFile(path.join(folder,'Recorridos_evaluados.json'),JSON.stringify(selected.map(c=>c.plan),null,2));
let svg=`<svg xmlns="http://www.w3.org/2000/svg" width="1500" height="1100" viewBox="0 0 1500 1100"><rect width="1500" height="1100" fill="#101925"/><g font-family="Segoe UI,Arial" fill="#e7eff6"><text x="26" y="35" font-size="25">Ciudad del Sol · Etapa 4 · VS01 / VS02</text><text x="26" y="62" font-size="15">${approved.length}/2 recibidas técnicamente · verde: recibida · ámbar: pendiente · rojo: riesgo / vacío · sin obras</text><text x="26" y="85" font-size="13">Norte +Z arriba; Este +X derecha. Anillos a escala y vacíos reales; no representan salas con piso completo.</text>`;
for(const [panel,level] of [-16,-24,-30].entries()){
 const left=25+panel*495,k=1.9,X=x=>left+(x+25)*k,Y=z=>155+(125-z)*k;
 svg+=`<rect x="${left-6}" y="108" width="474" height="520" fill="none" stroke="#50677d"/><text x="${left+9}" y="135" font-size="21">Piso Y=${level}</text>`;
 if(level!==-16){const min=level===-24?-18:-12,max=level===-24?54:48,a=min+6,b=max-6;
   svg+=`<path d="M${X(min)} ${Y(max)}H${X(max)}V${Y(min)}H${X(min)}Z M${X(a)} ${Y(b)}H${X(b)}V${Y(a)}H${X(a)}Z" fill="#29415b" fill-rule="evenodd" stroke="#8aa4bb"/><text x="${X(18)-30}" y="${Y(18)}" font-size="13">VACÍO</text><text x="${X(min)}" y="${Y(min)+20}" font-size="12">${level===-24?'B01 · 72×72 · anillo 6 m':'B02 · 60×60 · anillo 6 m'}</text>`;
 }
 if(level===-16)svg+=`<rect x="${X(144)}" y="${Y(114)}" width="${24*k}" height="${24*k}" fill="#29415b" stroke="#8aa4bb"/><text x="${X(144)}" y="${Y(114)-7}" font-size="12">A03</text><circle cx="${X(132)}" cy="${Y(105)}" r="4" fill="#ff7878"/><text x="${X(132)-20}" y="${Y(105)-9}" font-size="12">Pilastra conservada</text>`;
 for(const c of selected){const color=approved.includes(c.plan.id)?'#54ddba':'#ffbc66';for(let i=1;i<c.plan.points.length;i++){const a=c.plan.points[i-1],b=c.plan.points[i];if(a.y!==level&&b.y!==level)continue;svg+=`<path d="M${X(a.x)} ${Y(a.z)}L${X(b.x)} ${Y(b.z)}" fill="none" stroke="${color}" stroke-width="3" ${a.y!==b.y?'stroke-dasharray="6 4"':''}/>`;}}
 if(level===-16||level===-24)svg+=`<rect x="${X(42)}" y="${Y(12)}" width="${6*k}" height="${24*k}" fill="none" stroke="#ff7878" stroke-width="2"/><text x="${left+10}" y="573" font-size="12">Rojo: cuatro módulos sin protección acreditada</text>`;
 if(level===-30)svg+=`<rect x="${X(24)}" y="${Y(48)}" width="${6*k}" height="${6*k}" fill="none" stroke="#ff7878" stroke-width="2"/><rect x="${X(-12)}" y="${Y(36)}" width="${6*k}" height="${24*k}" fill="#101925" stroke="#ff7878" stroke-dasharray="4 4"/><text x="${left+10}" y="553" font-size="12">Apertura occidental VS03: fuera de alcance</text><text x="${left+10}" y="573" font-size="12">Rojo: último módulo VS02 sin protección</text>`;
 svg+=`<path d="M${left+10} 596h${10*k}" stroke="#e7eff6"/><text x="${left+10}" y="618" font-size="12">10 m · ↑+Z</text>`;
}
svg+=`<text x="26" y="680" font-size="20">Perfil del recorrido evaluado · −16 → −24 → −30 m</text><text x="26" y="708" font-size="13">VS01: rodeo norte físicamente continuo; pendiente de bordes. VS02: tres tramos recibidos. No es prueba de cuatro humanos.</text>`;
let distance=0,profile=[];for(const c of selected)for(const q of c.plan.points){if(profile.length){const p=profile.at(-1);distance+=Math.hypot(q.x-p.q.x,q.z-p.q.z);}profile.push({q,s:distance,id:c.plan.id});}
const px=s=>65+s/distance*1380,py=y=>750+(-16-y)*17;
for(const y of [-16,-24,-30])svg+=`<path d="M65 ${py(y)}H1445" stroke="#3a5067"/><text x="18" y="${py(y)+5}" font-size="12">${y}</text>`;
for(let i=1;i<profile.length;i++){const a=profile[i-1],b=profile[i];svg+=`<path d="M${px(a.s)} ${py(a.q.y)}L${px(b.s)} ${py(b.q.y)}" stroke="${approved.includes(b.id)?'#54ddba':'#ffbc66'}" stroke-width="3"/>`;}
svg+=`<text x="26" y="1045" font-size="14">D03 y su cámara final fuera del alcance. Arquitectura, NavMeshData y etapas anteriores conservados.</text></g></svg>`;
await fs.writeFile(path.join(folder,'Recorrido_por_niveles.svg'),svg);
console.log(JSON.stringify(summary,null,2));
