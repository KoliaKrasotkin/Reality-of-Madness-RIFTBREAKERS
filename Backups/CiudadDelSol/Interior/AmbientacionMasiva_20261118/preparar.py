import csv, json, hashlib, shutil
from pathlib import Path
root=Path.cwd()
work=root/'Backups/CiudadDelSol/Interior/AmbientacionMasiva_20261118'
work.mkdir(parents=True,exist_ok=True)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
files=['Assets/Scenes/Dungeon_Isla_del_Sol.unity','Assets/Scenes/Dungeon_Isla_del_Sol.unity.meta','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset.meta','Assets/ROM/Editor/CiudadDelSolInteriorSymbols.cs','Assets/ROM/Editor/CiudadDelSolInteriorR21Atmosphere.cs']
manifest=work/'Antes_SHA256.json'
if not manifest.exists():
    entries=[]
    for name in files:
        p=root/name; dest=work/'Antes'/name
        dest.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(p,dest)
        assert sha(p)==sha(dest)
        entries.append({'file':name,'sha256':sha(p),'backup':str(dest.relative_to(root))})
    manifest.write_text(json.dumps(entries,indent=2),encoding='utf-8')
    art=root/'Assets/ROM/Art/CiudadDelSol/Interior'
    (work/'Recursos_protegidos.json').write_text(json.dumps({str(p.relative_to(root)).replace('\\','/'):sha(p) for p in art.rglob('*') if p.is_file()},indent=2),encoding='utf-8')
rows={r['ID']:r for r in csv.DictReader(open(root/'Docs/CiudadDelSol/Interior/Fase2_Salas_20261118/Inventario_68_actualizado.csv',encoding='utf-8-sig'),delimiter=';')}
audits={r['id']:r for r in json.load(open(root/'Docs/CiudadDelSol/Interior/Fase2_Salas_20261118/Unity_68.json',encoding='utf-8-sig'))}
sectors=list(csv.DictReader(open(root/'Docs/CiudadDelSol/Interior/Fase2_Salas_20261118/Etapas_construccion.csv',encoding='utf-8-sig'),delimiter=';'))
order=['S02','S03','S04','S05','S06','S07','S08','S09','S01']
completed={'A03','R21','R23','G11','R26'}
reserved={'A00','D03','P01','B01','B02','B03','B04'}
rooms=[]
for sector in order:
    s=next(s for s in sectors if s['ID']==sector)
    for id in s['Espacios'].split():
        if id in completed or id in reserved: continue
        r,a=rows[id],audits[id]
        safe=[t for t in a['samples'] if t['support'] and t['footprint'] and t['capsule'] and t['nav'] and abs(t['normal']['y']-1)<.02]
        # Spatially diverse original probes, rechecked against live PhysX/NavMesh in Unity.
        safe.sort(key=lambda t:t['navEdge'],reverse=True)
        selected=[]
        for t in safe:
            p=t['floor']
            if all((p['x']-q['x'])**2+(p['z']-q['z'])**2>16 for q in selected): selected.append(p)
            if len(selected)>=32: break
        rooms.append({'id':id,'sector':sector,'hierarchy':r['Jerarquia'],'center':{'x':float(r['Centro_X']),'y':float(r['Piso_Y']),'z':float(r['Centro_Z'])},'width':float(r['Ancho_nominal_X']),'depth':float(r['Fondo_nominal_Z']),'seed':a['seed']['floor'],'sites':selected,'bounds':a['sourceVisualBounds']})
(work/'Espacios.json').write_text(json.dumps({'rooms':rooms,'preserved':sorted(completed),'reserved':sorted(reserved)},ensure_ascii=False,indent=2),encoding='utf-8')
rsp=(root/'Backups/CiudadDelSol/Interior/Lote02_20261118/Batch02.rsp').read_text(encoding='utf-8-sig')
rsp=rsp.replace('Lote02_20261118/Batch02','AmbientacionMasiva_20261118/Masiva')
(work/'Masiva.rsp').write_text(rsp,encoding='utf-8')
print(json.dumps({'rooms':len(rooms),'order':[r['id'] for r in rooms],'backupVerified':True,'files':len(files)}))
