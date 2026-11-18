# Ciudad del Sol — Auditoría visual y arquitectónica final de Etapa 4

**Resultado: revisión visual satisfactoria del recorrido recibido de VS01 y VS02. No se efectuaron obras, cambios de colliders ni actualizaciones de NavMeshData. El atajo global inverso mantiene una incidencia de acreditación; cuatro jugadores reales siguen pendientes.**

## Inspección realmente ejecutada

Se abrió la escena original Assets/Scenes/Dungeon_Isla_del_Sol.unity en el Editor gráfico Unity 6000.3.23f1 con Direct3D 11 (Intel UHD Graphics). No se utilizó una copia de escena, un render offline ni una reconstrucción del escenario. Se recorrieron 14 encuadres de Scene View, acercamientos, contracaras y vistas de contexto, fuera de Play Mode.

Se guardaron 21 capturas reales del Editor. [Galería completa](Galeria.html). [Inventario de los 66 elementos](Inventario_visual.csv). Las imágenes no se retocaron ni se sustituyeron por planos.

El aislamiento temporal incluyó las siete escaleras originales modificadas, las veinte ampliaciones, las treinta y nueve protecciones y pisos existentes próximos al recorrido. Se ocultaron temporalmente otras plantas para poder observar las uniones. Se contrastaron vistas de contexto; al terminar se salió del aislamiento y se restauró la arquitectura visible completa. Los elementos de contexto ausentes en una imagen aislada no deben interpretarse como arquitectura eliminada ni como piezas flotantes.

| Conjunto | Cantidad | Resultado visual | Capturas principales |
|---|---:|---|---|
| Escaleras VS01 | 4 | Peldaños, caras laterales y uniones con descanso alineados; parapetos integrados | 03, 04, 12, 14, 15 |
| Escaleras VS02 | 3 | Continuidad visual de los tres vuelos y llegada inferior; sin piezas separadas detectadas | 09, 10, 11, 13 |
| Ampliaciones de piedra | 20 | Superficies y franjas adyacentes revisadas; piedra solar y cotas coherentes con el piso vecino | 01–02, 04–08, 10–11, 13–15 |
| Protecciones | 39 | Parapetos, balustres, esquinas y remates revisados por conjuntos y contracaras | 02–15 |

El inventario relaciona cada elemento con sus coordenadas y vistas. La inspección se refiere a sus caras expuestas y encuentros arquitectónicos; no implica inspección visual de caras internas enterradas ni certificación de iluminación desde todas las cámaras posibles.

## Hallazgos y decisión de no modificar

- No se observaron materiales ausentes o magenta, barandas suspendidas respecto de sus apoyos ni huecos nuevos en las superficies del recorrido.
- Las uniones de los vuelos y descansos conservan el aparejo de piedra original. Las franjas estrechas y los cierres cortos muestran cambios de densidad de textura y juntas de módulo; no se identificó un defecto que justifique reemplazarlos.
- Las aperturas de las protecciones en los cruces se conservaron: no se cerraron accesos para hacer más uniforme una baranda. Su seguridad se contrastó con los controles físicos existentes.
- Las caras laterales escalonadas de los módulos y el encuentro de la ampliación inferior se inspeccionaron de cerca (13_VS02_junta_detalle.jpg). No se confirmó una pieza flotante ni un hueco que requiera una reparación.
- Las piezas diagonales vistas en el contexto amplio no se atribuyeron sin comprobación a las protecciones nuevas. La revisión aislada permitió distinguir las piezas de Etapa 4 de la ornamentación del núcleo original. No se modificó el Sol ni la cámara final.
- No se detectó una protección nueva que obligue a corregir la circulación recibida. Las aberturas originales y el espacio central permanecen libres en las comprobaciones realizadas.

Como control auxiliar, se compararon los perfiles de las veinte parejas de mallas visuales/colliders de las protecciones. En las inclinadas, el perfil superior visual se sitúa aproximadamente a 1,383 m frente a 1,400 m del collider, medido respecto de su pendiente; no existe la gran discrepancia que sugeriría comparar solamente las cajas AABB verticales. Este control de archivos complementa las imágenes y no las sustituye.

No se corrigió arquitectura porque no se demostró un defecto que hiciera necesario modificarla. No se guardó la escena ni se volvió a hornear.

## Atajo inverso de VS01

Se reprodujo la incidencia con el NavMesh real desde el Editor y se inspeccionó visualmente la junta. El par de muestras que provoca el rechazo es:

- A: (48.386, -15.951, -12.086); distancia a la arista: 0.014286 m.
- B: (48.557, -15.950, -12.257); distancia a la arista: 0.000000 m.
- Raycast A→B: bloqueado=True, impacto en (48.400, -15.950, -12.100); máscara 0.
- Raycast B→A: bloqueado=False.
- CalculatePath A→B: completo=True, 2 esquinas; B→A: completo=True, 2 esquinas.

Las pruebas locales de apoyo, huella, cápsula y barridos no encontraron fallos físicos en ninguno de los sentidos; tampoco faltó proyección NavMesh. La validación estricta del trazado local A→B conserva CONTINUIDAD_LOCAL_NAVMESH_NO_DEMOSTRADA, mientras el trazado B→A pasa. La reproyección independiente de las muestras originales reproduce la incidencia.

**Conclusión comprobada:** no hay evidencia de desconexión topológica local ni de un obstáculo físico en esa junta; hay una asimetría real entre las consultas de raycast, con un extremo sobre una arista. Es compatible con una discrepancia numérica/de representación del borde, pero no se acredita por ello todo el atajo global. El impacto no coincide con el extremo dentro de la igualdad vigente y no se añadió una excepción para ignorarlo.

**Estado:** el atajo global continúa sin recepción completa; no se declara resuelto ni se modificó el validador. El recorrido por los descansos originales sí conserva su recepción bidireccional. No se ejecutó un agente dinámico ni un Warrior por ese atajo durante esta auditoría. No hay una obra física justificada con estos resultados.

Evidencia original: Backups/CiudadDelSol/Interior/AuditoriaVisualEtapa4_20261118/Atajo/Recepcion.json y Atajo_detalle.jsonl.

## Regresiones reales en Unity

| Etapa | Resultado |
|---|---|
| 1 | 1/1: A03-R21 |
| 2 | 6/6: H12, H06, H13, H14, VC01, VC02 |
| 3 | 3/3: VE01, VE02, VD01 |
| 4 | 2/2: VS01, VS02 |

Se reutilizó el motor recibido: PhysX, huella, cápsula del Warrior, barridos, pendientes/escalones, ancho, bordes, proyección por planta, rutas completas y raycasts locales. También pasaron de nuevo las 854 muestras laterales de los siete vuelos. Se conservaron las tolerancias y los controles negativos.

Límites inferiores muestreados del recorrido: VS01 4,94 m; VS02 4,74 m. Los vuelos conservan 8 m físicos y aproximadamente 6,10 m entre protecciones en los tramos rectos. La prueba de cuatro cápsulas estáticas no equivale a circulación real de cuatro jugadores.

Compilación del Editor y ejecución de los menús: sin errores de C#. Consola al finalizar: 0 errores y 7 advertencias preexistentes CS0618 sobre herramientas ajenas de NavMesh experimental. No se borró la consola para ocultar errores. El visor nuevo no utiliza NavMeshQuery ni ejecuta tareas durante el gameplay.

## Archivos, respaldo y protecciones

Nuevo: Assets/ROM/Editor/CiudadDelSolInteriorStageFour.Visual.cs y su .meta. Añade únicamente encuadres manuales de Scene View, aislamiento temporal, controles de navegación del visor y la reproducción explícita de las pruebas existentes. No tiene inicialización automática ni cambia criterios de aprobación.

Nuevos: este dossier, Inventario_visual.csv, Galeria.html y Capturas/*.jpg. Los registros, manifest SHA-256, copias de seguridad y datos nativos están bajo Backups/CiudadDelSol/Interior/AuditoriaVisualEtapa4_20261118/.

No se modificó ningún archivo preexistente de escena, arquitectura, prefabs, mallas, colliders, NavMesh, gameplay ni validadores. El SHA-256 antes/después coincide para la escena, el NavMeshData y CiudadDelSolInteriorStageTwo.cs. Las modificaciones que Git ya mostraba en escena/NavMesh pertenecen a la construcción anterior y no fueron revertidas. Se conservaron los 59 NavMeshModifier al permanecer idéntica la escena.

Se leyó la guía unity-cli.md y common-workflows.md para abrir el Editor; se consultaron las API locales de SceneView y SceneVisibilityManager. La conexión Pipeline no estaba instalada; no se instaló ningún paquete. Se utilizó la interfaz gráfica real mediante la habilidad de control del equipo.

## Repetir la revisión sin obras

1. Abrir la escena original, fuera de Play Mode, sin cambios pendientes.
2. ROM → Etapa 4 visual → Siguiente encuadre. Utilizar Anterior/Siguiente en la barra de Scene View; Ver contexto muestra el núcleo monumental original. Las vistas pueden ampliarse con Shift+Space y rueda del mouse.
3. Atajo: prueba nativa repite las consultas locales y las regresiones, sin mover al Warrior ni guardar la escena.
4. Usar Restaurar visibilidad, o Exit en Isolation View, al terminar. No se necesita un Bake.

## Estado y alcance recomendado de Etapa 5

**Etapa 4 está lista para continuar con la siguiente zona por su recorrido recibido, tanto en revisión visual como en recepción técnica individual.** Continúan pendientes los cuatro jugadores reales, el último tramo manual sin telemetría de Etapa 1 y la acreditación dinámica del atajo global. No se presentan estas pruebas como realizadas.

Recomiendo limitar Etapa 5 a auditar y recibir VS04 y VS05 antes de construir:

- VS04: B01→B03, Y=−24; (51,−24,39) → (45,−24,39) → (18,−24,39) → (−15,−24,39). Puente transversal y accesos.
- VS05: B02→B04, Y=−30; (21,−30,45) → (21,−30,39) → (21,−30,−3) → (21,−30,−9). Incluir la interfaz desde la llegada actual (24,−30,45) hasta ese eje, sin presuponer que esté recibida.
Comprobar primero arquitectura existente, ancho, cabeza libre bajo puentes, interferencias de plantas, protección de bordes y NavMesh bidireccional. VS03 desciende a D03, la cámara final; debe quedar fuera de esta propuesta acotada hasta contar con autorización específica. No se comenzó ninguna construcción de Etapa 5.

Sin commits ni pushes.
