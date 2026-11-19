from pathlib import Path
s=Path('Backups/CiudadDelSol/Interior/Atmosfera_20261118/verificar.py').read_text(encoding='utf-8')
s=s.replace('Atmosfera_20261118','Unificacion_20261119').replace('Art_protegido.json','Runas_protegidas.json')
s=s.replace("if kind=='4' and name.startswith('Luz_'):continue", """if kind=='4':
        strip=lambda s:re.sub(r'^  m_Children:.*\\n(?:  - \\{fileID:.*\\n)*','',s,flags=re.M)
        if strip(old)==strip(new):continue""")
Path('Backups/CiudadDelSol/Interior/Unificacion_20261119/verificar.py').write_text(s,encoding='utf-8')
print('Comprobación reutilizada: protege runas, arquitectura, colliders, NavMesh y cámara final.')
