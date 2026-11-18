# Ciudad del Sol — Etapa 3: acceso Sureste y conexión inferior

**Estado: recepción técnica individual completa, 3/3.** Existe evidencia nativa; ver resultados individuales. Cuatro jugadores reales: pendiente.

## Alcance e inventario comprobado en archivos

Se consultaron Plan_20261117, Auditoria_20261117, Etapa1_20261117, RecepcionIndividual_20261117 y la recepción 6/6 de Etapa2_20261117. Se conserva el mismo archivo de escena de 96.932.348 bytes y su SHA-256; no se ejecutaron los generadores de construcción.

| Conexión | Extremos documentales originales | Desnivel | Recorrido documental | Dependencias existentes | Resultado |
|---|---|---:|---:|---|---|
| VE01 · A03 ↔ B06 | (156.00, -16.00, 99.00) ↔ (120.00, -30.00, -42.00) | -14 m | 317.27 m | A03; galerías a −16; cuatro y tres escaleras 2/6; descanso a −24; B06 | Recibida técnicamente |
| VE02 · B06 ↔ D02 | (120.00, -30.00, -42.00) ↔ (99.00, -34.00, -9.00) | -4 m | 78.65 m | B06 a −30; giro de terraza; dos escaleras 2/6; D02 a −34 | Recibida técnicamente |
| VD01 · D02 ↔ D01 | (99.00, -34.00, -9.00) ↔ (105.00, -34.00, 63.00) | 0 m | 87.71 m | D02 y D01 a −34; galerías inferiores y pilares de arcadas | Recibida técnicamente |

B06 es la terraza centrada en (120, −30, −42), con huella documental 24 × 12 m. D02 está centrada en (99, −34, −9), huella 66 × 78 m; D01 en (105, −34, 63), huella 54 × 66 m. A03 tiene centro documental (156, −16, 102), pero el extremo de circulación usado es (156, −16, 99). Esas envolventes no acreditan que todas sus áreas sean jugables.

La secuencia es coherente en cotas: A03 −16 → descanso −24 → B06 −30 → D02 −34 → D01 −34. VD01 cierra un lazo con el acceso VC02 de la Etapa 2; no lleva a la cámara final D03. En las escaleras la selección añade los extremos de cada módulo 2/6 que el dossier resumía en tramos largos: no crea escaleras ni cambia trazados.

**Ancla recibida en B06: (123, −30, −45).** Sustituye al centro documental (120, −30, −42) como punto de unión de VE01 y VE02, sin mover la sala ni la baranda. VE01 utiliza el rodeo norte de A03. Los ejes rechazados siguen archivados y no se presentan como transitables.

## Problemas y alternativas existentes

La auditoría de mallas, cuya firma de escena coincide con la actual, detectó una intersección del eje VE01 en (132, −16, 105) con la pilastra de `Subsuelo_34_Naves_Sin_Reticula / Nave 102/102 (30×42) / Nervaduras, pilastras y transiciones`, recurso `Assets/ROM/CiudadDelSol34Naves/Nave_08_2.asset`. Es evidencia de archivos de geometría, no un resultado nuevo de PhysX. No se elimina ni recorta esa pilastra.

El receptor comprueba primero los tres ejes originales. Solo si VE01 falla, comprueba también un rodeo existente: (156,−16,99) → (174,−16,99) → (174,−16,117) → (120,−16,117) → (120,−16,105), y desde allí la continuación original hasta B06. El tramo norte procede de Etapa 1; toda la continuación se vuelve a evaluar con física y NavMesh. Proponerlo no lo acredita. Los resultados del eje original se guardan en EjesOriginales y los del rodeo en AlternativaNorteVE01.

Las advertencias laterales de la auditoría señalan Railing_01 del Centro Oriental junto a X≈132,75, Z=63…90; barandas del descanso a Y=−24; protecciones del borde sur de B06 a Y=−30; y Column_Stackable_01 de las arcadas a Y=−34, X≈90,75. No se confunde una advertencia de cuatro carriles de cajas con un bloqueo del Warrior ni se retira una protección por esa advertencia.

### Hallazgo real de Unity: ancla documental de B06

La primera recepción nativa confirmó el obstáculo de la pilastra y otro problema compartido: el punto documental (120, −30, −42) corta lateralmente una baranda de la escalera. El MeshCollider activo es Nucleo_Monumental_Conjunto/02 - Sureste arcadas aprobadas/07 - Protecciones/Railing_Staircase_01, GlobalObjectId terminado en 1916801645-0. Hubo seis estaciones obstruidas cerca del pie y falta de proyección local del extremo. El rodeo norte evita la pilastra, pero no corrige ese extremo: tampoco se aprueba por sí solo.

El generador existente registra la salida física (123, −30, −42) → (123, −30, −45) → (111, −30, −45). Unity confirmó la alternativa completa, con ancla común (123, −30, −45), en ambos sentidos y sin atravesar la baranda. Se conserva el centro documental de la sala; solo se replantea el punto de paso. Las primeras pruebas se conservan en AntesDescansoB06 y los ensayos nuevos en DescansoRealB06.

## Resultados nativos individuales

### VE01 — recibida técnicamente

Extremos realmente evaluados: (156.00, -16.00, 99.00) ↔ (123.00, -30.00, -45.00).
Puntos de paso evaluados: (156.00, -16.00, 99.00) → (174.00, -16.00, 99.00) → (174.00, -16.00, 117.00) → (120.00, -16.00, 117.00) → (120.00, -16.00, 105.00) → (120.00, -16.00, 12.00) → (123.00, -16.00, 12.00) → (123.00, -16.00, 6.00) → (123.00, -18.00, 0.00) → (123.00, -20.00, -6.00) → (123.00, -22.00, -12.00) → (123.00, -24.00, -18.00) → (123.00, -24.00, -24.00) → (123.00, -26.00, -30.00) → (123.00, -28.00, -36.00) → (123.00, -30.00, -42.00) → (123.00, -30.00, -45.00).
Elección: Recorrido existente por descanso B06 (123,-30,-45); evita salida lateral a través de baranda; ejes originales archivados. Anchura útil inferior muestreada: 3.84 m. Cuatro cápsulas estáticas: true; protección de bordes: true.
Guía física: 1050 muestras, 0 fallos de estación, 0 barridos fallidos, 0 sin NavMesh; pendiente máxima 18.92°.
Ida: PathComplete, físico=true, impactos NavMesh sin resolver=0, muestras=1061, fallos=0, barridos=0, sin NavMesh=0, discontinuidades=0.
Regreso: PathComplete, físico=true, impactos NavMesh sin resolver=0, muestras=1053, fallos=0, barridos=0, sin NavMesh=0, discontinuidades=0.

### VE02 — recibida técnicamente

Extremos realmente evaluados: (123.00, -30.00, -45.00) ↔ (99.00, -34.00, -9.00).
Puntos de paso evaluados: (123.00, -30.00, -45.00) → (111.00, -30.00, -45.00) → (111.00, -30.00, -39.00) → (108.00, -30.00, -39.00) → (102.00, -32.00, -39.00) → (96.00, -34.00, -39.00) → (93.00, -34.00, -39.00) → (93.00, -34.00, -9.00) → (99.00, -34.00, -9.00).
Elección: Recorrido existente por descanso B06 (123,-30,-45); evita salida lateral a través de baranda; ejes originales archivados. Anchura útil inferior muestreada: 3.94 m. Cuatro cápsulas estáticas: true; protección de bordes: true.
Guía física: 300 muestras, 0 fallos de estación, 0 barridos fallidos, 0 sin NavMesh; pendiente máxima 18.92°.
Ida: PathComplete, físico=true, impactos NavMesh sin resolver=0, muestras=240, fallos=0, barridos=0, sin NavMesh=0, discontinuidades=0.
Regreso: PathComplete, físico=true, impactos NavMesh sin resolver=0, muestras=240, fallos=0, barridos=0, sin NavMesh=0, discontinuidades=0.

### VD01 — recibida técnicamente

Extremos realmente evaluados: (99.00, -34.00, -9.00) ↔ (105.00, -34.00, 63.00).
Puntos de paso evaluados: (99.00, -34.00, -9.00) → (93.00, -34.00, -9.00) → (93.00, -34.00, -3.00) → (96.00, -34.00, -3.00) → (96.00, -34.00, 27.00) → (96.00, -34.00, 33.00) → (99.00, -34.00, 39.00) → (99.00, -34.00, 45.00) → (102.00, -34.00, 45.00) → (102.00, -34.00, 63.00) → (105.00, -34.00, 63.00).
Elección: Eje documental físico. Anchura útil inferior muestreada: 5.22 m. Cuatro cápsulas estáticas: true; protección de bordes: true.
Guía física: 361 muestras, 0 fallos de estación, 0 barridos fallidos, 0 sin NavMesh; pendiente máxima 0.00°.
Ida: PathComplete, físico=true, impactos NavMesh sin resolver=0, muestras=292, fallos=0, barridos=0, sin NavMesh=0, discontinuidades=0.
Regreso: PathComplete, físico=true, impactos NavMesh sin resolver=0, muestras=292, fallos=0, barridos=0, sin NavMesh=0, discontinuidades=0.

## Herramienta, compilación y protecciones

Se reutiliza el motor de CiudadDelSolInteriorStageTwo.cs mediante una entrada parametrizada por selección y carpeta de resultados. No se duplicaron los métodos de física, cápsula, barridos, pendientes, raycasts o navegación. CiudadDelSolInteriorStageThree.cs es una selección de conexiones, un menú y una solicitud explícita de recepción; no es un sistema de gameplay.

La compilación C# de Editor pasó con el compilador instalado de Unity 6000.3.23f1. Las advertencias CS0618 pertenecen al diagnóstico experimental antiguo, que no se invoca. No se utiliza NavMeshQuery experimental ni se crean enlaces, instancias de Warrior o objetos de prueba.

La recepción actual se ejecutó con Unity 6000.3.23f1 en modo batch/nographics, después de verificar fuera del sandbox que no había otro Editor del proyecto. Se cargó la escena guardada mediante OpenScene y se invocó el mismo receptor en Edit Mode. Son consultas reales de PhysX y NavMesh; no renderizado, simulación de movimiento ni una sesión Host. Inicio: 2026-11-18T01:58:12.6645498Z; cierre de consultas: 2026-11-18T01:58:20.9374122Z. El registro completo está en Unity_recepcion_batch.log.

Comprobaciones de código e integridad: 73. Incluyen comparación literal del motor recibido, conservación de los seis planes anteriores, extremos y puntos originales de VE01/VE02/VD01 y nueve escaleras 2/6. También se contrastó la recepción nativa nueva.

Regresión nativa de Etapa 2: seis conexiones satisfactorias, sin sobrescribir la evidencia anterior. Integridad de escena, NavMeshData, población y Etapa 1: SHA-256 conservados. La modificación del receptor Editor está respaldada y declarada; no es una modificación arquitectónica.

Las consultas conservan el Warrior original (radio 0,32; altura 2; escalón 0,30; pendiente 48°), Humanoid 0 (radio 0,57; escalón 0,36), proyección local y comprobación de nivel. No se amplían tolerancias ni se cambian colliders. Las cuatro cápsulas estáticas no equivalen a cuatro jugadores reales, evitación dinámica o movimiento del CharacterController.

**Obras realizadas: ninguna.** Se aprovechó el trazado físico existente; no se construyó, eliminó arquitectura, movió barandas ni actualizó NavMeshData. Se preservan los siete sectores, las Etapas 1–2, D03, combate, Mirror, HUD, población y progresión.

## Recepción completada / reproducción

Fuera de Play, con Dungeon_Isla_del_Sol cargada y sin cambios pendientes: ROM → Dungeon Ciudad del Sol → Interior - Etapa 3 → Recibir VE01 VE02 VD01 (sin obras). La solicitud GUID Recepcion.pending permite la misma ejecución una sola vez al importar los scripts. No abre, guarda ni reconstruye escenas; no inicia Host ni mueve al Warrior. Si la escena está sucia o hay Play, se informa el bloqueo y no se fuerza la prueba.

La solicitud pendiente fue consumida por la entrada batch del receptor existente. No hace falta un recorrido manual para repetir estas consultas. El bloqueo de la automatización de escritorio no se evitó: se usó la instalación local de Unity únicamente después de confirmar que el Editor estaba cerrado.

Después de ejecutarla, correr `node Docs/CiudadDelSol/Interior/Etapa3_20261117/PrepararInforme.mjs` actualiza este dossier exclusivamente desde los resultados guardados.

## Etapa 4 — recomendación condicionada, sin construcción

Con la recepción individual de estas tres conexiones puede planificarse la siguiente auditoría.

La siguiente selección lógica para explorar el descenso restante es VS01 (A03→B01, −16→−24) y VS02 (B01→B02, −24→−30), con VS04/VS05 solo como alternativas documentales de los anillos. VS03 entra en D03 y requiere autorización específica por afectar el acceso a la cámara final; no se interviene ahora. La circulación simultánea de cuatro humanos y el último tramo sin telemetría manual de Etapa 1 siguen pendientes.

## Archivos

- Assets/ROM/Editor/CiudadDelSolInteriorStageTwo.cs: reutilización parametrizada del receptor; misma lógica de seguridad.
- Assets/ROM/Editor/CiudadDelSolInteriorStageThree.cs y .meta: selección, alternativa existente, menú y solicitud de Etapa 3.
- Docs/CiudadDelSol/Interior/Etapa3_20261117/: informe, resumen, integridad, ejes y plano por niveles.
- Backups/CiudadDelSol/Interior/Etapa3_20261117/: respaldo completo, compilación, solicitud y resultados nativos cuando estén disponibles.

Sin cambios de escena ni NavMesh, sin commits ni pushes.
