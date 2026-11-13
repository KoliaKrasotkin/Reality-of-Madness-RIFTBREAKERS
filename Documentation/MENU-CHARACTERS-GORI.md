# Personajes del menú: guía para Gori

## Archivos que se editan

Los tres personajes del menú tienen copias de presentación independientes en:
`Assets/ROM/UI/FrontEnd/Characters/`

- `WarriorMenu.prefab`
- `PaladinMenu.prefab`
- `MageMenu.prefab`

Abrir el prefab con doble clic en Unity. Allí se pueden cambiar el modelo, accesorios, posición local de las piezas, materiales y Animator. Guardar fuera de Play Mode. Los prefabs Player originales no se modifican.

Cada copia dispone de materiales y controladores de animación propios en la misma carpeta. Los meshes, texturas y clips de animación originales siguen compartidos: para modificar el contenido de uno de esos archivos sólo en el menú, duplicarlo antes y asignar la copia. Reemplazar una referencia en el prefab de menú no cambia el personaje jugable. Editar un archivo compartido sí puede afectarlo.

## Cómo se conectan

`Rift > UI > Menu Characters > Select Catalog` abre `FrontEndCatalog`.
En Models, los SkinIndex 1, 2 y 3 apuntan respectivamente a WarriorMenu, PaladinMenu y MageMenu. Se puede reemplazar su campo Prefab por otra presentación visual. No cambiar SkinIndex ni Key para modificar apariencia.

Estos previews se usan en menú, selección y creación. No son los prefabs de spawn de gameplay y no deben llevar scripts de gameplay, NetworkIdentity, cámaras, luces ni físicas.

## Ver el resultado

Abrir `Assets/ROM/Scenes/MainMenu.unity` y entrar en Play; recorrer menú, selección y creación y alternar clases. Fuera de Play también existen comandos de vista previa en el componente `RiftFrontEndView`. Los objetos transitorios llamados ROM Isolated Preview no son el archivo fuente: editar siempre los prefabs anteriores.

El renderizador normaliza la altura del personaje; cambiar escala del root no equivale a cambiar el zoom. El área visible se ajusta en HomePreview, SelectionPreview y CreationPreview de `Assets/ROM/UI/FrontEnd/Prefabs/RiftFrontEnd.prefab`.

## Validación y mantenimiento

`Rift > UI > Menu Characters > Validate Presentation Prefabs` verifica que las tres referencias estén aisladas, con renderers, materiales y controladores propios y sin scripts de comportamiento.

`Create Missing Presentation Prefabs` es una migración manual: crea únicamente archivos faltantes y nunca sobreescribe un prefab de presentación ya existente. No ejecutarla para refrescar animaciones desde gameplay: las copias son deliberadamente independientes.

Compartir con el equipo los prefabs, materiales, controladores, catálogo y todos sus `.meta`. No publicar Library ni los logs locales de Unity.
