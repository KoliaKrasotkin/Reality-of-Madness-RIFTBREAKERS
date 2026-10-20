# Riftbreakers — gameplay HUD

Implemented as native UGUI controls, custom masked vector frames, live Text/Image/Button components and client-only target indicators. The four painted ability icons are separate generated art assets; no full-screen image overlay is used.

## Open and edit

- Start from `Assets/ROM/Scenes/MainMenu.unity` and enter El Rift.
- Scene: `Assets/ROM/Levels/map_rom_horde.unity`, root **Rift Gameplay HUD** (prefab instance).
- Editable prefab: `Assets/ROM/UI/GameplayHUD/Resources/RiftGameplayHUD.prefab`.
- Theme: `Assets/ROM/UI/GameplayHUD/RiftHUDTheme.asset`.
- Rebuild: **ROM > UI > Reconstruir HUD de gameplay**.

1448×1086 reference canvas, height scaling, anchored corners, compact bottom hotbar. Verified captures also at 1920×1080. Title and body fonts, native portrait and individual icons are replaceable assets.

## Connections and controls

- Safe zone and checkpoint: existing Horde exploration state.
- Local gold: existing exploration wallet (never Ancient Gold).
- Player and target health, dummy last/total damage: existing replicated Health.
- Warrior name stays fixed as the class; the visible character name comes from the selected character identity.
- Rage, availability and cooldowns: existing AbilityController / Mirror NetworkTime. Unimplemented abilities remain locked. No gameplay balancing values changed.
- Q / left mouse: basic attack. F: cleave. 2 / Z: Earthbreaker. T: roar. The HUD adds Q and 2 aliases and keeps existing controls.
- I: current inventory/equipped weapon and potion counts. B: character. K: actual ability definitions. Escape: existing pause and settings. Hold left Alt to access HUD buttons with the mouse; native buttons also accept touch.
- Small red ring and diamond are local presentation only. Soft selection follows the forward facing direction within 45 m and does not change authoritative melee hit detection.
- Level uses the configurable starting value 1: no level/XP progression system was added.
- The portrait currently represents Nertör, the requested Warrior prototype; extra character portrait assets can be assigned to the theme.
- Minimap renders the real environment to a 256×256 texture at 4 Hz, with shadows/postprocessing disabled on its private camera. No environment lighting is modified. Device performance has not been benchmarked.

## Preservation and validation

The project actually uses **Mirror**, not Photon Fusion 2. Existing Mirror ownership, health, resource and damage authority are preserved. Validation runs a local Mirror host; a second remote client and mobile hardware have not been tested.

Scene block comparison against the previous Git LFS version: no existing objects removed; existing geometry, transforms, characters and lighting unchanged. Only the old orange Training circle renderer is disabled, the scene-root list receives the HUD prefab, and one prefab instance is added. Existing legacy HUD and closed-chat presentation are hidden at runtime to avoid overlap. NPC interaction dialogs remain available on demand.

Run `ROM.EditorTools.RiftGameplayHUDProbe.Run` in Unity batch mode to regenerate and exercise the real menu-to-host flow, bindings, combat damage/resource/cooldown UI, target acquisition/removal, and pause-page shortcuts. Results: `validation.txt`; captures: `hud-1448x1086.png` and `hud-1920x1080.png`. Existing map collider warnings remain outside this HUD task.
