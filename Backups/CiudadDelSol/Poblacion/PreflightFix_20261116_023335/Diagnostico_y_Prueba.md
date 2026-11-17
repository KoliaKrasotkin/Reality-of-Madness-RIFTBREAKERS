# Ciudad del Sol — corrección del preflight exterior

## Evidencia de partida

Editor_Baseline.log conserva el registro real leído de C:/Users/juang/AppData/Local/Unity/Editor/Editor.log. FallosOriginales.txt extrae los 23 bloqueos y el resumen 0/23, 0/34. No se ejecutó Unity ni Play Mode en esta intervención.

- Tres grupos de Pórtico rechazados por la misma muestra de recuperación (-147,0,-103).
- Diecinueve grupos rechazados en el tramo común de accesibilidad desde la entrada. Ejemplos: (30.370,-1.628,-194.564), (26.421,-1.628,-172.281), (26.642,-1.639,-172.309). El Oráculo ya probado superó Placement y falló después al comprobar esa ruta común. No eran 19 fallos demostrados de los spawns locales.
- Santuario-sur rechazado localmente en (135,4,-108) por un mensaje que mezclaba ausencia de NavMesh, nivel, consulta de borde y margen. Ese registro no permite distinguir cuál fue la causa física.

## Causas y cambios

### 1. Altura interpolada de ruta tratada como suelo

Route calculaba un PathComplete, interpolaba linealmente sus esquinas y pasaba cada punto a Project. Este último exige altura física y NavMesh a distancias locales estrictas. Las esquinas de un corredor no codifican todos los cambios de altura del suelo entre sus extremos. Una recta interpolada puede elevarse sobre un tablero plano antes de una escalera, aunque la ruta de navegación completa sí exista. El validador anterior confundía esa recta con la superficie detallada. El patrón del log corresponde a esa comprobación y la ubicación común explica su propagación; confirmar la transitabilidad final sigue requiriendo Unity.

Ahora se conserva CalculatePath completo en ambos sentidos. Se recorre el XZ de cada segmento cada 0,10 m, tomando el apoyo físico desde la altura del apoyo anterior. La ventana vertical depende exclusivamente del escalón configurado del agente (0,36 m más 0,03 m de margen de origen de raycast); el cambio real entre apoyos no puede superar 0,36 m, salvo 0,001 m numérico. Se exige pendiente autorizada, procedencia exterior, proyección local a NavMesh, holgura de cuerpo y coincidencia con la cota final. No se hace una búsqueda vertical abierta ni se acepta el piso de abajo si falta suelo. Las tolerancias generales de Project y Placement no se ampliaron: consulta NavMesh 0,25 m, diferencia de cota 0,36 m.

Por tanto no se eliminó la accesibilidad global ni se aceptan PathPartial. Los errores ahora distinguen ORIGEN_ACCESIBILIDAD, ACCESIBILIDAD_ENTRADA, RUTA_SIN_APOYO, RUTA_APOYO_INCOMPATIBLE, cota física, NavMesh local y obstrucción, con coordenadas y collider cuando existe.

### 2. Referencia de recuperación junto a terraza

CiudadDelSolRemainingZonesGenerator.BuildPortico sitúa la terraza occidental sobre módulos de 6 m, con borde X=-147 y superficie elevada Y=4. La recuperación anterior (-145,0,-103) pedía además una muestra dos metros al oeste, exactamente en ese borde. El centro del área no demostraba que las otras cuatro muestras estuvieran en el mismo nivel.

Se cambió únicamente la referencia de portico-descanso a (-138,0,-103), conservando radio 6 m. Está fuera de la terraza según la construcción. El atlas aprobado confirma color de pavimento Y=0 para centro y cuatro muestras ±2 m: RGB(164,134,82). La antigua muestra (-147,-103) figura gris RGB(74,75,78). Este contraste es evidencia de autoría, no sustituto de las cinco consultas físicas, que continúan obligatorias en Play. No se movieron enemigos ni se alteró la terraza.

### 3. Santuario: falta información para una corrección física

No se modificó el puesto (135,4,-108), ningún collider ni los márgenes. Placement ahora informa por separado SPAWN_SIN_NAVMESH, SPAWN_OTRO_NIVEL, SPAWN_BORDE_NO_EVALUABLE y SPAWN_MARGEN_INSUFICIENTE. Este último muestra nav, borde, distancia medida y mínimo requerido (radio original + 0,25 m). También se detallan suelo, jerarquía, Layer, normal y pendiente. Si no cumple, seguirá bloqueado. Un píxel del atlas con navegación a Y=4 no demuestra que exista margen suficiente para ese prefab.

## Archivos modificados

- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolEncounterValidation.cs: apoyo secuencial en rutas, diagnóstico de fase y de recuperación.
- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolSpawnSafety.cs: separación de causas y valores reales; límites de seguridad sin cambios.
- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolPopulation.Encounters.cs: identifica el fallo compartido de recuperación, evitando presentarlo como fallo local de cada spawn.
- Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset: solo portico-descanso, X=-145 → -138.

## Verificaciones

Respaldo previo en esta carpeta de los cuatro archivos y registro original. Antes.json/Despues.json verifican que solo cambiaron estos cuatro archivos entre los vigilados; escena, NavMeshData, etapa 1, prefabs, definiciones originales, HUD, MainMenu, población Horde y agente de combate conservan sus hashes de partida.

Compilación Roslyn incluida en Unity 6000.3.23f1: ejecución y Editor, salida 0, sin errores. Solo advertencias previas CS0108 CriliumRiftFall y CS0414 RiftHub.Characters. Se reutilizó la referencia MultiFPS ya compilada porque no se cambió ese ensamblado. No se sobrescribieron DLL del proyecto ni se ejecutó el Editor. Unity CLI 1.0.0 informa que no hay un Editor conectado a Pipeline; no se instaló nada. La importación y el weaving de Mirror deben confirmarse al abrir Unity.

Documentación consultada: AI Navigation 2.0.14, Documentation~/NavInnerWorkings.md, especialmente separación entre corredor global y superficie/movimiento local. No se consultaron ni cambiaron generadores para hornear de nuevo.

## Una nueva prueba en Play Mode

1. Fuera de Play, esperar la compilación de Unity y mantener habilitado el modo automático existente. No hornear ni guardar la escena.
2. Limpiar la Console, presionar Play una sola vez y esperar CIUDAD_EXTERIOR PREFLIGHT_TERMINADO.
3. Confirmar Warrior y los actores previos; revisar grupos aprobados/bloqueados. No se exige artificialmente 34/34 para considerar útil la prueba: los rechazos físicos reales deben seguir visibles.
4. Si hay bloqueos, guardar el log completo. Distinguir RECUPERACION_SECTOR, ACCESIBILIDAD_ENTRADA y SPAWN_*. En Santuario interesa especialmente el código exacto y las distancias medidas.
5. Verificar visualmente el descanso del Pórtico, los encuentros habilitados y que no aparezcan enemigos en ruinas intermedias/interior. Probar una patrulla habilitada y el respawn de los dos actores previos. Salir de Play sin guardar objetos temporales.

Pendiente: validar las rutas y la nueva referencia física en Unity y medir el coste del preflight más fino (solo arranque, un grupo por tick). También pueden aparecer bloqueos reales adicionales que el primer fallo común estaba ocultando. No se afirma que los 34 puestos sean seguros o estén habilitados, ni que el Santuario esté resuelto.

Sin commits, pushes, escenas, prefabs, arquitectura, colliders ni NavMeshData modificados.
