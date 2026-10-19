"""Restore the newly imported vendor assets with independent, stable GUIDs.

Some vendor URP GUIDs collide with the Egyptian temple already in this project.
Only the two new package paths are touched; their internal text references are
rewritten together. Existing gameplay and temple assets are never modified.
"""
import json
import pathlib
import re
import tarfile
import uuid

workspace = pathlib.Path(__file__).resolve().parent.parent
staging = workspace / "Documentation" / "AssetImport"
namespace = uuid.UUID("ce6d6084-77c7-4db9-a182-41c818558bc5")
guid_pattern = re.compile(rb"\b[0-9a-fA-F]{32}\b")
text_extensions = {".mat", ".prefab", ".unity", ".terrainlayer", ".shadergraph", ".asset"}

for package in sorted(staging.glob("*.unitypackage")):
    rows = json.loads(package.with_suffix(".index.json").read_text(encoding="utf8"))
    paths = {r["entry"].split("/")[0]: r["path"] for r in rows}
    remap = {}
    with tarfile.open(package, "r|gz") as archive:
        for member in archive:
            parts = member.name.split("/")
            if len(parts) != 2 or parts[1] != "asset.meta" or parts[0] not in paths:
                continue
            data = archive.extractfile(member).read()
            match = re.search(rb"^guid: ([0-9a-f]{32})", data, re.M)
            if match:
                guid = match.group(1)
                new = uuid.uuid5(namespace, paths[parts[0]]).hex.encode()
                if guid in remap and remap[guid] != new:
                    raise ValueError(f"Internally duplicated GUID in {package.name}: {guid}")
                remap[guid] = new
    count = 0
    with tarfile.open(package, "r|gz") as archive:
        for member in archive:
            parts = member.name.split("/")
            if len(parts) != 2 or parts[0] not in paths or parts[1] not in {"asset", "asset.meta"}:
                continue
            relative = pathlib.PurePosixPath(paths[parts[0]])
            if str(relative) == "Assets/Junker_TownSettings.lighting":
                continue  # Demo-scene lighting is not part of the imported city geometry.
            if ".." in relative.parts or relative.parts[:2] != ("Assets", "HIVEMIND"):
                raise ValueError(relative)
            target = workspace.joinpath(*relative.parts).resolve()
            if not target.is_relative_to((workspace / "Assets/HIVEMIND").resolve()):
                raise ValueError(target)
            data = archive.extractfile(member).read()
            if parts[1] == "asset.meta":
                target = pathlib.Path(str(target) + ".meta")
            if parts[1] == "asset.meta" or target.suffix.lower() in text_extensions:
                if b"\0" not in data[:256]:
                    data = guid_pattern.sub(lambda m: remap.get(m.group(0), m.group(0)), data)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
            count += 1
    print(f"{package.name}: {count} assets/metadata restored, {len(remap)} GUIDs isolated", flush=True)
