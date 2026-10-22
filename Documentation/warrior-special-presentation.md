# Warrior: iconos y animaciones especiales

Primera pasada de presentación original sobre el rig Humanoid nativo. No modifica daño, coste, cooldown, duración, hit time ni buff duration. Los tres iconos se restauraron a las referencias exactas anteriores por pedido del usuario.

- Salto de guerra: anticipación breve, elevación y recogida de piernas del esqueleto y compresión de aterrizaje. El actor sigue usando el único desplazamiento autoritativo existente al HitTime. La elevación del mesh es visual; no es una trayectoria física nueva.
- Torbellino: cuatro vueltas visuales del esqueleto en torno al eje vertical del mundo, con brazos/arma extendidos y recuperación final. Conserva los cuatro impactos existentes y el factor de movimiento del juego.
- Última resistencia: guardia defensiva con brazos y torso, mantenida después del cast mientras dura DefensiveUntil. Cede al siguiente ataque; no bloquea locomoción ni añade mitigación propia.

WarriorAvatar.PresentAttack se ejecuta desde el RPC de presentación existente. WarriorSpecialMotion restaura los offsets antes de la siguiente evaluación del Animator, cancela ante interrupción/muerte y retira offsets al desactivarse. No transforma el root de red, no activa root motion ni toca el CharacterController. El servidor conserva todos los cálculos de combate. Otros personajes conservan la animación previa.

Los iconos nuevos de 256px se conservan localmente como borradores sin referencias en Characters/Warrior/AbilityIcons. Las habilidades conservan los tres iconos anteriores, que reutilizaban arte de otras habilidades. WarriorSpecialPresentationBuilder solo puede regenerar los borradores y no los asigna; WarriorProgressionBuilder no llama a ese generador. No se borraron los borradores ni se cambió otra interfaz.

## Validación y límites

`ROM.EditorTools.RiftBagReview.Run -riftWarriorReview -riftReviewOutput <directorio>` inicia un host aislado y captura poses reales con una luz y cámara de prueba. Comprueba reproducción por la API real, desplazamiento único, giro vertical, interrupción, prioridad del ataque normal, muerte/reaparición y root motion desactivado. Las capturas pertenecen a ese fixture dentro del mapa real.

Esta es animación procedural de personaje, no clips cinematográficos nuevos ni solo partículas. Mantiene los VFX anteriores y sincroniza su ciclo con los parámetros de la habilidad; hace falta evaluación artística manual en más ángulos, armas y tasas de cuadros. No se ha validado cliente remoto separado, latencia, build standalone ni móvil.

`WarriorSpecialPresentationBuilder.RunRuntimeAudit` verifica referencias de los iconos desde las escenas habilitadas, el shader de quests en Resources y componentes fuera de Editor. No sustituye ejecutar un build.
