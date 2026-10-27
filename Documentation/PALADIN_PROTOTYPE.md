# Paladín: prototipo jugable

En creación elegir PALADÍN (PRUEBA). Su identidad visual se llama Defensor de Zhion; no hay nombre personal editable. Este registro entra directamente en El Rift: Crelium sigue siendo el prólogo de Nertör. No se convierten personajes existentes ni se marca artificialmente completado el prólogo.

## Ajustes para el equipo

- Assets/ROM/Resources/PaladinPrototypeSettings.asset: nombre de la skin, descripción, prefab y perfil del único autoataque.
- Assets/ROM/Characters/PaladinPrototype/: Species, Class, Baseline y PracticeSword son assets independientes. Vida180, daño16–20, intervalo1.8s, alcance2.8m son parámetros provisionales de prueba, no un kit de soporte aprobado.
- PaladinPrototypePlayer.prefab conserva el adaptador de motor/red nativo y sustituye sólo la presentación de esta copia. No modificar WarriorPlayer ni el rig de Gori para editarlo. PaladinPrototypePresentation expone Animator, arma y grip.
- PaladinLocomotion.controller: reposo, caminar y Attack0, retarget Humanoid desde clips existentes. PaladinVisual.prefab y material PaladinIvoryBlue son editables.
- FBX BIND: Avatar Humanoid validado, altura medida2.023m. FBX QA_POSES es sólo diagnóstico, nunca animación de juego. Fuente Blender original preservada fuera del proyecto.

No se han definido habilidades activas, talentos ni nuevo recurso del Paladín: los slots permanecen ausentes. El kit de Julián sigue pendiente. No usa la ruta antigua con pistola ni copia habilidades Warrior. Transporte conserva el campo histórico Warrior como selector del adaptador nativo, no como clase visible.

## Evidencia y límites

21 comprobaciones aprobadas en Host local automatizado y perfil aislado: creación real por callbacks, identidad seleccionada, Avatar, locomoción mediante W, daño real del autoataque servidor, ausencia de habilidades/talentos/Furia heredados. La prueba de daño posicionó expresamente el perfil aislado cerca de un enemigo; no es un recorrido manual ni validación Mirror remota.

El modelo tiene ropa/armadura fusionadas, deformación de cadera/rodilla provisional y manos sin dedos individuales. Cuatro triángulos y cinco correspondencias UV no sobrevivieron a la exportación documentada del proveedor local; el blend original conserva la superficie. No se declara listo para producción ni optimizado para WebGL.

Se verificó además ID Mirror propio derivado del GUID del prefab, distinto del Warrior y resuelto correctamente por el registro de cliente. El roster rechaza intercambiar skins entre ambas clases. Esto no sustituye una prueba de otro cliente remoto.
