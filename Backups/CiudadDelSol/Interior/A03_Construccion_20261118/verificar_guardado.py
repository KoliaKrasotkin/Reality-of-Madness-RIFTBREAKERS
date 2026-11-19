"""Read-only scene comparison; only writes this intervention's receipt."""
import collections
import difflib
import hashlib
import json
import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[5]
work = pathlib.Path(__file__).resolve().parent
scene = pathlib.Path('Assets/Scenes/Dungeon_Isla_del_Sol.unity')

def blocks(path):
    text = path.read_text(encoding='utf-8')
    parts = re.split(r'(?m)^--- !u!', text)[1:]
    result = {}
    for part in parts:
        m = re.match(r'(\d+) &(-?\d+)', part)
        result[m[2]] = (int(m[1]), part)
    return result

original = blocks(work / scene)
current = blocks(scene)
deleted = sorted(original.keys() - current.keys())
added = sorted(current.keys() - original.keys())
changed = sorted(k for k in original.keys() & current.keys() if original[k] != current[k])
diff = []
unexpected = []
for k in changed:
    kind, old = original[k]
    new = current[k][1]
    patch = list(difflib.unified_diff(old.splitlines(), new.splitlines(), fromfile=k+' antes', tofile=k+' después', n=2))
    diff.extend(patch)
    if kind not in (1001, 23, 1660057539):
        unexpected.append((k, kind))
    if kind == 1001:
        # Every new override must be a material reference; preserve all other lines.
        def only_material(m):
            return '' if m[0].count('propertyPath:') == 1 and re.search(r"propertyPath: '?m_Materials\.Array\.data\[\d+\]'?\s*\n", m[0]) else m[0]
        old_clean = re.sub(r'(?ms)^    - target:.*?(?=^    - target:|^    m_RemovedComponents:)', only_material, old)
        new_clean = re.sub(r'(?ms)^    - target:.*?(?=^    - target:|^    m_RemovedComponents:)', only_material, new)
        if old_clean != new_clean:
            unexpected.append((k, 'prefab override other than material'))

hashes = json.loads((work/'Antes_SHA256.json').read_text(encoding='utf-8'))
protected = []
for item in hashes:
    actual = hashlib.sha256(pathlib.Path(item['path']).read_bytes()).hexdigest().upper()
    protected.append(dict(path=item['path'], sha256=actual, unchanged=actual == item['sha256']))

collider_classes = {64,65,135,136,143,154}
summary = dict(
    originalBlocks=len(original), currentBlocks=len(current), deleted=deleted,
    addedClasses=dict(collections.Counter(str(current[k][0]) for k in added)),
    changedClasses=dict(collections.Counter(str(original[k][0]) for k in changed)),
    unexpectedOriginalChanges=unexpected,
    originalTransformsUnchanged=not any(original[k][0]==4 for k in changed+deleted),
    originalCollidersUnchanged=not any(original[k][0] in collider_classes for k in changed+deleted),
    newColliders=sum(current[k][0] in collider_classes for k in added),
    protected=protected)
(work/'Diferencias_escena.txt').write_text('\n'.join(diff),encoding='utf-8')
(work/'Integridad_guardado.json').write_text(json.dumps(summary,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k!='protected'},indent=2))
print('Recursos protegidos sin cambios:', sum(p['unchanged'] for p in protected if p['path']!=scene.as_posix()), '/', len(protected)-1)
assert not deleted and not unexpected
assert summary['originalTransformsUnchanged'] and summary['originalCollidersUnchanged'] and summary['newColliders']==0
assert all(p['unchanged'] for p in protected if p['path']!=scene.as_posix())
