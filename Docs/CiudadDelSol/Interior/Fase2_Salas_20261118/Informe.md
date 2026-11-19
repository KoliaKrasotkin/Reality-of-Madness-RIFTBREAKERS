# Ciudad del Sol — Fase 2: auditoría de salas y desarrollo arquitectónico

**La base monumental ya existe; no hace falta construir 68 habitaciones nuevas.** Se reconocieron las 67 jerarquías físicas del inventario y sus superficies actuales en Unity. P01 sigue siendo una delimitación funcional dentro de D03, no un recinto independiente. El trabajo principal pendiente es convertir las naves existentes en lugares diferenciados, legibles y preparados para exploración y combate, conservando sus recorridos.

**Recomendación:** autorizar primero un lote arquitectónico de **A03–R21–R23**, con R21 como Salón de las Ofrendas. No se ejecutó construcción, guardado de escena, Bake ni modificación de NavMeshData en esta auditoría.

## Entregables

- [Índice visual e inventario filtrable](Indice.html).
- [Inventario de los 68 espacios y fichas individuales](Inventario_detallado.md).
- [Tabla completa CSV](Inventario_68_actualizado.csv), [datos JSON](Inventario_68_actualizado.json).
- [Sectores y etapas de construcción](Etapas_construccion.csv).
- [Plano general por niveles](Plano_general.svg).
- Planos detallados: [Y=20](Plano_Y20.svg), [Y=4](Plano_Y4.svg), [Y=−16](Plano_Y-16.svg), [Y=−24](Plano_Y-24.svg), [Y=−30](Plano_Y-30.svg), [Y=−34](Plano_Y-34.svg), [Y=−38](Plano_Y-38.svg).
- [Propuesta del primer sector a escala](Propuesta_Sector01.svg) y [familias visuales propuestas](Propuesta_visual.svg).
- [Mediciones reales de los 68 espacios](Unity_68.json), [cotas y luces globales](Unity_Cotas_y_luz.json), [resumen de la ejecución](Unity_Resumen.json).
- 14 capturas reales del Editor en `Capturas/`, también enlazadas desde el índice visual.

## Qué se comprobó y qué no

Se consultaron Plan_20261117, Auditoria_20261117, los informes de las siete etapas, RecepcionIndividual_20261117 y RutasAutomaticas_20261118. Se contrastó el inventario con la escena definitiva abierta en **Unity 6000.3.23f1, DX11, Edit Mode**.

Se reutilizó `CiudadDelSolSubsueloSpatialAudit` y el método `Probe` de la recepción existente de Etapa 2, con la configuración real del Warrior. Una instrumentación temporal de lectura recopiló:

- Las 67 jerarquías, renderizadores, materiales, colliders, estados activos y envolventes visuales reales. No se utilizaron pivotes como si fueran centros de las salas.
- **18.243 estaciones físicas** en una retícula estratificada de hasta 2 m: apoyo local, huella, cápsula Warrior y proyección NavMesh en su cota. Las muestras no autorizan superficies a otra altura.
- Altura libre sobre puntos despejados, cuando un rayo vertical encuentra un techo físico. Se conserva la diferencia entre techo estructural, altura libre mínima y volumen de una envolvente.
- Camino NavMesh completo de ida y vuelta entre A03 y una semilla despejada de cada espacio, y conexión de las muestras despejadas con esa semilla.
- Iluminación local dentro de las envolventes y configuración global existente.
- **14 vistas representativas reales**: A01, A03, R21, R23, R34, G08, R19, M01, M02, M03, M04, D01, D02 y B01.

La retícula no reemplaza un barrido continuo de toda la sala: puede omitir obstáculos pequeños entre muestras. El camino a una semilla no recibe físicamente toda la ruta, todos los accesos ni toda la superficie. No se movió al Warrior, no se inició Host y no se probó a cuatro jugadores. Tampoco se repitieron innecesariamente las siete recepciones: **escena y NavMeshData permanecen idénticos a los datos recibidos**, lo que conserva sus evidencias anteriores sin presentarlas como pruebas nuevas.

El sondeo de salas usa la cápsula del Warrior. No se presenta como barrido completo de Humanoid en las 68 áreas. Se conservan sus parámetros originales y las recepciones anteriores; las nuevas piezas deberán recibirse con ambos tamaños antes de poblar el interior.

Las capturas aíslan temporalmente las raíces interiores para evitar que las cubiertas exteriores oculten las naves. Son vistas del Editor, no la iluminación final del juego. Un hueco azul durante ese aislamiento no demuestra por sí solo una pared ausente en la escena completa.

## Clasificación actual

| Estado solicitado | Cantidad | Interpretación |
|---|---:|---|
| Construida y funcional | **4** | B03, B04, B05 y B06, en su función técnica de puente/terraza de tránsito recibida. No equivale a cuatro jugadores certificados. |
| Construida, pero necesita mejoras | **63** | Existe estructura comprobable, pero falta identidad, remate, legibilidad o recepción integral de su uso como sala. Incluye ámbitos protegidos que no se construirán ahora. |
| Parcialmente construida | **0** | No hay evidencia suficiente para atribuir una estructura deliberadamente abierta o un anillo con vacío central a una obra incompleta. |
| Pendiente de construcción | **0** | No apareció una sala aprobada completamente inexistente que obligue a construir un volumen nuevo. |
| Espacio propuesto sin recinto independiente | **1** | P01, dentro de D03. |

La clasificación es conservadora: **no se llama sala terminada a una superficie despejada con un nombre**. Las 54 naves y galerías tienen suelo compartido, bóveda y mallas estructurales reales; en esta Fase 2 se consideran construidas con desarrollo arquitectónico pendiente, no habitaciones independientes ya diseñadas y terminadas.

### Las 54 naves de Y=−16

Son 34 espacios R, 16 galerías G y cuatro salas monumentales M. Sus envolventes nominales suman 38.412 m²; esta cifra describe el inventario de ese nivel, **no el área total jugable certificada de la dungeon**. Los niveles profundos y las subzonas no deben sumarse sin deduplicar.

En estas naves se probaron 9.603 estaciones: todas encontraron apoyo a la cota esperada; 9.440 quedaron despejadas y también sobre NavMesh. Las restantes coinciden con ocupaciones o márgenes que se conservan como tales, no con supuestos pisos que haya que rellenar. Todas las muestras despejadas con NavMesh de cada una de las 54 naves conectaron con su semilla de sala.

Hay escala suficiente para una dungeon monumental: R19 tiene 42×54 m, R21 30×42 m, R11/R26/R34 18×78 m, M03 18×60 m y M04 18×66 m. Las naves pequeñas de 18×18 m pueden servir como recuperación, relicario o exploración; no todas necesitan convertirse en arenas grandes.

Las cotas visuales se midieron sobre las mallas actuales. Por ejemplo, R21 conserva centro de planta (117,−16,123), 30×42 m; su AABB visual mide 30×18,62×42 m y su centro vertical no es el piso. D01 tiene envolvente nominal 54×66 m y AABB visual aproximada 56,088×21,25×68,088 m por los espesores arquitectónicos. No se confunden estas diferencias con desplazamientos o ampliaciones nuevas.

**Criterio cooperativo propuesto, no certificado:** en las salas destinadas a combate, buscar campos continuos de al menos 12×12 m y mayores en los grandes salones, siempre que los pilares y las rutas existentes lo permitan. En las salas de recuperación se prioriza estancia y paso, no una arena. Los puentes y giros de acceso conservan su función; no se les exige convertir toda su envolvente en campo de combate. El ancho objetivo de una propuesta no sustituye la anchura útil medida ni permite estrechar los recorridos recibidos.

## Hallazgos arquitectónicos

### 1. El principal trabajo es de identidad y composición

Las capturas muestran bóvedas, arcos y piedra monumental coherentes, pero una repetición elevada de pavimento, columnas, arcadas y grandes centros vacíos. R21, R34 y las cuatro M todavía se leen más como variaciones del mismo conjunto que como espacios con funciones diferenciadas.

No se propone llenar esos vacíos con obstáculos. Se propone introducir **jerarquía de accesos, uno o pocos focos laterales, frisos, composición de luz y pausas de escala**. Algunas naves conservarán deliberadamente el vacío ceremonial.

Las mallas estructurales de las naves están combinadas: una bóveda longitudinal y una malla de nervaduras/pilastras/transiciones. No conviene editar un supuesto elemento individual mediante desplazamiento de la malla completa: podría mover muchas piezas. Los remates futuros deben añadirse de manera localizada o editarse con alcance explícito y respaldo.

### 2. Iluminación interior sin desarrollo por salas

La captura global muestra ambientación `Skybox`, intensidad ambiente 1 y **cero lightmaps**. Hay 21 componentes Light en la escena, 14 activos en total —incluido el exterior—. Esa cifra no representa luces propias de cada sala.

En las 54 envolventes de naves sólo M03 contiene una luminaria local en la ventana vertical consultada. Esto no significa que las otras naves estén negras: intervienen ambiente, luces globales, materiales y el modo del Scene View. Sí demuestra que no existe una propuesta local de iluminación para distinguir la mayoría de los recintos.

La propuesta usa luz cálida para llegada/recuperación, contraluz para procesiones y violeta concentrado para corrupción. El método de horneado y presupuesto de sombras se definirá al construir, sin alterar ahora la iluminación definitiva ni multiplicar Point Lights con sombras por cada columna.

### 3. Conectividad: no hay una sala entera demostrada como isla

Las 68 semillas tienen `PathComplete` en ambos sentidos con A03. P01 utiliza el suelo de D03 y no añade una sala. Esto acredita una conexión topológica a un punto, no todos los trazados directos del plano.

Se detectaron **37 muestras despejadas con NavMesh que no conectan con la semilla de su espacio**:

| Espacio | Muestras desconectadas | Consecuencia |
|---|---:|---|
| A00 | 27 | Conservar el acceso exterior recibido; no extender zonas de estancia a estas muestras sin estudiar su función. |
| A01 | 6 | No utilizar toda la envolvente de recepción como si fuera una única zona de combate. |
| A03 | 4 | Conservar rodeo y accesos acreditados; estos puntos no invalidan la ruta recibida. |

No se atribuye todavía una causa física a esos bolsillos ni se propone rellenarlos. La captura conserva la cuenta por espacio, pero no exportó el resultado de conectividad individual de cada punto: **no se inventan coordenadas de los bolsillos**. Antes de una obra que los afecte, basta un examen local, no repetir las siete etapas.

Los enlaces H ajenos a H06/H12/H13/H14 y los lazos LA1–LA4 siguen siendo enlaces documentados, no rutas físicamente recibidas por esta auditoría. La existencia de un camino global a una sala no aprueba una puerta dibujada o un enlace recto concreto.

### 4. Vacíos y límites de envolventes

- B01 y B02 son anillos: sus rectángulos nominales incluyen el vacío central. Los porcentajes de apoyo bajos **no son una orden de construir un piso sobre el hueco**.
- B03/B04 son puentes y B05/B06 terrazas con bordes/piezas que ocupan parte de sus envolventes. Su función de tránsito recibida se conserva; no se convierten en salas de combate para elevar la ocupación.
- D02 tiene muestras sin apoyo dentro de su rectángulo nominal. Se mantienen señaladas en el plano de Y=−34 y en los datos; no se concluye automáticamente que sean un agujero accidental o una obra incompleta.
- El borde de losa visible en primer plano de D01 merece una revisión visual localizada antes de cualquier remate. No se lo declara defecto de colisión a partir de la imagen.
- Las arcadas ornamentales de las galerías no se consideran puertas abiertas por su apariencia.

## Propuesta visual y arquitectónica

### Llegada solar y espacios de recuperación

Piedra dorada existente, relieves de rayos, focos cálidos y señalización arquitectónica. Las salas de recuperación conservan esa función; no se les añade combate en este diseño. Los alias artísticos del inventario son opcionales y no cambian identificadores ni jerarquías.

### Procesiones y grandes ceremonias

R21, R34, R11, R19, R26, R22 y R32 conservan su escala. Se propone rematar los arranques de bóveda y ordenar el ritmo en tramos, con hitos en el perímetro y suelo central continuo. No se plantean tabiques cada pocos metros ni recorridos estrechos para forzar variedad.

### Claustro, archivos y relicarios

M01 gana un motivo de eclipse en el pavimento y lectura de sus salidas. Las ramas occidentales incorporan depósitos de tablillas, libros o piedra inscrita y focos de exploración. R09, R12 y R33 son candidatos a descubrimientos secundarios dentro de sus huellas existentes.

No se acredita ahora una sala secreta nueva detrás de un muro. Un panel o nicho secreto sólo se desarrollará donde exista volumen físico y un acceso compatible; no mediante teletransporte ni perforando arquitectura sin aprobación.

### Jardines interiores

M03 y su rama utilizan relieves vegetales y jardines minerales laterales, con piedra más erosionada y variedad de luz. No necesitan un estanque nuevo, plantas que tapen rutas ni un cambio de altura del eje principal.

### Santuario y corrupción

M04 mantiene una composición longitudinal de votos solares rotos e incensarios laterales. M02 se desarrolla como templo previo al núcleo, con corrupción violeta focal y salida legible. El grado de corrupción se decide por composición de cada sala, no sólo por profundidad: varias M comparten la cota −16.

### Cámaras profundas y verticalidad

D01/D02 se desarrollan aprovechando las grandes arcadas, escaleras y terrazas existentes. Los balcones actuales bastan para una primera lectura vertical; no se propone un nivel nuevo por encima de las salas. D03/P01 permanecen reservados e intactos.

## Construcción por sectores

| Orden | Sector | Espacios | Cantidad | Objetivo |
|---|---|---|---:|---|
| S01 | Llegada y Ofrendas | A01, A02, A03, R21, R23 | 5 | Primer lote A03–R21–R23; remate superior después. |
| S02 | Procesión de la Aurora | G05, R34, G13, R16, R20 | 5 | Galerías legibles y gran procesión. |
| S03 | Claustro ceremonial | R11, R17, G12, M01, R33 | 5 | Centro ceremonial y relicario lateral. |
| S04 | Ala del Sol Cautivo | G11, R26, G03, R30, G04, G01, M02, G02, R31, R27 | 10 | Rama oriental, templo y recuperación. |
| S05 | Archivos occidentales | G14, R01, G08, G15, G07, R09, R08 | 7 | Exploración y relicarios. |
| S06 | Jardines interiores | R05, G10, R18, R15, R14, R12, R13, M03, R04, R02 | 10 | Jardín mineral, cámaras y ceremonias. |
| S07 | Solsticios suroeste | R03, R07, R19, R06, G06, R10 | 6 | Salón mayor y procesiones. |
| S08 | Santuario interior | R25, G16, R32, R28, M04, G09, R29, R24, R22 | 9 | Santuario, corrupción y recuperación. |
| S09 | Cámaras profundas | D01, D02, B05, B06 | 4 | Lectura de arcadas, terrazas y grandes campos. |
| S10 | Conservación, no nueva obra ahora | B01, B02, B03, B04 | 4 | Mantener rutas/verticalidad; sin intervenir D03. |
| RES | Fuera de construcción | A00, D03, P01 | 3 | Preservar referencia exterior y cámara final. |

La suma asigna los 68 espacios exactamente una vez. El orden progresa desde el acceso por ramas contiguas; S04 puede realizarse después de S01 si se prioriza el ala oriental. No se exige cerrar todas las ramas occidentales para ambientar el Santuario, pero cada unión que se use debe comprobarse localmente antes de construir sobre ella.

### Primera autorización recomendada: lote S01-A

| Espacio | Centro de piso | Planta | Trabajo propuesto |
|---|---|---|---|
| A03 | (156,−16,102) | 24×24 m | Hito de orientación y luz guía; conservar arco y rodeo norte. |
| R21 | (117,−16,123) | 30×42 m | Salón de las Ofrendas: friso, composición ceremonial y foco lateral; gran centro libre. |
| R23 | (123,−16,165) | 18×18 m | Cámara de recuperación legible, cálida y conectada con la salida; sin encuentro nuevo. |

El esquema utiliza el rodeo real `(156,−16,99) → (174,−16,99) → (174,−16,117) → (120,−16,117) → (117,−16,123)` y la continuación recibida de H12. No dibuja una puerta recta ficticia entre las envolventes A03 y R21.

Se propone un foco lateral candidato en **(127,−16,135)** de R21. La estación existente en ese punto pasó apoyo, huella, cápsula y NavMesh, pero **no se ha validado una pieza de tamaño finito** ni su efecto sobre todos los recorridos recibidos. Por eso el símbolo del plano es propuesta, no un pedestal ya autorizado o instalado. No se fija un altar sobre H12 ni sobre el rodeo aprobado.

Primero se aprobará un sector visual completo y representativo, no una nueva herramienta de diagnóstico. El estilo resultante servirá como referencia para extenderse por las demás salas.

## Recursos reutilizables y límites de los generadores

Recursos comprobados en disco y/o en renderizadores vivos:

- `Assets/Standard Assets/Eternal Temple/Source_FBX/Materials/Atlas_Architecture_01.mat` y `Assets/ROM/CiudadDelSolSalasMonumentales/PiedraOpaca.mat`: materiales ya usados por la arquitectura.
- Eternal Temple: `Platform_Base_01`, `Staircase_01`, columnas, arcos, `Bottom_Decor_01` y barandas originales. Los prefabs no se modificarán globalmente para ambientar una sala.
- Iluminación/ornamento: `Lanterns/Pedestal_01.prefab`, `Pedestal_02.prefab`, `Torch_01.prefab`, `Lantern_Wall_01.prefab` y linternas existentes.
- Sun_Temple contiene `Prop_Book_*`, `Prop_BookStash_*` y `Prop_BookCase_*` para una rama de archivos; su escala, material y coste deben probarse antes de desplegarlos.
- `Assets/ROM/Art/RiftCrystals/Prefabs/CrystalGround.prefab` y `CrystalAerial.prefab`: candidatos visuales para corrupción, conservando sus originales y sin activar sistemas de gameplay asociados.
- Los generadores de acceso/alas, `CiudadDelSolSubsueloOpenHalls.cs`, `CiudadDelSolSalasIntegration.cs` y el conjunto del núcleo documentan la construcción existente.

**No se deben volver a ejecutar los generadores globales sobre la escena actual.** Algunos tienen hashes históricos rígidos y operaciones de sustitución/integración. Reutilizar su geometría, materiales o criterios no significa regenerar la dungeon ni perder las correcciones manuales de las siete etapas.

## Recepción de cada sector futuro

1. Respaldar escena y cualquier asset afectado con SHA-256; congelar el alcance por IDs/jerarquías.
2. Construir remates y focos sobre la arquitectura existente, sin ocupar las cápsulas/rutas recibidas ni crear nuevas cotas por decoración.
3. Recibir suelos, anchura/altura, colisiones, apoyo, barridos y bordes de los espacios realmente modificados. Las dimensiones nominales no son anchura útil.
4. Repetir la recepción de rutas vecinas afectadas. Sólo si se cambia NavMeshData compartido, ejecutar las regresiones que protegen todas sus áreas; no repetirlas por una captura o una propuesta documental.
5. Comparar visualmente tomas de aproximación, interior y regreso, y medir rendimiento. No multiplicar luces con sombras, transparencias o renderizadores pequeños sin medir su coste.
6. Probar posteriormente con cuatro jugadores reales. Cuatro cápsulas estáticas o una semilla NavMesh no sustituyen ese ensayo.

Se mantienen las restricciones de las alternativas automáticas: paso individual recibido, pero bandas laterales amplias sin certificación. No deben usarse para situar nueva decoración como si toda la zona alrededor del atajo estuviera aprobada.

## Integridad, herramientas y límites

La instrumentación se añadió temporalmente al auditor existente, se compiló y ejecutó, y **se restauró su archivo original byte a byte**. No queda un nuevo sistema de diagnóstico en Assets. Sólo se conservan datos y copias de las rutinas como texto en este dossier para trazabilidad.

La escena y NavMeshData permanecen con sus hashes iniciales; no se hizo Bake ni guardado. Los 48 puestos exteriores, Mirror, combate, controles, HUD, XP y progresión no se tocaron. La cámara final sólo se incluyó como referencia del inventario; no se construyó ni alteró.

Durante el cambio de una vista se registró una incidencia de lectura/escritura simultánea en el archivo temporal de solicitud (`Vista.pending`). Se completó la vista y se publicaron las siguientes solicitudes de forma atómica; esa instrumentación ya fue retirada. No se atribuye esa incidencia a la escena o al gameplay ni se borraron registros de consola para ocultarla. Las advertencias de APIs NavMesh obsoletas pertenecen a diagnósticos previos.

No se hicieron commits, pushes ni cambios en staging. La auditoría completa de las 68 fichas, los planos, las propuestas y la medición real quedan disponibles para aprobar la construcción.

Los SVG se renderizaron y revisaron también en PNG. El índice HTML se comprobó estructuralmente; no se ejecutó su interfaz en un navegador. Se consultaron las guías `using-unity`, `unity-cli.md` y `common-workflows.md` para el flujo de inspección. No se instalaron paquetes.
