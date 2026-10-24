# ROM: Riftbreakers

Proyecto de Unity para **Reality of Madness: Riftbreakers**. La experiencia actual se centra en Nertör, un Warrior, y la exploración de **El Rift**: islas conectadas por puentes, refugios, enemigos, misiones y progresión.

Este repositorio contiene el juego, herramientas de Editor y numerosos paquetes de terceros. Es un proyecto en desarrollo: una prueba local en modo Host no demuestra que el multiplayer remoto, la persistencia online o una distribución WebGL estén listos.

## Entorno del proyecto

| Componente | Versión / ubicación verificada |
| --- | --- |
| Unity Editor | **6000.3.23f1**, según `ProjectSettings/ProjectVersion.txt` |
| Render pipeline | URP **17.3.0** |
| Networking | Mirror **96.0.1**, incluido en `Assets/Mirror` |
| Input System | **1.20.0** |
| Animation Rigging | **1.4.1** |
| AI Navigation | **2.0.14** |
| uGUI | **2.0.0** |
| Unity Test Framework | **1.6.0** |

Las dependencias de Unity se declaran en `Packages/manifest.json` y se fijan en `Packages/packages-lock.json`. Usar la versión indicada del Editor para evitar migraciones y reserializaciones involuntarias. Los módulos de otras plataformas se preparan cuando corresponda probar esas plataformas.

## Primer inicio

1. Instalar Git, Git LFS y Unity Hub con Unity **6000.3.23f1**.
2. Obtener acceso al repositorio y a los assets que el equipo utiliza.
3. Clonar y descargar los objetos LFS antes de abrir Unity:

   ```sh
   git lfs install
   git clone https://github.com/Practicas-Laborales-UTN-IDS/Reality-of-Madness-RIFTBREAKERS.git
   cd Reality-of-Madness-RIFTBREAKERS
   git lfs pull
   git lfs ls-files
   ```

4. En Unity Hub, agregar la carpeta que contiene `Assets`, `Packages` y `ProjectSettings`.
5. Esperar a que terminen los imports y la compilación. Si aparece Safe Mode, revisar primero los errores de Console; no continuar como si hubiera compilado correctamente.
6. Abrir **`Assets/ROM/Scenes/MainMenu.unity`** y pulsar **Play**.
7. Usar **Jugar** y la confirmación para iniciar la ruta local hacia **`Assets/ROM/Levels/map_rom_horde.unity`**. El menú también conserva el acceso de conexión por dirección y puerto.

No es necesario generar un player build para esta prueba en Editor. No abrir escenas de demostración de los paquetes pensando que son el punto de entrada de Riftbreakers.

Si un modelo, imagen o escena contiene texto que empieza con `version https://git-lfs.github.com/spec/v1`, sigue siendo un puntero LFS: faltan sus bytes reales. Comprobar LFS antes de atribuir el fallo a Unity. Ante un error de TerrainData o serialización, conservar el archivo y su log; no borrar assets ni reimportar todo el proyecto a ciegas.

## Controles básicos

Los controles se centralizan en `Assets/MultiFPS/Scripts/Inputs/RiftControls.cs` y pueden reasignarse en Ajustes. Estos son los valores por defecto, no una sustitución de las preferencias guardadas:

| Acción | Control |
| --- | --- |
| Movimiento | W/S; A/D para girar; Q/E para desplazamiento lateral |
| Cámara | Arrastrar con el ratón; rueda para zoom |
| Saltar / correr | Espacio / Shift |
| Seleccionar / recorrer enemigos | Clic / Tab; Shift+Tab para anterior |
| Seleccionarte | F6 o clic en el retrato |
| Autoataque / barra de habilidades | T / 1–7 |
| Interactuar | F |
| Inventario | I o B durante gameplay |
| Habilidades / talentos | K / P |
| Diario / logros | J / U |
| Mapa / menú | M / Escape |
| Pociones | H / Shift+H |

El acceso Personaje del menú inicial corresponde a la selección de personaje/skin; durante gameplay se reutiliza el inventario. Las técnicas tienen requisitos de nivel, recurso y recarga. Que una técnica aparezca en una barra guardada no evita su validación de uso.

## Organización del repositorio

| Carpeta | Responsabilidad |
| --- | --- |
| `Assets/ROM/Scenes` | Punto de entrada y escenas propias de interfaz |
| `Assets/ROM/Levels` | El Rift, navegación y contenido de escenario propio/integrado |
| `Assets/ROM/Characters/Warrior` | Prefabs, controlador y presentación del Warrior |
| `Assets/ROM/Combat` | Definiciones de habilidades, especie, armas y configuración de combate |
| `Assets/ROM/Scripts/UI` | Menú, inventario, habilidades, talentos, diario, NPC, social y transiciones |
| `Assets/ROM/Scripts/UI/GameplayHUD` | Barras, targeting visual, minimapa, mapa completo, tooltips y alertas |
| `Assets/ROM/UI` | Prefabs, fuentes, iconos y recursos gráficos de interfaz |
| `Assets/ROM/Scripts/Progression` | Misiones, objetivos, snapshots y progresión |
| `Assets/ROM/Scripts/Horde` | Exploración, checkpoints, encuentros y comportamiento del mundo |
| `Assets/ROM/Resources` | Configuración y recursos cargados por nombre, incluido el atlas/proyección del mapa |
| `Assets/ROM/Editor` | Herramientas de edición, revisión y pruebas del proyecto |
| `Assets/MultiFPS` | Base del template y adaptaciones de red, jugador, personaje, salud e input |
| `Assets/Mirror` | Código y transports de Mirror; incluye ejemplos ajenos al juego principal |
| `Assets/Standard Assets`, `Assets/LeartesStudios`, `Assets/DoubleL`, `Assets/StoneGolem` y otros packs | Contenido y dependencias de terceros; su presencia no significa que todo esté usado en El Rift |
| `Packages`, `ProjectSettings` | Dependencias y configuración compartida de Unity |
| `Tools`, `.githooks`, `.github/workflows` | Validación de escenas y automatización Git/CI |
| `Documentation` | Diseño, auditorías, evidencias y documentación técnica; consultar fecha y alcance de cada documento |

**MultiFPS sigue siendo una dependencia real.** Entre sus componentes activos están `DNNetworkManager`, `PlayerInstance`, `CharacterInstance`, `Health` y las adaptaciones `RiftMMOController` / `RiftCombat`. Mirror es el networking; eso no vuelve prescindible el resto del template. No eliminar carpetas completas por contener demos sin revisar referencias, GUID, cargas por `Resources` y uso indirecto.

## Sistemas y límites actuales

- **Combate:** movimiento y targeting local con comandos/validación de servidor; habilidades y rage del Warrior, autoataque, recargas, talentos y progresión. El evento de animación `Hit` es de presentación: no debe aplicar nuevamente el daño que ya resuelve el servidor.
- **Identidad:** el nombre visible de una skin y el `CharacterId` son conceptos distintos. No renombrar IDs, claves de guardado o GUID al cambiar textos visibles.
- **Inventario y equipo:** existen datos/presentación de provisiones y equipo. Preservar las validaciones de cantidades, uso y servidor. Las guardas de comercio/cartera de clientes remotos no deben retirarse para simular que la economía online está terminada.
- **Misiones:** hay definiciones, objetivos, seguimiento, marcadores, crédito y progresión. La infraestructura no garantiza que todos los recolectables estén colocados, que todo el contenido esté terminado ni que recompensas y persistencia hayan pasado una prueba remota completa. Distinguir oro local de una cartera autoritativa online.
- **Mapa:** el arte, la proyección y la geografía del mundo se calibran juntos. `Assets/ROM/Resources/RiftWorldMap/Projection.json` define el mapeo; no sustituir una imagen con otra de distinta composición sin recalibrar posiciones, puentes y clicks. No cambiar la escena para ocultar una desalineación gráfica.
- **HUD:** barras, objetivos self/friendly/hostile, controles compactos, tooltips y alertas locales. Los títulos de zona se basan en las superficies reales de las islas. Las habilidades actuales no incluyen una clase healer.
- **Social:** contactos/mensajes y datos locales no equivalen a un servicio online. No identificar automáticamente a todos los jugadores o miembros de un equipo como una party; falta cerrar el contrato autoritativo de grupo y posiciones.
- **Red:** `DNTransportImplementer` contiene selección KCP / SimpleWebTransport. Verificar la configuración efectiva del entorno antes de conectar instancias. Un Host local no valida un servidor dedicado, un cliente WebGL remoto ni interés/AOI entre clientes separados.
- **Servicios futuros:** Google/guest linking, integración ICP/backend, creación visual futura de personajes y persistencia online completa no deben mostrarse como implementados por existir propuestas o interfaces de prueba.

## Pruebas y validación

Para comprobar las escenas propias del commit sin abrir Unity:

```sh
python Tools/validate_unity_scenes.py --revision HEAD
```

Para habilitar el guard local desde PowerShell:

```powershell
.\Tools\Install-GitValidation.ps1
```

El validador requiere Python 3.8+ y los objetos LFS de las escenas. Ver [Tools/README.md](Tools/README.md) para comprobar el índice, configurar la ruta local de Python y descargar únicamente las escenas necesarias. El CI **Unity scene integrity** ejecuta esta validación en pushes/PR; no compila el juego ni certifica UI o multiplayer.

Hay probes y revisiones específicas en `Assets/ROM/Editor`. Revisar su código antes de ejecutarlas: algunas entran en Play, crean fixtures, inician un Host local o cierran su instancia del Editor al finalizar. Usar perfiles temporales, puertos separados y una instancia diagnóstica identificada; no ejecutarlas en una sesión manual con trabajo sin guardar. Los scripts temporales de una revisión no forman necesariamente parte del commit publicado.

La revisión local del 23/10/2026 registró 107 comprobaciones del bloque targeting/HUD en el proyecto integrado, con resoluciones 1920×1080 y 1440×1080. Al realizarse, este bloque era un cambio del working tree: confirmar el commit que lo publica antes de atribuirlo a `main`. Es evidencia del estado probado, no una garantía para futuras revisiones ni una prueba de dos clientes remotos. Quedan QA visual adicional y advertencias `JobTempAlloc` al cerrar el Editor. Los resultados y capturas locales se guardan en `Logs`, carpeta excluida de Git.

No se debe dar por completada una entrega multiplayer hasta probar cliente y servidor separados, conexión/desconexión, autoridad, sincronización, progresión y persistencia. Builds, hosting, optimización y cleanup son etapas separadas; no se ejecutan por el solo hecho de abrir o probar la UI.

## Trabajo en equipo y Git

1. Revisar rama, upstream y `git status` antes de editar o sincronizar. Hacer fetch y examinar commits entrantes; un commit remoto pendiente y un pull request son cosas distintas.
2. Trabajar en ramas de propósito acotado y revisar por PR cuando corresponda al flujo acordado por el equipo. Publicar directamente en `main` solo cuando se haya acordado explícitamente ese conjunto.
3. Coordinar cambios a escenas, prefabs, animator controllers, rigging, `ProjectSettings` y build settings. No hacer pull/merge mientras otra persona está probando o guardando esa misma copia en Unity.
4. Conservar cada `.meta` junto a su asset. Los assets de imagen/modelo/audio y las escenas usan LFS según `.gitattributes`; los TerrainData/NavMesh binarios requieren sus reglas específicas. No normalizar sus saltos de línea como si fueran YAML.
5. No resolver escenas o prefabs eligiendo globalmente “ours/theirs”. Conservar ambas intenciones y verificar en Unity. No usar force push, reset destructivo, borrado de pendientes o `git add .` como sustituto de revisar los archivos.
6. Revisar reserializaciones/imports antes de incluirlos. Los cambios automáticos no son permiso para descartar trabajo. No compartir `Library` escribible entre dos Editors simultáneos ni commitear cachés, logs, credenciales o fixtures temporales.

Consultar [GIT-SETUP.md](GIT-SETUP.md) para la configuración local. **La autoridad sobre los tipos de archivo es `.gitattributes` vigente:** actualmente `*.unity` usa LFS y `merge=binary`; algunas explicaciones anteriores de `GIT-SETUP.md` mencionan YAML merge para escenas y no reflejan esa regla actual.

## Assets y licencias

El repositorio reúne código y assets con licencias diferentes. Mirror conserva su [licencia MIT](Assets/Mirror/LICENSE). Mantener los avisos y archivos de licencia de cada paquete. El acceso a este repositorio no sustituye las licencias necesarias de Unity/Asset Store ni concede permiso para redistribuir assets de terceros de forma independiente. Antes de publicar el juego o compartir contenido fuera del equipo, revisar las condiciones aplicables a cada recurso; no asumir que todo el repositorio tiene una única licencia.

## Documentación relacionada

- [Historial de cambios](CHANGELOG.md)
- [Frontend de El Rift](Documentation/EL-RIFT-FRONTEND.md)
- [Exploración](Documentation/EL-RIFT-EXPLORATION.md)
- [Movimiento](Documentation/EL-RIFT-MOVEMENT.md)
- [Misiones y progresión](Documentation/quest-system/README.md)
- [Presentación de personajes](Documentation/CHARACTER-PRESENTATION.md)
- [Validación de escenas](Tools/README.md)

Los documentos históricos y capturas describen su propia revisión. Ante diferencias, contrastar el código/configuración actuales, el SHA compartido y una prueba real; una captura de una copia de trabajo puede contener cambios todavía no publicados.
