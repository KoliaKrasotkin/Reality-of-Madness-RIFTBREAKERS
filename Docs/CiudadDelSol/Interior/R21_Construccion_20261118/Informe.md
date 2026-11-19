# R21 — Salón de las Ofrendas

Construcción del 18 de noviembre de 2026. **Escena original guardada y recargada en Unity 6000.3.23f1, DX11, Edit Mode.** Se reutilizó el Editor abierto. No se inició Play ni se construyeron otras salas.

## Resultado arquitectónico

Se mantuvo la nave existente de **30 × 42 m**, centro **(117,−16,123)** y planta X=102–132 / Z=102–144. La altura libre central medida antes de instalar el mecanismo llega a 18 m bajo el intradós Y=2; no es una altura uniforme de toda la sala. Se conservaron sus pilares, bóveda, accesos y conexiones.

El foco está desplazado nueve metros al norte del cruce funcional: **(117,132) en planta**. Así la llegada desde A03 y los ejes H12, H13 y VC01 conservan su función. El centro visual lo forman un sello ceremonial integral en el suelo y un instrumento antiguo suspendido, no un pedestal colocado sobre las rutas recibidas.

| Elemento construido | Implantación y función |
|---|---|
| Fábrica de piedra erosionada | Material exclusivo sobre los dos renderizadores de bóveda/pilastras de R21. Textura existente de bloques dañados, sustituyendo la repetición de un atlas arquitectónico impropio. No se modificaron las mallas ni el material compartido. |
| Pavimento antiguo | Referencias de material local en los 35 módulos completos que forman la planta. No se movió ni sustituyó el suelo físico. |
| Gran sello solar | Centro (117,−16,132), diámetro exterior 13 m, anillos incompletos, escala radial y marcas de desgaste. Tratamiento visual adosado al pavimento existente, sin collider ni resalto físico. |
| Mecanismo ceremonial/de contención | Centro de los anillos (117,−8,132); órbitas de radios 3,8 / 3,15 / 2,55 m. El anillo menor tiene una abertura deliberada. Metal oxidado y ejes de unión visibles, sin piezas orbitales flotantes. |
| Carcasa pétrea | Malla visual original de `Stone_Altar_04`, escala 1,8, reutilizada como carcasa tallada. Conserva el prefab original intacto. No se presenta como evidencia de sacrificios. |
| Cuna y placa de apoyo | Vigas metálicas, travesaños y placa visible bajo la carcasa. La cara superior de la placa y la inferior visible de la carcasa coinciden en Y=−10,19. No es una plataforma jugable ni un piso invisible. |
| Suspensiones | Cuatro anclajes en X=114/120 y Z=129/135. Llegan al techo físico medido en Y≈1,985, dentro de la bóveda existente. |
| Gran relieve septentrional | Centro aproximado (117,−3,35,143,43), diámetro 4,3 m: disco solar incompleto, límite concéntrico y escala de medición. Proyectado sobre el muro real; no se construyó una pared para sujetarlo. |
| Corrupción localizada | Una fractura mural de unos 1,75 m y otra menor sobre la carcasa. Incisión oscura y filamento violeta separados para evitar coplanaridad. Sin portal, animación ni luz violeta uniforme. |
| Luz residual | Cuatro spots cálidos locales, sin sombras dinámicas nuevas de esas luces; dos franjas ámbar y una luminaria visual original montada sobre la suspensión. No se modificaron iluminación ambiente, skybox, volúmenes, postprocesado ni cámara jugable. |

Las texturas de piedra, normal y óxido ya existían en el proyecto. Se referencian desde los materiales nuevos; no se duplicaron ni alteraron sus archivos originales. El instrumento y su placa tienen colliders sólidos: **21 nuevos colliders**, todos por encima de la circulación. La menor cota de un sólido nuevo deja **4.070 m sobre el suelo**.

Se añadió un `NavMeshModifier` solo a la raíz nueva para que los sólidos decorativos no se clasifiquen como superficies transitables en un futuro Bake. Se mantiene el Surface existente con Physics Colliders; los grabados sin collider no añaden fuentes físicas. **No se horneó ni modificó ningún NavMeshData y no se creó otro NavMeshSurface.** Un Bake futuro deberá volver a validarse.

## Relación con el lore

Se volvió a verificar el SHA-256 del PDF oficial contra la extracción previamente revisada de sus seis páginas. El canon confirma investigación subterránea, mecanismos de contención, expansión de la anomalía, grietas, corrupción, pérdida de control y abandono. Ciudad del Sol fue una víctima tardía de las consecuencias de The Great Release; no su causante.

| Recurso visual | Relación y límite narrativo |
|---|---|
| Sello solar y carcasa ceremonial | Identidad arquitectónica solar. Su asociación concreta con una ceremonia u ofrendas es una interpretación ambiental del nombre de diseño R21. |
| Escalas, órbitas y ejes metálicos | Sugieren investigación y contención, confirmadas como actividades generales de las profundidades. La forma de esta máquina es una propuesta artística, no un artefacto histórico identificado en el PDF. |
| Anillo roto, óxido y desgaste | Sugieren abandono y fracaso. No fijan cuándo, quién ni cómo se rompió esta pieza concreta. |
| Dos fracturas violetas | Remiten a las alteraciones estructurales y corrupción confirmadas. No demuestran que este instrumento siga funcionando. |
| Vacío ceremonial y luz residual | Buscan transmitir ausencia, tragedia y peligro sin añadir víctimas, sangre, cuerpos o relatos escritos. |

**No se afirmaron sacrificios, tortura ni experimentación humana.** Tampoco se inventaron fechas, nombres, sucesos definitivos o una explicación del funcionamiento actual de la anomalía.

## Pruebas reales ejecutadas

- Retícula original: **312/315 muestras despejadas y conectadas**, idénticas después de construir. Las tres muestras obstruidas siguen siendo pilastras existentes en **(131,−16,105), (131,−16,123) y (131,−16,141)**; no se habilitaron artificialmente.
- **12/12 recorridos locales** con caminos NavMesh completos y física satisfactoria, en ambos sentidos. Apoyo, huella, cápsula original del Warrior, barridos y niveles compatibles se comprobaron mediante los validadores existentes.
- **164 mediciones de anchura**, cota inferior comprobada de **6 m** en los ejes muestreados. Las 164 admiten cuatro cápsulas estáticas; cero bordes sin apoyo/protección detectados en esas bandas. No significa que todo el recinto tenga una banda uniforme de seis metros.
- Colliders y transformaciones originales preservados. Los nuevos sólidos están elevados y no invaden el volumen de circulación; la altura libre mínima añadida es ≈4,07 m.
- Regresiones reales en Unity sobre la candidata en memoria, usando las herramientas de las siete etapas:

| Recepción | Rutas con navegación y física bidireccional |
|---|---:|
| Etapa 1 | 1/1 |
| Etapa 2 | 6/6 |
| Etapa 3 | 3/3 |
| Etapa 4 | 2/2 |
| Etapa 5 | 2/2 |
| Etapa 6 | 1/1 |
| Etapa 7 | 1/1 |
| Continuidades encadenadas | 3/3 |

- Las comprobaciones densas, controles negativos, bandas físicas y pruebas Humanoid que componen `AutoRegressions` también devolvieron resultado satisfactorio. No se alteraron sus criterios ni tolerancias.
- **8/8 rutas automáticas navegables** A03–P01 y R21–P01, ida/vuelta, Warrior y Humanoid. Cero fallos de apoyo, barrido o navegación. Se compararon con la recepción de A03: esquinas, número de muestras y avisos son idénticos en las ocho.
- **Advertencias conservadas:** esas rutas automáticas mantienen `accepted=False` para sus bandas laterales globales completas. Sus avisos previos de bordes y cuatro carriles no se ocultaron ni se declaran corregidos por esta ambientación.
- Guardado y recarga reales: una sola raíz R21, retícula conservada, misma triangulación cargada y mismo NavMeshData. La inspección visual se hizo con iluminación de escena real y Gizmos desactivados.
- Compilación de Editor por Roslyn e importación de Unity sin errores nuevos; permanecen siete avisos CS0618 de un diagnóstico experimental anterior, ajenos al código de R21.

**No se movieron Warriors ni se simularon cuatro clientes.** La recepción técnica individual y las cuatro cápsulas estáticas no equivalen a una prueba con cuatro jugadores reales. Esa prueba, la cámara original durante gameplay y el rendimiento en Play Mode continúan pendientes. Se conserva también el tramo antiguo sin telemetría manual de la Etapa 1; no se inventó evidencia nueva.

## Revisión visual y correcciones

Se revisaron llegada, aproximación a altura del jugador, ambos flancos, muro norte y detalle de carcasa/apoyos. Se corrigieron reflejos excesivos, uniformidad del metal, la textura de atlas repetida en la fábrica, acentos sin soporte, uniones de los anillos y el asiento de la carcasa. Las fracturas y los relieves se proyectaron sobre superficies físicas coincidentes con sus mallas visibles.

Los solapes de ejes, vigas, placa y carcasa son uniones mecánicas deliberadas; no atraviesan la arquitectura base. No se observaron nuevas piezas flotantes, obstáculos en accesos o huecos de suelo. Los recintos vecinos siguen sin esta ambientación y permanecen visibles a través de las arcadas; no se cerraron ni oscurecieron globalmente para conseguir una captura más dramática.

Las capturas son del **Editor real**, no imágenes conceptuales o composiciones externas. La vista a altura del jugador es una cámara del Editor a esa altura; los detalles altos no representan posiciones accesibles del Warrior. La calidad artística final queda para revisión del usuario.

## Archivos, respaldo e integridad

- Modificado: `Assets/Scenes/Dungeon_Isla_del_Sol.unity`.
- Nuevo: `Assets/ROM/Editor/CiudadDelSolInteriorR21Atmosphere.cs` y meta. Editor exclusivo, reutiliza la recepción existente; no es un sistema runtime.
- Recursos exclusivos: `Assets/ROM/Art/CiudadDelSol/Interior/R21/` y sus metas.
- Informes, capturas, CSV y resumen: esta carpeta.
- Respaldo y pruebas completas: `Backups/CiudadDelSol/Interior/R21_Construccion_20261118/`.

Antes de intervenir se verificaron **81 copias SHA-256**. Después del guardado, los **80 archivos protegidos distintos de la escena** siguen idénticos, incluidos los recursos y el script de A03. La comparación completa del YAML confirma: ningún bloque original eliminado, ningún collider ni transformación original modificado; únicamente cambian referencias de material en **35 instancias de suelo y dos renderizadores propios de R21**, más la referencia a la nueva raíz. Las demás partes de la escena, incluido A03, permanecen idénticas.

Escena de partida: `3D552FBE4480DBDCD5D3B0CCEFC02E1640FDF4E493E83BFE5841E17A4204CEBA`

Escena guardada: `5AEEAFEFF31C724525ED014130E700420D5B56A83A95FEF7083F1AC850407EE4`

NavMeshData conservado: `B22F805300D430ECC7AE3FC602A01043A4E4BEF4AAAC4F0A5FB4AF31C4DEAE99`

Los sellos históricos no se reescribieron: esta recepción acredita el nuevo hash. La ambientación está guardada y se carga con la escena al iniciar Play; **no requiere ejecutar un generador**. La herramienta solo atiende solicitudes explícitas de Editor y evita duplicados.

## Pendientes y recomendación

Revisar visualmente R21 antes de extender este lenguaje a R23. Continúan pendientes cuatro jugadores reales, observación de la cámara/combate en Play y medición de rendimiento. No se certifican todas las rutas posibles o todas las superficies por una retícula.

**A03 permanece intacto. R23, cámara final, exterior, población, Mirror, Warrior, controles, combate, salud, XP, progresión, HUD y cartografía no se modificaron. Sin commits, pushes ni staging.**
