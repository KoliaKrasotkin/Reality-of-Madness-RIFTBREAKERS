"""Recover the serialized height matrix from the damaged Sun Temple asset.

The file is 387 bytes shorter than its header declares. The height matrix is
short by 213 CR bytes; its trailing array headers and scale remain readable.
Only a new float heightmap is written. The source asset is preserved.
"""
import array
import pathlib
import struct
import sys

source = pathlib.Path("Assets/Standard Assets/Sun_Temple/Scenes/DemoScene_Terrain.asset")
blob = source.read_bytes()
start, end, n = 1943886, 10340475, 2049
raw = blob[start:end]
missing = n*n*2-len(raw)
assert struct.unpack_from("<I", blob, start-4)[0] == n*n
assert missing == 213
assert struct.unpack_from("<I", blob, 10602637)[0] == n
scale_x, scale_y, scale_z = struct.unpack_from("<fff", blob, 10602645)
out = array.array("H")
position = inserted = 0
bonus=float(sys.argv[1]) if len(sys.argv)>1 else 1200

def score(value, consume, index):
    previous = out[-1] if out else 0
    cost = abs(value-previous)
    if index >= n:
        cost += abs(value-out[index-n])*.3
    previous = value
    p = position+consume
    for _ in range(6):
        if p+1 >= len(raw):
            break
        value = raw[p] | raw[p+1]<<8
        cost += abs(value-previous)
        if value > 32767:
            cost += 50000
        previous = value
        p += 2
    return cost-(bonus if consume==1 else 0)

for index in range(n*n):
    if position>=len(raw):
        print("early end",index,inserted,"max",max(out),"bad",sum(v>32767 for v in out)); sys.exit(1)
    a = raw[position]
    b = raw[position+1] if position+1 < len(raw) else 0
    options = [(a | b<<8, 2, False)]
    if inserted < missing:
        if a == 10:
            options.append((13 | 10<<8, 1, True))
        if b == 10:
            options.append((a | 13<<8, 1, True))
    value, consume, repair = min(options, key=lambda option: score(option[0], option[1], index)) if len(options)>1 else options[0]
    out.append(value)
    position += consume
    inserted += repair
    if position > len(raw):
        raise ValueError("Height reconstruction exceeded the available stream")

assert inserted == missing and position == len(raw), (inserted, position, len(raw))
print("CHECK",inserted,"max",max(out),"bad",sum(v>32767 for v in out))
repaired_spikes=0
for index,value in enumerate(out):
    if value<=32767:
        continue
    row,column=divmod(index,n)
    neighbors=[out[y*n+x] for y in range(max(0,row-1),min(n,row+2)) for x in range(max(0,column-1),min(n,column+2)) if out[y*n+x]<=32767]
    out[index]=sorted(neighbors)[len(neighbors)//2] if neighbors else 0
    repaired_spikes+=1
destination = pathlib.Path("Documentation/sun-terrain-heights.bin")
with destination.open("wb") as target:
    target.write(struct.pack("<ifff", n, scale_x*(n-1), scale_y, scale_z*(n-1)))
    # Unity's serialized height values use the signed 16-bit positive range.
    floats = array.array("f", (value/32767.0 for value in out))
    floats.tofile(target)
print(f"Recovered {n}x{n} heights, inserted={inserted}, min={min(out)}, max={max(out)}, size={scale_x*(n-1),scale_y,scale_z*(n-1)}")
pathlib.Path("Documentation/sun-terrain-recovery.txt").write_text(f"Reconstructed {n}x{n} heights from damaged serialized data; restored {inserted} missing CR bytes using local height continuity; repaired {repaired_spikes} invalid samples by neighborhood median. Recovery is approximate. Source asset preserved.\n",encoding="utf8")
