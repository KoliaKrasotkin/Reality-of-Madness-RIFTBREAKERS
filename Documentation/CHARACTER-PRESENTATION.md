> Actualización: la compensación manual de piernas descrita en esta primera pasada fue retirada. La implementación vigente usa el Animator nativo y variantes erguidas: ver ARMED-LOCOMOTION-REVIEW.md.

# Presentación de personajes IDS

Se conservan las mallas y texturas originales. Esta pasada cambia su presentación y el ajuste de pose; no rediseña la anatomía ni añade detalle geométrico.

- Nueve materiales URP específicos para las cinco skins, albedo original, normales importadas correctamente, mapas metallic/smoothness y oclusión cuando existe. Los mapas separados de metal y roughness se empaquetan como R=metal, A=1-roughness en espacio lineal. Las copias configuradas se guardan dentro de ROM; los importadores del proveedor se conservan.
- Menos iluminación frontal acumulada en el visor y colores más neutros para mantener el volumen. La piel de Dinork tiene un límite de suavidad menor que la armadura.
- Postura del menú con menor apertura de piernas, altura de apoyo conservada y una oscilación muy suave del torso.
- Durante la partida, la diferencia de longitud de piernas ajusta la altura de cadera. Una solución de dos huesos por pierna sigue las trayectorias originales de los pies sin escalar los huesos; conserva la rotación retargeteada del pie. No añade raycasts de foot IK sobre el terreno.
- El constructor de frontend vuelve a aplicar los materiales para que una reconstrucción no descarte esta pasada.

Validación: character-pbr-validation.txt documenta los conjuntos de textura de cada submalla. locomotion-rig-validation.txt revisa ciclos de marcha y longitudes. locomotion-play-validation.txt registra un host real de El Rift y los cinco cambios de skin. Las capturas locomotion-ids-character-0.png a -4.png muestran la pose de marcha; rift-button-style-lobby.png muestra el menú real actualizado.

Queda la evaluación visual humana de los ciclos completos, cada arma y pendientes. Las hitboxes y anclajes siguen en el rig del kit; la compensación visual no redefine las hitboxes por personaje. Los brazos de primera persona siguen usando sus materiales y sistema anteriores. Una estética realista exige revisar o sustituir las mallas estilizadas originales; estos cambios no se presentan como un rediseño de personajes.
