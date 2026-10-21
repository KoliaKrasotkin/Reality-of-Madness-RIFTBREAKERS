# Bosque Fracturado — primera etapa

## Ciudad del Sol — Sun Temple en URP

Sexta isla, centrada en (820, 8, 370), con 560 m de diámetro. Conserva la composición
de la DemoScene de Sun Temple: ciudad escalonada, templo, calles, patios, detalles
y LODGroups. Un puente desde la región oriental lleva a una plaza; un paseo
elevado conecta la plaza con el patio alto. Se despejaron piezas que interferían
con ese acceso y se dejaron abiertas las puertas de la demo.

Se repararon 70 materiales originales, con respaldo en
`SunTempleURP/OriginalMaterials`. Se recuperaron texturas base, normales,
transparencias y partículas; se desactivaron los efectos de imagen del renderizador
antiguo. La propia DemoScene se guarda corregida, además de la zona del mapa.

El TerrainData original no se podía cargar: su archivo era 387 bytes más corto
que la longitud declarada. Se reconstruyó una altura de 2049 × 2049 muestras,
recuperando aproximadamente los bytes faltantes por continuidad espacial y
corrigiendo cuatro muestras fuera de rango. Se conserva el archivo dañado sin
sobrescribirlo. El nuevo `SunTempleURP/RecoveredTerrain.asset` conserva el relieve
aproximado y usa una capa de suelo compatible; no conserva exactamente la pintura
del terreno anterior. El informe está en `Documentation/sun-terrain-recovery.txt`.

La zona nueva usa una malla recortada de ese relieve con colisión. La navegación
verifica el puente, el acceso al patio alto y los destinos anteriores. Las calles
bajas y algunos accesos interiores todavía no tienen una ruta completa de IA;
ver `Documentation/suntemple-navigation.txt`. Falta probar el recorrido con jugadores
y medir el rendimiento de esta zona más densa en una partida real.

Para encontrar las zonas en Unity:

- `ROM > Horda > Encontrar Ciudad del Sol`.
- `ROM > Horda > Encontrar distrito urbano`.

Ambos comandos abren `map_rom_horde.unity`, seleccionan el grupo y lo enfocan.
La copia previa del mapa es `map_rom_horde_before_suntemple.unity`; la de la demo
está en `SunTempleURP/DemoScene_BeforeURP.unity`. El comando de construcción de esta
pasada es `ROM > Horda > Convertir Sun Temple URP y agregar ciudad`.

## Distrito urbano, cascadas y caída al vacío

La quinta isla está al sur, centrada en (100, 8, -470), con 360 m de diámetro y
un puente hacia el núcleo. Combina edificios comerciales, garaje, torres y
utilería de Post Apocalyptic Town con estructuras dañadas y escombros de
Modular Destroyed Buildings. Es una primera pasada urbana, sin objetivos nuevos.

Los dos paquetes URP locales se importaron bajo `Assets/HIVEMIND`. Sus GUIDs
se aislaron de forma determinista porque dos prefabs compartían GUIDs con el
templo egipcio existente. Las referencias internas de los assets nuevos se
actualizaron juntas; los assets del templo no se modificaron. Los materiales
compatibles se reutilizan y las conversiones de Shader Graph se guardan aparte.

Hay diez cascadas decorativas con textura animada, cauce y partículas de bruma.
No tienen colisión. Las mallas de roca sí tienen MeshCollider estático y bloquean
personajes y raycasts del juego. Se retiraron las rocas que tapaban puentes.

La DeathZone cubre 2300 × 2300 metros bajo el destino. Su trigger ocupa de
Y=-78 a Y=-38 y tiene Rigidbody cinemático. `HordeVoidKillPlane` comprueba además
la altura de los Health registrados, solo en servidor, cada 0,1 s. Por debajo de
Y=-38 aplica daño de caída igual a la vida actual, conservando el flujo de muerte
del kit. El DeathZone del kit ahora reconoce colliders hijos y mata personajes
con más de 9999 puntos de vida.

Validación: compilación, rutas hacia las regiones y cinco puntos urbanos,
raycasts contra 365 rocas y cobertura del trigger. Una prueba aislada en Play Mode
con Mirror, Health y un Rigidbody confirmó que se conserva la vida sobre el
límite y que una caída dispara una sola muerte incluso con 20000 HP. Resultado
en `Documentation/void-fall-smoke-test.txt`. No sustituye una sesión completa con
jugadores remotos ni una medición de rendimiento.

Menú de reconstrucción: `ROM > Horda > Aplicar cascadas, colisiones y vacio`.
Conserva la escena previa en `map_rom_horde_before_waterfalls.unity`. Ejecutar esta
pasada después de regenerar cualquiera de las etapas anteriores para actualizar
la ciudad, los colliders, el agua y toda la navegación. Las capturas urbanas están
en `Documentation/urban-island-overview.png` y `urban-island-ground.png`.

## Escala de destino: versión ampliada

El mapa se plantea como el único destino actual del juego, con el núcleo de Horda
en la isla central y tres regiones exteriores de 310, 340 y 370 metros de diámetro.
La extensión total aproximada es de 1 km de oeste a este y 800 m de sur a norte.
Estas cifras describen la composición; no implican que toda su superficie tenga
contenido jugable terminado.

Cada región contiene tres claros separados, senderos, coberturas y vegetación.
Las ruinas occidentales, la región de cristales oriental y el bosque septentrional
tienen cinco conexiones: tres desde el núcleo y dos cruces laterales que forman
circuitos. Se validan 14 destinos nuevos y las cinco rutas originales en NavMesh.

La versión de islas pequeñas está en `map_rom_horde_before_destination.unity`.
Las capturas de diseño se encuentran en `Documentation/forest-destination-overview.png`
y `Documentation/forest-destination-ground.png`; la vista aérea reduce la niebla
solo para la captura, sin modificar la atmósfera guardada en la escena.

Esta es una base espacial para un destino compartido: faltan encuentros por región,
objetivos, recompensas, progresión, pruebas multijugador y mediciones de rendimiento.
El terreno exterior sigue siendo una primera composición plana con acantilados y
afloramientos; el relieve y las transiciones artísticas requieren otra pasada.

## Etapa 2: islas conectadas

En la versión inicial de esta etapa había tres islas de 84 metros de diámetro, al oeste,
este y norte, conectadas a la isla principal con puentes de ocho metros de ancho.
Cada acceso tiene una rampa y un tramo elevado con barandas y colisión continua.
El conjunto ocupa aproximadamente 470 × 440 metros, sin contar rocas decorativas.

Los objetos están bajo `Bosque Fracturado - Islas y puentes` y el comando
`ROM > Horda > Agregar islas y puentes (etapa 2)` reconstruye solo esa ampliación.
También despeja vegetación en las entradas y amplía la zona de muerte bajo el mapa.
La copia previa queda en `map_rom_horde_before_islands.unity`.
Se comprueba una ruta completa desde la arena hasta el centro de cada isla.
La navegación se guarda en `FracturedForest/NavMesh-Islands.asset`.

Las islas son sectores explorables; no incorporan nuevos spawns, objetivos ni
recompensas. Falta comprobar su recorrido con jugadores y enemigos en Play Mode.
Si se regenera la etapa 1, ejecutar después la etapa 2 para actualizar los accesos
y volver a hornear toda la navegación.

Escena: `Assets/ROM/Levels/map_rom_horde.unity`.

Primera pasada de composición a partir de la referencia: isla de aproximadamente
230 × 300 metros, arena de Horda conservada en el centro, bosque periférico,
camino principal norte-sur, ruinas al oeste, monumento dimensional al este y
restos de un campamento al sudeste. Los fragmentos distantes amplían la silueta.

Se reutilizan assets locales de Eternal Temple, Sun Temple y The Grand Egyptian
Temple. No se descargan paquetes externos. Las conversiones de materiales a URP
se guardan en una carpeta propia; los materiales originales no se modifican.

## Edición

La geometría queda guardada como objetos normales de escena bajo
`Bosque Fracturado - Etapa 1`; no se genera en tiempo de ejecución.
Los prefabs conservan la conexión con sus assets de origen.

`ROM > Horda > Aplicar Bosque Fracturado (etapa 1)` reconstruye esta pasada y
reemplaza ese grupo: mover objetos dentro del grupo y volver a generar pierde
esos ajustes. Guardar cambios manuales fuera del grupo o editar el generador.
El comando original `Construir mapa Horda` reconstruye la escena base completa
y por tanto elimina este entorno.

La primera ejecución conserva una copia en
`map_rom_horde_before_forest.unity`; no se añade esa copia al menú ni al build.

## Alcance y próximos pasos

- Los portales, arsenal, spawns y reglas originales siguen definiendo la Horda.
- La nueva extensión tiene suelo con colisión y NavMesh; los caminos visuales
  se apoyan sobre ese suelo. Se validan cinco rutas desde la arena.
- Los sectores nuevos son composición ambiental, sin misiones ni recompensas.
- Cristales y acantilados son formas provisionales. Faltan cascadas, agua,
  puentes artísticos, raíces, carpas y detalles de transición con la arena.
- Verificar en Play Mode y con host/cliente: navegación de enemigos, colisiones
  de vegetación, cobertura, visibilidad, materiales y coste de renderizado.
- No hay mediciones de rendimiento móvil ni iluminación horneada en esta etapa.
