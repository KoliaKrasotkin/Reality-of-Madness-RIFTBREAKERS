import collections,difflib,hashlib,json,pathlib,re
w=pathlib.Path(__file__).parent
scene=pathlib.Path('Assets/Scenes/Dungeon_Isla_del_Sol.unity')
def blocks(p):
 result={}
 for b in re.split(r'(?m)^--- !u!',p.read_text(encoding='utf-8'))[1:]:
  m=re.match(r'(\d+) &(-?\d+)',b);result[m[2]]=(int(m[1]),b)
 return result
before=blocks(w/scene);after=blocks(scene)
deleted=sorted(before.keys()-after.keys());added=sorted(after.keys()-before.keys());changed=sorted(k for k in before.keys()&after.keys() if before[k]!=after[k])
unexpected=[];patches=[]
for k in changed:
 kind,old=before[k];new=after[k][1]
 patches+=list(difflib.unified_diff(old.splitlines(),new.splitlines(),fromfile=k+' antes',tofile=k+' después',n=2))
 if kind==1001:
  def strip(m):
   return '' if m[0].count('propertyPath:')==1 and re.search(r"propertyPath: '?m_Materials\.Array\.data\[\d+\]'?\s*\n",m[0]) else m[0]
  pattern=r'(?ms)^    - target:.*?(?=^    - target:|^    m_RemovedComponents:)'
  if re.sub(pattern,strip,old)!=re.sub(pattern,strip,new):unexpected.append((k,'Override ajeno a materiales'))
 elif kind==23:
  pattern=r'(?ms)^  m_Materials:\n.*?(?=^  [A-Za-z_])'
  if re.sub(pattern,'',old)!=re.sub(pattern,'',new):unexpected.append((k,'Renderer cambió algo más que materiales'))
 elif kind!=1660057539:unexpected.append((k,kind))
protected=[]
for item in json.loads((w/'Antes_SHA256.json').read_text(encoding='utf-8')):
 digest=hashlib.sha256(pathlib.Path(item['path']).read_bytes()).hexdigest().upper()
 protected.append(dict(path=item['path'],sha256=digest,unchanged=digest==item['sha256']))
colliderKinds={64,65,135,136,143,154}
result=dict(deleted=deleted,unexpected=unexpected,changedClasses=dict(collections.Counter(str(before[k][0])for k in changed)),addedClasses=dict(collections.Counter(str(after[k][0])for k in added)),originalTransformsUnchanged=not any(before[k][0]==4 for k in changed+deleted),originalCollidersUnchanged=not any(before[k][0]in colliderKinds for k in changed+deleted),protected=protected)
(w/'Integridad_guardado.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
(w/'Diferencias_escena.txt').write_text('\n'.join(patches),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items()if k!='protected'},indent=2))
print('Archivos protegidos conservados:',sum(x['unchanged'] for x in protected if x['path']!=scene.as_posix()),'/',len(protected)-1)
assert not deleted and not unexpected and result['originalTransformsUnchanged'] and result['originalCollidersUnchanged']
assert all(x['unchanged']for x in protected if x['path']!=scene.as_posix())
assert result['changedClasses']=={'1001':35,'23':2,'1660057539':1} or result['changedClasses']=={'23':2,'1001':35,'1660057539':1}
