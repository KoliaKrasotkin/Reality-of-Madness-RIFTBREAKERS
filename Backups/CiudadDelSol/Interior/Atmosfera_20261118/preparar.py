import json,hashlib,shutil
from pathlib import Path
r=Path.cwd();w=r/'Backups/CiudadDelSol/Interior/Atmosfera_20261118';w.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
files=['Assets/Scenes/Dungeon_Isla_del_Sol.unity','Assets/Scenes/Dungeon_Isla_del_Sol.unity.meta','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset.meta','Assets/ROM/Editor/CiudadDelSolInteriorSymbols.cs','Assets/ROM/Editor/CiudadDelSolInteriorR21Atmosphere.cs']
if not (w/'Antes_SHA256.json').exists():
    out=[]
    for f in files:
        p=r/f;q=w/'Antes'/f;q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q);assert sha(q)==sha(p);out.append({'file':f,'sha256':sha(p),'backup':str(q.relative_to(r))})
    (w/'Antes_SHA256.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
    (w/'Art_protegido.json').write_text(json.dumps({str(p.relative_to(r)).replace('\\','/'):sha(p) for p in (r/'Assets/ROM/Art/CiudadDelSol/Interior').rglob('*') if p.is_file()},indent=2),encoding='utf-8')
for name in ['Espacios.json']:
    if not (w/name).exists():shutil.copy2(r/'Backups/CiudadDelSol/Interior/Runas_20261118'/name,w/name)
rsp=(r/'Backups/CiudadDelSol/Interior/Runas_20261118/Runas.rsp').read_text(encoding='utf-8-sig').replace('Runas_20261118/Runas','Atmosfera_20261118/Atmosfera')
(w/'Atmosfera.rsp').write_text(rsp,encoding='utf-8');print('Respaldo SHA-256 verificado')
