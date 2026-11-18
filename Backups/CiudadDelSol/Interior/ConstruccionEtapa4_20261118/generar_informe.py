import csv, hashlib, html, json
from pathlib import Path

root = Path(__file__).resolve().parents[4]
work = root / 'Backups/CiudadDelSol/Interior/ConstruccionEtapa4_20261118'
out = root / 'Docs/CiudadDelSol/Interior/ConstruccionEtapa4_20261118'
b = json.loads((work/'Construccion.json').read_text(encoding='utf-8-sig'))
assert b['saved'] and b['confirmedReload'] and b['regressionsPassed'] and not b['error'], b
out.mkdir(parents=True, exist_ok=True)
def v(p): return f"({p['x']:.3f}, {p['y']:.3f}, {p['z']:.3f})"
def table(name, fields, rows):
    with (out/name).open('w', encoding='utf-8-sig', newline='') as f:
        writer=csv.writer(f); writer.writerow(fields); writer.writerows(rows)
table('Escaleras.csv',['Jerarquia','GlobalId','PosicionAnterior','PosicionActual','EscalaAnterior','EscalaActual','AnchoFisico'],
      [[c['hierarchy'],c['id'],v(c['oldPosition']),v(c['newPosition']),v(c['oldScale']),v(c['newScale']),8] for c in b['changes']])
table('Ampliaciones.csv',['Id','CentroCollider','DimensionesCollider','CotaSuperior','Prefab'],
      [[i+1,v(p['center']),v(p['size']),p['top'],p['prefab']] for i,p in enumerate(b['platforms'])])
table('Protecciones.csv',['Objeto','InicioBorde','FinBorde','PosicionObjeto','Prefab','EspesorCollider','AlturaCollider'],
      [[p['name'],v(p['start']),v(p['end']),v(p['position']),p['prefab'],1,1.5] for p in b['rails']])
receipts=[]
rows=[]
for stage in range(1,5):
    r=json.loads((work/f'Recepcion_recarga/Etapa{stage}/Recepcion.json').read_text(encoding='utf-8-sig'))
    receipts.append(r)
    for c in r['connections']:
        widths=[w['usefulWidthLowerBound'] for w in c['guide']['widths']]
        rows.append([stage,c['plan']['id'],c['technicalIndividual'],c['guide']['samples'],c['guide']['failures'],c['guide']['sweepFailures'],c['guide']['missingNav'],c['forward']['complete'],c['reverse']['complete'],c['forward']['physical'],c['reverse']['physical'],min(widths),c['geometryFourLanes'],c['edgeProtectionClear']])
table('Recepcion.csv',['Etapa','Conexion','TecnicaIndividual','MuestrasGuia','FallosFisicos','BarridosFallidos','SinNavMesh','NavIda','NavVuelta','FisicaIda','FisicaVuelta','AnchoInferiorMuestreado','CuatroCapsulasEstaticas','BordesProtegidos'],rows)
global_rows=[]
for c in receipts[3]['connections']:
    for direction in ('forward','reverse'):
        p=c[direction];t=p['rejectedGlobalTrace']
        global_rows.append([c['plan']['id'],direction,t['insideCorridor'],t['failures'],t['sweepFailures'],t['disconnectedSurfaceSteps'],p['physical']])
table('Caminos_globales_no_seleccionados.csv',['Conexion','Sentido','DentroCorredor','FallosFisicos','BarridosFallidos','ContinuidadLocalNoDemostrada','RutaPorDescansosAprobada'],global_rows)
proof=json.loads((work/'Navegacion_referencia.json').read_text(encoding='utf-8-sig'))
complete=sum(x['complete'] for x in proof['exterior'])
manifest=json.loads((work/'Antes_SHA256.json').read_text(encoding='utf-8-sig'))
checks=[]
for item in manifest:
    p=root/item['path']; sha=hashlib.sha256(p.read_bytes()).hexdigest().upper()
    checks.append(dict(path=item['path'],before=item['sha256'],after=sha,unchanged=sha==item['sha256']))
(out/'SHA256_comparacion.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
changed=[c['path'] for c in checks if not c['unchanged']]
parts=['# Ciudad del Sol — Construcción y recepción de Etapa 4', '',
       '**VS01 y VS02: recepción técnica individual completada en Unity. Cuatro jugadores reales: pendiente.**', '',
       '## Construcción guardada', '',
       f"Se ensancharon {len(b['changes'])} módulos originales de Staircase_01 de 6 a 8 m, conservando ejes y desniveles. Se añadieron {len(b['platforms'])} franjas de piedra físicamente sólidas y {len(b['rails'])} protecciones. No se ejecutaron generadores globales ni se repitió la búsqueda de 40 combinaciones.", '',
       'VS01: cuatro vuelos desde (45, −16, −12) hasta (45, −24, 12), con descanso B01 en (45, −24, 15). VS02: galería oriental, descanso (45, −24, 45) y tres vuelos desde (42, −24, 45) hasta (24, −30, 45).', '',
       'Las franjas se añadieron solamente donde faltaba suelo, sin duplicar pisos coplanares. Se recortaron las ampliaciones que sobresalían sobre el inicio de una pendiente: ese solape generaba un labio brusco. Los pisos originales no se escalaron.', '',
       'Se reutilizaron Platform_Base_01, Railing_01 y Railing_Staircase_01 de Eternal Temple, con sus materiales originales. Las mallas adaptadas son recursos independientes; los prefabs originales permanecen intactos. Cada protección nueva tiene un collider prismático continuo de 1 m de espesor y 1,5 m de altura, embebido 0,10 m. Se conserva el relieve visual ornamental sin utilizarlo como cara irregular de colisión.', '',
       'El ancho físico de los vuelos es 8 m. Las caras interiores de las protecciones dejan aproximadamente 6,10 m entre sí en los tramos rectos. La tabla de recepción informa el límite inferior muestreado del recorrido completo; no debe confundirse con una medición uniforme de toda la dungeon.', '',
       'Coordenadas completas y prefabs: [Escaleras.csv](Escaleras.csv), [Ampliaciones.csv](Ampliaciones.csv) y [Protecciones.csv](Protecciones.csv). Plano: [Plano.svg](Plano.svg).', '',
       '## Pruebas reales', '',
       'Unity 6000.3.23f1, batch/nographics, Edit Mode, PhysX y NavMesh nativos. No se ejecutó Play, Host, movimiento real del Warrior ni cuatro clientes. La cápsula y sus dimensiones proceden del prefab Warrior.', '',
       '| Etapa | Conexiones recibidas después de recargar desde disco |', '|---|---|']
for n,r in enumerate(receipts,1):
    parts.append(f"| {n} | {sum(c['technicalIndividual'] for c in r['connections'])}/{len(r['connections'])}: "+', '.join(c['plan']['id'] for c in r['connections'])+' |')
parts += ['', 'Se verificaron apoyo físico, huella, cápsula, barridos, pendientes/escalones, planta compatible, anchura, bordes y caminos completos de ida y vuelta con raycasts locales. Además se comprobaron ambos flancos de los siete vuelos cada 0,10 m: 854 muestras sin riesgos detectados. Se mantuvieron controles negativos contra obstáculos y plantas incorrectas.', '',
          'La recepción de Etapa 4 exige pasar por los puntos y descansos originales. Cada tramo NavMesh se calcula y después la ruta concatenada se somete íntegramente a los mismos controles físicos y nativos. Los caminos globales más cortos se conservan como evidencia separada: no se declaran aprobados sus atajos fuera del corredor ni sus comprobaciones fallidas. Esta selección explícita solo se activa para esta recepción; no cambia el movimiento de enemigos ni sus decisiones.', '',
          'En concreto, el camino global inverso de VS01 mantiene una comprobación local de continuidad no demostrada cerca de (48,557; −16; −12,257), además de salir del corredor documentado. No se anuló ese rechazo ni se calificó de falso positivo sin prueba. La aprobación corresponde al recorrido completo alternativo por los descansos originales, que sí pasó todas las comprobaciones. Véase Caminos_globales_no_seleccionados.csv. Una prueba real con NavMeshAgent/Warrior sigue siendo necesaria para evaluar la elección autónoma de atajos globales.', '',
          '## Navegación compartida y protección del resto del mapa', '',
          f"Se actualizó una copia del NavMeshData del mismo NavMeshSurface manual, con {b['sources']} fuentes obtenidas mediante la selección original del paquete. No se creó otro Surface, enlace ni suelo ficticio. Parámetros: radio {b['radius']:.2f} m, escalón {b['climb']:.2f} m, vóxel {b['voxel']:.2f} m.", '',
          f"La candidata conservó {b['outsideWitnesses']} testigos proyectados fuera del área de intervención, comprobados a 0,001 m; también se verificó simétricamente que no apareciera navegación nueva fuera del área. Se compararon {len(proof['exterior'])} rutas dirigidas entre las siete anclas exteriores ({complete} completas en la referencia), preservando todas las completas.", '',
          'El área de comparación excluida corresponde al volumen autorizado X=16..68, Y=−33..−13, Z=−20..54. Se contrastaron las firmas de todos los objetos originales no autorizados; no cambiaron. La cámara final, las Etapas 1–3 y la población exterior no se modificaron.', '',
          'La escena conservó exactamente los 59 NavMeshModifier originales. Las 12 comparaciones exteriores que ya no daban camino completo en la referencia no se reinterpretan como aprobadas; se conservaron las 30 completas existentes y la cobertura fuera del sector.', '',
          'Solo después de superar la prevalidación se guardaron la escena y el mismo NavMeshData, conservando sus GUID. Otra ejecución de Unity volvió a cargar ambos recursos, comprobó la huella de la triangulación persistida y repitió las recepciones y la navegación exterior. Resultado: confirmado.', '',
          '## Archivos y respaldo', '',
          'Respaldo completo verificado antes de intervenir: Backups/CiudadDelSol/Interior/ConstruccionEtapa4_20261118/Antes_SHA256.json y archivos originales bajo esa carpeta. El comparativo final está en SHA256_comparacion.json.', '',
          'Archivos respaldados que cambiaron de hash:', '', *['- '+p for p in changed], '',
          'Además: nuevo CiudadDelSolInteriorStageFour.Construction.cs y .meta; mallas de las protecciones y .meta bajo '+b['assetFolder']+'; este dossier. CiudadDelSolInteriorStageTwo.cs añade la inspección de candidata sin guardar y la selección explícita opcional de puntos originales; no cambia tolerancias ni reglas físicas.', '',
          '## Pendientes y límites', '',
          '- Prueba funcional de cuatro jugadores reales, cruces simultáneos y combate: pendiente. Cuatro cápsulas estáticas no equivalen a cuatro clientes.',
          '- Recorrido real del Warrior en las nuevas protecciones y revisión estética/iluminación en el Editor gráfico: pendiente. Esta recepción utilizó PhysX/NavMesh reales, pero sin renderizado.',
          '- El último tramo manual sin telemetría de Etapa 1 continúa sin acreditarse; no fue completado ni simulado por estas pruebas.',
          '- No se certifica toda combinación de saltos intencionados o caída voluntaria: se recibió el recorrido y su protección para circulación ordinaria.',
          '- Compatibilidad geométrica con Humanoid 0 demostrada; no se ejecutaron enemigos ni red durante la recepción.', '',
          'Compilación del Editor: sin errores; Unity recompiló/importó los scripts y ejecutó la recepción. Persisten advertencias CS0618 preexistentes de herramientas ajenas sobre API experimental; esta construcción no crea NavMeshQuery.', '',
          'Para comprobar visualmente: abrir Dungeon_Isla_del_Sol, seleccionar CiudadDelSol_Etapa4_Protecciones_VS01_VS02 y recorrer VS01/VS02. No hace falta hornear ni ejecutar generadores. Se puede continuar con la planificación de la siguiente etapa; no se construyó ninguna zona adicional. Sin commits ni pushes.']
(out/'Informe.md').write_text('\n'.join(parts)+'\n',encoding='utf-8')
# Local plan, physical scale 9 px/m; north +Z points upward.
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="1160" height="900" viewBox="0 0 1160 900">','<rect width="1160" height="900" fill="#151a24"/>','<style>text{fill:#eee;font-family:Arial} .floor{fill:#b29959;stroke:#dcc58a;stroke-width:1}.rail{stroke:#ed7665;stroke-width:4;fill:none}.route{stroke:#65dbe0;stroke-width:3;fill:none}</style>','<text x="25" y="30" font-size="23">Etapa 4 — VS01 / VS02 · arquitectura guardada</text>','<text x="25" y="55" font-size="14">Planta local a escala · norte +Z arriba · cotas en metros · recepción individual; cuatro jugadores pendiente</text>']
def xy(x,z): return (45+(x-16)*10, 820-(z+22)*10)
def line(a,c,cls):
    x,y=xy(a['x'],a['z']);xx,yy=xy(c['x'],c['z']);return f'<path class="{cls}" d="M{x:.2f},{y:.2f} L{xx:.2f},{yy:.2f}"/>'
for p in b['platforms']:
    cx,cz=p['center']['x'],p['center']['z'];sx,sz=p['size']['x'],p['size']['z'];x,y=xy(cx-sx/2,cz+sz/2)
    svg.append(f'<rect class="floor" x="{x:.2f}" y="{y:.2f}" width="{sx*10:.2f}" height="{sz*10:.2f}"/>')
flights=[(45,-16-i*2,-12+i*6,45,-18-i*2,-6+i*6) for i in range(4)]+[(42-i*6,-24-i*2,45,36-i*6,-26-i*2,45) for i in range(3)]
for i,(x,y,z,xx,yy,zz) in enumerate(flights):
    if x==xx: px,py=xy(x-4,max(z,zz));w,h=80,abs(z-zz)*10
    else: px,py=xy(min(x,xx),z+4);w,h=abs(x-xx)*10,80
    svg.append(f'<rect class="floor" x="{px}" y="{py}" width="{w}" height="{h}"/><text x="{px+3}" y="{py+17}" font-size="11">{y} → {yy}</text>')
for p in b['rails']: svg.append(line(p['start'],p['end'],'rail'))
for c in receipts[3]['connections']:
    pts=c['plan']['points']
    for a,bb in zip(pts,pts[1:]):
        if all(16<=p['x']<=70 and -22<=p['z']<=55 for p in (a,bb)):svg.append(line(a,bb,'route'))
svg += ['<text x="665" y="105" font-size="20">Obra y verificación</text>', f'<text x="665" y="140" font-size="16">7 vuelos de 8 m · {len(b["platforms"])} franjas · {len(b["rails"])} protecciones</text>', '<text x="665" y="175" font-size="15">Dorado: vuelos y ampliaciones nuevas</text>','<text x="665" y="205" font-size="15">Coral: bordes protegidos</text>','<text x="665" y="235" font-size="15">Cian: eje del recorrido recibido</text>','<text x="665" y="280" font-size="15">No representa la totalidad de pisos existentes.</text>','<text x="665" y="310" font-size="15">Coordenadas exactas: tablas CSV del dossier.</text>','<text x="665" y="360" font-size="18">Cotas / progresión</text>','<text x="665" y="395" font-size="16">VS01: −16 → −18 → −20 → −22 → −24</text>','<text x="665" y="425" font-size="16">VS02: −24 → −26 → −28 → −30</text>','<text x="665" y="475" font-size="15">Descansos: (45,−24,15) y (45,−24,45)</text>','<text x="665" y="505" font-size="15">Llegada B02: (24,−30,45)</text>','<text x="665" y="555" font-size="15">Protección sólida: 1 m × 1,5 m</text>','<text x="665" y="585" font-size="15">Entre caras interiores: ≈6,10 m en vuelos</text>','<text x="665" y="650" font-size="17">VS01: APROBADA · VS02: APROBADA</text>','<text x="665" y="680" font-size="15">Regresiones: Etapa 1 · 1/1; Etapa 2 · 6/6;</text>','<text x="665" y="705" font-size="15">Etapa 3 · 3/3. Recarga desde disco confirmada.</text>','</svg>']
(out/'Plano.svg').write_text('\n'.join(svg),encoding='utf-8')
print(json.dumps({'output':str(out),'changes':changed,'stairs':len(b['changes']),'platforms':len(b['platforms']),'rails':len(b['rails']),'exteriorComplete':complete,'receipts':[(r['stage'] if 'stage' in r else '',sum(c['technicalIndividual'] for c in r['connections'])) for r in receipts]},ensure_ascii=False))
