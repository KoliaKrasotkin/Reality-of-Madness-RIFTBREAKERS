import json,re,hashlib,difflib
from pathlib import Path
r=Path.cwd();w=r/'Backups/CiudadDelSol/Interior/Runas_20261118'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def blocks(p):
    s=p.read_text(encoding='utf-8-sig')
    return {m.group(2):(m.group(1),m.group(0)) for m in re.finditer(r'^--- !u!(\d+) &([^\n]+)\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)}
a=blocks(w/'Antes/Assets/Scenes/Dungeon_Isla_del_Sol.unity');b=blocks(r/'Assets/Scenes/Dungeon_Isla_del_Sol.unity')
unexpected=[];changed={}
for id,(kind,old) in a.items():
    if id not in b:unexpected.append((id,'deleted'));continue
    new=b[id][1]
    if old==new:continue
    changed[kind]=changed.get(kind,0)+1
    if kind=='1660057539':continue
    if kind in ('23','108'):
        # Only render-enabled and local light intensity may change on existing content.
        clean=lambda s:re.sub(r'^  m_(Enabled|Intensity):.*$','',s,flags=re.M)
        if clean(old)==clean(new):continue
    if kind=='1001':
        # Existing glyph prefab overrides may add only Renderer.m_Enabled.
        added=[l[1:] for l in difflib.unified_diff(old.splitlines(),new.splitlines()) if l.startswith('+') and not l.startswith('+++')]
        props=[l.strip().split(': ',1)[1] for l in added if 'propertyPath: ' in l]
        if props and set(props)=={'m_Enabled'}:continue
    unexpected.append((id,kind))
art=json.loads((w/'Recursos_protegidos.json').read_text())
altered=[p for p,h in art.items() if not (r/p).exists() or sha(r/p)!=h]
manifest=json.loads((w/'Antes_SHA256.json').read_text());nav=next(x for x in manifest if x['file'].endswith('.asset'))
receipt=json.loads((w/'Resultado.json').read_text());done=[x for x in receipt['rooms'] if x['status']=='actualizado']
result={'unexpectedOriginalChanges':unexpected,'oldArtResourcesChanged':altered,'navUnchanged':sha(r/nav['file'])==nav['sha256'],'changedOriginalDocumentTypes':changed,'rooms':len(done),'inscriptions':[sum(x['inscriptions'][i] for x in done) for i in range(5)],'characters':sum(x['characters'] for x in done),'lights':sum(x['lights'] for x in done),'fogPatches':sum(x['fogPatches'] for x in done),'pending':[(x['id'],x['reason']) for x in receipt['rooms'] if x['status']!='actualizado'],'persisted':receipt['persisted']}
(w/'Integridad.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
assert not unexpected and not altered and result['navUnchanged']
