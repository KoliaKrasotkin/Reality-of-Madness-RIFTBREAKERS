# UI unificada de El Rift y emotes de lobby

La interfaz del kit usa la misma paleta negra, roja y dorada, bordes y botones
angulares del frontend nuevo. Se mantienen los componentes de datos y sus
referencias para no sustituir la lógica de salud, munición, chat o red.

## Pantallas

- HUD: tarjetas de vitalidad y munición; arma actual; inventario con nombres,
  teclas y selección; aviso de recarga; indicadores de impacto y daño.
- Horda: tipografía común, marca ROM y colores nuevos en introducción, oleada,
  reservas y avisos. También se aplica el tema a instancias desempaquetadas que
  estaban guardadas dentro del mapa.
- Pausa: reutiliza la vista de Video, Sonido y Controles del menú nuevo. Incluye
  continuar, arsenal y salida con confirmación. Usa el bloqueo de entrada original,
  sin detener el tiempo de la partida cooperativa.
- Arsenal: conserva la selección de equipamiento del kit para la próxima aparición,
  con rótulos de ranura, selectores y botones del tema común.
- Chat, marcador, killfeed, avisos de reaparición/espectador, daño y carga:
  fuentes, fondos y controles actualizados en los prefabs de UI.
- El marcador instancia filas con escala local correcta y en Raid excluye a los
  enemigos BOT de la horda. Muestra a los jugadores humanos. Los datos siguen
  procediendo de PlayerInstance.

Se corrigieron etiquetas de inventario que estaban deshabilitadas, campos de
selector demasiado bajos para la nueva fuente y bordes que conservaban un tamaño
anterior al redimensionar su panel. Las filas de inventario vacías no dibujan un
cuadrado de arma inexistente. El fondo del chat acompaña la visibilidad del texto.

## Emotes

El panel EMOTES de JUGAR ofrece seis acciones: saludo, celebrar, manos arriba,
zombi, samurái y suplicar. MÁS cambia de página; DETENER interrumpe la acción.
Al terminar la duración se vuelve al idle. Se aplican al personaje propio del
escenario 3D del menú, con el Avatar Humanoid de la apariencia seleccionada.
Cambiar de apariencia cancela el emote anterior.

Saludo y celebración usan copias locales de los FBX antiguos de Mixamo, convertidas
a Humanoid en Assets/ROM/UI/Emotes. Se conservan los originales. Los otros cuatro
clips vienen de los paquetes ya importados. El controlador del lobby mezcla idle
con emote y no aplica root motion, para mantener al personaje en su sitio.
Los emotes son locales del lobby; no se añadió sincronización de party por red.

## Archivos y mantenimiento

- GameUIUpgrade: adapta los prefabs originales manteniendo sus GUID y callbacks.
  La lista exacta está en Documentation/game-ui-upgrade.txt.
- RiftLegacyButton conserva Button y sus eventos; añade el marco nuevo.
- RiftPauseAdapter conecta el frontend de pausa al UIPauseMenu original.
- RiftHudDetails muestra arma y recarga reales; RiftChatFrame sigue la opacidad.
- FrontEndCatalog contiene las definiciones y el controlador de emotes.
- LobbyEmoteBuilder construye el catálogo y el controlador.

Las escenas, el relieve, las armas y el equipamiento inicial no se modifican por
este cambio de UI. VideoPreferences conserva su flujo de confirmación/restauración.

## Verificación

Unity 6000.3.23f1 compiló el proyecto. Se ejecutaron los seis emotes en cada uno de
los cinco personajes (30 combinaciones) y se comprobaron los clips activos.
Después se probó una partida local real: salud/munición, etiquetas del inventario,
pausa, ajustes, arsenal, cerrar, continuar, entrada de chat y marcador en vivo.
Se comprueba también la escala local de las filas del marcador.

- Emotes: Documentation/game-ui/emote-validation.txt.
- Verificación final de partida: Documentation/game-ui/validation.txt.
- Capturas: main-menu.png, hud.png, pause-settings.png, pause-arsenal.png,
  chat.png y scoreboard.png dentro de Documentation/game-ui.

Las capturas de partida componen render de mundo y render de canvases fuera de
pantalla para revisar la disposición a 1920x1080. No equivalen a una captura de
cada estado posible. No se verificó una sesión multicliente ni cada relación de
aspecto; los avisos de muerte y las miras conservan sus controladores originales.
