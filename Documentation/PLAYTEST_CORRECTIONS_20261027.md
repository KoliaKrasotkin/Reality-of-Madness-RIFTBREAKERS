# Correcciones y cobertura del playtest

## Navegación y Crelium

Home permanece accesible con roster vacío; Jugar requiere personaje válido. Volver/Cerrar, crear/cancelar, borrar recuperable/restaurar se probaron con registros aislados. Registro de conexiones valida identidad para evitar que la limpieza del personaje anterior elimine al nuevo al cruzar escenas. La salida de sesión es idempotente. Última resistencia cuesta0Furia exclusivamente en Crelium8/15, conservando cooldown y sin afectar Rugido o El Rift.

98 comprobaciones Host automatizadas de la ruta de15etapas y21 de muerte/reintento/salto/salida pasaron antes de la integración visual. Después:34 de UI/configuración,21 del Paladín y51 de regresión Warrior/UI/transición/mapa. No sumar estas cifras como recorridos manuales independientes ni afirmar multijugador remoto.

## Continuidad del nivel5

Se mantienen recompensas y personajes. La misión del Guardián requiere patrulla occidental avanzada; registros aceptados siguen existiendo. Guía/objetivos descartan destinos genéricos desproporcionados y muestran continuación válida. Patrulla inicial y fauna occidental6–8; segunda patrulla9–12; Guardián15 y regiones16–60 conservadas.

Cobertura real aislada:1337XP nivel5→nivel6→nivel7,3877XP tras cristal occidental y recuperación de suministros. Mezcla callbacks/interacciones servidor y movimiento por input virtual; hubo reconexión al campamento y limitaciones del auxiliar de navegación. Nivel9 NO fue validado por recorrido runtime continuo.

Continuación comprobada estáticamente, sin quitar prerrequisitos: tras pt20-west-crystal están Despejar el acceso(700XP), Signos en la piedra(800), Muestras de ceniza(900), Las sombras del acceso(1000). Las cuatro suman3400XP;3877+3400=7277 supera umbral5700 de nivel9, sin contar combates. El acceso está abierto(1100) exige además esas cuatro y pt20-west-salvage; después sigue entrenamiento. No se entregó XP artificial a personajes reales.

## Configuración editable

- Clases/prototipo: Resources/PaladinPrototypeSettings.asset y Characters/PaladinPrototype/*.asset/prefab/controller; ver PALADIN_PROTOTYPE.md.
- Habilidades Warrior: definiciones AbilityDefinition/Specialization/Weapon existentes bajo Gameplay; no activar la antigua clase Paladin de pistola.
- Misiones: Progression/Quests/*.asset, incluidos prerrequisitos adicionales y recompensas.
- Zonas: componentes RiftEnemySpawnZone del mapa, niveles y puntos de aparición.
- UI y mapa: Resources/UI/RiftPresentationSettings.asset; ver EDITABLE_PRESENTATION_RIFT.md.
- Tutorial: Tutorial/Crilium/Resources/Crilium/Definition.asset, metas/textos y referencias; sus escenas mantienen objetos editables.
- Cristales: Art/RiftCrystals/Materials, componentes RiftCrystalRadiance y luces de escena Solar bounded violet spill / Checkpoint violet spill. HDR/emisión/pulso son locales, DirectionalLight original se conserva.
- Vegetación y pisos: ver CRELIUM_DRESSING.md y prefab Dressing/CreliumFoliageLayer.

Los ajustes de presentación se guardaron/reimportaron; el runtime lee los valores y no los sobrescribe con defaults. Esto no convierte toda lógica de red/persistencia en programación visual.

## Límites conocidos

Cierre nativo intermitente-1073741819 después de QA sigue sin resolver; no equivale a un PASS de estabilidad del editor. Hay warnings preexistentes de colliders negativos SunTemple y plantillas ShaderGraph. Sin build de juego, benchmark WebGL o prueba Mirror remota. Guardado portátil/backend siguen pausados. Se preservaron22 archivos originales y1697 preferencias del usuario; sólo contabilidad de sesiones Unity cambió.

Cierre focalizado:18 comprobaciones de reapertura/materiales emisivos, ID Mirror independiente del prototipo y guardas de skin, sin fallos. El cierre nativo volvió a fallar después de completarlas. Algunas capturas aisladas muestran una cola Warrior estirada; su prefab/rig y animaciones de Gori se conservaron, sin atribuir ni declarar resuelto ese defecto visual.
