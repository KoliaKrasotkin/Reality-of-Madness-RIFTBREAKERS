# Aldren — articulación de manos

Corrección local del modelo de prueba: cinco dedos separados por mano, tres articulaciones por dedo, pesos distales normalizados y pose de agarre de espada. No cambia el kit Fe/Rezar, daño, clases o partidas.

## Edición

- Modelo usado: `Assets/ROM/Characters/PaladinPrototype/Models/Aldren_ArticulatedHands_BIND.fbx`. Humanoid con 52 correspondencias (22 corporales + 30 de dedos). El FBX original se conserva intacto como respaldo.
- Material corporal original: `PaladinIvoryBlue`. Guante nuevo: `Materials/AldrenArticulatedGlove.mat`, URP Lit.
- `PaladinVisual.prefab` sirve al preview; `PaladinPrototypePlayer.prefab` contiene además su propio modelo embebido. Ambos fueron actualizados conservando sus GUIDs y componentes originales.
- En cada modelo, `AldrenWeaponGripPose` expone treinta referencias y rotaciones locales: quince del agarre derecho y quince de la mano izquierda relajada, junto con `GripWeight` (0 deja la pose fuente; 1 aplica la corrección). La izquierda evita que el retargeting cruce los dedos; el agarre derecho se conservó exactamente. Se ejecuta después del Animator, sin modificar muñeca ni cuerpo. El controlador `PaladinLocomotion.controller` permanece intacto. No usa una capa de músculos que reinterprete las articulaciones.
- El guante reutiliza detalle del cuero/tejido del atlas original, redistribuido en UV nuevas con densidad uniforme. Textura: `Models/textures/AldrenHands_OriginalAtlasTransfer.png`; material: `AldrenArticulatedGlove.mat` (URP Lit, parámetros de `PaladinIvoryBlue`). El cuerpo y su atlas original permanecen intactos.
- La posición/rotación de la espada se edita en `PaladinPrototypePresentation` del prefab jugable. Son ajustes locales de Aldren; no se modificó el prefab compartido WarriorSword.
- Fuente Blender editable y pose de referencia: carpeta de trabajo `aldren-hands-fix-20261028/candidate`. No guardar un `.blend` directamente sobre el FBX: exportar una copia, conservar escala y correspondencias y revisar Humanoid antes de sustituir.

## Validación realizada

Reimportación FBX en Blender: 63 huesos, máximo cuatro influencias, ningún vértice sin peso. Matrices de los 33 huesos originales conservadas.

Unity: Avatar válido/humano, 30 huesos de dedos, 63 referencias de renderer completas y quince juntas de agarre asignadas. Se evaluaron los clips actuales Idle, Walk y Attack0 a tres tiempos distintos mediante el Animator/PlayableGraph real en una escena de preview aislada. Se renderizó `BakeMesh` de cada pose para evitar caché GPU entre muestras. Se inspeccionaron imágenes de cuerpo, dorso y palma. La prueba no es un recorrido manual de gameplay ni validación multijugador; no se entró a Play sobre la partida del usuario.

El acabado sigue siendo de prototipo: guantes sencillos y transición angular bajo la armadura de muñeca; ropa y cuerpo fusionados originales no se retopologizaron. No se afirma calidad AAA ni ausencia de toda intersección posible en animaciones no revisadas.

## Conservación

MainMenu tenía cambios sin guardar de origen no atribuido. Se canceló el cierre y se respaldaron la versión en disco y la escena en memoria mediante SaveScene(saveAsCopy:true), sin sobrescribir el original. Las copias están en la carpeta de trabajo y `Assets/_Recovery/AldrenHands20261028/`; no publicar `_Recovery`.

No se crearon/eliminaron personajes, no se restauraron partidas ni preferencias y no se modificaron assets del kit. Sin commit ni push en esta tanda. La foto original del reporte no pudo materializarse por la restricción previa; esta corrección se basó en el modelo comprobado y capturas aisladas reales.
