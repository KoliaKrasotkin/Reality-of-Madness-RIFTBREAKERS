# Paladin and mage playable entry

Playable class integration, originally based on d4d3a00a; publication history is recorded in Git.

From Assets/ROM/Scenes/MainMenu.unity: Play > Personajes > Crear personaje > PALADIN or MAGO > Crear y seleccionar > Probar en El Rift (confirm host).

Paladin retains the existing Faith prototype. Mage uses the team's existing MagePlayer and skin/controller, a dedicated prototype class, and an authoritative ranged basic attack (25 m). Full mage abilities and talents are not claimed by this change. Warrior remains unchanged.

Character records preserve their IDs; no existing saves were converted. New mage creation validates class/skin pairs. Native prefabs are registered with Mirror and previews remove gameplay/network behaviours before activation. Catalog preview lookups use SkinIndex, not array positions.

Validation: Unity 6000.3.23f1 batch compilation passed. ROM.EditorTools.PlayableClassesValidation.Run passed 29 isolated asset/roster checks (validation.txt). git diff --check passed. End-to-end graphical host smoke tests passed for mage and paladin using a hidden Unity graphics process and synthetic in-engine keyboard (not OS input): authored creation tab, confirm/create, roster selection, host confirmation, owned network spawn, correct class/skin, humanoid renderer, movement and return to idle. See skin-2-play.txt / skin-3-play.txt and creation/spawn/moving/idle PNG captures. Captures were visually inspected.

QA caught and fixed two runtime issues: dependency ordering when stripping gameplay from previews, and first class initialization outside protected zones (subsequent respec restrictions remain). Mage HUD now names Mana rather than Fury.

Not tested: damage against enemies, full mage skill kit, remote clients, WebGL build. Existing scene warnings (negative-scale BoxColliders, repeated ground-loot spawn-handler registration) are outside this change. User preferences are restored and the test character roster is removed at test completion. No scene/prefab reserialization or model changes.
