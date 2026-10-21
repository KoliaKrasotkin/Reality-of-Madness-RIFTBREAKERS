# El Rift: frame spikes — 2026-10-21

## Reproduction and evidence

Real Unity 6000.3.23f1 host session, existing menu -> map -> training dummy -> walking into encounter -> combat. Stopwatch scopes are opt-in through RiftFrameDiagnostics; disabled in normal play. Baselines are retained alongside this document.

The realtime minimap Camera.Render call peaked at **216.52 ms** on entering another area (baseline-exploration.txt). Its total render CPU cost was 622.84 ms across 156 updates. Periodic legacy HUD scene searches cost up to 6.34 ms per call. Journal disk saves were below 1 ms and native corpse cloning was 1.29 ms in that capture; they were not the reproduced large stall.

A follow-up frame capture isolated the remaining stalls: **156.94 ms and 166.30 ms**, both accompanied by `GC.CollectionCount(0)` increasing. `ProjectSettings/ProjectSettings.asset` had `gcIncremental: 0`. This is enabled now; the final capture records whether Unity actually uses incremental GC. Evidence: before-incremental-gc.txt.

## Changes

- Enabled incremental garbage collection at project level so collection work can be spread over frames on supported targets.
- Replaced realtime minimap camera with a 4096px baked environment atlas. UV cropping follows world position; player arrow rotation updates every frame. No second camera, runtime scene render, shadow pass or minimap RenderTexture. The map does not display moving actors through imagery; future markers should be UI icons.
- Bake tool: **ROM > UI > Bake Rift minimap**. Re-run after changing island geometry/buildings. Captures real scene with fog disabled and temporary overhead light, restores editor settings, leaves gameplay lighting untouched.
- Village NPC list registers on enable/disable. Nearest NPC lookup no longer scans/sorts the entire scene every frame.
- Legacy HUD cleanup runs only during initial setup / replacement of legacy character UI, rather than performing global searches every second indefinitely.
- Health/resource/target bars invalidate geometry only when values change. HUD content updates at 10 Hz; input, minimap positioning and target marker motion remain per frame.
- Binding display names and immutable class/journal resource catalogs are cached; rebinding invalidates cached labels.
- Encounter creation has a budget of one new enemy per exploration tick (0.4s) across nearby encounters. Counts, health, rewards and network authority are preserved.

## Final result

Incremental GC was confirmed active in the Editor (3 ms time slice). Across 5,038 measured gameplay frames, median 8.34 ms, p99 9.69 ms, maximum **25.20 ms**, **0 frames above 100 ms**. Five GC completions occurred without reproducing the 157–166 ms pauses. Minimap runtime render calls: zero. Eight encounter enemies spawned and a native ragdoll was created during the capture.

## Validation

Execute `ROM.EditorTools.RiftJournalProbe.RunPerformance` in Unity batch mode. Captures training combat, 12 seconds of walking toward an encounter, then 12 seconds near a live enemy. Uses a disposable journal identity and restores the player's preferences afterward. Final report: validation.txt; log: Logs/Performance-IncrementalGC.log.

Timing numbers describe this machine's Editor host session, not a mobile device, standalone build, GPU benchmark or remote-client multiplayer benchmark. Editor background jobs and first-use resource work can still affect frame intervals. The change removes the measured realtime minimap stall; it is not a guarantee that all possible stalls throughout every island have been eliminated.
