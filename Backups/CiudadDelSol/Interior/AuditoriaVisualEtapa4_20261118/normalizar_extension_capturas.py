from pathlib import Path
d=Path('Docs/CiudadDelSol/Interior/AuditoriaVisualEtapa4_20261118/Capturas').resolve()
files=list(d.glob('*.png'))
assert len(files)==21
for p in files:
    assert p.resolve().parent==d and p.read_bytes().startswith(bytes.fromhex('ffd8ff'))
    q=p.with_suffix('.jpg')
    assert q.parent==d and not q.exists()
    p.rename(q)
print('21 JPEG originales conservados, sin recodificar.')
