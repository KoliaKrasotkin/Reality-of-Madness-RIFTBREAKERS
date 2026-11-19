from pathlib import Path
import json, math, html, base64

OUT=Path(__file__).parent
ROOT=OUT.parents[3]
def header(w,h,title,subtitle):
    return [f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><defs><pattern id="stone" width="64" height="32" patternUnits="userSpaceOnUse"><rect width="64" height="32" fill="#302d2a"/><path d="M0,0 H64 M0,32 H64 M0,0 V32 M32,0 V16 M64,16 H0" stroke="#201f20" stroke-width="1.4"/></pattern></defs><rect width="{w}" height="{h}" fill="#11171f"/><style>text{{font-family:Arial;fill:#ddd6ca}}.small{{font-size:13px}}.tag{{font-size:14px;font-weight:bold;paint-order:stroke;stroke:#11171f;stroke-width:3px}}</style><text x="35" y="36" font-size="26">{html.escape(title)}</text><text x="35" y="65">{html.escape(subtitle)}</text>']

# Precise floor plan, deliberately not a fabricated footprint for the existing arch.
S=20
xy=lambda x,z:(80+(x-140)*S,105+(120-z)*S)
s=header(1380,1030,'A03 — El umbral del silencio · propuesta de implantación','No construido · piso Y=−16 · norte +Z arriba · posiciones artísticas pendientes de montaje real')
x,y=xy(144,114)
s.append(f'<rect x="{x}" y="{y}" width="480" height="480" fill="url(#stone)" stroke="#a38b56" stroke-width="3"/>')
for a in [144,150,156,162,168]:
    ax,ay=xy(a,114);_,by=xy(a,90);s.append(f'<path d="M{ax},{ay} V{by}" stroke="#696052" stroke-width="1"/>')
for z in [90,96,102,108,114]:
    ax,ay=xy(144,z);bx,_=xy(168,z);s.append(f'<path d="M{ax},{ay} H{bx}" stroke="#696052" stroke-width="1"/>')
# Rails are schematized from the existing openings, not new construction.
for a,b in [((144.53,90),(144.53,114)),((144,113.47),(168,113.47)),((144,90.53),(147,90.53)),((153,90.53),(168,90.53)),((167.47,90),(167.47,96.32)),((167.47,101.68),(167.47,114))]:
    x1,y1=xy(*a);x2,y2=xy(*b);s.append(f'<path d="M{x1},{y1} L{x2},{y2}" stroke="#8a8170" stroke-width="6"/>')
route=[(156,99),(174,99),(174,117),(120,117)]
# The northern continuation is clipped only in this drawing, not in the original route.
route=[(156,99),(174,99),(174,117),(141,117)]
co=' '.join('%.1f,%.1f'%xy(*p) for p in route)
s.append(f'<polyline points="{co}" fill="none" stroke="#70a9bd" stroke-width="6"/>')
for a,b in [((150,85),(150,99)),((150,99),(156,99))]:
    x1,y1=xy(*a);x2,y2=xy(*b);s.append(f'<path d="M{x1},{y1} L{x2},{y2}" stroke="#a8a092" stroke-width="3" stroke-dasharray="7 5"/>')
ax,ay=xy(156,108)
s.append(f'<circle cx="{ax}" cy="{ay}" r="12" fill="none" stroke="#b29a69" stroke-width="2" stroke-dasharray="4 3"/><text class="small" x="{ax+18}" y="{ay-12}">Arco existente</text><text class="small" x="{ax+18}" y="{ay+7}">ancla (156,108), no huella medida</text>')
items=[('02/03/04',(151,105),'#b1a48b'),('04/05',(161,105),'#76529c'),('06',(165,102.8),'#c6954b'),('08',(147,113),'#706047')]
for label,(xx,zz),color in items:
    px,py=xy(xx,zz);s.append(f'<circle cx="{px}" cy="{py}" r="8" fill="{color}" stroke="#11171f" stroke-width="2"/><text class="tag" x="{px+13}" y="{py+5}">{label}</text>')
a,b=xy(159.5,99),xy(165,99)
s.append(f'<path d="M{a[0]},{a[1]-6} L{b[0]},{b[1]-6}" stroke="#c6954b" stroke-width="4" stroke-dasharray="11 8"/><text class="tag" x="{a[0]}" y="{a[1]+26}">07 · marca superficial</text>')
# Real dimension lines.
x1,y1=xy(144,87);x2,_=xy(168,87)
s.append(f'<path d="M{x1},{y1-8} V{y1+8} M{x1},{y1} H{x2} M{x2},{y1-8} V{y1+8}" stroke="#b1a48b"/><text text-anchor="middle" x="{(x1+x2)/2}" y="{y1+28}">24 m · 4 módulos de 6 m</text>')
labels=[
 ('01','Materiales locales','Piedra mate oscurecida; sin cambiar geometría.'),
 ('02/03','Memoria solar y advertencia','Relieve roto y texto breve adosados a piedra.'),
 ('04','Cierre abandonado','Anclajes en jambas; nada cruza el hueco.'),
 ('05','Una sola cicatriz violeta','Acento pequeño; no portal ni nueva mecánica.'),
 ('06','Guía cálida de salida','Luminaria suspendida al techo real, fuera del eje.'),
 ('07','Vestigio del pavimento','Tratamiento visual sin resalto ni collider.'),
 ('08','Linterna apagada','Sobre soporte existente; omitir si no cabe.')]
for i,(tag,title,desc) in enumerate(labels):
    yy=130+i*83;s.append(f'<text x="815" y="{yy}" font-size="18">{tag} — {title}</text><text class="small" x="815" y="{yy+25}">{desc}</text>')
s += ['<text class="small" x="80" y="117">Hacia R21 por rodeo norte recibido ←</text>', '<text x="780" y="805">Azul: eje recibido, no anchura cooperativa certificada.</text><text x="780" y="833">Gris discontinuo: llegada documental AC04.</text><text x="780" y="861">No añadir objetos sólidos al piso libre.</text>', '<text x="35" y="932">Las barandas se representan de forma esquemática. Sus colliders originales no se modifican ni se sustituyen por versiones rotas.</text><text x="35" y="959">Los puntos de montaje son propuestas: ajustar a superficie, apoyo, normal y techo reales antes de guardar una construcción.</text><text x="35" y="986">El dibujo no amplía A03 ni acredita toda su envolvente. La circulación con cuatro jugadores reales continúa pendiente.</text></svg>']
(OUT/'A03_Plano_propuesto.svg').write_text('\n'.join(s),encoding='utf-8')

# Art board: new native vector illustration, explicitly schematic rather than an invented render.
s=header(1380,950,'A03 — intención visual y narrativa','Esquema artístico, no captura de Unity ni alzado dimensional · sin construcción ejecutada')
s.append('<rect x="35" y="95" width="870" height="640" rx="12" fill="#1b2027" stroke="#44474a"/><path d="M60,715 L150,520 L790,520 L880,715 Z" fill="#413a32"/><path d="M160,520 V310 Q465,100 770,310 V520" fill="url(#stone)" stroke="#71624a" stroke-width="22"/><path d="M240,520 V330 Q465,195 690,330 V520" fill="#11151c"/><rect x="150" y="320" width="90" height="235" fill="url(#stone)"/><rect x="690" y="320" width="90" height="235" fill="url(#stone)"/>')
# Partial solar disc, not a new historical symbol asserted as canon.
s.append('<path d="M213,366 A28,28 0 1 0 190,400 M183,370 l-11,-7 M184,392 l-12,5 M212,410 l2,10 M226,354 l7,-10" stroke="#a38b56" stroke-width="5" fill="none"/><rect x="166" y="438" width="62" height="39" fill="#302d2a" stroke="#706047"/><text x="197" y="454" text-anchor="middle" font-size="7">ACCESO</text><text x="197" y="466" text-anchor="middle" font-size="7">RESTRINGIDO</text>')
s.append('<path d="M736,357 L725,389 L742,411 L729,430 L746,455 L739,480" fill="none" stroke="#76529c" stroke-width="4"/><path d="M718,488 l19,-6 M734,466 l16,5" stroke="#594264" stroke-width="2"/><rect x="217" y="490" width="14" height="28" fill="#706047"/><rect x="699" y="490" width="14" height="28" fill="#706047"/>')
s.append('<path d="M847,160 V355" stroke="#706047" stroke-width="4"/><path d="M830,357 H864 L858,405 H837 Z" fill="#3b3024" stroke="#a38b56"/><rect x="838" y="366" width="17" height="26" fill="#c6954b"/><path d="M844,405 L760,700 L877,700 Z" fill="#c6954b" opacity=".07"/><path d="M525,646 l25,10 M578,666 l26,10 M633,688 l26,9" stroke="#a38b56" stroke-width="3"/>')
for y,title,desc in [(140,'No es el exterior','Piedra mate, hollín y bronce apagado.'),(250,'Sin relato impuesto','Advertencias y cierres como sugerencia.'),(360,'Violeta con medida','Una fractura; la sala no se tiñe entera.'),(470,'Silencio y ausencia','Luz residual y objetos abandonados.'),(580,'Paso reconocible','La salida este conserva su lectura.')]:
    s.append(f'<text x="945" y="{y}" font-size="20">{title}</text><text class="small" x="945" y="{y+29}">{desc}</text>')
palette=[('#302D2A','Piedra'),('#413A32','Pavimento'),('#706047','Bronce'),('#B1A48B','Grabado'),('#1D2630','Sombra'),('#C6954B','Ámbar'),('#76529C','Acento')]
for i,(color,name) in enumerate(palette):
    x=55+i*180;s.append(f'<rect x="{x}" y="778" width="135" height="34" rx="4" fill="{color}" stroke="#777"/><text class="small" x="{x}" y="835">{name} · {color}</text>')
s += ['<text x="35" y="891">Las proporciones del arco son ilustrativas. Se reutilizará el arco real; no se reemplaza por este esquema.</text><text x="35" y="922">No hay cadenas en el paso, escombros en el suelo, nueva puerta funcional ni alteración de NavMesh.</text></svg>']
(OUT/'A03_Intencion_visual.svg').write_text('\n'.join(s),encoding='utf-8')

picture=ROOT/'Docs/CiudadDelSol/Interior/Fase2_Salas_20261118/Capturas/A03.jpg'
image_uri='data:image/jpeg;base64,'+base64.b64encode(picture.read_bytes()).decode()
page='''<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>A03 — El umbral del silencio</title><style>body{background:#11171f;color:#ddd6ca;font:17px/1.6 system-ui;margin:0}main{max-width:1300px;margin:auto;padding:30px}h1,h2{color:#b99b66}a{color:#8bb3c2}img{width:100%;height:auto}nav{display:flex;gap:25px;flex-wrap:wrap}.note{border-left:4px solid #76529c;padding:12px 18px;background:#1b2027}</style><main><h1>A03 — El umbral del silencio</h1><p class="note">Propuesta para aprobación. No construida ni validada como montaje. Se conserva la arquitectura, el suelo y la navegación.</p><nav><a href="Propuesta.md">Propuesta detallada</a><a href="Elementos_propuestos.csv">Elementos y coordenadas</a><a href="A03_Plano_propuesto.svg">Plano vectorial</a></nav><h2>Intención visual propuesta</h2><img src="A03_Intencion_visual.svg" alt="Esquema artístico de la ambientación propuesta"><h2>Implantación sobre A03</h2><img src="A03_Plano_propuesto.svg" alt="Plano propuesto con suelo, accesos y anclas"><h2>Estado actual de referencia</h2><p>Captura real de la auditoría anterior. No es una imagen del resultado propuesto.</p>'''
page+=f'<img src="{image_uri}" alt="Captura real anterior de A03 sin construir esta propuesta">'
page+='''<p>El lore respalda investigación, contención y abandono. Los textos y cierres de A03 son interpretaciones ambientales, no acontecimientos nuevos confirmados.</p><p>No se intervinieron R21, R23, Mirror, combate, enemigos ni cinemáticas.</p></main></html>'''
(OUT/'Indice.html').write_text(page,encoding='utf-8')
print('Planos y presentación de propuesta generados; sin assets de juego.')
