# Etapa 5 — recepción técnica individual

Fecha: 18/11/2026. Unity 6000.3.23f1, Editor gráfico, Edit Mode. **VS04 y VS05: 2/2 aprobadas técnicamente.** No se ejecutó Play Mode ni una partida con jugadores. Esta recepción no acredita cuatro jugadores reales.

## Arquitectura y alcance

Se retomó `CiudadDelSolInteriorStageFive.cs`, reutilizando el motor físico y de recepción de Etapas 2–4; no se ejecutaron generadores globales. Se consultaron el Plan_20261117, la auditoría de 68 espacios/73 conexiones y los dossiers anteriores. Las conexiones existentes resultaron seguras: no fue necesario construir, ensanchar, eliminar ni sustituir piezas.

- **VS04 B01–B03:** piso Y=-24, eje Z=39; recorrido (51,-24,39) → (45,-24,39) → (18,-24,39) → (-15,-24,39). Puente este-oeste de 6 m nominales; se conservan pavimento modular y parapetos originales.
- **VS05 B02–B04:** piso Y=-30, eje X=21; recorrido (24,-30,45) → (21,-30,45) → (21,-30,39) → (21,-30,-3) → (21,-30,-9). Incluye los tres metros de enlace desde Etapa 4, que cuentan con suelo existente.
- Los puentes se superponen en X/Z=(21,39), separados verticalmente 6 m. **No hay una conexión vertical entre ellos.** La altura libre real inferior está condicionada por la estructura superior, no solo por las cotas de los pisos.

## Pruebas reales ejecutadas

Se utilizó el NavMesh cargado de la escena original y PhysX real, sin simulación de movimiento ni consultas experimentales NavMeshQuery nuevas. Se comprobó ruta completa en ambos sentidos tanto por los puntos de paso originales como mediante caminos globales naturales, sin forzar puntos intermedios. Ambos procedimientos aprobaron 2/2.

Las comprobaciones reutilizadas incluyen apoyo, huella, cápsula y barridos del Warrior original, obstáculos, desniveles, pendientes, escalones y protección lateral. Se conservan radio del Warrior 0,32 m, altura 2 m, escalón 0,30 m y pendiente 48°; agente Humanoid 0 radio 0,57 m, escalón 0,36 m y vóxel 0,10 m. No se cambiaron tolerancias.

| Conexión | Estaciones a 0,10 m | Fallos | Ancho útil mínimo | Pendiente máxima medida | Altura libre mínima |
|---|---:|---:|---:|---:|---:|
| VS04 | 663 | 0 | 4,04 m | 0° | 15,98 m |
| VS05 y enlace | 574 | 0 | 4,04 m | 18,92° | 3,23 m |

Son 1.237 estaciones, incluyendo extremos repetidos entre segmentos. La pendiente de VS05 corresponde a la interfaz con el descenso existente de Etapa 4. Las cuatro cápsulas estáticas laterales superaron las comprobaciones; esto es una comprobación geométrica, no una prueba cooperativa real. No se encontraron bordes desprotegidos dentro de los recorridos evaluados. El muestreo tiene resolución finita y no certifica todos los puntos de los anillos completos B01/B02.

### Regresiones nuevas en Unity

- Etapa 1: 1/1.
- Etapa 2: 6/6.
- Etapa 3: 3/3.
- Etapa 4: 2/2, además de sus 854 muestras laterales de escaleras.

El informe `RegresionesIniciales.txt` registra True. `Resultado.txt` registra True para las dos recepciones de Etapa 5 y el perfil denso. Las evidencias nativas completas están en `Backups/CiudadDelSol/Interior/Etapa5_20261118/`: Inicial/Recepcion.json, GlobalDirecta/Recepcion.json, Perfil_denso.jsonl y RegresionesIniciales/. Los JSON incluyen hashes, configuración y resultados individuales.

## Inspección visual real

Se recorrieron nueve encuadres del Editor: conjunto, ambos extremos y centro de VS04; enlace con Etapa 4, cruce a distinta altura, centro, extremo sur y contracara de VS05. Las capturas originales están en Capturas/ y Galeria.html. Se utilizó aislamiento temporal de visualización del núcleo y las adiciones de Etapa 4, luego se restauró la visibilidad.

No se observaron huecos, piezas flotantes, materiales ausentes ni intersecciones que requieran reparación en las superficies inspeccionadas. Los parapetos mantienen continuidad lateral y dejan libres los accesos. El puente superior forma una cubierta local sobre el inferior, físicamente despejada según cápsulas y altura libre medida. Las perspectivas se eligieron para revisar también uniones y caras laterales; no sustituyen una revisión de cada polígono oculto del conjunto.

## Archivos y seguridad

Archivos nuevos/completados: Assets/ROM/Editor/CiudadDelSolInteriorStageFive.cs y su .meta; este dossier, Recepcion.csv, Plano.svg, Galeria.html, capturas y manifiesto SHA-256. El receptor es exclusivo de Editor, manual por menú ROM → Etapa 5 interior → Recibir VS04 VS05 sin obras; la solicitud automatizada explícita se consume una sola vez. No instala comportamiento de gameplay.

Se respaldaron escena, NavMeshData, sus .meta y dependencias antes de continuar. `SHA256_sin_cambios.json` acredita igualdad antes/después de los siete archivos protegidos: escena, navegación y herramientas compartidas. No se guardó la escena, no se horneó ni cambió NavMeshData, no se modificaron prefabs ni sistemas protegidos. Los cambios que Git ya muestra en escena y NavMeshData son anteriores a esta etapa y se conservaron. No hay archivos preparados en staging. No hubo commits ni pushes.

La compilación independiente del ensamblado Editor mediante Roslyn y referencias del proyecto pasó; Unity importó el receptor y ejecutó sus métodos reales. Quedan avisos CS0618 anteriores de herramientas experimentales ajenas a este receptor; la consola observada al terminar muestra cero errores. Ver Compilacion.log en el respaldo de Etapa 5.

## Pendientes y siguiente etapa

La recepción técnica individual de Etapa 5 está completa. Sigue pendiente una prueba funcional de cuatro Warriors reales (cruce en sentidos opuestos, cámara, movimiento y red); tampoco se ejecutó desplazamiento real de enemigos en esta intervención. Se conservan las limitaciones previas: último tramo manual de Etapa 1 sin telemetría y atajo inverso global de VS01 no acreditado por aquella recepción. No se presentan como resueltas.

Recomendación de Etapa 6: auditar primero VS03 y el descenso desde B02 hacia Y=-38, delimitando la intervención antes de la cámara final. Ruta documental de referencia: (24,-30,45) → (-9,-30,45) → (-9,-30,36) → (-9,-38,12) → (-9,-38,9) → (18,-38,9). Es una propuesta pendiente de inspección y autorización; no se construyó ni aprobó en esta etapa.
