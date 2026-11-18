# Ciudad del Sol — Etapa 7: VS06

**VS06 tiene recepción técnica individual satisfactoria en Unity.** Se acreditaron además tres recorridos completos encadenados con las etapas anteriores. No fueron necesarias obras ni actualizaciones de navegación.

La recepción corresponde al acceso y a los itinerarios seleccionados, no a la cámara final completa ni a todas las rutas automáticas posibles del mapa. La prueba con cuatro jugadores reales continúa pendiente.

## Extremos, cotas y alcance

Se contrastaron los dossiers Plan_20261117, Auditoria_20261117, Etapa1_20261117, RecepcionIndividual_20261117 y los informes de Etapas 2–6. VS06 figura como **D03 → P01**, una delimitación funcional del encuentro, sin puerta nueva:

| Punto | Coordenadas del piso | Función |
|---|---|---|
| Origen | (18, −38, 9) | Ancla compartida con el final recibido de VS03 |
| Giro documental | (12, −38, 9) | Aproximación sobre el pavimento existente |
| Destino | (9, −38, 15) | Acceso a P01, subzona funcional dentro de D03 |

Longitud documental: **12,708 m**, sin desnivel. El NavMesh devuelve también un camino directo de **10,817 m** entre los extremos, sobre el mismo suelo, que pasó física y navegación en ambos sentidos. Se comprobaron ambos trazados; no se impuso el giro como si hubiera una puerta o pared.

P01 es una envolvente funcional candidata de 18 × 30 m, X=0..18 y Z=0..30, dentro de la cámara existente. No se construyó una arena, no se recibieron sus 540 m² ni su volumen completo, ni se modificó la cámara final. D03 tiene centro documental (18,−38,18); el origen de VS06 es un **ancla de acceso**, no el centro de la sala.

## Resultados reales de VS06

Pruebas ejecutadas en **Unity 6000.3.23f1, Editor gráfico, Direct3D 11, Edit Mode**, después de abrir el proyecto desde disco. PhysX y NavMesh son los reales de la escena definitiva; no son pruebas sintéticas ni movimiento simulado presentado como juego real.

| Comprobación | Resultado |
|---|---|
| NavMesh entre extremos | PathComplete ida y vuelta |
| Raycasts y continuidad local NavMesh | Satisfactorios en ambos sentidos |
| Suelo, huella y cápsula Warrior | Satisfactorios |
| Barridos físicos | Sin obstáculos en ambos sentidos |
| Perfil documental a 0,10 m | **130 estaciones, 0 fallos** |
| Cápsula Humanoid sobre perfil denso | **0 fallos** |
| Camino nativo directo | 45 muestras por sentido, sin fallos físicos |
| Cuatro cápsulas estáticas Warrior | Despejadas en la banda evaluada |
| Bordes peligrosos en las secciones evaluadas | No detectados |
| Pendiente máxima medida | **0°** |
| Anchura útil mínima muestreada | **Al menos 6 m** |
| Altura libre mínima medida | **6 m** |

El buscador de anchura reutilizado se limita a seis metros: el valor no afirma que la cámara tenga solamente seis metros de ancho ni acredita toda su superficie. La altura se midió con rayos verticales en el eje y desplazamientos del radio Warrior, además de las comprobaciones completas de cápsula. La superposición del puente B04 a Y=−30 no se interpretó como una conexión vertical al piso Y=−38.

Las dimensiones se leen del prefab Warrior y del agente original: radio Warrior 0,32 m, altura 2 m, escalón 0,30 m; Humanoid 0 con radio 0,57 m. No se modificaron controles, parámetros del agente ni tolerancias. No se crearon NavMeshLinks ni consultas experimentales NavMeshQuery.

## Continuidad entre etapas: recorridos completos, no solo enlaces aislados

Se concatenaron exclusivamente anclas documentadas coincidentes. La herramienta rechaza una junta cuyos extremos no coincidan dentro de la precisión de comprobación existente; no añade un tramo ficticio para cerrar una separación.

| Itinerario recibido | Conexiones recorridas | Longitud documental | Resultado |
|---|---|---:|---|
| CONT-A03-P01 | A03 → VS01 por rodeo norte recibido → VS02 → VS03 → VS06 | **502,278 m** | Ida y vuelta satisfactorias |
| CONT-R21-P01 | R21 → unión real (120,−16,117) → resto de VS01 → VS02 → VS03 → VS06 | **418,987 m** | Ida y vuelta satisfactorias |
| CONT-B04-P01 | Extremo sur B04 → regreso de VS05, incluido enlace de 3 m → VS03 → VS06 | **167,006 m** | Ida y vuelta satisfactorias |

Se comprobaron físicamente las juntas y los giros dentro de los recorridos completos. Cada unión se evaluó también por caminos nativos completos entre sus puntos de paso; no se dedujo continuidad únicamente por tener aprobaciones anteriores. Las cápsulas Humanoid pasan los tres itinerarios completos en ambos sentidos, sin fallos físicos ni de barrido. Las anchuras útiles inferiores muestreadas de estos itinerarios son 4,74 m, 4,74 m y 4,04 m respectivamente; incluyen tramos anteriores, no son medidas exclusivas de VS06.

Estos itinerarios acreditan **continuidad por recorridos seleccionados y comprobados**. No demuestran que un agente que solicite solamente el destino final vaya a elegir siempre esas mismas anclas.

### Exploración de los caminos globales automáticos

En este apartado, externos significa ajenos al corredor seleccionado de VS06, dentro del interior; no se está interviniendo en los siete sectores exteriores.

También se solicitaron caminos globales sin imponer puntos de paso. Se conservan los resultados completos en ContinuidadGlobal/; no se los convirtió artificialmente en aprobaciones:

- **B04 ↔ P01:** recibido también como camino global automático, sin sustitución por puntos originales.
- **A03 ↔ P01:** no recibido como itinerario automático completo. La ida del receptor usa el itinerario seleccionado de sustitución, por lo que no se presenta como prueba de una ruta libre. El regreso conserva una comprobación local de continuidad no demostrada cerca de **(48,557,−16,−12,257)** y sale del corredor de referencia. Esto coincide con el ámbito del atajo inverso de VS01 que ya estaba pendiente.
- **R21 ↔ P01:** el camino global se desvía por ramas ajenas al perfil seleccionado. El receptor restringido al itinerario proyecta algunas muestras sobre una cota inadecuada: por ejemplo, el punto nativo **(73,999,−15,950,84,633)** termina evaluado como **(73,999,−24,84,633)**, con una corrección de **8,05 m**. Esos rechazos no demuestran huecos o bloqueos físicos en la ruta automática; la prueba es inconclusiva fuera del perfil recibido.

Todos esos cálculos globales devolvieron PathComplete. Eso no se tomó como aprobación física. Tampoco se declararon roturas reales del NavMesh a partir de las muestras proyectadas sobre otro perfil. No se alteraron las cotas, los validadores ni la geometría de otras etapas para hacer pasar estos informes.

**Pendiente separado:** recepción específica de los trazados automáticos externos, con sus propios perfiles reales, y comprobación de la incidencia de VS01 indicada. No impide la recepción local de VS06 ni invalida los tres itinerarios completos seleccionados que sí pasaron, pero impide afirmar que todas las rutas globales automáticas de la dungeon están certificadas.

## Regresiones reales de las Etapas 1 a 6

| Etapa | Resultado nuevo de Unity |
|---|---|
| 1 | 1/1 |
| 2 | 6/6 |
| 3 | 3/3 |
| 4 | 2/2, incluidas sus comprobaciones laterales de escaleras |
| 5 | 2/2 por puntos seleccionados y 2/2 por caminos globales naturales |
| 6 | VS03 1/1 por puntos y camino global, perfil denso y cápsulas Humanoid satisfactorios |

El digest de triangulación de la sesión recién abierta coincide con el de la recepción persistida de Etapa 6. La escena y los recursos de navegación se cargaron desde sus archivos existentes; no fue necesario guardar una nueva versión de ninguno.

Resultado final de la herramienta:

```text
VS06=True; Denso=True; Continuidad=True; GlobalExploratoria=False;
HumanoidVS06=True; HumanoidContinuidad=True; Regresiones1a6=True;
DatosRecargados=True; Integridad=True; RecepcionEtapa7=True
```

GlobalExploratoria=False corresponde a la limitación explícita anterior; no se omite de este dictamen. Los informes JSON nativos están en **Backups/CiudadDelSol/Interior/Etapa7_20261118/**, especialmente VS06/, ContinuidadSeleccionada/, ContinuidadGlobal/, HumanoidVS06/, HumanoidContinuidad/ y Regresiones/.

## Inspección visual y decisión arquitectónica

Se inspeccionaron en el Editor el conjunto del acceso, la junta con VS03, el giro/llegada y el sentido de retorno. El pavimento existente es continuo; no se observaron huecos, piezas flotantes, materiales ausentes u obstáculos que justifiquen obras en la banda recibida. No se encontraron bordes expuestos dentro de las secciones comprobadas.

**Correcciones construidas: ninguna.** Se conservaron las escaleras, protecciones, puentes, pilares, suelos y la arquitectura monumental. No se añadieron salas, puertas, colliders, volúmenes o cámaras. El aislamiento visual utilizado para las capturas se restauró al finalizar, sin guardar cambios temporales de visibilidad.

Capturas reales: [Galeria.html](Galeria.html). Plano actualizado: [Plano.svg](Plano.svg). Las superficies y envolventes de contexto del plano no son una certificación de la arena completa.

## Archivos, respaldo y compilación

Archivos nuevos:

- **Assets/ROM/Editor/CiudadDelSolInteriorStageSeven.cs** y su .meta: receptor manual de VS06, composición y comprobación de continuidad, perfil denso y encuadres del Editor.
- Este dossier, [Recepcion.csv](Recepcion.csv), [Recorridos.csv](Recorridos.csv), plano, galería, capturas y [SHA256_sin_cambios.json](SHA256_sin_cambios.json).
- Evidencias de ejecución y respaldo en Backups/CiudadDelSol/Interior/Etapa7_20261118/.

No se modificaron archivos preexistentes. La herramienta amplía la misma clase parcial del receptor y reutiliza InspectSelected, Probe, Sweep, MeasureWidth, los planes de Etapas 4–6 y la recepción de regresiones; no incorpora otro sistema de navegación o gameplay.

Se respaldaron y verificaron **58 archivos protegidos**, incluida la escena, los recursos NavMesh y los receptores dependientes. Sus hashes antes y después son iguales. Las modificaciones que Git ya muestra en la escena y el NavMeshData proceden de etapas anteriores y se conservaron. Los sistemas de Mirror, combate, HUD, controles, salud, XP, progresión y población exterior no se tocaron.

Compilación Editor con Roslyn: **código de salida 0**. Unity importó el receptor y ejecutó la versión actual. Se conservan avisos CS0618 de herramientas anteriores; la consola final observada muestra cero errores. No hubo Play Mode, Host, commits, pushes ni cambios en staging.

Para repetir opcionalmente la recepción: **ROM → Etapa 7 interior → Recibir VS06 y continuidad**, con la escena limpia y fuera de Play. No es necesario repetirla para acreditar los resultados de esta entrega. El arranque automático del receptor solo consume una solicitud explícita de un solo uso; no quedó ninguna pendiente.

Documentación local consultada: **NavMeshSurface.md** y las guías **initialize-ai-navigation**, **unity-cli/common-workflows**. No se instalaron paquetes.

## Pendientes y siguiente alcance recomendado

- Cuatro jugadores reales, cruces simultáneos, cámaras e IA dinámica en juego: pendientes. Cuatro cápsulas estáticas no sustituyen una partida cooperativa.
- La captura manual histórica de Etapa 1 sigue sin telemetría de su último tramo; estas comprobaciones técnicas no inventan ese movimiento.
- Los itinerarios automáticos externos descritos arriba necesitan su recepción específica antes de certificar la navegación global completa de enemigos.
- D03/P01 no se certifican como arena ni volumen completo de jefe por haber recibido su acceso.

Recomiendo abordar primero la recepción de los trazados automáticos externos con perfiles reales y delimitar después la auditoría volumétrica de D03/P01. No se comenzó ninguna de esas intervenciones.
