# A03 — El umbral del silencio

Construcción y ambientación del 18 de noviembre de 2026. Escena original guardada y recargada satisfactoriamente en **Unity 6000.3.23f1, DX11, Edit Mode**. Se reutilizó la instancia abierta del usuario. No se inició Play, otro Editor ni un Host.

## Resultado construido

Se conservó el recinto existente de 24 × 24 m, centro (156, −16, 102), y su arco monumental. La intervención modifica la presentación de A03; no construye R21 ni R23.

| Elemento | Construcción y ubicación real |
|---|---|
| Arco dominante | `Subsuelo_Acceso_Etapa01/09 - Arco del descanso inferior/Arch_Wide_01`. Dos renderizadores con material local de piedra oscurecida. Ningún cambio de posición, escala, malla o collider original. |
| Suelo y límites | Los 16 módulos originales de pavimento y 16 renderizadores de las protecciones inferiores recibieron referencias a materiales locales. Conservan sus texturas originales y geometría. |
| Sol erosionado | Relieve de aproximadamente 1,9 m de diámetro, centrado en (156, −9,03, ≈105), sobre la cabecera del arco. Anillo fragmentado y radios desiguales; sin texto ni emisión. |
| Signos antiguos | Tres signos pequeños sobre la pilastra occidental, X≈151,1; Y de −11,9 a −13,32. Sustituyen la inscripción **propuesta** «ACCESO RESTRINGIDO». No se creó texto moderno ni se atribuyó una traducción o un acontecimiento al lore. |
| Anclajes deteriorados | Dos montajes junto a X=151,05 y X=161,1, con placas alrededor de Y=−14,22 y argollas abiertas alrededor de Y=−14,4. Se adaptaron al perfil visible de la piedra; no hay cadena atravesando el vano. |
| Fractura oriental | Trazo de unos 0,90 m, X≈161,28–161,69, Y≈−12,5 a −13,4. Incisión oscura, filamento violeta de 12 mm y ramificación menor. Sin portal, partículas ni luz violeta adicional. |
| Luminaria suspendida | Malla visual original de `Lantern_Small_03_Unlit`, escala 0,8; centro renderizado ≈(164,999, −11,669, 102,8). Extremo inferior Y=−11,9: 4,1 m sobre el suelo. No se copiaron sus colliders ni scripts. |
| Suspensión | Varilla metálica de 25 mm, desde la parte superior de la luminaria hasta el techo físico medido en (165, −7,244, 102,8). No termina en el aire. |
| Luz cálida del arco | Spot en (154, −8,25, 101,8), dirigido a (156, −11,3, 106), alcance 12 m. |
| Luz de orientación | Spot en (165, −11,93, 102,8), dirigido a (163, −16, 100), alcance 9 m. |

Las dos luces son locales, sin nuevas sombras dinámicas. No se tocaron el cielo, iluminación ambiental, volúmenes, postprocesado, cámaras del juego ni ajustes globales de renderizado. La piedra oscurecida contrasta con el contexto exterior al recinto, que permanece sin ambientar.

Los relieves se proyectaron contra los **triángulos renderizados reales del arco**, no contra un plano supuesto ni únicamente contra su collider simplificado. Sus pequeñas separaciones evitan coplanaridad. Los elementos nuevos no tienen colliders, scripts de gameplay ni componentes de navegación.

No se añadieron escombros en el piso, linternas sobre las barandas, marcas de guía en el suelo ni decoración repetitiva adicional. No hay puertas, bloqueos o mecanismos nuevos.

## Inspección visual real

Se observaron cuatro vistas reales de Scene View con iluminación de escena activa y Gizmos desactivados: llegada, aproximación a altura de jugador, pilastra oriental y regreso bajo el arco. La vista de llegada se volvió a capturar después de guardar y recargar.

Se corrigieron durante la inspección:

- Brillo excesivo inicial de los signos: albedo más sobrio, sin emisión.
- Coplanaridad entre incisión y filamento: separación de 4 mm.
- Recorte de transparencia heredado del atlas en adornos lisos: materiales propios opacos para metal y relieves.
- Pérdida del aspecto de la luminaria al sustituir su atlas: clon local del material original de la linterna, conservando su textura.
- Anclajes planos iniciales: argollas volumétricas adaptadas al perfil de la piedra. Las dos versiones planas preliminares permanecen **inactivas**, sin renderizado ni colisión; no son un segundo juego de anclajes activo.

No se observaron nuevos huecos, suspensiones sin apoyo, intersecciones visibles con los accesos ni objetos nuevos sobre las superficies libres del recorrido. Los anclajes y los relieves se leen como detalles pequeños; el arco conserva la jerarquía visual. La aprobación artística final corresponde al usuario.

## Validación ejecutada en Unity

| Comprobación | Resultado real |
|---|---|
| Ruta física A03–R21, antes y después | 1.292 muestras por recepción, ida y vuelta; apoyo, huella, cápsula original del Warrior, barridos y proyección local satisfactorios. |
| Ruta conservada | (156,−16,99) → (174,−16,99) → (174,−16,117) → (120,−16,117) → (117,−16,123), y regreso. |
| Regresiones automáticas existentes | **8/8 navegables**: A03→P01 y R21→P01, ida/vuelta, cápsulas Warrior y Humanoid. NavMesh real, caminos completos, física y barridos. |
| Comparación con la recepción anterior | Idénticos resultados, esquinas nativas, números de muestras y avisos en las ocho rutas. Cero fallos físicos, de barrido o navegación. |
| Colliders de la escena | Huella serializada global idéntica antes/después en la misma sesión. Cero colliders añadidos en A03. |
| NavMesh cargado | Triangulación cargada idéntica antes/después y tras recarga. No se horneó. |
| Guardado y recarga | Satisfactorios; una sola raíz de ambientación y ruta A03–R21 nuevamente comprobada. |
| Compilación | Roslyn y compilación/importación de Unity sin errores. Siete avisos CS0618 ya existentes del diagnóstico experimental NavMeshQuery, ajenos al nuevo código. |

**Límite importante:** las ocho regresiones conservan `navigable=True`, pero `accepted=False` para sus bandas laterales completas. Los avisos de bordes y cuatro carriles son exactamente los mismos que antes de A03. No se ocultaron, no se modificaron tolerancias ni se certificaron anchos cooperativos uniformes. Esta intervención acredita preservación de la navegación individual; no elimina incidencias fuera de su alcance.

No hubo desplazamiento nuevo de un Warrior real ni simulación presentada como prueba multijugador. `04_Altura_de_jugador.jpg` es una cámara del Editor a altura aproximada de jugador. Sigue pendiente la prueba funcional con cuatro Warriors y el último tramo sin telemetría manual de la Etapa 1.

## Integridad y respaldo

Respaldo verificado antes de la intervención:

`Backups/CiudadDelSol/Interior/A03_Construccion_20261118/Antes_SHA256.json`

Incluye escena y meta, NavMeshData y meta, materiales originales, prefabs del arco y luminaria, población exterior, paquetes y ajustes de renderizado. Las 13 comprobaciones coincidían antes de guardar; después solo cambió la escena. Los otros **12/12 archivos protegidos siguen idénticos**.

SHA-256 de la escena anterior:

`7A13D32FA5347C6A4DC33FD30687C565BFF228654C29AC47FE323D4A1960F16B`

SHA-256 de la escena guardada:

`3D552FBE4480DBDCD5D3B0CCEFC02E1640FDF4E493E83BFE5841E17A4204CEBA`

NavMeshData conservado:

`B22F805300D430ECC7AE3FC602A01043A4E4BEF4AAAC4F0A5FB4AF31C4DEAE99`

La comparación completa del YAML comprende 114.350 bloques originales: **ninguno eliminado**, ninguna transformación o collider original alterado. Solo se añadieron referencias de material en **33 instancias de prefab de A03** (34 renderizadores) y la referencia a la nueva raíz de ambientación. Se añadieron 32 objetos con sus transformaciones, 29 renderizadores/mallas visuales —dos inactivos— y dos luces. No se añadieron MonoBehaviours a la escena.

Los informes históricos conservan sus hashes originales; no se reescribieron para hacerlos coincidir con una escena visualmente modificada. La nueva evidencia de preservación acompaña al nuevo SHA.

## Archivos y recursos

- Modificado: `Assets/Scenes/Dungeon_Isla_del_Sol.unity`.
- Nuevo Editor: `Assets/ROM/Editor/CiudadDelSolInteriorA03Atmosphere.cs` y meta. Reutiliza las comprobaciones existentes; no incorpora un controlador o sistema runtime.
- Nuevos recursos locales: `Assets/ROM/Art/CiudadDelSol/Interior/A03/`, sus mallas, materiales y metas. Texturas existentes referenciadas, no duplicadas.
- Dossier, capturas e inventario: esta carpeta.
- Recepciones detalladas, solicitudes consumidas, respaldo y compilación: carpeta de Backups indicada arriba.

La herramienta no vuelve a construir al abrir Unity ni al pulsar Play. Solo atiende una solicitud explícita en `Accion.pending`, en Edit Mode. El guardado exige validación y revisión visual documentada; no ejecuta Bake. No hace falta ejecutarla para ver A03: **la ambientación está guardada en la escena**.

Evidencias principales: `Validacion_recarga.json`, `Integridad_guardado.json`, `Comparacion_regresion.json`, `Diferencias_escena.txt` y `Regresion_rutas/Resultado.txt`, dentro del respaldo de esta intervención. `Elementos.csv` contiene las posiciones y dimensiones renderizadas exportadas desde Unity.

## Pendiente y siguiente paso

Revisar artísticamente las capturas y A03 en la escena. Si se aprueba, planificar R21 después; **R21 y R23 no fueron construidos ni ambientados**. Mirror, Warrior, combate, enemigos, HUD, cinemáticas, población, progresión, colliders y NavMeshData permanecieron intactos. No hubo commits, pushes ni cambios de staging.

Referencias técnicas consultadas: [Renderer.sharedMaterials](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/renderer/sharedmaterials), [EditorSceneManager.SaveScene](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/scenemanagement/editorscenemanager/savescene). Guías aplicadas: Unity `using-unity`, `unity-cli`, `craft-game-floors` y sus referencias de dirección artística, iluminación y revisión visual, subordinadas al alcance local autorizado.
