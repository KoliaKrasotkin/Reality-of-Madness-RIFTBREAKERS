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

## Estado del playtest y publicación

La referencia publicada para esta revisión es el [checkpoint `f0f8ccb`](https://github.com/Practicas-Laborales-UTN-IDS/Reality-of-Madness-RIFTBREAKERS/commit/f0f8ccb6150940d66c3c1e4a8d74219da6644698): contiene la tanda de gameplay, niveles y campaña del playtest. No es un release descargable ni evidencia de un ejecutable generado. Las correcciones posteriores de paneles, coleccionables individuales, HUD, balance, reaparición, versión visible y validación de talentos se agrupan en una publicación separada. Esta referencia histórica no sustituye comprobar si esa publicación y cualquier cambio local están en el checkout actual:

```sh
git log -1 --oneline
git status --short
git diff --stat
git diff --cached --stat
```

El README describe las rutas presentes en la copia de trabajo. Al consultar otro commit, comprobar qué assets y correcciones contiene; una captura o un resultado local no garantiza que esté en `main`. La versión visible se configura por separado del SHA de Git y de una distribución del juego.

- **Jugador:** cap activo **20**, con **19 puntos de talento**; máximo de diseño **60**. No se concede nivel automáticamente. La curva acumula **63.175 XP** para alcanzar nivel 20 y no almacena excedente al cap.
- **Mundo:** zonas/enemigos hasta **60**, accesibles para jugadores de nivel inferior; el peligro procede de sus estadísticas y combate. El refugio inicial conserva su perímetro protegido. Elevar el cap del jugador a 30/60 es una etapa futura, no el contenido activo de esta prueba.
- **Campaña:** catálogo de **35 misiones** (cuatro originales y 31 añadidas), con combate, exploración y recolección. Hay pruebas por tramos y de recibos/recompensas; no equivalen a repetir manualmente toda la campaña con clientes remotos.
- **Talentos:** árbol inicial del Warrior con requisitos de nivel, padres y rangos. El árbol completo hasta 60 queda pendiente; aumentar un cap no crea talentos nuevos.

## Sistemas y límites actuales

- **Combate y equipo:** `AbilityController` resuelve daño autoritativo, recursos, autoataque y habilidades; el daño usa el arma y el bonus de la habilidad, con modificadores posteriores. Rango de daño y velocidad del arma producen un DPS derivado, no daño extra sumado. El evento de animación `Hit` es presentación: no debe aplicar daño otra vez. Conservar rigs y animaciones al cambiar datos de combate.
- **Loot y durabilidad:** oro común separado de Ancient Gold; tablas encadenadas para probabilidad de equipo, objeto ponderado, rareza y nivel. El catálogo de loot actual se limita al hacha de Nertör. El nivel del objeto se basa en el receptor con la política configurada, no se infiere automáticamente del nivel enemigo. Recoger y equipar tienen validaciones distintas. La durabilidad de esta etapa cubre hachas; no dar por terminada la de espada/escudo.
- **Inventario y comercio:** inventario de hachas, requisitos de equipo, vendedor NPC, reparación e intercambio entre jugadores. El intercambio actual transfiere objetos, **no oro**. Tienda de skins y Casa de subastas son accesos distintos de los vendedores NPC; sus servicios demo no son un mercado online operativo.
- **Misiones:** diario, objetivos, seguimiento, pines de mapa M/minimapa y recibos de entrega. El servidor valida aceptación, recolección y recompensa. Las correcciones locales hacen los recolectables individuales y evitan seleccionarlos como blancos de combate. No asumir reparto de kills entre party: las bajas se acreditan al atacante y los encuentros a jugadores cercanos según las rutas actuales.
- **HUD y ventanas:** barras, player/target frames, retratos dinámicos, tooltips, micromenú, XP y seguimiento. La revisión local conserva HUD fijo y paneles laterales no modales, con captura de clics de UI y bloqueo de acciones mientras se escribe. Parte del layout se genera en runtime: no todo es un prefab editable visualmente en Edit Mode.
- **Mapa:** el arte, la proyección y la geografía se calibran juntos. `Assets/ROM/Resources/RiftWorldMap/Projection.json` define el mapeo; no sustituir la imagen por otra composición sin recalibrar posiciones, puentes y clics. El desplazamiento de minimapa reportado permanece abierto sin reproducción concluyente; no está declarado resuelto por tener capturas estáticas alineadas.
- **Chat y party de sesión:** Mirror distribuye chat de proximidad, canal party y burbujas a destinatarios autorizados; buffer de mensajes efímero sin historial persistente. La party servidor admite hasta cuatro miembros y el modo PvE limita la sesión a cuatro. El social local anterior no es la autoridad del grupo. PvP 4 contra 4 y voz son trabajo futuro.
- **Red:** `DNTransportImplementer` contiene selección KCP / SimpleWebTransport. Verificar la configuración efectiva antes de conectar. Host local, participantes simulados y dos clientes completos son alcances de prueba diferentes.

### Autoridad, guardados y cooperativo pendiente

El flujo de gameplay parte de `PlayerInstance`/`CharacterInstance` y `AbilityController` en MultiFPS; los comandos Mirror pasan a `RiftJournalPlayer`, que persiste el diario y envía snapshots al propietario. `RiftJournalView` es una caché de presentación del cliente, no una orden para conceder recompensas.

Los diarios del servidor están en `Application.persistentDataPath/RiftJournals`, por ID estable de personaje. El oro común y las pociones todavía usan la cartera local de PlayerPrefs, con puentes y recibos específicos. Por eso **vendedor, reparación, consumo de pociones y entrega de quests bloquean actualmente al cliente remoto**. El oro de encuentros también conserva una ruta de crédito local. No retirar esas guardas sin migrar saldo, stacks y operaciones a una fuente de verdad por jugador en el servidor.

El cliente envía el `CharacterId` del prototipo y todavía falta cerrar su vínculo autorizado con la conexión; un GUID válido no demuestra propiedad de un guardado. No asumir protección frente a clientes modificados ni frente a un Host que controla archivos/proceso. Cambiar de Host no transfiere automáticamente el progreso. Para este playtest se decidió conservar el guardado local en la computadora de cada jugador; la adaptación cooperativa de esa persistencia todavía está pendiente. También está en cola compartir crédito de bajas de misión entre miembros de party cercanos, manteniendo recolección individual; no implica compartir oro o XP. El radio y las validaciones requieren implementación y pruebas. El intercambio de oro sigue sin habilitarse. Estas decisiones no se implementan por documentarlas aquí.

Google/guest linking, ICP/backend, fuentes de Ancient Gold y persistencia online global siguen diferidos. Ningún servicio externo es requisito de esta prueba local. No incluir saldos de demo como moneda premium real ni mostrar compras simuladas como transacciones publicadas.

## Dónde configurar y editar

Editar los assets en Inspector y revisar su diff; mantener sus `.meta`. Los valores de balance son provisionales. Los nombres de campos y las referencias del asset/código vigente tienen prioridad sobre capturas y guías históricas.

| Área | Datos / punto de edición |
| --- | --- |
| Menú inicial | [`MainMenuLayout.asset`](Assets/ROM/Resources/RiftMenu/MainMenuLayout.asset); `Rift/UI/Edit Main Menu Layout` abre la configuración y `Rift/UI/Rebuild Main Menu Editor Preview` refresca su preview usando la UI real. No son cambios hechos en Play que deban guardarse luego. |
| HUD | [`RiftGameplayHUD.prefab`](Assets/ROM/UI/GameplayHUD/Resources/RiftGameplayHUD.prefab), [`RiftHUDTheme.asset`](Assets/ROM/UI/GameplayHUD/RiftHUDTheme.asset) y código en `Scripts/UI/GameplayHUD`. Los componentes generados requieren editar su configuración/código, no solo mover el preview. |
| Opciones de interfaz | [`RiftHUDPreferences.cs`](Assets/ROM/Scripts/UI/GameplayHUD/RiftHUDPreferences.cs) y [`RiftHub.InterfaceOptions.cs`](Assets/ROM/Scripts/UI/RiftHub.InterfaceOptions.cs): frames compactos y UI minimalista guardados, reversibles. |
| Versión visible local | [`RiftReleaseInfo.asset`](Assets/ROM/Resources/RiftReleaseInfo.asset): `VersionName`, `PatchId`, `SourceCheckpoint` y `LocalChangesUnpublished`. Formato `0.1 Alpha (FaF) · Patch HHmmDDMMAAAA`, cierre fijo en hora Argentina; no reloj runtime. El indicador local se apaga explícitamente al publicar lo correspondiente. |
| Caps y talentos | [`RiftProgressionCaps.asset`](Assets/ROM/Resources/RiftProgressionCaps.asset), [`WarriorProgression.asset`](Assets/ROM/Resources/WarriorProgression.asset). Separar cap jugador, mundo y máximo de diseño. |
| Habilidades y armas | [`Assets/ROM/Combat`](Assets/ROM/Combat); clases de datos/cálculo en [`RiftCombat`](Assets/MultiFPS/Scripts/Gameplay/RiftCombat). |
| Misiones | [`RiftJournalCatalog.asset`](Assets/ROM/Progression/Resources/RiftJournalCatalog.asset) y [`Quests`](Assets/ROM/Progression/Quests); objetivos físicos en el mapa mediante `RiftQuestCollectible`. IDs estables, requisitos y recibos deben conservar compatibilidad. |
| Loot | [`RiftLootTuning.asset`](Assets/ROM/Resources/RiftLootTuning.asset), tablas [`Loot`](Assets/ROM/Resources/Loot), objetos y rarezas en sus subcarpetas. Asignación por definición de enemigo; fallback de tabla por defecto. |
| Reparación | [`RiftRepairTuning.asset`](Assets/ROM/Resources/RiftRepairTuning.asset): máximos, costes y desgaste. No destruir objetos al llegar a cero. |
| Vendedores | [`RiftVendorCatalog.asset`](Assets/ROM/Resources/RiftVendorCatalog.asset), referencia `Goods` del NPC; no confundir con Tienda de skins. |
| Niveles y encuentros | [`RiftEnemyLevelTuning.asset`](Assets/ROM/Resources/RiftEnemyLevelTuning.asset), [`Enemies`](Assets/ROM/Enemies) y componentes de zona/encuentro del mapa. |
| Chat y party | [`RiftSessionChatTuning.asset`](Assets/ROM/Resources/RiftSessionChatTuning.asset); radio, límites y tiempos de sesión. Nunca guardar contenido de mensajes en logs o fixtures. |

Las herramientas builder históricas pueden restablecer valores iniciales o regenerar contenido. No ejecutarlas para abrir un menú o cambiar un precio sin revisar su implementación y respaldar las diferencias. Consultar las guías de cada sistema para detalles y el código/asset actual para su estado más reciente.

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

Las revisiones de gameplay utilizan perfiles temporales y comprueban casos de combate, inventario/loot, comercio, quests, talentos y layouts 16:9 / 4:3. Sus archivos `validation.txt`, logs y capturas locales están en `Logs`, excluido de Git. Los auxiliares retirados no son una suite permanente disponible en cualquier checkout; no copiar un `-executeMethod` de un log si su clase no existe. Identificar siempre SHA, cambios locales, fixture y alcance de cada resultado.

La regresión local de talentos/progresión de esta revisión verificó límite de 19 puntos, requisitos, reset y cargas sin duplicar estadísticas; también recibos de recolección y un guardado final de las 35 misiones. No es una partida manual completa ni una prueba de reconexión con dos clientes reales. Siguen abiertas advertencias de scripts faltantes, registro repetido de spawn handlers y `JobTempAlloc`; conservar evidencia antes de atribuir una causa o eliminar componentes.

Compilar scripts/importar y completar el weaving de Mirror es un paso previo al QA runtime, no un build del juego ni una prueba funcional. **Esta tanda no genera un build, release ni despliegue.** Los procedimientos de builds documentados pertenecen a una etapa separada y requieren su propia validación.

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
- [Inventario y paperdoll](Documentation/inventory-paperdoll.md)
- [Árbol inicial de talentos](Documentation/warrior-progression/README.md)
- [HUD](Documentation/gameplay-hud/README.md)
- [Tooltips y barra de habilidades](Documentation/hotbar-tooltips/README.md)
- [Interacciones de UI](Documentation/ui-interaction-matrix.md)
- [Escenas](Documentation/SCENES.md)
- [Validación de escenas](Tools/README.md)

Los documentos históricos y capturas describen su propia revisión. Ante diferencias, contrastar el código/configuración actuales, el SHA compartido y una prueba real; una captura de una copia de trabajo puede contener cambios todavía no publicados.
