from pathlib import Path
import re,json,hashlib,difflib
r=Path.cwd();w=r/'Backups/CiudadDelSol/Interior/Unificacion_20261119';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def blocks(p):
    s=p.read_text(encoding='utf-8-sig')
    return {m.group(2):(m.group(1),m.group(0)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)}
a=blocks(w/'Antes/Assets/Scenes/Dungeon_Isla_del_Sol.unity');b=blocks(r/'Assets/Scenes/Dungeon_Isla_del_Sol.unity')
goNames={id:re.search(r'^  m_Name: (.*)$',s,re.M).group(1).strip('"') for id,(k,s) in a.items() if k=='1' and re.search(r'^  m_Name: (.*)$',s,re.M)}
def ref(s,key):
    m=re.search(r'^  '+key+r': \{fileID: (-?\d+)\}',s,re.M);return m.group(1) if m else None
trGo={id:ref(s,'m_GameObject') for id,(k,s) in a.items() if k=='4'}
parents={id:ref(s,'m_Father') for id,(k,s) in a.items() if k=='4'}
protectedGo={id for id,n in goNames.items() if n=='03 - El Sol Invertido V3'}
protectedTr={id for id,g in trGo.items() if g in protectedGo}
while True:
    extra={id for id,p in parents.items() if p in protectedTr}-protectedTr
    if not extra:break
    protectedTr|=extra
protectedGo|={trGo[t] for t in protectedTr}
protectedPrefabs={id for id,(k,s) in a.items() if k=='1001' and ref(s,'m_TransformParent') in protectedTr}
wordGo={id for id,n in goNames.items() if '_Inscripcion_' in n}
wordDocs={id for id,(k,s) in a.items() if id in wordGo or ref(s,'m_GameObject') in wordGo}
unexpected=[];protectedChanges=[];changed={}
for id,(kind,old) in a.items():
    if id not in b:unexpected.append((id,'deleted'));continue
    new=b[id][1]
    if old==new:continue
    changed[kind]=changed.get(kind,0)+1
    if id in wordDocs or id in protectedTr or id in protectedGo or id in protectedPrefabs or ref(old,'m_GameObject') in protectedGo or ref(old,'m_PrefabInstance') in protectedPrefabs:protectedChanges.append(id)
    go=ref(old,'m_GameObject');name=goNames.get(go,'')
    if kind=='23':
        strip=lambda s:re.sub(r'^  m_Materials:\n(?:  - .*\n)+','',s,flags=re.M)
        if strip(old)==strip(new):continue
    if kind=='33' and name=='Niebla_local_baja':continue
    if kind=='4':
        strip=lambda s:re.sub(r'^  m_Children:.*\n(?:  - \{fileID:.*\n)*','',s,flags=re.M)
        if strip(old)==strip(new):continue
    if kind=='108' and name.startswith(('Luz_','Luz_c')):continue
    if kind=='1001':
        # Unity quotes Array.data paths. Compare all non-material overrides byte-for-byte.
        strip=lambda s:re.sub(r"^    - target:.*\n      propertyPath: ['\"]?m_Materials\.Array\..*\n      value:.*\n      objectReference:.*\n",'',s,flags=re.M)
        if strip(old)==strip(new):continue
    unexpected.append((id,kind,name))
art=json.loads((w/'Runas_protegidas.json').read_text());altered=[p for p,h in art.items() if not (r/p).exists() or sha(r/p)!=h]
manifest=json.loads((w/'Antes_SHA256.json').read_text());nav=next(e for e in manifest if e['file'].endswith('.asset'))
d=json.loads((w/'Resultado.json').read_text());result={'unexpectedChanges':unexpected,'protectedOrRuneChanges':protectedChanges,'originalArtChanged':altered,'navUnchanged':sha(r/nav['file'])==nav['sha256'],'wordDocumentsVerified':len(wordDocs),'protectedDocuments':len(protectedTr)+len(protectedGo)+len(protectedPrefabs),'changedDocumentTypes':changed,'rooms':len([x for x in d['rooms'] if x['status']=='actualizado']),'renderersUpdated':sum(x['renderers'] for x in d['rooms']),'fogPatches':sum(x['fogPatches'] for x in d['rooms']),'persisted':d['persisted']}
(w/'Integridad.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in result.items() if k!='unexpectedChanges'}));print('Cambios inesperados:',len(unexpected),unexpected[:8]);assert not unexpected and not protectedChanges and not altered and result['navUnchanged']
