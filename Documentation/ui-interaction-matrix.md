# Interacción de UI: clic, teclado y foco

La cabecera y HUD muestran la asignación real del registro RiftControls. Si la primaria está vacía muestran la secundaria. Los tooltips muestran ambas cuando existen. Ajustes > Controles conserva dos asignaciones por acción, modificadores y detección de conflictos.

| Acción | Clic | Tecla predeterminada | Ruta |
| --- | --- | --- | --- |
| Habilidades | Hotbar | 1–7, según orden guardado | AbilityController.Request / comando y validación de servidor |
| Pociones | Botellas del HUD o Usar en bolsa | H / Shift+H | CmdDrinkPotion, misma cartera y cooldown compartido |
| Bolsa | Botón BOLSA y cabecera | I | Alterna panel; respeta rebind primario/secundario |
| Personaje | Icono/cabecera | B; Shift+B para equipo | Mismo panel de personaje |
| Habilidades / talentos | Iconos/cabecera | K / P | Cambia al subpanel correspondiente o lo cierra |
| Diario | Icono/cabecera | J | Alterna diario |
| Seleccionar misión | Lista y Anterior/Siguiente | PageUp / PageDown | Cambia selección y desplaza lista |
| Seguir misión | Seguir / Ocultar seguimiento | G | Misma preferencia local de tracking |
| Ajustes | Icono/cabecera | Escape abre menú; F10 alterna ajustes | Pantalla y controles existentes |
| Cerrar / volver | Cerrar, Cancelar, X del mapa | Escape | Primero diálogo o desplegable superior, luego menú |

Las acciones nuevas dejan su default sin asignar si colisiona con un binding guardado de otra acción. No se sobreescriben preferencias existentes. Si se muestra «-», se puede asignar en Controles.

El foco de InputField/TMP bloquea acciones de gameplay y movimiento en el registro y controlador MMO; no consume eventos de escritura. Chat/Cancelación siguen disponibles. Los clics en botones son interceptados por el raycaster de UI y no seleccionan ni orbitan el mundo subyacente. Clic y teclado conservan los cooldowns y validación del servidor.

El cursor marfil de 32x32 usa hotspot (2,2), aparece en menús y durante el control MMO sin arrastrar cámara; se oculta/bloquea al orbitar según el controlador existente. Abrir un menú solicita el cursor; al cerrar se devuelve esa propiedad al controlador. La textura puede verse en la salida rift-cursor-actual.png del probe.

Prueba: `ROM.EditorTools.RiftBagReview.Run -riftInteractionReview -riftReviewOutput <directorio>`. Usa host e identidad aislados, teclado/mouse virtuales y bindings temporales que restaura al salir. Las capturas de esta prueba pueden mostrar F8 secundario como demostración de rebinding; no es un nuevo default de bolsa. El informe enumera checks PASS/FAIL y las capturas se renderizan en 1920x1080 y 1280x720.

No implica paridad completa con WoW, remapeo táctil ni validación de cliente remoto. El rediseño artístico del árbol de talentos y mapa M queda fuera de esta capa.
