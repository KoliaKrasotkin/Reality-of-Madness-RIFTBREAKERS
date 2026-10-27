# Presentacion compartida: configuracion de esta tanda

Asset: `Assets/ROM/Resources/UI/RiftPresentationSettings.asset`.
Seleccionarlo en Unity para ajustar zoom minimo/maximo, tipografia, colores, anclajes del mapa, posiciones de HUD y tamano/fuentes/botones del tutorial. Los valores se leen del asset; los defaults en codigo solo sirven si falta el recurso. `Keep Authored HUD Layout` conserva el layout del prefab del HUD en lugar de modernizarlo al arrancar. `Keep Authored Map Layout` conserva el panel de mapa authored. Mantener referencias de componentes y botones funcionales.

Misiones: `Assets/ROM/Progression/Quests/` (prerrequisitos incluidos los adicionales, objetivos, XP). Zonas/spawns/rangos: componentes `RiftEnemySpawnZone` de `Assets/ROM/Levels/map_rom_horde.unity`; no confundir el nivel de isla con cada patrulla. No cambiar el cap global para editar una zona. Clases/habilidades y prefabs mantienen sus assets existentes. Esta configuracion no convierte la logica de red/persistencia en programacion visual.

Validacion de esta fase: 34 comprobaciones aprobadas, sin fallos, en Host local aislado automatizado. Incluye ambas escenas, mapas 16:9 y 4:3, limites de zoom/pan, cierre y restauracion, guardado y reimportacion de parametros, transicion y radiancia. Capturas reales inspeccionadas. No equivale a recorrido manual integral ni a multijugador remoto.

Iluminacion: editar materiales en Assets/ROM/Art/RiftCrystals/Materials y componentes RiftCrystalRadiance del mapa. Halo Gain/Pulse ajustan halo; las luces Solar bounded violet spill y Checkpoint violet spill conservan intensidad/rango editables y no son sobrescritas por el pulso. DirectionalLight original permanece activa. Se corrigio solo la altura visual de bases, conservando triggers, colliders y respawns. No se midio rendimiento WebGL.

El material terrestre usa ahora ROM/Rift Crystal Ground Radiance, con _EmissionColor HDR y PulseDepth explícitos; conserva niebla local y no depende de keywords Lit. Se probó guardado, reimportación, reapertura y superficie real en Play. Las luces de spill siguen siendo componentes independientes editables.
