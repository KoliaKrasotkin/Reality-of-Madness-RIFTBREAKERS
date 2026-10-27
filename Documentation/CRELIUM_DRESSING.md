# Crelium: decoración aditiva y pisos

La escena Assets/ROM/Tutorial/Crilium/Crilium.unity incorpora una instancia de Dressing/CreliumFoliageLayer.prefab, con nombre Crelium additive foliage - editable. Se puede desactivar, mover y editar cada prefab de vegetación sin tocar límites, objetivos o suelo físico. Usa vegetación seca y árboles existentes; no añade colliders, scripts de juego ni bake de NavMesh. Se revisó remoto antes de editar: sin commits entrantes ni cambios locales previos en esa escena; se mantiene el trabajo de otros miembros.

Son302 instancias, incluidos9 árboles; renderers activos150→452. Se inspeccionaron imágenes reales del editor antes/después con cámaras iguales y a altura de jugador. Este recuento no es una medición FPS ni una garantía WebGL. Ajustar densidad eliminando/desactivando instancias del prefab si el equipo detecta coste en el dispositivo objetivo.

## Pisos

Siete Bld_Square_Floor_A atravesaban su Walkable foundation: vértices entre-0.19356 y0.03498m contra superficie y0. Las representaciones originales se conservan deshabilitadas. Sus hijos Separated floor visual - original collider preserved usan copias de malla bajo Dressing/Meshes; sólo se elevan vértices visuales enterrados a0.012m. Se mantienen UV y superficie superior existente; no se desplazaron colliders, triggers, respawns, rutas o límites. No es un offset global ni modificación del mesh fuente.

La firma física completa se verificó idéntica como multiconjunto. Un primer guard rechazó guardar por orden inestable de objetos con igual nombre; el diagnóstico confirmó igualdad exacta y se corrigió sólo el orden del auxiliar.
