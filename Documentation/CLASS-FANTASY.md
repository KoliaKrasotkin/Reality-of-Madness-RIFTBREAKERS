# Fantasía de clases: primera versión jugable

Las clases son independientes de las cinco skins. Todas conservan pistola,
fusiles y demás armas del arsenal. Se eligen en MainMenu > Equipamiento y se
pueden cambiar durante la partida desde Pausa > Clase, únicamente en refugios.
La preferencia se guarda localmente; el servidor valida el identificador,
el lugar de cambio, las habilidades y sus tiempos de recarga.

| Clase | Rol | Vida | Daño base | Armadura | F | Z |
|---|---|---:|---:|---:|---|---|
| Paladín del Alba | Apoyo ofensivo | 130 | x1,15 | 10 % | Luz radiante: cura 35 % a 16 m y daño en 10 m | Juicio: daño, debilitamiento y cura en el objetivo |
| Bastión | Guerrero tanque | 180 | x1,05 | 22 % | Guardia 6 s: mitigación y amenaza | Golpe sísmico: área y debilitamiento |
| Tejedor de Guerra | Mago de batalla | 110 | x1,20 | 4 % | Estallido arcano sobre objetivo visible | Infusión 8 s: daño +35 %, defensa y cura 15 % |
| Acechador del Velo | Cazador furtivo | 120 | x1,15 | 8 % | Marca: daño y vulnerabilidad +20 % compartida | Velo 6 s: evade detección lejana, cura 20 %, siguiente impacto +60 % |

F/Z no sustituyen E (recoger), Q (arma anterior) ni R (recargar). El HUD muestra
los dos cooldowns y los estados de guardia/infusión/ocultamiento. Los efectos
son trazos y anillos temporales; todavía no se añadieron nuevas skins ni
animaciones específicas de lanzamiento. Ocultamiento altera detección de IA;
no vuelve invisible la malla. A menos de 5 m el enemigo puede detectar al cazador.

## Solo y composición

Todas las clases recuperan 8 % de vida máxima por segundo después de siete
segundos sin combatir. Cada una tiene autosustento activo. No se exige sanador
para entrar, resolver un encuentro o equipar un arma.

Guardia del Bastión cura 25 % si no hay paladín vivo a menos de 24 m. Con un
paladín cercano, Guardia puede absorber 25 % del daño que reciban aliados a
18 m. Conserva su armadura y mitigación; el enemigo no aumenta su daño por
la composición del grupo. Las curas del paladín al Bastión tienen +25 %.
La prioridad de amenaza de Guardia facilita atraer la atención de la IA.

El mago potencia su daño con armas y magia; la marca del cazador beneficia
a todos los atacantes del grupo. El paladín mantiene daño propio y puede
curarse aunque no haya aliados. Son valores iniciales para iterar, no un
balance definitivo verificado contra todo el contenido.

## Escalado de encuentros

Se cuentan jugadores humanos vivos presentes a 175 m del encuentro, hasta
cuatro. Las skins, clases y bots no incrementan esa cantidad por sí mismos.

| Jugadores | Vida enemiga | Daño enemigo |
|---:|---:|---:|
| 1 | x1,00 | x1,00 |
| 2 | x1,65 | x1,12 |
| 3 | x2,30 | x1,24 |
| 4 | x2,95 | x1,36 |

Al incorporarse alguien se conserva el porcentaje de vida del enemigo. El
encuentro recuerda participantes durante el combate y no baja de nivel por
muertes o retiradas individuales. Si la patrulla se descarga completamente
porque todos se alejaron, se reinicia esa lista de participantes. Las zonas
seguras, muerte por vacío y checkpoints mantienen sus reglas.

## Archivos y validación

Definiciones editables: Assets/ROM/Gameplay/Classes/*.asset.
Reglas compartidas: Assets/ROM/Resources/RiftClassRules.asset.
Registro de pruebas: Documentation/classes/validation.txt.

Las pruebas usan una partida real local y aliados sintéticos en el servidor
para comprobar sinergias y escalado de 1 a 4. No equivalen a una sesión con
cuatro clientes conectados ni a completar manualmente todo el mapa con cada
clase. El problema anterior de ragdolls de algunas skins sigue pendiente.

Actualización de acceso: las cuatro clases también se eligen directamente en
MainMenu > Jugar, en ELEGÍ TU CLASE. El refugio inicial tiene un muñeco pasivo
y una excepción de entrenamiento a 30 m que permite usar F/Z y armas contra
él. La barra de habilidades tiene Canvas independiente. Ver
Documentation/TRAINING-AND-CLASS-ACCESS.md.
