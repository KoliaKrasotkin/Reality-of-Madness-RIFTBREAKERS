# Recurrence after PirateTavern merge

The merge `ca665240` again stored a conflicted LFS pointer as the content of the scene's LFS object. The working file was 274 bytes. This time the newer map from `c131f622` (Editing PirateTavern) was recovered, preserving the contributor's additions rather than reverting to the older map.

- New scene SHA256: `ab114bd4bdf934df9b25fc8c512d862449f741d142c7b91021d069441cfe040f`
- New scene size: 182,438,859 bytes.
- Downloaded only this scene from Git LFS; verified size, SHA256 and Unity YAML header.
- Evidence: `pirate-tavern-conflicted-pointer.txt`, `pirate-tavern-validation.txt`, `guard-validation.txt`.
- Unity log: `Logs/PirateTavern-Recovery-Validation.log`.
- The StateCache log entry was a sharing violation (file open by another process), separate from the malformed map.

## Prevention

`.unity` files retain Git LFS storage and now use Git's built-in binary merge driver, so concurrent scene changes produce an explicit unresolved Git conflict instead of combining pointer text in the working scene.

`Tools/validate_unity_scenes.py` checks the first-party `Assets/ROM/**/*.unity` entries in the staged index or a supplied revision. It checks both the canonical LFS pointer **and the actual LFS object's Unity YAML**, which is essential to catch this nested-pointer failure. It rejects unresolved markers and mismatched/missing content. No Git index changes are performed by the validator.

Local pre-commit guard is installed on this PC. Each teammate should install it once from the repository root in PowerShell:

```powershell
.\Tools\Install-GitValidation.ps1
```

Requires Python 3 available through `py -3`, `python3` or `python`. Installer preserves existing Git LFS hooks and refuses to overwrite unrelated custom pre-commit hooks. No persistent credential settings are changed.

`.github/workflows/unity-scene-integrity.yml` adds a push/PR check after these files are committed and pushed. Checkout fetches only ROM scene LFS content and does not persist credentials. The workflow has not been run remotely in this task. A GitHub maintainer can require this check in branch protection to block merges with a failed integrity check; a workflow alone does not prevent direct pushes.

The restored scene and guard files need to be included in the next commit together. The current HEAD still points at the corrupt scene content, so validation correctly rejects it until the recovered map is staged.

---

## Previous recovery

# El Rift scene recovery

The working scene was 289 bytes, beginning with the Git LFS header and containing unresolved `<<<<<<< HEAD` / `=======` / `>>>>>>>` conflict markers. The actual Unity YAML was absent. Unity failed to load the scene, so the game mode never existed; the subsequent PlayerInstance.Start / RegisterPlayerInstance null references and Mirror disconnect were consequences of that failed scene load.

Recovered `Assets/ROM/Levels/map_rom_horde.unity` from the complete local Git LFS object:

- SHA256: `0d88ea4d4e99d0bdc80065ad1ce222747944bf2cafee6ab77d5cbfa5e287c522`
- Size: 154,493,941 bytes
- Verified object SHA256, declared size and Unity YAML header before copying.
- Original malformed pointer retained in `conflicted-lfs-pointer.txt`.
- No changes to scene GUID, gameplay code, Git index or commit history.

Verification uses the actual menu -> host -> El Rift -> training / walking / encounter combat flow, through `ROM.EditorTools.RiftJournalProbe.RunPerformance`. Results are copied into `validation.txt`; complete log is `Logs/Scene-Recovery-Validation.log`.

The recovered scene must be included in the next commit; the currently committed version contains the unresolved pointer conflict. Its existing `.unity` Git LFS attribute remains enabled. Do not replace the restored YAML with the text of an LFS pointer or conflict marker when resolving future merges.
