# Autoataque independiente, daño flotante y críticos

## Jugar

En El Rift, clic izquierdo sobre un enemigo activa el autoataque. Las habilidades ofensivas disponibles también lo activan. T lo activa o detiene, sin ocupar un espacio de la hotbar; se puede reasignar en Ajustes > Controles. Tab selecciona sin atacar y Escape limpia el objetivo. Seleccionar un NPC detiene el autoataque.

Warrior: **1 Furia cortante**, **2 Rompetierra**, **3 Rugido de honor**. Las habilidades futuras siguen bloqueadas. El autoataque ya no aparece como habilidad 1. La ventana K muestra el autoataque por separado.

El autoataque tiene su propio temporizador y golpe validado por servidor, sin consumir la recarga ni el global cooldown de la habilidad 1. Valida objetivo hostil vivo, alcance, orientación y línea de visión, tanto al comenzar como al aplicar el golpe. No camina hacia el enemigo. No inicia una animación de autoataque mientras otra habilidad está casteando. El golpe ya iniciado tiene un tiempo breve de preparación; quitar/cambiar el objetivo invalida el golpe pendiente.

## Perfiles editables

`Assets/ROM/Resources/RiftCombatTuning.asset` contiene los cinco perfiles:

| Clase | Ataque | Daño base | Intervalo | Alcance |
|---|---|---:|---:|---:|
| Warrior | Golpe de arma | 18 | 1,8 s | 3,4 m |
| Paladin | Golpe sagrado | 16 | 1,8 s | 3,4 m |
| Bastion | Golpe pesado | 20 | 2,1 s | 3,4 m |
| Battle Mage | Descarga arcana | 15 | 1,8 s | 25 m |
| Hunter | Disparo certero | 14 | 1,5 s | 30 m |

Son valores iniciales antes de modificadores de clase, especialización, arma y mitigación. La presentación reutiliza las animaciones disponibles; no se creó un paquete nuevo de animaciones ni proyectiles exclusivos para cada clase.

Los perfiles incluyen daño, alcance, arco frontal, intervalo, preparación, generación de recurso y críticos. El valor inicial de crítico es **15%**, con multiplicador **×2**. Los enemigos NPC no reciben esta nueva probabilidad de crítico. Se aplica a los ataques y habilidades dañinas de jugadores, incluidos golpes contra el muñeco. Caídas, curaciones, daño amistoso y daño transferido no producen críticos por esta vía.

## Números de daño

Los resultados aplicados se envían desde Health mediante un TargetRpc al jugador que causó el daño, sin objetos de red por cada número. Se muestra el valor real después de modificadores y mitigación; el daño de sobre-muerte se limita a la vida restante. El muñeco muestra el daño registrado sin perder vida.

Texto amarillo flotante sobre el objetivo; crítico dorado, en negrita, más grande, con un pulso de escala y un signo !. Duración 1,3 segundos, desplazamiento y desvanecimiento. Pool limitado a 48 textos; no bloquea clics y se oculta al abrir menús. Ningún texto ni VFX calcula daño.

La acción de disparo manual del arma convencional queda separada en Ajustes > Armas, con botón central del mouse por defecto; la tecla 1 pertenece a la habilidad de clase.

## Código y validación

- `AbilityController.AutoAttack.cs`: temporizador independiente, validación, golpe y presentación; RPC de resultados al propietario.
- `RiftCombatTuning.cs`: perfiles configurables.
- `CombatController.cs` y `Health.cs`: tirada de crítico en servidor y daño aplicado.
- `RiftCombatNumbers.cs`: presentación UGUI con pool.
- `RiftMMOController`, `RiftControls`, `RiftGameplayHUD` y `RiftHub.Gameplay`: entradas y hotbar separadas.
- `RiftCombatFeedbackBuilder`: crea el asset inicial sólo si falta; no pisa los valores editados.

La prueba `RiftJournalProbe.Run` usa un host local, datos de personaje aislados y eventos reales de daño. Incluye los cinco perfiles, independencia de recargas, habilidad 1 de hotbar, números normales y crítico forzado al 100% comparado con el daño registrado. El valor de crítico se restaura tras la prueba. La validación no sustituye una prueba de cliente remoto o de hardware móvil.

Última prueba: **70 comprobaciones aprobadas en host local**, incluidos daño real, eventos RPC, perfiles de cinco clases y hotbar. Captura: `../quest-system/combat-damage-numbers.png`.
