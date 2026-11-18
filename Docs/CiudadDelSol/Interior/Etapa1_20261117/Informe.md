# Ciudad del Sol — Etapa 1: recepción real A03–R21

17/11/2026. **Etapa arquitectónica no cerrada. No se guardó ni modificó la escena.**

## Resultado y decisión

Unity confirma una alternativa real por el distribuidor norte. El problema del dossier es el eje que atraviesa una pilastra, no una desconexión total A03–R21. No se ha demostrado que falte suelo ni que haga falta retirar arquitectura. Por ello no se hicieron obras gratuitas ni se cortaron pilastras, columnas o barandas.

El rodeo norte pasa apoyo, huella de cápsula, colisión y barridos del eje. El NavMesh existente devuelve camino completo en ambos sentidos y sus esquinas también pasan las comprobaciones físicas. **Esto no acredita todavía tránsito de cuatro jugadores, ancho continuo de seis metros ni protección de cada borde del ámbito.** No se ha construido una nueva conexión ni recibido el resto de la dungeon.

## Fuentes y ámbito

Se consultaron Informe, salas y conexiones del Plan_20261117 y Auditoria_20261117. A03: descanso inferior, centro (156,-16,102), envolvente 24×24 m, raíz Subsuelo_Acceso_Etapa01. R21: nave (117,-16,123), envolvente 30×42 m, jerarquía Subsuelo_34_Naves_Sin_Reticula/Nave 102/102 (30×42).

Las consultas se limitaron a recorridos dentro de X=102..180, Z=90..144, Y=-20..-4. Se consultaron colliders activos de la escena completa para no omitir obstáculos de otras raíces que interceptasen el trayecto. No se activaron colliders deshabilitados. El inventario previo de paredes deshabilitadas con riesgo no aporta posiciones dentro de esta caja; eso no sustituye una recepción de todos los límites laterales.

No se recibió AC01–AC04 completo, las rutas profundas, H46 ni las restantes alas. La prueba comienza en el piso de A03, no en el exterior.

## Hallazgos demostrados mediante Unity

1. **Eje original bloqueado:** de (135,-16,105) a (120,-16,105), siete muestras de cápsula interceptan Nervaduras, pilastras y transiciones de R21 alrededor de (132,-16,105). MeshCollider activo de Nave_08_2.asset. Se conserva íntegro.
2. **Rodeo Z111 no aceptado:** 38 fallos del eje y 531 muestras de ancho fallidas. La propuesta documental no se considera transitable por sí sola.
3. **Rodeo Z117:** 391 muestras del eje, cero problemas de apoyo/cápsula/barrido. 52 muestras fallidas en la banda objetivo de seis metros. No se ocultan ni se convierten en aprobadas.
4. **Barandas A03:** en X≈167, puntos Z=96.32 y 101.68, las cajas de Railing_01 miden 6.0004425 m en Z. El contacto calculado de cápsula es aproximadamente 0.00022155 m: una penetración de 0.22 mm en el extremo de una banda teórica exacta de seis metros. No es un bloqueo del eje ni se acepta como libre en la prueba de ancho. También se detectan extremos de barandas en X≈144.53 y 167.47, Z=111.
5. **Columnas próximas a Z120:** Column_Ceiling_02 de Subsuelo_Ala_Este_Cierre_Etapa05/04 - Columnas monumentales, centros X=144 y 132, bloquean muestras laterales alrededor de Z=119.19. Se midieron penetraciones aproximadas de 0.078 m. Estas son interferencias reales con la banda, no simples redondeos. No se movieron.
6. **Rodeos adicionales:** Z123 presenta 13 fallos de eje y 88 de ancho; Z126 pasa el eje pero tiene 30 fallos de ancho; Z129 pasa el eje pero tiene 22 de ancho. Los últimos incluyen nervaduras de Nave 132/120 (18×78) y R21. No se ha acreditado ninguna banda uniforme de seis metros entre todas estas estructuras.

## Trazado físico comprobado

Todos los puntos tienen piso Y=-16; no son spawns ni órdenes de teletransporte:

| Punto | X | Y piso | Z | Función |
| --- | ---: | ---: | ---: | --- |
| S1-01 | 156 | -16 | 99 | Salida desde descanso A03 |
| S1-02 | 174 | -16 | 99 | Apertura oriental existente |
| S1-03 | 174 | -16 | 117 | Rodeo del frente de la nave |
| S1-04 | 120 | -16 | 117 | Aproximación interior a R21 |
| S1-05 | 117 | -16 | 123 | Centro de referencia R21 |

Este replanteo queda documentado; no modifica UpperRoute de generadores globales ni crea objetos en la escena. La visualización SVG es un esquema a escala del ámbito y del trazado, no una reconstrucción de los muros.

## Navegación y física ejecutadas

Unity 6000.3.23f1, batch Edit Mode, escena definitiva real, sin Play Mode ni Host. NavMesh cargado: 33.914 vértices; 17.991 colliders activos no trigger consultables en la escena. Son inventarios globales, no elementos del sector ni coste de render.

Warrior original: CharacterController radio 0.32 m, altura 2 m, stepOffset 0.30 m, slopeLimit 48°. La capa de ejecución es character=6: CharacterInstance/Health la asignan durante el juego. No se modificó el prefab. Humanoid 0: radio 0.57 m, escalón 0.36 m; no se cambiaron parámetros.

- Raycasts sobre suelo físico del nivel correcto; diferencia máxima 0.03 m para las guías.
- Huella: ocho muestras periféricas por cápsula.
- OverlapCapsule con máscara de colisión real de character; no se encogen cápsulas ni se ignoran obstáculos.
- Ejes muestreados cada 0.25 m como máximo y barridos de cápsula entre muestras.
- Banda objetivo seis metros muestreada transversalmente: criterio independiente del eje. No se certifica cada punto continuo de esa banda.
- Proyección de NavMesh local a 0.35 m, Humanoid 0, control de altura y ámbito.
- CalculatePath completo ida y vuelta. Cada dirección: 327 muestras sobre las esquinas calculadas, cero problemas físicos, más barridos entre esquinas. Las rutas calculadas no son necesariamente la polilínea de guía: ambas se examinan por separado.
- Control negativo con la pilastra: correctamente rechazado.
- No se creó ninguna consulta experimental NavMeshQuery ni se modificaron NavMeshData.

## Límites y errores de preparación de las pruebas

La primera ejecución usó la capa serializada del prefab, Default=0, en lugar de character=6; sus resultados físicos no son válidos y no se usan para decidir obras. Se corrigió únicamente la herramienta; las ejecuciones siguientes emplearon la máscara real.

Se intentó preparar movimiento de cuatro CharacterController en física aislada sin Play. Unity rechazó la activación de preview scenes y la creación/activación del espacio temporal con las API ensayadas en Edit Mode. **Ninguna de esas pruebas movió cuatro cápsulas.** Los errores y resultados permanecen en Backups; no se presentan como éxito. La versión final de la herramienta ya no intenta esas operaciones y declara explícitamente movimiento pendiente. No simula la física de la escena original como alternativa.

Cada proceso batch estuvo supervisado por su PID, ruta, método y hora real de inicio, con plazo 180 segundos. Todos terminaron sin timeout; no se detuvo otra instancia. No se inició Host ni población en estas pruebas.

## Qué se conserva y qué cambió

Conservados sin cambios: escena, pisos, escaleras, pilastra R21, columnas monumentales, barandas, colliders activos/desactivados, prefabs, parámetros NavMesh y ambos NavMeshData, población exterior de 48 puestos, modos de juego, combate y red. No hay actualización de navegación necesaria para la alternativa que ya existe.

Archivos nuevos:

- Assets/ROM/Editor/CiudadDelSolInteriorStageOne.cs y su .meta: recepción manual, solo lectura, sin bootstrap, guardado ni horneado.
- Docs/CiudadDelSol/Interior/Etapa1_20261117/: este informe, resultados nativos, integridad y esquema.
- Backups/CiudadDelSol/Interior/Etapa1_20261117T162436Z/: respaldo y manifiesto SHA-256, logs, intentos y salidas de compilación.

No se modificó ningún archivo preexistente. Trece entradas protegidas del respaldo conservan sus hashes, incluyendo escena, NavMeshData, población, malla de R21, parámetros y paquetes. El hash de escena antes/después es 45B24571909CDBB425043FEC803381956D0D954F61C961AE2C733F109B8FFF35. No se guardó porque no hay obras justificadas que persistir.

## Compilación y comprobaciones

El ensamblado Editor, incluyendo la herramienta, compiló con el Roslyn de esta instalación de Unity. La compilación e importación reales de Unity finalizaron sin errores en la recepción final. Las advertencias de Experimental NavMeshQuery pertenecen a CiudadDelSolNavMeshDiagnostics.Encounters.cs preexistente; no se añadió ese uso ni se invocó esa herramienta. Runtime no fue modificado. No se hicieron commits ni pushes.

## Prueba manual pendiente y criterio de cierre

1. Abrir Dungeon_Isla_del_Sol fuera de Play, sin cambios pendientes.
2. Menú ROM → Dungeon Ciudad del Sol → Interior - Etapa 1 → Validar A03-R21 (solo lectura). Examinar Recepcion.json en la carpeta indicada por Console: eje, banda y NavMesh se informan por separado. La herramienta no entra en Play ni guarda.
3. Iniciar la sesión habitual de desarrollo. Llegar físicamente al descanso A03 por AC04 y recorrer el trazado anterior hasta R21 y volver, sin trasladar el personaje mediante Transform como sustituto de recorrer el acceso.
4. Verificar CharacterController real, escalones, giros, roce con barandas, cámara y ausencia de caídas. Recorrer también el perímetro inmediato sin salir del ámbito. La llegada por el último tramo de escalera no fue simulada aquí.
5. Repetir con cuatro jugadores: cruce en ambos sentidos y formación de dos por dos, sin empujarse a bordes ni bloquear las bocas. No se ha probado Mirror ni cuatro jugadores en esta intervención.

**Estado final: parcial, pendiente de recepción jugable/cooperativa.** Hay un eje alternativo comprobado con física y NavMesh reales; no hay motivo demostrado para rellenar pisos ni retirar pilares. Si se exige seis metros útiles uniformes en toda la conexión, los rodeos ensayados no lo acreditan: hace falta replantear el hueco entre barandas/nervaduras/columnas con aprobación de diseño antes de alterar estructuras importantes. No se propone rebake ni reducción de exigencias para cerrar artificialmente la etapa.