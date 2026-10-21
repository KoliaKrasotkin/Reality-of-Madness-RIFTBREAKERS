# Aldea nocturna y bruma de El Rift

Escena: Assets/ROM/Levels/map_rom_horde.unity.

## Aldea

Grupo editable en Hierarchy: **Refugio - Aldea**.
Ocho viviendas distribuidas en un círculo de 43 m de radio alrededor de la plaza, con entradas orientadas hacia el centro y pasos libres hacia los puentes, usando Sun Temple (piedra, revoco, madera y tejas), dos puestos con mesas y provisiones, bancos, reserva de leña, hogar y trece faroles de aceite a escala humana montados en postes.
Se mantienen el arsenal, el cristal, el entrenamiento y los puntos de aparición. Los marcos de portales, anillos emisivos y coberturas geométricas del antiguo inicio quedan desactivados; el suelo conserva sus colisiones y recibe pavimento de piedra con UV en espacio mundial.
Se despejó vegetación inmediata para las viviendas. Las casas conservan su escala proporcional y usan colisiones de las mallas detalladas.

## Iluminación y smoke

Grupo: **El Rift - Noche y bruma**.
Componente **RiftNightAtmosphere**: luz principal fría 0.8, cielo del Rift a exposición 0.42, ambiente azul con sombras legibles y bruma exponencial al cuadrado de densidad 0.0038.
Los faroles dan luz cálida local sin sombras adicionales; los cristales sirven como balizas frías.
La corrección de exposición nocturna base (+0.65 EV) se suma al brillo elegido por el jugador; el menú mantiene su exposición propia.
Siete sistemas de partículas de borde, con ruido procedural animado, intersecciones suaves con el terreno y desvanecimiento al acercarse a la cámara. Máximo 32 partículas por isla, 224 en total. La bruma de distancia reduce progresivamente la visibilidad de las islas alejadas.
El material del smoke es Assets/ROM/Rendering/VillageNight/RiftMist.mat y el shader ROM/Drifting Mist.
No utiliza un paquete externo de niebla volumétrica ni realiza streaming u occlusion culling de las islas: el ocultamiento es visual.

## Verificación

- Compilación de Unity y del shader.
- Colliders libres en todos los puntos iniciales y en el dummy; sigue dentro de la zona segura.
- Siete rutas completas de NavMesh desde el refugio a los checkpoints tras reconstruir la navegación.
- Prueba local desde el menú con personaje en tercera persona; capturas de la plaza y el camino norte con la cámara real del juego.
- validation.txt y play-validation.txt registran los resultados; las capturas están en esta carpeta.

No se midieron FPS en una partida multijugador. Los parámetros de luz, niebla, exposición, partículas y distribución quedan editables en la escena.


## Habitantes y comercio (iteración actual)

Alma, boticaria (-14, 0, -23); Bram, intendente (14, 0, -23); Iria, exploradora (-17, 0, 12). Altura ajustada al terreno. Modelos de los personajes IDS existentes con idle y colisión, sin Health de enemigo. Están guardados bajo Refugio - Aldea > Habitantes del refugio.

- G cerca de un NPC: diálogo. Hablar del refugio muestra sus consejos; Alma y Bram permiten abrir mercancía. Esc o Cerrar devuelve el control.
- Poción de vida: compra 25 oro del juego, recupera 40% de salud máxima, reventa 12.
- Poción mayor: compra 60, recupera 75%, reventa 30.
- H: bebe una normal si hay; si no, una mayor. Cinco segundos entre dosis. Estar muerto, en recarga o con toda la vida no gasta pociones. Máximo 99 por tipo.
- Se guardan las existencias y el oro del juego mediante PlayerPrefs por el nickname actual. Ancient Gold y Rift Crystals pertenecen a los servicios del menú y no intervienen en las transacciones de la aldea.
- Las órdenes de comercio y curación pasan por RiftVillagePlayer, componente de red en el prefab del jugador. El servidor comprueba proximidad al comerciante, zona protegida, producto, saldo y límites antes de aceptar. La salud y el cooldown se replican mediante Mirror.
- Esta cartera sigue siendo el prototipo local existente. El comercio y el uso de sus consumibles están habilitados para el host local; los clientes remotos reciben un aviso hasta conectar una cartera de backend, evitando que gasten el saldo del host. No es una economía online segura todavía.

commerce-validation.txt registra una partida de prueba que compra desde la UI, valida saldo insuficiente, cura, comprueba el cooldown, revende y rechaza comercio fuera de alcance. La prueba restaura los saldos y cantidades originales, incluso al fallar. npc-alma-world.png y npc-dialogue-shop.png muestran al NPC y la interfaz del comercio.
