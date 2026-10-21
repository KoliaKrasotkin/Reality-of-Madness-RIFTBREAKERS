# Riftbreakers — MMO controls and explicit targeting

## Play

Open `Assets/ROM/Scenes/MainMenu.unity`, select a character and enter El Rift. Controls apply to the native Warrior and the existing player input adapter used by other characters.

- Cursor is visible and free by default; pointing does not turn the camera.
- Hold left mouse: orbit the camera independently of character facing. A short click selects the object under the cursor; dragging does not select.
- Hold right mouse: steer the character and camera together. A short right click on an enemy selects it and starts basic auto attack.
- Hold both mouse buttons: move forward.
- Releasing a drag restores the cursor to its initial position. UI buttons remain clickable without holding Alt.
- W/S: forward/backward. A/D: turn in place. Q/E: strafe. While steering with right mouse, A/D strafe too.
- Space: jump. Num Lock: autorun (manual forward/backward cancels it). Numpad Divide: walking/running toggle. Shift allows running while walking mode is active. Backpedalling uses the slower speed.
- Mouse wheel: third-person zoom, limited to 2–14 m and still subject to camera collision.
- 1: basic attack/auto attack. 2: Cleaving Fury. 3: Earthbreaker. 4: Honored Roar. Slots 5–7 remain reserved for their unimplemented abilities.
- F: interact/talk/use nearby portal. H: potion. I: inventory. B: character. K: abilities. O (hold): squad. Enter: chat.
- Escape first clears the selected target/auto attack; with no selection it opens the existing pause menu.

## Targeting

Left-click selects an actual Health target through physics hitboxes, including native presentation roots detached from the network object. Selection persists when looking elsewhere; it is no longer chosen from the camera direction. Tab cycles visible hostile targets within 45 m, ordered by distance and stable network identity. Shift+Tab cycles backwards. Obstructed, dead, friendly or invalid enemies are excluded from cycling. F6 targets self. Dead/despawned/out-of-range selections are cleared.

The mouseover tooltip shows actual name, health and role only while hovering, independently of selection. Existing village NPCs also show their name, role and trading status. It hides over interactive UI, while dragging, during chat and in menus. The target frame and red markers follow the selected target rather than hover.

Target identity is sent through the existing Mirror-owned PlayerInstance AbilityController. The server validates existence and distance. Warrior basic attacks additionally require a living hostile selected target, reach, facing and line of sight before spending resources/cooldowns. A strike retains its initial target identity during the animation. AoE abilities retain their positional area/cone. Legacy directed class abilities also use explicit selection, reach, facing and visibility. Free-camera yaw is presentation-only and is not replicated as character facing.

## Rebinding

Main menu or pause > Ajustes > Controles. Scroll through Movement, Camera, Targets, Combat, World, Weapons and Interface. Each action has a primary and secondary binding. Click a binding, then press a key/mouse button/wheel direction. Ctrl/Shift/Alt combinations and individual modifiers are supported. Clear an assignment with ×. A conflict is reported rather than silently stealing another action's key. Camera-orbit/left-click-select and pause/clear-target intentionally share defaults.

Bindings save immediately in local PlayerPrefs `ROM.Controls.v1`; sensitivity/FOV retain the existing Apply/confirmation flow. Escape cancels capture without closing settings. “Teclas por defecto” restores the MMO bindings. Hotbar and interaction labels read the current assignments. The UI is migrated at runtime from the template's legacy StandaloneInputModule to InputSystemUIInputModule so actual pointer input and rebindings share the same Input System.

Conventional weapons keep configurable primary fire (1), aim (Z), reload (R), melee (V), drop (Y), previous weapon (X), and weapon slots (F1–F5). Native Warrior does not receive template gun slots. Developer-only editor noclip remains outside the gameplay binding menu.

## Implementation

- `Assets/MultiFPS/Scripts/Inputs/RiftControls.cs`: Input System actions, modifiers, defaults, conflict checks, save/load.
- `Assets/MultiFPS/Scripts/Inputs/RiftInputUI.cs`: UI Input System bridge.
- `Assets/MultiFPS/Scripts/Gameplay/RiftMMOController.cs`: independent camera, shared movement input, persistent selection, mouseover and cycling.
- Existing PlayerGameplayInput / WarriorAvatar / RiftThirdPersonCamera: route through the shared controller without restoring template rigging.
- Existing AbilityController and PlayerInstance.Classes: authoritative target validation.
- `Assets/ROM/Scripts/UI/RiftHub.Controls.cs`: binding settings and capture.
- Existing gameplay HUD and village UI: explicit selection, hover-only tooltip, live binding labels.
- Editable HUD prefab: `Assets/ROM/UI/GameplayHUD/Resources/RiftGameplayHUD.prefab`.

## Validation and limits

Run `ROM.EditorTools.RiftMMOProbe.Run` in Unity batch mode. The probe starts the actual main-menu-to-Mirror-host flow, supplies virtual Input System keyboard/mouse events and tests character movement, camera buttons, real raycast hover/click selection, cycling among two enemies, authority, cooldown/resource-safe rejection, binding capture/persistence/modifiers, chat blocking, wheel zoom, autorun and an actual UI button click. Test preferences/devices are restored afterwards. Latest local run: **38 checks passed**, no C# compilation errors or runtime exceptions. See `validation.txt`, `mouseover.png`, `bindings.png`, `target-selected.png`.

Validated on the local host with the native Warrior. Legacy target resolution is exercised on the actual PlayerInstance, but every legacy weapon/animation combination has not been replayed. No remote-client or mobile-device test was performed. Gamepad button assignment is supported by the binding capture; full analog movement/camera adapters and mobile touch MMO controls remain separate work. This implements the requested base control scheme, not every WoW interaction/target priority rule. The project still uses Mirror; it is not migrated to Photon Fusion.
