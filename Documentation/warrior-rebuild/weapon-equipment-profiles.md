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
