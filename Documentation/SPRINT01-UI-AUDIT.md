# Sprint 01 · UI y destino

## Auditoría técnica previa

- Unity **6000.3.23f1**, Universal Render Pipeline **17.3.0**. UGUI 2.0, TextMeshPro y Input System presentes. El menú ROM y el kit usan UGUI (`Text`, `InputField`, `Dropdown`), por lo que se conserva esa familia de controles sin dependencias nuevas.
- Inicio: `Assets/MultiFPS/Levels/hub.unity`, primera escena habilitada. `RoomCreator` y `RoomConnector` configuran la partida mediante `DNNetworkManager`, sobre **Mirror**. Horda es `Assets/ROM/Levels/map_rom_horde.unity`.
- `ROMMainMenu`, `ROMUIFactory`, `ROMUITheme`, `ROMLoadingScreen`: componentes reutilizables. El menú previo sigue disponible en «Conexión / perfil» y tiene regreso al nuevo HUB.
- Control del jugador: sistemas MultiFPS de personaje, armas y presentación FPS; este sprint no cambia la cámara a tercera persona. El objetivo TPS del documento requiere otro trabajo específico.
- Inventario de combate y HUD ya existen; `UserSettings.PlayerLodout`, `SelectedItemSkins` y `CharacterSkinID` guardan preferencias del kit. `UIUsernameSet` usa PlayerPrefs para nombre/apariencia. Eso no equivale a propiedad persistente de inventario validada por un servidor.
- No se identificó un servicio integrado de cuentas, saldo autoritativo, inventario comercial, subastas, marketplace o recompensas persistentes apto para conectarlo a esta UI. No se introducen sustitutos de producción.
- Misiones: Horda funcional y componentes de Raid en el kit; niveles de amenaza ROM. Recuperación e Investigación no tienen un ciclo de juego listo para este flujo y se muestran como vistas previas.
- Arte disponible: paleta y retratos de `ROMUITheme`, fuentes OdibeeSans/Inter, assets Eternal Temple, Egyptian Temple, Sun Temple y HIVEMIND. Se importan ambos logos suministrados conservando los archivos originales. Se genera un fondo nuevo sin texto ni UI para que cada botón sea interactivo.

## Arquitectura y orden aplicado

1. Modelos e interfaces en `SprintModels.cs`: perfil, monedas, inventario, misiones, escuadra, progreso, subastas, intercambio y marketplace.
2. `SprintMockServices.cs`: fixtures de sesión. `SprintServices` entrega interfaces reemplazables; cada pantalla consume esas interfaces. No escribe saldo, objetos ni propiedad de MultiFPS.
3. `RiftWidgets.cs`: botones, paneles, texto, campos y sliders compartidos. `RiftButtonGraphic` dibuja los marcos angulares y símbolos sin texturas de botones. `RiftTooltip` muestra ayuda al enfocar o pasar el puntero. Bordes oscuros, acentos carmesí, oro envejecido y retratos existentes.
4. `RiftHub.cs`: navegación y composición de pantallas. La UI puede cambiar de fondo 2D a una vista del mundo sin cambiar los contratos de servicios. Diálogos bloquean clics al fondo. Diseño 1920×1080 ajustado a área segura, escalado uniforme en horizontal; navegación UGUI por mouse, toque y selección por teclado/control compatible con el módulo de entrada existente.
5. `SprintUIBuilder`: prefab, escena de preview independiente e instalación en el hub actual. No reemplaza red, jugador ni inventario.
6. `SprintUIProbe`: prueba de Play Mode que abre las pantallas, pulsa los controles del flujo misión/equipo/Ready, confirma una compra de ejemplo y comprueba que los saldos no cambian. Las capturas son renders reales de Unity, no el mockup original.

## Funcionalidad disponible

- HUB con logo importado, nombre del kit, perfil de ejemplo, XP, dos monedas AG/RC, cuatro plazas y misión destacada.
- Jugar → misión → equipamiento → escuadra → listo → confirmación de despliegue. Horda se configura mediante los controles existentes de `RoomCreator`. El arranque de host es real; las recompensas del prototipo no lo son.
- Catálogo de misiones con tipo, dificultad, objetivos, tiempo, disponibilidad, recompensas pendientes de balance y tamaño de escuadra.
- Cuatro objetos de inventario, categorías, rareza, estadísticas, cantidad, identificadores y elegibilidad de transferencia; inspección en diálogo. Equipamiento de ejemplo, sin sobrescribir el inventario del jugador.
- Marketplace separado: filtros de colección, productos, retratos conceptuales, precio AG, temporada, detalle, confirmación y estado adquirido de sesión. No venta directa de AG.
- Subastas de ejemplo: búsqueda, categoría, rareza, precio máximo, orden, detalle y confirmación; selección de objeto transferible, precio, duración y revisión de publicación. Mis anuncios e historial muestran estado vacío explícito. Intercambio independiente deshabilitado hasta definir las reglas de pago.
- Progreso: XP, logros de muestra y solicitud de recompensa pendiente de validación, sin emisión de moneda.
- Ajustes de sesión: volumen, sensibilidad y FOV conectados a `AudioListener` / `UserSettings`.
- Códice, panel social y resultados de ejemplo; regreso al HUB y al menú de conexión anterior.

## Simulado y limitaciones

- Nivel, XP, monedas, inventario persistente, logros, cosméticos y catálogos son datos de prueba. El nombre sigue el dato del kit cuando está disponible. No hay backend de comercio, emisión de moneda ni propiedad real.
- La compra simulada marca solo el catálogo de sesión. No descuenta AG ni otorga objetos. La publicación de anuncios tampoco reserva ni publica objetos. Los IDs de transacción llevan prefijo `preview-`.
- Ready remoto, invitaciones, presencia, lista de amigos, fin de misión y pantalla de resultados no se sincronizan con Mirror en este sprint. El menú de conexión del kit sigue siendo el acceso a la conexión por dirección.
- Las tres misiones no implican tres modos implementados. Solo Horda permite el despliegue real desde este flujo.
- Los precios son ejemplos de catálogo; no existe una tasa de intercambio. Faltan servicios autoritativos con validación de propiedad, reserva, liquidación atómica, idempotencia, auditoría y protección contra fraude.
- No se implementan pagos ni monedas externas. AG no tiene recarga ni compra directa oficial.
- El diseño está orientado a pantalla horizontal. La validación visual por render no sustituye una prueba táctil en Android/iOS ni una prueba de mando físico. La escala puede reducir el tamaño físico de los controles en teléfonos pequeños; falta una variante móvil con navegación condensada.
- UI construida con controles reales, no extracción de botones de la imagen. El parecido es una primera pasada del estilo y distribución; no incluye aún el trabajo final de marcos ornamentales, animaciones ni arte individual de cada objeto.
- El controlador actual sigue siendo el del kit. Sin evaluación de rendimiento móvil o de múltiples clientes en esta entrega.

## Próximo sprint recomendado

Conectar el lobby a la escuadra real de Mirror y a un resultado de partida verificable; definir persistencia del perfil/inventario; luego una variante móvil con controles de tamaño mínimo y navegación compacta. Diseñar el contrato autoritativo de recompensas antes de integrar compras o intercambios reales.

## Cómo abrir

- Iniciar desde `Assets/MultiFPS/Levels/hub.unity` para usar las conexiones reales.
- Preview independiente: `Assets/ROM/UI/Sprint01/ROM_UI_Sprint01.unity` → Play. El despliegue está deshabilitado allí porque no hay `RoomCreator`.
- Reconstrucción: **ROM → UI → Sprint 01 → Construir e instalar HUB**. Reconstruye el prefab nuevo y sus objetos; el menú previo permanece preservado.
- Nueva isla: **ROM → Horda → Encontrar Santuario del Eclipse**.

## Verificación de esta entrega

- Compilación y construcción del mapa completadas. **125 raycasts de suelo y 491 de roca** correctos; puentes del Santuario, regiones anteriores, distrito urbano y acceso al patio de la Ciudad del Sol verificados con NavMesh.
- Play Mode: las doce pantallas abren; botones del flujo misión → equipo → escuadra → Ready, diálogo de compra, propiedad simulada, saldos inalterados y recompensa pendiente de validación comprobados.
- Play Mode desde el hub real: referencias a RoomCreator, mapa Horda registrado, apertura/regreso al menú anterior, disponibilidad del botón de despliegue y confirmación de inicio correctos. La confirmación se cancela antes de iniciar host.
- Capturas revisadas: HUB, misiones, patio del Eclipse y terreno de la Ciudad del Sol. La segunda pasada corrige la superposición coplanar del patio y las zonas recuperadas de terreno que bajaban por debajo de la base de la isla. Las texturas del templo se recuperan de las propiedades originales `_Albedo_Map` y `_Normal_Map`.
- No se realizó una sesión multijugador, una incursión completa ni mediciones de rendimiento móvil. Algunas rutas interiores de la Ciudad del Sol siguen requiriendo trabajo, como en la entrega anterior.

Resultados: `sprint01-ui-validation.txt`, `sprint01-startup-validation.txt`, `ground-validation.txt` y `eclipse-island.txt`.
