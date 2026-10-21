"""Reject malformed/conflicted first-party ROM scene pointers in Git, without downloading LFS assets."""
import argparse
from pathlib import Path
import re
import subprocess
import sys


def pointer_error(data):
    if re.search(rb"(?m)^(<<<<<<<|=======|>>>>>>>)", data):
        return "contains unresolved merge conflict markers"
    if not re.fullmatch(
        rb"version https://git-lfs.github.com/spec/v1\noid sha256:[0-9a-f]{64}\nsize [1-9][0-9]*\n?",
        data,
    ):
        return "is not a canonical Git LFS pointer (raw scene, nested pointer or damaged data)"
    return None


def git(*args):
    return subprocess.check_output(["git", *args])


def validate(revision=None):
    lfs_root = Path(git("rev-parse", "--git-path", "lfs/objects").decode().strip())
    if revision:
        records = git("ls-tree", "-r", "-z", revision)
    else:
        records = git("ls-files", "--stage", "-z", "--", "*.unity")
    entries = []
    errors = []
    for record in records.split(b"\0"):
        if not record:
            continue
        metadata, path = record.split(b"\t", 1)
        if not path.startswith(b"Assets/ROM/") or not path.endswith(b".unity"):
            continue
        fields = metadata.split()
        name = path.decode("utf-8", errors="replace")
        if not revision and fields[2] != b"0":
            errors.append(name + ": has unmerged index stages")
            continue
        entries.append((fields[2] if revision else fields[1], name))
    # Query sizes first, so an accidentally committed 180 MB raw scene is never read into memory.
    query = b"".join(oid + b"\n" for oid, _ in entries)
    metadata = subprocess.run(
        ["git", "cat-file", "--batch-check"], input=query, capture_output=True, check=True
    ).stdout.splitlines()
    small = []
    for (oid, name), meta in zip(entries, metadata):
        fields = meta.split()
        if len(fields) != 3 or fields[1] != b"blob":
            errors.append(name + ": missing Git blob")
        elif int(fields[2]) > 1024:
            errors.append(name + ": scene stored directly in Git instead of Git LFS")
        else:
            small.append((oid, name))
    result = subprocess.run(
        ["git", "cat-file", "--batch"],
        input=b"".join(oid + b"\n" for oid, _ in small),
        capture_output=True, check=True,
    ).stdout
    offset = 0
    for _, name in small:
        end = result.index(b"\n", offset)
        size = int(result[offset:end].split()[2])
        data = result[end + 1:end + 1 + size]
        offset = end + 2 + size
        error = pointer_error(data)
        if error:
            errors.append(name + ": " + error)
            continue
        oid = re.search(rb"oid sha256:([0-9a-f]{64})", data).group(1).decode()
        expected_size = int(re.search(rb"size ([0-9]+)", data).group(1))
        content = lfs_root / oid[:2] / oid[2:4] / oid
        if not content.is_file():
            errors.append(name + ": missing local LFS content; fetch ROM scenes before validation")
            continue
        if content.stat().st_size != expected_size:
            errors.append(name + ": LFS content size mismatch")
            continue
        with content.open("rb") as stream:
            header = stream.read(128).lstrip(b"\xef\xbb\xbf")
            if not header.startswith(b"%YAML 1.1"):
                errors.append(name + ": LFS object is not a Unity scene (possibly a nested/conflicted pointer)")
                continue
            stream.seek(0)
            tail = b""
            while chunk := stream.read(1024 * 1024):
                chunk = tail + chunk
                if re.search(rb"(?m)^(<<<<<<< |=======$|>>>>>>> )", chunk):
                    errors.append(name + ": Unity YAML contains unresolved merge markers")
                    break
                tail = chunk[-128:]
    if errors:
        print("Unity scene validation FAILED:\n" + "\n".join(errors), file=sys.stderr)
        print("Recover the full scene, then git add it with Git LFS enabled. Do not commit conflict markers.", file=sys.stderr)
        return 1
    print(f"Validated {len(entries)} Unity scene pointers ({revision or 'staged index'}).")
    return 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--revision", help="Validate a commit, rather than the staged index")
    args = parser.parse_args()
    sys.exit(validate(args.revision))
