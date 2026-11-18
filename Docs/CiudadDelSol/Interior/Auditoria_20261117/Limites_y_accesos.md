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


### R01

- N +Z en (-105, -16, 132), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-114, -16, 123), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-96, -16, 125.25), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-108.75, -16, 114), intervalo nominal 10.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-98.25, -16, 114), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-96, -16, 114.75), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R02

- N +Z en (-105, -16, -12), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-102, -16, -30), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-90, -16, -20.25), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-120, -16, -21), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-117.75, -16, -30), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-120, -16, -29.25), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R03

- N +Z en (-105, -16, -36), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-102, -16, -54), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-90, -16, -44.25), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-120, -16, -42.75), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-117.75, -16, -54), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-120, -16, -53.25), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R04

- N +Z en (-105, -16, 12), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-102, -16, -6), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-90, -16, 3.75), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-120, -16, 8.25), intervalo nominal 7.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-117.75, -16, -6), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-120, -16, 0), intervalo nominal 3 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R05

- E +X en (-90, -16, 48), intervalo nominal 33 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-111.75, -16, 66), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-111.75, -16, 30), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-120, -16, 42), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-96, -16, 66), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-96, -16, 30), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R06

- O −X en (-126, -16, -116.25), intervalo nominal 64.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-117, -16, -84), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-108, -16, -138), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-108, -16, -120), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-108, -16, -102), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-120, -16, -150), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R07

- O −X en (-138, -16, -20.25), intervalo nominal 64.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-129, -16, 12), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-120, -16, -42), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-132, -16, -54), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-120, -16, -21), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-120, -16, -9), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R08

- O −X en (-138, -16, 42), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-120, -16, 42), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-132, -16, 54), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-132, -16, 30), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-122.25, -16, 54), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-122.25, -16, 30), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R09

- S −Z en (-129, -16, 60), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-138, -16, 68.25), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-120, -16, 71.25), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-132, -16, 78), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-122.25, -16, 78), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-120, -16, 60.75), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R10

- N +Z en (-141, -16, -132), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-150, -16, -140.25), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-132, -16, -138.75), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-144, -16, -150), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-134.25, -16, -150), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-132, -16, -149.25), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R11

- E +X en (-6, -16, 132), intervalo nominal 69 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-15, -16, 90), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-24, -16, 102), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-24, -16, 120), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-24, -16, 138), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-24, -16, 156), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R12

- N +Z en (-33, -16, 6), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-42, -16, -3), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-24, -16, -0.75), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-36.75, -16, -12), intervalo nominal 10.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-26.25, -16, -12), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-24, -16, -11.25), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R13

- O −X en (-42, -16, -27), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-33.25, -16, -18), intervalo nominal 17.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-24, -16, -25), intervalo nominal 13 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-36.75, -16, -36), intervalo nominal 10.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-26.25, -16, -36), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-24, -16, -35.25), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R14

- N +Z en (-33, -16, 30), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-42, -16, 21), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-24, -16, 23.25), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-36.75, -16, 12), intervalo nominal 10.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-26.25, -16, 12), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-24, -16, 12.75), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R15

- O −X en (-42, -16, 51), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-33, -16, 66), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-24, -16, 46.5), intervalo nominal 12 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-36.75, -16, 36), intervalo nominal 10.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-24, -16, 62.25), intervalo nominal 7.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-26.25, -16, 36), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R16

- O −X en (-6, -16, 111.75), intervalo nominal 28.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (3, -16, 126), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (12, -16, 108), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (0, -16, 96), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (12, -16, 122.25), intervalo nominal 7.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (9.75, -16, 96), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R17

- S −Z en (-51, -16, 114), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-66, -16, 123), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-36, -16, 125.25), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-60.75, -16, 132), intervalo nominal 10.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-48, -16, 132), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-38.25, -16, 132), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R18

- E +X en (-54, -16, 45.75), intervalo nominal 28.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-63, -16, 60), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-72, -16, 43.5), intervalo nominal 12 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-60, -16, 30), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-69.75, -16, 30), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-72, -16, 57.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R19

- O −X en (-90, -16, -98.25), intervalo nominal 52.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-69, -16, -72), intervalo nominal 42 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-48, -16, -114), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-48, -16, -96), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-48, -16, -78.75), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-84, -16, -126), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R20

- S −Z en (21, -16, 78), intervalo nominal 42 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (42, -16, 86.25), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (0, -16, 89.25), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (12, -16, 96), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (24, -16, 96), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (36, -16, 96), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R21

- O −X en (102, -16, 123), intervalo nominal 42 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (117, -16, 102), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (132, -16, 132), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (132, -16, 112.5), intervalo nominal 12 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (114, -16, 144), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (126, -16, 144), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R22

- O −X en (114, -16, -86.25), intervalo nominal 64.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (123, -16, -54), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (132, -16, -108), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (132, -16, -90), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (132, -16, -72), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (120, -16, -120), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R23

- N +Z en (123, -16, 174), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (114, -16, 165), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (132, -16, 167.25), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (126, -16, 156), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (116.25, -16, 156), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R24

- N +Z en (147, -16, -102), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (162, -16, -110.25), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (132, -16, -108.75), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (144, -16, -120), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (156, -16, -120), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (134.25, -16, -120), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R25

- E +X en (150, -16, -8.5), intervalo nominal 19 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (150, -16, 14.5), intervalo nominal 19 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (141.25, -16, -18), intervalo nominal 17.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (144.75, -16, 24), intervalo nominal 10.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (132, -16, 12), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (134.5, -16, 24), intervalo nominal 4 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R26

- E +X en (150, -16, 159), intervalo nominal 78 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (141, -16, 198), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (132, -16, 132), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (132, -16, 168), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (132, -16, 186), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (138, -16, 120), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R27

- N +Z en (165, -16, 198), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (150, -16, 189), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (162, -16, 180), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (174, -16, 180), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (180, -16, 194.25), intervalo nominal 7.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (152.25, -16, 180), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R28

- N +Z en (171, -16, -30), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (186, -16, -38.25), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (168, -16, -48), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (180, -16, -48), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (156, -16, -40.5), intervalo nominal 6 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (158.25, -16, -48), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R29

- N +Z en (171, -16, -102), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (162, -16, -110.25), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (180, -16, -108.75), intervalo nominal 13.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (168, -16, -120), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (177.75, -16, -120), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (180, -16, -119.25), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R30

- N +Z en (188.75, -16, 138), intervalo nominal 29.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (174, -16, 129), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (204, -16, 130.5), intervalo nominal 12 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (186, -16, 120), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (198, -16, 120), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (176.25, -16, 120), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R31

- S −Z en (189, -16, 186), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (198, -16, 195), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (180, -16, 196.5), intervalo nominal 12 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (186, -16, 204), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (195.75, -16, 204), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (180, -16, 186.75), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R32

- O −X en (198, -16, -15), intervalo nominal 66 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (207, -16, 18), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (216, -16, -36), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (216, -16, -18), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (216, -16, 0), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (210, -16, -48), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R33

- N +Z en (51, -16, 174), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (51, -16, 162), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (66, -16, 168), intervalo nominal 12 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (36, -16, 171.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (36, -16, 162.75), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### R34

- E +X en (66, -16, 116.25), intervalo nominal 76.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (57, -16, 78), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (48, -16, 90), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (48, -16, 108), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (48, -16, 126), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (48, -16, 144), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G01

- E +X en (222, -16, 93), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (213, -16, 108), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (213, -16, 78), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (204, -16, 93), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (204, -16, 80.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (204, -16, 105.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G02

- E +X en (222, -16, 168), intervalo nominal 36 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (204, -16, 168), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (213, -16, 186), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (213, -16, 150), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (204, -16, 152.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (204, -16, 183.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G03

- E +X en (174, -16, 144), intervalo nominal 36 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (156, -16, 144), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (165, -16, 162), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (165, -16, 126), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (156, -16, 128.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (156, -16, 159.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G04

- E +X en (222, -16, 129), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (213, -16, 144), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (213, -16, 114), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (204, -16, 130.5), intervalo nominal 12 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (204, -16, 116.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (204, -16, 141.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G05

- O −X en (66, -16, 120), intervalo nominal 36 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (84, -16, 120), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (75, -16, 138), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (75, -16, 102), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (84, -16, 104.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (84, -16, 135.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G06

- O −X en (-150, -16, -102), intervalo nominal 36 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-141, -16, -84), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-141, -16, -120), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-132, -16, -108), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-132, -16, -96), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-132, -16, -117.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G07

- O −X en (-90, -16, 81.75), intervalo nominal 28.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-81, -16, 96), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-80.25, -16, 66), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, 81), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, 68.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, 93.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G08

- O −X en (-138, -16, 105), intervalo nominal 42 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-120, -16, 105), intervalo nominal 27 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-129, -16, 126), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-129, -16, 84), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-120, -16, 86.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-120, -16, 123.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G09

- O −X en (162, -16, -78), intervalo nominal 36 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (180, -16, -78), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (171, -16, -60), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (171, -16, -96), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (180, -16, -93.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (180, -16, -62.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G10

- O −X en (-90, -16, 45.75), intervalo nominal 28.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-81, -16, 60), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-80.25, -16, 30), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, 43.5), intervalo nominal 12 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, 57.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, 30.75), intervalo nominal 1.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G11

- E +X en (114, -16, 165), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (105, -16, 180), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (105, -16, 150), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (96, -16, 165), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (96, -16, 152.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (96, -16, 177.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G12

- O −X en (-54, -16, 93), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-45, -16, 108), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-45, -16, 78), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-36, -16, 93), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-36, -16, 80.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-36, -16, 105.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G13

- O −X en (18, -16, 114), intervalo nominal 24 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (27, -16, 126), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (27, -16, 102), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (36, -16, 114), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (36, -16, 104.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (36, -16, 123.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G14

- O −X en (-90, -16, 117), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-81, -16, 132), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-81, -16, 102), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, 117), intervalo nominal 15 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, 104.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, 129.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G15

- O −X en (-114, -16, 90), intervalo nominal 36 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-96, -16, 90), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-105, -16, 108), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-105, -16, 72), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-96, -16, 74.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-96, -16, 105.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### G16

- O −X en (174, -16, -11.5), intervalo nominal 25 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (192, -16, -6), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (183, -16, 12), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (183, -16, -24), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (174, -16, 8.5), intervalo nominal 7 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (192, -16, -21.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### M01

- O −X en (6, -16, 153), intervalo nominal 42 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (21, -16, 174), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (21, -16, 132), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (36, -16, 137.25), intervalo nominal 10.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (36, -16, 150), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (36, -16, 160.5), intervalo nominal 6 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### M02

- E +X en (198, -16, 162), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (180, -16, 162), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (189, -16, 144), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (189.75, -16, 180), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (198, -16, 146.25), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (198, -16, 177.75), intervalo nominal 4.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### M03

- E +X en (-72, -16, -30), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (-72, -16, -6), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-90, -16, -18), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (-81, -16, 12), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (-81, -16, -48), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-90, -16, -39.75), intervalo nominal 16.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### M04

- O −X en (138, -16, -63), intervalo nominal 66 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (156, -16, -72), intervalo nominal 21 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (147, -16, -30), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (147, -16, -96), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (156, -16, -52.5), intervalo nominal 12 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (156, -16, -90.75), intervalo nominal 10.5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### A00

- S −Z en (132, 20, 0), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (147, 20, 0), intervalo nominal 3 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### A01

- N +Z en (162, 4, 30), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### A02

- S −Z en (162, 4, 30), intervalo nominal 9 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### A03

- S −Z en (150, -16, 90), intervalo nominal 5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- E +X en (168, -16, 99), intervalo nominal 5 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### D01

- S −Z en (96, -34, 30), intervalo nominal 11 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### D02

- N +Z en (96, -34, 30), intervalo nominal 11 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### D03

Sin boca coplanar acreditada por este muestreo de límites; revisar los accesos verticales y conexiones de la ficha.

### B01

Sin boca coplanar acreditada por este muestreo de límites; revisar los accesos verticales y conexiones de la ficha.

### B02

Sin boca coplanar acreditada por este muestreo de límites; revisar los accesos verticales y conexiones de la ficha.

### B03

- E +X en (48, -24, 39), intervalo nominal 3 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (-12, -24, 39), intervalo nominal 3 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### B04

- N +Z en (21, -30, 42), intervalo nominal 3 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (21, -30, -6), intervalo nominal 3 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.

### B05

Sin boca coplanar acreditada por este muestreo de límites; revisar los accesos verticales y conexiones de la ficha.

### B06

Sin boca coplanar acreditada por este muestreo de límites; revisar los accesos verticales y conexiones de la ficha.

### P01

- E +X en (18, -38, 15), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- O −X en (0, -38, 15), intervalo nominal 30 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- N +Z en (9, -38, 30), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
- S −Z en (9, -38, 0), intervalo nominal 18 m. Pendiente: puerta efectiva, continuidad de cápsula y FBX/Unity.
