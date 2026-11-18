# Ciudad del Sol — plano arquitectónico del interior

**Revisión para aprobación, 17/11/2026. Diseño y levantamiento; no construcción.**

## Alcance y resultado

La escena actual ya contiene un interior monumental construido. Se conserva esa arquitectura como base, en lugar de diseñar otro subsuelo que se superponga. El paquete contiene un plano general, siete láminas por cota, cortes de circulación y fichas de **68 espacios**: 54 naves/galerías abovedadas existentes y 14 atrios, cámaras, anillos, puentes, descansos y una subzona propuesta para el jefe. No son 68 habitaciones cerradas nuevas. Las conexiones H/LA son ejes funcionales propuestos sobre la huella existente, no túneles ni aberturas ya certificadas.

**La exactitud corresponde a las coordenadas y dimensiones extraídas de archivos. La transitabilidad completa aún NO está certificada.** No se abrió Unity, no se calcularon rutas reales y no se afirmó que un trazado atraviese sin colisiones toda la arquitectura. Las cajas de obstáculos son una representación parcial: faltan las superficies exactas de algunos MeshCollider importados. El plano es una base métrica para revisar y aprobar, no una autorización automática para colocar geometría, spawns o jefes.

## Cómo abrir el paquete

Abrir **Indice.html** en Windows. Permite cambiar de nivel y consultar todas las tablas. Los SVG se pueden abrir por separado en Edge, Chrome o Inkscape, con sus medidas relativas conservadas. En las láminas, pasar el puntero sobre una sala, módulo u obstáculo muestra su referencia. Salas.csv y Conexiones.csv contienen las tablas completas; Conexiones.json conserva cada vértice XYZ de todos los ejes.

- [Plano general](Plano_general.svg).
- [Entrada Y20](Nivel_20.svg), [vestíbulo Y4](Nivel_4.svg).
- [Planta principal Y−16](Nivel_menos16.svg).
- [Y−24](Nivel_menos24.svg), [Y−30](Nivel_menos30.svg), [Y−34](Nivel_menos34.svg), [Y−38](Nivel_menos38.svg).
- [Cortes y descensos](Cortes_y_descensos.svg).

## Convenciones y evidencia

**E**: existente, leído de YAML de escena, prefab o datos de vértices de una malla. **P**: propuesta de uso, eje, ancho libre objetivo o límite funcional. **H**: sondeo histórico de Backups/CiudadDelSol/AuditoriaEspacial/Muestras.tsv; no se presenta como una prueba actual. Norte +Z, este +X, arriba +Y; 1 unidad Unity = 1 m. Los centros están en la cota del piso y son centros geométricos, **no puntos garantizados de spawn**. Los centros de B01/B02 caen en el vacío del anillo; esto es deliberado.

Se leyó la escena completa de 96.932.348 bytes y se resolvieron sus grupos y transformaciones. Las raíces interiores integradas conservan posición cero, rotación identidad y escala uno. Se extrajeron 4.032 registros de módulos Platform_Base_01 activos, incluidos basamentos y cubiertas que se etiquetan aparte; 108 mallas de bóvedas/nervaduras correspondientes a 54 volúmenes; 11.342 instancias enlazadas de prefabs de los conjuntos interiores (incluye piezas inactivas); y 10.714 cajas físicas disponibles. No se deben interpretar esos conteos como objetos transitables ni activos simultáneos para renderizado.

La unión nominal del piso principal a Y−16 tiene 2.791 celdas completas de 6×6 m, **100.476 m² de huella modular**, dentro de X[−162,240], Z[−162,216], una envolvente de 402×378 m. No es el área libre de combate ni el área del NavMesh. No sumar áreas de anillos, puentes y subzonas a la de sus cámaras: se superponen o pertenecen al mismo recinto.

Se contrastó el exterior con el informe del atlas aprobado, cuyo SHA-256 de escena coincide exactamente con la escena actual: **45b24571909cdbb425043fec803381956d0d954f61c961ae2c733f109b8fff35**. Los archivos de integridad contienen las huellas completas de la escena y de ambos NavMeshData.

## Exterior real y acceso

| Sector exterior | Pivote real XYZ | Caja física X | Caja física Z | Cotas envolventes de suelo |
| --- | --- | --- | --- | --- |
| 01 - Entrada Portico Solar | {"x":-126,"y":0,"z":-120} | -162..-90 | -162..-78 | -2..4.002 |
| 02 - Jardines Dorados | {"x":-102,"y":4,"z":-18} | -150..-54 | -66..30 | 2..8 |
| 03 - Plaza del Eclipse | {"x":18,"y":12,"z":12} | -42..78 | -48..72 | 10..16 |
| 04 - Archivos Solares | {"x":-108,"y":20,"z":108} | -150..-66 | 72..144 | 18..28.002 |
| 05 - Claustro del Eclipse | {"x":18,"y":28,"z":144} | -36..72 | 96..192 | 26..36.002 |
| 06 - Santuario Profano | {"x":150,"y":4,"z":-78} | 102..198 | -132..-24 | 2..12.002 |
| 07 - El Sol Corrompido | {"x":180,"y":36,"z":156} | 120..240 | 96..216 | 34..44.002 |

Estas cajas proceden de la geometría útil del atlas aprobado, no de pivotes inventados. Sus intervalos Y incluyen espesor y plataformas elevadas; no son una cota uniforme de circulación.

| Conexión exterior conservada | Ancho nominal del generador | X min..max | Z min..max |
| --- | --- | --- | --- |
| 01-02 - Ascenso a jardines - ancho util 12 | 12 | -120..-108 | -78.336..-66 |
| 01-06 - Ruta secundaria meridional - ancho util 6 | 6 | -96..108 | -135..-117 |
| 02-03 - Puente de la plaza - ancho util 12 | 12 | -54.336..-42 | 6..18 |
| 03-04 - Galeria occidental - ancho util 12 | 12 | -66..-30 | 36..90 |
| 03-05 - Gran escalinata del claustro - ancho util 24 | 24 | -24..66 | 60..96 |
| 03-06 - Descenso al santuario - ancho util 12 | 12 | 78..102 | -60..18 |
| 04-05 - Galeria de los archivos - ancho util 12 | 12 | -66..-36 | 114..126 |
| 05-07 - Puente ceremonial - ancho util 12 | 12 | 72..120 | 114..162 |
| 06-07 - Ascenso oriental - ancho util 12 | 12 | 198..234 | -60..114 |

La pasarela inicial del Warrior está en (30,−1.95,−206), mirando +Z. Es un punto EXTERIOR y no el acceso directo al subsuelo. El acceso arquitectónico interior está en el conjunto **Umbral_De_Las_Profundidades**, conectado físicamente al Santuario. Se conserva el acceso escalonado por X≈129/132: de (129,4,−42) a Y12 y luego Y20, hasta la arena octagonal centrada en (162,20,42). Desde ella, las galerías existentes conducen a la cabecera **(162,20,−18)**. La escalinata central baja a **(162,4,6)**. El vestíbulo ocupa X[150,174], Z[6,30], con piso Y4; la cubierta parcial de la cripta deja un intradós de losa en Y13.38, es decir 9.38 m sobre ese piso. No se debe confundir el techo de la losa con la altura libre bajo el arco.

Luego se sigue (162,4,33.45) → (150,4,33.45) → (150,4,36). Cinco escaleras reales, cada una de 4 m de desnivel y 6 m de avance, alternan con descansos de 6 m. Terminan en (150,−16,90), continúan al primer descanso de 24×24 m, X[144,168], Z[90,114]. Las coordenadas geométricas se contrastaron con las instancias guardadas; no se reutilizaron pivotes de prefabs como si fueran superficies.

**Ruinas intermedias:** quedan fuera de todo espacio propuesto, recorrido de IA, puesto o acceso. El descenso utiliza únicamente la arquitectura jugable específica del Umbral y el subsuelo, no las cubiertas de ambientación.

## Organización arquitectónica propuesta

1. **Recepción:** A00 → A01 → A02 → A03. La escalinata y el vestíbulo se mantienen libres de encuentros que bloqueen el tránsito. A03 es una zona de orientación y recuperación propuesta, sin alterar ahora las reglas del juego.
2. **Gran complejo Y−16:** se reutilizan las cuatro naves monumentales M01–M04, las 34 naves R y las 16 galerías G. Sus volúmenes abiertos evitan una secuencia de cajas pequeñas. Las funciones de combate, exploración y cámaras rituales se asignan sobre espacios ya construidos; no implican añadir paredes.
3. **Exploración vertical opcional:** D01 y D02, a Y−34, tienen accesos físicos independientes desde Y−16, terrazas a Y−24/−30 y conexión inferior VD01. Forman un lazo secundario sin obligar a cruzar pisos superpuestos ni atravesar salas inacabadas.
4. **Descenso final:** desde el eje superior se llega al descanso (45,−16,−15), luego a B01 Y−24, B02 Y−30 y D03 Y−38. Los puentes B03 y B04 cruzan en proyección cerca de (21,39), con **6 m de diferencia de piso**; no hay unión en ese cruce.
5. **Final y retorno:** D03 alberga la propuesta de jefe. Se vuelve por VS03 → VS02 → VS01 → AC04 → AC03 → AC02 → AC01 en sentido inverso. No se exige un teletransporte ni un nuevo acceso. Un mecanismo futuro de retorno puede habilitar ese itinerario al terminar el jefe; su implementación queda fuera del diseño actual.

El orden de visita de las alas es una propuesta de progresión, no un encierro territorial: Jardines y cámaras occidentales con corrupción leve; Archivos/Claustro como exploración y ceremonia; Santuario y ala del Sol con corrupción creciente; núcleo final violeta intenso. Las conexiones H forman un esqueleto nominal para llegar a todos los espacios y LA1–LA4 añaden lazos. **Son trazados preliminares sobre suelo modular, no rutas seguras demostradas**: cada uno requiere revisar pilares, barandas y arcos en Unity. No se crearán pisos ni enlaces para hacerlos pasar.

## Fichas de salas y espacios

Para las 54 naves, ancho/fondo son las huellas exactas de sus bóvedas guardadas; el piso Y−16 está respaldado por módulos físicos en toda esa huella. El techo E al centro se obtuvo intersectando verticalmente los triángulos decodificados de bóveda y nervaduras de la sala. El arranque común está próximo a Y−7.244: 8.756 m por encima del piso. Ese valor NO elimina obstáculos inferiores de otras piezas. La envolvente superior incluye espesor de la bóveda y no se confunde con el intradós.

En los espacios D/B/A con cubierta variable, los JSON incluyen la primera caja disponible sobre el centro y el sondeo H cuando existe. Es información **parcial**, porque algunos arcos y la corona tienen MeshCollider importados. D01 tiene una cubierta de cajas alrededor de Y−7.244; en D02, H registró un arco a 13.37 m sobre el piso en (99,−34,−9), por debajo de esa cubierta. En D03, el suelo del puente Y−30 deja un intradós Y−32 y algunas vigas descienden más. No asignar un techo plano ficticio a esas cámaras.

| ID | Nombre / referencia | Centro X,Y,Z (piso) | Huella X×Z m | Techo de nave al centro E | Forma / función propuesta | Conexiones |
| --- | --- | --- | --- | --- | --- | --- |
| R01 | Nave -114/114 (18×18) | (-105, -16, 123) | 18 × 18 | 2 | Sala de exploración / recuperación entre encuentros, según balance | H22, H23 |
| R02 | Nave -120/-30 (30×18) | (-105, -16, -21) | 30 × 18 | 0.5 | Cámara de exploración y combate opcional; conservar paso transversal | H38, H39, H40, H41 |
| R03 | Nave -120/-54 (30×18) | (-105, -16, -45) | 30 × 18 | 0.5 | Cámara de exploración y combate opcional; conservar paso transversal | H39, H42 |
| R04 | Nave -120/-6 (30×18) | (-105, -16, 3) | 30 × 18 | 0.5 | Cámara de exploración y combate opcional; conservar paso transversal | H37, H38 |
| R05 | Nave -120/30 (30×36) | (-105, -16, 48) | 30 × 36 | 1.8 | Salón ceremonial: encuentro amplio y exploración perimetral | H28, H29, H37 |
| R06 | Nave -126/-150 (18×66) | (-117, -16, -117) | 18 × 66 | -2.9 | Salón ceremonial: encuentro amplio y exploración perimetral | H43, H44, H45 |
| R07 | Nave -138/-54 (18×66) | (-129, -16, -21) | 18 × 66 | 0.5 | Salón ceremonial: encuentro amplio y exploración perimetral | H40 |
| R08 | Nave -138/30 (18×24) | (-129, -16, 42) | 18 × 24 | 1.8 | Sala de exploración / recuperación entre encuentros, según balance | H27, H28 |
| R09 | Nave -138/60 (18×18) | (-129, -16, 69) | 18 × 18 | 2 | Cámara de reliquias: objetivo secundario propuesto, sin bloquear ruta | H26, H27 |
| R10 | Nave -150/-150 (18×18) | (-141, -16, -141) | 18 × 18 | -2.9 | Sala de exploración / recuperación entre encuentros, según balance | H45 |
| R11 | Nave -24/90 (18×78) | (-15, -16, 129) | 18 × 78 | 1.67 | Salón ceremonial: encuentro amplio y exploración perimetral | H17, H19 |
| R12 | Nave -42/-12 (18×18) | (-33, -16, -3) | 18 × 18 | -2.9 | Cámara de reliquias: objetivo secundario propuesto, sin bloquear ruta | H33, H34 |
| R13 | Nave -42/-36 (18×18) | (-33, -16, -27) | 18 × 18 | 2 | Sala de exploración / recuperación entre encuentros, según balance | H34 |
| R14 | Nave -42/12 (18×18) | (-33, -16, 21) | 18 × 18 | 2 | Sala de exploración / recuperación entre encuentros, según balance | H32, H33 |
| R15 | Nave -42/36 (18×30) | (-33, -16, 51) | 18 × 30 | 2 | Cámara de exploración y combate opcional; conservar paso transversal | H31, H32 |
| R16 | Nave -6/96 (18×30) | (3, -16, 111) | 18 × 30 | 2 | Cámara de exploración y combate opcional; conservar paso transversal | H16, H17 |
| R17 | Nave -66/114 (30×18) | (-51, -16, 123) | 30 × 18 | 2 | Cámara de exploración y combate opcional; conservar paso transversal | H19, H20, H21 |
| R18 | Nave -72/30 (18×30) | (-63, -16, 45) | 18 × 30 | 0.5 | Cámara de exploración y combate opcional; conservar paso transversal | H30, H31 |
| R19 | Nave -90/-126 (42×54) | (-69, -16, -99) | 42 × 54 | 2 | Salón ceremonial: encuentro amplio y exploración perimetral | H42, H43 |
| R20 | Nave 0/78 (42×18) | (21, -16, 87) | 42 × 18 | 2 | Cámara de exploración y combate opcional; conservar paso transversal | H18 |
| R21 | Nave 102/102 (30×42) | (117, -16, 123) | 30 × 42 | 1.67 | Salón ceremonial: encuentro amplio y exploración perimetral | H12, H13 |
| R22 | Nave 114/-120 (18×66) | (123, -16, -87) | 18 × 66 | 0.5 | Salón ceremonial: encuentro amplio y exploración perimetral | H54 |
| R23 | Nave 114/156 (18×18) | (123, -16, 165) | 18 × 18 | 2 | Sala de exploración / recuperación entre encuentros, según balance | H05, H06, H12 |
| R24 | Nave 132/-120 (30×18) | (147, -16, -111) | 30 × 18 | 0.5 | Cámara de exploración y combate opcional; conservar paso transversal | H53 |
| R25 | Nave 132/-18 (18×42) | (141, -16, 3) | 18 × 42 | -3.23 | Cámara de exploración y combate opcional; conservar paso transversal | H46, H47 |
| R26 | Nave 132/120 (18×78) | (141, -16, 159) | 18 × 78 | 1.67 | Salón ceremonial: encuentro amplio y exploración perimetral | H04, H05 |
| R27 | Nave 150/180 (30×18) | (165, -16, 189) | 30 × 18 | 2 | Cámara de exploración y combate opcional; conservar paso transversal | H10 |
| R28 | Nave 156/-48 (30×18) | (171, -16, -39) | 30 × 18 | 0.5 | Cámara de exploración y combate opcional; conservar paso transversal | H49, H50 |
| R29 | Nave 162/-120 (18×18) | (171, -16, -111) | 18 × 18 | 0.5 | Sala de exploración / recuperación entre encuentros, según balance | H52, H53 |
| R30 | Nave 174/120 (30×18) | (189, -16, 129) | 30 × 18 | 2 | Cámara de exploración y combate opcional; conservar paso transversal | H01, H02, H03 |
| R31 | Nave 180/186 (18×18) | (189, -16, 195) | 18 × 18 | 2 | Sala de exploración / recuperación entre encuentros, según balance | H09, H10 |
| R32 | Nave 198/-48 (18×66) | (207, -16, -15) | 18 × 66 | 2 | Salón ceremonial: encuentro amplio y exploración perimetral | H48 |
| R33 | Nave 36/162 (30×12) | (51, -16, 168) | 30 × 12 | 1.8 | Cámara de reliquias: objetivo secundario propuesto, sin bloquear ruta | H36 |
| R34 | Nave 48/78 (18×78) | (57, -16, 117) | 18 × 78 | 1.67 | Salón ceremonial: encuentro amplio y exploración perimetral | H14, H15 |
| G01 | E2 Galería este hacia el sur | (213, -16, 93) | 18 × 30 | 0.5 | Galería secundaria y redistribución | H11 |
| G02 | E3 Galería oriental del Sol | (213, -16, 168) | 18 × 36 | 0.5 | Galería secundaria y redistribución | H08 |
| G03 | E4 Galería del Sol hacia el oeste | (165, -16, 144) | 18 × 36 | 0.5 | Galería secundaria y redistribución | H03, H04, H07 |
| G04 | Galería del borde oriental | (213, -16, 129) | 18 × 30 | 0.6 | Galería secundaria y redistribución | H02, H11 |
| G05 | N5 Galería norte hacia el Sol | (75, -16, 120) | 18 × 36 | 0.5 | Galería secundaria y redistribución | H13, H14 |
| G06 | Nave suroeste bajo Pórtico Solar | (-141, -16, -102) | 18 × 36 | -3.23 | Galería secundaria y redistribución | H44 |
| G07 | O3 Galería norte de Jardines | (-81, -16, 81) | 18 × 30 | 0.5 | Galería secundaria y redistribución | H25 |
| G08 | O6 Galería occidental de Archivos | (-129, -16, 105) | 18 × 42 | 0.5 | Galería secundaria y redistribución | H23, H24 |
| G09 | S2 Galería junto al Santuario | (171, -16, -78) | 18 × 36 | 0.5 | Galería secundaria y redistribución | H51, H52 |
| G10 | Transición norte de Jardines Dorados | (-81, -16, 45) | 18 × 30 | -1.15 | Galería secundaria y redistribución | H29, H30 |
| G11 | E6 Entrada occidental al ala del Sol | (105, -16, 165) | 18 × 30 | 0.5 | Galería secundaria y redistribución | H06 |
| G12 | N1 Ala oeste del Claustro | (-45, -16, 93) | 18 × 30 | 0.5 | Galería secundaria y redistribución | H20 |
| G13 | N3 Acceso sur del Claustro | (27, -16, 114) | 18 × 24 | 0.5 | Galería secundaria y redistribución | H15, H16, H18, H35 |
| G14 | O4 Jardines hacia Archivos | (-81, -16, 117) | 18 × 30 | 0.5 | Galería secundaria y redistribución | H21, H22 |
| G15 | O5 Patio bajo Archivos | (-105, -16, 90) | 18 × 36 | 0.5 | Galería secundaria y redistribución | H24, H25, H26 |
| G16 | S3 Enlace oriental del Santuario | (183, -16, -6) | 18 × 36 | -2.9 | Galería secundaria y redistribución | H47, H48, H49 |
| M01 | Claustro del Eclipse | (21, -16, 153) | 30 × 42 | 1.6 | Claustro ceremonial: encuentro cooperativo y rutas laterales | H35, H36, LA1, LA4 |
| M02 | El Sol Corrompido | (189, -16, 162) | 18 × 36 | 0.5 | Templo profundo del Sol: encuentro importante previo al núcleo | H07, H08, H09, LA1, LA2 |
| M03 | Jardines Dorados | (-81, -16, -18) | 18 × 60 | 0.32 | Nave de Jardines: gran galería de exploración y primer encuentro interior propuesto | H41, LA2, LA3 |
| M04 | Santuario Profano | (147, -16, -63) | 18 × 66 | 0.65 | Santuario bajo el Santuario: sala ritual de dificultad intermedia | H50, H51, H54, LA3, LA4 |
| A00 | Atrio octagonal del Umbral | (162, 20, 42) | 84 × 84 | Variable; ver ficha | Recepción exterior y acceso único legible | AC01, AC02 |
| A01 | Vestíbulo bajo la arena | (162, 4, 18) | 24 × 24 | 13.38 | Recepción interior y reagrupación | AC02, AC03 |
| A02 | Atrio del enlace | (159, 4, 33) | 30 × 6 | Variable; ver ficha | Giro hacia escalera; no encuentro | AC03, AC04 |
| A03 | Gran descanso inferior | (156, -16, 102) | 24 × 24 | Variable; ver ficha | Orientación y punto de recuperación propuesto | AC04, VC01, VE01, VS01, H01, H46 |
| D01 | Cámara Centro Oriental | (105, -34, 63) | 54 × 66 | Variable; ver ficha | Encuentro importante opcional y exploración vertical | VC02, VD01 |
| D02 | Cámara de las arcadas del Sureste | (99, -34, -9) | 66 × 78 | Variable; ver ficha | Sala ceremonial y encuentro de coordinación | VE02, VD01 |
| D03 | Gran Cámara del Sol Invertido | (18, -38, 18) | 84 × 84 | Variable; ver ficha | Cámara final; no asumir 84 × 84 totalmente despejados | VS03, VS06 |
| B01 | Galería perimetral superior del Sol | (18, -24, 18) | 72 × 72 | Variable; ver ficha | Aproximación final y visión del vacío | VS01, VS02, VS04 |
| B02 | Galería perimetral inferior del Sol | (18, -30, 18) | 60 × 60 | Variable; ver ficha | Recorrido de descenso y ruta de retorno | VS02, VS03, VS05 |
| B03 | Puente este-oeste | (18, -24, 39) | 60 × 6 | Variable; ver ficha | Exploración; no conexión con puente que pasa debajo | VS04 |
| B04 | Puente norte-sur | (21, -30, 18) | 6 × 48 | Variable; ver ficha | Exploración en otra cota | VS05 |
| B05 | Terraza del Centro Oriental | (84, -24, 90) | 12 × 12 | Variable; ver ficha | Descanso protegido; no grupo permanente | VC01, VC02 |
| B06 | Terraza del Sureste | (120, -30, -42) | 24 × 12 | Variable; ver ficha | Giro del descenso | VE01, VE02 |
| P01 | Área candidata del jefe | (9, -38, 15) | 18 × 30 | Variable; ver ficha | Combate principal del jefe; conservar circulación periférica y comprobar volumen completo | VS06 |

### Entradas, salidas y anchos

Salas.csv enumera, para cada espacio, todos sus IDs de conexión y los XYZ de anclaje. Conexiones.json detalla los ejes completos. **Los anclajes de H/LA están sobre centros de módulos de suelo; no se presentan como jambas de puertas existentes.** Antes de construir una puerta nueva habrá que verificar su intersección exacta con el muro y los colliders; no se ha diseñado una perforación ficticia. Las naves son volúmenes abiertos y sus límites de ficha no son necesariamente paredes.

Criterio P: pasos principales/puertas útiles de al menos **6 m de ancho y 5 m de altura**, tramos de combate con 12–18 m de espacio de maniobra cuando la arquitectura lo permita. Mantener la escalera central nominal de 24 m (eje de referencia de 16 m) y las escaleras de subsuelo de 6 m; no estrecharlas. Estas son metas de diseño o dimensiones nominales, no mediciones de holgura de todos los huecos. Se preservan puentes, barandas y nueve conexiones exteriores.

### Cámara final y enemigos grandes

D03 mide 84×84 m de envolvente de piso. **No es una arena libre de 84×84.** Tiene apoyos, puentes y descensos. La franja X18..24 bajo B04 reduce mucho la altura disponible; cerca de la escalera occidental también hay bajos. Se propone P01, X[0,18], Z[0,30], centro **(9,−38,15)** y tamaño **18×30 m**, como subzona de encuentro SIN geometría nueva. Las muestras históricas interiores de esa zona (X≤15) dieron alturas de al menos 29.98 m, pero no constituyen un barrido de cápsula ni incluyen toda la frontera X18; conservar margen respecto del puente.

La posición y el tamaño definitivo del jefe se aprobarán tras comprobar toda su envolvente y sus animaciones. Un agente de radio 0.57 no certifica la movilidad de una criatura de radio mayor: no se debe reutilizar ese dato como prueba para un jefe gigante. El recinto completo permite circulación y exploración; el área activa debe adaptarse a los obstáculos conservados.

## Tabla completa de conexiones

Todas se proponen bidireccionales. Las longitudes son del eje de diseño, no distancias calculadas por NavMesh. Las pendientes son del eje por tramo; no certifican la altura de cada peldaño. Los tramos H/LA usan exclusivamente adyacencia de módulos de suelo en Y−16 y siguen pendientes de obstáculos y conectividad.

| ID | Desde ↔ Hasta | Tipo | Ancho nominal/objetivo m | Longitud eje m | ΔY m | Pendiente máxima eje |
| --- | --- | --- | --- | --- | --- | --- |
| AC01 | EX06 ↔ A00 | Acceso existente por Santuario | 6 | 105.087 | 16 | 33.69° |
| AC02 | A00 ↔ A01 | Escalera monumental: 4 tramos de 4/6; ancho nominal 24 m | 16 | 121.844 | -16 | 33.69° |
| AC03 | A01 ↔ A02 | Paso y giro; salida de cripta nominal 12 m | 6 | 30 | 0 | 0° |
| AC04 | A02 ↔ A03 | 5 escaleras 4/6 y 4 descansos de 6 m | 6 | 75.056 | -20 | 33.69° |
| VC01 | A03 ↔ B05 | Acceso Centro Oriental y descenso 4×2/6 | 6 | 214.298 | -8 | 18.435° |
| VC02 | B05 ↔ D01 | Descenso 3+2 tramos 2/6 | 6 | 103.623 | -10 | 18.435° |
| VE01 | A03 ↔ B06 | Acceso Sureste, 4+3 tramos 2/6 | 6 | 317.272 | -14 | 18.435° |
| VE02 | B06 ↔ D02 | Giro y 2 escaleras 2/6 | 6 | 78.649 | -4 | 18.435° |
| VD01 | D02 ↔ D01 | Conexión inferior entre cámaras | 6 | 87.708 | 0 | 0° |
| VS01 | A03 ↔ B01 | Ruta superior existente y 4 escaleras 2/6 | 6 | 388.298 | -8 | 18.435° |
| VS02 | B01 ↔ B02 | Anillo y 3 escaleras 2/6 | 6 | 63.974 | -6 | 18.435° |
| VS03 | B02 ↔ D03 | Anillo y 4 escaleras 2/6 | 6 | 97.298 | -8 | 18.435° |
| VS04 | B01 ↔ B03 | Puente este-oeste, mismo nivel | 6 | 66 | 0 | 0° |
| VS05 | B02 ↔ B04 | Puente norte-sur, mismo nivel | 6 | 54 | 0 | 0° |
| VS06 | D03 ↔ P01 | Delimitación funcional del encuentro; no puerta nueva | 6 | 12.708 | 0 | 0° |
| H01 | A03 ↔ R30 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 66 | 0 | 0° |
| H02 | R30 ↔ G04 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H03 | R30 ↔ G03 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H04 | G03 ↔ R26 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 42 | 0 | 0° |
| H05 | R26 ↔ R23 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H06 | R23 ↔ G11 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 18 | 0 | 0° |
| H07 | G03 ↔ M02 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 42 | 0 | 0° |
| H08 | M02 ↔ G02 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 30 | 0 | 0° |
| H09 | M02 ↔ R31 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H10 | R31 ↔ R27 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 30 | 0 | 0° |
| H11 | G04 ↔ G01 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H12 | R23 ↔ R21 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 48 | 0 | 0° |
| H13 | R21 ↔ G05 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 42 | 0 | 0° |
| H14 | G05 ↔ R34 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H15 | R34 ↔ G13 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H16 | G13 ↔ R16 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H17 | R16 ↔ R11 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H18 | G13 ↔ R20 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 30 | 0 | 0° |
| H19 | R11 ↔ R17 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 42 | 0 | 0° |
| H20 | R17 ↔ G12 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H21 | R17 ↔ G14 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H22 | G14 ↔ R01 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 30 | 0 | 0° |
| H23 | R01 ↔ G08 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 42 | 0 | 0° |
| H24 | G08 ↔ G15 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 42 | 0 | 0° |
| H25 | G15 ↔ G07 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 30 | 0 | 0° |
| H26 | G15 ↔ R09 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 42 | 0 | 0° |
| H27 | R09 ↔ R08 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 30 | 0 | 0° |
| H28 | R08 ↔ R05 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 30 | 0 | 0° |
| H29 | R05 ↔ G10 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H30 | G10 ↔ R18 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 18 | 0 | 0° |
| H31 | R18 ↔ R15 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H32 | R15 ↔ R14 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 30 | 0 | 0° |
| H33 | R14 ↔ R12 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H34 | R12 ↔ R13 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H35 | G13 ↔ M01 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 48 | 0 | 0° |
| H36 | M01 ↔ R33 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 48 | 0 | 0° |
| H37 | R05 ↔ R04 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 42 | 0 | 0° |
| H38 | R04 ↔ R02 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H39 | R02 ↔ R03 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H40 | R02 ↔ R07 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H41 | R02 ↔ M03 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 30 | 0 | 0° |
| H42 | R03 ↔ R19 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 90 | 0 | 0° |
| H43 | R19 ↔ R06 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 66 | 0 | 0° |
| H44 | R06 ↔ G06 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 42 | 0 | 0° |
| H45 | R06 ↔ R10 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 48 | 0 | 0° |
| H46 | A03 ↔ R25 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 108 | 0 | 0° |
| H47 | R25 ↔ G16 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 48 | 0 | 0° |
| H48 | G16 ↔ R32 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H49 | G16 ↔ R28 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 48 | 0 | 0° |
| H50 | R28 ↔ M04 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 48 | 0 | 0° |
| H51 | M04 ↔ G09 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H52 | G09 ↔ R29 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 36 | 0 | 0° |
| H53 | R29 ↔ R24 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 24 | 0 | 0° |
| H54 | M04 ↔ R22 | Conexión funcional propuesta dentro de huella de piso; NO verifica pilares/barandas | 6 | 48 | 0 | 0° |
| LA1 | M01 ↔ M02 | Lazo secundario propuesto; preservar pasos y obstáculos existentes | 6 | 174 | 0 | 0° |
| LA2 | M02 ↔ M03 | Lazo secundario propuesto; preservar pasos y obstáculos existentes | 6 | 444 | 0 | 0° |
| LA3 | M03 ↔ M04 | Lazo secundario propuesto; preservar pasos y obstáculos existentes | 6 | 336 | 0 | 0° |
| LA4 | M04 ↔ M01 | Lazo secundario propuesto; preservar pasos y obstáculos existentes | 6 | 342 | 0 | 0° |

AC04: 5×(4 m / 6 m), inclinación 33.690°. Los descensos del núcleo usan módulos con 2 m / 6 m: 18.435°. Las cotas intermedias 0, −4, −8 y −12 son descansos del acceso, no plantas independientes del complejo. Y−32 corresponde a algunos basamentos, no se ofrece como planta jugable nueva. Las referencias propuestas entre Y−34 son las conexiones existentes; no conectar cámaras solo porque sus proyecciones se tocan.

## Prefabs, colisiones y navegación

Se conservaron los prefabs originales y sus GUID. El catálogo Prefabs.csv/JSON enumera los 24 tipos de prefab enlazados al inventario. La lectura no contabiliza como prefabs enlazados las piezas desempaquetadas del núcleo; sí aparecen sus pisos/cajas en el levantamiento.

- Platform_Base_01: BoxCollider local 6×2×6 m, centro (0,1,0). Se transformó la superficie superior, no el pivote del asset.
- Staircase_01: MeshCollider convexo nativo; geometría nominal de referencia 6 m de avance y 4 m de desnivel. En los descensos nuevos ya integrados se conserva la escala vertical 0.5 para 2/6, sin editar ahora el prefab.
- Column_Stackable_01: caja 1.369965×4×1.369965 m; no convertir sus apoyos en suelo de circulación.
- Base_Wall_02: caja aproximada 0.799408×8.000611×6 m.
- Base_Ceiling_3m_01: caja 6×3.244185×6 m; su intradós depende de la colocación de cada instancia.
- Railing_01: caja aproximada 0.987709×2.170100×6.000439 m; preservar protección y despeje.
- Arch_Wide_01, Entrance_Arch_01 y Staircase_01 utilizan MeshCollider: su caja visual no equivale a un hueco autorizado.

La escena conserva **dos componentes NavMeshSurface referenciados**: NavMesh_Exterior_CiudadDelSol con GUID de datos 90e6bf2d6b6475544ba727f13b89a23b, y el histórico Raid · Jardines Dorados · Encuentro 01 con datos cbf5bf96383732742a8e503240bd7c57. No se desactivó ninguno ni se sustituyeron datos. Se conservaron los **59 NavMeshModifier**. No se debe hornear para revisar estos documentos.

Humanoid 0: radio 0.57, altura 2, escalón 0.36, pendiente 45°; superficie manual con vóxel 0.10. Warrior CharacterController: altura 2, radio 0.32, pendiente 48°, stepOffset 0.30. La compatibilidad geométrica de diseño usa las condiciones más restrictivas; escalones, bordes, techo y curvas de persecución necesitan comprobación física, no solo ángulo del eje.

## Mirror y cooperación

La arquitectura no requiere otro sistema multijugador. PlayerInstance mantiene su flujo de creación de CharacterInstance y NetworkServer.Spawn con la conexión del jugador. Raid conserva los checkpoints del servidor. RiftEnemyAgent utiliza NetworkIdentity, Health y NavMeshAgent; su opción contextual de persecución de Ciudad del Sol debe seguir usando rutas completas y liberar objetivos inválidos. Este trabajo no modifica ninguno.

El Host local de desarrollo actual configura P_MaxPlayers=1 y NetworkServer.listen=false: **no demuestra cuatro jugadores remotos**. No se cambió ese ensayo. El diseño reserva pasos para cuatro personajes, recuperación y rodeo de enemigos, pero las pruebas reales de sincronización y congestión deberán hacerse después mediante el flujo original autorizado para cuatro conexiones. Evitar ataques o reservas de puestos entre plantas solo por proximidad XZ; conservar comprobación de altura, escena y camino.

## Construcción e integración por etapas, después de aprobar

1. **Revisión métrica y recorrido sin construcción.** Aceptar las funciones y el orden; abrir los planos frente a la escena. Revisar cada conexión H/LA contra todos los colliders, escaleras y huecos reales. Obtener rutas completas en ambos sentidos con el NavMesh existente y barridos de cápsula. Si una abertura no existe, corregir el diseño antes de proponer cambios físicos. Registrar cada bloqueo con objeto y coordenada.
2. **Acceso y primer circuito jugable.** Conservar AC01–AC04 y el descanso A03, verificar retorno, cuatro jugadores y espacio de cámara. Habilitar una sola nave monumental como primer encuentro interior, reutilizando IA/combate existentes, sin bloquear las escaleras. No reconstruir el subsuelo que ya existe.
3. **Alas monumentales y exploración.** Completar funciones de M01–M04, R y G por ala, preservando lazos y zonas de recuperación. La decoración debe mantener los anchos reales; no ampliar artificiosamente el NavMesh.
4. **Verticalidad secundaria.** D01/D02 y VC/VE/VD: probar descensos, rutas alternativas, techos bajos, colisiones de cámaras y persecución bidireccional sin seleccionar otro piso. Conservar barandas y apoyos.
5. **Núcleo final.** VS01–VS06, zona P01 y retorno. Medir la envolvente del jefe antes de fijar escala. Mantener separación de puentes y vacío; ningún enemigo debe aparecer en cubiertas ni ruinas intermedias.
6. **Rendimiento y cooperación final.** Medir por cámara/ala en Profiler y Frame Debugger antes de añadir carga. Usar los volúmenes arquitectónicos como unidades lógicas para futura visibilidad, iluminación y encuentros; no crear nuevas superficies ni modificar red ahora. Probar cuatro humanos, bloqueos mutuos, muerte, desconexión, regreso y reinicio de Raid.

## Comprobaciones realizadas y pendientes

Realizadas: lectura completa de escena y jerarquías, extracción de coordenadas de módulos enlazados y desempaquetados, decodificación de vértices/índices de 108 mallas, medición de 54 huellas y techos de nave, inventario de colisiones disponibles, contraste con atlas aprobado y documentación/generadores, comprobación de suelo modular bajo las 54 naves, consistencia del grafo de diseño de 68 fichas y 73 conexiones, 5095 muestras de apoyo nominal del eje horizontal a intervalos de hasta 1 m (sin barrer obstáculos), separación de plantas y perfiles, y comparación SHA-256 de los tres recursos protegidos antes/después.

**Pendientes:** rutas reales NavMesh, barridos de personaje/cámara, arcos y obstáculos de malla importada completos, huecos efectivos de puertas, despeje de enemigos grandes, protección de todas las aristas, cuatro jugadores y rendimiento en Unity. Ninguna comprobación estática de este paquete sustituye esas pruebas. No se ejecutó compilación: no se crearon ni modificaron scripts del juego o del Editor.

Fuentes técnicas: escena definitiva; prefabs Eternal Temple; generadores Umbral, Access, alas 2–5, NaveJardines, OpenHalls, Expansion/Connections, CentroOriental, ArticulacionSureste, SolInvertido y NucleoConjunto; mallas persistentes de salas; Muestras.tsv histórico; informe del atlas exterior aprobado. Guía consultada: using-unity / initialize-ai-navigation y documentación local NavMeshSurface.md, NavInnerWorkings.md y AreasAndCosts.md.

**No se construyó nada. No se modificaron Assets, escenas, prefabs, NavMesh, colliders, enemigos, HUD, población ni Mirror. Sin commits ni pushes.**
