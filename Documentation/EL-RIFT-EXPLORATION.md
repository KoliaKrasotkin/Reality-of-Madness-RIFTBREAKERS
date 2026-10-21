# El Rift: exploración local

El mapa existente pasa a una sesión de exploración cooperativa sin oleadas.
El punto inicial conserva los spawns anteriores y tiene una zona segura de
110 metros. Los seis destinos exteriores tienen un cristal y una zona segura
de 22 metros. Acercarse a 9 metros registra el último checkpoint individual
y cura al personaje. La reaparición utiliza ese cristal sin consumir reservas.
Los checkpoints son de sesión; salir del mapa reinicia el recorrido.

Cada isla exterior tiene tres grupos de cuatro enemigos: dos patrullas y un
encuentro con guardián de mayor vida. Se activan al aproximarse, patrullan cerca
de su origen y no buscan objetivos dentro de refugios. Las patrullas remotas
se retiran sin recompensa para liberar el presupuesto de enemigos; los grupos
completados permanecen despejados durante la sesión. No hay puzzles todavía.

Las zonas seguras bloquean daño entre enemigos y jugadores en ambas direcciones.
Las caídas conservan su muerte por vacío, incluso desde refugios. La cámara es
exclusivamente tercera persona, con prueba de colisión contra el entorno.

Prototipo de recompensas: 20 de oro local por enemigo, 150 por guardián y 75 por
limpiar un encuentro para cada jugador cercano. Las recompensas se otorgan en
el servidor de la partida y se guardan localmente por nombre de perfil. Se ven
en el HUD y en Progreso. Es un saldo de exploración separado de los saldos de
prueba de la tienda y no constituye una economía de backend.

Los nueve selectores de video usan listas desplegables. Aplicar conserva el
flujo de confirmación/reversión de los ajustes anteriores.

Configuración de escena: ROM > El Rift > Configurar exploración.
Cristales: reutilizan la malla CrystalShard y el material DimensionalCrystal
que ya tenía el mapa; no se descargaron assets nuevos.

Verificación: partida local con inicio sin enemigos después del tiempo que antes
disparaba la oleada, apertura de desplegables, registro de cristal, protección de
daño, recompensa de combate/guardián y reaparición en tercera persona.
El oro generado por las pruebas se restaura al saldo previo.

Limitación detectada: el ragdoll anterior del kit no puede mapear algunos huesos
de las skins nativas (pelvis/spine). La prueba de muerte/reaparición pasa, pero
los cuerpos al morir necesitan una adaptación independiente. No se probó aún
una sesión con varios clientes ni un recorrido manual completo de cada isla.
