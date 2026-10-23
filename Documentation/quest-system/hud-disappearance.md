# HUD invisible tras matar fauna

No existe un atajo de ocultación global del HUD en los controles actuales. El log del editor mostró una desconexión Mirror provocada por IndexOutOfRangeException en UIKillFeedElementV2.Write: el contador FPS accedía a TeamColors[2] al morir fauna, pero la paleta solo incluía equipos de jugadores. El HUD depende del jugador local conectado.

Correcciones: paleta con validación de rango y color de respaldo; validar víctima antes de acceder a sus datos; tolerar CharacterItemManager ausente; evitar eventos antes de inicializar el contador; retirar la suscripción al gamemode al desactivar o sustituir el componente.

Validación en Play/host: ejecutar el contador con una víctima del equipo de fauna, matarla por el sistema real, verificar que Mirror sigue conectado y el Canvas del HUD permanece activo después de cerrar el menú. Pasaron 23 comprobaciones, incluyendo las 20 anteriores de inventario/reutilización. Log: Logs/HUD-Recovery-Test.log. Resultados: inventory-validation.txt. No se agregó ningún botón para ocultar la UI.
