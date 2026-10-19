> Actualización: el puente manual de HumanPose y el ajuste manual de piernas fueron sustituidos. Consultar ARMED-LOCOMOTION-REVIEW.md para la implementación y las pruebas actuales de caminar/correr con pistola.

# El Rift: movimiento, rigs IDS y acceso a Sun Temple

## Cambios

- CharacterMotor mantiene su interfaz y la autoridad de Mirror; sustituye la integración horizontal por aceleración/frenado vectorial, limita diagonales, añade control aéreo, coyote time y buffer de salto. Salto de 1,2 m; caminar 4,2 m/s, correr 7,5 m/s y agacharse 2,2 m/s.
- Ground probe con límite de pendiente; chequeo real de espacio para levantarse; cápsula con skinWidth 0,04 y stepOffset 0,3. Se conserva la muerte por caída. Noclip de depuración pasa de C a F8.
- Los parámetros de locomoción usan desplazamiento real, también al frenar o chocar, y se actualizan por frame. La cadencia usa locomotionRate sólo en caminar/correr/agacharse; las acciones de armas conservan sus tiempos.
- Los cinco personajes se renderizan con sus rigs nativos IDS y sus bind poses originales. IDSGameplayPose transfiere la pose humanoide del rig de juego tras animación/IK y alinea la cadera. El rig existente conserva hitboxes y anclajes de armas. Se mantiene vivo aunque su renderer esté oculto.
- Dinork ya no necesita mapear cola, lengua, ojos y dedos adicionales al esqueleto del soldado. Primera persona conserva el sistema existente de brazos.
- Los brazos sin arma oscilan con la pierna contraria; con armas se conserva la pose del sistema de combate. La corrección de apertura de piernas sólo actúa en reposo, no recorta cada paso.
- El acceso a Sun Temple mezcla el recorte durante 39 m en vez de formar un corte abrupto. Su material proyecta roca en coordenadas de mundo sobre pendientes para evitar el estiramiento vertical. Se retiraron 20 elementos de vegetación que invadían puentes y se reconstruyó NavMesh.

## Verificación

- Compilación Unity 6000.3.23f1 sin errores C# ni de shader en las ejecuciones finales.
- Prueba aislada del motor con CharacterController y las capas reales: caminar, diagonal, frenado, techo, salto, aterrizaje y caída bajo -38 m. Resultado en locomotion-motor-validation.txt.
- Muestreo de un ciclo de marcha en los cinco rigs: oscilación de pies y alineación de cadera. Capturas locomotion-ids-character-0.png a -4.png; informe locomotion-rig-validation.txt.
- Play Mode: host local real carga El Rift, genera jugador propio y cambia las cinco skins. Cada visual nativo y su puente de pose se comprueban individualmente. Resultado en locomotion-play-validation.txt.
- Conectividad de destinos originales y patio Sun Temple comprobada tras reconstruir NavMesh. Informe sun-access-validation.txt y captura sun-access-after.png.

## Límites para la revisión

Se reutilizan y retargetean los clips existentes; no se ha creado una biblioteca nueva de captura de movimiento. Queda la evaluación humana de toda la marcha, combate y transiciones en pendientes, de la alineación visual de cada arma y de clientes remotos. El puente conserva las hitboxes del rig de juego y no adapta su silueta a cada personaje.

El terreno original de Sun Temple se recuperó aproximadamente de un archivo dañado. Conserva terrazas y algunos desniveles bruscos fuera del acceso. Esta reparación no equivale a rehacer toda la geografía ni a una pasada artística final.

Para revisar: abrir Assets/MultiFPS/Levels/hub.unity, Play y JUGAR EL RIFT. La tecla I del editor alterna la cámara de depuración en tercera persona. Los informes no sustituyen una prueba multicliente ni mediciones de rendimiento.

Herramientas: ROM.EditorTools.LocomotionUpgrade.FromCommandLine (configuración y pruebas aisladas), ROM.EditorTools.LocomotionPlayProbe.FromCommandLine (host local), ROM.Horde.EditorTools.FracturedForestBuilder.RepairSunAccessFromCommandLine (terreno y NavMesh). Ejecutarlas en batch con Unity cerrado. No se hizo commit ni push.
