# A03 — El umbral del silencio

**Propuesta para aprobación. No construida.** Alcance exclusivo: ambientación visual de A03, conservando arquitectura, colliders, alturas, NavMesh y recorridos. R21 y R23 sólo aparecen como destinos del contexto de circulación; no reciben intervención.

## Idea principal

Un antiguo acceso solar que alguien intentó cerrar y después abandonó. La piedra conserva la técnica monumental de la civilización, pero ha perdido su aspecto limpio: hollín, metal apagado, un sol parcialmente borrado y una advertencia casi ilegible. Una luz ámbar residual permite reconocer la salida; una única cicatriz violeta introduce la amenaza de las profundidades.

El espacio debe producir inquietud mediante el contraste y la ausencia. No se añaden sangre, cadáveres, jaulas humanas, voces, enemigos, puertas funcionales ni efectos que llenen toda la sala de violeta.

## Base narrativa

Fuente principal: `Ciudad del Sol Lore The Great Release.pdf`, seis páginas, SHA-256 registrado en `Fuentes_lore.json`. Se revisaron especialmente las páginas 2 y 4–6. El PDF se usa como fuente narrativa, no como autorización técnica.

**Confirmado por el relato:** investigación de una anomalía dimensional, instalaciones subterráneas, intentos de contención, deterioro y corrupción de las estructuras, refugios y evacuación, abandono final. Ciudad del Sol no provocó The Great Release; fue una víctima tardía de sus consecuencias. El funcionamiento actual de los mecanismos de contención es incierto.

**Interpretación ambiental propuesta para A03:** restos de un cierre, anclajes vacíos y mensajes fragmentarios. No se afirma que A03 fuera oficialmente una instalación de cuarentena ni que una evacuación concreta ocurriera aquí. Tampoco se confirma experimentación con personas, sacrificios, autores, fechas o número de víctimas. El término «Quiebre» se emplea sólo como referencia visual del encargo: el PDF no define un episodio con ese nombre.

Inscripciones propuestas, todavía no canónicas:

- **ACCESO RESTRINGIDO**, con letras parcialmente erosionadas.
- **…CONTENCIÓN…**, como fragmento secundario, sin una afirmación de que el sistema siga activo.

No se escriben entradas de diario inventadas como si fueran documentos históricos confirmados.

## Arquitectura y circulación verificadas

| Dato | Estado existente |
|---|---|
| Código y nombre original | A03 — Gran descanso inferior |
| Centro del piso | **(156, −16, 102)** |
| Planta | **24 × 24 m**, X=144..168, Z=90..114 |
| Suelo propio | 16 módulos `Platform_Base_01`, 6 × 6 m cada uno |
| Raíz del suelo | `Subsuelo_Acceso_Etapa01/05 - Primer piso inferior Y-16` |
| Arco existente | `Subsuelo_Acceso_Etapa01/09 - Arco del descanso inferior/Arch_Wide_01`, pivote serializado aproximado **(156, −15.9935, 108)** |
| Protecciones | Barandas originales del piso inferior; no se sustituyen por versiones rotas |
| Llegada documental | AC04 desde A02: último descenso hacia (150,−16,90), continuación por (150,−16,99) hasta (156,−16,99) |
| Continuación recibida hacia R21 | **(156,−16,99) → (174,−16,99) → (174,−16,117) → (120,−16,117) → (117,−16,123)** |
| Otras ramas | VC01, VE01 y VS01 continúan hacia las cotas profundas por sus trazados recibidos; H01/H46 del plano no se convierten en accesos aprobados por esta propuesta |

El centro vertical −17 del AABB de los módulos corresponde a su espesor; **el piso sigue en −16**. La altura libre observada en el sondeo previo varía: mínimo 4,08 m y mediana 8,76 m sobre muestras despejadas. No se considera una cubierta uniforme.

La captura de salas anterior reunió 144 estaciones en A03: 101 despejadas también sobre NavMesh, de las cuales 97 conectaron con la semilla del espacio. Esos datos no autorizan los cuatro puntos restantes como zonas de estancia. No se ha localizado individualmente cada bolsillo en ese informe y no se inventan sus posiciones.

Se conserva la recepción técnica individual existente. **Cuatro jugadores reales y seis metros útiles uniformes siguen sin acreditarse.** La propuesta deja todo el piso sin nuevos objetos sólidos y no reduce los pasos actuales. Eso protege la futura prueba cooperativa; no la sustituye.

## Dirección artística

**Estilo:** templo solar envejecido y oscurecido, con huellas de contención abandonada; monumental, legible y sobrio.

**Paleta propuesta:** piedra carbón `#302D2A`, pavimento pardo `#413A32`, bronce apagado `#706047`, inscripción caliza `#B1A48B`, sombra azul grisácea `#1D2630`, luz ámbar `#C6954B`; acento violeta localizado `#76529C`.

**Historia de la luz:** la iluminación cálida residual identifica piedra y dirección de salida. Las sombras contienen detalle; no se pretende ocultar suelo, bordes o barandas en negro absoluto. El violeta se reserva para una pequeña fractura del arco, no para teñir todo el descanso.

No se cambian el cielo, ambiente, exposición global, cámara jugable, postprocesado, HUD ni materiales compartidos del exterior. Si el ambiente existente impide alcanzar el contraste deseado, se documentará el límite antes de intervenir en un sistema global. Las naves del fondo conservarán provisionalmente su aspecto actual hasta sus propias etapas.

## Elementos y colocación

**Las posiciones siguientes son anclas de diseño propuestas, no montajes validados.** En las piezas de arco/baranda, la posición definitiva se proyectará sobre la superficie real y su normal. No se da por supuesta una cara plana en una coordenada Z concreta. Si no hay apoyo suficiente, se recoloca dentro de la misma pieza o se omite; nunca se deja un elemento flotando.

| ID | Elemento y recurso | Ancla propuesta en Unity | Tamaño/alcance inicial | Función visual y narrativa |
|---|---|---|---|---|
| A03-01 | Piedra oscurecida local; copias de los materiales originales | 16 módulos de A03 y renderizadores del arco/protecciones que correspondan al ámbito | Conservar toda la geometría y texturas de piedra; variar color, rugosidad y suciedad | Romper la continuidad con el exterior sin convertir la piedra en una superficie negra lisa. |
| A03-02 | Medallón solar incompleto, relieve o grabado decorativo nuevo | Pilastra visible del lado oeste del arco, referencia **(151, −13.2, 105)** | Aproximadamente 0,8 × 0,8 m, adherido a la piedra | La identidad solar persiste, erosionada y parcialmente anulada. No se corta el arco original. |
| A03-03 | Panel grabado «ACCESO RESTRINGIDO» | Misma pilastra, por debajo del medallón: **(151, −14.1, 105)** | Aproximadamente 0,9 × 0,4 m | Advertencia legible a corta distancia, sin convertirse en cartel de interfaz. Texto artístico propuesto. |
| A03-04 | Dos anclajes de un antiguo cierre, malla decorativa adaptada | Caras existentes de las jambas, cerca de **(151, −14.7, 105)** y **(161, −14.7, 105)** | Piezas de hasta 0,35 × 0,60 m; mínimo relieve | Sugerir un cierre retirado o fallido. Sin cadena transversal, puerta nueva, mecanismo ni bloqueo del hueco. |
| A03-05 | Cicatriz violeta fina y fragmento «…CONTENCIÓN…» | Cara visible de la pilastra oriental, referencia **(161, −13.8, 105)** | Fractura de 0,6–0,9 m de longitud y ancho visual muy reducido | Introducir la anomalía sin explicar su funcionamiento ni convertir el arco en un portal activo. |
| A03-06 | Luminaria de guía suspendida; modelo estático reutilizado | Al costado norte de la salida este, aproximadamente **(165, −12.6, 102.8)** | Cuerpo pequeño; extremo inferior por encima de 3 m del suelo; sujeción al techo real | Dar una señal cálida hacia la salida correcta. No se sitúa en el centro del hueco de paso ni se cuelga sin apoyo. |
| A03-07 | Restos de una marca solar incrustada en el pavimento | Tramo **(159.5, −16, 99) → (165, −16, 99)** | Motivo discontinuo, unos 0,15–0,20 m de ancho | Acompañar el giro hacia el este. Sólo tratamiento visual superficial: sin resalto, escalón, collider o suelo nuevo. |
| A03-08 | Linterna apagada abandonada, reutilizando `Lantern_Small_03_Unlit` | Remate superior de la baranda norte, referencia **(147, −13.8, 113)** | Ajustar tamaño y altura al soporte real; omitir si no cabe | Una ausencia humana discreta. No colocarla caída en la vía de paso ni usar los bolsillos NavMesh como depósito de objetos. |

Los IDs permiten revisar cada intervención por separado. No se amplía A03 hacia R21, el distribuidor norte o las ruinas intermedias.

### Iluminación propuesta

Como punto de partida artístico, **hasta tres luces locales**, sin un nuevo controlador de efectos:

1. Guía ámbar junto a A03-06: rango inicial del orden de 7–9 m, orientada para reconocer salida y suelo. Sólo una luz podrá usar sombras si la inspección y el coste lo justifican.
2. Relleno frío muy tenue en el descanso, aproximadamente (156,−12.8,100), sin sombras; puede omitirse si el ambiente existente ya aporta suficiente lectura.
3. Acento violeta corto junto a A03-05, rango del orden de 2–3 m, sin sombras; opcional si basta la emisión del material.

Las intensidades no se fijan como valores definitivos sin renderizar el resultado con el pipeline y la cámara reales. No se añade niebla global, parpadeo por scripts, volumetría, grandes partículas transparentes ni un resplandor que oculte la navegación.

## Recursos disponibles

| Recurso existente | Uso propuesto y precaución |
|---|---|
| `Assets/Standard Assets/Eternal Temple/Prefabs/Bases/Arch_Wide_01.prefab` | Ya existe en A03. Conservar modelo, escala, posición y collider. No colocar otro arco encima. |
| `Assets/Standard Assets/Eternal Temple/Source_FBX/Materials/Atlas_Architecture_01.mat` | Material de arquitectura existente. Crear variantes locales; no editar el original compartido. |
| `Assets/ROM/CiudadDelSolSalasMonumentales/PiedraOpaca.mat` | Referencia de piedra mate para paneles/relieves nuevos; no modificar globalmente el asset. |
| `Assets/Standard Assets/Eternal Temple/Prefabs/Lanterns/Lantern_Small_03_Unlit.prefab` | Modelo estático de linterna para el objeto abandonado y posible base visual de la guía. Tiene collider: antes de usar una instancia hay que comprobar su huella y cota. |
| `Assets/Standard Assets/Eternal Temple/Prefabs/Lanterns/Lantern_Wall_01.prefab` | Referencia visual para una luminaria. Incluye luz/efectos y scripts: no se instanciará indiscriminadamente el conjunto si sólo se necesita su modelo. |
| `Assets/Standard Assets/Eternal Temple/Prefabs/Railings and Decors/Bottom_Decor_01.prefab` | Posible remate de piedra, sólo si cabe sobre arquitectura ya sólida; no añadir zócalos en el paso. |
| `Assets/Standard Assets/Eternal Temple/Prefabs/Damaged/Broken_Wall_01.prefab` y `Column_Broken_01.prefab` | Recursos disponibles, pero **descartados del primer lote**: sus volúmenes son innecesarios y podrían invadir la circulación. |
| `Assets/ROM/Art/RiftCrystals/Prefabs/CrystalGround.prefab` | Sólo referencia opcional de material/forma violeta. En A03 se prefiere una cicatriz pequeña; no se propone un cristal grande ni añadir componentes de interacción del prefab. |

Las piezas nuevas necesarias son pocas: medallón erosionado, panel de inscripción, anclajes de cierre y la cicatriz superficial. No se propone otro generador global ni una nueva herramienta de diagnóstico para cada objeto.

## Lectura desde el jugador

1. **Llegada desde la escalera:** cambia la lectura de piedra limpia a material oscurecido; se distingue el arco original y la ausencia de actividad.
2. **Pausa en A03:** el jugador puede acercarse a la advertencia sin que una escena o interacción lo detenga. El medallón sugiere una autoridad antigua, no una religión o ceremonia nueva confirmada.
3. **Orientación:** la luz lateral y el vestigio del pavimento acompañan la salida este del rodeo recibido. El arco norte es un hito narrativo, no una señal engañosa de que deba seguirse un eje recto hasta R21.
4. **Regreso:** las inscripciones y la pequeña linterna apagada dan reconocimiento del lugar. No se agrega una barrera que cambie el paso en sentido inverso.

## Preservación y recepción futura

- No mover ni sustituir suelos, barandas, arco, pilastras, colliders, conexiones o NavMeshData.
- No colocar escombros, mesas, cofres, altares, cadenas o cristales sólidos en el suelo libre. Las intervenciones de R21/R23 no pertenecen a esta aprobación.
- No montar decoraciones sobre huecos del arco ni atribuirles un apoyo que todavía no se haya medido. Las piezas adosadas no pueden ampliar una colisión existente hacia un paso navegable.
- No introducir `NavMeshObstacle`, enlaces, teletransportes, puertas funcionales, scripts de combate, enemigos o lógica multijugador para ambientar el descanso.
- Conservar la salida hacia (174,−16,99), el giro norte y las alternativas recibidas, aunque se sitúen fuera del rectángulo de A03.
- Antes de construir: respaldo SHA-256 de la escena y recursos afectados. Materiales nuevos independientes y una raíz de ambientación identificable, con excepciones locales de material documentadas.
- Antes de guardar: comprobar montaje de cada pieza, apoyo, hueco libre del Warrior/Humanoid y ausencia de invasión de rutas. Repetir las comprobaciones físicas/NavMesh de los trayectos afectados, sin relajar tolerancias.
- Inspección visual real en Unity desde la llegada, la altura del jugador, el giro de salida y el regreso; verificar textos, contraste y cámaras. Las imágenes de este dossier son propuestas, no capturas de una obra nueva.
- Cuatro jugadores reales y el ancho cooperativo siguen pendientes. No se aprueban por conservar una retícula o un camino individual.

## Alcance que se propone aprobar

Ambientación A03-01 a A03-08, con iluminación local contenida y adaptación al soporte real. Puede omitirse cualquier pieza que no quepa sin afectar la circulación. No se aprueba la reconstrucción de arquitectura, un nuevo NavMesh, un portal, una mecánica de contención, audio, cinemática, encuentros o ampliación de la zona jugable.

La sensación buscada es: **«Aquí intentaron impedir que algo siguiera subiendo. Ya no queda nadie para explicar qué ocurrió».** Es una lectura ambiental sugerida, no una afirmación histórica nueva.

## Evidencia y límites de esta entrega

Se revisaron el dossier de 68 espacios, la recepción individual A03–R21, los recorridos recibidos y los recursos del proyecto. Se contrastaron las referencias con la escena serializada y sus hashes. No se abrió ni se modificó Unity, no se realizaron nuevas pruebas físicas y no se construyó nada. Las coordenadas propuestas necesitan su recepción de montaje tras la aprobación.

Guías de trabajo consultadas: `using-unity`, `craft-game-floors` / `art-direction.md` y lectura PDF. Los materiales y colores de esta propuesta son locales a A03; no se aplica una paleta global a sistemas protegidos.
