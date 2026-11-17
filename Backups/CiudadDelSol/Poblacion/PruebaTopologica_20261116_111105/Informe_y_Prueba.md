# Ciudad del Sol — ocho bloqueos, apoyo físico y prueba topológica

## Estado real de partida

Editor_Baseline.log conserva la ejecución real: 10 encuentros aprobados, 8 bloqueados, 16/29 puestos. No se ejecutó Unity ni Play durante esta intervención. El registro por sí solo no permite declarar rotas las conexiones 03–05 o 06–07.

| Grupo de causas | Encuentros | Evidencia |
|---|---|---|
| Acceso 03–05 | claustro-patio, claustro-patrulla, claustro-galeria | Arista de origen confirmada a distancia cero. Ambos raycasts se bloquean en sus respectivos orígenes entre (-0.699457,12.05,72.000530) y (-0.700001,12.05,72.100000). |
| Arco del Santuario | santuario-sur, santuario-elite, sol-este, sol-regente | Contacto de Entrance_Arch_01 en (137.586,4.055,-108.693), nav Y=4.171, pendiente 36,99°. Falla la búsqueda de pavimento centrada en el contacto inclinado. |
| Ascenso 06–07 | sol-sur | Ambos raycasts se bloquean al inicio entre (222.7,36.05,102.1) y (222.65,36.05,102.175), máscara exterior cero. Falta demostrar continuidad topológica. |

## Corrección demostrable del arco

La banda de 0,030 m estaba centrada en cada punto del arco. Al ascender su contacto a Y=4,055, la consulta empezaba en Y=4,085 y terminaba en Y=4,025, por lo que nunca podía encontrar el pavimento Y=4. Eso no es una ausencia demostrada de suelo: es una ventana de consulta situada por encima de él. El registro anterior ya acreditaba dicho pavimento en la misma aproximación; el generador sitúa el suelo modular del sector a esa cota.

Ahora el centro de referencia procede de la posición REAL del sector configurado en la escena. La banda de consulta sigue siendo 0,030 m alrededor del plano de pavimento, no sigue el contacto inclinado del arco. Se exige encontrar físicamente Platform_Base_01 bajo ese X/Z, en Suelo modular y dentro de 06-santuario, plano a 5° como máximo. No se inventa suelo ni se usa un corte global por altura.

El contacto del arco debe permanecer dentro del escalón ORIGINAL del agente respecto de ese pavimento (0,36 m). También se mantienen el cambio entre apoyos, diferencia NavMesh/suelo, pendiente máxima, cápsula y obstáculos. El arco sigue en las consultas de colisión. No se autoriza su parte alta ni otros monumentos.

Por tanto, se corrigió un defecto de consulta de procedencia; no se afirma que toda la ruta alrededor del arco ya sea segura. Si otra parte de la base excede los límites o la cápsula encuentra un pilar, seguirá bloqueada y deberá tratarse como una nueva causa.

## Claustro y Sol-sur: no se ignoran raycasts bloqueados

La condición de arista del Claustro ya se confirmó en Unity. La falta de un raycast recíproco libre impedía distinguir una ambigüedad de selección de polígono de un corte real. No se cambió la precisión de 1 mm ni se aceptó la distancia corta como evidencia.

Para el caso acotado de DOS impactos exactamente en sus respectivos orígenes se añadió una prueba nativa de solo lectura. Debe acreditar:

1. Ambos extremos sobre aristas reales dentro del milímetro existente.
2. Mapeo a polígonos de suelo válidos del MISMO tipo de agente y nivel, dentro de ese milímetro.
3. Recorrido nativo de ida y vuelta sobre superficie conectada, sin NavMeshLinks/OffMeshLinks.
4. Coincidencia con los polígonos destino/origen, o un portal nativo directo entre ellos. Dos polígonos desconectados con posiciones coincidentes NO bastan; un movimiento que se quede en un borde tampoco basta.
5. Además, los caminos locales bidireccionales existentes deben seguir siendo completos, rectos y sin desvío.
6. Todas las comprobaciones físicas posteriores deben aprobarse.

Se usa NavMeshQuery/MoveLocation para consultar valores de posición y polígonos; NO se mueve, instancia ni teletransporta ningún agente. No se modifica el mundo de navegación. Ante una consulta inválida/no disponible, un movimiento limitado, falta de portal, otro agente/planta, enlace o camino parcial, el grupo permanece bloqueado.

TOPOLOGIA_NATIVA_VERIFICADA solo se emite después de acreditar esa prueba Y aprobar toda la ruta física. Si no puede probarse, RUTA_CONTINUIDAD_NO_DEMOSTRADA informa el motivo y se adjuntan sondas físicas de origen, centro y destino con sus colliders, jerarquía, normal y diferencia de nivel. Esas sondas son diagnósticas: NO habilitan el encuentro.

La condición de doble impacto fuera de los orígenes sigue rechazándose directamente. Los caminos completos globales y sus sentidos, máscaras, límites físicos, recuperación y restricciones contra interiores/ruinas intermedias se mantienen.

## Qué está confirmado y qué continúa pendiente

- Confirmado por código y datos del log: la ventana del arco no alcanzaba el pavimento al seguir la rampa; fue corregida.
- Confirmado por log: Claustro presenta una arista real y dos impactos al inicio; NO está confirmada una discontinuidad física.
- Confirmado por log: Sol-sur presenta un borde exterior y doble impacto al inicio; NO está confirmada una rotura del mapa.
- La continuidad de esos dos puntos solo se considerará acreditada si las consultas al NavMesh REAL pasan en Play. Las pruebas simuladas no sustituyen ese resultado.

Si continúa fallando 03–05, inspeccionar el tramo de acceso Y=12 cerca de (-0.7,12.05,72.0), no atribuirlo directamente a los pisos Y=28/Y=32 del Claustro. Si continúa fallando 06–07, inspeccionar el descanso/giro cerca de (222.7,36.05,102.1). Si las sondas muestran suelo continuo pero la consulta nativa no acredita portal, la propuesta es revisar la conectividad y las fuentes del horneado de esa junta antes de autorizar cualquier cambio de datos. Si muestran una discontinuidad física, documentar el módulo y proponer una corrección específica de junta con autorización previa. No se añadió ningún enlace ni piso auxiliar y no se modificó geometría.

## Archivo y verificaciones

Único archivo del proyecto modificado:
Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolEncounterValidation.cs.

Respaldo previo de ese archivo, del log y de los SHA-256 en esta carpeta. Antes.json/Despues.json verifican 42 archivos: solo cambió el validador. Configuración de población, diez encuentros aprobados, escena, NavMeshData, colliders, arquitectura, HUD, prefabs, combate y recursos originales protegidos permanecen intactos. Continúan 29 puestos, 18 encuentros, 7 patrullas y cero animales.

Compilación con Roslyn incluido en Unity 6000.3.23f1: ejecución y Editor, salida 0, sin errores. Persisten las dos advertencias anteriores de CriliumRiftFall y RiftHub.Characters. Hay seis advertencias CS0618 nuevas porque Unity marca NavMeshQuery y sus tipos relacionados como obsoletos, sin reemplazo. La consulta queda aislada en un método del preflight; no se instalaron paquetes. Una futura actualización de Unity debe revisar esta dependencia. No se ocultaron las advertencias.

Pruebas ejecutadas:
- 71 comprobaciones del método REAL compilado con dobles de consulta: aprobadas. Incluyen rampa de arco de 0,055 m, contacto elevado fuera del escalón, ausencia de pavimento, colisión contra el propio arco, polígonos desconectados coincidentes, movimiento nativo limitado incluso a menos de 1 mm, retorno limitado, otro agente/planta, enlaces, consulta no disponible, caminos parciales/con rodeo, huecos y obstáculos.
- 138 comprobaciones estáticas de población: aprobadas. Se conservan íntegros los diez encuentros de combate aprobados y todos los bloques de puestos de combate, límites y tiempos sin fauna.
- 14 regresiones del planificador real de respawn, recompiladas de su fuente actual: aprobadas.

Las 71 pruebas son modelos de consulta, las 138 son verificaciones de datos y las 14 son lógica pura. Ninguna ejecuta PhysX, NavMeshData, Mirror o movimiento de agentes reales. Fuente y resultados están en RouteContractChecks.cs, RouteChecks.log, ConfigurationChecks.log y RespawnChecks.log, fuera de Assets. No se escribió ninguna DLL en Library.

Documentación consultada: AI Navigation 2.0.14, Documentation~/NavInnerWorkings.md; API de Unity 6000.3:
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/experimental/ai/navmeshquery/movelocation
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/experimental/ai/navmeshquery/maplocation
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Experimental.AI.NavMeshQuery.IsValid.html

MoveLocation documenta que conserva conexión continua sobre la superficie y no cruza enlaces. Además se añadieron comprobaciones explícitas de agente, tipo de polígono, portales y caminos locales. La disponibilidad de los miembros concretos se comprobó contra los ensamblados instalados mediante compilación.

## Una única prueba de Play Mode

1. Fuera de Play, esperar compilación/importación de Unity, limpiar Console y conservar el modo automático existente. No hornear ni guardar escena. Las advertencias CS0618 anteriores son conocidas; cualquier error de compilación debe detener la prueba.
2. Presionar Play UNA vez y esperar PREFLIGHT_TERMINADO. Debe seguir anunciando 18 encuentros y 29 puestos, sin animales. Los diez encuentros aprobados/16 puestos de referencia deben mantenerse sin regresión.
3. En el Santuario, la comprobación no debe fallar solo porque Y=4,055 esté a más de 3 cm del contacto anterior. Debe acreditar el pavimento en su cota real y pasar cápsula/pendiente/escalón. Buscar ARCO_APOYO_VERIFICADO o el nuevo bloqueo físico concreto.
4. En Claustro y Sol-sur, buscar TOPOLOGIA_NATIVA_VERIFICADA y el resumen ESQUINAS_VERIFICADAS. Si no aparecen y el grupo sigue bloqueado, guardar RUTA_CONTINUIDAD_NO_DEMOSTRADA completo, incluida la razón nativa y las tres sondas de suelo. No considerarlo automáticamente un corte físico ni aumentar tolerancias.
5. Guardar el log completo y salir de Play sin guardar objetos temporales. El resultado máximo sería 18/18 y 29/29 SOLO si todos los caminos y posiciones cumplen las pruebas reales. No se promete ese número: un obstáculo posterior o una conexión realmente no acreditada deben continuar bloqueados.

Sin commits, pushes, modificaciones de escena/NavMesh/prefabs ni reincorporación de animales. No se declara solucionado ningún encuentro sin el resultado real de esa prueba.
