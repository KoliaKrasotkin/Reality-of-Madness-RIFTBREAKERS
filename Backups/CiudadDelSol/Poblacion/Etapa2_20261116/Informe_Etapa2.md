# Ciudad del Sol — población exterior, etapa 2

## Alcance y estado

Implementación preparada: 34 puestos, 23 encuentros, siete patrullas cíclicas, cuatro élites y un miniboss. No se ejecutó Unity ni Play Mode. Las coordenadas se contrastaron con generadores y con el atlas exterior aprobado; esto NO demuestra apoyo, holgura ni rutas físicas en la escena actual. Cada encuentro debe superar el preflight de servidor antes de habilitar nuevas apariciones. Los grupos rechazados no se recolocan automáticamente.

La configuración persistente independiente es Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset. PopulationStage1.asset permanece intacto. ExteriorPlan.json contiene los puestos, coordenadas y rutas exactas; AtlasChecks.json contiene las referencias preliminares de altura. BuildExteriorAsset.ps1 documenta la generación del asset, sin ejecutar Unity.

## Distribución configurada

| Sector | Puestos / límite | Composición |
|---|---:|---|
| Pórtico Solar | 4 / 4 | Tres Diablillos y un Oráculo; incluye el Diablillo original de la entrada |
| Jardines Dorados | 7 / 7 | Dos Diablillos, Oráculo original, oso, jabalí, ciervo y alce |
| Plaza del Eclipse | 5 / 5 | Dos Diablillos, dos Rompecadenas (uno élite) y un Oráculo |
| Archivos Solares | 4 / 4 | Dos Oráculos, un Juramentado y un Diablillo |
| Claustro del Eclipse | 5 / 5 | Dos Juramentados, Heraldo, Rompecadenas élite y Oráculo; incluye galería real Y=32 |
| Santuario Profano | 4 / 4 | Heraldo, Oráculo, Rompecadenas élite y lobo |
| El Sol Corrompido | 5 / 5 | Heraldo, Oráculo, Juramentado élite, Diablillo y Regente miniboss |

Total por definición: nueve Diablillos, ocho Oráculos, cuatro Rompecadenas, cuatro Juramentados, tres Heraldos, un Regente y cinco animales. Son once tipos nativos. Zorro y liebre quedan fuera: sus cápsulas serializadas requieren una revisión física específica; no se modificaron sus prefabs.

Todos mantienen nivel base 1 para la primera revisión; la progresión inicial depende de composición y rango. Élites: vida ×2 y daño ×1,15. Regente: vida ×1,5 y daño ×1,1. No se añadieron habilidades ni escalado paralelo: continúa el escalado nativo para humanos presentes. Balance para cuatro jugadores pendiente.

## Patrullas

Una patrulla por sector, siete en total. Coordenadas completas en ExteriorPlan.json y el asset:

- Pórtico: Diablillo, (-120,0,-108) ↔ (-120,0,-96).
- Jardines: Diablillo, rectángulo X=-93/-86, Z=5/11, Y=4.
- Plaza: Diablillo, (44,12,-12), (52,12,-12), (52,12,-3).
- Archivos: Juramentado, (-105,20,112) ↔ (-105,20,102).
- Claustro: Juramentado, (31,28,112) ↔ (43,28,112).
- Santuario: Rompecadenas élite, (174,4,-56) ↔ (168,4,-56).
- Sol: Juramentado élite, (207,36,146) ↔ (214,36,146).

Espera 1,5 s por punto. El controlador solo solicita destinos al agente nativo cuando está inactivo. Combate, ataques, habilidades, stagger y retorno original tienen prioridad. Al terminar combate solicita retorno original al origen antes de reanudar. Las rutas fallidas detienen al agente y reintentan con frecuencia limitada; no hay warp, teletransporte ni NavMeshLinks. La fauna conserva su IA original.

## Respawn y presupuestos

- Diablillo: 90 s; Oráculo: 120 s, conservados.
- Otros normales y fauna: 180 s; élites: 240 s.
- Regente: sin respawn automático durante la sesión, aunque la definición original ya lo trate como jefe.
- Límite global 36; límites sectoriales iguales a los 34 puestos previstos. No se eliminan enemigos vivos por distancia para liberar presupuesto.
- Activación a 110 m; los dos actores originales conservan 150 m. Exclusión de aparición a 25 m de humanos y 40 m si están en combate; memoria de combate de 10 s. Reintento diferido 5 s.
- Las primeras apariciones se autorizan por grupo completo. El respawn se difiere mientras un integrante del mismo encuentro conserve objetivo, ataque o habilidad activa, aunque haya perseguido al jugador a otro sector.
- Los cadáveres siguen ocupando presupuesto y bloquean su puesto hasta su retirada nativa. También se comprueba despeje real justo antes de aparecer.
- Ocho áreas de recuperación configuradas (entrada y una por sector). Se comprueba suelo y despeje del Warrior; una recuperación inválida bloquea los nuevos grupos del sector y genera diagnóstico.

## Validación incorporada

Un grupo por tick de servidor, recorriendo los siete sectores. Antes de nuevas apariciones: apoyo físico, cota, cápsula, borde, NavMesh del agente, separación de puestos y ruta completa de ida y vuelta desde la entrada. Las rutas se muestrean cada 0,5 m con suelo exterior reconocido y holgura de torso/cabeza. Se verifican todos los puntos y tramos cíclicos de patrulla en ambos sentidos, sin cruzar recuperaciones. No se hornea ni modifica navegación.

El filtro de procedencia solo permite siete sectores, nueve conexiones y la entrada. No se configuran interiores ni ruinas intermedias. Se conserva el filtro original de objetivos/impactos por planta. Su efectividad real y posibles casos de proyectiles/AoE siguen requiriendo Play Mode.

GRUPO_BLOQUEADO indica sector, encuentro, puesto/coordenada y causa; no fuerza el spawn. Los dos actores de etapa 1 ya adoptados se conservan incluso si el preflight de su grupo falla, pero el grupo no genera nuevas apariciones hasta corregir su causa. PREFLIGHT_TERMINADO informa aprobados, rechazados y puestos habilitados. No es una certificación funcional.

## Archivos

Modificados:
- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolPopulationDefinition.cs
- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolPopulation.cs
- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolSpawnSafety.cs
- Assets/ROM/Editor/CiudadDelSolDevelopmentPopulation.cs
- Assets/ROM/Editor/CiudadDelSolDevelopmentSetup.cs
- Assets/MultiFPS/Scripts/Gameplay/RiftEnemies/RiftEnemyAgent.cs

Nuevos, cada uno con su .meta:
- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolPopulation.Encounters.cs
- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolEncounterValidation.cs
- Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolPatrol.cs
- Assets/MultiFPS/Scripts/Gameplay/RiftEnemies/RiftEnemyAgent.Patrol.cs
- Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset

La única extensión del agente compartido es una solicitud opcional de ruta inactiva, nula por defecto y restablecida al configurar/reutilizar/desactivar la instancia. Las nuevas solicitudes exigen la política CityRaid. No se cambió la lógica de ataques, habilidades, detección ni red de otros modos.

## Comprobaciones realizadas

- Compilación final con el Roslyn incluido en Unity 6000.3.23f1: MultiFPS, Assembly-CSharp y Editor, salida 0; sin errores. Advertencias preexistentes en WarriorProgressionDefinition, CriliumRiftFall y RiftHub.Characters. Logs y response files en esta carpeta. Esta comprobación no ejecuta Unity ni sustituye la importación y el weaving de Mirror del Editor.
- 364 comprobaciones estáticas de configuración aprobadas; no son pruebas físicas.
- 14 regresiones puras del planificador de respawn aprobadas; no simulan Mirror, movimiento ni combate.
- diff --check limitado a los archivos de esta intervención: sin problemas. El árbol general contiene cambios anteriores ajenos; no se restauraron ni prepararon en staging.
- SHA-256 de 39 archivos antes/después: solo cambiaron los seis scripts previstos. Escena, NavMesh, definición de etapa 1, MainMenu, población Horde, HUD y los 13 pares de definiciones/prefabs originales permanecen idénticos. Antes.json/Despues.json permiten verificarlo.
- Consultadas las guías locales de AI Navigation: NavAgentPatrol.md y MixingComponents.md (paquete 2.0.14). No se instalaron paquetes.
- No se ejecutó Unity: el CLI no ofrece un Editor conectado a Pipeline. No se inició Host ni Play.

## Una prueba integrada en Unity

1. Abrir Dungeon_Isla_del_Sol, dejar habilitado el modo automático existente y esperar la compilación/importación. No guardar objetos del ensayo.
2. Presionar Play. Confirmar Warrior, HUD y minimapa. Esperar CIUDAD_EXTERIOR PREFLIGHT_TERMINADO; objetivo de revisión: 23 grupos aprobados, 0 bloqueados y 34 puestos habilitados. Esto aún no equivale a 34 actores visibles: la activación y exclusión por proximidad siguen operativas.
3. Si hay GRUPO_BLOQUEADO, guardar el log completo y verificar esa ubicación; no mover automáticamente el spawn ni relajar comprobaciones.
4. Recorrer los siete sectores. Comprobar encuentros, áreas libres, fauna nativa, cada patrulla y combate. Atraer un patrullero, comprobar persecución conectada más allá de 36/120 m, perder el objetivo de manera legítima y observar retorno/reanudación sin teleport.
5. Matar normales y élites, apartarse más de las distancias de seguridad y medir 90/120/180/240 s. Permanecer cerca o combatir para confirmar aplazamiento. Regente no debe reaparecer en la misma sesión. No debe acumular cadáveres ni duplicados.
6. Probar jugadores/objetivos en plantas distintas para revisar ausencia de impactos indebidos. Salir y volver a Play: sin duplicados, sesión nueva limpia.

Pendiente: la prueba física/funcional anterior, balance y rendimiento en los siete sectores, movimiento de animales y tamaños mayores, y prueba multijugador real con cuatro humanos. El Host de desarrollo sigue siendo el ensayo local de un jugador; no se modificó su límite ni escucha remota. No se avanzó al interior.

## Reversibilidad

El respaldo de los seis scripts y la configuración Stage1 intacta permiten volver a la etapa 1 sin tocar escena/NavMesh/prefabs. Para una reversión parcial, restaurar únicamente esos scripts desde este respaldo y seleccionar la configuración original; no usar reset/clean global. No se hicieron commits ni pushes.
