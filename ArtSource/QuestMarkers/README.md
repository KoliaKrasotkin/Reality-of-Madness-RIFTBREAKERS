# Riftbreakers quest marker art

Four Blender-authored silhouettes share their 3D and transparent UI versions:

- `Available`: amber crystal exclamation mark and diamond dot.
- `InProgress`: silver question mark and diamond dot.
- `TurnIn`: gold question mark, diamond dot and lower delivery chevron.
- `Tracked`: violet open diamond with a central crystal.

Authoring: `ArtSource/QuestMarkers/RiftQuestMarkers.blend` and `create_markers.py`.
Exports: `Assets/ROM/Art/QuestMarkers/Models`.
Editable URP materials: `Assets/ROM/Art/QuestMarkers/Materials`.
Runtime prefabs: `Assets/ROM/Resources/RiftQuestMarkers/World`.
Transparent 512px UI sprites: `Assets/ROM/Resources/RiftQuestMarkers`.

`ROM > Art > Reimport quest marker prefabs` rebuilds the prefabs from the FBX exports and assigns URP materials. It normalizes FBX orientation and sets a 0.8m visual height. It does not place objects or save scenes. Back up deliberate material/prefab edits before rebuilding.

NPC state priority is ready to deliver, available, then in progress. The presentation resolver reads the current journal; it does not accept quests or grant progress/rewards. Map grouping preserves ready/objective/available priority, with in-progress below available. The offscreen guidance arrow remains directional; only the onscreen tracked diamond uses the violet art.

Marker models have no colliders. Decorative UI Images do not receive raycasts; existing pin hit targets, tooltip behavior, geographic positions and 20/25px footprints remain. No changes to quest definitions, balances, profiles, or HUD layout are required.

Validation on 2026-10-26: isolated Unity Editor/Play presentation, in-memory journal state transitions, sprite import, URP shader support, bounds, removal after state changes, geographic positioning under movement, and light/dark backgrounds. This is not a complete Host campaign, remote multiplayer test, or measured platform performance benchmark. Blender and Unity screenshots are retained separately from production assets.
