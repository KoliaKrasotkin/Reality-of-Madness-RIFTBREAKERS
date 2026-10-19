# Animaciones compartidas de personajes

El set aprobado en Lizardman se aplica a Black Panther, Lizardman (slot de
Dinork), Purplehead, Redhead y White Knight. Cada uno mantiene su propio modelo,
esqueleto y Avatar Humanoid. No se incorporan personajes de demostración.

## Combinación final

- Tres controladores de cuerpo: Mobility, Pistol y Rifle, en
  `Assets/ROM/Animation/SharedCharacters`.
- Solo se reemplazan los movimientos de la capa `body`. Los clips originales que
  también utilizaba la capa del arma ya no comparten la misma sustitución.
- Pistola y rifle mantienen las poses y disparos de los paquetes nuevos.
- Se recuperan equipar, recargar y atacar con las animaciones antiguas disponibles
  de cada arma. Se generan overrides específicos para pistola, fusil, ametralladora,
  lanzacohetes, escopeta, cuchillo, granada y bomba.
- Rifle V2 y francotirador usan el controlador de fusil que ya tenían asignado en
  el kit. No existe un set TPP propio de francotirador entre esos archivos.
- Escopeta conserva su pose original y su recarga de manos/FPP. Como el kit solo
  trae idle TPP para ella, la recarga TPP usa el clip humanoide antiguo de fusil.
- El movimiento lateral y hacia atrás sigue usando los clips previos: los paquetes
  Starter no contienen esas direcciones. El avance normal usa jog porque se
  mantienen los 4.2 m/s del motor, y sprint acelera ese ciclo como en Lizardman.

La posición de las piernas procede de las animaciones, sin las correcciones
procedurales anteriores. También se usa el idle nuevo en las previsualizaciones
3D del menú. La biblioteca importada queda referenciada en los cinco perfiles para
incorporar posteriormente acciones de Samurai, Zombie, Scared y Deaths.

## Recargas y eventos

`CharacterAnimator` envía tanto las órdenes PlayAnimation como los triggers de
arma al Animator del modelo visible. `IDSGameplayPose` conserva esas órdenes hasta
que el controlador correcto está instalado. Esto evita perder una recarga o un
trigger consumido en el rig invisible antes de llegar al modelo nativo.

La escopeta inicia su animación TPP al recargar. Cancelar una recarga limpia el
indicador IsReloading del dueño para que no quede activo al cambiar de arma.
El sistema de munición, las duraciones y la autoridad de Mirror se conservan.
No se añaden armas al inventario inicial ni se implementan espadas en esta etapa.

## Reconstrucción

El constructor del frontend termina en LizardmanIntegration, que ahora aplica
SharedCharacterAnimationBuilder a los cinco prefabs. Los controladores compartidos
y los overrides existentes conservan sus GUID al reconstruirse. Los archivos FBX
originales de animaciones no se modifican.

## Validación

Prueba real de host en El Rift, con cinco personajes y once situaciones por
personaje: las diez variantes de arma del kit y pistola agachado. Se comprueba que
el clip frontal nuevo tenga más de 50% de peso y que cada Avatar sea Humanoid válido.

Para armas Gun se inicia la recarga a través de PushReload y se exige que el
Animator visible entre en reload usando un clip de recarga. Después se comprueba
el evento visual recoil desde CharacterAnimator. Esta última prueba valida la
animación de disparo, no proyectiles, daño ni precisión.

Las capturas de movimiento, recarga y disparo están en
`Documentation/shared-character-animations`. Resultado y asignaciones en
`validation.txt` y `Documentation/shared-character-animation-build.txt`.
La plataforma y las armas de prueba se crean temporalmente en Play Mode; no se
guardan en el mapa ni en el equipamiento inicial.

No se verificaron clientes remotos, cada inclinación del suelo ni completar todas
las recargas hasta su último cartucho. Las posturas de dedos y agarres pueden
necesitar ajustes específicos de cada modelo de arma.
