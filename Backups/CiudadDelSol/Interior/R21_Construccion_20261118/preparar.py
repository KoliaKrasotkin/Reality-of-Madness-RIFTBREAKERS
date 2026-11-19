import hashlib,json,pathlib,shutil
w=pathlib.Path(__file__).parent
assert not (w/'Antes_SHA256.json').exists(), 'No sobrescribir respaldo'
paths=[pathlib.Path(p) for p in [
 'Assets/Scenes/Dungeon_Isla_del_Sol.unity',
 'Assets/Scenes/Dungeon_Isla_del_Sol.unity.meta',
 'Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset',
 'Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset.meta',
 'Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset',
 'Assets/ROM/CiudadDelSolSalasMonumentales/PiedraOpaca.mat',
 'Assets/Standard Assets/Eternal Temple/Source_FBX/Materials/Atlas_Architecture_01.mat',
 'Assets/ROM/Editor/CiudadDelSolInteriorA03Atmosphere.cs',
 'ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset','Packages/manifest.json']]
paths+=list(pathlib.Path('Assets/ROM/Art/CiudadDelSol/Interior/A03').rglob('*'))
rows=[]
for p in paths:
 if not p.is_file():continue
 dest=w/p;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
 h=hashlib.sha256(p.read_bytes()).hexdigest().upper()
 assert hashlib.sha256(dest.read_bytes()).hexdigest().upper()==h
 rows.append(dict(path=p.as_posix(),sha256=h))
(w/'Antes_SHA256.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
print('Copias SHA-256 verificadas:',len(rows))
source=json.loads(pathlib.Path('Docs/CiudadDelSol/Interior/A03_Propuesta_20261118/Fuentes_lore.json').read_text(encoding='utf-8'))
assert hashlib.sha256(pathlib.Path(source['source']).read_bytes()).hexdigest()==source['sha256']
(w/'Lore_verificado.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')
print('PDF oficial coincide con la extracción de seis páginas previamente revisada.')
