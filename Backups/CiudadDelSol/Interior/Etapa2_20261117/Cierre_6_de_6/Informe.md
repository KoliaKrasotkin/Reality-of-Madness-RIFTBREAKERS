# Ciudad del Sol — Etapa 2: conexiones del distribuidor y primer descenso

Resultado real de Unity: **5/6 conexiones con recepción técnica individual; 1 pendientes**. No es una certificación de cuatro jugadores.

## Auditoría del trabajo interrumpido

Se conservaron los seis IDs seleccionados y la herramienta existente CiudadDelSolInteriorStageTwo.cs. La primera intervención no quedó vacía: ya había compilación y recepción real de las seis conexiones en Unity (17/11/2026, 19:54 UTC), con H06, H13 y H14 aprobadas. Faltaban resolver el diagnóstico de H12, el perfil de las escaleras de VC01–VC02, documentación, plano y comprobaciones de regresión. No se reinició el trabajo ni se creó otro receptor.

Se contrastaron los 73 enlaces originales y los dossiers Plan_20261117, Auditoria_20261117, Etapa1_20261117 y RecepcionIndividual_20261117. La clasificación de los 73 se conserva en Backups/.../Etapa2_20261117/Revision_73_conexiones.json; 67 enlaces quedan fuera de esta intervención. Se preservan VS y la cámara final D03.

## Conexiones y coordenadas

| Código | Enlace | Piso inicial/final | Estado técnico | Anchura útil inferior muestreada | Cuatro cápsulas estáticas |
| --- | --- | --- | --- | --- | --- |
| H12 | R21 ↔ R23 | -16 → -16 m | Recibida | 5,22 m | Despeje geométrico |
| H06 | R23 ↔ G11 | -16 → -16 m | Recibida | 6,00 m | Despeje geométrico |
| H13 | R21 ↔ G05 | -16 → -16 m | Recibida | 6,00 m | Despeje geométrico |
| H14 | G05 ↔ R34 | -16 → -16 m | Recibida | 6,00 m | Despeje geométrico |
| VC01 | R21 (cola de VC01) ↔ B05 | -16 → -24 m | Recibida | 4,04 m | Despeje geométrico |
| VC02 | B05 ↔ D01 (acceso) | -24 → -34 m | Pendiente | 3,84 m | Despeje geométrico |

La anchura es una cota inferior muestreada, limitada a seis metros en el buscador; no afirma seis metros continuos ni incluye maniobras/cámaras de cuatro humanos. Las cuatro consultas transversales usan las dimensiones originales del Warrior, sin crear jugadores ni NPC.

### H12

(117,00, -16,00, 123,00) → (117,00, -16,00, 165,00) → (123,00, -16,00, 165,00)

NavMesh: PathComplete ida y PathComplete vuelta. Física nativa: satisfactoria / satisfactoria. Guardas de borde: comprobadas en las secciones inspeccionadas.

Muestras del eje: 194; de los caminos nativos: 175 / 175. Pendiente máxima del apoyo en el eje: 0,00°.

Se conservaron los ejes y la arquitectura existentes.

### H06

(123,00, -16,00, 165,00) → (105,00, -16,00, 165,00)

NavMesh: PathComplete ida y PathComplete vuelta. Física nativa: satisfactoria / satisfactoria. Guardas de borde: comprobadas en las secciones inspeccionadas.

Muestras del eje: 73; de los caminos nativos: 73 / 73. Pendiente máxima del apoyo en el eje: 0,00°.

Se conservaron los ejes y la arquitectura existentes.

### H13

(117,00, -16,00, 123,00) → (75,00, -16,00, 123,00)

NavMesh: PathComplete ida y PathComplete vuelta. Física nativa: satisfactoria / satisfactoria. Guardas de borde: comprobadas en las secciones inspeccionadas.

Muestras del eje: 169; de los caminos nativos: 169 / 169. Pendiente máxima del apoyo en el eje: 0,00°.

Se conservaron los ejes y la arquitectura existentes.

### H14

(75,00, -16,00, 123,00) → (57,00, -16,00, 123,00) → (57,00, -16,00, 117,00)

NavMesh: PathComplete ida y PathComplete vuelta. Física nativa: satisfactoria / satisfactoria. Guardas de borde: comprobadas en las secciones inspeccionadas.

Muestras del eje: 98; de los caminos nativos: 77 / 77. Pendiente máxima del apoyo en el eje: 0,00°.

Se conservaron los ejes y la arquitectura existentes.

### VC01

(117,00, -16,00, 123,00) → (120,00, -16,00, 123,00) → (120,00, -16,00, 105,00) → (120,00, -16,00, 90,00) → (114,00, -16,00, 90,00) → (108,00, -18,00, 90,00) → (102,00, -20,00, 90,00) → (96,00, -22,00, 90,00) → (90,00, -24,00, 90,00) → (84,00, -24,00, 90,00)

NavMesh: PathComplete ida y PathComplete vuelta. Física nativa: satisfactoria / satisfactoria. Guardas de borde: comprobadas en las secciones inspeccionadas.

Muestras del eje: 305; de los caminos nativos: 260 / 260. Pendiente máxima del apoyo en el eje: 18,92°.

Se conservaron los ejes y la arquitectura existentes.

### VC02

(84,00, -24,00, 90,00) → (81,00, -24,00, 90,00) → (81,00, -24,00, 84,00) → (81,00, -26,00, 78,00) → (81,00, -28,00, 72,00) → (81,00, -30,00, 66,00) → (81,00, -30,00, 60,00) → (81,00, -32,00, 54,00) → (81,00, -34,00, 48,00) → (81,00, -34,00, 39,00) → (99,00, -34,00, 39,00) → (99,00, -34,00, 45,00) → (102,00, -34,00, 45,00) → (102,00, -34,00, 63,00) → (105,00, -34,00, 63,00)

NavMesh: PathComplete ida y PathComplete vuelta. Física nativa: pendiente / pendiente. Guardas de borde: comprobadas en las secciones inspeccionadas.

Muestras del eje: 432; de los caminos nativos: 333 / 333. Pendiente máxima del apoyo en el eje: 18,92°.

Se conservaron los ejes y la arquitectura existentes.

VC01 conserva su código del dossier A03→B05; esta etapa recibe solo su continuación R21→B05. No repite ni modifica el tramo A03→R21 ya recibido. Los puntos intermedios de los nueve tramos 2/6 proceden del generador CentroOriental existente y se contrastan con sus colliders reales; no son escaleras nuevas.

## Causas raíz y correcciones del receptor

1. **Esquinas exactas del NavMesh.** H12 tenía un raycast de retorno detenido en su propio origen, aunque el sentido recíproco y los caminos locales eran válidos. Ahora se conservan los impactos originales y solo se resuelven esquinas/aristas verificadas, con caminos locales rectos completos en ambos sentidos y raycasts recíprocos. La igualdad numérica sigue siendo 1 mm; no es holgura del agente ni un hueco permitido. Si ambos sentidos impactan, se requieren además testigos interiores dentro de ese mismo milímetro y ausencia de enlaces activos. No se utiliza NavMeshQuery experimental.

2. **Altura del camino entre esquinas.** La interpolación lineal de Y de esquinas lejanas no describe todos los cambios de altura del suelo. La guía física tenía NavMesh en todas sus muestras, mientras el interpolador antiguo declaraba ausencias. Entre esquinas se utiliza el perfil físico seleccionado, se conserva la altura real de las propias esquinas, se exige apoyo local y se vuelve a proyectar sobre NavMesh con control de nivel. Cada paso local acredita continuidad; no se acepta proyectar hacia otra planta.

3. **Cápsula en juntas rampa/descanso.** La cara bajo el rayo central puede ser horizontal aunque la semiesfera inferior esté apoyándose sobre la rampa adyacente. Se calcula la envolvente del apoyo real de esa semiesfera y se conservan los barridos completos, subdividiendo la trayectoria si debe seguir una junta curva. No se mueven objetos ni se altera radio, altura, skin, pendiente o stepOffset del Warrior.

4. **Suelo oculto bajo la rampa.** El receptor podía elegir el basamento inferior por cercanía a la altura esperada. Se elige la primera superficie física apta del rayo en la ventana de altura local, sin atravesar una rampa para usar el piso bajo ella. Se mantiene el límite real de escalón y se comprueban todos los obstáculos de la cápsula.

5. **Clasificación lateral de las escaleras.** El antiguo criterio cercano al eje dejaba fuera muestras laterales que aún estaban sobre el collider de la escalera. La clasificación usa sus bounds reales activos, ampliados solo por huella/escalón, y conserva el nivel local. Los otros pisos siguen con su tolerancia de 3 cm.

6. **Corredor de VC02.** El camino global directo pasaba por (92,888; -34; 45,003), unos milímetros fuera del corredor documental. Las comprobaciones físicas y de continuidad local no detectaron un corte en ese camino; el rechazo corresponde al alcance documental. Se conserva ese rechazo. Está preparada y compilada la comprobación por todos los puntos originales, sin ampliar el límite; su recepción nativa sigue pendiente mientras no aparezca una nueva ejecución en la evidencia.

7. **Datos no finitos.** SamplePosition/Raycast sin resultado pueden devolver coordenadas infinitas. El JSON usa una posición vacía únicamente cuando su indicador de validez es falso; nunca se transforma una consulta fallida en una posición acreditada. Los informes intermedios se conservan.

## Arquitectura existente y elementos conservados

El inventario del ámbito contiene 185 colliders activos y 8 de objetos inactivos. Se verificaron los nueve Staircase_01 del Centro Oriental y sus terrazas/descansos, pisos modulares y protecciones.

Las ocho columnas detectadas como inactivas ya estaban así en la escena: cuatro en el Ala Norte y cuatro en el Ala Este. No se reactivaron ni se desactivó ningún collider para obtener un resultado favorable. Sus posiciones, tipos, jerarquías, bounds y GlobalObjectId se conservan en Recepcion_Unity.json.

**Correcciones arquitectónicas efectivamente construidas: ninguna.** No se demostró que falten pisos, escaleras o descansos en estos seis enlaces; por tanto se conservan las naves, columnas, barandas y la monumentalidad del conjunto. Corregir un error de medición no justifica una demolición o un suelo auxiliar.

## Validaciones realmente ejecutadas

Unity 6000.3.23f1, ejecución final desde 2026-11-17T23:22:55.5575704Z hasta 2026-11-17T23:23:07.3304667Z. Play Mode: false. Error: ninguno. Archivo de escena intacto: true.

- Raycasts de apoyo, huella completa muestreada, overlap de cápsula y barridos de los seis ejes y de las rutas calculadas, en ambos sentidos.
- Pendientes y desniveles frente a la cápsula real: radio 0,32 m, altura 2 m, escalón 0,30 m, slopeLimit 48°. Humanoid 0 conserva radio 0,57 m, climb 0,36 m y sus parámetros de proyecto.
- Medición de anchura, cuatro cápsulas estáticas separadas y búsqueda de apoyo lateral. Una rampa transitable o un escalón bajo no se cuentan como protección: la guarda exige una barrera no caminable por encima del stepOffset.
- CalculatePath, SamplePosition, FindClosestEdge y Raycast nativos, con comprobación física y continuidad local. Los caminos parciales siguen rechazados.
- Controles negativos (pilastra, planta equivocada y vacío sobre D01): rechazados correctamente.
- Compilación Editor offline y compilación real de Unity; no se cambiaron ensamblados Runtime ni sistemas compartidos.

No hubo simulación de movimiento, CharacterControllers añadidos, Warriors instanciados ni cuatro conexiones reales. No se ejecutó Host ni se cambiaron sus límites. Las advertencias de API experimental corresponden al diagnóstico preexistente, que no se invocó.

## Estado y siguiente paso

Siguen pendientes: **VC02**. No se declara terminada la Etapa 2. El código preparado debe recibir una nueva ejecución real antes de aprobar esos enlaces.

La automatización dejó de poder acceder a la ventana del Editor abierto. El usuario indicó que tampoco puede acceder ahora. No se cerró el proceso ni se abrió otra instancia del proyecto. Queda una solicitud explícita de recepción de solo lectura en Recepcion.pending: podrá procesarse al importar los scripts, fuera de Play y con la escena limpia. La última revisión del código compila offline; no se atribuye a Unity la ejecución de esa revisión hasta obtener su informe nuevo. No hace falta repetir el recorrido manual del Warrior.

Las 40 comprobaciones de regresión de la evidencia guardada pasaron, incluyendo controles negativos, conservación de las conexiones aprobadas e integridad de diez archivos protegidos. Son comprobaciones sobre resultados nativos existentes; no equivalen a una ejecución nueva de VC02 ni a movimiento real de jugadores.

Se conservan los pendientes de la Etapa 1: último tramo sin telemetría manual y cuatro jugadores. No se completó ni reinterpretó aquella captura.

Para Etapa 3 se recomienda revisar VE01–VE02 y VD01: acceso Sureste, D02 y el lazo inferior hacia D01, aprovechando las estructuras existentes. No se autoriza implícitamente intervenir D03, el Sol Invertido ni los enlaces finales VS. La construcción de la siguiente etapa requiere su aprobación específica.

## Archivos y reproducción

Único archivo de código modificado durante esta reanudación: Assets/ROM/Editor/CiudadDelSolInteriorStageTwo.cs. Se conservaron su .meta y las herramientas previas. Se agregaron este informe, el plano y resultados derivados; respaldos bajo Backups/CiudadDelSol/Interior/Etapa2_20261117/Reanudacion/.

La escena, NavMeshData, población de 48 puestos y los cuatro scripts de la Etapa 1 conservan sus SHA-256. No hubo guardado de escena, horneado, commit ni push.

Menú de recepción existente: ROM → Dungeon Ciudad del Sol → Interior - Etapa 2 → Recibir conexiones seleccionadas (sin obras). Funciona fuera de Play, con la escena guardada cargada; no inicia el juego ni construye automáticamente. No hace falta ejecutar recorridos manuales punto por punto para repetir esta recepción.

El plano SVG muestra envolventes existentes y ejes; no representa estructuras nuevas. Los niveles están separados para no confundir pasos superpuestos.
