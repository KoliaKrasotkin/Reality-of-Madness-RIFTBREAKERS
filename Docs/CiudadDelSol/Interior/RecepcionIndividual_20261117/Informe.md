# Ciudad del Sol — recepción técnica individual A03–R21

17/11/2026. Física y NavMesh reales de Unity 6000.3.23f1; captura manual original conservada.

## Dictamen y alcance

**El recorrido documentado tiene respaldo técnico de transitabilidad para un Warrior individual y no se ha demostrado necesidad de corregir su arquitectura.** Se combinan movimiento real ya registrado hasta el punto 7 y comprobaciones físicas/NavMesh bidireccionales del tramo 7→8. Esto permite continuar la planificación y recepción acotada de la Etapa 2, previa autorización de su alcance.

**No está completada la ida/vuelta manual de nueve puntos.** El último regreso no tiene muestras reales de movimiento. Tampoco se certifican cuatro jugadores, ancho uniforme de seis metros, todo AC01–AC04, otras alas o niveles profundos. Para cerrar formalmente esa captura sigue faltando solo (174,-16,99)→(156,-16,99), 18 m; no hace falta repetir A03–R21 completo.

## Evidencia adicional buscada

Se revisaron las carpetas de capturas de Backups/CiudadDelSol/Interior y las herramientas de recuperación. Sigue existiendo una sola captura WarriorWalk: WarriorWalk_20261117T184352415Z/Recorrido_Warriors.json, con 2181 muestras, personaje 3, conexión humana local 0, puntos 0–7 acreditados. Los informes derivados, pruebas estáticas y pruebas sintéticas de regresión no agregan movimiento real. La visualización del personaje en el Editor tampoco aporta una secuencia temporal del CharacterInstance para el último tramo, por lo que no se usa para completar el punto 8.

## Estado de cada tramo

| Tramo | Evidencia de desplazamiento real | Resultado técnico |
| --- | --- | --- |
| 0→1: (156,-16,99)→(174,-16,99) | Acreditado en captura original | Apoyo y NavMesh presentes; nueva comprobación bidireccional satisfactoria |
| 1→2: (174,-16,99)→(174,-16,117) | Acreditado | Comprobaciones nativas previas de guía y captura física sin fallos |
| 2→3: (174,-16,117)→(120,-16,117) | Acreditado | Tres avisos de arista investigados en Unity; origen explicado por baranda |
| 3→4: (120,-16,117)→(117,-16,123) | Acreditado, llegada R21 | Sin fallos de apoyo/NavMesh registrados |
| 4→5: (117,-16,123)→(120,-16,117) | Acreditado | Sin fallos de apoyo/NavMesh registrados |
| 5→6: (120,-16,117)→(174,-16,117) | Acreditado por puntos, con hueco histórico de 1,708 s | Física localizada del medio metro sin muestras satisfactoria; no se inventa movimiento durante el hueco |
| 6→7: (174,-16,117)→(174,-16,99) | Acreditado | Sin fallos de apoyo/NavMesh registrados |
| 7→8: (174,-16,99)→(156,-16,99) | NO acreditado por captura de movimiento | Física y NavMesh nativos satisfactorios en ambos sentidos |

## Prueba nueva ejecutada realmente en Unity

Se utilizó el Editor existente del proyecto (PID 13464 identificado), sin lanzar otra instancia ni instalar Pipeline. La CLI no encontraba un Editor conectado a Pipeline. La sesión abierta mantenía errores de HUD y avisos de dos AudioListener; el cierre normal de Play permitió la recarga. No se guardó la escena ni se reinició el Host. La primera recepción quedó registrada durante la transición, sin servidor activo ni propietario local; la recepción final es explícitamente Edit Mode.

No se movió el Warrior ni se añadió un dispositivo de entrada virtual, se modificó Input, se usó Transform/ServerTeleport o se cambió el bootstrap. No se simularon CharacterControllers. Las consultas fueron Raycast/OverlapCapsule/CapsuleCast de PhysX y SamplePosition/CalculatePath/Raycast/FindClosestEdge de NavMesh, sin la API experimental NavMeshQuery.

La ejecución final comenzó a 2026-11-17T19:30:41.0756147Z y terminó a 2026-11-17T19:30:49.2594885Z. Error de ejecución: ninguno. Estado: CONSULTAS_FISICAS_Y_NAVMESH_COMPLETAS; NO_ACREDITA_MOVIMIENTO_WARRIOR_FALTANTE.

Por cada sentido del último tramo:
- 73 posiciones físicas, separación máxima 0,25 m; huella de ocho rayos por cápsula y barridos entre muestras.
- Cápsula Warrior original: radio 0,32 m, altura 2 m, capa character y matriz de colisión reales. Suelo local con tolerancia de 0,03 m; pendientes compatibles con controlador/Humanoid 0.
- Pendiente máxima observada: 0°. Cero fallos del eje, obstáculos o huecos de huella.
- Suelo encontrado en ocho direcciones hasta 1,5 m alrededor de las posiciones; es una cota inferior muestreada de apoyo, no una medida continua de anchura libre de obstáculos.
- Humanoid 0 conserva radio 0,57 m y escalón 0,36 m; no se cambió vóxel ni dato alguno.
- CalculatePath completo y físicamente comprobado en ambas direcciones; raycasts NavMesh directo e inverso sin impacto.
- Distancia mínima del eje al límite NavMesh: 2.300 m.

El intervalo histórico sin muestras, aproximadamente (168.09131,-16,117.69969)→(168.57707,-16,117.71467), también pasó apoyo, cápsula, barridos, raycasts y caminos completos bidireccionales. Esto demuestra la condición física actual de ese corto paso; no reconstruye lo sucedido entre los timestamps originales.

## Los tres avisos de proximidad

| Tiempo original s | X/Z Warrior | Distancia al borde NavMesh m | Resultado nativo actual |
| --- | --- | --- | --- |
| 130.954 | 167.763 / 114.968 | 0.268 | Suelo, huella, cápsula y NavMesh correctos |
| 131.101 | 166.853 / 114.907 | 0.207 | Suelo, huella, cápsula y NavMesh correctos |
| 131.236 | 165.967 / 115.085 | 0.392 | Suelo, huella, cápsula y NavMesh correctos |

Las aristas se encuentran alrededor de Z=114,70, con normal hacia +Z. Los perfiles se midieron a ambos lados de cada arista (-1, -0,57, -0,32, 0, +0,32, +0,57, +1 m). **Todos los puntos de esos perfiles tienen apoyo físico**. Hacia la baranda, las posiciones -1 y -0,57 m chocan con Subsuelo_Acceso_Etapa01/04 - Protecciones del piso inferior/Railing_01; las posiciones originales y el resto del perfil permiten la cápsula. Se trata de un límite de navegación junto a un obstáculo sólido, no de una caída ni de falta de pavimento demostrada.

La distancia de FindClosestEdge es distancia al límite navegable ya erosionado para el agente; no es por sí sola distancia al vacío ni holgura física de la cápsula. Los avisos se conservan y se contextualizan, no se borran ni se reduce el radio del agente.

La primera versión de los perfiles usó Y del NavMesh (aprox. -15,95) como Y física, generando SIN_APOYO_LOCAL con tolerancia 0,03 aunque el suelo estaba en -16. Se corrigió únicamente la referencia vertical al suelo medido. La tolerancia sigue en 0,03. El resultado intermedio está conservado como Recepcion_ultimo_tramo_V1_perfil_alturaNav.json y no se utiliza para el dictamen físico final.

## Riesgos y restricciones que permanecen

- La banda exacta de seis metros sigue sin aprobarse: ocho muestras laterales por sentido rozan los extremos de dos Railing_01 aproximadamente 0,00022 m. El eje y la cápsula individual pasan. No se movieron barandas ni se abrieron bordes para forzar la anchura.
- El giro norte y las columnas monumentales mantienen las restricciones de ancho del dossier anterior. Esta recepción no impone seis metros útiles uniformes ni acredita combate en formación de cuatro.
- El modelo local no sustituye cuatro clientes: el Host de ensayo sigue configurado para un jugador y sin escucha remota. Se necesitan cuatro conexiones humanas reales, cruce en ambos sentidos, maniobra en bocas y revisión de cámara/colisiones/sincronización antes de certificar cooperación. No se cambió la configuración de red para conseguirlo.
- El Editor registró RiftCombatNumbers.LateUpdate con NullReferenceException y dos AudioListener antes del cierre de Play. No son un corte físico del recorrido. No se corrigieron HUD ni servicios compartidos dentro de esta intervención. La recepción final de geometría se ejecutó sin errores propios.

## Archivos y seguridad

- Extendida la herramienta existente: Assets/ROM/Editor/CiudadDelSolInteriorStageOne.cs ahora es partial; el filtro opcional excluye únicamente colliders propios del Warrior si el propietario existe. Con propietario nulo se conserva el comportamiento anterior. En la recepción final no había propietario y no fue necesario excluir actores.
- Nuevo Assets/ROM/Editor/CiudadDelSolInteriorStageOne.LastLeg.cs y .meta: comprobación específica y menú manual. Reutiliza Ground, Standing, Sweep, CheckRoute y NativePath existentes.
- Se utilizó una solicitud explícita de una sola ejecución para correr las sondas en el Editor abierto tras recargar. Ambas solicitudes quedaron consumidas; no queda solicitud pendiente ni un ensayo en Update. Sin solicitud, el callback del Editor no realiza consultas ni modifica el inicio del juego.
- El menú disponible es ROM → Dungeon Ciudad del Sol → Interior - Etapa 1 → Comprobar ultimo tramo y avisos (sin mover). No es necesario repetirlo para este dictamen.
- Respaldos y evidencia: Backups/CiudadDelSol/Interior/AutoLastLeg_20261117/.
- Escena, NavMeshData exterior, población y captura original conservan SHA-256. No hubo guardado de escena, horneado, commit ni push.
- Compilación C# Editor con referencias de Unity y compilación real del Editor satisfactorias. Las advertencias de Experimental NavMeshQuery pertenecen al diagnóstico preexistente; no se ejecutó esa herramienta.

## Decisión para continuar

**Recepción técnica de geometría y navegación para un jugador: satisfactoria en el recorrido norte definido. No se propone ninguna obra en A03–R21.** Puede avanzarse a la planificación/recepción limitada de Etapa 2, sin extrapolar esta aprobación al resto del interior ni a cuatro jugadores.

**Recepción funcional integral de ida/vuelta con captura del Warrior: parcial.** El punto 8 continúa pendiente. Si se exige cerrar esa acta con movimiento real, la comprobación mínima es solo el regreso de 18 m, usando Continuar solo desde el último punto acreditado y la captura original. No hace falta volver a R21. La recuperación puede enlazar ese tramo posterior de forma explícita, sin falsificar continuidad entre capturas.

Guías consultadas: using-unity, unity-cli/common-workflows y computer-use (guidance, API y confirmations). No se instaló ningún paquete.
