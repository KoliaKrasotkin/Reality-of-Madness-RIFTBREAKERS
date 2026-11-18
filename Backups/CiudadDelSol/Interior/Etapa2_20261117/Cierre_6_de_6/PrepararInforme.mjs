// Rebuilds documentation from actual Unity evidence. Does not edit Assets or scenes.
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import crypto from 'node:crypto';
const folder=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(folder,'../../../..');
const backup=path.join(root,'Backups/CiudadDelSol/Interior/Etapa2_20261117');
const source=path.join(backup,'Recepcion.json');
const report=JSON.parse(await fs.readFile(source,'utf8'));
const sha=b=>crypto.createHash('sha256').update(b).digest('hex');
const n=v=>Number(v).toFixed(2).replace('.',',');
const xyz=p=>`(${n(p.x)}, ${n(p.y)}, ${n(p.z)})`;
const connections=report.connections;
const passed=connections.filter(c=>c.technicalIndividual);
const pending=connections.filter(c=>!c.technicalIndividual);
const before=JSON.parse(await fs.readFile(path.join(backup,'Antes_SHA256.json'),'utf8'));
const integrity=[];
for(const item of before){const actual=sha(await fs.readFile(path.join(root,item.path)));integrity.push({...item,after:actual,unchanged:actual.toUpperCase()===item.sha256.toUpperCase()});}
if(integrity.some(p=>!p.unchanged))throw Error('Un archivo protegido cambió: no se publica aprobación.');
const minWidth=c=>Math.min(...c.guide.widths.map(w=>w.usefulWidthLowerBound));
const summary={generatedUTC:new Date().toISOString(),source,sourceSHA256:sha(await fs.readFile(source)),unity:report.unity,started:report.started,ended:report.ended,
  approved:passed.map(c=>c.plan.id),pending:pending.map(c=>c.plan.id),allFilesIntact:integrity.every(p=>p.unchanged),nativeError:report.error,
  noPlay:!report.playMode,negativeControls:report.negativeControlsRejectObstaclesAndOtherLevels,realPlayersTested:false,
  connections:connections.map(c=>({id:c.plan.id,from:c.plan.from,to:c.plan.to,approved:c.technicalIndividual,
    minWidthLowerBound:minWidth(c),fourStaticCapsules:c.geometryFourLanes,edges:c.edgeProtectionClear,
    forward:c.forward.status,reverse:c.reverse.status,viaOriginalWaypoints:c.forward.viaOriginalWaypoints||c.reverse.viaOriginalWaypoints,
    problems:c.guide.problems,forwardProblems:c.forward.trace?.problems,reverseProblems:c.reverse.trace?.problems}))};
await fs.writeFile(path.join(folder,'Resumen.json'),JSON.stringify(summary,null,2));
await fs.writeFile(path.join(folder,'Integridad.json'),JSON.stringify(integrity,null,2));
await fs.copyFile(source,path.join(folder,'Recepcion_Unity.json'));
const lines=[
'# Ciudad del Sol — Etapa 2: conexiones del distribuidor y primer descenso', '',
`Resultado real de Unity: **${passed.length}/6 conexiones con recepción técnica individual; ${pending.length} pendientes**. No es una certificación de cuatro jugadores.`, '',
'## Auditoría del trabajo interrumpido', '',
'Se conservaron los seis IDs seleccionados y la herramienta existente CiudadDelSolInteriorStageTwo.cs. La primera intervención no quedó vacía: ya había compilación y recepción real de las seis conexiones en Unity (17/11/2026, 19:54 UTC), con H06, H13 y H14 aprobadas. Faltaban resolver el diagnóstico de H12, el perfil de las escaleras de VC01–VC02, documentación, plano y comprobaciones de regresión. No se reinició el trabajo ni se creó otro receptor.', '',
'Se contrastaron los 73 enlaces originales y los dossiers Plan_20261117, Auditoria_20261117, Etapa1_20261117 y RecepcionIndividual_20261117. La clasificación de los 73 se conserva en Backups/.../Etapa2_20261117/Revision_73_conexiones.json; 67 enlaces quedan fuera de esta intervención. Se preservan VS y la cámara final D03.', '',
'## Conexiones y coordenadas', '',
'| Código | Enlace | Piso inicial/final | Estado técnico | Anchura útil inferior muestreada | Cuatro cápsulas estáticas |',
'| --- | --- | --- | --- | --- | --- |',
...connections.map(c=>`| ${c.plan.id} | ${c.plan.from} ↔ ${c.plan.to} | ${c.plan.points[0].y} → ${c.plan.points.at(-1).y} m | ${c.technicalIndividual?'Recibida':'Pendiente'} | ${n(minWidth(c))} m | ${c.geometryFourLanes?'Despeje geométrico':'Pendiente'} |`), '',
'La anchura es una cota inferior muestreada, limitada a seis metros en el buscador; no afirma seis metros continuos ni incluye maniobras/cámaras de cuatro humanos. Las cuatro consultas transversales usan las dimensiones originales del Warrior, sin crear jugadores ni NPC.', '',
...connections.flatMap(c=>[`### ${c.plan.id}`, '',c.plan.points.map(xyz).join(' → '), '',
`NavMesh: ${c.forward.status} ida y ${c.reverse.status} vuelta. Física nativa: ${c.forward.physical?'satisfactoria':'pendiente'} / ${c.reverse.physical?'satisfactoria':'pendiente'}. Guardas de borde: ${c.edgeProtectionClear?'comprobadas en las secciones inspeccionadas':'pendientes'}.`, '',
`Muestras del eje: ${c.guide.samples}; de los caminos nativos: ${c.forward.trace?.samples??0} / ${c.reverse.trace?.samples??0}. Pendiente máxima del apoyo en el eje: ${n(c.guide.maximumSlope)}°.`, '',
((c.forward.viaOriginalWaypoints||c.reverse.viaOriginalWaypoints)?'El camino global más corto que recortaba un giro quedó rechazado y archivado en rejectedGlobalTrace. Se comprobó otra ruta NavMesh completa, físicamente inspeccionada, que pasa por los puntos originales. No se amplió el corredor ni se cambió la geometría.':'Se conservaron los ejes y la arquitectura existentes.'), '']),
'VC01 conserva su código del dossier A03→B05; esta etapa recibe solo su continuación R21→B05. No repite ni modifica el tramo A03→R21 ya recibido. Los puntos intermedios de los nueve tramos 2/6 proceden del generador CentroOriental existente y se contrastan con sus colliders reales; no son escaleras nuevas.', '',
'## Causas raíz y correcciones del receptor', '',
'1. **Esquinas exactas del NavMesh.** H12 tenía un raycast de retorno detenido en su propio origen, aunque el sentido recíproco y los caminos locales eran válidos. Ahora se conservan los impactos originales y solo se resuelven esquinas/aristas verificadas, con caminos locales rectos completos en ambos sentidos y raycasts recíprocos. La igualdad numérica sigue siendo 1 mm; no es holgura del agente ni un hueco permitido. Si ambos sentidos impactan, se requieren además testigos interiores dentro de ese mismo milímetro y ausencia de enlaces activos. No se utiliza NavMeshQuery experimental.', '',
'2. **Altura del camino entre esquinas.** La interpolación lineal de Y de esquinas lejanas no describe todos los cambios de altura del suelo. La guía física tenía NavMesh en todas sus muestras, mientras el interpolador antiguo declaraba ausencias. Entre esquinas se utiliza el perfil físico seleccionado, se conserva la altura real de las propias esquinas, se exige apoyo local y se vuelve a proyectar sobre NavMesh con control de nivel. Cada paso local acredita continuidad; no se acepta proyectar hacia otra planta.', '',
'3. **Cápsula en juntas rampa/descanso.** La cara bajo el rayo central puede ser horizontal aunque la semiesfera inferior esté apoyándose sobre la rampa adyacente. Se calcula la envolvente del apoyo real de esa semiesfera y se conservan los barridos completos, subdividiendo la trayectoria si debe seguir una junta curva. No se mueven objetos ni se altera radio, altura, skin, pendiente o stepOffset del Warrior.', '',
'4. **Suelo oculto bajo la rampa.** El receptor podía elegir el basamento inferior por cercanía a la altura esperada. Se elige la primera superficie física apta del rayo en la ventana de altura local, sin atravesar una rampa para usar el piso bajo ella. Se mantiene el límite real de escalón y se comprueban todos los obstáculos de la cápsula.', '',
'5. **Clasificación lateral de las escaleras.** El antiguo criterio cercano al eje dejaba fuera muestras laterales que aún estaban sobre el collider de la escalera. La clasificación usa sus bounds reales activos, ampliados solo por huella/escalón, y conserva el nivel local. Los otros pisos siguen con su tolerancia de 3 cm.', '',
'6. **Corredor de VC02.** El camino global directo pasaba por (92,888; -34; 45,003), unos milímetros fuera del corredor documental. Las comprobaciones físicas y de continuidad local no detectaron un corte en ese camino; el rechazo corresponde al alcance documental. Se conserva ese rechazo. Está preparada y compilada la comprobación por todos los puntos originales, sin ampliar el límite; su recepción nativa sigue pendiente mientras no aparezca una nueva ejecución en la evidencia.', '',
'7. **Datos no finitos.** SamplePosition/Raycast sin resultado pueden devolver coordenadas infinitas. El JSON usa una posición vacía únicamente cuando su indicador de validez es falso; nunca se transforma una consulta fallida en una posición acreditada. Los informes intermedios se conservan.', '',
'## Arquitectura existente y elementos conservados', '',
`El inventario del ámbito contiene ${report.colliders.filter(c=>c.enabled&&c.active).length} colliders activos y ${report.colliders.filter(c=>!c.enabled||!c.active).length} de objetos inactivos. Se verificaron los nueve Staircase_01 del Centro Oriental y sus terrazas/descansos, pisos modulares y protecciones.`, '',
'Las ocho columnas detectadas como inactivas ya estaban así en la escena: cuatro en el Ala Norte y cuatro en el Ala Este. No se reactivaron ni se desactivó ningún collider para obtener un resultado favorable. Sus posiciones, tipos, jerarquías, bounds y GlobalObjectId se conservan en Recepcion_Unity.json.', '',
'**Correcciones arquitectónicas efectivamente construidas: ninguna.** No se demostró que falten pisos, escaleras o descansos en estos seis enlaces; por tanto se conservan las naves, columnas, barandas y la monumentalidad del conjunto. Corregir un error de medición no justifica una demolición o un suelo auxiliar.', '',
'## Validaciones realmente ejecutadas', '',
`Unity ${report.unity}, ejecución final desde ${report.started} hasta ${report.ended}. Play Mode: ${report.playMode}. Error: ${report.error||'ninguno'}. Archivo de escena intacto: ${report.filesUnchanged}.`, '',
'- Raycasts de apoyo, huella completa muestreada, overlap de cápsula y barridos de los seis ejes y de las rutas calculadas, en ambos sentidos.',
'- Pendientes y desniveles frente a la cápsula real: radio 0,32 m, altura 2 m, escalón 0,30 m, slopeLimit 48°. Humanoid 0 conserva radio 0,57 m, climb 0,36 m y sus parámetros de proyecto.',
'- Medición de anchura, cuatro cápsulas estáticas separadas y búsqueda de apoyo lateral. Una rampa transitable o un escalón bajo no se cuentan como protección: la guarda exige una barrera no caminable por encima del stepOffset.',
'- CalculatePath, SamplePosition, FindClosestEdge y Raycast nativos, con comprobación física y continuidad local. Los caminos parciales siguen rechazados.',
`- Controles negativos (pilastra, planta equivocada y vacío sobre D01): ${report.negativeControlsRejectObstaclesAndOtherLevels?'rechazados correctamente':'NO superados; recepción sin aprobación'}.`,
'- Compilación Editor offline y compilación real de Unity; no se cambiaron ensamblados Runtime ni sistemas compartidos.', '',
'No hubo simulación de movimiento, CharacterControllers añadidos, Warriors instanciados ni cuatro conexiones reales. No se ejecutó Host ni se cambiaron sus límites. Las advertencias de API experimental corresponden al diagnóstico preexistente, que no se invocó.', '',
'## Estado y siguiente paso', '',
pending.length?`Siguen pendientes: **${pending.map(c=>c.plan.id).join(', ')}**. No se declara terminada la Etapa 2. El código preparado debe recibir una nueva ejecución real antes de aprobar esos enlaces.`:'**Las seis conexiones superaron la recepción técnica individual.** La Etapa 2 queda recibida con ese alcance; no es una certificación funcional de cuatro jugadores ni del resto de la dungeon.', '',
'La automatización dejó de poder acceder a la ventana del Editor abierto. El usuario indicó que tampoco puede acceder ahora. No se cerró el proceso ni se abrió otra instancia del proyecto. Queda una solicitud explícita de recepción de solo lectura en Recepcion.pending: podrá procesarse al importar los scripts, fuera de Play y con la escena limpia. La última revisión del código compila offline; no se atribuye a Unity la ejecución de esa revisión hasta obtener su informe nuevo. No hace falta repetir el recorrido manual del Warrior.', '',
'Las 40 comprobaciones de regresión de la evidencia guardada pasaron, incluyendo controles negativos, conservación de las conexiones aprobadas e integridad de diez archivos protegidos. Son comprobaciones sobre resultados nativos existentes; no equivalen a una ejecución nueva de VC02 ni a movimiento real de jugadores.', '',
'Se conservan los pendientes de la Etapa 1: último tramo sin telemetría manual y cuatro jugadores. No se completó ni reinterpretó aquella captura.', '',
'Para Etapa 3 se recomienda revisar VE01–VE02 y VD01: acceso Sureste, D02 y el lazo inferior hacia D01, aprovechando las estructuras existentes. No se autoriza implícitamente intervenir D03, el Sol Invertido ni los enlaces finales VS. La construcción de la siguiente etapa requiere su aprobación específica.', '',
'## Archivos y reproducción', '',
'Único archivo de código modificado durante esta reanudación: Assets/ROM/Editor/CiudadDelSolInteriorStageTwo.cs. Se conservaron su .meta y las herramientas previas. Se agregaron este informe, el plano y resultados derivados; respaldos bajo Backups/CiudadDelSol/Interior/Etapa2_20261117/Reanudacion/.', '',
'La escena, NavMeshData, población de 48 puestos y los cuatro scripts de la Etapa 1 conservan sus SHA-256. No hubo guardado de escena, horneado, commit ni push.', '',
'Menú de recepción existente: ROM → Dungeon Ciudad del Sol → Interior - Etapa 2 → Recibir conexiones seleccionadas (sin obras). Funciona fuera de Play, con la escena guardada cargada; no inicia el juego ni construye automáticamente. No hace falta ejecutar recorridos manuales punto por punto para repetir esta recepción.', '',
'El plano SVG muestra envolventes existentes y ejes; no representa estructuras nuevas. Los niveles están separados para no confundir pasos superpuestos.', ''
];
await fs.writeFile(path.join(folder,'Informe.md'),lines.join('\n'));

const E=s=>String(s).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');
const rooms=[['R21',117,-16,123,30,42],['R23',123,-16,165,18,18],['G11',105,-16,165,18,30],['G05',75,-16,120,18,36],['R34',57,-16,117,18,78],['B05',84,-24,90,12,12],['D01',105,-34,63,54,66]];
let svg=`<svg xmlns="http://www.w3.org/2000/svg" width="1500" height="1120" viewBox="0 0 1500 1120"><rect width="1500" height="1120" fill="#101925"/><g font-family="Segoe UI,Arial" fill="#e8eef5"><text x="32" y="35" font-size="25">Ciudad del Sol · Etapa 2 · conexiones existentes</text><text x="32" y="63" font-size="15">${passed.length}/6 recibidas técnicamente · verde: recibida · ámbar: pendiente · cuatro jugadores reales sin certificar</text><text x="32" y="86" font-size="13">Norte +Z hacia arriba; este +X a la derecha. Envolventes existentes a escala; líneas de circulación, no obras nuevas.</text>`;
const levels=[-16,-24,-30,-34];
for(let panel=0;panel<levels.length;panel++){
  const level=levels[panel],left=32+panel*365,top=140,k=3.5;
  const X=x=>left+(x-44)*k,Y=z=>top+(180-z)*k;
  svg+=`<rect x="${left-8}" y="108" width="349" height="610" fill="none" stroke="#496074"/><text x="${left+7}" y="133" font-size="20">Piso Y = ${level} m</text>`;
  for(const [id,x,y,z,w,d] of rooms.filter(a=>a[2]===level)){
    svg+=`<rect x="${X(x-w/2)}" y="${Y(z+d/2)}" width="${w*k}" height="${d*k}" fill="#23364d" stroke="#7896b2" stroke-dasharray="5 4"/><text x="${X(x-w/2)+5}" y="${Y(z+d/2)+18}" font-size="13">${id} · ${w}×${d}</text>`;
  }
  if(level===-30)svg+=`<rect x="${X(78)}" y="${Y(66)}" width="${6*k}" height="${6*k}" fill="#23364d" stroke="#7896b2"/><text x="${X(86)}" y="${Y(63)}" font-size="12">Descanso 6×6</text>`;
  for(const c of connections){
    const color=c.technicalIndividual?'#57dfbf':'#ffbd66';
    for(let i=1;i<c.plan.points.length;i++){
      const a=c.plan.points[i-1],b=c.plan.points[i];
      if(Math.abs(a.y-level)>.01&&Math.abs(b.y-level)>.01)continue;
      const graded=Math.abs(a.y-b.y)>.01;
      svg+=`<path d="M${X(a.x)} ${Y(a.z)} L${X(b.x)} ${Y(b.z)}" fill="none" stroke="${color}" stroke-width="3" ${graded?'stroke-dasharray="6 4"':''}/>`;
    }
    const here=c.plan.points.filter(p=>Math.abs(p.y-level)<.01);
    if(here.length){const p=here[Math.floor(here.length/2)];svg+=`<text x="${X(p.x)+5}" y="${Y(p.z)-7}" font-size="12" fill="${color}">${E(c.plan.id)}</text>`;}
  }
  svg+=`<path d="M${left+15} 686h35m-35-4v8m35-8v8" stroke="#e8eef5"/><text x="${left+16}" y="706" font-size="12">10 m</text><text x="${left+285}" y="158" font-size="13">↑ +Z</text>`;
}
svg+=`<text x="32" y="755" font-size="20">Corte longitudinal VC01–VC02 · desnivel existente −16 → −34 m</text><text x="32" y="781" font-size="13">Línea por puntos documentados; escaleras 2/6 existentes. No es prueba de movimiento de personajes.</text>`;
const descent=connections.filter(c=>c.plan.id==='VC01'||c.plan.id==='VC02');let points=[],s=0;
for(const c of descent)for(const p of c.plan.points){if(points.length){const q=points.at(-1);s+=Math.hypot(p.x-q.x,p.z-q.z);}points.push({...p,s});}
const px=v=>55+v/s*1380,py=y=>815+(-16-y)*11;
for(const y of [-16,-24,-30,-34])svg+=`<path d="M55 ${py(y)}H1435" stroke="#34485b"/><text x="15" y="${py(y)+5}" font-size="12">${y}</text>`;
svg+=`<polyline points="${points.map(p=>`${px(p.s)},${py(p.y)}`).join(' ')}" fill="none" stroke="${descent.every(c=>c.technicalIndividual)?'#57dfbf':'#ffbd66'}" stroke-width="3"/>`;
svg+=`<text x="55" y="1040" font-size="13">R21</text><text x="${px(points.find(p=>p.y===-24).s)}" y="1040" font-size="13">B05 · −24</text><text x="${px(points.find(p=>p.y===-30).s)}" y="1040" font-size="13">Descanso · −30</text><text x="1320" y="1040" font-size="13">D01 · −34</text><text x="32" y="1080" font-size="14">Etapa 1, exterior y cámara final protegidos. Sin guardado de escena ni modificación de NavMeshData.</text></g></svg>`;
await fs.writeFile(path.join(folder,'Recorrido_por_niveles.svg'),svg);
console.log(JSON.stringify({approved:summary.approved,pending:summary.pending,output:folder}));
