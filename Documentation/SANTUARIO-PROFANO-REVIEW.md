# Santuario Profano: revisión pendiente

Generador: `Assets/ROM/Editor/SantuarioProfanoArchitectureGenerator.cs`.

La escena original no se integró ni se guardó. SHA-256 original verificado:
`A700660B3D2E40F4F5D8B259019F2E9DC02DB1DE6F619438171BBC88EF3B4922`.

## Abrir la previsualización

1. Abrir `Assets/Scenes/Dungeon_Isla_del_Sol.unity` y esperar la compilación.
2. Elegir **ROM > Dungeon Ciudad del Sol > Santuario Profano > 1. Previsualizar complejo ceremonial (copia temporal)**.
3. La copia vive en un `PreviewSceneStage`; salir con **3. Cerrar previsualizacion sin guardar**.
4. **2. Validar accesos, escaleras y colliders** ejecuta las comprobaciones geométricas sobre la copia.
5. Para una copia completa del dungeon en disco, guardar o descartar cambios pendientes y elegir **4. Crear copia completa de revision temporal**. Si las comprobaciones pasan, se guarda únicamente `Assets/Scenes/Previews/TEMP_SantuarioProfano_Review.unity` y se escribe el informe `Documentation/santuario-profano-validation.txt`.

No existe comando para integrar en la original. Los prefabs fuente permanecen intactos.

## Composición propuesta

Templo con fachada monumental, columnatas y patios interiores. Galerías laterales y presbiterio a +4 m; santuario posterior a +8 m. Diez módulos de escalera nativos de 6 m de ancho. Altares de piedra, ábside, arcos, muros deteriorados y columnas rotas. Se preservan tres accesos y sus corredores.

Se reutilizan los paquetes Eternal Temple, Sun Temple y Grand Egyptian Temple. Las piezas que invaden estructuras previas o recorridos se descartan; si una pieza necesaria es incompatible, la herramienta falla explícitamente. No se agregan enemigos, cofres, partículas ni efectos del Vacío. La luz neutra existe únicamente en el escenario temporal de inspección.

## Estado real de verificación

- Las 15 rutas de prefabs referenciadas existen.
- Compilación estática satisfactoria con Roslyn y las referencias del proyecto Unity 6000.3.23f1.
- SHA-256 de la escena original sin cambios.
- Unity batch terminó con código 1 antes de ejecutar el método; no produjo una copia construida ni una validación geométrica completa.
- La consulta adicional de procesos no fue autorizada. No se cerraron procesos del usuario.
- La referencia original del mapa no estaba adjunta; la composición se basa en la estructura existente y los generadores previos.
- Pendientes: ejecución satisfactoria del generador en Unity, revisión visual, pruebas del controlador de cuatro jugadores y NavMesh. La compilación estática por separado no sustituye esas comprobaciones.
