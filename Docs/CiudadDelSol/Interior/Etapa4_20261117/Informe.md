# Ciudad del Sol — Etapa 4: VS01 y VS02

**Recepción técnica individual: 0/2. NO completa.** Cuatro jugadores reales: pendiente. No se construyó ni guardó arquitectura para forzar una aprobación.

## Alcance, fuentes y extremos

Se contrastaron Plan_20261117, Auditoria_20261117, Etapa1_20261117, RecepcionIndividual_20261117 y las recepciones completas de Etapa2_20261117 y Etapa3_20261117. También se leyeron UpperRoute del generador NucleoConjunto y Build/Flight/Ring del generador SolInvertido, sin ejecutarlos.

| Conexión | Espacios y extremos de prueba | Cotas | Elementos existentes | Estado |
|---|---|---|---|---|
| VS01 | A03 (156,−16,99) ↔ B01 (45,−24,15) | −16 → −24 | Acceso superior, galerías G04/G03, descanso y cuatro escaleras 2/6 | Pendiente de protección lateral |
| VS02 | B01 (45,−24,15) ↔ B02 (24,−30,45) | −24 → −30 | Anillo superior, descanso (45,−24,45) y tres escaleras 2/6 | Pendiente |

B01 es un anillo de seis metros, envolvente 72×72 m centrada en X/Z=(18,18), a Y=−24, con vacío central de 60×60 m. B02 es un anillo de seis metros, envolvente 60×60 m al mismo centro, a Y=−30, con vacío 48×48 m y apertura de escalera. Los puntos de conexión no son el centro de esos anillos: el centro no es un piso transitable. El plano representa los vacíos y separa los niveles.

## Pruebas realmente ejecutadas

Unity 6000.3.23f1, batch/nographics, PhysX y NavMesh nativos en Edit Mode. Recepción principal: 2026-11-18T02:21:20.0229728Z → 2026-11-18T02:21:22.7063898Z. La instalación se inició solo tras comprobar fuera del sandbox que no existía un Editor de este proyecto. No hubo Play, Host, movimiento del Warrior ni simulación de cuatro personajes.

Se utilizó el mismo motor de CiudadDelSolInteriorStageTwo: SamplePosition local con control de planta, CalculatePath en ambos sentidos, raycasts recíprocos/locales, apoyo y huella, cápsula del prefab Warrior, barridos, pendientes/escalones, anchura y bordes. No se aumentaron tolerancias ni se modificó el motor recibido. Se conservan radio Warrior 0,32 m, altura 2 m, escalón 0,30 m y pendiente 48°; Humanoid 0 conserva radio 0,57 m y escalón 0,36 m.

### VS01 — pendiente

Recorrido evaluado: (156.000, -16.000, 99.000) → (174.000, -16.000, 99.000) → (174.000, -16.000, 117.000) → (120.000, -16.000, 117.000) → (120.000, -16.000, 105.000) → (120.000, -16.000, 12.000) → (63.000, -16.000, 12.000) → (63.000, -16.000, -15.000) → (57.000, -16.000, -15.000) → (45.000, -16.000, -15.000) → (45.000, -16.000, -12.000) → (45.000, -18.000, -6.000) → (45.000, -20.000, 0.000) → (45.000, -22.000, 6.000) → (45.000, -24.000, 12.000) → (45.000, -24.000, 15.000).
Guía: 1331 muestras; apoyo/cápsula/huella fallidos: 0; barridos fallidos: 0; sin NavMesh: 0. Pendiente máxima de apoyo: 18.92°. Anchura útil inferior muestreada: 4.94 m; no es certificación de ancho uniforme.
Ida: PathComplete, física=true, impactos sin resolver=0. Regreso: PathComplete, física=true, impactos sin resolver=0.
Cuatro cápsulas estáticas despejadas: true. Protección de bordes: false. No equivale a circulación simultánea real ni evitación dinámica.

### VS02 — pendiente

Recorrido evaluado: (45.000, -24.000, 15.000) → (51.000, -24.000, 15.000) → (51.000, -24.000, 45.000) → (42.000, -24.000, 45.000) → (36.000, -26.000, 45.000) → (30.000, -28.000, 45.000) → (24.000, -30.000, 45.000).
Guía: 264 muestras; apoyo/cápsula/huella fallidos: 0; barridos fallidos: 0; sin NavMesh: 0. Pendiente máxima de apoyo: 18.92°. Anchura útil inferior muestreada: 4.74 m; no es certificación de ancho uniforme.
Ida: PathComplete, física=true, impactos sin resolver=0. Regreso: PathComplete, física=true, impactos sin resolver=0.
Cuatro cápsulas estáticas despejadas: true. Protección de bordes: false. No equivale a circulación simultánea real ni evitación dinámica.

## Obstáculos y causas

El eje documental de VS01 cruza la pilastra de Nave 102/102, recurso Nave_08_2.asset, cerca de (132,−16,105). Unity confirmó siete muestras de cápsula obstruida y un barrido bloqueado. No se eliminó ni recortó esa estructura. El rodeo norte existente de A03 evita la pilastra y conserva el resto de la continuación documentada.

En el rodeo, el eje completo y las rutas nativas son físicamente continuos, pero 29 secciones de anchura señalan borde sin protección en las cuatro escaleras centradas en X=45, de Z=−12 a Z=12 y Y=−16 a Y=−24. Los colliders Staircase_01 activos tienen seis metros de ancho; no figuran Railing_Staircase_01 laterales en ese vuelo. Flight del generador crea escaleras y recorridos, pero no esas barandas.

La auditoría lateral adicional ejecutó 14 sondas nativas en los SIETE módulos: X=41,5 y X=48,5 para VS01; Z=41,5 y Z=48,5 para VS02. 10 sondas encontraron una caída mayor que el escalón permitido sin barrera horizontal ni bloqueo de cápsula. Ejecución: 2026-11-18T03:10:44.8413932Z → 2026-11-18T03:10:44.8579806Z. Esto acredita un riesgo real, no solo una imprecisión de la huella.

| Conexión / escalera | Lado | Suelo bajo el exterior | Desnivel medido | Barrera | Barrido de cápsula bloqueado |
|---|---|---|---:|---|---|
| VS01 / 1 | Oeste | (41.500, -30.000, -9.000) | 13.119 m | false | false |
| VS01 / 1 | Este | (48.500, -24.000, -9.000) | 7.119 m | false | false |
| VS01 / 2 | Oeste | (41.500, -38.000, -3.000) | 19.119 m | false | false |
| VS01 / 2 | Este | (48.500, -24.000, -3.000) | 5.119 m | false | false |
| VS01 / 3 | Oeste | (41.500, -38.000, 3.000) | 17.119 m | false | false |
| VS01 / 3 | Este | (48.500, -24.000, 3.000) | 3.119 m | false | false |
| VS01 / 4 | Oeste | (41.500, -38.000, 9.000) | 15.119 m | false | false |
| VS01 / 4 | Este | (48.500, -24.000, 9.000) | 1.119 m | false | false |
| VS02 / 1 | Sur | (39.000, -38.000, 41.500) | 13.119 m | true | true |
| VS02 / 1 | Norte | (39.000, -38.000, 48.500) | 13.119 m | true | true |
| VS02 / 2 | Sur | (33.000, -38.000, 41.500) | 11.119 m | false | true |
| VS02 / 2 | Norte | (33.000, -38.000, 48.500) | 11.119 m | false | true |
| VS02 / 3 | Sur | (27.000, -38.000, 41.500) | 9.119 m | false | false |
| VS02 / 3 | Norte | (27.000, -38.000, 48.500) | 9.119 m | false | false |

**VS02: aprobación provisional retirada.** El motor heredado aprobó el camino central y el muestreo limitado de anchura. La auditoría lateral reveló una caída de aproximadamente 9,119 m sin barrera en ambos lados del tercer módulo (30,−28,45) → (24,−30,45). EjesOriginales conserva el resultado inicial; la recepción consolidada registra el riesgo, sin ocultar este falso negativo de la comprobación anterior.

## Correcciones y propuesta acotada

**Corrección de trazado preparada y comprobada:** usar el rodeo norte existente en VS01, preservando la pilastra. **Obras efectivamente realizadas: ninguna.** No se acepta la conexión mientras falte acreditar su protección lateral.

Propuesta acotada: ocho Railing_Staircase_01 en ambos flancos de VS01, adaptadas a 2/6 como en Centro Oriental. Ámbito X≈42…48, Z≈−12…12, Y≈−24…−16. Ejes ensayados X≈42,45 y X≈47,55. En VS02, proteger el tercer módulo X=30→24, Z≈42…48, Y=−28→−30 y sus juntas. Las sondas de los dos primeros módulos encuentran bloqueo de la arquitectura existente; no se proponen demoliciones ni protecciones redundantes sin comprobarlas.

Se ensayaron 8 barandas TEMPORALES de VS01 con los colliders del prefab original. No se guardaron y se destruyeron al terminar. 8/8 muestras con NavMesh local y cápsula Humanoid 0 originalmente despejada resultaron obstruidas por esas barandas. La escena conservó su SHA-256. Son pruebas reales de una propuesta hipotética en memoria, no arquitectura existente ni una recepción aprobada.


La colocación estándar ensayada NO puede guardarse como solución final manteniendo sin más la huella navegable actual. Hay que resolver una ubicación/espesor y apoyo constructivo compatibles con esa huella, o autorizar un ajuste local de navegación que conserve el exterior. El dato manual actual es compartido y está protegido: no se reemplaza ni rehace. Tampoco se añaden superficies flotantes, colliders invisibles, enlaces o navegación paralela. Se conservan todos los peldaños, descansos, apoyos y arquitectura monumental.

Los recorridos comprobados, incluido el camino global, siguen usando esos vuelos. No se ha acreditado otro descenso seguro dentro del alcance VS01–VS02. La siguiente escalera del anillo conduce a VS03/D03 y queda fuera de esta etapa: no se usa la cámara final como rodeo no autorizado.

## Regresiones e integridad

- Etapa 1: 1/1 en consultas nativas nuevas; error=ninguno; escena intacta=true.
- Etapa 2: 6/6 en consultas nativas nuevas; error=ninguno; escena intacta=true.
- Etapa 3: 3/3 en consultas nativas nuevas; error=ninguno; escena intacta=true.
- Los resultados nuevos se guardaron dentro de Etapa4_20261117; no se sobrescribieron los dossiers ni recepciones originales.
- Los 16 archivos protegidos conservan sus SHA-256, incluidos escena, NavMeshData, población y receptores previos.
- Compilación offline de Editor y compilación real de Unity superadas. Advertencias de API experimental proceden del diagnóstico anterior y no se ejecuta NavMeshQuery experimental.
- Se mantiene pendiente la telemetría manual del último regreso de Etapa 1. Estas consultas no completan esa captura.

## Archivos y próximo paso

Complemento nuevo: Assets/ROM/Editor/CiudadDelSolInteriorStageFour.Guards.cs y su .meta. Ensaya barandas únicamente en memoria y solo en batch con la opción explícita; no guarda geometría ni modifica prefabs.

Nuevo código: Assets/ROM/Editor/CiudadDelSolInteriorStageFour.cs y su .meta; selección de rutas y diagnóstico suplementario dentro del receptor parcial existente. No se modificaron scripts compartidos de gameplay ni el motor de validación de Etapa 2.

Documentación: esta carpeta contiene informe, resumen, evidencia Unity, integridad y plano por niveles. Respaldos, compilación y registros completos: Backups/CiudadDelSol/Interior/Etapa4_20261117/.

La Etapa 4 NO se declara completa. Los caminos centrales tienen respaldo físico/NavMesh, pero VS01 y VS02 requieren resolver las protecciones laterales y su compatibilidad con navegación. Las pruebas iniciales se conservan; no hace falta repetir recorridos humanos completos.

La próxima zona documentada sería VS03 (B02→D03, −30→−38), pero implica el acceso a la cámara final y necesita autorización específica. VS04/VS05 pueden evaluarse como ramales de exploración en una etapa separada. No se construyen ahora. Cuatro jugadores reales siguen pendientes. Sin commits ni pushes.
