# Ciudad del Sol — Construcción y recepción de Etapa 4

**VS01 y VS02: recepción técnica individual completada en Unity. Cuatro jugadores reales: pendiente.**

## Construcción guardada

Se ensancharon 7 módulos originales de Staircase_01 de 6 a 8 m, conservando ejes y desniveles. Se añadieron 20 franjas de piedra físicamente sólidas y 39 protecciones. No se ejecutaron generadores globales ni se repitió la búsqueda de 40 combinaciones.

VS01: cuatro vuelos desde (45, −16, −12) hasta (45, −24, 12), con descanso B01 en (45, −24, 15). VS02: galería oriental, descanso (45, −24, 45) y tres vuelos desde (42, −24, 45) hasta (24, −30, 45).

Las franjas se añadieron solamente donde faltaba suelo, sin duplicar pisos coplanares. Se recortaron las ampliaciones que sobresalían sobre el inicio de una pendiente: ese solape generaba un labio brusco. Los pisos originales no se escalaron.

Se reutilizaron Platform_Base_01, Railing_01 y Railing_Staircase_01 de Eternal Temple, con sus materiales originales. Las mallas adaptadas son recursos independientes; los prefabs originales permanecen intactos. Cada protección nueva tiene un collider prismático continuo de 1 m de espesor y 1,5 m de altura, embebido 0,10 m. Se conserva el relieve visual ornamental sin utilizarlo como cara irregular de colisión.

El ancho físico de los vuelos es 8 m. Las caras interiores de las protecciones dejan aproximadamente 6,10 m entre sí en los tramos rectos. La tabla de recepción informa el límite inferior muestreado del recorrido completo; no debe confundirse con una medición uniforme de toda la dungeon.

Coordenadas completas y prefabs: [Escaleras.csv](Escaleras.csv), [Ampliaciones.csv](Ampliaciones.csv) y [Protecciones.csv](Protecciones.csv). Plano: [Plano.svg](Plano.svg).

## Pruebas reales

Unity 6000.3.23f1, batch/nographics, Edit Mode, PhysX y NavMesh nativos. No se ejecutó Play, Host, movimiento real del Warrior ni cuatro clientes. La cápsula y sus dimensiones proceden del prefab Warrior.

| Etapa | Conexiones recibidas después de recargar desde disco |
|---|---|
| 1 | 1/1: A03-R21 |
| 2 | 6/6: H12, H06, H13, H14, VC01, VC02 |
| 3 | 3/3: VE01, VE02, VD01 |
| 4 | 2/2: VS01, VS02 |

Se verificaron apoyo físico, huella, cápsula, barridos, pendientes/escalones, planta compatible, anchura, bordes y caminos completos de ida y vuelta con raycasts locales. Además se comprobaron ambos flancos de los siete vuelos cada 0,10 m: 854 muestras sin riesgos detectados. Se mantuvieron controles negativos contra obstáculos y plantas incorrectas.

La recepción de Etapa 4 exige pasar por los puntos y descansos originales. Cada tramo NavMesh se calcula y después la ruta concatenada se somete íntegramente a los mismos controles físicos y nativos. Los caminos globales más cortos se conservan como evidencia separada: no se declaran aprobados sus atajos fuera del corredor ni sus comprobaciones fallidas. Esta selección explícita solo se activa para esta recepción; no cambia el movimiento de enemigos ni sus decisiones.

En concreto, el camino global inverso de VS01 mantiene una comprobación local de continuidad no demostrada cerca de (48,557; −16; −12,257), además de salir del corredor documentado. No se anuló ese rechazo ni se calificó de falso positivo sin prueba. La aprobación corresponde al recorrido completo alternativo por los descansos originales, que sí pasó todas las comprobaciones. Véase Caminos_globales_no_seleccionados.csv. Una prueba real con NavMeshAgent/Warrior sigue siendo necesaria para evaluar la elección autónoma de atajos globales.

## Navegación compartida y protección del resto del mapa

Se actualizó una copia del NavMeshData del mismo NavMeshSurface manual, con 18050 fuentes obtenidas mediante la selección original del paquete. No se creó otro Surface, enlace ni suelo ficticio. Parámetros: radio 0.57 m, escalón 0.36 m, vóxel 0.10 m.

La candidata conservó 103524 testigos proyectados fuera del área de intervención, comprobados a 0,001 m; también se verificó simétricamente que no apareciera navegación nueva fuera del área. Se compararon 42 rutas dirigidas entre las siete anclas exteriores (30 completas en la referencia), preservando todas las completas.

El área de comparación excluida corresponde al volumen autorizado X=16..68, Y=−33..−13, Z=−20..54. Se contrastaron las firmas de todos los objetos originales no autorizados; no cambiaron. La cámara final, las Etapas 1–3 y la población exterior no se modificaron.

La escena conservó exactamente los 59 NavMeshModifier originales. Las 12 comparaciones exteriores que ya no daban camino completo en la referencia no se reinterpretan como aprobadas; se conservaron las 30 completas existentes y la cobertura fuera del sector.

Solo después de superar la prevalidación se guardaron la escena y el mismo NavMeshData, conservando sus GUID. Otra ejecución de Unity volvió a cargar ambos recursos, comprobó la huella de la triangulación persistida y repitió las recepciones y la navegación exterior. Resultado: confirmado.

## Archivos y respaldo

Respaldo completo verificado antes de intervenir: Backups/CiudadDelSol/Interior/ConstruccionEtapa4_20261118/Antes_SHA256.json y archivos originales bajo esa carpeta. El comparativo final está en SHA256_comparacion.json.

Archivos respaldados que cambiaron de hash:

- Assets/Scenes/Dungeon_Isla_del_Sol.unity
- Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset
- Assets/ROM/Editor/CiudadDelSolInteriorStageTwo.cs

Además: nuevo CiudadDelSolInteriorStageFour.Construction.cs y .meta; mallas de las protecciones y .meta bajo Assets/ROM/Generated/InteriorEtapa4_20261118T044438772; este dossier. CiudadDelSolInteriorStageTwo.cs añade la inspección de candidata sin guardar y la selección explícita opcional de puntos originales; no cambia tolerancias ni reglas físicas.

## Pendientes y límites

- Prueba funcional de cuatro jugadores reales, cruces simultáneos y combate: pendiente. Cuatro cápsulas estáticas no equivalen a cuatro clientes.
- Recorrido real del Warrior en las nuevas protecciones y revisión estética/iluminación en el Editor gráfico: pendiente. Esta recepción utilizó PhysX/NavMesh reales, pero sin renderizado.
- El último tramo manual sin telemetría de Etapa 1 continúa sin acreditarse; no fue completado ni simulado por estas pruebas.
- No se certifica toda combinación de saltos intencionados o caída voluntaria: se recibió el recorrido y su protección para circulación ordinaria.
- Compatibilidad geométrica con Humanoid 0 demostrada; no se ejecutaron enemigos ni red durante la recepción.

Compilación del Editor: sin errores; Unity recompiló/importó los scripts y ejecutó la recepción. Persisten advertencias CS0618 preexistentes de herramientas ajenas sobre API experimental; esta construcción no crea NavMeshQuery.

Para comprobar visualmente: abrir Dungeon_Isla_del_Sol, seleccionar CiudadDelSol_Etapa4_Protecciones_VS01_VS02 y recorrer VS01/VS02. No hace falta hornear ni ejecutar generadores. Se puede continuar con la planificación de la siguiente etapa; no se construyó ninguna zona adicional. Sin commits ni pushes.
