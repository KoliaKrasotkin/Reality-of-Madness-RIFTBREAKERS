# Little Demon — reusable common enemy

## Assets and launch

- Enemy prefab: `Assets/ROM/Enemies/LittleDemon/LittleDemon_Enemy.prefab`.
- Tuning/prefab definition: `Assets/ROM/Enemies/LittleDemon/LittleDemon.asset`.
- Dedicated Animator: `Assets/ROM/Enemies/LittleDemon/LittleDemon_Enemy.controller`.
- Animation copies and URP material variants are inside the same folder.
- The Sculptiq Studios source package is preserved unchanged.

Open `Assets/ROM/Scenes/MainMenu.unity`, Play, enter El Rift and leave the starting refuge toward an encounter. Twelve regular encounter zones now use Little Demon through the new population system. Safe checkpoints remain protected. Existing miniboss encounters retain their previous enemies/AI/spawning; this iteration migrates the regular encounters, not those minibosses.

Defaults: 70 HP, 5 damage, 20 XP, 8 local gold; speed 2.4 m/s, detection 22m, attack range 1.65m, windup 0.55s, cooldown 1.8s, leash 36m. Balance values are editable in the definition. Test fixtures temporarily use 400 HP to prevent random player critical hits from invalidating follow-up AI tests; the production definition remains at 70 HP.

## Own enemy and spawn architecture

The prefab has its own Mirror NetworkIdentity / NetworkTransformReliable, NavMeshAgent, capsule, body hitbox, `RiftEnemyAgent` and native visual skeleton. It has **no PlayerInstance, CharacterInstance, BotAI, CharacterMotor, CharacterAnimator, IDSGameplayPose, template equipment or soldier rig**.

The shared Health component is a compatibility bridge to existing player damage, critical hits, damage numbers, targeting and gamemode safety. It is not the template player prefab. Native NPC health, death and animation events have their own synchronized state; movement is server-authoritative. NPCs never register as fake players. The new runtime classes live in the existing gameplay assembly so the existing combat assembly can refer to them without circular dependencies.

`RiftEnemyPopulation` manages a maximum of 24 living native NPCs across active zones, creating at most one every 0.5 seconds. A `RiftEnemySpawnZone` contains independent prefab definitions with configurable weights, spawn points, activation/retirement radii and a respawn delay (90s default). Remote living NPCs retire without rewards. Dead NPCs play their own death clip and are removed through Mirror after five seconds. Regular RiftEncounter objects are flagged NativeSpawn so the legacy horde does not create duplicate template bots there.

Each native NPC detects and pursues human players, respects safe areas and leash, returns home and recovers health, uses threat and taunt, supports stagger, and performs a telegraphed single-target melee strike. NavMesh avoidance and a limit of two concurrent attackers per target keep close-range encounters readable.

## Reuse / other enemy types

1. Reuse the prefab directly through a spawn zone, or duplicate it and its definition for another enemy type.
2. Assign each definition's Prefab and each prefab's Definition. Supply the type's own visual, Animator and tuning.
3. Add `RiftEnemySpawnZone`, assign ground-level transforms to SpawnPoints and definitions/weights to Enemies.
4. Add the zone to the map's `RiftEnemyPopulation.Zones`. Spawn points must be on the baked NavMesh and outside all checkpoint safe radii.
5. Register any new network prefab in the NetworkManager spawnable prefab list, as done for Little Demon in the menu, map and manager prefab.

The Little Demon builder does not replace existing non-empty weighted enemy choices. It is available at **ROM > Enemies > Build Little Demon and spawn zones**. Editing the existing definition is enough for balance; do not regenerate the map merely to change damage or rewards.

## Animations and rendering

Uses package-native Idle01, Walk01, Attack01/02/03, Hit01 and Death01 on its original avatar. Dedicated gameplay controller, no template retargeting or runtime bone mapper. Death uses animation rather than the template ragdoll.

Hell Brute transformation is intentionally not enabled on these low-tier minions. The original Transform and alternate clips remain in the source package for a future distinct enemy/elite prefab.

Source materials use names such as `_Base_Map` / `_Normal_Map`, not the URP Lit property names. The build uses dedicated URP variants, preserving base textures and normals; hair/eyebrow alpha masks are packed into game-specific RGBA textures. Source material/texture assets are not overwritten. Preview and actual in-game capture are included here.

The source visual contains 60,023 vertices across nine skinned renderers. The population cap, staggered spawn/path updates and offscreen animation culling reduce work, but mobile hardware still needs its own profiling and possibly lower LOD meshes or a smaller cap. No mobile performance claim is made from host tests.

## Combat / progression integration

Health now exposes a shared combat target registry, so Tab and AoE abilities can include real NPCs without relying on GameManager.Players. Existing Warrior and legacy class attacks share this target list. Mouse picking uses the native collider/body hitbox. Native kills update existing quests/achievements and grant definition-specific XP and local gold exactly once. Clearing a native pack grants the existing encounter reward and schedules its repopulation.

## Validation

- `build-validation.txt`: prefab isolation, animation selection, geometry/material inventory, Mirror registration and zone count.
- `validation.txt`: 41 real host checks (menu -> map -> independent spawn -> mouse/Tab targeting -> autoattack/cleave -> AI attack -> death -> XP/gold/quest -> corpse cleanup -> respawn/retirement).
- `spawn-validation.txt`: audit of configured map spawn points using NavMesh.SamplePosition and checkpoint safety.
- `preview.png`: native prefab preview with converted materials.
- `gameplay.png`: actual gameplay capture (test fixture HP).
- Logs: `Logs/LittleDemon-Final.log`, `Logs/LittleDemon-Spawn-Audit.log`.

Tests run through `ROM.EditorTools.LittleDemonBuilder.Run` or `RiftJournalProbe.RunNativeEnemies`; spawn-only audit is `LittleDemonSpawnAudit.Run`. Remote-client/late-join multiplayer and mobile-device performance have not been tested in this iteration. Mirror authority, registration and replicated state were validated in the local host session.
