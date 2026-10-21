"""Import only asset payloads and metadata from the two local URP packages.

Preserves package GUIDs and paths. Never imports scripts or project settings.
"""
import json
import pathlib
import shutil
import tarfile

workspace = pathlib.Path(__file__).resolve().parent.parent
assets = (workspace / "Assets").resolve()
staging = workspace / "Documentation" / "AssetImport"
allowed = {"", ".mat", ".shadergraph", ".unity", ".asset", ".prefab", ".png", ".fbx", ".terrainlayer", ".lighting"}

for package in sorted(staging.glob("*.unitypackage")):
    rows = json.loads(package.with_suffix(".index.json").read_text(encoding="utf-8"))
    mapping = {}
    for row in rows:
        relative = pathlib.PurePosixPath(row["path"])
        if relative.parts[0] != "Assets" or ".." in relative.parts or relative.suffix.lower() not in allowed:
            raise ValueError(f"Unsupported package path: {relative}")
        target = workspace.joinpath(*relative.parts).resolve()
        if not target.is_relative_to(assets):
            raise ValueError(f"Path escapes Assets: {target}")
        mapping[row["entry"].split("/")[0]] = target
    count = 0
    with tarfile.open(package, "r|gz") as archive:
        for member in archive:
            parts = member.name.split("/")
            if len(parts) != 2 or parts[0] not in mapping or parts[1] not in {"asset", "asset.meta"}:
                continue
            target = mapping[parts[0]]
            if parts[1] == "asset.meta":
                target = pathlib.Path(str(target) + ".meta")
            if target.exists():
                if parts[1] == "asset.meta" and mapping[parts[0]].is_dir():
                    continue  # Packages share vendor folders; retain the first folder GUID.
                with archive.extractfile(member) as source, target.open("rb") as existing:
                    while True:
                        chunk = source.read(1024 * 1024)
                        if existing.read(len(chunk)) != chunk:
                            raise FileExistsError(f"Refusing to overwrite differing asset: {target}")
                        if not chunk:
                            if existing.read(1):
                                raise FileExistsError(f"Existing asset has trailing data: {target}")
                            break
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            with archive.extractfile(member) as source, target.open("wb") as output:
                shutil.copyfileobj(source, output)
            count += 1
    print(f"Imported {package.name}: {count} payloads and metadata files", flush=True)
