import csv,json,hashlib,shutil
from pathlib import Path
r=Path.cwd();w=r/'Backups/CiudadDelSol/Interior/Runas_20261118';w.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
files=['Assets/Scenes/Dungeon_Isla_del_Sol.unity','Assets/Scenes/Dungeon_Isla_del_Sol.unity.meta','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset.meta','Assets/ROM/Editor/CiudadDelSolInteriorSymbols.cs','Assets/ROM/Editor/CiudadDelSolInteriorR21Atmosphere.cs']
if not (w/'Antes_SHA256.json').exists():
    manifest=[]
    for f in files:
        p=r/f;q=w/'Antes'/f;q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q);assert sha(p)==sha(q)
        manifest.append({'file':f,'sha256':sha(p),'backup':str(q.relative_to(r))})
    (w/'Antes_SHA256.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    (w/'Recursos_protegidos.json').write_text(json.dumps({str(p.relative_to(r)).replace('\\','/'):sha(p) for p in (r/'Assets/ROM/Art/CiudadDelSol/Interior').rglob('*') if p.is_file()},indent=2),encoding='utf-8')
plan=json.loads((r/'Backups/CiudadDelSol/Interior/AmbientacionMasiva_20261118/Espacios.json').read_text(encoding='utf-8'))
rows={x['ID']:x for x in csv.DictReader(open(r/'Docs/CiudadDelSol/Interior/Fase2_Salas_20261118/Inventario_68_actualizado.csv',encoding='utf-8-sig'),delimiter=';')}
audits={a['id']:a for a in json.load(open(r/'Docs/CiudadDelSol/Interior/Fase2_Salas_20261118/Unity_68.json',encoding='utf-8-sig'))}
for id in ['A03','R21','R23','G11','R26']:
    row=rows[id];a=audits[id]
    good=[s for s in a['samples'] if s['support'] and s['footprint'] and s['capsule'] and s['nav'] and s['normal']['y']>.99]
    good.sort(key=lambda t:t['navEdge'],reverse=True);sites=[]
    for s in good:
        p=s['floor']
        if all((p['x']-q['x'])**2+(p['z']-q['z'])**2>16 for q in sites):sites.append(p)
        if len(sites)==32:break
    plan['rooms'].append({'id':id,'sector':row['Sector'],'hierarchy':row['Jerarquia'],'center':{'x':float(row['Centro_X']),'y':float(row['Piso_Y']),'z':float(row['Centro_Z'])},'width':float(row['Ancho_nominal_X']),'depth':float(row['Fondo_nominal_Z']),'seed':a['seed']['floor'],'sites':sites,'bounds':a['sourceVisualBounds']})
plan['preserved']=[]
(w/'Espacios.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
rsp=(r/'Backups/CiudadDelSol/Interior/AmbientacionMasiva_20261118/Masiva.rsp').read_text(encoding='utf-8-sig').replace('AmbientacionMasiva_20261118/Masiva','Runas_20261118/Runas')
(w/'Runas.rsp').write_text(rsp,encoding='utf-8')
print('Respaldo SHA-256 verificado; espacios autorizados:',len(plan['rooms']))
