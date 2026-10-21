# Misiones, logros y selección — El Rift

## Cómo probarlo

Abrí `Assets/ROM/Scenes/MainMenu.unity`, elegí tu personaje y entrá en El Rift.

- Clic izquierdo: seleccionar un enemigo o NPC. El objetivo queda bajo la información de zona, arriba a la izquierda, fuera del centro de combate.
- NPC amistoso: nombre y oficio, círculo y rombo verdes; no tiene una barra de vida ficticia. Clic derecho breve o F estando cerca abre su conversación. Tab continúa recorriendo enemigos.
- Iria ofrece la primera misión. Los signos ! y ? indican misiones disponibles y entregas pendientes.
- J abre el diario: Activas, Completadas o Disponibles. Podés seguir una misión u ocultar su seguimiento; las activas se pueden abandonar con confirmación.
- U abre Logros. Ambos accesos son reasignables en Ajustes > Controles y están disponibles desde el menú principal.
- El seguimiento compacto aparece debajo del minimapa. Las notificaciones desaparecen a los cinco segundos.

## Primera cadena jugable

| Misión | NPC | Objetivos | Oro local |
|---|---|---|---|
| Antes de cruzar el puente | Iria | Tres golpes al muñeco y hablar con Bram | 60 |
| Provisiones para el camino | Alma | Comprar y consumir una poción | 40 |
| Un paso más allá | Iria | Derrotar cinco enemigos y visitar un cristal fuera del refugio | 120 |
| Romper el asedio | Bram | Derrotar un miniboss y despejar un encuentro | 200 |

Las dos misiones intermedias requieren la primera; la última requiere completar la exploración. Comprar y consumir una poción es una acción real: necesitás oro, y para consumirla tenés que haber perdido vida. Las misiones no son repetibles una vez entregadas. Los objetivos comienzan a contar al aceptarlas.

Siete logros registran el primer golpe de entrenamiento, primera victoria, diez enemigos, tres cristales distintos, primer miniboss, primera misión entregada y hablar con los tres NPCs. Incluyen progreso, fecha de desbloqueo y puntos. El muñeco no cuenta como enemigo derrotado. Volver al mismo cristal no aumenta el contador de cristales distintos.

## Datos y autoridad

El proyecto usa **Mirror**. El servidor registra eventos reales de combate, compras, consumo, exploración y conversación; valida cercanía al NPC, prerrequisitos y estado antes de aceptar o entregar. No hay un comando de cliente para declarar objetivos completados.

El progreso se guarda por ID estable de personaje en `Application.persistentDataPath/RiftJournals/<SHA256-ID>.json`. El cliente conserva una copia local para consultar el diario desde el menú. Cambiar el nombre no cambia la identidad del personaje. Los recibos locales de recompensa evitan que volver a recibir el mismo estado duplique oro. Las recompensas usan oro local, nunca Ancient Gold.

Es un prototipo local: la identidad del personaje y la cartera todavía dependen de datos locales confiados. No hay autenticación de backend, cartera transaccional ni protección contra editar los guardados. Cambiar de host no transfiere automáticamente su archivo de progreso. Las bajas se acreditan al atacante; despejar un encuentro acredita a jugadores cercanos. El reparto de objetivos de grupo requiere una próxima iteración.

## Edición y archivos

- `Assets/ROM/Scripts/Progression`: definiciones, estado, persistencia Mirror y caché del diario.
- `Assets/ROM/Progression`: cuatro ScriptableObjects de misiones, siete de logros y catálogo Resources editable.
- `Assets/MultiFPS/Scripts/Gameplay/RiftWorldTarget.cs`: puente de selección e interacción sin depender de código ROM.
- `Assets/ROM/Scripts/UI/RiftHub.Journal.cs`: diario y logros reales.
- `Assets/ROM/Scripts/UI/RiftVillageUI.Quests.cs`: aceptar y entregar desde conversaciones.
- `Assets/ROM/Scripts/UI/GameplayHUD/RiftJournalHUD.cs`: seguimiento y avisos.
- `Assets/ROM/UI/GameplayHUD/Resources/RiftGameplayHUD.prefab`: HUD editable en Unity.
- `Assets/ROM/Editor/RiftJournalBuilder.cs`: preparación reproducible mediante ROM > Progresión > Preparar misiones y logros. Ya está ejecutada; no requiere configuración manual para jugar. Volver a ejecutarla restaura los valores iniciales del catálogo.

No se modificaron la geometría del mapa, iluminación ni modelos del personaje.

## Validación

`ROM.EditorTools.RiftJournalProbe.Run` ejecuta el flujo real menú → host local con identidad y carpeta de prueba aisladas. Comprueba raycast, selección amistosa, interacción, comandos de aceptación/entrega, tres impactos de entrenamiento, compra/consumo, bajas de enemigos y miniboss reales, cristales, las cuatro entregas, logros, guardado, separación entre personajes y recompensas idempotentes. **Última ejecución: 52 comprobaciones aprobadas**, sin errores de compilación C# ni excepciones de ejecución. `validation.txt` y las capturas documentan la prueba. No se probó un cliente remoto ni hardware móvil.
