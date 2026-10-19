# Locomoción armada erguida

## Resultado actual

Se retiró la transferencia manual de HumanPose y la solución manual de piernas entre avatares. Cada personaje usa su Animator, avatar y proporciones originales. Se sincronizan controlador, parámetros y pesos de capas con el rig del kit, que conserva las hitboxes y la lógica existente.

Se crearon once variantes de locomoción dentro de Assets/ROM/Animation/Upright. Se mantienen los FBX originales. Las variantes de caminar elevan la cadera, reducen la flexión permanente de rodillas y limitan abducción y torsión de piernas y columna. Las variantes de carrera mantienen una postura más alta, una inclinación moderada del torso y menor flexión extrema durante la recuperación de la pierna. La rama de agacharse conserva sus clips originales; no se activa por caminar o correr.

La capa del arma enmascara brazos, dedos y cabeza; no sobrescribe la pelvis. Durante la carrera armada mantiene peso 0,8 para evitar pasar al braceo sin arma. Las restricciones existentes de pecho/cabeza reciben rotaciones explícitas en espacio global.

La velocidad que controla la cadencia procede del CharacterController y está limitada por la velocidad horizontal integrada. Las correcciones de Transform no añaden pasos. No se cambió la recepción de posiciones de red.

El modelo visual del arma sigue la mano del avatar nativo, con restauración de su pose local al cambiar de personaje, perspectiva o arma. No se mueve el objeto raíz de red para esta corrección visual. Primera persona conserva su presentación anterior.

## Verificación realizada

- Unity compiló y la partida local cargó El Rift con el arma inicial real.
- Purplehead caminó y corrió con pistola a 0, 90, 180 y 270 grados.
- Black Panther, Dinork, Redhead y White Knight caminaron y corrieron con pistola a 90 grados.
- Se revisaron 192 fotogramas del conjunto final. Las velocidades registradas fueron 4,2 m/s y 7,5 m/s, sin los picos de 8,4/15 observados antes.
- armed-motion-after/validation.txt contiene las medidas y condiciones. Se conservan fotogramas clave; los GIF military-walk-after.gif y military-run-after.gif muestran secuencias reales del host. Las hojas PNG muestran varias fases.
- El control de dirección permite oscilación natural del torso; no exige congelar el pecho a cero grados. Los valores finales de pecho y cabeza se miden en los huesos restringidos.

Las primeras medidas de antes observaban el objeto auxiliar de la restricción, no el hueso final. No prueban por sí solas un giro doble del pecho y no deben compararse directamente con los ángulos finales. Las imágenes de antes sí corresponden al comportamiento original reproducido.

## Límites

La revisión visual se hizo con pistola en una plataforma plana temporal que no se guarda en el mapa. Quedan por evaluar otros tipos de arma, apuntado, recarga, pendientes y clientes remotos en una sesión completa. Se conservan los diseños estilizados IDS. Esta pasada no redefine la silueta de las hitboxes por personaje ni presenta las variantes como captura de movimiento militar nueva.

Herramientas: UprightLocomotionBuilder.FromCommandLine crea variantes y ejecuta la revisión; ArmedLocomotionFix.FromCommandLine aplica máscara y apuntado y ejecuta la revisión con los clips ya configurados. Ejecutar con Unity cerrado.
