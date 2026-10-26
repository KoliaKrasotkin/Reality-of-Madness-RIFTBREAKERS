# Rift crystal prototypes

Two independent art prefabs for Unity 6000.3 / URP:

- `Prefabs/CrystalGround.prefab`: floating mineral cluster and basalt base.
- `Prefabs/CrystalAerial.prefab`: floating crystal and three shards, without base, studio floor, lights or cameras.

Open `Preview/RiftCrystalsPreview.unity` to inspect them. The game preloader redirects an ordinary Play start to MainMenu; use the Animation window to preview the imported `LevitatingLoop`, or load this preview after startup. The prefabs' legacy Animation components play the six-second loop automatically when instantiated in a running scene. No gameplay behavior, colliders, networking or crystal interaction was added.

## Export

Source: the project's own `rift_crystal_v2.blend` and `rift_aerial_sun_v1.blend`, preserved outside Assets. FBX export uses meter scale, Y up, -Z forward, evaluated bevel geometry, and object animation sampled at 24 fps. Four independent animated objects remain. Static geometry sharing a parent and material was merged. The inclusive loop endpoint repeats the start after six seconds; import takes retain their actual start frame.

Ground: 4,766 exported triangles and 22 mesh objects. Aerial: 1,472 triangles and 12 mesh objects. These are prototype counts, not a measured WebGL performance budget or production optimization claim.

## Materials

Materials use URP/Lit: alpha-blended amethyst shells and internal cleavage, controlled emission for the core, opaque basalt. Blender Cycles transmission/refraction, procedural surface noise, compositor glow and studio lighting are not reproduced physically. Internal fracture geometry is retained; apparent transparency depends on lighting and normal transparent-object sorting limitations. No scene-wide rendering options or sunlight were changed.

For placement, move the prefab root. Keep the imported child pivots and Animation paths intact. Both prefabs preserve original source pivots and relative dimensions. They are not wired to existing checkpoint functionality and do not replace any world asset automatically.
