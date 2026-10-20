# Rally Point y PirateTavern en El Rift

Escena guardada: Assets/ROM/Levels/map_rom_horde.unity.
Grupo de Hierarchy: **El Rift - Rally y costa pirata**.
Se decoran y esculpen las islas existentes. No se crea ninguna isla ni se agregan barcos.

## Sectores

- **Ruinas occidentales:** Archivo hundido en (-265, 2, -88), con bajada abierta, marco de entrada, cámara interior de unos 15 × 16 m, pilares, suelo modular, mesa, suministros e iluminación. El techo queda a la cota del terreno.
- **Santuario del Eclipse:** Refugio de excavación en (-525, 2, 375), con acceso descendente e interior equivalente. Se esculpió el terreno y se abrió la base de la isla para que no atraviese la habitación.
- **Distrito urbano:** dos edificios de taller/comercio en (55, 8, -335) y (145, 8, -335), con fachadas de Rally Point, módulos residenciales, toldos, mobiliario y faroles.
- **Isla del cristal / cantera:** puesto de exploradores en (260, 8, -70) y pabellón arqueológico abierto en (360, 8, -105).
- **Isla del bosque antiguo, costa oriental:** playa en torno a (140, 4, 450), arena seca y húmeda, descenso desde el bosque, acantilados y bahía. Muelle de madera en torno a (160, 2.7, 417), puesto de carga, barriles, redes, cajones y faroles. El espacio de agua queda libre para barcos futuros.
- **Aldea inicial:** pequeño cargamento de barriles y cajas junto al mercado; se conservan sus casas circulares, NPCs y tienda.

## Assets y materiales

El paquete llamado pirate cavern en el pedido está instalado como **Assets/LeartesStudios/PirateTavern**. El otro es **Assets/LeartesStudios/The_Rally_Point**. Se usan prefabs de ambos paquetes, manteniendo sus materiales URP compatibles.
Se crearon materiales locales para piedra con proyección mundial, arena, agua animada y las hojas que presentaban coloración incorrecta. Los originales de los paquetes no se sobrescriben.
Las palmeras se extraen de la composición CoconutPalm de la demo: un conjunto de 304 renderers se combinó en cinco grupos de materiales para seis instancias costeras. El prefab generado está en Assets/ROM/Levels/RiftSetDressing/Prefabs/PiratePalm.prefab.
Los recintos usan mallas de las islas existentes con aberturas recortadas exactamente y nuevas colisiones acordes. Se mantienen copias originales de los terrenos en sus rutas anteriores; las variantes de esta iteración están en RiftSetDressing/Meshes.
El agua es decorativa y animada: todavía no hay sistema de nado, navegación ni barcos.

## Verificación

- Unity compila sin errores de C# ni de los shaders nuevos.
- Se reconstruyó la navegación conservando la referencia de su asset.
- Los siete checkpoints y dieciocho encuentros se conservan. Hay rutas completas desde el refugio a los siete cristales, los dos interiores, el muelle y los puestos nuevos.
- Las rampas tienen espacio para la cápsula del jugador; se comprobó una cuadrícula interior contra colisiones antiguas y se eliminaron las bases que penetraban los recintos.
- Prueba de partida local: personaje en tercera persona dentro de ambos recintos, sobre el muelle, en la playa y ante un local nuevo. Permaneció sobre sus colliders y no cayó.
- validation.txt, palm-validation.txt y play-validation.txt registran estas comprobaciones. Los archivos validation-failed e interior-clearance son diagnósticos intermedios previos a las reparaciones finales.
- Las capturas game-*.png corresponden a la prueba de juego; beach-ground.png y beach-overview.png muestran el acabado posterior de las palmeras.

No se midió el rendimiento multijugador. La distribución es una primera pasada de ambientación y los grupos quedan editables.

## Encontrar las zonas en Unity

**ROM > El Rift > Encontrar playa del puerto**
**ROM > El Rift > Encontrar archivo semienterrado**

Los constructores son herramientas de editor de esta iteración; Build evita duplicar una expansión ya guardada. Para cambios de distribución, editar los grupos existentes en la escena.
