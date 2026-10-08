# Configuración de git para este proyecto

Pasos que **cada persona** tiene que hacer una vez en su máquina. No se pueden
commitear porque dependen de rutas locales de instalación.

## 1. Merge driver de Unity (obligatorio)

El `.gitattributes` del repo declara `merge=unityyamlmerge` para `*.unity`,
`*.prefab`, `*.asset` y el resto del YAML de Unity. Ese driver **no viene
configurado con git**: si no lo configurás, git cae al merge de texto común y
sobre YAML de Unity eso produce escenas y prefabs corruptos o conflictos falsos.

Ajustá la versión de Unity en la ruta si usás otra:

```bash
git config merge.unityyamlmerge.name "Unity YAML merge"
git config merge.unityyamlmerge.driver '"C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p --force --fallback-none "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
```

Verificar que quedó:

```bash
git config --get-regexp merge.unityyamlmerge
```

## 2. Git LFS

El repo usa LFS. Si clonaste sin LFS instalado, los binarios bajan como
punteros de texto y el proyecto no abre bien:

```bash
git lfs install
git lfs pull
```

## Archivos compartidos: cuidado al tocarlos

Estos no mergean bien y conviene coordinarlos antes de editarlos:

| Archivo | Por qué |
|---|---|
| `Assets/MultiFPS/Levels/hub.unity` | lista de mapas de `RoomCreator` |
| `ProjectSettings/EditorBuildSettings.asset` | lista de escenas del build |
| `ProjectSettings/ProjectSettings.asset` | layers, input handler, plataformas |

Regla práctica: tocalos en commits chicos y separados, y avisá al equipo.

## Unity 6: reserialización automática

Al abrir el proyecto con Unity 6, el editor reescribe solo algunos assets
(por ejemplo los renderers de URP y los fallback de TextMesh Pro). Eso aparece
como cambios sin commitear que **no son trabajo de nadie**. Ya están commiteados
una vez en `main`; si te vuelven a aparecer, revisá que estés en la versión de
Unity del proyecto (`ProjectSettings/ProjectVersion.txt`) antes de commitearlos
de nuevo.
