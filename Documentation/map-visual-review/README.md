# Revisión visual del mapa El Rift

Escena guardada: Assets/ROM/Levels/map_rom_horde.unity.

## Cambios

- Old Town: los materiales sólidos convertidos de Post Apocalyptic Town dejan de recortar sus texturas por alfa. Se conservan los recortes de rejas, vegetación y materiales especiales.
- Sus edificios y props con LOD usan colisiones de la malla detallada, en lugar de cajas completas y colliders duplicados en cada nivel de detalle. Las aberturas y partes destruidas dejan de tener paredes invisibles de esas cajas.
- Sun Temple: restauradas 20 referencias de ventanas y 54 props de libros (los libros abiertos ausentes se reemplazan por el libro cerrado disponible). Restauradas las tres texturas de corteza.
- Ocultos 146 planos ornamentales del shader antiguo: mostraban rectángulos/atlas en las fachadas. Para recuperarlos como adornos hace falta una conversión específica a decals URP.
- Cimientos adicionales bajo las fachadas y 210 elementos de vegetación/rocas en el perímetro exterior, con reservas de espacio alrededor de edificios, accesos y cristales.
- Checkpoint de Ciudad del Sol trasladado a la plaza de llegada (626, 9, 229); su antigua terraza estaba debajo del relieve y desconectada de la navegación.
- NavMesh reconstruido conservando la referencia del asset existente.

## Verificación

Unity 6000.3.23f1 compila sin errores de C#. Se reabrió la escena guardada para comprobar los assets.
Ambas zonas: cero MeshFilters sin malla y cero materiales faltantes o shaders incompatibles en los MeshRenderers habilitados.
Los siete puntos de reaparición tienen suelo y ruta completa de NavMesh desde el refugio. Ver asset-and-route-validation.txt.
Se inspeccionaron capturas generales y cercanas, incluidas pruebas con material sólido y LOD único para separar los defectos de materiales de los artefactos de capturas del editor.

Esta pasada repara la integración técnica y añade cobertura al perímetro. El relieve recuperado de Sun Temple sigue siendo aproximado: la composición de sus grandes taludes y la densidad del entorno todavía necesitan trabajo artístico. No se midió rendimiento en una partida multijugador ni se hizo una prueba manual de recorrido con el personaje.

## Encontrar las zonas en Unity

ROM > Horda > Encontrar Ciudad del Sol
ROM > Horda > Encontrar distrito urbano
