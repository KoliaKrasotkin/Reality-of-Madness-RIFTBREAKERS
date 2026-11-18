# Ciudad del Sol — auditoría de arquitectura interior

**17/11/2026. Solo lectura del proyecto; no construcción ni prueba ejecutada en Unity.**

## Dictamen

Existe un interior de gran escala. El dossier no debe interpretarse como 68 habitaciones cerradas ni 73 pasos terminados: contiene **67 referencias geométricas presentes y P01, una subzona funcional propuesta**. A00 es el atrio exterior de acceso, no una sala subterránea. Las 54 naves y galerías sí tienen sus dos mallas estructurales con MeshCollider activos y su suelo de referencia; no son nombres vacíos. Eso no acredita que cada acceso, techo, retorno y tránsito cooperativo esté terminado.

**No se propone reconstruir el interior. La primera necesidad es recibir y replantear los recorridos sobre la arquitectura existente.** No se ha demostrado que hagan falta nuevas salas. Hay 15 ejes que cruzan sólidos en los datos examinados, incluido un tramo compartido por el acceso a las cámaras profundas y al Sol Invertido. El objetivo prioritario es resolver su trazado, conservando pilastras, pretiles y suelos.

## Qué se comprobó realmente

- Se leyó la escena completa en disco (96932348 bytes), prefabs, overrides por ID de componente, mallas y generadores. La escena coincide exactamente con la del dossier: SHA-256 **45b24571909cdbb425043fec803381956d0d954f61c961ae2c733f109b8fff35**.
- Se compararon Salas.csv/JSON, Conexiones.csv/JSON, Informe.md y las láminas: se conservan 68 IDs y 73 IDs; las coordenadas y dimensiones CSV/JSON concuerdan. La interpretación de terminación/accesibilidad requiere las correcciones de este informe.
- Se reconstruyeron **8.140 BoxCollider activos no trigger** del ámbito interior/Umbral y se inventariaron **390 MeshCollider activos**. Se contemplaron jerarquías, escalas, rotaciones, activación y overrides individuales. No son cifras de coste ni de objetos renderizados.
- Se decodificaron **108 mallas .asset / 213108 triángulos**, verificando sus hashes y las 108 cadenas de transformación identidad. Se compararon cápsulas con cajas orientadas y con esos triángulos. Los **282 MeshCollider importados restantes** no se cocinaron ni consultaron en PhysX; sus tipos y referencias están inventariados.
- Los ejes se muestrearon cada 0.5 m como máximo. Esto encuentra interferencias de diseño; **no es un barrido continuo**, no simula resolución de escalones del CharacterController, ni reemplaza al NavMesh. La estimación de superficies usa puntos de 1 m² y no prueba cada punto ni cada volumen.
- Se preservaron por SHA los dos NavMeshData, la escena y todos los archivos originales del dossier. No se ejecutó Unity, Play Mode, Host, generadores ni horneado. **Comprobaciones nuevas en el Editor: cero.** Los sondeos de Backups se mantienen como historia, no como recepción actual.

## Clasificación completa de las conexiones

| Clase | Cantidad | Interpretación |
| --- | --- | --- |
| A | 0 | Construida y físicamente transitable demostrada en Unity durante esta auditoría. No se puede otorgar esta categoría sin ejecución real. |
| B | 12 | Construcción asociada presente, pendiente de validación física/NavMesh. |
| C | 15 | Eje del dossier parcial o bloqueado por interferencia detectada en archivos. No demuestra que todo el paso o el NavMesh esté desconectado. |
| D | 46 | Propuesta sin paso físico individual acreditado. **No se confunde falta de prueba con inexistencia física.** Puede existir una alternativa sin obras. |

De los 15 itinerarios de referencia AC/VC/VE/VD/VS, 12 quedan B y tres C. De los 58 H/LA propuestos, 12 quedan C y 46 D. Ningún H/LA se construyó mediante el dossier. Cuando el usuario solicita la categoría “propuesta y todavía inexistente”, aquí se conserva una distinción necesaria: no hay objeto/abertura construida individualizada para estos códigos, pero tampoco se ha demostrado que no exista ya un paso sobre los suelos.

| ID | Desde ↔ Hasta | Clase | Estado | Ancho del dossier (nominal/objetivo) | Longitud de eje m | ΔY | Cajas Warrior | Mallas directas | Avisos de carriles | NavMesh real |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| AC01 | EX06 ↔ A00 | B | Construida, pero pendiente de validación | 6 | 105.087 | 16 | 1 | 0 | 3 | No ejecutado |
| AC02 | A00 ↔ A01 | B | Construida, pero pendiente de validación | 16 | 121.844 | -16 | 4 | 0 | 15 | No ejecutado |
| AC03 | A01 ↔ A02 | B | Construida, pero pendiente de validación | 6 | 30 | 0 | 0 | 0 | 4 | No ejecutado |
| AC04 | A02 ↔ A03 | B | Construida, pero pendiente de validación | 6 | 75.056 | -20 | 0 | 0 | 0 | No ejecutado |
| VC01 | A03 ↔ B05 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 214.298 | -8 | 0 | 1 | 6 | No ejecutado |
| VC02 | B05 ↔ D01 | B | Construida, pero pendiente de validación | 6 | 103.623 | -10 | 0 | 0 | 8 | No ejecutado |
| VE01 | A03 ↔ B06 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 317.272 | -14 | 0 | 1 | 9 | No ejecutado |
| VE02 | B06 ↔ D02 | B | Construida, pero pendiente de validación | 6 | 78.649 | -4 | 0 | 0 | 9 | No ejecutado |
| VD01 | D02 ↔ D01 | B | Construida, pero pendiente de validación | 6 | 87.708 | 0 | 0 | 0 | 1 | No ejecutado |
| VS01 | A03 ↔ B01 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 388.298 | -8 | 0 | 1 | 9 | No ejecutado |
| VS02 | B01 ↔ B02 | B | Construida, pero pendiente de validación | 6 | 63.974 | -6 | 0 | 0 | 1 | No ejecutado |
| VS03 | B02 ↔ D03 | B | Construida, pero pendiente de validación | 6 | 97.298 | -8 | 0 | 0 | 1 | No ejecutado |
| VS04 | B01 ↔ B03 | B | Construida, pero pendiente de validación | 6 | 66 | 0 | 0 | 0 | 20 | No ejecutado |
| VS05 | B02 ↔ B04 | B | Construida, pero pendiente de validación | 6 | 54 | 0 | 0 | 0 | 16 | No ejecutado |
| VS06 | D03 ↔ P01 | B | Construida, pero pendiente de validación | 6 | 12.708 | 0 | 0 | 0 | 0 | No ejecutado |
| H01 | A03 ↔ R30 | D | Propuesta: paso físico individual no acreditado | 6 | 66 | 0 | 0 | 0 | 1 | No ejecutado |
| H02 | R30 ↔ G04 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 0 | No ejecutado |
| H03 | R30 ↔ G03 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H04 | G03 ↔ R26 | D | Propuesta: paso físico individual no acreditado | 6 | 42 | 0 | 0 | 0 | 0 | No ejecutado |
| H05 | R26 ↔ R23 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 24 | 0 | 0 | 2 | 1 | No ejecutado |
| H06 | R23 ↔ G11 | D | Propuesta: paso físico individual no acreditado | 6 | 18 | 0 | 0 | 0 | 0 | No ejecutado |
| H07 | G03 ↔ M02 | D | Propuesta: paso físico individual no acreditado | 6 | 42 | 0 | 0 | 0 | 0 | No ejecutado |
| H08 | M02 ↔ G02 | D | Propuesta: paso físico individual no acreditado | 6 | 30 | 0 | 0 | 0 | 0 | No ejecutado |
| H09 | M02 ↔ R31 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H10 | R31 ↔ R27 | D | Propuesta: paso físico individual no acreditado | 6 | 30 | 0 | 0 | 0 | 0 | No ejecutado |
| H11 | G04 ↔ G01 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H12 | R23 ↔ R21 | D | Propuesta: paso físico individual no acreditado | 6 | 48 | 0 | 0 | 0 | 2 | No ejecutado |
| H13 | R21 ↔ G05 | D | Propuesta: paso físico individual no acreditado | 6 | 42 | 0 | 0 | 0 | 0 | No ejecutado |
| H14 | G05 ↔ R34 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 0 | No ejecutado |
| H15 | R34 ↔ G13 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 36 | 0 | 0 | 1 | 0 | No ejecutado |
| H16 | G13 ↔ R16 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 0 | No ejecutado |
| H17 | R16 ↔ R11 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H18 | G13 ↔ R20 | D | Propuesta: paso físico individual no acreditado | 6 | 30 | 0 | 0 | 0 | 1 | No ejecutado |
| H19 | R11 ↔ R17 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 42 | 0 | 0 | 1 | 1 | No ejecutado |
| H20 | R17 ↔ G12 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H21 | R17 ↔ G14 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H22 | G14 ↔ R01 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 30 | 0 | 0 | 1 | 1 | No ejecutado |
| H23 | R01 ↔ G08 | D | Propuesta: paso físico individual no acreditado | 6 | 42 | 0 | 0 | 0 | 0 | No ejecutado |
| H24 | G08 ↔ G15 | D | Propuesta: paso físico individual no acreditado | 6 | 42 | 0 | 0 | 0 | 0 | No ejecutado |
| H25 | G15 ↔ G07 | D | Propuesta: paso físico individual no acreditado | 6 | 30 | 0 | 0 | 0 | 0 | No ejecutado |
| H26 | G15 ↔ R09 | D | Propuesta: paso físico individual no acreditado | 6 | 42 | 0 | 0 | 0 | 1 | No ejecutado |
| H27 | R09 ↔ R08 | D | Propuesta: paso físico individual no acreditado | 6 | 30 | 0 | 0 | 0 | 1 | No ejecutado |
| H28 | R08 ↔ R05 | D | Propuesta: paso físico individual no acreditado | 6 | 30 | 0 | 0 | 0 | 0 | No ejecutado |
| H29 | R05 ↔ G10 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 0 | No ejecutado |
| H30 | G10 ↔ R18 | D | Propuesta: paso físico individual no acreditado | 6 | 18 | 0 | 0 | 0 | 0 | No ejecutado |
| H31 | R18 ↔ R15 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H32 | R15 ↔ R14 | D | Propuesta: paso físico individual no acreditado | 6 | 30 | 0 | 0 | 0 | 1 | No ejecutado |
| H33 | R14 ↔ R12 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 1 | No ejecutado |
| H34 | R12 ↔ R13 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 1 | No ejecutado |
| H35 | G13 ↔ M01 | D | Propuesta: paso físico individual no acreditado | 6 | 48 | 0 | 0 | 0 | 0 | No ejecutado |
| H36 | M01 ↔ R33 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 48 | 0 | 0 | 1 | 1 | No ejecutado |
| H37 | R05 ↔ R04 | D | Propuesta: paso físico individual no acreditado | 6 | 42 | 0 | 0 | 0 | 1 | No ejecutado |
| H38 | R04 ↔ R02 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 0 | No ejecutado |
| H39 | R02 ↔ R03 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 0 | No ejecutado |
| H40 | R02 ↔ R07 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 0 | No ejecutado |
| H41 | R02 ↔ M03 | D | Propuesta: paso físico individual no acreditado | 6 | 30 | 0 | 0 | 0 | 0 | No ejecutado |
| H42 | R03 ↔ R19 | D | Propuesta: paso físico individual no acreditado | 6 | 90 | 0 | 0 | 0 | 0 | No ejecutado |
| H43 | R19 ↔ R06 | D | Propuesta: paso físico individual no acreditado | 6 | 66 | 0 | 0 | 0 | 0 | No ejecutado |
| H44 | R06 ↔ G06 | D | Propuesta: paso físico individual no acreditado | 6 | 42 | 0 | 0 | 0 | 0 | No ejecutado |
| H45 | R06 ↔ R10 | D | Propuesta: paso físico individual no acreditado | 6 | 48 | 0 | 0 | 0 | 0 | No ejecutado |
| H46 | A03 ↔ R25 | C | Parcial o bloqueada: interferencia del eje propuesto con sólido activo | 6 | 108 | 0 | 1 | 0 | 2 | No ejecutado |
| H47 | R25 ↔ G16 | C | Parcial o bloqueada: interferencia del eje propuesto con sólido activo | 6 | 48 | 0 | 3 | 0 | 3 | No ejecutado |
| H48 | G16 ↔ R32 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H49 | G16 ↔ R28 | D | Propuesta: paso físico individual no acreditado | 6 | 48 | 0 | 0 | 0 | 0 | No ejecutado |
| H50 | R28 ↔ M04 | D | Propuesta: paso físico individual no acreditado | 6 | 48 | 0 | 0 | 0 | 0 | No ejecutado |
| H51 | M04 ↔ G09 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H52 | G09 ↔ R29 | D | Propuesta: paso físico individual no acreditado | 6 | 36 | 0 | 0 | 0 | 0 | No ejecutado |
| H53 | R29 ↔ R24 | D | Propuesta: paso físico individual no acreditado | 6 | 24 | 0 | 0 | 0 | 0 | No ejecutado |
| H54 | M04 ↔ R22 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 48 | 0 | 0 | 1 | 0 | No ejecutado |
| LA1 | M01 ↔ M02 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 174 | 0 | 0 | 1 | 4 | No ejecutado |
| LA2 | M02 ↔ M03 | C | Parcial o bloqueada: eje con interferencia geométrica detectada en archivos | 6 | 444 | 0 | 0 | 2 | 7 | No ejecutado |
| LA3 | M03 ↔ M04 | C | Parcial o bloqueada: interferencia del eje propuesto con sólido activo | 6 | 336 | 0 | 3 | 0 | 19 | No ejecutado |
| LA4 | M04 ↔ M01 | C | Parcial o bloqueada: interferencia del eje propuesto con sólido activo | 6 | 342 | 0 | 2 | 0 | 18 | No ejecutado |

### Interferencias concretas y causas comunes

| Eje | Salas | Obstáculo / causa | Ubicación XYZ | Evidencia |
| --- | --- | --- | --- | --- |
| VC01 | A03 ↔ B05 | "Nave 102/102 (30×42)"/Nervaduras, pilastras y transiciones | (132, -16, 105) | Triángulos .asset |
| VE01 | A03 ↔ B06 | "Nave 102/102 (30×42)"/Nervaduras, pilastras y transiciones | (132, -16, 105) | Triángulos .asset |
| VS01 | A03 ↔ B01 | "Nave 102/102 (30×42)"/Nervaduras, pilastras y transiciones | (132, -16, 105) | Triángulos .asset |
| H05 | R26 ↔ R23 | "Nave 132/120 (18×78)"/Nervaduras, pilastras y transiciones; "Nave 114/156 (18×18)"/Nervaduras, pilastras y transiciones | (133, -16, 159); (132, -16, 159) | Triángulos .asset; Triángulos .asset |
| H15 | R34 ↔ G13 | "Nave 48/78 (18×78)"/Nervaduras, pilastras y transiciones | (49, -16, 117) | Triángulos .asset |
| H19 | R11 ↔ R17 | "Nave -24/90 (18×78)"/Nervaduras, pilastras y transiciones | (-23, -16, 129) | Triángulos .asset |
| H22 | G14 ↔ R01 | "Nave -114/114 (18×18)"/Nervaduras, pilastras y transiciones | (-96, -16, 117) | Triángulos .asset |
| H36 | M01 ↔ R33 | "Nave 48/78 (18×78)"/Nervaduras, pilastras y transiciones | (48, -16, 153) | Triángulos .asset |
| H46 | A03 ↔ R25 | Railing_01 | (145, -16, 99) | Caja activa |
| H47 | R25 ↔ G16 | SM_GranitePillar; SM_GranitePillar; SM_GranitePillar | (151, -16, 3); (160, -16, 3); (169, -16, 3) | Caja activa; Caja activa; Caja activa |
| H54 | M04 ↔ R22 | "Nave 114/-120 (18×66)"/Nervaduras, pilastras y transiciones | (132, -16, -63) | Triángulos .asset |
| LA1 | M01 ↔ M02 | "Nave 48/78 (18×78)"/Nervaduras, pilastras y transiciones | (48, -16, 153) | Triángulos .asset |
| LA2 | M02 ↔ M03 | "Nave 132/120 (18×78)"/Nervaduras, pilastras y transiciones; "Nave 114/156 (18×18)"/Nervaduras, pilastras y transiciones | (133, -16, 159); (132, -16, 159) | Triángulos .asset; Triángulos .asset |
| LA3 | M03 ↔ M04 | Railing_01; Railing_01; Railing_01 | (-21, -16, -17); (-18, -16, -21); (53, -16, -21) | Caja activa; Caja activa; Caja activa |
| LA4 | M04 ↔ M01 | Railing_01; Railing_01 | (57, -16, 53); (54, -16, 57) | Caja activa; Caja activa |

1. **Distribuidor crítico junto a R21:** VC01, VE01 y VS01 comparten el punto aproximadamente **(132,−16,105)**. El eje atraviesa la pilastra/nervadura de `Assets/ROM/CiudadDelSol34Naves/Nave_08_2.asset`, MeshCollider activo. La misma polilínea se conserva en UpperRoute del generador, pero eso no prueba que la geometría posterior la deje libre. El rodeo documental por Z111 pasó las comprobaciones de cajas y mallas directas conocidas; requiere confirmar arcos importados, ancho y NavMesh. No retirar la pilastra.
2. **Cruces de naves mal alineados con sus apoyos:** H05/LA2, H15, H19, H22, H36/LA1 y H54 cruzan pilastras al pasar de una envolvente a otra. El piso es continuo; el sólido también existe. La solución inicial es buscar la abertura existente entre apoyos, no inventar una perforación ni desactivar colliders.
3. **Pretiles y apoyos del acceso/núcleo:** H46 cruza la protección occidental de A03 cerca de (144.5,−16,99); H47 cruza los tres SM_GranitePillar próximos a X153/162/171, Z3. LA3/LA4 cortan pretiles del Sol Invertido. Deben rodearse mediante las rutas existentes.
4. **Contactos en escaleras AC01/AC02:** la cápsula sobre el eje lineal ideal toca bordes de basamentos aproximadamente 4.44 cm. No se declara eso una discontinuidad ni una obra faltante: el eje interpolado no reproduce la superficie de escalones ni la resolución del CharacterController (stepOffset 0.30 m). Requiere revisión física; no se ha cambiado ninguna tolerancia o validador.

Al retirar los 15 ejes interferidos, el **grafo documental** queda en componentes de tamaños 22, 5, 3, 5, 1, 7, 1, 13, 1, 4, 6. No son componentes medidos del NavMesh. Este resultado demuestra que el esquema de conexiones del dossier aún no acredita un circuito global; no demuestra que la ciudad esté dividida físicamente de ese modo.

## Diferencias importantes respecto del dossier

- **No cambió la escena:** no hay desviación de posición detectada por comparación de hash. Las diferencias son de interpretación y profundidad de comprobación. Las 67 referencias de jerarquía construida existen; solo P01 carece de jerarquía propia, como corresponde a una propuesta.
- El anterior inventario de cajas podía incluir colliders desactivados. Ahora se leen los overrides por ID. Hay 2.063 Base_Ceiling_3m_01 activos como objetos pero con su collider desactivado, además de 367 Column_Stackable_01, 120 Ceiling_Beam_02 y otros componentes. No se cuentan como obstáculos físicos activos. Se inventariaron 2672 referencias con renderer activo y collider desactivado; **visible no significa colisionable**. Su presencia no demuestra una sala sin techo ni autoriza reactivar colisiones indiscriminadamente.
- Los 38.412 m² de envolventes de las 54 naves no explican por sí solos los 100.476 m² nominales de piso Y−16. La diferencia corresponde a amplias bandas, circulaciones y otras zonas; no toda ella es área libre ni falta de geometría. La navegación y el diseño de uso deben decidir qué partes son efectivamente jugables.
- Los anclajes H/LA no son jambas ni bocas de puertas medidas. Las envolventes de naves abiertas no son muros. **No cerrar cada rectángulo para convertirlo en una habitación.**
- El ancho de AC02 en el dossier (16 m) corresponde al eje de la gran escalinata, no al mínimo de todo su recorrido: incluye la galería de X144..150, de 6 m de piso nominal y una referencia de paso de 4 m en el generador. Los 6 m de los puentes B03/B04 son ancho de losa; sus pretiles y apoyos reducen la franja útil.
- Los carriles laterales de diagnóstico generan avisos incluso en algunos ejes centrales sin cruces de cajas. Son una alerta de ancho/giro, **no una obligación de que cuatro jugadores caminen siempre en línea transversal**. Hay que probar el grupo, paso alternado y retorno, sin estrechar arquitectura ni ampliar por suposición.

## Calidad arquitectónica y planta Y−16

La escala es monumental: envolvente del piso principal X[−162,240], Z[−162,216], con naves de 18–42 m de ancho y largos de hasta 78 m. M01 (30×42), M03 (18×60), M04 (18×66) y el salón R19 (42×54) ofrecen bases suficientes para combate cooperativo y exploración, sujeto a despeje real. Las naves de 18×18 sirven mejor como espacios secundarios que como todos los encuentros principales.

El problema actual del diseño no es escasez de salas sino **circulación no recibida y falta de jerarquía de uso**. La malla de piso muy extensa, las circulaciones largas y la repetición modular pueden sentirse vacías o poco orientables. No se propone rellenarlas con enemigos o decoración ahora. Primero conviene aprobar un circuito corto, accesos de alas, descansos y referencias visuales; luego decidir qué áreas se mantienen como cámaras, galerías o límites no jugables. No se puede deducir un callejón sin salida real de un enlace documental rechazado. Los puentes B03/B04 sí necesitan revisar en Unity sus bocas y retorno antes de prometer un lazo.

La secuencia documental entrada→jefe usa AC01 → AC02 → AC03 → AC04 → VS01 → VS02 → VS03 → VS06 y suma aproximadamente **894.265 m de eje**. Incluye giros por arquitectura existente; no es longitud de ruta NavMesh. La ruta no queda aprobada porque VS01 cruza la pilastra común. Los cuatro lazos LA1–LA4 también tienen interferencias; hoy no acreditan cuatro alternativas transitables. El diseño sigue siendo viable si se replantean las trazas sobre huecos existentes, pero no está listo para dar por completada la circulación global.

## Cámara final y niveles profundos

- **D01:** centro (105,−34,63), envolvente 54×66 m; 3.564 m² de suelo muestreado en la envolvente. Acceso con terraza B05 Y−24 y escalera por Y−30. No se ha certificado techo/colisión completa ni cuatro jugadores.
- **D02:** centro (99,−34,−9), envolvente 66×78 m. Aproximadamente 4.860 m² tienen piso Y−34 dentro de esa caja; los 288 m² elevados corresponden a la terraza Y−30, no a un agujero que deba rellenarse. Se conservan los puentes superiores y su relación con D01 por VD01.
- **D03:** centro (18,−38,18), piso 84×84 m (7.056 m² nominales). Hay apoyos y puentes; la estimación sin cruces con geometría conocida es una **cota superior** de 6.644 m², no arena libre certificada. Los arcos importados y la corona pueden reducirla.
- **B01/B02:** los rectángulos 72×72 y 60×60 son envolventes de anillos con vacíos, no plantas macizas. Las superficies muestreadas dentro de sus cajas incluyen puentes/descansos del mismo nivel: no sumarlas de nuevo.
- **B03/B04:** cruce en proyección cerca de (21,39), con pisos Y−24 y Y−30: diferencia 6 m. Bajo la losa Y−30 el intradós está en Y−32, solo 6 m sobre el piso Y−38; algunas vigas bajan más. No conectar puentes por proximidad XZ ni reservar allí un jefe de altura grande.
- **P01:** (9,−38,15), 18×30 m dentro de D03. Hay 540 m² de apoyo muestreado sin cruces con las cajas y mallas directas conocidas a altura del Warrior. No hay un recinto construido P01 ni una validación de volumen para jefe. El techo parcial/histórico no certifica animaciones, radio ni desplazamiento de una criatura grande. El Humanoid 0 de radio 0.57 no acredita otra envolvente.

El retorno propuesto usa las mismas escaleras en sentido inverso; no falta un portal por definición. Sí falta recibir ese regreso. Los huecos, terrazas y pasos entre −16, −24, −30, −34 y −38 son intencionales: no deben rellenarse como supuestas piezas ausentes.

## Límites físicos y huecos de acceso que aún necesitan recepción

Se identificaron **18 muros visibles con su BoxCollider desactivado y suelo cercano**. En 9 posiciones de referencia no se encontró una caja o malla directa de reemplazo sobre el plano del muro. Esto **no demuestra 9 agujeros jugables**: puede haber una baranda antes del muro, otro collider importado o una intención de diseño distinta. Es una revisión necesaria por el riesgo potencial de atravesar límites o caer. No se reactivó ningún componente.

| Instancia | Posición de referencia al pie | Cota | Reemplazo caja en plano | Reemplazo malla directa en plano |
| --- | --- | --- | --- | --- |
| 68810919 | (101.6, -16, -117) | -16 | 0 | 0 |
| 140159736 | (101.6, -16, -57) | -16 | 0 | 0 |
| 278026176 | (101.6, -16, -69) | -16 | 0 | 0 |
| 428488519 | (144, 4, 33) | 4 | 0 | 0 |
| 521283009 | (101.6, -16, -111) | -16 | 1 | 0 |
| 603310574 | (147, 4, 30) | 4 | 1 | 0 |
| 671579072 | (101.6, -16, -63) | -16 | 1 | 0 |
| 692613236 | (101.6, -16, -87) | -16 | 1 | 0 |
| 728112842 | (101.6, -16, -93) | -16 | 0 | 0 |
| 752294326 | (-39, -16, -48.4) | -16 | 1 | 0 |
| 946276367 | (101.6, -16, -129) | -16 | 0 | 0 |
| 973264271 | (101.6, -16, -81) | -16 | 0 | 0 |
| 1413357427 | (101.6, -16, -123) | -16 | 1 | 0 |
| 1504786647 | (101.6, -16, -99) | -16 | 1 | 0 |
| 1741860583 | (101.6, -16, -75) | -16 | 1 | 0 |
| 1938855145 | (101.6, -16, -51) | -16 | 1 | 0 |
| 1966284995 | (101.6, -16, -105) | -16 | 0 | 0 |
| 2055364877 | (201, -16, -60.4) | -16 | 0 | 0 |

Los casos se concentran en el perímetro X≈101.6, Z−129..−51, Y−16; también aparecen límites del enlace Y4 y otros retornos. Paredes_a_revisar.csv y Paredes_visibles_pendientes.json conservan IDs y jerarquías. Si en Unity se confirma un límite sólido sin protección alternativa, la reparación a proponer sería su colisión original específica, no una nueva sala ni la activación masiva de cubiertas. Requiere autorización separada y recepción de navegación antes de tocarla.

La matriz de colisiones en disco permite Default (0) ↔ character (6); las cajas arquitectónicas inventariadas están en Default. Esto no sustituye comprobar overrides y llamadas de exclusión en ejecución.

Para no confundir anclajes con puertas, se muestrearon los cuatro lados de cada envolvente. Bocas_candidatas.csv contiene intervalos con piso nominal en ambos lados y sin cruces conocidos de cápsula Humanoid 0 en tres posiciones. **No son puertas construidas medidas ni rutas aprobadas**: pueden faltar obstáculos importados, girar el paso o existir una entrada vertical interior. En los anillos y cámaras, no tener una boca coplanar exterior no significa carecer de acceso.

## Inventario de los 68 espacios

Las alturas de techo de naves se leen de las mallas guardadas; los demás techos pueden ser variables o no estar resueltos por los datos disponibles. Las superficies son muestreadas, solapables y parciales: **no se declaran superficies transitables certificadas**. En Inventario_68.csv figuran todos los campos resumidos y en Espacios_auditados_final.json los objetos y accesos asociados.

| ID | Nombre | Centro XYZ | X×Z | Piso | Suelo apoyado m² (estimado) | Cota superior libre conocida m² | Ejes interferidos |
| --- | --- | --- | --- | --- | --- | --- | --- |
| R01 | Nave -114/114 (18×18) | (-105, -16, 123) | 18 × 18 | -16 | 324 | 320 | H22 |
| R02 | Nave -120/-30 (30×18) | (-105, -16, -21) | 30 × 18 | -16 | 540 | 535 |  |
| R03 | Nave -120/-54 (30×18) | (-105, -16, -45) | 30 × 18 | -16 | 540 | 535 |  |
| R04 | Nave -120/-6 (30×18) | (-105, -16, 3) | 30 × 18 | -16 | 540 | 535 |  |
| R05 | Nave -120/30 (30×36) | (-105, -16, 48) | 30 × 36 | -16 | 1080 | 1070 |  |
| R06 | Nave -126/-150 (18×66) | (-117, -16, -117) | 18 × 66 | -16 | 1188 | 1177 |  |
| R07 | Nave -138/-54 (18×66) | (-129, -16, -21) | 18 × 66 | -16 | 1188 | 1177 |  |
| R08 | Nave -138/30 (18×24) | (-129, -16, 42) | 18 × 24 | -16 | 432 | 422 |  |
| R09 | Nave -138/60 (18×18) | (-129, -16, 69) | 18 × 18 | -16 | 324 | 319 |  |
| R10 | Nave -150/-150 (18×18) | (-141, -16, -141) | 18 × 18 | -16 | 324 | 319 |  |
| R11 | Nave -24/90 (18×78) | (-15, -16, 129) | 18 × 78 | -16 | 1404 | 1389 | H19 |
| R12 | Nave -42/-12 (18×18) | (-33, -16, -3) | 18 × 18 | -16 | 324 | 320 |  |
| R13 | Nave -42/-36 (18×18) | (-33, -16, -27) | 18 × 18 | -16 | 324 | 320 |  |
| R14 | Nave -42/12 (18×18) | (-33, -16, 21) | 18 × 18 | -16 | 324 | 320 |  |
| R15 | Nave -42/36 (18×30) | (-33, -16, 51) | 18 × 30 | -16 | 540 | 534 |  |
| R16 | Nave -6/96 (18×30) | (3, -16, 111) | 18 × 30 | -16 | 540 | 533 |  |
| R17 | Nave -66/114 (30×18) | (-51, -16, 123) | 30 × 18 | -16 | 540 | 534 | H19 |
| R18 | Nave -72/30 (18×30) | (-63, -16, 45) | 18 × 30 | -16 | 540 | 533 |  |
| R19 | Nave -90/-126 (42×54) | (-69, -16, -99) | 42 × 54 | -16 | 2268 | 2255 |  |
| R20 | Nave 0/78 (42×18) | (21, -16, 87) | 42 × 18 | -16 | 756 | 747 |  |
| R21 | Nave 102/102 (30×42) | (117, -16, 123) | 30 × 42 | -16 | 1260 | 1247 |  |
| R22 | Nave 114/-120 (18×66) | (123, -16, -87) | 18 × 66 | -16 | 1188 | 1177 | H54 |
| R23 | Nave 114/156 (18×18) | (123, -16, 165) | 18 × 18 | -16 | 324 | 319 | H05 |
| R24 | Nave 132/-120 (30×18) | (147, -16, -111) | 30 × 18 | -16 | 540 | 533 |  |
| R25 | Nave 132/-18 (18×42) | (141, -16, 3) | 18 × 42 | -16 | 756 | 748 | H46, H47 |
| R26 | Nave 132/120 (18×78) | (141, -16, 159) | 18 × 78 | -16 | 1404 | 1387 | H05 |
| R27 | Nave 150/180 (30×18) | (165, -16, 189) | 30 × 18 | -16 | 540 | 533 |  |
| R28 | Nave 156/-48 (30×18) | (171, -16, -39) | 30 × 18 | -16 | 540 | 533 |  |
| R29 | Nave 162/-120 (18×18) | (171, -16, -111) | 18 × 18 | -16 | 324 | 319 |  |
| R30 | Nave 174/120 (30×18) | (189, -16, 129) | 30 × 18 | -16 | 540 | 533 |  |
| R31 | Nave 180/186 (18×18) | (189, -16, 195) | 18 × 18 | -16 | 324 | 319 |  |
| R32 | Nave 198/-48 (18×66) | (207, -16, -15) | 18 × 66 | -16 | 1188 | 1177 |  |
| R33 | Nave 36/162 (30×12) | (51, -16, 168) | 30 × 12 | -16 | 360 | 358 | H36 |
| R34 | Nave 48/78 (18×78) | (57, -16, 117) | 18 × 78 | -16 | 1404 | 1391 | H15 |
| G01 | E2 Galería este hacia el sur | (213, -16, 93) | 18 × 30 | -16 | 540 | 536 |  |
| G02 | E3 Galería oriental del Sol | (213, -16, 168) | 18 × 36 | -16 | 648 | 644 |  |
| G03 | E4 Galería del Sol hacia el oeste | (165, -16, 144) | 18 × 36 | -16 | 648 | 644 |  |
| G04 | Galería del borde oriental | (213, -16, 129) | 18 × 30 | -16 | 540 | 536 |  |
| G05 | N5 Galería norte hacia el Sol | (75, -16, 120) | 18 × 36 | -16 | 648 | 644 |  |
| G06 | Nave suroeste bajo Pórtico Solar | (-141, -16, -102) | 18 × 36 | -16 | 648 | 642 |  |
| G07 | O3 Galería norte de Jardines | (-81, -16, 81) | 18 × 30 | -16 | 540 | 535 |  |
| G08 | O6 Galería occidental de Archivos | (-129, -16, 105) | 18 × 42 | -16 | 756 | 752 |  |
| G09 | S2 Galería junto al Santuario | (171, -16, -78) | 18 × 36 | -16 | 648 | 644 |  |
| G10 | Transición norte de Jardines Dorados | (-81, -16, 45) | 18 × 30 | -16 | 540 | 535 |  |
| G11 | E6 Entrada occidental al ala del Sol | (105, -16, 165) | 18 × 30 | -16 | 540 | 536 |  |
| G12 | N1 Ala oeste del Claustro | (-45, -16, 93) | 18 × 30 | -16 | 540 | 536 |  |
| G13 | N3 Acceso sur del Claustro | (27, -16, 114) | 18 × 24 | -16 | 432 | 428 | H15 |
| G14 | O4 Jardines hacia Archivos | (-81, -16, 117) | 18 × 30 | -16 | 540 | 536 | H22 |
| G15 | O5 Patio bajo Archivos | (-105, -16, 90) | 18 × 36 | -16 | 648 | 644 |  |
| G16 | S3 Enlace oriental del Santuario | (183, -16, -6) | 18 × 36 | -16 | 648 | 644 | H47 |
| M01 | Claustro del Eclipse | (21, -16, 153) | 30 × 42 | -16 | 1260 | 1254 | H36, LA1, LA4 |
| M02 | El Sol Corrompido | (189, -16, 162) | 18 × 36 | -16 | 648 | 639 | LA1, LA2 |
| M03 | Jardines Dorados | (-81, -16, -18) | 18 × 60 | -16 | 1080 | 1070 | LA2, LA3 |
| M04 | Santuario Profano | (147, -16, -63) | 18 × 66 | -16 | 1188 | 1182 | H54, LA3, LA4 |
| A00 | Atrio octagonal del Umbral | (162, 20, 42) | 84 × 84 | 20 | 5796 | 4902 |  |
| A01 | Vestíbulo bajo la arena | (162, 4, 18) | 24 × 24 | 4 | 576 | 479 |  |
| A02 | Atrio del enlace | (159, 4, 33) | 30 × 6 | 4 | 180 | 144 |  |
| A03 | Gran descanso inferior | (156, -16, 102) | 24 × 24 | -16 | 576 | 496 | VC01, VE01, VS01, H46 |
| D01 | Cámara Centro Oriental | (105, -34, 63) | 54 × 66 | -34 | 3564 | 3303 |  |
| D02 | Cámara de las arcadas del Sureste | (99, -34, -9) | 66 × 78 | -34 | 4860 | 4625 |  |
| D03 | Gran Cámara del Sol Invertido | (18, -38, 18) | 84 × 84 | -38 | 7056 | 6644 |  |
| B01 | Galería perimetral superior del Sol | (18, -24, 18) | 72 × 72 | -24 | 2016 | 1879 | VS01 |
| B02 | Galería perimetral inferior del Sol | (18, -30, 18) | 60 × 60 | -30 | 1440 | 1330 |  |
| B03 | Puente este-oeste | (18, -24, 39) | 60 × 6 | -24 | 360 | 240 |  |
| B04 | Puente norte-sur | (21, -30, 18) | 6 × 48 | -30 | 288 | 192 |  |
| B05 | Terraza del Centro Oriental | (84, -24, 90) | 12 × 12 | -24 | 144 | 111 | VC01 |
| B06 | Terraza del Sureste | (120, -30, -42) | 24 × 12 | -30 | 288 | 230 | VE01 |
| P01 | Área candidata del jefe | (9, -38, 15) | 18 × 30 | -38 | 540 | 540 |  |

### R01 — Nave -114/114 (18×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-105, -16, 123); envolvente 18 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 324 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 320 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 1 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H22, H23. Ejes con interferencia detectada: H22.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -114/114 (18×18)"`. Coincidencia literal en la escena: sí.

### R02 — Nave -120/-30 (30×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-105, -16, -21); envolvente 30 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 535 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H38, H39, H40, H41. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -120/-30 (30×18)"`. Coincidencia literal en la escena: sí.

### R03 — Nave -120/-54 (30×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-105, -16, -45); envolvente 30 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 535 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H39, H42. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -120/-54 (30×18)"`. Coincidencia literal en la escena: sí.

### R04 — Nave -120/-6 (30×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-105, -16, 3); envolvente 30 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 535 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H37, H38. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -120/-6 (30×18)"`. Coincidencia literal en la escena: sí.

### R05 — Nave -120/30 (30×36)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-105, -16, 48); envolvente 30 × 36 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 1.8; primera cubierta conocida en el conjunto parcial de datos: 1.8. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1080 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1070 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 4 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H28, H29, H37. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -120/30 (30×36)"`. Coincidencia literal en la escena: sí.

### R06 — Nave -126/-150 (18×66)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-117, -16, -117); envolvente 18 × 66 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: -2.9; primera cubierta conocida en el conjunto parcial de datos: -2.9. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1188 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1177 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H43, H44, H45. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -126/-150 (18×66)"`. Coincidencia literal en la escena: sí.

### R07 — Nave -138/-54 (18×66)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-129, -16, -21); envolvente 18 × 66 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1188 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1177 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H40. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -138/-54 (18×66)"`. Coincidencia literal en la escena: sí.

### R08 — Nave -138/30 (18×24)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-129, -16, 42); envolvente 18 × 24 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 1.8; primera cubierta conocida en el conjunto parcial de datos: 1.8. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 432 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 422 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 4 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H27, H28. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -138/30 (18×24)"`. Coincidencia literal en la escena: sí.

### R09 — Nave -138/60 (18×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-129, -16, 69); envolvente 18 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 324 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 319 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H26, H27. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -138/60 (18×18)"`. Coincidencia literal en la escena: sí.

### R10 — Nave -150/-150 (18×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-141, -16, -141); envolvente 18 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: -2.9; primera cubierta conocida en el conjunto parcial de datos: -2.9. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 324 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 319 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H45. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -150/-150 (18×18)"`. Coincidencia literal en la escena: sí.

### R11 — Nave -24/90 (18×78)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-15, -16, 129); envolvente 18 × 78 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 1.67; primera cubierta conocida en el conjunto parcial de datos: 1.67. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1404 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1389 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 3 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H17, H19. Ejes con interferencia detectada: H19.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -24/90 (18×78)"`. Coincidencia literal en la escena: sí.

### R12 — Nave -42/-12 (18×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-33, -16, -3); envolvente 18 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: -2.9; primera cubierta conocida en el conjunto parcial de datos: -2.9. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 324 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 320 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 1 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H33, H34. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -42/-12 (18×18)"`. Coincidencia literal en la escena: sí.

### R13 — Nave -42/-36 (18×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-33, -16, -27); envolvente 18 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 324 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 320 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H34. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -42/-36 (18×18)"`. Coincidencia literal en la escena: sí.

### R14 — Nave -42/12 (18×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-33, -16, 21); envolvente 18 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 324 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 320 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 1 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H32, H33. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -42/12 (18×18)"`. Coincidencia literal en la escena: sí.

### R15 — Nave -42/36 (18×30)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-33, -16, 51); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 534 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H31, H32. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -42/36 (18×30)"`. Coincidencia literal en la escena: sí.

### R16 — Nave -6/96 (18×30)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (3, -16, 111); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 533 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H16, H17. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -6/96 (18×30)"`. Coincidencia literal en la escena: sí.

### R17 — Nave -66/114 (30×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-51, -16, 123); envolvente 30 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 534 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H19, H20, H21. Ejes con interferencia detectada: H19.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -66/114 (30×18)"`. Coincidencia literal en la escena: sí.

### R18 — Nave -72/30 (18×30)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-63, -16, 45); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 533 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H30, H31. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -72/30 (18×30)"`. Coincidencia literal en la escena: sí.

### R19 — Nave -90/-126 (42×54)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-69, -16, -99); envolvente 42 × 54 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 2268 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 2255 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 4 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H42, H43. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave -90/-126 (42×54)"`. Coincidencia literal en la escena: sí.

### R20 — Nave 0/78 (42×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (21, -16, 87); envolvente 42 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 756 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 747 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 4 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H18. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 0/78 (42×18)"`. Coincidencia literal en la escena: sí.

### R21 — Nave 102/102 (30×42)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (117, -16, 123); envolvente 30 × 42 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 1.67; primera cubierta conocida en el conjunto parcial de datos: 1.67. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1260 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1247 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 4 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H12, H13. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 102/102 (30×42)"`. Coincidencia literal en la escena: sí.

### R22 — Nave 114/-120 (18×66)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (123, -16, -87); envolvente 18 × 66 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1188 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1177 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H54. Ejes con interferencia detectada: H54.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 114/-120 (18×66)"`. Coincidencia literal en la escena: sí.

### R23 — Nave 114/156 (18×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (123, -16, 165); envolvente 18 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 324 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 319 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H05, H06, H12. Ejes con interferencia detectada: H05.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 114/156 (18×18)"`. Coincidencia literal en la escena: sí.

### R24 — Nave 132/-120 (30×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (147, -16, -111); envolvente 30 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 533 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 3 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H53. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 132/-120 (30×18)"`. Coincidencia literal en la escena: sí.

### R25 — Nave 132/-18 (18×42)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (141, -16, 3); envolvente 18 × 42 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: -3.23; primera cubierta conocida en el conjunto parcial de datos: -3.23. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 756 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 748 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 3 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H46, H47. Ejes con interferencia detectada: H46, H47.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 132/-18 (18×42)"`. Coincidencia literal en la escena: sí.

### R26 — Nave 132/120 (18×78)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (141, -16, 159); envolvente 18 × 78 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 1.67; primera cubierta conocida en el conjunto parcial de datos: 1.67. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1404 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1387 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 4 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H04, H05. Ejes con interferencia detectada: H05.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 132/120 (18×78)"`. Coincidencia literal en la escena: sí.

### R27 — Nave 150/180 (30×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (165, -16, 189); envolvente 30 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 533 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 3 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H10. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 150/180 (30×18)"`. Coincidencia literal en la escena: sí.

### R28 — Nave 156/-48 (30×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (171, -16, -39); envolvente 30 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 533 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 3 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H49, H50. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 156/-48 (30×18)"`. Coincidencia literal en la escena: sí.

### R29 — Nave 162/-120 (18×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (171, -16, -111); envolvente 18 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 324 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 319 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H52, H53. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 162/-120 (18×18)"`. Coincidencia literal en la escena: sí.

### R30 — Nave 174/120 (30×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (189, -16, 129); envolvente 30 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 533 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 3 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H01, H02, H03. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 174/120 (30×18)"`. Coincidencia literal en la escena: sí.

### R31 — Nave 180/186 (18×18)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (189, -16, 195); envolvente 18 × 18 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 324 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 319 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H09, H10. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 180/186 (18×18)"`. Coincidencia literal en la escena: sí.

### R32 — Nave 198/-48 (18×66)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (207, -16, -15); envolvente 18 × 66 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 2; primera cubierta conocida en el conjunto parcial de datos: 2. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1188 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1177 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H48. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 198/-48 (18×66)"`. Coincidencia literal en la escena: sí.

### R33 — Nave 36/162 (30×12)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (51, -16, 168); envolvente 30 × 12 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 1.8; primera cubierta conocida en el conjunto parcial de datos: 1.8. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 360 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 358 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H36. Ejes con interferencia detectada: H36.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 36/162 (30×12)"`. Coincidencia literal en la escena: sí.

### R34 — Nave 48/78 (18×78)

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (57, -16, 117); envolvente 18 × 78 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 1.67; primera cubierta conocida en el conjunto parcial de datos: 1.67. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1404 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1391 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H14, H15. Ejes con interferencia detectada: H15.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_34_Naves_Sin_Reticula/"Nave 48/78 (18×78)"`. Coincidencia literal en la escena: sí.

### G01 — E2 Galería este hacia el sur

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (213, -16, 93); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 536 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H11. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"E2 Galería este hacia el sur"`. Coincidencia literal en la escena: sí.

### G02 — E3 Galería oriental del Sol

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (213, -16, 168); envolvente 18 × 36 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 648 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 644 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H08. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"E3 Galería oriental del Sol"`. Coincidencia literal en la escena: sí.

### G03 — E4 Galería del Sol hacia el oeste

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (165, -16, 144); envolvente 18 × 36 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 648 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 644 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H03, H04, H07. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"E4 Galería del Sol hacia el oeste"`. Coincidencia literal en la escena: sí.

### G04 — Galería del borde oriental

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (213, -16, 129); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.6; primera cubierta conocida en el conjunto parcial de datos: 0.6. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 536 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 1 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H02, H11. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"Galería del borde oriental"`. Coincidencia literal en la escena: sí.

### G05 — N5 Galería norte hacia el Sol

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (75, -16, 120); envolvente 18 × 36 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 648 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 644 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H13, H14. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"N5 Galería norte hacia el Sol"`. Coincidencia literal en la escena: sí.

### G06 — Nave suroeste bajo Pórtico Solar

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-141, -16, -102); envolvente 18 × 36 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: -3.23; primera cubierta conocida en el conjunto parcial de datos: -3.23. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 648 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 642 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H44. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"Nave suroeste bajo Pórtico Solar"`. Coincidencia literal en la escena: sí.

### G07 — O3 Galería norte de Jardines

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-81, -16, 81); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 535 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 1 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H25. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"O3 Galería norte de Jardines"`. Coincidencia literal en la escena: sí.

### G08 — O6 Galería occidental de Archivos

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-129, -16, 105); envolvente 18 × 42 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 756 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 752 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H23, H24. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"O6 Galería occidental de Archivos"`. Coincidencia literal en la escena: sí.

### G09 — S2 Galería junto al Santuario

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (171, -16, -78); envolvente 18 × 36 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 648 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 644 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H51, H52. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"S2 Galería junto al Santuario"`. Coincidencia literal en la escena: sí.

### G10 — Transición norte de Jardines Dorados

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-81, -16, 45); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: -1.15; primera cubierta conocida en el conjunto parcial de datos: -1.15. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 535 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 1 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H29, H30. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/"Transición norte de Jardines Dorados"`. Coincidencia literal en la escena: sí.

### G11 — E6 Entrada occidental al ala del Sol

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (105, -16, 165); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 536 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H06. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/E6 Entrada occidental al ala del Sol`. Coincidencia literal en la escena: sí.

### G12 — N1 Ala oeste del Claustro

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-45, -16, 93); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 536 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H20. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/N1 Ala oeste del Claustro`. Coincidencia literal en la escena: sí.

### G13 — N3 Acceso sur del Claustro

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (27, -16, 114); envolvente 18 × 24 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 432 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 428 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H15, H16, H18, H35. Ejes con interferencia detectada: H15.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/N3 Acceso sur del Claustro`. Coincidencia literal en la escena: sí.

### G14 — O4 Jardines hacia Archivos

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-81, -16, 117); envolvente 18 × 30 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 536 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H21, H22. Ejes con interferencia detectada: H22.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/O4 Jardines hacia Archivos`. Coincidencia literal en la escena: sí.

### G15 — O5 Patio bajo Archivos

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-105, -16, 90); envolvente 18 × 36 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 648 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 644 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H24, H25, H26. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/O5 Patio bajo Archivos`. Coincidencia literal en la escena: sí.

### G16 — S3 Enlace oriental del Santuario

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (183, -16, -6); envolvente 18 × 36 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: -2.9; primera cubierta conocida en el conjunto parcial de datos: -2.9. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 648 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 644 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H47, H48, H49. Ejes con interferencia detectada: H47.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Rediseno_16_Sectores/S3 Enlace oriental del Santuario`. Coincidencia literal en la escena: sí.

### M01 — Claustro del Eclipse

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (21, -16, 153); envolvente 30 × 42 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 1.6; primera cubierta conocida en el conjunto parcial de datos: 1.6. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1260 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1254 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H35, H36, LA1, LA4. Ejes con interferencia detectada: H36, LA1, LA4.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Salas_Monumentales/Claustro del Eclipse`. Coincidencia literal en la escena: sí.

### M02 — El Sol Corrompido

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (189, -16, 162); envolvente 18 × 36 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.5; primera cubierta conocida en el conjunto parcial de datos: 0.5. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 648 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 639 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 1 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H07, H08, H09, LA1, LA2. Ejes con interferencia detectada: LA1, LA2.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Salas_Monumentales/El Sol Corrompido`. Coincidencia literal en la escena: sí.

### M03 — Jardines Dorados

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (-81, -16, -18); envolvente 18 × 60 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.32; primera cubierta conocida en el conjunto parcial de datos: 0.32. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1080 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1070 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 2 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H41, LA2, LA3. Ejes con interferencia detectada: LA2, LA3.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Salas_Monumentales/Jardines Dorados`. Coincidencia literal en la escena: sí.

### M04 — Santuario Profano

- Estado: **Volumen abovedado existente, no habitación cerrada independiente**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (147, -16, -63); envolvente 18 × 66 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 0.65; primera cubierta conocida en el conjunto parcial de datos: 0.65. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1188 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1182 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 2 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: ninguno individualizado en esta ficha. Ejes propuestos: H50, H51, H54, LA3, LA4. Ejes con interferencia detectada: H54, LA3, LA4.
- Elementos pendientes: No se demuestra necesidad de paredes nuevas: la nave abierta es deliberada. Pendientes accesos libres, continuidad y delimitación funcional.
- Jerarquía: `Subsuelo_Salas_Monumentales/Santuario Profano`. Coincidencia literal en la escena: sí.

### A00 — Atrio octagonal del Umbral

- Estado: **Acceso/atrio construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (162, 20, 42); envolvente 84 × 84 m; piso Y20. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: Pendiente. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 5796 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 4902 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 102 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: AC01, AC02. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No hay estructura faltante demostrada; pendientes juntas, huecos útiles, protección y recepción del acceso.
- Jerarquía: `Umbral_De_Las_Profundidades/01 - Arena octagonal`. Coincidencia literal en la escena: sí.

### A01 — Vestíbulo bajo la arena

- Estado: **Acceso/atrio construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (162, 4, 18); envolvente 24 × 24 m; piso Y4. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: 13.38; primera cubierta conocida en el conjunto parcial de datos: 13.38. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 576 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 479 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 18 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: AC02, AC03. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No hay estructura faltante demostrada; pendientes juntas, huecos útiles, protección y recepción del acceso.
- Jerarquía: `Umbral_De_Las_Profundidades/06 - Descanso bajo la arena`. Coincidencia literal en la escena: sí.

### A02 — Atrio del enlace

- Estado: **Acceso/atrio construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (159, 4, 33); envolvente 30 × 6 m; piso Y4. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: 16. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 180 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 144 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 10 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: AC03, AC04. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No hay estructura faltante demostrada; pendientes juntas, huecos útiles, protección y recepción del acceso.
- Jerarquía: `Subsuelo_Acceso_Etapa01/01 - Atrio de enlace Y4`. Coincidencia literal en la escena: sí.

### A03 — Gran descanso inferior

- Estado: **Acceso/atrio construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (156, -16, 102); envolvente 24 × 24 m; piso Y-16. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: Pendiente. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 576 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 496 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 16 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: AC04, VC01, VE01, VS01. Ejes propuestos: H01, H46. Ejes con interferencia detectada: VC01, VE01, VS01, H46.
- Elementos pendientes: No hay estructura faltante demostrada; pendientes juntas, huecos útiles, protección y recepción del acceso.
- Jerarquía: `Subsuelo_Acceso_Etapa01/05 - Primer piso inferior Y-16`. Coincidencia literal en la escena: sí.

### D01 — Cámara Centro Oriental

- Estado: **Recinto vertical construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (105, -34, 63); envolvente 54 × 66 m; piso Y-34. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: Pendiente. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 3564 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 3303 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 46 cajas con intersección de la envolvente a altura de personaje; 29 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VC02, VD01. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: Pendientes delimitación jugable, huecos efectivos, límites de combate y recepción del recorrido entre apoyos y techos.
- Jerarquía: `Nucleo_Monumental_Conjunto/01 - Centro Oriental aprobado`. Coincidencia literal en la escena: sí.

### D02 — Cámara de las arcadas del Sureste

- Estado: **Recinto vertical construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (99, -34, -9); envolvente 66 × 78 m; piso Y-34. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: Pendiente. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 4860 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 4625 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 56 cajas con intersección de la envolvente a altura de personaje; 34 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VE02, VD01. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: Pendientes delimitación jugable, huecos efectivos, límites de combate y recepción del recorrido entre apoyos y techos.
- Jerarquía: `Nucleo_Monumental_Conjunto/02 - Sureste arcadas aprobadas`. Coincidencia literal en la escena: sí.

### D03 — Gran Cámara del Sol Invertido

- Estado: **Recinto vertical construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (18, -38, 18); envolvente 84 × 84 m; piso Y-38. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: -32. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 7056 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 6644 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 90 cajas con intersección de la envolvente a altura de personaje; 15 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VS03, VS06. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: Pendientes delimitación jugable, huecos efectivos, límites de combate y recepción del recorrido entre apoyos y techos.
- Jerarquía: `Nucleo_Monumental_Conjunto/03 - El Sol Invertido V3`. Coincidencia literal en la escena: sí.

### B01 — Galería perimetral superior del Sol

- Estado: **Galería, puente o descanso construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (18, -24, 18); envolvente 72 × 72 m; piso Y-24. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: -8.3. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 2016 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1879 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 26 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VS01, VS02, VS04. Ejes propuestos: ninguno. Ejes con interferencia detectada: VS01.
- Elementos pendientes: No hay tramo faltante demostrado; pendientes anchura efectiva, juntas, protección y recepción.
- Jerarquía: `Nucleo_Monumental_Conjunto/03 - El Sol Invertido V3/02 - Galerias Y-24`. Coincidencia literal en la escena: sí.

### B02 — Galería perimetral inferior del Sol

- Estado: **Galería, puente o descanso construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (18, -30, 18); envolvente 60 × 60 m; piso Y-30. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: -8.3. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 1440 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 1330 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 37 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VS02, VS03, VS05. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No hay tramo faltante demostrado; pendientes anchura efectiva, juntas, protección y recepción.
- Jerarquía: `Nucleo_Monumental_Conjunto/03 - El Sol Invertido V3/02 - Galerias Y-30`. Coincidencia literal en la escena: sí.

### B03 — Puente este-oeste

- Estado: **Galería, puente o descanso construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (18, -24, 39); envolvente 60 × 6 m; piso Y-24. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: -8.017. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 360 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 240 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 20 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VS04. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No hay tramo faltante demostrado; pendientes anchura efectiva, juntas, protección y recepción.
- Jerarquía: `Nucleo_Monumental_Conjunto/03 - El Sol Invertido V3/03 - Puente este oeste Y-24`. Coincidencia literal en la escena: sí.

### B04 — Puente norte-sur

- Estado: **Galería, puente o descanso construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (21, -30, 18); envolvente 6 × 48 m; piso Y-30. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: Pendiente. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 288 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 192 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 16 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VS05. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: No hay tramo faltante demostrado; pendientes anchura efectiva, juntas, protección y recepción.
- Jerarquía: `Nucleo_Monumental_Conjunto/03 - El Sol Invertido V3/03 - Puente norte sur Y-30`. Coincidencia literal en la escena: sí.

### B05 — Terraza del Centro Oriental

- Estado: **Galería, puente o descanso construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (84, -24, 90); envolvente 12 × 12 m; piso Y-24. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: Pendiente. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 144 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 111 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 11 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VC01, VC02. Ejes propuestos: ninguno. Ejes con interferencia detectada: VC01.
- Elementos pendientes: No hay tramo faltante demostrado; pendientes anchura efectiva, juntas, protección y recepción.
- Jerarquía: `Nucleo_Monumental_Conjunto/01 - Centro Oriental aprobado/04 - Terraza Y-24`. Coincidencia literal en la escena: sí.

### B06 — Terraza del Sureste

- Estado: **Galería, puente o descanso construido**. Volumen estructural construido; completitud funcional no acreditada. Recepción real en Unity: pendiente.
- Centro de ficha (120, -30, -42); envolvente 24 × 12 m; piso Y-30. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: Pendiente. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 288 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 230 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 14 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VE01, VE02. Ejes propuestos: ninguno. Ejes con interferencia detectada: VE01.
- Elementos pendientes: No hay tramo faltante demostrado; pendientes anchura efectiva, juntas, protección y recepción.
- Jerarquía: `Nucleo_Monumental_Conjunto/02 - Sureste arcadas aprobadas/01 - Ocho pisos terraza Y-30`. Coincidencia literal en la escena: sí.

### P01 — Área candidata del jefe

- Estado: **Solo propuesta funcional dentro de una cámara existente**. Propuesta sin recinto independiente. Recepción real en Unity: pendiente.
- Centro de ficha (9, -38, 15); envolvente 18 × 30 m; piso Y-38. El centro geométrico no es un punto de aparición.
- Techo de nave medido en triángulos guardados: Pendiente; primera cubierta conocida en el conjunto parcial de datos: Pendiente. Valores absolutos Y. Si falta un valor, no significa cielo abierto.
- Suelo con apoyo dentro de la envolvente: aproximadamente 540 m² a malla de 1 m. Cota superior sin cruces con cajas y mallas directas conocidas: 540 m². **No es superficie transitable certificada**; no sumar las áreas superpuestas.
- Colliders: 0 cajas con intersección de la envolvente a altura de personaje; 0 MeshCollider de la jerarquía de fuente. Las 54 naves tienen dos mallas directas activas cada una. Cajas cercanas pueden pertenecer a otra agrupación; las mallas importadas necesitan la revisión indicada.
- Accesos construidos referenciados: VS06. Ejes propuestos: ninguno. Ejes con interferencia detectada: ninguno en los modelos examinados; no equivale a aprobado.
- Elementos pendientes: Falta aprobar subzona del jefe y su despeje tridimensional; no añadir una sala por defecto.
- Jerarquía: `D03, X0..18 Z0..30`. Coincidencia literal en la escena: no: P01 es una propuesta funcional dentro de D03.


## Plan por etapas para aprobación

### E0 — Recepción en Unity del acceso y distribuidor, antes de construir

- Conservar: AC01–AC04, A03 y R21; escaleras, barandas y pilastra de Nave_08_2.
- Ámbito y dimensiones: A03 (156,−16,102), 24×24 m. R21 (117,−16,123), 30×42 m. Tramo común en (132,−16,105). Límites con aviso en X≈101.6/Z−129..−51 a Y−16 y en (144,4,33), además de los IDs específicos del CSV de paredes.
- Faltantes: Validación física y NavMesh ida/vuelta. Replantear eje junto a la pilastra, no retirarla. Revisar además los límites visibles con collider desactivado, antes de considerar seguro el perímetro.
- Propuesta: Desvío documental (135,−16,105) → (135,−16,111) → (120,−16,111) → (120,−16,105). Objetivo de paso útil 6 m; no ancho libre demostrado.
- Nuevas salas/conexiones físicas: Ninguna necesaria demostrada.
- Dependencias: Aprobación de esta auditoría.
- Riesgo: Bajo si se limita a recepción/documentación; prohibido alterar colisionadores para hacer pasar el eje.
- Recepción necesaria: Ruta completa en ambos sentidos; cápsula real del Warrior, Humanoid 0; barrido continuo en curvas; giro y encuentro de cuatro personajes; verificar arcos FBX. Comprobar toda la protección perimetral y cualquier colisión alternativa situada delante del muro, no solo su plano.

### E1 — Consolidación de la planta Y−16 y sus enlaces laterales

- Conservar: 54 naves/galerías existentes, apoyos, huecos de cámaras inferiores y pretiles del núcleo.
- Ámbito y dimensiones: X[−162,240], Z[−162,216], Y−16; no es una orden de construir una losa de 402×378 m.
- Faltantes: Demostrar pasos existentes y corregir el trazado de los 12 enlaces propuestos interferidos. Evitar H46/H47 y LA1–LA4 tal como estaban dibujados. Si se demuestra una pared límite atravesable, proponer reponer únicamente su colisión existente, con autorización, sin activar en bloque cubiertas o pilares.
- Propuesta: Reutilizar desvíos H46/H47/LA3/LA4 en Alternativas_con_mallas.json. Revisar ventanas 12×12 m alrededor de (132,−16,159), (48,−16,117), (−24,−16,129), (−96,−16,117), (48,−16,153), (132,−16,−63). Son áreas de inspección, no nuevas salas.
- Nuevas salas/conexiones físicas: No se ha demostrado necesidad de un nuevo corredor. Puertas u obras solo si la recepción prueba que no hay paso alternativo.
- Dependencias: E0 recibida sin bloqueos.
- Riesgo: Medio: una falsa abertura puede invadir pilastras, barandas o un vacío vertical.
- Recepción necesaria: Cada H/LA con ruta NavMesh bidireccional y nivel correcto; márgenes, techos, obstáculos y dos sentidos de circulación. No sumar las salas a un único grupo de combate.

### E2 — Completar funciones arquitectónicas sin duplicar las naves

- Conservar: M01 30×42; M02 18×36; M03 18×60; M04 18×66 m; 34 R y 16 G.
- Ámbito y dimensiones: Cotas existentes Y−16; las coordenadas de cada ficha se conservan en Inventario_68.csv.
- Faltantes: Definición de itinerario legible, descansos y accesos medidos; no hay prueba de que falten cuatro paredes en cada nave abierta.
- Propuesta: No reservar todas las superficies para combate. Seleccionar un circuito corto entre A03 y R21 primero; habilitar alas por recepción. Meta de huecos útiles 6×5 m cuando el diseño lo requiera, pendiente de medir cada hueco real.
- Nuevas salas/conexiones físicas: Cero salas nuevas propuestas por defecto; cualquier cierre/puerta adicional requiere un plano específico y aprobación.
- Dependencias: E1 valida los enlaces del ala elegida.
- Riesgo: Medio si se compartimenta: puede destruir la escala monumental o las rutas alternativas.
- Recepción necesaria: Orientación, distancias, lectura visual y espacios de recuperación; cuatro personajes, cámara y enemigos de tamaño previsto, sin implementar combate nuevo.

### E3 — Recepción de cámaras profundas y conexiones secundarias

- Conservar: D01, D02, B05/B06 y descensos VC/VE; VD01 entre cámaras.
- Ámbito y dimensiones: D01 centro (105,−34,63), 54×66 m. D02 centro (99,−34,−9), 66×78 m; terraza elevada (120,−30,−42), 24×12 m.
- Faltantes: Alturas libres completas con arcos FBX, límites jugables, protección y retorno. El área elevada de D02 no es un piso inferior faltante.
- Propuesta: Conservar las escaleras 2/6 existentes y sus terrazas; no rellenar aberturas del piso Y−16.
- Nuevas salas/conexiones físicas: Ninguna demostrada. Retornos o protecciones solo si se identifica un borde realmente descubierto en Unity.
- Dependencias: E0 y acceso a cámaras por Y−16 recibidos.
- Riesgo: Alto si se rellenan huecos o se confunden suelos superpuestos.
- Recepción necesaria: Apoyo continuo, alturas, paso bajo puentes, rutas ida/vuelta por escaleras y separación estricta de plantas; no proyectar al NavMesh más próximo de otra cota.

### E4 — Cámara final y retorno

- Conservar: D03 84×84, anillos B01/B02, puentes B03/B04, escaleras VS y corona solar.
- Ámbito y dimensiones: D03 (18,−38,18), X/Z[−24,60]. B01 Y−24, B02 Y−30. P01 X[0,18], Z[0,30], 18×30 m.
- Faltantes: Definir volumen completo de combate y escala futura del jefe; recorrido y regreso sin colisión. P01 no está construida como arena separada.
- Propuesta: P01 es una subzona, no una nueva sala. Reserva preliminar de centros para una criatura de radio 2 m: X[2,15], Z[3,27], 13×24 m, condicionada a comprobar corona/arcos/animaciones. No se certifica esa reserva como libre.
- Nuevas salas/conexiones físicas: No quitar ni desplazar puentes para obtener una arena de 84×84. Si esa amplitud libre es una exigencia de diseño, detener y decidir una alternativa antes de obras.
- Dependencias: E3 y VS01–VS03 recibidos.
- Riesgo: Alto: puentes reducen altura; los centros de anillos están sobre vacío; riesgo de confundir cruce XZ con conexión.
- Recepción necesaria: Volumen de jefe, giros, cámara, regreso completo; franja bajo B04 X18..24 a Y−30; vigas por debajo de losas; prueba real de caídas y protecciones.

### E5 — Recepción cooperativa y rendimiento arquitectónico

- Conservar: Flujo original de Raid/Mirror y agrupaciones existentes; no crear sistemas paralelos.
- Ámbito y dimensiones: Interior aprobado por alas y cotas; sin ampliación del exterior.
- Faltantes: Prueba de cuatro conexiones reales. El Host local actual está configurado para un jugador.
- Propuesta: Medir por ala, limitar líneas de visión por decisiones arquitectónicas aprobadas y reutilizar divisiones existentes. Ningún ocultamiento automático ni nueva partición de red en esta etapa.
- Nuevas salas/conexiones físicas: Ninguna por defecto.
- Dependencias: E0–E4 aprobadas; configuración cooperativa autorizada aparte.
- Riesgo: No asumir que recepción estática o Host de un jugador certifica multijugador.
- Recepción necesaria: Cuatro jugadores, cruce en puertas y escaleras, desconexión/regreso, cámara, Profiler/Frame Debugger y coste de visibilidad antes de población/ambientación final.


**Primera etapa recomendada: E0, recepción y replanteo del acceso A03–R21 y del punto común (132,−16,105).** Es la dependencia de las tres rutas profundas. No recomiendo empezar agregando salas, rellenando huecos ni moviendo pilares. El desvío por Z111 y los otros cuatro rodeos están documentados, no aplicados. Pasan las comprobaciones offline conocidas, pero quedan pendientes de MeshCollider importados, NavMesh y cooperación.

### Recepción requerida en Unity antes de obras

1. Abrir la escena definitiva fuera de Play sin ejecutar generadores ni Bake; preservar cambios no relacionados que pudiera tener el usuario. Contrastar visualmente los 15 puntos y sus objetos/GUID/IDs con el plano de auditoría.
2. Recibir primero AC01–AC04 y la llegada a R21, luego el tramo común de VC01/VE01/VS01. Obtener camino completo ida y vuelta para Humanoid 0 usando el NavMesh existente; proyección local con control de altura y collider de apoyo. No sustituir una ruta parcial por una línea entre puntos.
3. Barrer la cápsula real y la envolvente de cámara continuamente, incluyendo las esquinas. Registrar anchura mínima por tramo; no usar el ancho global del CSV. Revisar arcos FBX, escalones y cambios de techo.
4. Probar regreso por las mismas escaleras y dos sentidos de circulación. Para cuatro jugadores hace falta una sesión original preparada para cuatro: el Host de desarrollo actual es de uno y no valida esa condición. No se cambia su configuración en esta auditoría.
5. Solo tras registrar esas pruebas, decidir si alguna abertura física falta realmente. En ese caso presentar objeto, coordenadas y obra mínima para autorización. Nada de ello se realiza ahora.

## Integridad, herramientas y pruebas

Los scripts auditar.mjs, auditar_mallas.mjs y presentar.mjs son herramientas de análisis offline guardadas en Docs, no componentes de Unity. Solo leen Assets y escriben esta carpeta de auditoría. No se instalaron paquetes ni se modificaron escenas, prefabs, colliders, NavMesh, población, combate, Mirror, HUD o progresión.

Pasaron las 8 comprobaciones offline de cajas/integridad, las 3 pruebas analíticas de distancia a mallas, la verificación de 108 hashes/cadenas y los controles de consistencia CSV/JSON. Estas pruebas comprueban los cálculos documentales; **no son regresiones del juego, rutas NavMesh ni una validación en Play Mode**. No se compila el juego porque no se cambió código Runtime/Editor.

Documentación consultada: guía using-unity / initialize-ai-navigation y referencias locales NavInnerWorkings.md y AboutAgents.md (AI Navigation 2.0.14). No se utilizó un canal de consulta en vivo del Editor; no se instaló ni abrió Unity para suplirlo. Evidencia nueva obtenida directamente desde Unity: ninguna; evidencia de archivos y código: la que se enumera en cada ficha.

Los archivos originales de Plan_20261117 se conservan para trazabilidad. Esta auditoría corrige su interpretación y aporta láminas nuevas; no los sobrescribe. Sin commits ni pushes.
