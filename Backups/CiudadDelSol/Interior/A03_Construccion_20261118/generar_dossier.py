import csv
import html
import json
import pathlib
from PIL import Image

work = pathlib.Path('Backups/CiudadDelSol/Interior/A03_Construccion_20261118')
output = pathlib.Path('Docs/CiudadDelSol/Interior/A03_Construccion_20261118')
proof = json.loads((work/'Validacion_recarga.json').read_text(encoding='utf-8'))
assert all(proof[k] for k in ('saved','reloaded','physicsUnchanged','navUnchanged','individualClear'))
with (output/'Elementos.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer = csv.writer(f,delimiter=';')
    writer.writerow(['Objeto','X','Y','Z','Ancho','Alto','Fondo','Materiales'])
    for obj in proof['additions']:
        writer.writerow([obj['path']] + [round(obj['center'][k],5) for k in 'xyz'] + [round(obj['size'][k],5) for k in 'xyz'] + [' | '.join(obj['materials'])])
brief = {k:v for k,v in proof.items() if k not in ('before','after','additions')}
brief['muestras_por_recepcion'] = len(proof['before'])
brief['comparacion_ocho_rutas'] = json.loads((work/'Comparacion_regresion.json').read_text(encoding='utf-8'))
brief['integridad_yaml'] = json.loads((work/'Integridad_guardado.json').read_text(encoding='utf-8'))
(output/'Recepcion_resumen.json').write_text(json.dumps(brief,ensure_ascii=False,indent=2),encoding='utf-8')
shots = [
    ('01_Llegada.jpg','Llegada — arco y piedra oscurecida, tras guardar y recargar'),
    ('02_Pilastra_oriental.jpg','Pilastra oriental — fractura y detalle de piedra'),
    ('03_Regreso_y_salida.jpg','Regreso bajo el arco — superficie libre y acceso existente'),
    ('04_Altura_de_jugador.jpg','Aproximación — cámara del Editor, sin simular un Warrior'),
]
cards = ''.join(f'<figure><img src="Capturas/{name}" alt="{html.escape(title)}"><figcaption>{html.escape(title)}</figcaption></figure>' for name,title in shots)
page = '''<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>A03 — El umbral del silencio</title>
<style>body{margin:0;background:#171715;color:#e1d7c4;font:17px/1.6 system-ui}main{max-width:1280px;margin:auto;padding:40px 24px}h1{font:42px Georgia,serif;color:#d8b776;margin:0}p{max-width:1000px}a{color:#d8b776}figure{margin:32px 0;border:1px solid #514735;background:#24211c}img{width:100%;display:block}figcaption{padding:12px 18px}small{color:#b9b1a4}.status{padding:12px 18px;border-left:3px solid #c4a36a;background:#24211c}</style>
<main><small>CIUDAD DEL SOL · INTERIOR · 18 NOV 2026</small><h1>A03 — El umbral del silencio</h1>
<p>Ambientación construida sobre la escena existente. Capturas reales del Editor de Unity; sin imágenes conceptuales o renderizaciones externas.</p>
<p class="status">Escena guardada y recargada · 1.292 muestras físicas conservadas · 8/8 rutas individuales navegables · Colliders y NavMeshData intactos · Cuatro jugadores pendientes</p>
<p><a href="Informe.md">Informe completo</a> · <a href="Elementos.csv">Inventario de elementos</a> · <a href="Recepcion_resumen.json">Recepción técnica</a></p>'''
page += cards + '<p>R21 y R23 permanecen sin intervención. La aprobación artística queda pendiente de tu revisión. Las advertencias anteriores de bandas laterales se conservan; no equivalen a una certificación de cuatro jugadores.</p></main></html>'
(output/'Indice.html').write_text(page,encoding='utf-8')
assets = pathlib.Path('Assets/ROM/Art/CiudadDelSol/Interior/A03')
print('Materiales locales:',len(list(assets.glob('*.mat'))),'Mallas locales:',len(list(assets.glob('*.asset'))))
for name,_ in shots:
    image = Image.open(output/'Capturas'/name)
    print(name,image.format,image.size)
