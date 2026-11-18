# Ciudad del Sol — Etapa 6: VS03 y descenso a Y=−38

**Recepción técnica individual satisfactoria en Unity, después de guardar y recargar desde disco.** VS03: 1/1. Las cinco etapas anteriores mantienen sus recepciones. Cuatro jugadores reales: pendiente.

## Alcance contrastado con la arquitectura

Se consultaron Plan_20261117 (Salas.csv, Conexiones.csv y planos por cota), Auditoria_20261117 y los informes y receptores de Etapas 1–5. El generador CiudadDelSolSolInvertidoPreviewGenerator se leyó como referencia; no se ejecutó.

VS03 conecta B02, galería perimetral inferior, con el acceso de D03. Se conservaron sus ejes, extremos y cotas documentales:

| Tramo interno de VS03 | Coordenadas del piso | Función |
|---|---|---|
| Aproximación B02 | (24,−30,45) → (−9,−30,45) → (−9,−30,36) | Continuación de VS02 y enlace con VS05 |
| Vuelo 1 | (−9,−30,36) → (−9,−32,30) | Descenso de 2 m en 6 m horizontales |
| Vuelo 2 | (−9,−32,30) → (−9,−34,24) | Descenso de 2 m en 6 m horizontales |
| Vuelo 3 | (−9,−34,24) → (−9,−36,18) | Descenso de 2 m en 6 m horizontales |
| Vuelo 4 | (−9,−36,18) → (−9,−38,12) | Descenso de 2 m en 6 m horizontales |
| Desembarco y acceso | (−9,−38,12) → (−9,−38,9) → (18,−38,9) | Continuidad hasta el ancla del acceso D03 |

Longitud documental: **97,30 m**. Desnivel: **8 m**. Estos subtramos no son conexiones nuevas ni nuevos IDs del dossier. No se recibió la cámara final completa, VS06, P01 ni el resto del piso profundo. No se construyeron nuevas salas.

## Problemas demostrados y correcciones

### Protección y anchura

La primera recepción real encontró 395 estaciones del eje con suelo, cápsula, huella y NavMesh satisfactorios, pero 26 secciones muestreadas con borde lateral no protegido. Ambas rutas nativas eran completas; eso no bastaba para aprobar la conexión. Las cuatro escaleras de 6 m carecían de protección lateral adecuada hacia el piso profundo.

Se construyó exclusivamente dentro del acceso VS03:

- **4 Staircase_01 ensanchadas de 6 a 10 m**, conservando sus posiciones, alturas y desniveles. No se eliminaron pilares ni estructuras monumentales.
- **2 ampliaciones de piedra**, cada una de 2 × 6 m en planta, a ambos lados de la cabecera existente en Y=−30. Centros de planta: (−13,39) y (−5,39). No se duplicó el suelo original ni se añadió suelo en la cámara final.
- **12 protecciones solares**: ocho paneles inclinados, dos laterales de cabecera y dos retornos cortos que cierran solamente las franjas añadidas. El acceso central permanece abierto.

Se reutilizaron los prefabs Eternal Temple **Staircase_01**, **Platform_Base_01**, **Railing_Staircase_01** y **Railing_01**, con sus materiales. Los prefabs originales no se modificaron. Las mallas adaptadas de las barandas son seis assets independientes; la colisión es un parapeto continuo de 1 m de espesor y 1,5 m de altura total, con 0,10 m embebidos según el eje nominal. No depende de los huecos ornamentales del modelo para proteger al jugador.

Las posiciones y escalas efectivas están en [Escaleras.csv](Escaleras.csv), [Ampliaciones.csv](Ampliaciones.csv) y [Protecciones.csv](Protecciones.csv). El suelo profundo y sus pilares se conservaron. El ensanche de 8 m ensayado inicialmente interfería con posiciones nativas existentes; se descartó sin guardar.

### Dos esquinas de navegación

La comprobación adicional con radio Humanoid **0,57 m** encontró roces en los extremos de Proteccion_12, junto a (−6,−30,42,05), y Proteccion_7, junto a (−4,95,−38,12). La cápsula Warrior de 0,32 m pasaba, pero no se declaró equivalente a una cápsula mayor.

Se añadieron **dos NavMeshModifierVolume Not Walkable**, exclusivamente para agente 0, como margen conservador de esas esquinas. Sus huellas son de **1,4 × 1,4 m**, derivadas de redondear radio + un vóxel al siguiente múltiplo de vóxel; altura 3 m. Centros mundiales: **(−6,−29,42,05)** y **(−4,95,−37,12)**. Parámetros completos: [MargenesNav.csv](MargenesNav.csv).

No tienen colliders ni mallas, no crean pisos o enlaces y no alteran el movimiento del Warrior. Restringen ligeramente las trayectorias de IA alrededor de obstáculos físicos existentes. La candidata se rechazó hasta comprobar la cápsula completa. Los 59 NavMeshModifier originales permanecen; estos dos componentes son NavMeshModifierVolume nuevos, hijos de la intervención.

### Receptor y preservación de la navegación

Se corrigió un defecto demostrado del receptor compartido: InsideCorridor tenía un límite histórico fijo en Y=−34,5, incompatible con el destino documental Y=−38. Ahora deriva el intervalo vertical de los puntos del plan seleccionado. Se conservó el margen exterior de 0,5 m y, especialmente, la condición vertical por segmento de **0,36 m**, las comprobaciones físicas, el filtro de agente y los controles negativos. La regresión comprueba puntos reales aceptados y plantas desplazadas ±4 m rechazadas.

El NavMeshData compartido necesitó actualización: con el dato anterior, el camino global cortaba diagonalmente el pie del nuevo parapeto. La versión final pasa **directamente** los raycasts nativos, la ruta global y la física en ambos sentidos, sin recurrir a puntos obligatorios para ocultar el corte. Una comprobación intermedia por subcaminos se archivó en Backups y se retiró de Assets; no forma parte del receptor final.

Antes de persistir se compararon 105.093 testigos iniciales fuera del volumen de intervención y 42 referencias de rutas exteriores. Siete testigos cambiaron solamente su altura interpolada de NavMesh, sobre el mismo Platform_Base_01 físico a Y=−38. Se exigieron posición horizontal conservada a 1 mm, mismo collider de apoyo, misma posición y normal físicas a 1 mm y cápsula despejada, usando la proyección original de 0,35 m. No se incrementaron tolerancias ni se aceptó un hueco por proximidad. La comparación inversa también pasó. Las equivalencias concretas están en Cobertura_Original_en_candidata.txt y Cobertura_Candidata_en_original.txt; la corrección posterior de esquinas pasó sus comparaciones sin equivalencias adicionales.

No se crearon NavMeshSurface ni NavMeshLinks. Se mantuvieron **Humanoid 0, radio 0,57 m, escalón 0,36 m y vóxel 0,10 m**. Se actualizó el asset existente conservando su GUID y sus referencias.

## Recepción real desde disco

Unity **6000.3.23f1**, Editor gráfico, Edit Mode; PhysX y NavMesh nativos cargados desde la escena definitiva. No hubo Play Mode, Host, simulación de movimiento del Warrior ni clientes multijugador.

| Comprobación | Resultado final |
|---|---|
| VS03 por puntos de paso originales | 1/1, caminos completos y física satisfactoria en ambos sentidos |
| VS03 por camino global automático | 1/1, raycasts resueltos por las comprobaciones originales; sin refinamiento especial |
| Perfil a 0,10 m | **978 estaciones, 0 fallos** |
| Flancos de cuatro escaleras, cada 0,10 m | **488 comprobaciones, 0 riesgos detectados** |
| Cápsula Humanoid sobre camino global | **366 muestras por sentido, 0 fallos de cápsula o barrido** |
| Controles de obstáculos y plantas incompatibles | Rechazados correctamente |
| Digest de triangulación después de recargar | Igual al guardado recibido |
| Escena después de la recepción | Limpia, sin cambios sin guardar |

Métricas del recorrido completo:

- **Anchura útil mínima muestreada: 4,74 m.** Es una cota inferior del recorrido completo, incluida la aproximación; no significa que las escaleras ensanchadas midan solamente 4,74 m. El buscador reutilizado tiene un límite transversal de seis metros.
- **Altura libre mínima medida: 2,45 m**, bajo arquitectura existente. La prueba Humanoid utiliza al menos la altura original del Warrior y la configurada para el agente; esto no certifica enemigos de otras dimensiones.
- **Pendiente física máxima: 18,92°**. El eje nominal de los vuelos tiene pendiente de 18,43°; se informa la superficie física medida.
- Cuatro cápsulas estáticas Warrior despejadas en las secciones evaluadas. No son cuatro jugadores reales.

### Regresiones después de recargar la versión final

| Etapa | Recepción |
|---|---|
| 1 | 1/1 |
| 2 | 6/6 |
| 3 | 3/3 |
| 4 | 2/2, incluidas sus comprobaciones laterales |
| 5 | 2/2 por recorrido seleccionado y 2/2 por camino global natural |

Los informes completos están en `Backups/CiudadDelSol/Interior/Etapa6_20261118/Recepcion_recarga/`. `Resultado_final.txt` registra **Recarga=True; Recepciones=True; CapsulaHumanoid=True; Cotas=True; EscenaLimpia=True**. Construccion.json registra saved=true, confirmedReload=true, originalObjectsUnchanged=true y regressionsPassed=true.

## Inspección visual

Se inspeccionaron en Unity el anillo de aproximación, la cabecera, los cuatro vuelos, el pie, el acceso profundo y la contracara occidental. Se conservaron once capturas reales, incluidas comparaciones anteriores y posteriores, en [Galeria.html](Galeria.html).

No se observaron piezas flotantes, huecos de pavimento, materiales ausentes o invasiones del eje que requieran una intervención adicional. Se revisaron las uniones de parapetos, las franjas de cabecera y los extremos libres. Las vigas y pilares existentes se conservaron; la altura libre se comprobó físicamente. Se utilizó aislamiento temporal de visualización, restaurado al finalizar. La escena no se guardó con objetos desactivados para tomar capturas.

## Archivos y respaldos

Modificados:

- Assets/Scenes/Dungeon_Isla_del_Sol.unity: cuatro escalas locales y raíz CiudadDelSol_Etapa6_VS03 con dos ampliaciones, doce protecciones y dos márgenes NavMesh.
- Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset: navegación actualizada y recibida; .meta original intacto.
- Assets/ROM/Editor/CiudadDelSolInteriorStageTwo.cs: intervalo de cotas derivado de la conexión, sin cambiar tolerancias por segmento.

Nuevos:

- CiudadDelSolInteriorStageSix.cs: selección de VS03, solicitud explícita de un solo uso, menú y encuadres visuales.
- CiudadDelSolInteriorStageSix.Construction.cs: construcción local, candidata, preservación y recepción reutilizando las comprobaciones existentes.
- CiudadDelSolInteriorStageSix.CornerMargins.cs: instalación de los dos márgenes justificados y actualización protegida.
- CiudadDelSolInteriorStageSix.Verification.cs: recarga desde disco, cápsula Humanoid y regresión de cotas.
- Sus cuatro .meta; seis mallas y .meta en Assets/ROM/Generated/InteriorEtapa6_20261118T064400292/, y el dossier actual.

La herramienta es exclusiva de Editor. Menú de comprobación: **ROM → Etapa 6 interior → Recibir VS03**. No reconstruye una intervención existente ni inicia Play. La solicitud automática solo se ejecuta al existir un archivo explícito con nonce; los archivos consumidos quedan como evidencia. Los errores intermedios se conservaron en los respaldos, no se borraron globalmente de los registros.

Respaldo inicial verificable: Backups/CiudadDelSol/Interior/Etapa6_20261118/Antes_SHA256.json y copias completas. Existe otro punto de recuperación AntesMargenes. El manifiesto final está en [SHA256.json](SHA256.json). Los archivos compartidos de Etapas 3–5 se conservaron; la única corrección del motor compartido es la cota documentada. No se modificaron prefabs originales, Mirror, controles, combate, HUD, XP, progresión ni población exterior.

La compilación del ensamblado Editor con Roslyn y referencias del proyecto terminó con código 0. Unity importó la versión final y ejecutó sus métodos. Persisten siete avisos CS0618 preexistentes de una herramienta NavMeshQuery ajena a esta intervención; la consola final observada tiene cero errores. No se introdujeron consultas experimentales NavMeshQuery nuevas.

Documentación local consultada del paquete AI Navigation: **NavMeshSurface.md, NavMeshAgent.md y NavMeshModifierVolume.md**. No se instalaron paquetes. No se hicieron commits, pushes ni cambios en staging.

## Límites y siguiente etapa

La **recepción técnica individual de Etapa 6 está completa**. Esto acredita el recorrido seleccionado, no toda la cámara final ni todas las zonas del complejo.

Siguen pendientes cuatro Warriors reales, cruces en sentidos opuestos y comportamiento de cámaras/IA dinámica en una partida. La comprobación geométrica de cápsulas no sustituye esa prueba. También se conservan las limitaciones históricas de telemetría manual de Etapa 1 y del atajo inverso de VS01: esta intervención no las declara resueltas.

Recomendación de Etapa 7: auditar **VS06, D03→P01**, desde (18,−38,9) por (12,−38,9) hasta (9,−38,15), antes de autorizar la delimitación del encuentro final. Es una referencia documental pendiente de inspección, no una ruta aprobada aquí. No se comenzó su construcción.

Plano actualizado: [Plano.svg](Plano.svg). Resumen tabular: [Recepcion.csv](Recepcion.csv).
