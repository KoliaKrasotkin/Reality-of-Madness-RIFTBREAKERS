# Escenas principales

- Menú: Assets/ROM/Scenes/MainMenu.unity.
- Mapa activo: Assets/ROM/Levels/map_rom_horde.unity (El Rift).

En Unity: ROM > Escenas > Abrir Main Menu. Abrir Game para ver la vista previa;
Play activa el escenario 3D, los personajes, los botones y las conexiones.
La jerarquía guardada incluye SafeAreaDesign con la interfaz visible en edición.
El layout se reconstruye desde RiftHub al ejecutar. Para actualizar la vista
previa tras cambios de código: ROM > Escenas > Actualizar vista previa del menú.
Los cambios directos en los hijos generados se sustituyen al entrar en Play;
las referencias y estilos permanentes se configuran en RiftHub y sus fuentes.

MainMenu conserva el GUID de hub.unity. Los constructores y las pruebas apuntan
a la nueva ruta. Build Settings contiene solo las dos escenas activas.
Se verificó cargar el lobby 3D, crear una partida y volver desde la pausa.

Nueve escenas de iteraciones/pruebas y un MapRepresenter de pruebas fueron
retirados de Assets y archivados con sus .meta en Archive/DeprecatedScenes.
Se conservaron las demos originales de paquetes y los modelos/materiales.
Los constructores antiguos ya no generan las escenas de respaldo eliminadas.
Manifest: Documentation/scene-cleanup.txt.
