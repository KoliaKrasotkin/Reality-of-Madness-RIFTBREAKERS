# Hotbar, tooltips y accesos de gameplay

## Uso

- Pasá el mouse por una habilidad de la hotbar: nombre, descripción, daño base, alcance/área, coste de Furia, recarga efectiva, ejecución, estado actual y tecla asignada. Los datos de alcance y recarga incluyen sus talentos.
- K abre Habilidades. Los iconos del libro y el encabezado del autoataque también tienen tooltips.
- Arrastrá con clic izquierdo una habilidad hasta otra ranura para intercambiarlas, incluso si están en cooldown. Soltar fuera de la barra cancela el movimiento. No se elimina ninguna habilidad.
- Las teclas pertenecen a las ranuras. Si movés Barrido a la primera posición, lo ejecutás con 1. Icono y cooldown siguen a la habilidad; la recarga real no se reinicia.
- El orden se guarda por personaje y clase en preferencias locales. No se modifica ninguna definición de habilidad ni su ID de servidor.
- K > RESTAURAR BARRA devuelve el orden inicial. No reinicia talentos, cooldowns ni controles.

## Accesos

| Función | Atajo inicial |
|---|---|
| Habilidades de la hotbar | 1–7, según posición |
| Autoataque independiente | T |
| Talentos, acceso directo | P |
| Habilidades | K |
| Personaje y equipo | B; Shift+B también abre equipo |
| Inventario | I |
| Misiones | J |
| Logros | U |
| Amigos y mensajes | N |
| Chat de grupo | L |
| Chat de partida | Enter |
| Ver escuadra | Mantener O |
| Interactuar | F o clic izquierdo en NPC cercano |
| Poción | H |

Los accesos nuevos están en Ajustes > Controles y pueden reasignarse, igual que los anteriores. La barra muestra la tecla de cada ranura y los botones de HUD explican sus accesos al pasar el mouse. No se agregaron combinaciones para ajustes gráficos o de sonido; Escape conserva la pausa.

El HUD mantiene los cuatro accesos inferiores y agrega una fila compacta con Talentos, Misiones, Logros y Amigos. Talentos tiene además una etiqueta visible. P y el botón abren directamente el árbol; K abre el libro de habilidades. La consola social mantiene su implementación local previa, sin backend nuevo.

## Implementación

- `Assets/MultiFPS/Scripts/Inputs/RiftHotbar.cs`: permutación validada de siete IDs, preferencias por personaje/clase y restauración.
- `RiftMMOController`: teclas por ranura resuelven esa misma permutación. El módulo Input System de UI se activa aunque el EventSystem del mapa aparezca después del controlador.
- `RiftGameplayHUD`: botones, iconos, recursos y cooldowns leen los IDs resueltos; accesos directos.
- `RiftHotbarSlot`: eventos UGUI de hover y drag/drop con icono fantasma; el arrastre evita clics de lanzamiento.
- `RiftAbilityTooltip` / `RiftTooltipTrigger`: tooltip UGUI reutilizable, no bloquea raycasts y funciona en gameplay y menú.
- `RiftGameplayHUDBuilder`: prefab real editable y botones de acceso serializados.

La lógica de autoridad, daño, talento y recargas permanece en Mirror. Mover una habilidad sólo cambia cómo el cliente elige su ID; el servidor valida la ejecución como antes. Las siete habilidades siguen disponibles y el autoataque conserva su canal independiente.

## Validación

`ROM.EditorTools.RiftJournalProbe.RunUI` inicia un host desde MainMenu con un personaje de prueba aislado y dispositivos Input System virtuales. Comprueba hover real, arrastre y drop de una habilidad en cooldown, ausencia de lanzamiento al arrastrar, persistencia y separación entre personajes, ejecución por teclado de la habilidad reordenada, acceso P y clic al árbol, tooltip del libro, amigos/party y reasignación de P a otra tecla. Restaura los controles previos tras salir. No sustituye una prueba de cliente remoto o pantalla táctil.

Última ejecución: **23 comprobaciones de UI aprobadas en host local**, sin errores de compilación C# ni excepciones de ejecución. Capturas finales: `Documentation/quest-system/ui-final-tooltip.png` y `ui-final-reordered-hotbar.png`. Unity quedó cerrado.
