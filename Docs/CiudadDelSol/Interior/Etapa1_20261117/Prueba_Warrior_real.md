# Prueba funcional A03–R21 con Warriors reales

## Alcance y estado

Herramienta preparada y compilada en C# con referencias y Roslyn de Unity 6000.3.23f1. **No se ejecutó esta recepción en Play Mode ni se verificó su ventana dentro del Editor.** El acceso todavía no queda certificado funcionalmente para cuatro jugadores.

Se reutilizan el bootstrap original, PlayerInstance.MyCharacter, WarriorAvatar, CharacterController y las entradas originales de RiftMMOController. La herramienta solo observa; no crea ni mueve personajes, no escribe entradas, no cambia velocidades, no agrega componentes, no inicia Host/Play, no modifica autoridad ni usa teletransportes. La polilínea se comparte con CiudadDelSolInteriorStageOne.NorthGuide, evitando dos definiciones distintas del recorrido.

No se intenta automatizar el movimiento por Transform, CharacterController.Move ni actor.Input: esas opciones eludirían o competirían con el procesamiento real de controles. La automatización de observación de un recorrido manual es la alternativa preparada, tal como autoriza el pedido. No es una prueba end-to-end automatizada de entradas.

## Restricción multijugador demostrada en código

CiudadDelSolWarriorLocalHost.cs configura P_MaxPlayers=1 y NetworkServer.listen=false (líneas próximas a 162–168). Conservar ese bootstrap implica que no pueden conectarse tres clientes reales adicionales. No se cambió esta configuración.

El botón de cuatro Warriors es una capacidad condicional de observación para una sesión futura que ya disponga de cuatro conexiones reales. Exige cuatro PlayerInstance no BOT, autenticados, con identidades de conexión distintas y MyCharacter Warrior vivo. No usa clones, bots ni cuatro personajes con una sola conexión. Rechaza explícitamente el Host local sin escucha. No constituye una solución para conectar clientes, probar admisión o cargar Ciudad del Sol en clientes remotos.

## Una prueba manual inicial

1. Esperar la importación/compilación de Unity. Abrir la escena definitiva y utilizar el modo de desarrollo ya existente. Presionar Play como siempre. No es necesario ejecutar una herramienta adicional para crear Warrior/HUD/Host.
2. Llegar caminando al descanso A03 por el acceso real. El ensayo empieza alrededor de **(156,-16,99)**; no trasladar el Transform al destino como sustituto de recorrer la entrada. La ventana no prepara ese traslado ni prueba AC01–AC04 completos.
3. Abrir **ROM → Dungeon Ciudad del Sol → Interior - Etapa 1 → Prueba funcional Warrior (manual asistida)**. Puede abrirse antes de Play; no arranca nada automáticamente. Acoplarla al lado de Game y, si se desea, abrir Scene para ver la guía cian y sus coordenadas.
4. Elegir **Registrar ida y vuelta · Warrior local real**, situado a menos de 2,5 m del inicio. El personaje debe estar vivo, en la escena correcta y tener su CharacterController habilitado.
5. Volver a dar foco a Game. Usar WASD y el mouse originales, caminar sin salto/roll/teleport para esta recepción. Seguir en orden los puntos:

   (156,-16,99) → (174,-16,99) → (174,-16,117) → (120,-16,117) → (117,-16,123)

   Regresar por los mismos puntos en orden inverso. Se marca cada punto al acercarse a menos de un metro; la ventana muestra el próximo destino. No simula ni corrige el recorrido.
6. La ida/vuelta completa finaliza el registro. Si se necesita abandonar, usar **Finalizar y guardar informe**. Cerrar la ventana, salir de Play o recompilar también finaliza el registro como incompleto. La herramienta no detiene el Host ni saca al usuario de Play.
7. Abrir **Mostrar informe JSON**. Se guarda en Backups/CiudadDelSol/Interior/WarriorWalk_FECHA/Recorrido_Warriors.json. No hay spam continuo de Console: un resumen al terminar.

## Qué mide

Muestreo manual activado a 10 Hz, máximo 15 minutos. No hay coste de consultas cuando no se está registrando.

- Identidades de PlayerInstance/CharacterInstance y connectionId real.
- Coordenadas, distancia recorrida, entradas observadas, tiempo y puntos alcanzados.
- CharacterController real: grounded y collisionFlags; dimensiones constantes y escala unitaria.
- Apoyo del centro y ocho puntos de huella, con colisiones reales, triggers excluidos, sin usar otros personajes como suelo. Se verifica piso Y=-16 y pendiente compatible con el controlador.
- NavMesh local Humanoid 0 y distancia al límite de su malla. La cercanía al borde del NavMesh es una advertencia: **no equivale por sí sola a vacío físico**.
- Colliders en contacto/proximidad a la cápsula, con jerarquía completa. Un OverlapCapsule, incluidos suelo y otros Warriors, no se interpreta automáticamente como un obstáculo que invalida el paso.
- Cinco segundos de entrada de movimiento sostenida sin desplazamiento: posible atasco, no causa probada. Mirar contra una pared también puede producir este aviso.
- Interrupciones del muestreo mayores de medio segundo; no se certifica el intervalo perdido.
- Saltos grandes de posición, cambio de personaje, muerte, desconexión o salida del ámbito detienen el registro como incompleto. No hay rescate automático.
- Hashes SHA-256 de escena, NavMesh exterior y población antes/después. No se guarda ninguno de esos archivos.

No sustituye un barrido continuo ni todos los contactos transitorios entre muestras. La recepción nativa previa aporta las cápsulas/barridos de la guía; esta herramienta agrega observación del personaje controlado realmente, sin declarar aprobado el ancho uniforme de seis metros.

## Prueba cooperativa posterior, solo con sesión real habilitada

Reunir cuatro Warriors humanos cerca del inicio y elegir el botón correspondiente desde el Editor que actúa como Host. Cada uno debe recorrer ida y vuelta con su propio cliente. No basta con ejecutar el ensayo individual cuatro veces.

Se registran cuatro cruces por participante: abertura de A03 en X≈167,47/Z≈99, cruce de R21 en X≈132/Z≈117, y ambos de regreso. El campo fourCrossingWindows expresa el tiempo entre el primero y el último de los cuatro cruces para cada dirección. Valor -1 significa evidencia faltante. Una ventana breve (orientativamente ≤10 s) ayuda a documentar paso próximo en el tiempo, **pero no prueba por sí misma circulación simultánea segura**. Coordinar paso de dos por dos, encuentro en sentidos opuestos y maniobra en las bocas; verificar también desde los clientes remotos cámara, empujes, bloqueo y sincronización. No modificar el Host de ensayo para conseguirlo sin una intervención multijugador autorizada.

## Criterio de revisión

El resultado RECORRIDOS_REALES_OBSERVADOS indica que se observaron los puntos de ida/vuelta, no que toda la arquitectura esté aprobada. Revisar eventos, soporte, caídas, atascos, muestreo, contactos y clientes; mantener pendiente cualquier observación sin explicación. No hay aprobación automática ni modificación de tolerancias para hacer pasar una ruta.

Por ahora: eje físico y NavMesh previamente comprobados, circulación jugable de un Warrior pendiente de esta nueva prueba; cuatro jugadores pendientes por restricción del Host. No se ha demostrado necesidad de ampliar arquitectura.

## Archivos y verificaciones

- Nuevo Assets/ROM/Editor/CiudadDelSolInteriorWarriorWalkWindow.cs y su .meta: ventana UI Toolkit exclusiva del Editor, activación manual.
- CiudadDelSolInteriorStageOne.cs: solo se extrajo NorthGuide para compartir los cinco puntos existentes. No cambió su validador físico ni el NavMesh.
- Este documento.
- Respaldo previo del script anterior y manifiesto de hashes en Backups/CiudadDelSol/Interior/PruebaWarrior_20261117/.

Compilación Editor offline sin errores; las advertencias de Experimental NavMeshQuery proceden del diagnóstico preexistente, no de estas herramientas. Runtime, bootstrap, Mirror, controles, población, geometría, colliders y NavMeshData no fueron modificados. La CLI no encontró un Editor con Pipeline conectado; no se instaló ningún paquete ni se inició Unity para esta entrega.

Guías consultadas: unity-cli.md, references/common-workflows.md, ui-uitk.md y fix-3d-physics-collisions.md del skill Unity local.