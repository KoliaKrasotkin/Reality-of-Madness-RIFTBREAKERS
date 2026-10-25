# Warrior weapon definitions

`WeaponDefinition` is the single source for weapon gameplay data, model, grip, stored pose and Animator variant. There is no separate `WeaponPresentationProfile` system.

## Configured assets

All definitions and original models live under `Assets/ROM/Characters/Warrior`:

- `WarriorAxe.asset`: existing axe definition, stats and model; uses the built-in axe and its calibrated two-hand rig.
- `WarriorSword.asset`: existing sword definition and `WarriorSword.prefab`; uses the DoubleL Animator Override Controller. Its `AdditionalEquipment` references the shield definition.
- `WarriorShield.asset`: companion definition using the original `WarriorShield.prefab`, attached to the left hand.

The original sword multiplier (0.85), axe multiplier (1), model scale, class restrictions and existing definition GUIDs are preserved. `AbilityController` now reads the sword damage multiplier from its definition instead of repeating a numeric constant. `WarriorProgression` references the sword; its inventory shield model is derived from the sword's companion definition rather than stored again.

## Runtime components

`CharacterWeaponPresentation.Equip(WeaponDefinition)` configures the Animator variant and each equipped model. It instantiates the main weapon and its `AdditionalEquipment`, except definitions marked `UsesBuiltInWeapon`. The list is flat: companion items do not recursively equip their companions. `ResolveClip(source)` supplies the equipped clip for action timing.

`RiftWarriorEquipment` is the Warrior adapter for the existing replicated `SwordAndShield` selection. `WarriorPlayer.prefab` references the original axe and sword definitions directly. It waits until rolls and Animator transitions finish before changing equipment. Combat ownership, saved builds and networking keep their existing behavior.

## Animation and grips

Sword/shield mode replaces locomotion, jump and attack clips with DoubleL Humanoid animations. The nine attack states reuse the six available sword attacks and a shield block pose. Unarmed, roll, fall and landing clips keep their existing fallback animations.

The presentation layer gives the axe's shared grip solver and arm IK constraints zero weight in sword/shield mode. Sword and shield independently follow their own animated palms. `UseGripFrame` interprets Position and Rotation in an anatomical palm frame, avoiding differences between humanoid wrist axes. Otherwise they use the bone's rotation. The stored pose uses an anatomical torso frame.

`EquipmentBackWeight` blends each item between its hand and its stored position during the existing draw/sheath states. Models remain on the back while unarmed. Death hides all equipped models.

## Adding another weapon

1. Create a `ROM > Combat > Weapon` asset and assign its model and gameplay properties.
2. Assign an Animator variant based on that character's state machine.
3. Configure Socket, Position, Rotation, Scale, UseGripFrame and the stored pose. Add companion definitions in AdditionalEquipment if needed.
4. Select that definition through the character's combat adapter on each client using replicated equipment state.

Paladin, Battle Mage and Rogue can reuse these definitions and the presenter when implemented. Their character adapters and Animator states remain character-specific. The Warrior currently retains its two existing gameplay loadout choices.

## Editor tools

Use `ROM > Animation Rigging > Configure Warrior Equipment` to rebuild animation mappings and prefab references. Artist-adjusted grip values are preserved after initial calibration. `Validate Warrior Equipment` checks both modes, all attacks, hand attachments, holster/draw, runtime Animator parameters, inventory references, combat multipliers and Mirror identity. `Preview Warrior Sword and Shield` renders inspection images in this directory.

See `equipment-validation.txt` for results. Full multiplayer gameplay remains a separate manual check.

## Parámetros de Weapon Definition

| Parámetro | Efecto actual |
|---|---|
| Role | Weapon o Shield. Shield oculta los campos ofensivos y mantiene daño, velocidad y alcance en cero. No agrega defensa por sí mismo. |
| Id | Identificador técnico del equipo; también se usa para nombrar sus instancias. |
| Display Name | Nombre que se muestra en interfaces que consultan la definición. |
| Weapon Type | Etiqueta descriptiva, por ejemplo OneHandSword. Actualmente no selecciona animaciones ni lógica automáticamente. |
| Damage Multiplier | Multiplica el daño del arma principal: 1 conserva el daño; 0.85 aplica un 85 %. No corresponde al escudo. |
| Attack Speed | Campo reservado: actualmente no modifica la velocidad del Warrior. El combate usa las duraciones de habilidades y el intervalo del autoataque. |
| Reach | Campo reservado: actualmente no modifica el alcance del Warrior. El combate consulta las habilidades y AutoAttackProfile. |
| Model | Prefab visual que se instancia para ese equipo. |
| Socket | Hueso humanoide de referencia: Right Hand para espada, Left Hand para escudo. |
| Position | Desplazamiento visual respecto del agarre, en metros. Con Use Grip Frame, Z apunta desde la muñeca hacia los dedos. |
| Rotation | Orientación adicional del modelo, en grados. Rota el objeto, sin cambiar la animación del brazo. |
| Scale | Tamaño del modelo. Valores menores lo achican; valores diferentes en X/Y/Z lo deforman. |
| Allowed Classes | Lista prevista de clases compatibles. Actualmente no se valida al seleccionar el equipo del Warrior. |
| Allowed Specializations | Lista prevista de especializaciones compatibles. Actualmente no se valida en esta selección. |
| Attack | Clip alternativo reservado. El Warrior usa el Animator, su Controller y las animaciones de las habilidades. |
| Impact Vfx | Efecto de impacto reservado en esta definición. El Warrior usa los efectos definidos en las habilidades. |
| Controller | Animator Controller u Override Controller del conjunto principal. Cambia las animaciones, manteniendo la máquina de estados compatible. |
| Uses Built In Weapon | Usa el arma ya integrada en el prefab y su rig, como el hacha. En este modo no instancia Model ni Additional Equipment; los offsets de esta definición no mueven el hacha existente. |
| Use Grip Frame | Interpreta Position y Rotation en un marco calculado desde la palma. Desactivado usa los ejes del hueso. Cambiarlo reinterpreta los mismos números y puede cambiar mucho la pose. |
| Stored Bone | Hueso que sirve de ancla para el equipo guardado; actualmente Chest. |
| Stored Position | Desplazamiento del objeto guardado respecto de ese hueso, con ejes anatómicos del torso. No modifica el agarre en la mano. |
| Stored Rotation | Rotación del objeto cuando está guardado. |
| Additional Equipment | Definiciones acompañantes, como el escudo de la espada. Cada una aporta su propio modelo, hueso y offsets. No se recorren acompañantes de forma recursiva; el Controller del conjunto principal controla las animaciones. |

La espada se ajustó de `Position Z = 0.055` a `0.03`: 2,5 cm más cerca de la muñeca, manteniendo sus demás ajustes. Para retocarla, seleccionar `Assets/ROM/Characters/Warrior/WarriorSword.asset` y modificar Position en pasos de 0.005 o 0.01. Bajar Z acerca el modelo a la muñeca; subirlo lo desplaza hacia los dedos. El ajuste es del asset, no de la instancia temporal creada durante Play Mode.

## Preview de distintas armas desde Weapon IK Controller

1. Abrir `WarriorPlayer.prefab` en Prefab Mode. Si está seleccionado desde Project, usar `Open Prefab for Preview` en el componente.
2. En `Weapon IK Controller > Animation Preview`, elegir `Equipment`: Axe, Sword and shield, o Custom definition. El campo Weapon Definition acepta cualquier definición preparada con su Controller.
3. Elegir la animación y pulsar Start Preview. La espada usa automáticamente sus clips de DoubleL; las animaciones Unarmed muestran el equipo en la espalda.
4. Mover Preview Time para revisar el agarre en distintos momentos. Es posible cambiar de equipo o animación con el preview activo.
5. En `Equipment offsets (definition assets)`, ajustar Position, Rotation y Scale de cada pieza. Stored Position y Stored Rotation se revisan con Unarmed/Idle o las animaciones de guardar y sacar equipo.
6. Los controles de offsets editan y guardan los assets WeaponDefinition, con soporte de Undo. Se actualizan en vivo; no es necesario seleccionar otro objeto ni modificar los modelos temporales.
7. Stop Preview elimina los modelos temporales y restaura el Animator, la visibilidad del hacha, el rig y la pose del personaje. Los ajustes de las definiciones se conservan. También se limpia al cerrar el Inspector, recompilar o entrar en Play Mode.

`Validate Axe Animation Grips` conserva la validación del rig de dos manos del hacha. Para probar el ciclo completo del nuevo Inspector está disponible `ROM > Animation Rigging > Validate Equipment Inspector Preview`.

## Idle de espada apoyada en la hombrera

El idle de espada usa una copia Humanoid de DoubleL con la pose del brazo derecho ajustada al Lizardman. El mango se calibra en la palma y la hoja se apoya en el extremo exterior de la geometría deformada de `RightShoulderArmor`, usando el grosor del modelo de espada para la separación. La referencia no es el hueso del hombro.

Las curvas de dedos de la mano derecha se cierran sobre el mango en el idle y en los clips armados de DoubleL usados por este conjunto. Las animaciones originales del pack permanecen intactas; las copias están en `Assets/ROM/Animation/WarriorEquipment/SwordGrip_*.anim`, asignadas mediante el Override Controller. Unarmed conserva sus poses originales.

Se puede revisar en `Weapon IK Controller > Equipment: Sword and shield > Animation: Idle`. Los offsets del WeaponDefinition siguen siendo editables. Cambiarlos modifica el contacto calibrado; para volver a calcularlo con este personaje y modelo, usar `ROM > Animation Rigging > Calibrate Warrior Sword Shoulder Idle`. La calibración es específica del Warrior Lizardman y no debe reutilizarse sin adaptar otro cuerpo.

`Validate Warrior Sword Shoulder Idle` mide 61 muestras del clip final: mango respecto de la palma, apoyo respecto del mesh exterior y separación de la geometría de la hombrera con el plano de la hoja. También comprueba las curvas de cierre de dedos. Los resultados quedan en `sword-shoulder-validation.txt`; las vistas cercanas están en `sword-idle-grip-front.png` y `sword-idle-grip-side.png`.

### IK compartido por arma y animacion

En `WarriorPlayer > Weapon IK Controller`, seleccionar Equipment, Animation y Start Preview. La seccion **Hand IK — selected animation** edita los assets WeaponDefinition, con Undo y guardado inmediato.

- **Enable Hand IK** activa la correccion de brazos del item.
- **Hand Grips** define cada muneca en el espacio local del modelo: Hand, Position y Rotation. Es el agarre comun a todas sus animaciones. Espada y escudo tienen un agarre cada uno; el hacha tiene ambos.
- **Animation IK** guarda correcciones por referencia al clip. El inspector muestra la entrada del clip seleccionado; Add IK settings crea una si falta. Si dos estados usan el mismo clip, comparten este ajuste.
- **Weapon Position / Rotation** desplaza el item y sus agarres respecto de la pose animada, usando los ejes del cuerpo. La posicion y rotacion originales de la definicion siguen siendo la base. Un ajuste de idle no cambia un ataque con otro clip.
- **Hand Position / Rotation / Elbow Offset** corrige la mano derecha y su codo. Los campos **Left Hand Position / Rotation / Left Elbow Offset** corrigen la izquierda. Posiciones de manos en espacio del modelo; codos en espacio del cuerpo. Para el escudo usar los campos Left.
- **Weight** mezcla el IK con las manos originales: 0 conserva la animacion y 1 intenta alcanzar completamente los agarres. El alcance sigue limitado por la longitud de los brazos.

La evaluacion toma primero la pose animada, coloca todos los items y despues resuelve ambos brazos con el mismo solver de dos huesos. Las armas no vuelven a colocarse usando las manos resueltas. Antes de evaluar el siguiente frame se restauran las rotaciones base para evitar realimentacion, incluso con clips que no animan todos los huesos. El preview usa exactamente este flujo. El hacha mantiene su rig de stream existente como pose de entrada y agrega esta misma capa de ajustes.

El Animator selecciona y mezcla ajustes de los clips actual y siguiente durante transiciones. Las animaciones desarmadas y de sacar/guardar conservan las manos originales; EquipmentBackWeight desvanece el IK al guardar el arma. La pose de dedos continua viniendo de los clips/rig existentes.

Calibracion inicial: se conserva el montaje de espada elegido por el usuario (Position 0, .02, .116447635; Rotation 12.5, 83.1, 168.2). El agarre se calibra usando la referencia de palma del ajuste anterior; las manos acompañan el nuevo montaje. El escudo se desplaza .025m hacia afuera en sus perfiles. Configure Equipment Hand IK solo inicializa items sin agarres; no sobrescribe calibraciones posteriores.

Validacion: ROM > Animation Rigging > Validate Equipment Hand IK. Reporte: `equipment-hand-ik-validation.txt`. Para consumidores externos que evalúen manualmente un PlayableGraph, llamar `BeginPoseEvaluation()` antes de evaluar y `RefreshPose(storedWeight, optionalPreviewClip)` despues.

### Calibracion completa del hacha

Menu `ROM > Animation Rigging > Calibrate All Warrior Axe Animations`: recalibra unicamente WarriorAxe. Mide 61 poses de cada clip armado sobre el rig de stream original y ajusta orientacion de munecas, contacto de palma, separacion de manos y una ligera apertura de codos. Los contactos quedan sobre el eje del mango. Los perfiles desarmados, de muerte y de guardar/sacar tienen Weight 0 para conservar su movimiento original.

`Keep Grips Reachable` traslada lo minimo necesario el arma completa para que ambas munecas puedan alcanzar los contactos, conservando su orientacion y la distancia entre manos. `Hand Grips > Preserve Palm Contact` y `Palm Offset` permiten mezclar los perfiles de muneca usando rotaciones suaves y conservar el contacto de las palmas durante transiciones. Estos ajustes se activan en el hacha; espada y escudo conservan sus ajustes previos.

`EquipmentBackWeight` tambien se anima en los cuatro clips originales de presentacion del hacha. Asi el IK compartido y el rig de stream usan el mismo estado al pasar de mano a espalda. El generador de Weapon States conserva esta sincronizacion al ejecutarse nuevamente.

Validacion: `ROM > Animation Rigging > Validate All Warrior Axe Animations`. Se comprueban 1.769 muestras de 29 clips (18 armados), contacto de palmas y munecas, rotacion, estabilidad al repetir el preview y 1.002 frames del Animator con ataques, movimiento, salto/caida/aterrizaje, roll y guardar/sacar. El reporte esta en `axe-equipment-ik-validation.txt`. Las hojas `axe-ik-poses-0.png`, `axe-ik-poses-1.png` y `axe-ik-poses-2.png` muestran poses al 20%, 50% y 80%; el orden esta en `axe-ik-poses-index.txt`. Las imagenes usan mallas horneadas desde los huesos de cada muestra para evitar buffers de skinning desactualizados en el render de preview.

El boton Validate Axe Animation Grips del inspector ahora mide los targets finales del sistema compartido, en lugar de comparar contra los targets anteriores al ajuste por equipo.

### Ajustes basados en las referencias manuales (2026-10-24)

`Fit Remaining Axe Animations From Approved Poses` toma como plantillas los valores actuales de Idle, Run-Forward, Run-Backward, Run-Left, Run-Right y Attack1. Conserva sus 54 campos manuales de arma, munecas y codos, junto con HandGrips. Ajusta Attack2–Attack9, Jump, Fall, Land y DiveRoll-Forward1 usando esas formas reales de agarre del mesh. La palma virtual del rig original no se usa como criterio visual de centrado: el artista desplazo deliberadamente las manos para envolver el mango.

Los perfiles nuevos incluyen **Pose Keys**: correcciones suaves durante el clip, con giro y deslizamiento del agarre sobre el mango, posicion/rotacion adicional del arma y posiciones de codos. El giro transforma conjuntamente mano y punto de agarre alrededor del eje local Z; conserva la forma de contacto de las plantillas en lugar de doblar solamente la muneca. El preview evalua el mismo tiempo normalizado que el Animator. Los perfiles de referencia no reciben Pose Keys.

**Right Thumb Rotation / Left Thumb Rotation** modifica exclusivamente la articulacion inicial del pulgar. Se comprueban vertices del mesh influenciados por sus huesos contra el radio del mango. Tambien se corrigen pulgares en las seis referencias si atraviesan el mango, manteniendo intactos los valores manuales originales. La pose base del dedo se restaura antes de evaluar cada frame para no acumular giros.

**Use Approved Carry Grip / Carry Grip Position / Carry Grip Rotation** conserva el agarre aprobado de la mano derecha durante Sheath y Unsheath. El binder del rig original deriva su contacto y orientacion desde estos valores. La izquierda se libera para permitir el movimiento hacia la espalda, y los dos brazos vuelven al IK de combate al terminar de sacar el arma.

Validacion actual: `Validate Axe Against Approved Grips`. El informe `axe-reference-fit.txt` incluye errores de muneca, angulos de muneca/codo, comprobaciones de pulgares y pruebas con el Animator. Se validan 61 puntos por clip y transiciones reales del controlador, incluidos los frames de transporte con una mano y el guardado en la espalda. Los menus anteriores de calibracion/validacion del hacha delegan a este flujo cuando ya existe un agarre de transporte aprobado, para proteger las seis referencias manuales.

Imagenes de revision: `axe-reference-hands-0.png`, `axe-reference-hands-1.png`, `axe-reference-hands-2.png` (20%, 50%, 80%). Orden en `axe-reference-hands-index.txt`. `axe-approved-grips.json.txt` guarda las referencias de esta calibracion. Los informes anteriores de centrado de palma en el eje corresponden a la calibracion matematica previa, antes de los ajustes visuales del artista.

### Espada y escudo: calibracion completa (2026-10-25)

`ROM > Animation Rigging > Calibrate Warrior Sword and Shield Grips` usa los perfiles actuales de Idle como referencias independientes. Conserva el montaje de espada, su contacto sobre el hombro y los valores manuales de los dos perfiles de idle. Ajusta los otros clips de locomocion, nueve estados de ataque, salto/caida/aterrizaje y roll mediante Pose Keys. Los estados que comparten el mismo clip comparten su calibracion. Cada item usa su propia mano y su propia correccion de alcance.

El escudo original solo incluia tabla, aro y pieza frontal. Se agregaron Back grip y dos Grip bracket como hijos visuales de WarriorShield.prefab, alineados con el puno izquierdo y separados de la cara posterior para alojar la mano. No agregan colliders. La espada sigue usando el modelo original y la misma posicion/rotacion/escala.

`Finger Poses` en cada WeaponDefinition guarda la pose local de sus quince huesos de dedos. La derecha conserva el cierre aprobado del idle; la izquierda usa el cierre del agarre de referencia. Se mezcla con la animacion y se restaura antes del frame siguiente. Puede editarse en el inspector de Weapon IK Controller, junto a Hand Grips. Los pulgares tienen ademas sus correcciones independientes por clip. El hacha mantiene Finger Poses vacio y conserva su rig de dedos existente.

Los parametros Carry Grip tambien funcionan ahora para items independientes: durante guardar/sacar, el modelo sigue la forma aprobada de la mano animada antes de pasar al socket de espalda. Esto evita recuperar el antiguo montaje sin IK durante el gesto. Las posiciones de guardado elegidas por el usuario se conservan.

Validacion: `ROM > Animation Rigging > Validate Warrior Sword and Shield Grips`. Informe `sword-shield-grip-fit.txt`: 1.586 muestras de 26 clips distintos, estabilidad al repetir preview, limites de alcance, nueve estados de ataque y 1.146 frames del Animator con cambios de direccion, aire, roll, guardado y extraccion. `sword-shield-thumb-fit.txt` contiene las comprobaciones de vertices del pulgar contra las empunaduras en cinco fases del clip. Las imagenes `sword-shield-grips-front.png` y `sword-shield-grips-back.png` muestran cada clip armado; el orden esta en `sword-shield-grips-index.txt`. Los detalles de idle estan en `sword-grip-detail.png` y `shield-grip-detail.png`.
