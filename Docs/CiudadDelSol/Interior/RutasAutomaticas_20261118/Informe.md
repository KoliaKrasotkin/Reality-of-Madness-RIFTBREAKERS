# Ciudad del Sol — cierre de incidencias automáticas A03 y R21

**Resultado comprobado en Unity:** A03 ↔ P01 y R21 ↔ P01 son navegables para las cápsulas Warrior y Humanoid en los ocho ensayos solicitados. Se guardó y recargó el NavMeshData existente. Las regresiones de las Etapas 1–7 y los tres itinerarios encadenados conservaron sus aprobaciones.

**Alcance:** recepción técnica del paso individual sobre esos trazados concretos. No equivale a movimiento real registrado del Warrior, prueba de cuatro jugadores, certificación de las bandas laterales ni de todas las rutas posibles.

## Causas demostradas

1. **Perfil de altura incorrecto en la comprobación exploratoria anterior.** Se usaba la Y del itinerario documental seleccionado para una ruta automática diferente. El ejemplo (73,999; −15,950; 84,633) se evaluaba en Y=−24: una desviación de 8,05 m. Los rechazos así obtenidos no demostraban discontinuidad física.
2. **Holgura insuficiente de la representación NavMesh en esquinas concretas.** Con el perfil nativo correcto, la cápsula Humanoid de 0,57 m contactaba pilastras/columnas y dos protecciones. El Warrior también contactaba la pilastra N5 en la ida desde R21. Son contactos PhysX reales, no errores de altura.
3. **Continuidad local no acreditada junto a Protección_39.** El regreso rechazaba un segmento próximo a (48,557; −16; −12,257). No se ignoró el impacto ni se cambió Connected: el margen local recalculado aparta el recorrido de esa esquina y la nueva traza supera la comprobación original.

## Intervención aplicada

Se añadieron **37 NavMeshModifierVolume Not Walkable**, agente Humanoid 0, bajo `CiudadDelSol_Automaticas_Margenes_20261118`. Representan exclusivamente centros de cápsula que solapaban los obstáculos identificados y márgenes locales en dos esquinas de protecciones. No son colliders ni suelos; no crean pasos, enlaces o atajos.

El raster de ocupación usa el vóxel existente de 0,10 m y una expansión conservadora de un vóxel. Las esquinas de Protección_16 y _39 usan volúmenes de 1,40 × 3 × 1,40 m. Las 37 posiciones y dimensiones persistidas figuran en `Volumenes.csv`.

| Nº | Collider / jerarquía | Primer contacto físico registrado (X,Y,Z) |
|---|---|---|
| 1 | Subsuelo_Central_Sureste_Etapa02/04 - Columnas monumentales del piso/Column_Ceiling_02 | (139.212, -16.000, 22.913) |
| 2 | CiudadDelSol_Etapa4_Protecciones_VS01_VS02/Proteccion_39 | (48.759, -16.000, -12.459) |
| 3 | CiudadDelSol_Etapa4_Protecciones_VS01_VS02/Proteccion_16 | (49.503, -24.000, 18.497) |
| 4 | Subsuelo_Acceso_Etapa01/07 - Pilas de apoyo de descansos/Column_Stackable_01 | (144.925, -16.000, 69.925) |
| 5 | Subsuelo_Rediseno_16_Sectores/N5 Galería norte hacia el Sol/Nervaduras, pilastras y transiciones | (83.192, -16.000, 109.238) |
| 6 | Subsuelo_Acceso_Etapa01/07 - Pilas de apoyo de descansos/Column_Stackable_01 | (147.281, -16.000, 32.043) |
| 7 | Subsuelo_34_Naves_Sin_Reticula/Nave 132/-18 (18×42)/Nervaduras, pilastras y transiciones | (133.316, -16.000, 19.911) |

No se modificaron los modelos, prefabs, posiciones, arquitectura o colliders originales. La comparación de los 114.237 documentos YAML anteriores encontró **cero eliminaciones y cero cambios en objetos originales**, salvo la lista `SceneRoots` para registrar la nueva raíz: se agregaron 113 documentos, correspondientes a raíz y volúmenes.

Se utilizó el NavMeshSurface manual y **el mismo NavMeshData y GUID**. Parámetros conservados: agente 0, radio 0,57 m, escalón 0,36 m, vóxel 0,10 m. La candidata se evaluó en memoria antes de guardar; las candidatas rechazadas no se persistieron.

## Corrección del receptor

- Las rutas automáticas se calculan sin puntos de paso forzados. Se conserva el corredor X/Z nativo y se sigue su altura local, con muestras cada 0,10 m y radio de proyección original de 0,35 m.
- Cada esquina nativa debe alcanzarse en su planta; se conservan apoyo, huella, cápsula, pendientes, barridos y continuidad local original.
- Se separan `navigable` (cápsula individual y continuidad) de `accepted` (además, banda lateral amplia protegida y comprobaciones estáticas correspondientes). No se ocultan los avisos de la segunda.
- El menú de Etapa 7 ya no aplica el perfil documental a las dos rutas automáticas. Reconoce la prueba del NavMesh autorizado actualizado mediante SHA-256 y digest, sin sobrescribir la evidencia histórica de Etapa 6.
- La conservación de cobertura distingue identidad de vértices de transitabilidad: pequeñas retriangulaciones solo se admitieron con mismo suelo, cápsula y barrido despejados y conexión local recíproca demostrada. No se modificó la tolerancia de rutas.

## Resultados reales tras recarga

Unity 6000.3.23f1, Editor gráfico DX11, Edit Mode, PhysX y NavMesh reales. No se inició Host ni se movió un personaje para simular una prueba funcional.

| Ruta | Cápsula | Sentido | Muestras | Fallos físicos | Barridos | Continuidad/Nav local | Resultado individual |
|---|---|---|---:|---:|---:|---:|---|
| A03-P01 | 0.57 m | ida | 3635 | 0 | 0 | 0 | Satisfactorio |
| A03-P01 | 0.57 m | vuelta | 3672 | 0 | 0 | 0 | Satisfactorio |
| A03-P01 | 0.32 m | ida | 3635 | 0 | 0 | 0 | Satisfactorio |
| A03-P01 | 0.32 m | vuelta | 3672 | 0 | 0 | 0 | Satisfactorio |
| R21-P01 | 0.57 m | ida | 3335 | 0 | 0 | 0 | Satisfactorio |
| R21-P01 | 0.57 m | vuelta | 3295 | 0 | 0 | 0 | Satisfactorio |
| R21-P01 | 0.32 m | ida | 3335 | 0 | 0 | 0 | Satisfactorio |
| R21-P01 | 0.32 m | vuelta | 3295 | 0 | 0 | 0 | Satisfactorio |

Todos los caminos fueron PathComplete y alcanzaron todas sus esquinas. Las ocho capturas finales tienen integridad de archivos y no son previsualizaciones. Pendiente máxima muestreada: 18,916°. La proyección horizontal máxima quedó por debajo de 0,00015 m.

### Espacio lateral: pendiente explícito

**`accepted=false` en las ocho capturas:** las bandas laterales amplias no reciben aprobación. El paso de la cápsula sí quedó acreditado (`navigable=true`). Los avisos de bordes se refieren al ensayo lateral fuera de la huella ocupada; no deben descartarse como inexistentes ni interpretarse como aprobación cooperativa.

| Ruta | Sentido | Avisos laterales Warrior | Secciones sin cuatro cápsulas estáticas despejadas | Ancho útil inferior muestreado |
|---|---|---:|---:|---:|
| A03-P01 | ida | 23 | 146 | 2.84 m |
| A03-P01 | vuelta | 27 | 149 | 2.84 m |
| R21-P01 | ida | 39 | 129 | 3.62 m |
| R21-P01 | vuelta | 46 | 135 | 3.52 m |

Esas secciones no son fallos de continuidad del eje individual, pero impiden acreditar una banda amplia protegida alrededor de todo el atajo. Los itinerarios encadenados recibidos siguen siendo la referencia amplia comprobada. Si se exige libertad lateral segura en los atajos, corresponde una intervención específica de protecciones y su validación, no cambiar esta etiqueta a aprobado.

## Regresiones y preservación

| Área | Resultado real posterior al guardado |
|---|---|
| Etapa 1 A03–R21 | 1/1 técnica |
| Etapa 2 | 6/6 |
| Etapa 3 | 3/3 |
| Etapa 4 | 2/2 |
| Etapa 5 y trazas globales | 2/2 en ambos conjuntos |
| Etapa 6 VS03 / perfil denso / global / Humanoid | Satisfactorios |
| Etapa 7 VS06 | 1/1 |
| Encadenadas A03, R21 y B04 → P01 | 3/3, más cápsula Humanoid ida/vuelta |

Se compararon 60.183 testigos originales en la candidata y 60.173 en sentido inverso, fuera de los dominios locales afectados: cero fallos. La conectividad exterior previamente completa entre las siete anclas sectoriales se conservó. No es una nueva certificación física de todos los recorridos exteriores.

Compilación C# Editor: sin errores; permanecen advertencias de API obsoleta NavMeshQuery en diagnósticos anteriores. Importación/compilación real de Unity comprobada. No se cambiaron scripts Runtime de Mirror, combate, población, salud, XP ni progresión.

## Archivos de esta intervención

- `Assets/ROM/Editor/CiudadDelSolInteriorStageSeven.Automatic.cs` y `.meta`: recepción focalizada y prueba de recarga.
- `Assets/ROM/Editor/CiudadDelSolInteriorStageSeven.AutomaticSources.cs` y `.meta`: corrección transaccional de fuentes locales y preservación.
- `Assets/ROM/Editor/CiudadDelSolInteriorStageSeven.cs`: uso del perfil automático correcto y reconocimiento del NavMesh autorizado actualizado.
- `Assets/Scenes/Dungeon_Isla_del_Sol.unity`: solo raíz y 37 modificadores de navegación locales añadidos.
- `Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset`: datos actualizados, referencia conservada.
- Este dossier y evidencia independiente en `Backups/CiudadDelSol/Interior/RutasAutomaticas_20261118/`.

## Respaldo y evidencia

- Escena antes: `9C1D5DD1D0260DB1E6CA6789EFE1809DC519FCC6B1DDD8327B901D706BFE3375`.
- Escena después: `7A13D32FA5347C6A4DC33FD30687C565BFF228654C29AC47FE323D4A1960F16B`.
- NavMeshData antes: `FC3EC2B8E0DC7CA1B76F1333B4DD472438D87711C64B20DAA9C1E05FFD8B69DB`.
- NavMeshData después: `B22F805300D430ECC7AE3FC602A01043A4E4BEF4AAAC4F0A5FB4AF31C4DEAE99`.
- Respaldo completo de escena/datos/scripts afectados con manifiesto `Antes_SHA256.json` en la carpeta de evidencia.
- `Recepcion/` y `Recarga/`: ocho capturas detalladas de estaciones y raycasts por conjunto.
- `RegresionesRecepcion/`: resultados reales de las siete etapas y recorridos encadenados.
- `Comparacion_YAML.json`: integridad de objetos originales.
- `Prueba_final.json`: recarga, digest nativo, huellas e integridad.

## Estado global y continuación

Quedan cerradas las incidencias de proyección/colisión/continuidad que impedían acreditar el paso individual de estas dos alternativas. Se conservan todos los recorridos encadenados aprobados. **No están certificadas todas las rutas posibles, las bandas laterales de los atajos ni cuatro jugadores reales.** Tampoco se ha completado retrospectivamente el tramo manual sin telemetría de Etapa 1.

Se puede continuar con el desarrollo de la dungeon manteniendo esos pendientes explícitos. No se construyó ni se modificó la cámara final. No se realizaron commits, pushes ni cambios en staging.

Repetición focalizada opcional: con la escena limpia en Edit Mode, menú **ROM → Etapa 7 interior → Comprobar automaticas A03 R21**. No hace falta repetir ahora: la recepción desde disco ya se ejecutó.
