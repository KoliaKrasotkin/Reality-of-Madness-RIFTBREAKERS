# Reality of Madness: RIFTBREAKERS — Notas de versión

Historial de builds entregados a QA. El build más reciente va primero.

---

## Build 0.1.0 — 13 de octubre de 2026

**Primera entrega a QA.** Base del proyecto sobre el kit MultiFPS, con el primer
modo de juego jugable, el conjunto inicial de personajes y la base del sistema
de dificultad.

- Rama: `main`
- Motor: Unity 6000.3.23f1
- Plataforma: Windows x64
- Red: servidor autoritativo (Mirror), host + clientes

### Nuevo

**Modo de juego: Raid**

- Modo Raid integrado al kit, seleccionable desde el menú principal en el mapa
  `raid_test`.
- Sistema de oleadas de enemigos con generación automática. Dos oleadas
  configuradas y funcionales.
- Contador de enemigos vivos. La oleada avanza al eliminar a todos los enemigos.
- Los enemigos aparecen en puntos elegidos al azar, con control de distancia
  mínima respecto del jugador.
- IA enemiga con patrulla, detección de jugadores cercanos y combate.
- Persecución limitada: los enemigos dejan de perseguir cuando el jugador se
  aleja lo suficiente.
- Los enemigos sueltan su arma al morir y se puede recoger.
- Objetivo destructible con vida propia, que aparece en uno de varios puntos
  posibles del mapa elegido al azar. Al destruirse desaparece y no vuelve a
  aparecer hasta la próxima partida.
- Aparición de jugadores en zonas designadas, elegidas al azar.
- Sistema de checkpoints: registro individual por jugador y reaparición en el
  último checkpoint alcanzado.
- Generación y destrucción del objetivo sincronizadas por red.

**Personajes y apariencias**

- Cinco personajes jugables nuevos: Redhead, Purplehead, White Knight,
  Black Panther y Dinork.
- La apariencia del personaje pasa a ser una preferencia del jugador en lugar de
  elegirse al azar en cada aparición.
- Soporte de materiales múltiples en las mallas de primera persona, que antes
  admitían uno solo.

**Sistema de dificultad: Threat Levels**

- Base del sistema de dificultad del GDD. Cada nivel de amenaza es una
  configuración editable que agrupa los parámetros que la dificultad controla:
  duración del sangrado de un personaje caído, tiempo necesario para revivir a un
  compañero, vida con la que vuelve el revivido, disponibilidad de auto-revive y
  presupuesto de revividas de la escuadra.
- Tres niveles configurados: Threat I, II y III, con una progresión que acorta el
  sangrado, encarece el revive y reduce las revividas disponibles.
- Los Threat Levels **no** escalan daño ni vida, por decisión de diseño del GDD:
  la dificultad sube por presión y por margen de error.
- El nivel activo lo decide el servidor y se sincroniza a los clientes, incluido
  el caso de un jugador que se conecta a una partida ya iniciada.

### Cambios

- El reemplazo del personaje `soldier2` por `Soldier` unifica el esqueleto base
  de los personajes.
- La generación automática de bots de relleno queda desactivada en el modo Raid,
  donde los enemigos los administra el sistema de oleadas.
- Los bots del kit se reutilizan como enemigos de las oleadas de Raid.

### Correcciones

- Corregida la estructura de huesos de Redhead, Purplehead y White Knight, que
  impedía que las animaciones y el ragdoll funcionaran correctamente.
- Corregido el ragdoll de los personajes nuevos.
- Corregida la posición del arma en vista de tercera persona.
- Corregidos errores de visualización en las mallas de primera persona.
- Corregida la escala de los personajes.
- Corregido el manejo de equipos y de daño de los enemigos en el modo Raid.
- Eliminado el personaje de ejemplo `BOOM`, que no forma parte del proyecto.

### Notas para QA

**Qué conviene probar en este build**

- Modo Raid en `raid_test`: avance de oleadas, comportamiento de la IA, recogida
  de armas soltadas, destrucción del objetivo y reaparición en checkpoints.
- Los cinco personajes nuevos: animaciones, ragdoll al morir, posición del arma
  en tercera y primera persona.
- Partidas en red con más de un jugador, incluido conectarse a una partida ya
  iniciada.

**Qué no está implementado todavía en este build**

Esto se aclara para evitar reportes sobre funcionalidad que aún no existe:

- **El sistema de dificultad no tiene efecto visible.** Es la capa de datos y de
  red; los sistemas que la consumen están en desarrollo. No hay forma de elegir
  el nivel de amenaza desde el menú: por ahora se configura en la escena.
- **No existe el estado de personaje caído ni el revivir entre compañeros.** Al
  quedarse sin vida, el personaje reaparece con el comportamiento del kit base.
- **No hay controles táctiles.** El proyecto apunta a móvil, pero este build es
  solo de escritorio.
- **No hay cuentas ni progresión persistente.** Nada se guarda entre partidas.
- **No hay reconexión a una partida en curso.** Si un jugador pierde la conexión,
  no puede volver a la misma partida.
- El mapa `ROM Testbed` que aparece en la lista es un entorno interno de
  desarrollo, no contenido jugable.

**Problemas conocidos**

- Al cerrar un cliente de forma abrupta, el servidor registra excepciones del
  transporte de red. No afecta a los jugadores que siguen en la partida.

### Detalle técnico

- Se agregó una sobrecarga a la resurrección de personajes para poder devolver
  una cantidad parcial de vida en lugar de la vida completa.
- Mapa interno de pruebas (`ROM Testbed`) para desarrollar sistemas propios sin
  modificar los mapas del kit.
- Herramienta de diagnóstico de dispositivos de entrada, accesible por consola.
- Configuración y documentación del merge driver de Unity para el repositorio,
  necesario para que escenas y prefabs se integren correctamente entre ramas.
- Migración de assets al formato de serialización de Unity 6.
