> Actualización: la locomoción aprobada se extendió a los cinco personajes y se combinó con las armas antiguas. El estado actual está en SHARED-CHARACTER-ANIMATIONS.md. Lo que sigue documenta la etapa anterior.

# Animaciones exclusivas de Lizardman (slot de Dinork)

Se importaron completos los siete paquetes de Descargas: Mobility, Pistol, Rifle,
Deaths, Scared, Zombie y Samurai. Se conservaron sus rutas y GUID. Los archivos
originales de Descargas no se modificaron. Los modelos de demostración no se
agregaron al selector de personajes.

El prefab `Assets/ROM/UI/FrontEnd/Models/ids-character-1.prefab` contiene el set
exclusivo `CharacterAnimationSet`: 359 clips Humanoid de las carpetas de animación.
Se excluyen tomas de prueba de los modelos de demostración. Samurai se convirtió
de Generic a Humanoid usando su propio Skeleton y un mapeo de huesos/falanges.
Los 366 clips auditados, incluyendo tomas de modelos, se importaron como Humanoid.

## Aplicado al juego

- Idle nuevo en el menú y en partida.
- Locomoción frontal, diagonales delanteras, carrera, giro, pose aérea y avance
  agachado de Mobility. Pistola y rifle seleccionan las variantes W1 y W2.
- Al conservarse los 4.2 m/s de desplazamiento normal, se usa el jog del paquete;
  el walk de 1.19 m/s permanece en la biblioteca para un futuro modo de paso lento.
- La cadencia del set exclusivo sigue la velocidad real del CharacterMotor.
- Las poses de arma se superponen conservando las transiciones de equipar,
  disparar y recargar del controlador existente. No cambia el input, el movimiento
  autoritativo de Mirror ni los tiempos de uso de las armas.
- Solo el slot 1 tiene el componente y las referencias a esta biblioteca. Cambiar
  de arma reconstruye sus overrides; los otros personajes conservan sus clips.
- El constructor del frontend vuelve a aplicar el set después de regenerar el
  modelo, para no perderlo en una reconstrucción del menú.

## Biblioteca preparada, sin nuevas mecánicas

Deaths, Scared, Zombie y Samurai están importados y referenciados exclusivamente
por este personaje. Sus ataques, reacciones, muertes y poses no se disparan solos
ni sustituyen el movimiento normal. Quedan disponibles para conectar los estados
correspondientes cuando existan esas mecánicas. Espadas, impactos, daño cuerpo a
cuerpo y equipamiento de espada todavía no se implementan.

Los Starter no contienen strafe ni retroceso de locomoción de tirador. Esas
cinco direcciones mantienen los clips previos. Los diagonales delanteros usan el
nuevo avance; falta un paquete direccional completo para sustituir cada ángulo.
La carrera usa jog acelerado porque estos Starter no incluyen sprint dedicado.
Las animaciones FPP de manos y las recargas del kit permanecen como estaban.

## Verificación

Unity 6000.3.23f1 compiló los cambios. Prueba real de host en El Rift, sobre una
plataforma temporal no guardada: pistola en movimiento y sprint, pistola agachado,
rifle en movimiento y sprint, y cambio a Purplehead como control. El probe exige
que el clip nuevo tenga más de 50% de peso para Lizardman y que Purplehead no
herede su componente. Se verifican velocidad y orientación del rig fuente.

Resultado: `Documentation/armed-motion-lizardman-after/validation.txt` (PASS).
Asignaciones: `Documentation/lizardman-animation-bindings.txt`.
Inventario: `Documentation/imported-animation-audit.txt`.
Capturas y GIF: `Documentation/lizardman-0-walk.gif`, `lizardman-1-run.gif`,
`lizardman-3-run.gif`. La captura agachado es intencional y solo corresponde a C.

No se probó una sesión multicliente, cada arma del catálogo ni cada acción de
combate en gameplay. La importación no equivale a tener esas mecánicas activas.
