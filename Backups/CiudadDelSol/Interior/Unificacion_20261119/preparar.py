from pathlib import Path
import hashlib,json,shutil
r=Path.cwd();w=r/'Backups/CiudadDelSol/Interior/Unificacion_20261119';w.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
files=['Assets/Scenes/Dungeon_Isla_del_Sol.unity','Assets/Scenes/Dungeon_Isla_del_Sol.unity.meta','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset.meta','Assets/ROM/Editor/CiudadDelSolInteriorSymbols.cs','Assets/ROM/Editor/CiudadDelSolInteriorR21Atmosphere.cs','Assets/ROM/Art/CiudadDelSol/Interior/Atmosfera/InteriorForward.hlsl','Backups/CiudadDelSol/Interior/Atmosfera_20261118/recursos.py']
if not (w/'Antes_SHA256.json').exists():
    out=[]
    for f in files:
        p=r/f;q=w/'Antes'/f;q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q);assert sha(q)==sha(p);out.append({'file':f,'sha256':sha(p)})
    (w/'Antes_SHA256.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
    (w/'Runas_protegidas.json').write_text(json.dumps({str(p.relative_to(r)).replace('\\','/'):sha(p) for p in (r/'Assets/ROM/Art/CiudadDelSol/Interior/Simbolos').rglob('*') if p.is_file()},indent=2),encoding='utf-8')
shutil.copy2(r/'Backups/CiudadDelSol/Interior/Atmosfera_20261118/Espacios.json',w/'Espacios.json')
rsp=(r/'Backups/CiudadDelSol/Interior/Atmosfera_20261118/Atmosfera.rsp').read_text(encoding='utf-8-sig').replace('Atmosfera_20261118/Atmosfera','Unificacion_20261119/Unificacion')
(w/'Unificacion.rsp').write_text(rsp,encoding='utf-8');print('Escena, NavMesh, scripts y shader respaldados y verificados.')
