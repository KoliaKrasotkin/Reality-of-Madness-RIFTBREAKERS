from pathlib import Path
import hashlib,json,shutil
r=Path.cwd();w=r/'Backups/CiudadDelSol/Interior/BalanceVisual_20261119';w.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
files=['Assets/Scenes/Dungeon_Isla_del_Sol.unity','Assets/Scenes/Dungeon_Isla_del_Sol.unity.meta','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset','Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset.meta','Assets/ROM/Editor/CiudadDelSolInteriorSymbols.cs','Assets/ROM/Editor/CiudadDelSolInteriorR21Atmosphere.cs','Assets/ROM/Characters/Warrior/WarriorPlayer.prefab','Assets/ROM/Characters/Warrior/WarriorPlayer.prefab.meta']
if not (w/'Antes_SHA256.json').exists():
    out=[]
    for f in files:
        p=r/f;q=w/'Antes'/f;q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q);assert sha(q)==sha(p);out.append({'file':f,'sha256':sha(p)})
    (w/'Antes_SHA256.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
    (w/'Arte_protegido.json').write_text(json.dumps({str(p.relative_to(r)).replace('\\','/'):sha(p) for p in (r/'Assets/ROM/Art/CiudadDelSol/Interior').rglob('*') if p.is_file()},indent=2),encoding='utf-8')
rsp=(r/'Backups/CiudadDelSol/Interior/Unificacion_20261119/Unificacion.rsp').read_text(encoding='utf-8-sig').replace('Unificacion_20261119/Unificacion','BalanceVisual_20261119/Balance')
(w/'Balance.rsp').write_text(rsp,encoding='utf-8');print('Respaldo de escena, NavMesh, scripts y prefab Warrior verificado con SHA-256.')
