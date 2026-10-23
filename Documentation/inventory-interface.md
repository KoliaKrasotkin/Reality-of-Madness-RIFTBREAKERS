# Bolsa e interfaz de juego

La bolsa muestra las dos pilas reales de provisiones; no crea celdas de almacenamiento ficticias ni objetos comerciales.

## Uso

- I abre/cierra la bolsa. B conserva Personaje; Shift+B conserva Equipo.
- El botón BOLSA del HUD permite abrirla con el mouse.
- Escape o Cerrar vuelve al juego. El cursor y la cámara siguen el gestor de pausa existente.
- Seleccionar una provisión muestra cantidad, curación, tecla y recarga. Pasar el cursor muestra su tooltip.
- Usar poción ejecuta el comando existente del servidor, respetando vida, existencias y cooldown compartido.
- Bolsa, Personaje, Habilidades, Diario y Ajustes están accesibles desde la cabecera. Salir de partida sigue disponible en Ajustes.

## Alcance

El oro y las provisiones conservan su persistencia local. Los clientes remotos ven el motivo por el que no pueden consumir esa cartera. Equipo sigue dependiendo de Warrior y sus reglas actuales. No se añade backend, comercio entre jugadores, loot genérico ni capacidad nueva de bolsa.

El cursor marfil es original y se genera por código; el hotspot está en la punta (2,2). Su componente solo cambia la apariencia: el sistema existente mantiene la propiedad del bloqueo y visibilidad.

## Validación

`ROM.EditorTools.RiftBagReview.Run` inicia un host con identidad de prueba, usa un directorio de diarios aislado y restaura preferencias al terminar. No ejecuta builders ni guarda escenas. Opcional: `-riftReviewOutput <directorio>`; salida predeterminada `Logs/BagReview`.

Comprueba stacks, consumo real, cooldown, selección, tooltip, reaperturas, navegación, I, Escape y supresión al escribir; captura 1920x1080 y 1280x720. No sustituye una prueba de cliente remoto, móvil, muerte/reaparición o build standalone.
