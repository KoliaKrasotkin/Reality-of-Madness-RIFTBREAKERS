# Adaptación de misiones de JulianCarrizo — 2026-11-13

Se adaptan las ideas útiles de `origin/JulianCarrizo` (`dc755ed4b`) dentro de RiftJournal. No se hace merge literal ni se declara esa rama como antecesora integrada. La revisión con `git merge-tree --write-tree` confirmó siete conflictos: PlayerInstance, HUD, pausa, Health, MainMenu, TMP y build settings. El sistema antiguo carga claves globales `Mission_*` en el servidor y las guarda en el cliente, acredita bajas sin comprobar autor/equipo y declara XP sin concederla. Sus pruebas de munición/headshots y cambios de física, escenas, prefabs y salud no se importan.

## Encargos RPG

El catálogo existente de 35 misiones se conserva y recibe dos encargos opcionales después de `refuge-training`:

| Encargo | NPC | Objetivo desde aceptación | Oro | XP |
|---|---|---|---:|---:|
| La fuerza del refugio | Bram | 300 puntos de daño efectivo a enemigos | 65 | 100 |
| Volver con vida | Alma | 100 puntos de salud efectiva restaurada a uno mismo o a otro jugador aliado | 45 | 80 |

Se reutilizan aceptación, requisitos, seguimiento, abandono y entrega del diario actual. No hay otro MissionManager, claves globales de misiones ni comando cliente para declarar daño/curación. Los objetivos nuevos no son repetibles. El daño excluye entrenamiento, aliados, uno mismo, blancos ya muertos y exceso de daño sobre la vida restante. La curación usa el evento existente `ServerHealed`: excluye exceso de curación, muertos y receptores que no sean jugadores. Se conserva `RpcHealthAdded` y el flujo actual de salud; la única ampliación es notificar daño efectivo del servidor. Las bajas, recolección y progreso de grupo continúan usando sus puentes actuales.

## Persistencia y red

El servidor guarda el diario por ID estable de personaje, incluidos XP, entrega y recibos. Las escrituras habituales usan archivo temporal y reemplazo para evitar truncar el último diario válido. Los objetivos y el abandono vuelven al estado anterior si no pueden guardar.

El host conserva su transacción actual de cartera/diario (`RewardLedgerVersion=1`). Una entrega de cliente no local guarda XP y recibo de oro en el diario del servidor (`RewardLedgerVersion=2`) sin tocar la cartera del host. El snapshot del propietario permite al cliente acreditar ese recibo en su cartera local del personaje seleccionado. El cliente guarda una asignación absoluta pendiente antes de aplicarla y marca el recibo pagado; la recuperación repite asignaciones, no incrementos. Se recupera antes de consultar el oro. Repetir snapshots o reconectar al mismo servidor no debe volver a pagar; los registros antiguos y del host no generan pagos retroactivos.

Se mantienen los límites del prototipo: identidad y cartera local confiadas, sin backend autenticado. Los recibos locales y los archivos de servidor deben conservarse; borrar o editar guardados no está protegido. Cambiar de host no transfiere automáticamente su diario. Comercio y otras operaciones de cartera ajenas a las recompensas de misiones conservan sus restricciones previas; esto no habilita comercio remoto general.

## Ejecución de pruebas

Unity 6000.3.23f1, procesos ocultos, sin entrada del sistema operativo:

- `-batchmode -nographics -executeMethod ROM.EditorTools.MissionAdaptationValidation.Run`: comprobaciones aisladas de catálogo, objetivos, requisitos, serialización, recibos, separación de personajes y recuperación de cartera. Resultado en `mission-adaptation-validation.txt`.
- `-batchmode -executeMethod ROM.EditorTools.MissionAdaptationPlayProbe.Run`: menú real, creación de mago, host Mirror, comandos de aceptación/entrega, eventos reales de Health, snapshots, recompensas y recarga del diario. Incluye la ruta de servidor no local mediante conexión sintética, sin socket remoto. Resultado en `mission-adaptation-host.txt`.
- `-batchmode -nographics -executeMethod ROM.EditorTools.PlayableClassesValidation.Run`: comprobaciones aisladas de clases jugables.

Los runners nuevos usan el catálogo existente y almacenamiento de prueba aislado. No ejecutar `RiftJournalProbe.Run` para esta integración: su preparación llama al builder antiguo, que restaura el catálogo inicial de cuatro misiones y sobrescribe datos actuales.

No se afirma validación entre dos procesos/equipos ni desconexión/reconexión completa: la recarga de persistencia se fuerza en el servidor del host. Esas pruebas siguen pendientes, al igual que WebGL. Los resultados reales de cada ejecución se registran en los archivos indicados.

## Conservación

Se incorporó por fast-forward `origin/main` en `dde0f1294` antes de editar, incluyendo el trabajo nuevo de rig/roll de JuanGori. No se importan eliminaciones de `TechnicalArt_Fede_T001` (`4c380d186`): se conservan las demos y `Documentation/TerrainRecovery/Assets/Terrain.asset` (~33 MB). Tampoco se fabrica ascendencia de merge de esa rama.
