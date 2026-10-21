# Inventario y tirones al caminar / combatir

- El Animator del Warrior recibe FootL y FootR mediante RiftFootstepEvents. Se conservan los clips originales. Los eventos son de presentación y por ahora no reproducen audio de superficie.
- Las instancias de enemigos se preparan al inicio de la escena y se reutilizan. Se conserva un máximo de 48 instancias inactivas, separado del límite de 24 enemigos activos. Si se agota un tipo, se permite crear una instancia adicional. La preparación agrega trabajo a la carga inicial para evitar hacerlo al explorar.
- Cada reutilización reinicia vida, amenazas, casteo, recargas, navegación, animación, muerte y collider. Se cancela la eliminación diferida del cadáver. Mirror despublica y vuelve a publicar la identidad; el registro de daño se libera antes de retirar y se reconstruye después del mensaje de spawn.
- Se corrigió la resuscripción de DNNetworkBehaviour al reactivar objetos.
- Inventario con I: casillas de provisiones reales, cantidades, detalles, porcentaje de curación, tecla asignada, botón de uso y recarga compartida con la hotbar. Muestra oro local y equipo actual, con acceso a Personaje. Las casillas vacías están reservadas para futuras categorías: todavía no son un sistema nuevo de loot/equipamiento arbitrario.
- La barra de pociones y el botón de mapa se ocultan mientras el menú de pausa/inventario está abierto.

## Validación

20 comprobaciones funcionales en Play/host: abrir inventario, consumir, seleccionar poción mayor, recarga, eventos de pisada, retirar/reutilizar un enemigo, matarlo, revivir la instancia y registrar nuevamente su identidad de daño. Resultado: inventory-validation.txt. Captura: progression-inventory.png.

Prueba de 42 segundos aproximados de entrenamiento, caminar y combatir en el mapa real, en Editor y sin Deep Profile:

| Medida | Antes del pool | Después del pool |
| --- | --- | --- |
| Máximo de Enemy.Spawn | 113,37 ms | 3,57 ms |
| Creaciones/apariciones medidas | 9 | 9 |
| Frame más largo de la muestra | 121,06 ms | 19,91 ms |
| Frames por encima de 100 ms | 2 | 0 |
| Mediana de frame | 8,34 ms | 8,34 ms |

Son muestras locales, no una garantía para cualquier combate, hardware o cliente remoto. Se mide el tramo jugable, no la carga inicial. No aparecen errores de receiver, excepciones ni errores de compilación en la pasada de rendimiento posterior. Permanecen avisos preexistentes de colliders con escala negativa en Sun Temple, fuera de este cambio.

Resultados detallados: performance-before-enemy-pool.txt, performance-validation.txt y Logs/Inventory-Performance-After.log. Las provisiones conservan su inventario local del host; el backend de inventario para clientes remotos sigue pendiente.
