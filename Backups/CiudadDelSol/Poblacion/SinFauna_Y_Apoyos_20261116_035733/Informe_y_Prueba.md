# Ciudad del Sol — sin fauna y correcciones acotadas de apoyo/aristas

## Alcance y conservación de aprobados

Se implementó la retirada explícita de animales y las dos correcciones autorizadas. No se inició Unity, Play ni Host y no se afirma que ningún encuentro pendiente haya quedado resuelto en la escena real.

Las condiciones «retirar todos los animales» y «conservar los 13 encuentros aprobados» son incompatibles literalmente: tres de esos 13 eran jabalí, ciervo y lobo. Se interpretó la retirada explícita como excepción, comunicada antes de editar. Se mantienen los DIEZ encuentros de combate aprobados, con sus DIECISÉIS puestos íntegros. Los tres encuentros animales aprobados y los dos animales bloqueados se retiran. No se corrigieron las rutas de Oso/Alce ni se alteraron sus prefabs; tampoco se intervino en Sol-sur ni en el ascenso oriental.

## Corrección específica del arco

En CiudadDelSolEncounterValidation.cs, SantuarioArchOnFloor solo puede acreditar el contacto bajo de Entrance_Arch_01 cuando:

- Es el collider exacto, directamente bajo Arquitectura monumental - fuera de los pasos y la raíz Dungeon_Ciudad_del_Sol_Generated.
- Una consulta física corta encuentra Platform_Base_01 bajo ese contacto, dentro de Suelo modular - superficie Y=... y del sector configurado 06-santuario.
- El pavimento está a no más de 0,03 m, usando la banda de consulta existente como exigencia de coplanaridad. No basta con el nombre del arco o estar cerca del Santuario.
- El pavimento es plano según la exigencia de 5°; una consulta saturada se rechaza.

Después se siguen comprobando la posición real del contacto, diferencia NavMesh/suelo, escalón, pendiente del arco, cuerpo y obstáculos. El arco NO se excluye de ninguna máscara ni consulta de cápsula. Un arco alto, otro monumento, un pavimento de otra planta/sector o un contacto sin suelo autorizado continúan rechazados. La raíz monumental completa NO pasa a considerarse jugable.

ARCO_APOYO_VERIFICADO se registra solo al aprobar TODA la ruta y sus comprobaciones físicas. No certifica movimiento ni combate.

## Corrección de aristas del Claustro

ConnectedStep conserva los casos anteriores de esquina. Para una muestra intermedia cuyo impacto coincide con el origen dentro de 0,001 m:

- FindClosestEdge debe confirmar una arista real: distancia <=0,001 m y coincidencia del punto de arista tanto con origen como con impacto dentro del MISMO milímetro.
- Solo entonces se ejecutan el raycast inverso libre y los caminos locales completos, rectos y bidireccionales ya existentes.
- Si falta la arista, está más lejos, el inverso se bloquea o el camino requiere un rodeo/queda parcial, se rechaza.
- Las comprobaciones físicas posteriores no se omiten y no se aumenta ninguna tolerancia.

ESQUINAS_VERIFICADAS ahora identifica esquinas/aristas. Los rechazos incluyen aristaOrigenVerificada y distanciaArista. No hay excepciones por coordenadas concretas ni cambios en el ascenso oriental. Los casos de ambos raycasts bloqueados continúan rechazados.

## Retirada de animales y nuevos totales

PopulationExterior.asset: eliminados jardines-jabali, jardines-oso, jardines-ciervo, jardines-alce y santuario-lobo. Se eliminó el bloque completo de cada puesto (incluidos respawn y cualquier campo de patrulla); ninguno tenía una patrulla de combate asignada. No se reemplazaron por otros enemigos.

Se retiraron las referencias de fauna de CompatibleEnemies tanto en PopulationExterior.asset como en PopulationStage1.asset. Las dos configuraciones conservan las seis definiciones nativas de combate. No existe una configuración interior de este tipo en Assets; no se creó ni se pobló el interior. CiudadDelSolPopulation.ValidateConfiguration ahora rechaza fauna en el catálogo de cualquier configuración de Ciudad del Sol, incluidas futuras configuraciones interiores, sin modificar los sistemas de El Rift/Horde.

| Sector | Puestos / límite | Encuentros | Patrullas |
|---|---:|---:|---:|
| Pórtico Solar | 4 / 4 | 3 | 1 |
| Jardines Dorados | 3 / 3 | 2 | 1 |
| Plaza del Eclipse | 5 / 5 | 3 | 1 |
| Archivos Solares | 4 / 4 | 2 | 1 |
| Claustro del Eclipse | 5 / 5 | 3 | 1 |
| Santuario Profano | 3 / 3 | 2 | 1 |
| El Sol Corrompido | 5 / 5 | 3 | 1 |
| TOTAL | 29 / 29 | 18 | 7 |

Límite global: 29. Composición conservada: nueve Diablillos, ocho Oráculos, cuatro Rompecadenas, cuatro Juramentados, tres Heraldos y un Regente. Incluye cuatro élites y un miniboss; no se cambiaron sus estadísticas o rangos.

Respawns conservados: Diablillo 90 s, Oráculo 120 s, otros normales 180 s, élites 240 s. Regente sin respawn automático. Recuperaciones, exclusiones de jugadores, reintentos, activación, escalado nativo y persecución no cambiaron.

PopulationStage1 mantiene sus dos puestos originales, tiempos y límite 2; únicamente se depuró su catálogo compatible. Todos los prefabs y definiciones animales originales siguen disponibles para otros modos, y su registro compartido en Mirror no se tocó.

## Archivos modificados

- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolEncounterValidation.cs: acreditación específica del contacto del arco y prueba de arista real.
- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolPopulation.cs: prohibición de fauna en el catálogo propio de Ciudad del Sol.
- Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset: retirada de cinco puestos, catálogo sin fauna, límites recalculados.
- Assets/ROM/Resources/CiudadDelSol/PopulationStage1.asset: catálogo sin fauna, dos puestos intactos.

Los archivos de pruebas e informes se crearon solo en esta carpeta de respaldo, fuera de Assets. No se crearon herramientas de Editor ni sistemas paralelos.

## Respaldos y verificaciones ejecutadas

- Copias previas de los cuatro archivos y del registro real Editor_Baseline.log.
- Antes_Unicos.json/Despues_Unicos.json: 42 archivos vigilados, solo los cuatro cambios previstos. Escena (incluidos sus modificadores), NavMeshData, HUD, MainMenu, población Horde, agente de combate y los trece pares de definiciones/prefabs nativos conservaron sus hashes. Antes.json/Despues.json originales se retienen también.
- Compilación Roslyn incluida en Unity 6000.3.23f1: ejecución y Editor, salida 0, sin errores. Advertencias preexistentes CS0108 CriliumRiftFall y CS0414 RiftHub.Characters. No se ejecutó el Editor ni se sobrescribieron DLL de Library.
- 42 comprobaciones del método REAL de validación compilado con dobles de consultas: aprobadas. Incluyen las regresiones anteriores y casos nuevos de arista real, arista demasiado lejos/ausente, inverso bloqueado, ruta local parcial/con rodeo, arco coplanar, ausencia de pavimento, contacto alto, monumento diferente, sector/interior incorrecto, consulta saturada, cápsula contra el propio arco y pendiente excesiva.
- 138 comprobaciones estáticas de configuración: aprobadas. Los 29 bloques de puestos de combate coinciden íntegramente con el respaldo, al igual que las siete patrullas. Se comprueban los diez encuentros de combate aprobados y la retirada exclusiva de los tres animales aprobados, además de los dos bloqueados.
- 14 regresiones del planificador real de respawn: aprobadas, compiladas de nuevo desde su fuente actual.
- Esas comprobaciones NO ejecutan PhysX, NavMeshData real, Mirror ni Play. La importación y el weaving del Editor siguen pendientes de su ejecución normal.
- Revisión de diferencias sin problemas de espacios; solo avisos normales de conversión LF/CRLF. No se hicieron staging, commits ni pushes.

Documentación consultada: AI Navigation 2.0.14, Documentation~/NavInnerWorkings.md. Se reutilizó la API FindClosestEdge ya usada en la seguridad de spawn del proyecto; no se instalaron paquetes.

## Una prueba integrada en Play Mode

1. Fuera de Play, esperar compilación/importación de Unity. No hornear ni guardar escenas; conservar habilitado el modo automático existente.
2. Limpiar Console, presionar Play una vez y esperar PREFLIGHT_TERMINADO. La configuración debe anunciar 18 encuentros y 29 puestos, no 23/34. No deben aparecer animales.
3. Comprobar que permanecen aprobados los diez encuentros de combate de referencia: entrada, portico-oeste, portico-patrulla, jardines-encuentro-01, jardines-norte, plaza-sur, plaza-patrulla, plaza-norte, archivos-lectura y archivos-norte.
4. Revisar los tres encuentros del Claustro: si se acredita la arista, todavía deben pasar raycast inverso, rutas locales y física. Si falla, guardar el mensaje completo con aristaOrigenVerificada y distanciaArista; no ampliar tolerancias.
5. Revisar santuario-sur, santuario-elite, sol-este y sol-regente: ARCO_APOYO_VERIFICADO solo puede aparecer después de acreditar pavimento y todas las comprobaciones de cuerpo/NavMesh. Un nuevo bloqueo de cápsula o soporte sigue siendo válido y debe investigarse.
6. Sol-sur/ascenso oriental continúa pendiente: no fue corregido y puede seguir bloqueado. Resolver el primer fallo de un grupo puede revelar otro posterior; no se promete 18/18 ni 29/29.
7. En grupos habilitados, observar las siete patrullas, probar combate y respawn sin animales, y salir de Play comprobando limpieza. No guardar objetos temporales.

Pendiente: resultados reales de esa ejecución, geometría del ascenso oriental, cualquier obstrucción posterior y balance/rendimiento para cuatro jugadores. No se declara resuelto ningún encuentro pendiente ni se avanzó a poblar el interior.
