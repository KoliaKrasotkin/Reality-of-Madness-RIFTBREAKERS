# Crelium: decoración aditiva y pisos

La escena Assets/ROM/Tutorial/Crilium/Crilium.unity incorpora una instancia de Dressing/CreliumFoliageLayer.prefab, con nombre Crelium additive foliage - editable. Se puede desactivar, mover y editar cada prefab de vegetación sin tocar límites, objetivos o suelo físico. Usa vegetación seca y árboles existentes; no añade colliders, scripts de juego ni bake de NavMesh. Se revisó remoto antes de editar: sin commits entrantes ni cambios locales previos en esa escena; se mantiene el trabajo de otros miembros.

Son302 instancias, incluidos9 árboles; renderers activos150→452. Se inspeccionaron imágenes reales del editor antes/después con cámaras iguales y a altura de jugador. Este recuento no es una medición FPS ni una garantía WebGL. Ajustar densidad eliminando/desactivando instancias del prefab si el equipo detecta coste en el dispositivo objetivo.

## Pisos

Siete Bld_Square_Floor_A atravesaban su Walkable foundation: vértices entre-0.19356 y0.03498m contra superficie y0. Las representaciones originales se conservan deshabilitadas. Sus hijos Separated floor visual - original collider preserved usan copias de malla bajo Dressing/Meshes; sólo se elevan vértices visuales enterrados a0.012m. Se mantienen UV y superficie superior existente; no se desplazaron colliders, triggers, respawns, rutas o límites. No es un offset global ni modificación del mesh fuente.

La firma física completa se verificó idéntica como multiconjunto. Un primer guard rechazó guardar por orden inestable de objetos con igual nombre; el diagnóstico confirmó igualdad exacta y se corrigió sólo el orden del auxiliar.

## Regiones del mapa ilustrado (rama Tutorial-map-evolved)

`ROM > Crilium > Apply Crelium map regions (visual only)` (`Editor/CriliumMapEvolved.cs`) reconstruye la raíz **Crelium map regions - editable** tomando como guía el mapa ilustrado de Crelium. Se puede volver a ejecutar: borra y regenera únicamente esa raíz, con semilla fija.

| Grupo | Ubicación (x, z) | Assets |
| --- | --- | --- |
| Vitral desert camp | alrededor del inicio | anillo de carpas rojas (TradingTent) y toldos, empalizada con banderas, fogata comunal con bancos y alfombras, puestos con vasijas a los lados del camino, suministros, rincón de entrenamiento, antorchas hacia el norte y polvo en el aire |
| Vitral oasis | (-58, -4) | estanque y palmeras (PiratePalm) |
| Saara Woods | sólo al otro lado del río (x>112) | Fol_TallTree/Pine, Eternal Temple Tree/Bush |
| Saara river | del Sea of Embers (sudeste) hasta las dunas del norte | cinta de agua continua con curvas (CreliumRiverWater), orillas y palmeras |
| Gibahar Forest camp | (150, 95), al otro lado del río | carpas alrededor de una fogata |
| Quahar Outpost | (80, 190) | Cliff_Formation, Building_Preset_05, muros, puerta y antorchas |
| Old Sanctuary | (-60, 45) | aguja de roca, ruinas, arco y columnas |
| Ashara Dunes | noroeste | dunas y puerta egipcia con columnata |
| Eshkar Ruins | (-45, 190) | Building_Ruins, muros rotos y estatua egipcia |
| The Great Desert | norte, hacia la pirámide | avenida de estatuas/pilares, mastabas, dunas y pilón caído |
| Mountains and seas | lejano | Bgr_Mountains, Sea of Solace, Sea of Embers y Azras Spire |
| Route edge hints | justo fuera de los muros ocultos | piedras y arbustos bajos que marcan el borde jugable |

Agrega un suelo de arena continuo (`Ground - visual only`, y−0.15 bajo las fundaciones en y0), así la ruta se lee como parte del continente. Sólo hay verde donde hay agua: la orilla del río, el bosque de Saara y el Old Sanctuary. No hay casas: Vitral y Gibahar son campamentos. Todo es solo visual: la herramienta elimina los colliders de la raíz y se niega a guardar si cambia la firma física del resto de la escena, así que las fundaciones, los muros ocultos, los objetivos, los respawns y el NavMesh quedan intactos. El decorado se marca como Batching/Occludee static, salvo los sistemas de partículas.

También vuelve a capturar `Resources/Crilium/CriliumAtlas.png` con la misma cámara ortográfica, así el mapa y el minimapa muestran las regiones nuevas.

Resultado: 655 objetos y 2128 renderers, contando los niveles de LOD. Es una cifra de edición, no una medición de FPS. Si el dispositivo objetivo lo necesita, se puede desactivar por grupo (por ejemplo Mountains and seas o Route edge hints). Las capturas reales del editor están en `Documentation/crelium-map-evolved/`.

### Vista a distancia

Los parches de suelo (bosque de Saara, orillas, oasis, Old Sanctuary) y los dos mares son mallas generadas con contorno irregular, no planos cuadrados. El río es una cinta suave (Catmull-Rom) con ancho variable. Las mallas se guardan en `Dressing/Meshes/MapRegions/` y se regeneran en cada ejecución. La arena continental mide 6 km, así el borde queda dentro de la niebla. Los árboles, las palmeras, las estatuas, las mastabas, las montañas y las rocas lejanas son más grandes. Para que el decorado lejano no desaparezca, el último LOD de cada prefab recién se descarta al 0,2 % de la pantalla.
