import json,re,hashlib
from pathlib import Path
r=Path.cwd();w=r/'Backups/CiudadDelSol/Interior/AmbientacionMasiva_20261118'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
before=json.loads((w/'Antes_SHA256.json').read_text())
old=(w/'Antes/Assets/Scenes/Dungeon_Isla_del_Sol.unity').read_text(encoding='utf-8-sig')
new=(r/'Assets/Scenes/Dungeon_Isla_del_Sol.unity').read_text(encoding='utf-8-sig')
def blocks(s):
    return {m.group(1):m.group(0).replace('\r\n','\n') for m in re.finditer(r'^--- !u!\d+ &([^\n]+)\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)}
a,b=blocks(old),blocks(new)
changed=[k for k in a if k not in b or a[k]!=b[k]]
nonroots=[k for k in changed if not a[k].startswith('--- !u!1660057539')]
art=json.loads((w/'Recursos_protegidos.json').read_text())
altered=[p for p,h in art.items() if not (r/p).exists() or sha(r/p)!=h]
nav=next(e for e in before if e['file'].endswith('.asset'))
receipt=json.loads((w/'Resultado.json').read_text()) if (w/'Resultado.json').exists() else {}
done=[x for x in receipt.get('rooms',[]) if x['status']=='ambientado']
out={'originalSceneObjectsChanged':nonroots,'oldResourcesChanged':altered,'navUnchanged':sha(r/nav['file'])==nav['sha256'],'newSceneDocuments':len(b)-len(a),'rooms':len(done),'symbols':sum(x['symbols'] for x in done),'lights':sum(x['lights'] for x in done),'fogQuads':sum(x['fog'] for x in done),'pending':[x for x in receipt.get('rooms',[]) if x['status']!='ambientado'],'persisted':receipt.get('persisted',False)}
(w/'Integridad.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
print(json.dumps(out))
assert not nonroots and not altered and out['navUnchanged']
