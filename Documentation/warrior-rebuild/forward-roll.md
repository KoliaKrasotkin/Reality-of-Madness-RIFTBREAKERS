# Roll hacia adelante

CTRL izquierdo o derecho ejecuta un roll en la dirección hacia la que mira el
Warrior, tanto armado como con el hacha guardada. La tecla usa RiftControls
y aparece en Ajustes > Movimiento como «Rodar hacia adelante».

Armado usa 2Hand-Sword-DiveRoll-Forward1 en RollForward. Desarmado usa
Unarmed-DiveRoll-Forward1 en UnarmedRollForward. Ambos clips se reproducen a
velocidad natural (RollPlaybackSpeed = 1). Rolling controla la salida y
RollArmed conserva la locomoción a la que debe volver. Si termina fuera del
suelo, pasa a la caída correspondiente. RollExitNormalizedTime = 0,85 permite
salir antes del tramo final de recuperación, conservando la velocidad natural.
La transición de salida dura 0,04 segundos y no agrega otro bloqueo a un nuevo
roll una vez cumplida la pausa configurada.

El CharacterController conserva las colisiones y la gravedad. El avance tiene
una aceleración breve, un impulso central y una frenada antes de levantarse.
Se integra la distancia de esa curva por tick para mantener el recorrido.

RollTravelMultiplier = 1,2 calcula la distancia según SprintSpeed y la duración
del clip: distancia = SprintSpeed × RollTravelMultiplier × (duración + pausa).
Así, varios rolls separados por la pausa mínima superan correr aproximadamente
un 20 % en llano, aun sin avanzar durante esa pausa. El avance normal entre
rolls puede aumentar esa ventaja. Cada clip conserva su duración propia.

RollRecovery = 0,05 segundos (ajuste actual del prefab) es la pausa mínima después de cada roll. Requiere
una nueva pulsación de CTRL una vez terminada la pausa: no almacena pulsaciones
anticipadas ni repite rolls por mantener la tecla apretada.

RollPlaybackSpeed, RollExitNormalizedTime, RollTravelMultiplier y RollRecovery se pueden ajustar en
WarriorAvatar del prefab. La distancia es calculada; la rapidez del movimiento
se ajusta con RollTravelMultiplier, sin acelerar el gesto de la animación.

Solo comienza vivo, en el suelo y en locomoción, sin una habilidad en curso ni
una transición de guardar/sacar. Durante el roll se bloquean nuevos ataques,
saltos y giros del personaje; la cámara sigue disponible. Los portales aplican
el último estado de arma solicitado cuando termina el roll.

La lógica está en WarriorAvatar.Roll.cs, parte de la misma clase WarriorAvatar.
No requiere otro componente. RiftMMOController llama a RequestRoll() desde la
entrada normal, que respeta chat, menús y reasignación de teclas. En red, el
servidor valida el inicio y sincroniza tiempo, orientación y estado de arma;
el dueño mueve el CharacterController por el recorrido existente.

ROM > Animation Rigging > Configure Warrior Forward Roll reconstruye sus estados
y asigna los clips al prefab, conservando los ajustes de movimiento.
Validate Warrior Forward Roll comprueba ambos clips, sujeción en 61 muestras,
velocidad media de cinco rolls con pausa, colisiones, restricciones y API offline.
También verifica que una pulsación temprana no se guarde y que la mezcla de
salida del Animator no bloquee otra pulsación después de RollRecovery.