# Acceso a clases y entrenamiento

En MainMenu > Jugar hay un panel ELEGÍ TU CLASE con las cuatro opciones.
La selección se guarda y se usa al entrar en El Rift. Equipamiento conserva
el selector detallado, y Pausa > Clase permite cambiar dentro del refugio.

El inicio contiene un muñeco pasivo dentro de un círculo dorado, sobre suelo
transitable y con línea de tiro desde un spawn. No ataca, no muere ni concede
oro o progreso. Su rótulo muestra el último daño y el total de la serie;
la serie se reinicia tras diez segundos sin impactos.

A menos de 30 m del muñeco se pueden usar F y Z aunque el jugador esté en zona
segura. La excepción de daño solo permite atacar al muñeco: no habilita daño
entre jugadores ni contra enemigos normales desde el refugio. Se mantienen
los cooldowns normales y las reglas de muerte por caída.

La barra de habilidades usa un Canvas propio, independiente del HUD de horda.
Muestra clase, teclas F/Z, cooldowns y aviso cuando el refugio bloquea habilidades.
Se oculta durante la pausa y fuera de la partida.

Configuración de escena: componente Horde.TrainingSpot en El Rift.
Instalador: ROM.EditorTools.TrainingGroundBuilder.Run.
Pruebas y capturas: Documentation/training/.
