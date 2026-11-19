from pathlib import Path
import json, csv, collections, statistics, math, html, base64

OUT = Path(__file__).parent
ROOT = OUT.parents[3]
PLAN = ROOT / 'Docs/CiudadDelSol/Interior/Plan_20261117'
rooms = json.loads((PLAN / 'Salas.json').read_text(encoding='utf-8-sig'))
live = json.loads((OUT / 'Unity_68.json').read_text(encoding='utf-8'))
byid = {r['id']: r for r in rooms}
lb = {r['id']: r for r in live}
PHASES = [
 ('S01', 'Llegada y Salón de las Ofrendas', 'A01 A02 A03 R21 R23', 'Piedra dorada cálida, frisos solares y orientación.', 'Lote inicial A03–R21–R23; después rematar A01–A02 sin estrechar el ingreso.', 'A03–R21 y H12 recibidas; los bolsillos de navegación no se consideran zonas de combate.'),
 ('S02', 'Procesión de la Aurora', 'G05 R34 G13 R16 R20', 'Portadas altas y luz rasante; tramos visuales sin tabiques nuevos.', 'Dar ritmo a la nave larga y ordenar las bifurcaciones.', 'S01; H13–H14 recibidas. H15–H18 requieren recepción de su trazado concreto.'),
 ('S03', 'Claustro ceremonial', 'R11 R17 G12 M01 R33', 'Disco del eclipse en pavimento, oro antiguo y acentos violetas.', 'M01 ceremonial y R33 relicario lateral; conservar salidas cruzadas.', 'S02; H17, H19–H20 y H35–H36 no se presumen acreditadas.'),
 ('S04', 'Ala del Sol Cautivo', 'G11 R26 G03 R30 G04 G01 M02 G02 R31 R27', 'Corona solar, piedra erosionada y corrupción violeta localizada.', 'Alternar procesión, templo y cámaras de descanso; no duplicar la arena final.', 'Desde R23 por H06 recibida. Resto de enlaces del ala pendiente de recepción específica.'),
 ('S05', 'Archivos y relicarios occidentales', 'G14 R01 G08 G15 G07 R09 R08', 'Tablillas, depósitos de piedra y luz cálida de lectura.', 'Exploración perimetral y relicarios sin cerrar el tránsito.', 'S03 por H21; H21–H27 pendientes de recepción física de itinerario.'),
 ('S06', 'Jardines interiores', 'R05 G10 R18 R15 R14 R12 R13 M03 R04 R02', 'Jardín mineral, relieves vegetales y oro envejecido.', 'M03 como nave de exploración, R05 ceremonial y R12 relicario; sin nuevo estanque.', 'S05 por H28. H28–H34, H37–H38 y H41 siguen sin recepción específica.'),
 ('S07', 'Solsticios y ala suroeste', 'R03 R07 R19 R06 G06 R10', 'Grandes procesiones, braseros laterales y vacío monumental.', 'R19 como gran salón; reservar el centro, no llenarlo de obstáculos.', 'S06 por R02; H39–H45 requieren comprobar sus accesos exactos.'),
 ('S08', 'Santuario Profano interior', 'R25 G16 R32 R28 M04 G09 R29 R24 R22', 'Votos rotos, incensarios y focos violetas.', 'M04 longitudinal; R32/R22 procesionales y R29 de recuperación.', 'Desde A03. H46–H54 son enlaces de diseño; no se aprueban sólo por el camino global a una semilla.'),
 ('S09', 'Cámaras profundas Oriental y Sureste', 'D01 D02 B05 B06', 'Arcadas iluminadas, terrazas legibles y escala monumental.', 'Ambientar los recintos existentes y estudiar sus vacíos sin rellenarlos por defecto.', 'VC01–VC02, VE01–VE02 y VD01 recibidas; no certifican toda D01/D02.'),
 ('S10', 'Galerías y puentes: conservación', 'B01 B02 B03 B04', 'Conservar piedra, protecciones y vistas verticales.', 'No construir ahora. Cualquier ambientación bajo D03 exige alcance posterior explícito.', 'VS01–VS05 recibidas. Los vacíos centrales de los anillos son deliberados.'),
 ('RES', 'Ámbitos conservados o reservados', 'A00 D03 P01', 'Sin intervención en esta Fase 2.', 'A00 es referencia exterior; D03 y P01 quedan fuera de construcción.', 'Cámara final protegida. P01 es subzona propuesta, no una sala adicional.')]
phase_of = {id: p[0] for p in PHASES for id in p[2].split()}
assert len(phase_of) == 68 and set(phase_of) == set(byid)
ALIASES = dict(zip([f'R{i:02}' for i in range(1,35)], [
 'Cámara del Retorno','Cámara del Polen Solar','Cámara del Ocaso','Antesala de los Brotes','Salón de la Germinación','Procesión del Alba Extinta','Sala de los Solsticios','Refugio de los Custodios','Relicario del Primer Rayo','Puesto del Último Vigía','Procesión de los Siete Astros','Relicario del Eclipse','Refugio del Disco Partido','Cámara del Horizonte','Cámara de las Raíces de Piedra','Cámara del Tránsito Estelar','Sala de las Inscripciones','Cámara de los Jardineros','Salón del Solsticio Mayor','Sala de los Testigos','Salón de las Ofrendas','Procesión del Voto Profanado','Cámara de los Vigías','Cámara de los Incensarios','Sala del Umbral Fendido','Salón de la Corona','Cámara de las Constelaciones','Cámara del Juramento','Refugio de los Novicios','Sala de las Custodias','Puesto del Custodio Caído','Procesión de las Cenizas','Relicario del Séptimo Sello','Procesión de la Aurora']))
FUNCTIONAL = {'B03','B04','B05','B06'}
APPROVED = {'H12','H06','H13','H14','VC01','VC02','VE01','VE02','VD01','VS01','VS02','VS03','VS04','VS05','VS06'}
photos = {p.stem for p in (OUT/'Capturas').glob('*.jpg')}

def proposal(r):
    id, f = r['id'], r['function'].lower()
    special = {
      'A00':'Conservar arquitectura exterior; no incorporar obra a esta Fase 2.',
      'A01':'Rematar recepción y lectura del descenso; no ampliar automáticamente la zona jugable sobre bolsillos NavMesh.',
      'A02':'Orientar el giro con luz y materiales, sin mobiliario volumétrico en sus 6 m nominales. Medir el ancho libre antes de intervenir.',
      'A03':'Señalizar el rodeo norte aprobado; conservar arco, pilastras, parapetos y rasantes. No usar muestras aisladas como campo de combate.',
      'D01':'Jerarquizar arcadas y terraza, reservar centro y circulación perimetral; investigar el borde de losa observado antes de atribuirlo a un fallo.',
      'D02':'Dar lectura a las arcadas y al descenso, mantener pilares y delimitar los vacíos de la envolvente antes de decidir sobre ellos.',
      'D03':'Conservar íntegra la cámara final; el acceso aprobado no certifica todo su volumen.',
      'P01':'Mantener como delimitación propuesta dentro de D03, sin recinto ni encuentro nuevo.',
      'B01':'Conservar anillo, vacío central y protecciones. No construir ahora en el ámbito de D03.',
      'B02':'Conservar anillo y cambios de cota; no cerrar el vacío. No construir ahora en D03.',
      'M01':'Pavimento ceremonial del eclipse sin nuevo desnivel, friso solar y focos laterales; conservar campo central y salidas.',
      'M02':'Templo previo al núcleo con foco solar lateral corrompido, luz violeta localizada y salida legible; no crear otra arena final.',
      'M03':'Jardines minerales y relieves laterales, conservando el eje de exploración; sin piscinas ni pisos falsos.',
      'M04':'Santuario longitudinal con incensarios y pedestales laterales, votos rotos y corrupción progresiva; procesión despejada.'}
    if id in special: return special[id]
    if id in FUNCTIONAL: return 'Conservar su función de paso/descanso recibida y las colisiones; ambientación futura sin invadir el ancho útil.'
    if id.startswith('G'): return 'Diferenciar la galería con portadas, friso y balizas de salida; no convertir arcos ornamentales cerrados en puertas sin comprobarlos.'
    if 'reliquias' in f: return 'Relicario lateral e inscripciones dentro de la envolvente; secreto sólo propuesto, sin nueva sala ni pasadizo acreditado.'
    if 'recuperación' in f: return 'Pausa visual con luz cálida e hito lateral; mantener función de recuperación y ruta abierta, sin nuevo combate.'
    if 'ceremonial' in f: return 'Rematar encuentros de bóveda y friso solar, introducir un foco lateral y ritmo de tramos; dejar el gran centro despejado.'
    return 'Dar identidad a accesos y perímetro con relieves e hitos laterales; mantener espacio de exploración/combate y paso transversal.'

records = []
for r in rooms:
    d, id = lb[r['id']], r['id']
    samples = d['samples']
    h = [s['headroom'] for s in samples if s.get('headroom') is not None and s['support'] and s['footprint'] and s['capsule']]
    state = 'Espacio propuesto sin recinto independiente' if id == 'P01' else 'Construida y funcional' if id in FUNCTIONAL else 'Construida, pero necesita mejoras'
    record = dict(ID=id, Nombre_original=r['name'], Alias_propuesto=ALIASES.get(id,r['name']), Estado=state,
      Sector=phase_of[id], Centro_X=r['x'], Piso_Y=r['floor'], Centro_Z=r['z'], Ancho_nominal_X=r['w'], Fondo_nominal_Z=r['d'],
      Jerarquia=d['matchedHierarchy'] or 'Sin recinto propio; subzona de D03', Bounds_visuales_Unity=json.dumps(d.get('sourceVisualBounds'),ensure_ascii=False),
      Renderizadores_fuente=len(d['renderers']), Colliders_solidos_fuente=sum(c['active'] and c['enabled'] and not c['trigger'] for c in d['colliders']),
      Muestras=len(samples), Apoyo=sum(s['support'] for s in samples), Huella_capsula_despejadas=sum(s['support'] and s['footprint'] and s['capsule'] for s in samples),
      Despejadas_con_Nav=d['clearSamples'], Sin_conexion_a_semilla=d['clearSamples']-d.get('navSamplesConnectedToSeed',0),
      Apoyo_estimado_m2=round(sum(s['support'] for s in samples)*d['cellArea'],2), Despejado_Nav_estimado_m2=round(d['clearSamples']*d['cellArea'],2),
      Altura_libre_min=round(min(h),3) if h else '', Altura_libre_mediana=round(statistics.median(h),3) if h else '',
      Camino_A03_ida=d.get('entrancePath'), Camino_A03_vuelta=d.get('entranceReturn'), Luminarias_locales=sum(l['active'] for l in d['lightsInEnvelope']),
      Funcion_original=r['function'], Propuesta=proposal(r), Enlaces_documentados=', '.join(r.get('connections',[])),
      Enlaces_recibidos=', '.join(c for c in r.get('connections',[]) if c in APPROVED), Foto=f'Capturas/{id}.jpg' if id in photos else '')
    records.append(record)

def savecsv(name, rows):
    with (OUT/name).open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=rows[0].keys(),delimiter=';');w.writeheader();w.writerows(rows)
savecsv('Inventario_68_actualizado.csv',records)
(OUT/'Inventario_68_actualizado.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
savecsv('Etapas_construccion.csv',[dict(ID=p[0],Sector=p[1],Espacios=p[2],Cantidad=len(p[2].split()),Identidad=p[3],Alcance=p[4],Dependencias=p[5]) for p in PHASES])
md=['# Inventario de los 68 espacios','', 'Las dimensiones de planta son las envolventes del dossier, contrastadas con sus mallas actuales. Los bounds visuales reales figuran también en CSV/JSON: incluyen espesor de pilares y cubierta, no sólo suelo útil. Las áreas y alturas son muestreos, no una certificación integral de sala.','', '| ID | Estado | Centro del piso XYZ | Planta X×Z | Sector | Función documentada |','|---|---|---|---|---|---|']
for d in records: md.append(f'| {d["ID"]} | {d["Estado"]} | ({d["Centro_X"]}, {d["Piso_Y"]}, {d["Centro_Z"]}) | {d["Ancho_nominal_X"]}×{d["Fondo_nominal_Z"]} m | {d["Sector"]} | {d["Funcion_original"]} |')
md+=['','## Fichas individuales','']
for d in records:
    md += [f'### {d["ID"]} — {d["Nombre_original"]}','',f'**{d["Estado"]}.** Sector {d["Sector"]}.',
      f'- Piso: ({d["Centro_X"]}, {d["Piso_Y"]}, {d["Centro_Z"]}); envolvente nominal {d["Ancho_nominal_X"]}×{d["Fondo_nominal_Z"]} m.',
      f'- Jerarquía real: `{d["Jerarquia"]}`. {d["Renderizadores_fuente"]} renderizadores propios y {d["Colliders_solidos_fuente"]} colliders sólidos activos. El suelo compartido puede depender de otra raíz.',
      f'- Apoyo: {d["Apoyo"]}/{d["Muestras"]}; huella/cápsula despejadas: {d["Huella_capsula_despejadas"]}; despejadas también con NavMesh: {d["Despejadas_con_Nav"]}. {d["Sin_conexion_a_semilla"]} de estas últimas no conectan a la semilla.',
      f'- Área con apoyo estimada {d["Apoyo_estimado_m2"]} m²; despejada con NavMesh estimada {d["Despejado_Nav_estimado_m2"]} m². No sumar áreas de subzonas superpuestas.',
      f'- Altura libre muestreada sobre cápsulas despejadas: mínimo {d["Altura_libre_min"]} m; mediana {d["Altura_libre_mediana"]} m. No es un techo uniforme.',
      f'- Semilla ↔ A03: {d["Camino_A03_ida"]}/{d["Camino_A03_vuelta"]}. Topología real, no recepción física de toda esa ruta.',
      f'- Función original: {d["Funcion_original"]}.',f'- Mejora propuesta: {d["Propuesta"]}',
      f'- Enlaces documentados: {d["Enlaces_documentados"]}. Recibidos previamente: {d["Enlaces_recibidos"] or "ninguno se aprueba por este sondeo"}.']
    if d['Alias_propuesto']!=d['Nombre_original']: md.append(f'- Alias artístico opcional: **{d["Alias_propuesto"]}**. No renombra el ID ni modifica la función.')
    if d['Foto']: md.append(f'- [Captura real representativa del Editor]({d["Foto"]}).')
    md.append('')
(OUT/'Inventario_detallado.md').write_text('\n'.join(md),encoding='utf-8')

COLORS=dict(zip([p[0] for p in PHASES],['#d9ae4c','#56a6d1','#aa91dd','#cf79ba','#82bb9b','#91b95b','#d79061','#ba7797','#67b9b7','#b3a886','#6b6c83']))
LEVELS=[20,4,-16,-24,-30,-34,-38]
def svgbase(w,h,title,sub):
    return [f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><rect width="{w}" height="{h}" fill="#101b29"/><style>text{{font-family:Arial;fill:#eef2f8;font-size:13px}} .small{{font-size:10px}} .id{{font-size:12px;font-weight:bold;paint-order:stroke;stroke:#101b29;stroke-width:2px}}</style><text x="35" y="34" font-size="24">{html.escape(title)}</text><text x="35" y="60">{html.escape(sub)}</text>']
for level in LEVELS:
    rs=[r for r in rooms if r['floor']==level]
    x0=min(r['x']-r['w']/2 for r in rs)-12;x1=max(r['x']+r['w']/2 for r in rs)+12
    z0=min(r['z']-r['d']/2 for r in rs)-12;z1=max(r['z']+r['d']/2 for r in rs)+12
    scale=min(1160/(x1-x0),980/(z1-z0));height=int(240+(z1-z0)*scale)
    xy=lambda x,z:(60+(x-x0)*scale,100+(z1-z)*scale)
    s=svgbase(1280,height,f'Ciudad del Sol — planta Y={level} m','Envolventes del dossier y muestras reales. No son muros nuevos ni recepción integral de toda la superficie.')
    for tick in range(math.ceil(x0/12)*12,math.floor(x1/12)*12+1,12):
        x,y=xy(tick,z1);_,b=xy(tick,z0);s.append(f'<path d="M{x},{y} V{b}" stroke="#263346" stroke-width=".6"/><text class="small" x="{x}" y="{b+15}">{tick}</text>')
    for tick in range(math.ceil(z0/12)*12,math.floor(z1/12)*12+1,12):
        x,y=xy(x0,tick);b,_=xy(x1,tick);s.append(f'<path d="M{x},{y} H{b}" stroke="#263346" stroke-width=".6"/><text class="small" x="{x-30}" y="{y}">{tick}</text>')
    for r in rs:
        id=r['id'];x,y=xy(r['x']-r['w']/2,r['z']+r['d']/2);color=COLORS[phase_of[id]]
        stroke='#77d3a0' if id in FUNCTIONAL else '#f8cd70' if phase_of[id]=='S01' else color
        dash='stroke-dasharray="5 3"' if id=='P01' else ''
        s.append(f'<g><title>{html.escape(id+" "+r["name"]+"; "+phase_of[id])}</title><rect x="{x}" y="{y}" width="{r["w"]*scale}" height="{r["d"]*scale}" fill="{color}" fill-opacity=".12" stroke="{stroke}" stroke-width="2" {dash}/></g>')
        for p in lb[id]['samples']:
            a=p['requested'];px,py=xy(a['x'],a['z']);clear=p['support'] and p['footprint'] and p['capsule']
            color='#65c5a0' if clear and p['nav'] else '#ba8a56' if p['support'] else '#5d3a48'
            s.append(f'<circle cx="{px:.2f}" cy="{py:.2f}" r="{max(.6,min(1.5,scale*.2)):.2f}" fill="{color}" opacity=".65"/>')
        px,py=xy(r['x'],r['z']);s.append(f'<text class="id" text-anchor="middle" x="{px}" y="{py-11}">{id}</text><text class="small" text-anchor="middle" x="{px}" y="{py+1}">{phase_of[id]}</text><text class="small" text-anchor="middle" x="{px}" y="{py+13}">{r["w"]}×{r["d"]} m</text>')
    if level==-16:
        pts=[(156,99),(174,99),(174,117),(120,117),(117,123),(117,165),(123,165)]
        coords=' '.join('%.2f,%.2f'%xy(x,z) for x,z in pts);s.append(f'<polyline points="{coords}" fill="none" stroke="#fff3bb" stroke-width="3"/>')
    s += [f'<text x="45" y="{height-96}">Norte +Z ↑ · Este +X → · retícula 12 m · verde: muestra despejada con NavMesh</text>',f'<text x="45" y="{height-72}">Ocre: apoyo con obstáculo/sin NavMesh · oscuro: sin apoyo en esa cota (puede ser vacío deliberado).</text>',f'<text x="45" y="{height-48}">Contorno verde: tránsito recibido · dorado: primer sector · D03/P01: reservados, sin obras.</text><text x="45" y="{height-24}">Entre envolventes hay arquitectura y suelos compartidos no dibujados: no son islas geométricas.</text></svg>']
    (OUT/f'Plano_Y{level}.svg').write_text('\n'.join(s),encoding='utf-8')
sheet=svgbase(1800,1800,'Ciudad del Sol — sectores arquitectónicos por desarrollar','S01 primero · siete cotas separadas · abrir cada planta para sus medidas · sin construcción ejecutada')
for i,level in enumerate(LEVELS):
    x=30+(i%3)*590;y=95+(i//3)*540
    sheet.append(f'<a href="Plano_Y{level}.svg"><rect x="{x}" y="{y}" width="570" height="520" fill="#1b2838" stroke="#44536a"/><text x="{x+15}" y="{y+25}" font-size="20">Y={level} m</text><image href="data:image/svg+xml;base64,{base64.b64encode((OUT/f'Plano_Y{level}.svg').read_bytes()).decode()}" x="{x+8}" y="{y+40}" width="554" height="470" preserveAspectRatio="xMidYMid meet"/></a>')
sheet.append('<text x="630" y="1300" font-size="20">Cada sector conserva las funciones originales.</text><text x="630" y="1340" font-size="18">Ámbar: mejoras arquitectónicas pendientes.</text><text x="630" y="1380" font-size="18">D03 y P01: reserva, no construir.</text></svg>')
(OUT/'Plano_general.svg').write_text('\n'.join(sheet),encoding='utf-8')

# First lot: exact documented room coordinates. Violet mark is only a proposed prop.
xy=lambda x,z:(65+(x-96)*9,95+(180-z)*9)
s=svgbase(1250,1040,'Primer lote propuesto: A03 → R21 → R23','Planta a Y=−16 · propuesta artística sin ejecutar · norte +Z arriba · proporción 9 px/m')
for id in ['A03','R21','R23']:
    r=byid[id];x,y=xy(r['x']-r['w']/2,r['z']+r['d']/2)
    s.append(f'<rect x="{x}" y="{y}" width="{r["w"]*9}" height="{r["d"]*9}" fill="#94743a" fill-opacity=".17" stroke="#e8ba60" stroke-width="2"/><text x="{x+10}" y="{y+24}">{id} · {r["w"]}×{r["d"]} m</text>')
    for p in lb[id]['samples']:
        a=p['requested'];x,y=xy(a['x'],a['z']);color='#548976' if p['support'] and p['footprint'] and p['capsule'] and p['nav'] else '#b8616a';s.append(f'<circle cx="{x}" cy="{y}" r="1.7" fill="{color}"/>')
pts=[(156,99),(174,99),(174,117),(120,117),(117,123),(117,165),(123,165)]
coords=' '.join('%.1f,%.1f'%xy(x,z) for x,z in pts);s.append(f'<polyline points="{coords}" fill="none" stroke="#75b8e9" stroke-width="6"/>')
x,y=xy(127,135);s.append(f'<rect x="{x-9}" y="{y-9}" width="18" height="18" fill="#df9bdb"/><text x="{x+15}" y="{y}">P: foco lateral candidato</text><text x="{x+15}" y="{y+20}">(127,−16,135); huella por validar</text>')
for yy,title,a,b in [(170,'A03 — orientación','Conservar arco y rodeo norte.','Luz guía; no añadir un obstáculo.'),(295,'R21 — salón ceremonial','Friso solar y foco lateral.','Conservar campo central abierto.'),(420,'R23 — recuperación','Pausa cálida y salida legible.','Sin nuevo encuentro ni desnivel.')]:
    s.append(f'<text x="790" y="{yy}" font-size="21">{title}</text><text x="790" y="{yy+28}">{a}</text><text x="790" y="{yy+54}">{b}</text>')
s += ['<text x="790" y="545">Azul: eje recibido, no ancho libre.</text><text x="790" y="572">No construir sobre ese itinerario.</text><text x="790" y="599">Sin paredes transversales nuevas.</text>','<text x="40" y="940">El candidato lateral sólo tiene una muestra puntual despejada acreditada. No se aprobó un pedestal de tamaño finito.</text><text x="40" y="969">Los espacios entre envolventes pertenecen a suelos compartidos: la línea azul no propone pisos nuevos.</text><text x="40" y="998">Se conservan las cotas, el rodeo A03–R21, H12 y las alternativas automáticas recibidas.</text></svg>']
(OUT/'Propuesta_Sector01.svg').write_text('\n'.join(s),encoding='utf-8')

s=svgbase(1200,780,'Propuesta arquitectónica — tres familias del templo solar','Esquemas artísticos: no son elevaciones medidas ni geometría construida.')
for i,(title,color,sub) in enumerate([('Llegada / ofrendas','#dcb257','Oro cálido · orientación'),('Claustro / archivos','#b2a6de','Relieve narrativo · exploración'),('Santuario corrompido','#b67bd2','Violeta localizada · erosión')]):
    x=35+i*390
    s.append(f'<rect x="{x}" y="100" width="365" height="570" rx="12" fill="#1c2938" stroke="{color}"/><text x="{x+18}" y="137" font-size="20">{title}</text><path d="M{x+35},430 L{x+35},270 Q{x+182},100 {x+330},270 L{x+330},430" fill="none" stroke="#8f7545" stroke-width="22"/><path d="M{x+60},430 L{x+60},275 Q{x+182},145 {x+305},275 L{x+305},430" fill="none" stroke="{color}" stroke-width="3"/><path d="M{x+18},432 L{x+348},432 L{x+318},550 L{x+48},550 Z" fill="#665638" stroke="#aa956a"/><rect x="{x+57}" y="300" width="20" height="130" fill="#a58d5c"/><rect x="{x+289}" y="300" width="20" height="130" fill="#a58d5c"/><circle cx="{x+183}" cy="240" r="32" fill="none" stroke="{color}" stroke-width="6"/><path d="M{x+155},540 L{x+163},445 L{x+204},445 L{x+214},540" fill="#7caaaf" fill-opacity=".3"/><text x="{x+18}" y="590" font-size="16">{sub}</text><text x="{x+18}" y="622" font-size="16">Circulación abierta; hitos laterales.</text>')
s.append('<text x="35" y="720">Reutilizar bóvedas, pilastras y piedra. Variar composición y luz; no llenar el suelo con obstáculos.</text><text x="35" y="751">No representa una obra aprobada ni un asset nuevo de juego.</text></svg>')
(OUT/'Propuesta_visual.svg').write_text('\n'.join(s),encoding='utf-8')

escape=html.escape
page=['<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Ciudad del Sol · Fase 2</title><style>body{margin:0;background:#101b29;color:#e8edf5;font:16px/1.5 system-ui}main{max-width:1450px;margin:auto;padding:32px}h1,h2{color:#ecc672}a{color:#8fccec}nav{display:flex;gap:20px;flex-wrap:wrap}.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:20px}.card,figure{background:#1c2938;padding:18px;border-radius:10px}img{max-width:100%;height:auto}table{border-collapse:collapse;width:100%;font-size:13px}th,td{padding:8px;border-bottom:1px solid #3b4859;vertical-align:top}input,select{padding:10px;background:#1c2938;color:#eef2f8;border:1px solid #6b7887;margin:12px 8px 16px 0}.scroll{overflow:auto}.gallery{display:grid;grid-template-columns:repeat(2,1fr);gap:20px}summary{cursor:pointer;color:#e9c679}</style><main><h1>Ciudad del Sol · Fase 2 arquitectónica</h1><p>Auditoría real de 68 espacios · 18.243 muestras PhysX · sin construcción ni cambios de escena.</p><nav><a href="Informe.md">Informe</a><a href="Inventario_detallado.md">68 fichas</a><a href="Inventario_68_actualizado.csv">CSV</a><a href="Etapas_construccion.csv">Etapas</a><a href="Plano_general.svg">Plano general</a></nav><div class="cards"><article class="card"><b>67 estructuras físicas</b><p>54 naves/galerías y 13 accesos, cámaras, terrazas y puentes.</p></article><article class="card"><b>1 subzona propuesta</b><p>P01 dentro de D03, sin recinto independiente.</p></article><article class="card"><b>Primer lote: A03–R21–R23</b><p>Identidad monumental para la llegada y el salón ceremonial.</p></article></div><h2>Propuesta, sin ejecutar</h2><a href="Propuesta_visual.svg"><img src="Propuesta_visual.svg" alt="Familias artísticas propuestas"></a><a href="Propuesta_Sector01.svg"><img src="Propuesta_Sector01.svg" alt="Primer sector propuesto"></a><h2>Planos por cota</h2><nav>']
page+= [f'<a href="Plano_Y{y}.svg">Y={y}</a>' for y in LEVELS]
page+=['</nav><h2>Inventario filtrable</h2><input id="q" placeholder="Código, nombre o función"><select id="phase"><option value="">Todos los sectores</option>']+[f'<option>{p[0]}</option>' for p in PHASES]+['</select><p>Una ruta recibida no acredita toda la sala ni cuatro jugadores. Dimensiones en unidades Unity.</p><div class="scroll"><table><thead><tr><th>ID y nombre</th><th>Estado / sector</th><th>Centro y planta</th><th>Función / propuesta</th><th>Muestreo real</th></tr></thead><tbody>']
for d in records:
    page.append(f'<tr data-phase="{d["Sector"]}"><td><b>{d["ID"]}</b><br>{escape(d["Nombre_original"])}</td><td>{escape(d["Estado"])}<br>{d["Sector"]}</td><td>({d["Centro_X"]}, {d["Piso_Y"]}, {d["Centro_Z"]})<br>{d["Ancho_nominal_X"]}×{d["Fondo_nominal_Z"]} m</td><td>{escape(d["Funcion_original"])}<details><summary>Propuesta</summary>{escape(d["Propuesta"])}</details></td><td>{d["Despejadas_con_Nav"]}/{d["Muestras"]} muestras despejadas con NavMesh<br>{d["Sin_conexion_a_semilla"]} sin conexión a semilla<br>Altura mín. {d["Altura_libre_min"]} m</td></tr>')
page+=['</tbody></table></div><h2>Orden de construcción</h2><div class="cards">']
for p in PHASES: page.append(f'<article class="card"><h3>{p[0]} · {escape(p[1])}</h3><p>{p[2]}</p><p>{escape(p[3])}</p><p>{escape(p[4])}</p><small>{escape(p[5])}</small></article>')
page+=['</div><h2>Capturas reales del Editor</h2><p>14 vistas representativas, con aislamiento temporal del interior. No son iluminación final ni inspección visual de cada metro cuadrado.</p><div class="gallery">']
for id in sorted(photos):page.append(f'<figure><a href="Capturas/{id}.jpg"><img loading="lazy" src="Capturas/{id}.jpg" alt="Vista real {id}"></a><figcaption>{id} · {escape(byid[id]["name"])}</figcaption></figure>')
page+=['</div><p>Cuatro jugadores reales y recepción integral de salas: pendientes.</p></main><script>function filter(){let q=document.getElementById("q").value.toLowerCase(),p=document.getElementById("phase").value;document.querySelectorAll("tbody tr").forEach(r=>r.hidden=!(r.textContent.toLowerCase().includes(q)&&(!p||r.dataset.phase===p)))}document.getElementById("q").oninput=filter;document.getElementById("phase").onchange=filter;</script></html>']
(OUT/'Indice.html').write_text('\n'.join(page),encoding='utf-8')
summary=dict(espacios=68,estados=dict(collections.Counter(d['Estado'] for d in records)),jerarquias_fisicas=67,muestras=sum(len(d['samples']) for d in live),semillas_con_ruta_bidireccional=68,capturas=len(photos),sectores={p[0]:len(p[2].split()) for p in PHASES})
(OUT/'Resumen.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False))
