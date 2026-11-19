import csv,html,json,pathlib
from PIL import Image
w=pathlib.Path(__file__).parent
o=pathlib.Path('Docs/CiudadDelSol/Interior/R21_Construccion_20261118')
d=json.loads((w/'Validacion_recarga.json').read_text(encoding='utf-8'))
assert all(d[k]for k in ('saved','reloaded','a03Preserved','originalCollidersPreserved','navPreserved','gridPreserved','localRoutesClear','regressions','automaticRoutes'))
integrity=json.loads((w/'Integridad_guardado.json').read_text(encoding='utf-8'))
assert not integrity['unexpected'] and not integrity['deleted']
with (o/'Elementos.csv').open('w',encoding='utf-8-sig',newline='')as f:
 c=csv.writer(f,delimiter=';');c.writerow(['Objeto','X','Y','Z','Ancho','Alto','Fondo','Materiales'])
 for x in d['additions']:c.writerow([x['path']]+[round(x['center'][k],5)for k in 'xyz']+[round(x['size'][k],5)for k in 'xyz']+[' | '.join(x['materials'])])
with (o/'Anchuras.csv').open('w',encoding='utf-8-sig',newline='')as f:
 c=csv.writer(f,delimiter=';');c.writerow(['X','Y','Z','Ancho_util_minimo','Cuatro_capsulas_estaticas','Borde_izquierdo_inseguro','Borde_derecho_inseguro'])
 for x in d['widths']:c.writerow([x['center'][k]for k in 'xyz']+[x['usefulWidthLowerBound'],x['fourStaticCapsulesClear'],x['leftUnsupportedUnprotected'],x['rightUnsupportedUnprotected']])
results=[]
keys=['navigable','complete','samples','physicalFailures','sweepFailures','navFailures','missingNav','unsafeEdges','fourLaneFailures','corners']
old=pathlib.Path('Backups/CiudadDelSol/Interior/A03_Construccion_20261118/Regresion_rutas')
for p in sorted((w/'Regresion_rutas').glob('*.json')):
 a=json.loads(p.read_text(encoding='utf-8'));b=json.loads((old/p.name).read_text(encoding='utf-8'));same=all(a[k]==b[k]for k in keys)
 results.append(dict(route=p.name,navigable=a['navigable'],identicalToA03=same,unsafeEdges=a['unsafeEdges'],fourLaneFailures=a['fourLaneFailures']))
assert len(results)==8 and all(x['navigable']and x['identicalToA03']for x in results)
stages=[]
base=w/'Regresiones_Etapas1a7'
for name,path in [
 ('Etapa 1','Etapas1a6/Regresiones/Etapa1'),('Etapa 2','Etapas1a6/Regresiones/Etapa2'),('Etapa 3','Etapas1a6/Regresiones/Etapa3'),('Etapa 4','Etapas1a6/Regresiones/Etapa4'),('Etapa 5','Etapas1a6/Regresiones/Etapa5'),('Etapa 6','Etapas1a6/VS03'),('Etapa 7','VS06'),('Continuidades encadenadas','Encadenadas')]:
 r=json.loads((base/path/'Recepcion.json').read_text(encoding='utf-8'))
 passed=sum(c['forward']['complete']and c['reverse']['complete']and c['forward']['physical']and c['reverse']['physical']for c in r['connections'])
 stages.append(dict(stage=name,count=len(r['connections']),passed=passed))
 assert passed==len(r['connections'])
brief={k:v for k,v in d.items()if k not in ('before','after','additions','localNative','widths')}
brief.update(regresionesPorEtapa=stages,rutasAutomaticas=results,integridad=integrity,anchuras=len(d['widths']),anchuraMinima=min(x['usefulWidthLowerBound']for x in d['widths']))
(o/'Recepcion_resumen.json').write_text(json.dumps(brief,ensure_ascii=False,indent=2),encoding='utf-8')
stageTable='\n'.join(f"| {r['stage']} | {r['passed']}/{r['count']} |"for r in stages)
report=f'''# R21 — Salón de las Ofrendas

Construcción del 18 de noviembre de 2026. **Escena original guardada y recargada en Unity 6000.3.23f1, DX11, Edit Mode.** Se reutilizó el Editor abierto. No se inició Play ni se construyeron otras salas.

## Resultado arquitectónico

Se mantuvo la nave existente de **30 × 42 m**, centro **(117,−16,123)** y planta X=102–132 / Z=102–144. La altura libre central medida antes de instalar el mecanismo llega a 18 m bajo el intradós Y=2; no es una altura uniforme de toda la sala. Se conservaron sus pilares, bóveda, accesos y conexiones.

El foco está desplazado nueve metros al norte del cruce funcional: **(117,132) en planta**. Así la llegada desde A03 y los ejes H12, H13 y VC01 conservan su función. El centro visual lo forman un sello ceremonial integral en el suelo y un instrumento antiguo suspendido, no un pedestal colocado sobre las rutas recibidas.

| Elemento construido | Implantación y función |
|---|---|
| Fábrica de piedra erosionada | Material exclusivo sobre los dos renderizadores de bóveda/pilastras de R21. Textura existente de bloques dañados, sustituyendo la repetición de un atlas arquitectónico impropio. No se modificaron las mallas ni el material compartido. |
| Pavimento antiguo | Referencias de material local en los 35 módulos completos que forman la planta. No se movió ni sustituyó el suelo físico. |
| Gran sello solar | Centro (117,−16,132), diámetro exterior 13 m, anillos incompletos, escala radial y marcas de desgaste. Tratamiento visual adosado al pavimento existente, sin collider ni resalto físico. |
| Mecanismo ceremonial/de contención | Centro de los anillos (117,−8,132); órbitas de radios 3,8 / 3,15 / 2,55 m. El anillo menor tiene una abertura deliberada. Metal oxidado y ejes de unión visibles, sin piezas orbitales flotantes. |
| Carcasa pétrea | Malla visual original de `Stone_Altar_04`, escala 1,8, reutilizada como carcasa tallada. Conserva el prefab original intacto. No se presenta como evidencia de sacrificios. |
| Cuna y placa de apoyo | Vigas metálicas, travesaños y placa visible bajo la carcasa. La cara superior de la placa y la inferior visible de la carcasa coinciden en Y=−10,19. No es una plataforma jugable ni un piso invisible. |
| Suspensiones | Cuatro anclajes en X=114/120 y Z=129/135. Llegan al techo físico medido en Y≈1,985, dentro de la bóveda existente. |
| Gran relieve septentrional | Centro aproximado (117,−3,35,143,43), diámetro 4,3 m: disco solar incompleto, límite concéntrico y escala de medición. Proyectado sobre el muro real; no se construyó una pared para sujetarlo. |
| Corrupción localizada | Una fractura mural de unos 1,75 m y otra menor sobre la carcasa. Incisión oscura y filamento violeta separados para evitar coplanaridad. Sin portal, animación ni luz violeta uniforme. |
| Luz residual | Cuatro spots cálidos locales, sin sombras dinámicas nuevas de esas luces; dos franjas ámbar y una luminaria visual original montada sobre la suspensión. No se modificaron iluminación ambiente, skybox, volúmenes, postprocesado ni cámara jugable. |

Las texturas de piedra, normal y óxido ya existían en el proyecto. Se referencian desde los materiales nuevos; no se duplicaron ni alteraron sus archivos originales. El instrumento y su placa tienen colliders sólidos: **{d['newColliders']} nuevos colliders**, todos por encima de la circulación. La menor cota de un sólido nuevo deja **{d['minimumNewColliderHeight']:.3f} m sobre el suelo**.

Se añadió un `NavMeshModifier` solo a la raíz nueva para que los sólidos decorativos no se clasifiquen como superficies transitables en un futuro Bake. Se mantiene el Surface existente con Physics Colliders; los grabados sin collider no añaden fuentes físicas. **No se horneó ni modificó ningún NavMeshData y no se creó otro NavMeshSurface.** Un Bake futuro deberá volver a validarse.

## Relación con el lore

Se volvió a verificar el SHA-256 del PDF oficial contra la extracción previamente revisada de sus seis páginas. El canon confirma investigación subterránea, mecanismos de contención, expansión de la anomalía, grietas, corrupción, pérdida de control y abandono. Ciudad del Sol fue una víctima tardía de las consecuencias de The Great Release; no su causante.

| Recurso visual | Relación y límite narrativo |
|---|---|
| Sello solar y carcasa ceremonial | Identidad arquitectónica solar. Su asociación concreta con una ceremonia u ofrendas es una interpretación ambiental del nombre de diseño R21. |
| Escalas, órbitas y ejes metálicos | Sugieren investigación y contención, confirmadas como actividades generales de las profundidades. La forma de esta máquina es una propuesta artística, no un artefacto histórico identificado en el PDF. |
| Anillo roto, óxido y desgaste | Sugieren abandono y fracaso. No fijan cuándo, quién ni cómo se rompió esta pieza concreta. |
| Dos fracturas violetas | Remiten a las alteraciones estructurales y corrupción confirmadas. No demuestran que este instrumento siga funcionando. |
| Vacío ceremonial y luz residual | Buscan transmitir ausencia, tragedia y peligro sin añadir víctimas, sangre, cuerpos o relatos escritos. |

**No se afirmaron sacrificios, tortura ni experimentación humana.** Tampoco se inventaron fechas, nombres, sucesos definitivos o una explicación del funcionamiento actual de la anomalía.

## Pruebas reales ejecutadas

- Retícula original: **312/315 muestras despejadas y conectadas**, idénticas después de construir. Las tres muestras obstruidas siguen siendo pilastras existentes en **(131,−16,105), (131,−16,123) y (131,−16,141)**; no se habilitaron artificialmente.
- **12/12 recorridos locales** con caminos NavMesh completos y física satisfactoria, en ambos sentidos. Apoyo, huella, cápsula original del Warrior, barridos y niveles compatibles se comprobaron mediante los validadores existentes.
- **164 mediciones de anchura**, cota inferior comprobada de **6 m** en los ejes muestreados. Las 164 admiten cuatro cápsulas estáticas; cero bordes sin apoyo/protección detectados en esas bandas. No significa que todo el recinto tenga una banda uniforme de seis metros.
- Colliders y transformaciones originales preservados. Los nuevos sólidos están elevados y no invaden el volumen de circulación; la altura libre mínima añadida es ≈4,07 m.
- Regresiones reales en Unity sobre la candidata en memoria, usando las herramientas de las siete etapas:

| Recepción | Rutas con navegación y física bidireccional |
|---|---:|
{stageTable}

- Las comprobaciones densas, controles negativos, bandas físicas y pruebas Humanoid que componen `AutoRegressions` también devolvieron resultado satisfactorio. No se alteraron sus criterios ni tolerancias.
- **8/8 rutas automáticas navegables** A03–P01 y R21–P01, ida/vuelta, Warrior y Humanoid. Cero fallos de apoyo, barrido o navegación. Se compararon con la recepción de A03: esquinas, número de muestras y avisos son idénticos en las ocho.
- **Advertencias conservadas:** esas rutas automáticas mantienen `accepted=False` para sus bandas laterales globales completas. Sus avisos previos de bordes y cuatro carriles no se ocultaron ni se declaran corregidos por esta ambientación.
- Guardado y recarga reales: una sola raíz R21, retícula conservada, misma triangulación cargada y mismo NavMeshData. La inspección visual se hizo con iluminación de escena real y Gizmos desactivados.
- Compilación de Editor por Roslyn e importación de Unity sin errores nuevos; permanecen siete avisos CS0618 de un diagnóstico experimental anterior, ajenos al código de R21.

**No se movieron Warriors ni se simularon cuatro clientes.** La recepción técnica individual y las cuatro cápsulas estáticas no equivalen a una prueba con cuatro jugadores reales. Esa prueba, la cámara original durante gameplay y el rendimiento en Play Mode continúan pendientes. Se conserva también el tramo antiguo sin telemetría manual de la Etapa 1; no se inventó evidencia nueva.

## Revisión visual y correcciones

Se revisaron llegada, aproximación a altura del jugador, ambos flancos, muro norte y detalle de carcasa/apoyos. Se corrigieron reflejos excesivos, uniformidad del metal, la textura de atlas repetida en la fábrica, acentos sin soporte, uniones de los anillos y el asiento de la carcasa. Las fracturas y los relieves se proyectaron sobre superficies físicas coincidentes con sus mallas visibles.

Los solapes de ejes, vigas, placa y carcasa son uniones mecánicas deliberadas; no atraviesan la arquitectura base. No se observaron nuevas piezas flotantes, obstáculos en accesos o huecos de suelo. Los recintos vecinos siguen sin esta ambientación y permanecen visibles a través de las arcadas; no se cerraron ni oscurecieron globalmente para conseguir una captura más dramática.

Las capturas son del **Editor real**, no imágenes conceptuales o composiciones externas. La vista a altura del jugador es una cámara del Editor a esa altura; los detalles altos no representan posiciones accesibles del Warrior. La calidad artística final queda para revisión del usuario.

## Archivos, respaldo e integridad

- Modificado: `Assets/Scenes/Dungeon_Isla_del_Sol.unity`.
- Nuevo: `Assets/ROM/Editor/CiudadDelSolInteriorR21Atmosphere.cs` y meta. Editor exclusivo, reutiliza la recepción existente; no es un sistema runtime.
- Recursos exclusivos: `Assets/ROM/Art/CiudadDelSol/Interior/R21/` y sus metas.
- Informes, capturas, CSV y resumen: esta carpeta.
- Respaldo y pruebas completas: `Backups/CiudadDelSol/Interior/R21_Construccion_20261118/`.

Antes de intervenir se verificaron **81 copias SHA-256**. Después del guardado, los **80 archivos protegidos distintos de la escena** siguen idénticos, incluidos los recursos y el script de A03. La comparación completa del YAML confirma: ningún bloque original eliminado, ningún collider ni transformación original modificado; únicamente cambian referencias de material en **35 instancias de suelo y dos renderizadores propios de R21**, más la referencia a la nueva raíz. Las demás partes de la escena, incluido A03, permanecen idénticas.

Escena de partida: `{d['sceneBefore']}`

Escena guardada: `{d['sceneAfter']}`

NavMeshData conservado: `{d['nav']}`

Los sellos históricos no se reescribieron: esta recepción acredita el nuevo hash. La ambientación está guardada y se carga con la escena al iniciar Play; **no requiere ejecutar un generador**. La herramienta solo atiende solicitudes explícitas de Editor y evita duplicados.

## Pendientes y recomendación

Revisar visualmente R21 antes de extender este lenguaje a R23. Continúan pendientes cuatro jugadores reales, observación de la cámara/combate en Play y medición de rendimiento. No se certifican todas las rutas posibles o todas las superficies por una retícula.

**A03 permanece intacto. R23, cámara final, exterior, población, Mirror, Warrior, controles, combate, salud, XP, progresión, HUD y cartografía no se modificaron. Sin commits, pushes ni staging.**
'''
(o/'Informe.md').write_text(report,encoding='utf-8')
shots=[('01_Llegada.jpg','Llegada al salón'),('02_Aproximacion_jugador.jpg','Aproximación a altura de jugador — cámara del Editor'),('03_Flanco_occidental.jpg','Flanco occidental y suelo libre'),('04_Vista_desde_norte.jpg','Continuidad y salidas desde el norte'),('05_Relieve_y_fractura_mural.jpg','Detalle elevado del muro norte'),('06_Detalle_mecanismo.jpg','Carcasa, pátina y apoyos del mecanismo')]
cards=''.join(f'<figure><img src="Capturas/{n}" alt="{html.escape(t)}"><figcaption>{html.escape(t)}</figcaption></figure>'for n,t in shots)
page='''<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>R21 — Salón de las Ofrendas</title><style>body{margin:0;background:#181b1a;color:#e1dacb;font:17px/1.6 system-ui}main{max-width:1280px;margin:auto;padding:40px 24px}h1{font:44px Georgia,serif;color:#c8a36c;margin:0}a{color:#c8a36c}small{color:#aeb7b4}figure{margin:30px 0;border:1px solid #4d483e;background:#232725}img{display:block;width:100%}figcaption{padding:12px 18px}.status{border-left:3px solid #b88d54;background:#232725;padding:16px}</style><main><small>CIUDAD DEL SOL · FASE 2 · 18 NOV 2026</small><h1>R21 — Salón de las Ofrendas</h1><p>Mecanismo ceremonial suspendido, fábrica erosionada, sello solar y corrupción localizada. Capturas reales del Editor de Unity.</p><p class="status">Guardado y recargado · 12/12 rutas locales · 164 medidas con anchura mínima comprobada de 6 m · Regresiones de las etapas 1–7 satisfactorias · 8/8 rutas automáticas conservadas · A03 intacto · Cuatro jugadores reales pendientes</p><p><a href="Informe.md">Informe completo</a> · <a href="Elementos.csv">Elementos y posiciones</a> · <a href="Anchuras.csv">Anchuras reales</a> · <a href="Recepcion_resumen.json">Recepción</a> · <a href="Capturas/00_Antes.jpg">Vista anterior</a></p>'''+cards+'<p>La máquina es una interpretación ambiental compatible con investigación y contención. No confirma experimentación humana o sacrificios. La aprobación artística queda pendiente de tu revisión.</p></main></html>'
(o/'Indice.html').write_text(page,encoding='utf-8')
for name,_ in shots:
 im=Image.open(o/'Capturas'/name);print(name,im.format,im.size)
print('Dossier generado. Regresiones:',stages)
