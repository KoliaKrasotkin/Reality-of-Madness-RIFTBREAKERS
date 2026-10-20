# El Rift · Lobby 3D y frontend local

## Uso

Abrir `Assets/MultiFPS/Levels/hub.unity` y entrar en Play. El menú principal muestra el personaje IDS, la party y el botón **Jugar El Rift**. Ya no es necesario pasar por selección de misión, equipamiento y un segundo lobby para empezar. **Unirse por dirección** abre directamente dirección y puerto.

El único destino habilitado es **El Rift**, con capacidad de 1 a 4 jugadores. El archivo de escena conserva su ruta `Assets/ROM/Levels/map_rom_horde.unity` para preservar referencias. Los otros mapas se conservan como fuentes del proyecto, deshabilitados en Build Settings y fuera de RoomCreator.

Equipamiento permite elegir entre los cinco personajes IDS ya configurados, inspeccionar armas IDS y girar/acercar los modelos. Marketplace y Subastas comparten un visor 3D con modelos reales del catálogo. Las operaciones de comercio continúan siendo demostraciones y no modifican saldos o inventario autoritativo.

## Party, amigos y DM en local

- En **Amigos**, agregar un contacto local y enviarle una invitación o un mensaje privado. También existe un chat de party.
- Para probar la recepción en esta misma PC: **Ajustes → Perfil → Perfil local**, cambiar al contacto creado, regresar a Amigos y aceptar la invitación o leer el DM.
- Volver al perfil original permite ver el estado Ready y el avatar del segundo miembro. El líder puede quitar miembros. La party admite cuatro plazas.
- Los contactos, invitaciones, mensajes, apariencia y Ready se guardan en `Application.persistentDataPath/rom-local-social.json`. Los DM se consultan solo entre el perfil activo y el destinatario; no hay respuestas falsas ni estados online inventados.
- Es una simulación persistente de **un dispositivo y un proceso**, sin autenticación ni entrega por internet. No conecta automáticamente perfiles locales a una partida Mirror. Los jugadores reales se unen por dirección usando la red existente.
- `ISocialService` separa el frontend del adaptador `LocalSocialService`; `RiftHub.UseSocialService` permite sustituirlo. El selector de perfiles locales es una herramienta de pruebas, no un futuro mecanismo de autenticación.

## Video, sonido y controles

Resolución, pantalla completa/sin bordes/ventana, cuatro calidades, límite de FPS, VSync, antialiasing, escala de render, bloom, motion blur, brillo, volumen general, sensibilidad y FOV. Los cambios requieren aplicar y confirmar. Se restauran al cancelar o transcurrir 15 segundos sin confirmar.

Se guardan en `Application.persistentDataPath/rom-video.json`. Las modificaciones de URP durante la partida usan copias en memoria, no reescriben los assets del pipeline. Los cambios de resolución se omiten durante pruebas batch. Faltan pruebas en hardware móvil, mando físico y monitores con distintas frecuencias. No se añadieron controles de música/SFX independientes porque todavía no hay buses de audio separados a los que conectarlos.

## Presentación 3D

`FrontEndCatalog.asset` referencia los modelos originales, los prefabs de presentación y el decorado del lobby. Los escenarios de preview se encuentran separados del mundo jugable, sin scripts de gameplay ni objetos de red. Cada vista tiene su cámara, luces y RenderTexture. Los modelos se giran arrastrando o con el botón Girar; se acercan con la rueda.

La imagen ilustrada anterior se mantiene únicamente como respaldo si el escenario o el modelo no pueden mostrarse. El fondo habitual es geometría de templo con una fisura animada, no la imagen del sprint anterior.

La navegación principal se reduce a Jugar, Equipamiento, Progreso, Tienda y Ajustes; Amigos tiene un acceso propio. Se usa una fuente estática legible, paneles de alto contraste y listas desplazables. Marketplace sigue separado de los anuncios entre jugadores y no vende Ancient Gold directamente.

## Personajes y piernas

La selección usa únicamente los cinco `SkinContainer` que remiten a arte IDS: Black Panther, Dinork, Purplehead, Redhead y White Knight. Los modelos ajenos del kit permanecen en disco pero no están en la selección.

Las vistas 3D se generan desde `Assets/IDS ASSETS/Characters`, conservando cada esqueleto nativo y sus proporciones. Se crean avatares de preview separados; no se reimportan ni se cambian los GUID de los FBX. Los alias `upperReg.R` y `lowerReg.R` se resuelven al construir el avatar. Se corrigió además la referencia de Dinork que apuntaba a Black Panther.

`NaturalLegStance` limita la apertura excesiva de la parte superior de las piernas. En el prefab de gameplay solo actúa sobre el humanoide TPP, apoyado y sin agacharse; no modifica las animaciones FPP, el salto o el crouch. El lobby tiene adicionalmente una postura de brazos relajados. Se corrigió el giro que interpolaba con un factor mayor que uno y producía saltos de orientación, usando movimiento angular limitado.

Las pruebas de pose confirman humanoides válidos, escalas positivas y menor separación de pies en los cinco modelos nativos. **Eso no sustituye una prueba completa de locomoción, IK, disparo y skins en una partida real.** La partida conserva el sistema de skins y el rig del kit: esta entrega no rehace toda la vinculación de armas y manos ni todas las animaciones de combate. Los demás personajes del directorio IDS que todavía no tienen un SkinContainer compatible necesitan una integración aparte.

## Dirección visual de El Rift

- Perfiles URP Bajo/Medio/Alto/Ultra; HDR, sombras suaves y mayor distancia/detalle en los perfiles altos.
- ACES, bloom contenido, contraste/color, viñeta y SSAO en Alto/Ultra. Motion blur queda apagado por defecto y es configurable.
- Cielo procedural, luz direccional y niebla ambiental.
- 378 mallas primitivas de acantilado sustituidas por rocas de un paquete existente, preservando los corredores de puentes y reconstruyendo NavMesh. Las rutas principales verificadas incluyen la arena original, las tres regiones, el distrito urbano, el santuario y el patio de la Ciudad del Sol.

Esta es una primera pasada de dirección visual; **no convierte por sí sola el mapa en un entorno AAA terminado**. La vegetación, los senderos, el relieve, los límites de isla y muchos props siguen necesitando composición y assets de mayor calidad. No se ha medido FPS ni memoria en dispositivos móviles.

## Verificación y límites del backend

Las pruebas de Play Mode comprueban el único mapa, las pantallas, los modelos referenciados, invitaciones y Ready, entrega de DM entre perfiles locales, privacidad de la consulta, persistencia tras recargar el archivo, vista 3D de productos, separación de compras simuladas y restauración de ajustes. El inicio de host se verifica hasta su confirmación y se cancela: no se inicia una sesión de red desde estas pruebas.

Reportes: `frontend-local-validation.txt`, `frontend-ids-models.txt`, `ids-leg-stance-validation.txt` y `el-rift-visual-validation.txt`. Capturas: `rift-lobby-3d.png`, `rift-auction-3d.png`, `rift-marketplace-3d.png`, `rift-video-options.png`, `rift-friends-console.png` y las cinco `rift-character-*.png`.

Para el backend se necesitan identidad autenticada, presencia, invitaciones y Ready sincronizados, entrega de mensajes, matchmaking, inventario y saldo autoritativos. Las compras y subastas precisan reserva de propiedad/fondos, transacciones atómicas e idempotentes y auditoría. Ninguna de esas garantías se atribuye al archivo local de pruebas.

Reconstruir el frontend: **ROM → UI → Construir lobby 3D de El Rift**. La reconstrucción genera los prefabs de frontend; los scripts de auditoría no forman parte del juego compilado.
