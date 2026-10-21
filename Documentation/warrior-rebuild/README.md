# Warrior: jugador nativo y personajes locales

## Probar

Abrir `Assets/ROM/Scenes/MainMenu.unity`. En **CLASE** aparece **Warrior**, una sola clase. En **PERSONAJES → + CREAR PERSONAJE** introducir nombre, elegir clase y crear. Se pueden guardar hasta ocho personajes por perfil local, incluidos varios Warrior. Seleccionar uno y pulsar JUGAR.

Warrior usa ahora el Lizardman nativo de IDS y un hacha de doble filo (`Axe_DS`). Clic izquierdo ataca; F, Z y T conservan las tres habilidades. WASD mueve, Shift corre y Espacio salta. La cámara sigue en tercera persona. La opción de especialización se retiró del menú y del título del HUD.

## Causa del arma ausente

El template separa CharacterParent del objeto de red en Start. El equipamiento anterior buscaba IDSGameplayPose con `actor.GetComponentInChildren`, por lo que no encontraba el cuerpo después de esa separación. La ruta de animación de ataques tenía el mismo problema. La nueva implementación guarda referencias explícitas a su Animator y arma; no realiza esa búsqueda. Las rutas de enemigos antiguos que seguían usando el puente fueron corregidas para buscar en CharacterParent.

## Reconstrucción

Prefab nuevo creado desde un GameObject vacío: `Assets/ROM/Characters/Warrior/WarriorPlayer.prefab`.

- Rig y Avatar nativos de Lizardman Berserker.
- `WarriorAvatar`: motor propio con aceleración, frenado, movimiento relativo a cámara, sprint, salto, gravedad y daño por caída calculado en servidor. No instancia CharacterMotor.
- `WarriorLocomotion.controller`: Animator nuevo, blend de direcciones/velocidad, aire, muerte y cuatro estados de ataque. Clips humanoides importados de Mobility Starter y Samurai; no usa clips ni controladores Soldier del template.
- No contiene CharacterAnimator, CharacterMotor, IDSGameplayPose, RigBuilder ni el esqueleto del template. No hay copia de poses desde un rig invisible. Los pies/las piernas los anima directamente Mecanim sobre el Avatar nativo.
- Hacha visible en el hueso RightHand, con escala y orientación obtenidas del modelo real; materiales URP propios. El prefab de lobby usa el mismo cuerpo/controlador y muestra el arma.
- Hitboxes de cabeza, pecho y cadera en el nuevo rig; cápsula de movimiento independiente. Muerte animada y resurrección, sin reutilizar el ragdoll del template.

**Adaptadores conservados:** CharacterInstance y CharacterItemManager (vacío) permiten seguir usando el spawn, registro de vida, HUD y mensajes actuales. DNTransform y los mensajes de input/posición conservan la replicación Mirror existente. No controlan el esqueleto nuevo. Las animaciones de ataque se disparan mediante el RPC de AbilityController, directamente al Animator nuevo; vida, daño y recursos mantienen autoridad de servidor. El movimiento conserva el modelo de propietario del proyecto, no se anuncia como una nueva solución anticheat.

## Clase e identidades

`LocalCharacterRoster` guarda registros con Id, nombre, ClassKey y Skin, separados de los perfiles sociales y de las especializaciones. Se persiste en PlayerPrefs bajo `ROM.CharacterRoster.v1.<profileId>`. La clase nueva se llama Warrior; Nertör queda como definición de especie y los antiguos datos Guardian/Ravager permanecen como configuración interna de balance, sin selector de especialización.

Esta primera versión tiene un cuerpo Warrior (Lizardman). Se pueden crear varias identidades Warrior ahora; otros cuerpos/modelos para la clase se incorporarán posteriormente. La creación es local; no es todavía una cuenta de personajes validada por backend.

## Archivos

- `Assets/MultiFPS/Scripts/Gameplay/RiftCombat/WarriorAvatar.cs`: nuevo motor, Animator nativo, presentación de ataques, muerte/equipo y caída.
- `Assets/ROM/Editor/WarriorRebuildBuilder.cs`: generación del prefab/controlador/hacha, lobby y registro de prefab Mirror. Menú **ROM → Personajes → Reconstruir Warrior nativo**. Los assets ya están guardados; no hace falta reconstruir para jugar.
- `Assets/ROM/Scripts/UI/LocalCharacterRoster.cs`: registros persistentes de personajes.
- `Assets/ROM/Scripts/UI/RiftHub.Characters.cs`: creación, listado, selección y clase Warrior.
- `PlayerInstance.cs`: selección del prefab nuevo por clase y adaptación de aplicación del modelo.
- `CharacterInstance.cs`, `CharacterItemManager.cs`, `PlayerGameplayInput.cs`: rutas específicas para no crear armas del template ni ejecutar su motor/input de armas en Warrior.
- `AbilityController.cs`, `CombatEquipment.cs`, `RiftEnemyCombat.cs`: destinos explícitos de presentación y corrección de la búsqueda del rig separado.
- `NertorHUD.cs`: el título muestra Warrior y el nombre del personaje.
- `Assets/ROM/Characters/Warrior/`: prefabs, arma/materiales y Animator propios. Los modelos y animaciones originales de terceros no se modifican.

## Verificación y límites

Informes: `build-validation.txt` y `play-validation.txt`. Capturas: `menu.png`, `walk.png`, `run.png`, `attack.png`, `idle-axe.png`, `warrior-clean.png`.

La prueba usa la UI real para crear un personaje; comprueba persistencia de varias identidades de la misma clase, registro/autoridad del prefab en Mirror, hacha renderizada y vinculada a la mano nativa, ausencia de componentes antiguos, caminar/correr mediante un teclado virtual del Input System, alineación cuerpo/cápsula, frenado, estados de ataque animados, daño/Furia, muerte/resurrección, salto y daño de una caída real. Los datos temporales del test se eliminan y se restauran preferencias/dispositivo/foco de input.

La verificación de red ejecuta un host local; aún falta validar cliente remoto/latencia y rendimiento móvil. El ajuste artístico del agarre y animaciones seguirá necesitando feedback jugando. No se promete eliminar todas las diferencias entre clips de paquetes distintos.

La reconstrucción se aplica a **Warrior**. Las cuatro clases anteriores conservan su ruta existente hasta migrarlas; no se borran en esta iteración. Warrior equipa el hacha en esta entrega: rifles/pistolas y otros tipos de armas necesitarán estados y sockets del nuevo sistema antes de habilitarlos. El ragdoll físico y una mano secundaria con IK son tareas posteriores.
