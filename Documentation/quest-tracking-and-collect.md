# Objetivos de misión: seguimiento y recolección

Extiende el diario existente. J abre el diario y Seguir misión elige el destino del HUD. Un rombo señala el objetivo visible; fuera de pantalla se convierte en flecha hacia el objetivo. El tracker muestra distancia a una referencia real. Una misión lista dirige al NPC de entrega. Si no existe referencia, no se inventan coordenadas.

## Recolección

1. Crear/editar un RiftQuestDefinition y añadir un JournalObjective con Event = Collect, Filter = clave de objeto (por ejemplo supplies-fruit), Required = cantidad. Mantener Unique desactivado: cada fuente ya tiene recibo individual.
2. Incorporar la misión al RiftJournalCatalog existente y asignar sus NPC de inicio/entrega y prerrequisitos.
3. Añadir RiftQuestCollectible a cada objeto de escena con Collider no trigger y MeshRenderer o SkinnedMeshRenderer. Asignar ItemKey igual al Filter; DisplayName para el jugador; Range entre 1 y 5 metros.
4. Cada fuente necesita StableId único y estable entre host y clientes. Reset del componente genera uno. Al duplicar un objeto, generar otro id; el servidor rechaza ids duplicados. No generar ids aleatorios al entrar en Play.
5. Seleccionar/hacer clic cerca del objeto o seleccionarlo y usar Interactuar (F por defecto).

El servidor resuelve el id contra objetos activos, comprueba misión activa y objetivo incompleto, clave, vida, proximidad, collider y línea de visión. El cliente no elige cantidad ni progreso. Cada fuente entrega una unidad por objetivo de cada misión y deja un recibo dentro del QuestRecord guardado. Repetir el comando o recargar ese estado no duplica el crédito. Una nueva misión puede volver a usar la fuente. Abandonar borra el progreso/recibos de esa misión según la semántica previa del diario.

La fuente permanece en el mundo: no se destruye ni desaparece globalmente. Cada personaje elegible puede recogerla independientemente; al completar su objetivo pierde su resaltado local. No genera un objeto comerciable en la bolsa de provisiones.

## Enemigos y ubicaciones existentes

Kill y MiniBoss conservan las bajas del servidor existentes. Filter usa el Id de RiftEnemyDefinition para enemigos nativos; vacío conserva cualquier enemigo elegible. Wildlife no cuenta para Kill. Los enemigos heredados usan enemy/boss, igual que sus eventos actuales. No se condiciona daño, XP o ataque a tener una quest.

El crédito de quest se aplica solo a misiones activas y objetivos que coinciden. La producción de eventos sigue en la muerte del enemigo/población del servidor; no existe un comando cliente para pedir crédito de bajas.

El resolver también conoce NPCs, muñe­cos, checkpoints y encuentros. Para enemigos no cargados puede dirigir a una zona de spawn configurada y la identifica como Zona; no afirma que un enemigo esté vivo allí. Los cristales ya visitados de un objetivo único se omiten.

## Presentación

RiftQuestGuidance consulta el diario local cada 0,25 s y actualiza el marcador por frame. El rim URP usa copias de renderers con el mismo mesh/rig y un material de presentación local, nunca sustituye materiales compartidos. ZTest LEqual impide resaltar a través de paredes. Límite: 64 renderers resaltados. Se retira al completar/abandonar, cambiar de personaje, desactivar/destruir el objetivo o salir de gameplay.

## Prueba aislada

Ejecutar Unity batch con `-executeMethod ROM.EditorTools.RiftBagReview.Run -riftQuestReview -riftReviewOutput <directorio>`. Usa identidad propia, catálogo clonado en memoria, diarios aislados y esferas rojas explícitamente de prueba. No añade manzanas ni misiones nuevas a la campaña ni guarda escenas. Conserva la prueba normal de bolsa cuando se omite -riftQuestReview.

Pendiente de QA: dos clientes en procesos distintos, build standalone/móvil, recorrido completo y balance/contenido de campaña. La persistencia conserva las limitaciones de identidad y JSON del host que ya tenía el prototipo.
