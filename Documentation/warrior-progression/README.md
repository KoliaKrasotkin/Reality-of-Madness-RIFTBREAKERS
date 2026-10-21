# Warrior — siete habilidades, progresión y talentos

## Probar

Abrí `Assets/ROM/Scenes/MainMenu.unity`, seleccioná Warrior y entrá en El Rift.

- Clic izquierdo / T: autoataque independiente, igual que en la iteración anterior.
- Teclas **1–7**: siete habilidades activas. La 7 recupera el antiguo golpe manual como **Barrido**, un cleave frontal que golpea a varios objetivos.
- **K > TALENTOS**: invertir puntos en tres líneas iniciales. Cada nodo explica sus requisitos y efectos. El reinicio es gratuito con confirmación.
- **B > EQUIPAR ESPADA Y ESCUDO / HACHA A DOS MANOS**: cambiar equipo. No cambia la clase ni el personaje. También se guarda por personaje.
- XP y nivel aparecen junto al retrato; las misiones muestran su recompensa de XP.

## Siete habilidades, además del autoataque

| Tecla | Habilidad | Prototipo funcional |
|---|---|---|
| 1 | Furia cortante | Golpe frontal amplio que genera Furia |
| 2 | Rompetierra | Área circular, daño y stagger |
| 3 | Rugido de honor | Buff y amenaza/taunt según el perfil de combate existente |
| 4 | Salto de guerra | Desplazamiento hasta el suelo bajo el objetivo o hacia delante, máximo 12 m, con impacto de área |
| 5 | Torbellino | Cuatro impactos circulares durante 1,8 s; velocidad de movimiento al 55% |
| 6 | Última resistencia | Reduce un 50% el daño recibido durante 6 s; separado del buff de Rugido |
| 7 | Barrido | Golpe recuperado: cleave de 110°, radio 3,6 m, 22 daño base, 1,5 s de recarga y generación de Furia |

Cada golpe alcanza una sola vez a cada Health. Torbellino define explícitamente cuatro eventos de golpe. Barrido no reemplaza ni utiliza el temporizador del autoataque. Las habilidades usan las animaciones y VFX disponibles; se reutilizan iconos temporalmente. El salto hace un desplazamiento autoritativo al aterrizaje; todavía no tiene una trayectoria aérea animada exclusiva. No se creó un nuevo rig ni se volvió al personaje del template.

## Experiencia y niveles

Primera curva configurable: nivel máximo **20**, un punto de talento por nivel ganado (19 en total), daño base +2% y vida base +8% por nivel. XP requerida para pasar del nivel L al siguiente: `100 + 50*(L-1) + 25*(L-1)^2`.

| Evento válido del servidor | XP inicial |
|---|---:|
| Enemigo derrotado | 35 |
| Miniboss | 150 adicionales |
| Encuentro despejado | 75 |
| Primer descubrimiento de un cristal fuera del refugio | 60 |
| Misión entregada | 250 |

El muñeco da **0 XP**. Volver al mismo cristal y repetir un comando de entrega no permite duplicar sus recompensas. Los eventos anteriores a esta implementación no reciben XP retroactiva. El reparto sigue las reglas actuales: bajas al atacante y encuentros a jugadores cercanos. No hay todavía escalado de niveles de enemigos ni balance definitivo de toda la curva.

## Rama inicial de talentos de Warrior

Tres rangos por nodo. Los nodos iniciales requieren nivel 2, los intermedios nivel 5 y los finales nivel 10. Para avanzar hace falta completar los tres rangos del nodo anterior de la misma línea.

| Línea | Talento | Por rango |
|---|---|---|
| Armas | Disciplina de armas | +3% daño |
| Armas | Hoja amplia | +15° y +0,3 m a Barrido y Furia cortante |
| Armas | Instinto ejecutor | +1% crítico y +5% daño de Barrido |
| Protección | Piel de hierro | +2% reducción de daño |
| Protección | Tenacidad | +5% vida máxima |
| Protección | Maestría del escudo | Con escudo, +3% reducción y -5% recarga de Última resistencia |
| Furia | Corazón de furia | +10% generación de Furia |
| Furia | Golpe sísmico | +8% daño y +0,1 s de stagger de Rompetierra |
| Furia | Ritmo de batalla | -3% recargas de habilidades |

El servidor valida puntos, clase, nivel, rango máximo y prerrequisitos. Aprender, reiniciar y cambiar equipo requiere estar vivo, sin ejecutar una habilidad y estar en el refugio o llevar ocho segundos sin combate. Se conserva la proporción de vida al cambiar el máximo, evitando curarse repetidamente al alternar equipo.

## Espada y escudo

La espada proviene de `Free medieval weapons/Prefabs/Sword_OH.prefab`, con un wrapper y materiales URP propios. El escudo es un prefab temporal circular de madera y metal generado dentro de ROM. La espada se adjunta a la mano derecha y el escudo al antebrazo izquierdo; sus colliders no interfieren con movimiento ni ataques.

Al equipar escudo se oculta el hacha, se gana +15% de vida y una capa de reducción de daño del 12%, a cambio de un multiplicador de arma inicial de 0,85. Los talentos pueden mejorar esa configuración. No hay aún una animación exclusiva de guardia con escudo ni bloqueo activo direccional.

## Datos y archivos

`Assets/ROM/Resources/WarriorProgression.asset` configura curva, recompensas, talentos y referencias de equipo. `Assets/ROM/Combat/CleavingSweep.asset` contiene Barrido. La clase Warrior contiene siete definiciones activas y mantiene BasicAttack sólo para la presentación del autoataque. Las habilidades futuras pueden reemplazar esas definiciones sin aumentar los siete espacios, pero todavía no hay un selector de loadout de talentos activos.

- `AbilityController.Progression.cs`: XP sincronizada, rangos, modificadores y equipo.
- `WarriorProgressionDefinition.cs`: datos de curva y talentos.
- `RiftJournalPlayer.Progression.cs`: comandos y persistencia por personaje, junto a las misiones ya existentes.
- `RiftWarriorEquipment.cs`: presentación del equipo nativo.
- `RiftHub.Talents.cs`: árbol inicial accesible desde K.
- `WarriorProgressionBuilder.cs`: preparación de assets, siete habilidades y HUD. Ya está ejecutado. Volver a ejecutarlo restaura los valores iniciales de habilidades/equipo; editar el asset de progresión no requiere ejecutarlo.

Los guardados previos migran con XP cero, talentos vacíos y hacha, conservando misiones/logros. Se mantiene Mirror. El guardado local aún confía en la identidad del personaje del prototipo; no añade autenticación ni backend.

## Validación

**97 comprobaciones aprobadas en host local** con `RiftJournalProbe.Run`: además de las regresiones anteriores, se verificó XP real, puntos por nivel, rechazo de talentos inválidos, mejoras de daño, guardado de XP/rangos/equipo, tres blancos frontales y exclusión del trasero para Barrido, 16 eventos de Torbellino (4 impactos × 4 blancos), mitigación de Última resistencia, desplazamiento de Salto, reinicio de talentos y vuelta al hacha.

`RiftJournalProbe.RunGear` verifica por separado la orientación final del escudo y el ocultamiento del hacha. Las capturas están en `Documentation/quest-system/progression-talents.png` y `gear-equipment-final.png`. Los enemigos adicionales, la XP de prueba y los guardados de prueba son fixtures aislados; no modifican los personajes reales. No se probó un cliente remoto ni dispositivos móviles.

La hotbar ahora puede reordenarse arrastrando habilidades; la tabla anterior describe el orden inicial. P abre directamente Talentos y los tooltips explican las habilidades. Más detalles en `Documentation/hotbar-tooltips/README.md`.
