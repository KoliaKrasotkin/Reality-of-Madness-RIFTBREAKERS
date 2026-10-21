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
