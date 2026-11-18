# Ciudad del Sol — recuperación de la prueba A03–R21

17/11/2026. Análisis del JSON real, sin ejecutar otra sesión de Unity ni mover al personaje.

## Dictamen

La prueba terminó por **salida del ámbito X/Z**, no porque dejara de detectar un punto ni porque se haya demostrado una caída. El mensaje «Incompleta» corresponde a la evidencia que alcanzó a guardarse: falta la llegada final a A03. No hay base para convertirlo en prueba completa ni para obligar a repetir los tramos ya acreditados.

Fuente inalterada:

`Backups/CiudadDelSol/Interior/WarriorWalk_20261117T184352415Z/Recorrido_Warriors.json`

Inicio UTC 18:43:50.538; cierre UTC 18:48:58.827; duración 308,285 s. Warrior netId=3, PlayerInstance netId=2, conexión humana local=0, un jugador. Play Mode y conexiones reales constan en el archivo. La integridad de escena, NavMesh y población fue declarada al cerrar y sus hashes siguen intactos en esta intervención.

## Causa exacta del cierre

A los **308,2835755 s**, la posición fue **(175.437225, -15.920000, 89.853615)**. El observador tenía la condición Z<90 como salida estricta; el Warrior la superó por **0,146385 m**. Y permanecía en el piso esperado. El código ejecutó Finish("FUERA_AMBITO; INCOMPLETA") antes de consultar física o guardar esa posición como muestra ordinaria. Sí conservó su evento terminal. El registro ya no observó ningún movimiento posterior, aunque la ventana siguiera abierta.

El nombre anterior FUERA_AMBITO_O_CAIDA mezclaba dos causas. No hay evidencia de una caída en ese evento. Tampoco es un falso negativo de la tolerancia de llegada: los puntos 1–7 sí fueron detectados correctamente.

## Puntos efectivamente acreditados

El analizador usa exactamente el radio original: **distancia 3D <1 m**, sin aumentarlo ni interpolar posiciones. Recorre las muestras en orden cronológico y no utiliza la salida inicial como llegada final.

| Punto | Destino (X,Y,Z) | Tiempo registrado s | Distancia al centro m | Evidencia |
| --- | --- | ---: | ---: | --- |
| 0 | (156,-16,99) | 1,900 | 0,875 | Primera muestra dentro del inicio |
| 1 | (174,-16,99) | 21,402 | 0,968 | Muestra y evento |
| 2 | (174,-16,117) | 36,239 | 0,864 | Muestra y evento |
| 3 | (120,-16,117) | 140,379 | 0,966 | Muestra y evento |
| 4 | (117,-16,123) | 182,380 | 0,964 | Llegada real a R21 |
| 5 | (120,-16,117) | 238,839 | 0,906 | Regreso |
| 6 | (174,-16,117) | 278,015 | 0,909 | Regreso |
| 7 | (174,-16,99) | 306,575 | 0,443 | Regreso |
| 8 | (156,-16,99) | **No registrado** | **Mínimo posterior 17,557 m** | Pendiente |

La tabla usa las marcas temporales de las muestras; los eventos se emitieron unos milisegundos después durante la misma observación. Las cifras redondeadas deben consultarse con precisión completa en Analisis_captura_original.json. El punto 7 representa el último extremo acreditado; el puesto de progreso `next=8` del archivo concuerda con el análisis independiente de las muestras. La menor distancia posterior al punto 7 ocurrió a los 306,840207 s. No se recupera el punto 8 a partir del testimonio ni de posiciones anteriores a completar el regreso.

## Física y navegación observadas

- **2.181 muestras ordinarias** guardadas. El contador anterior del participante indica 2.182 porque se incrementaba antes de la comprobación terminal, que interrumpió la iteración sin añadir una muestra. No hay una muestra oculta que recuperar.
- Cero muestras con fallo de apoyo/huella, cero pérdidas de NavMesh local, cero muestras sin grounded, cero contactos laterales de CharacterController y cero eventos de atasco.
- Tres muestras cerca de un límite del NavMesh; distancia mínima **0,207008 m**. El primer aviso fue alrededor de (167.763,-15.920,114.968). Todas conservan apoyo y NavMesh. Un límite de la malla no demuestra un borde físico o vacío. No se elimina el aviso ni se confunde con caída.
- Un hueco de muestreo de **1,7081523 s**, entre 273,965388 y 275,673540 s. Extremos: (168.09131,-15.92,117.69969) y (168.57707,-15.92,117.71467), separados aproximadamente 0,486 m. Se conserva como limitación; no se inventan posiciones, contactos ni continuidad en el intervalo. Las muestras de los extremos son físicamente válidas.
- No hay datos de cuatro jugadores: este archivo solo permite evaluar el Warrior local.

**Resultado físico:** no se ha encontrado un fallo físico demostrado en lo observado. Esto no certifica cada instante ni permite aprobar circulación cooperativa de cuatro personas.

## Correcciones mínimas realizadas

1. Se diferencia la salida X/Z de una cota incompatible en los eventos y el diagnóstico final. Los límites no cambiaron y el registro se sigue deteniendo al salir del ámbito.
2. La observación terminal ahora se conserva como muestra expresamente marcada `physicalNotChecked`, sin asignarle apoyo o NavMesh ficticios ni usarla para acreditar puntos.
3. Se agregó un analizador puro de las muestras, reutilizable sin Unity: respeta el orden, el radio estricto, la altura y los netId; muestra huecos, muestras inválidas y puntos pendientes. No utiliza interpolación para fabricar llegadas.
4. La ventana permite **Analizar captura existente sin modificarla** y genera análisis automático al finalizar futuras capturas. Los archivos fuente se mantienen intactos; el resultado derivado lleva referencia y SHA-256.
5. Se permite **Continuar solo desde el último punto acreditado** en una captura individual original. Verifica el prefijo mediante muestras, el ancla de inicio y los mismos hashes de escena/NavMesh/población. Registra el nuevo tramo en otro archivo, con su conexión/Warrior actuales, ruta de origen y SHA-256. La consolidación se etiqueta como tramos separados, no como una única caminata continua.
6. Guardado parcial de futuras capturas cada 30 s, para reducir pérdidas ante un cierre inesperado. No certifica ni completa capturas parciales automáticamente.
7. El inicio de nuevas capturas exige menos de un metro del punto inicial, coherente con la tolerancia de los otros puntos. Antes permitía iniciar a 2,5 m aunque esa posición no acreditara el punto 0. Este defecto potencial no explica el fallo actual: la captura real sí acredita el inicio.

No se cambiaron geometría, colliders, NavMeshData, controles, movimiento, Mirror, bootstrap ni población. No se movió al Warrior. No se modificó el JSON original.

## Comprobación puntual mínima — no repetir todo

1. Abrir la ventana **ROM → Dungeon Ciudad del Sol → Interior - Etapa 1 → Prueba funcional Warrior (manual asistida)** tras compilar.
2. En el Host habitual, llegar **caminando** a **(174,-16,99)**, último punto acreditado. Situarse a menos de un metro. No requiere volver a R21.
3. Elegir **Continuar solo desde el último punto acreditado** y seleccionar el JSON original citado arriba. No usar el botón de comenzar toda la ida/vuelta.
4. Dar foco a Game y caminar solo hacia **(156,-16,99)**, unos **18 metros hacia -X**, manteniéndose en el piso Y≈-15,92. No continuar hacia Z<90. El único punto pendiente será el 8.
5. Al llegar a menos de un metro se guardan el nuevo registro y el análisis enlazado. La captura original no cambia. Se conservan los límites históricos, incluido el hueco anterior; no se declara continuidad entre dos sesiones ni validación de cuatro jugadores.

Si se exige resolver también el pequeño intervalo sin telemetría, basta inspeccionar y registrar de forma localizada el paso de aproximadamente medio metro entre los dos extremos indicados cerca de (168,117,7). No hace falta repetir A03–R21 entero. Esa recepción específica debe revisarse como evidencia adicional; el analizador mantiene el hueco histórico visible.

El botón de análisis puede usarse fuera de Play para revisar el archivo original sin iniciar Host ni mover al personaje. Es la vía para comprobar que el informe derivado identifica punto 8 como único destino no acreditado.

## Pruebas y archivos

**17 regresiones aprobadas**, ejecutando el mismo analizador C# sobre la captura real y casos de prueba explícitamente sintéticos solo en memoria. Incluyen: conservación de 0–7; ausencia de punto 8; clasificación correcta de salida XZ; no reutilizar la salida inicial como llegada final; hueco de 1,708 s; radio estricto; salto de posición; nivel vertical; ausencia de cuatro conexiones; continuación con/sin prefijo; ancla incorrecta; muestra terminal sin física; hash original intacto.

Los casos sintéticos no son pruebas de gameplay ni aportan evidencia a la recepción. El informe original nunca se sustituye por una de esas secuencias.

Compilación Editor con Roslyn y referencias de Unity 6000.3.23f1: sin errores. Persisten solo las advertencias de Experimental NavMeshQuery del diagnóstico anterior; no se agregó ese uso. No se inició Unity ni Play Mode en esta intervención, y la UI/continuación corregidas requieren la comprobación puntual descrita.

Archivos de código:

- Modificado `Assets/ROM/Editor/CiudadDelSolInteriorWarriorWalkWindow.cs`.
- Nuevo `Assets/ROM/Editor/CiudadDelSolWalkEvidence.cs` y `.meta`.
- Nuevo este informe, análisis JSON y `Regresion_evidencia.ps1` en esta carpeta documental.
- Respaldo previo, JSON original adicional, compilación, regresiones y hashes en `Backups/CiudadDelSol/Interior/Recuperacion_20261117/`.

Desde la raíz del proyecto, repetir el análisis/regresiones sin Unity:

```powershell
& 'Docs/CiudadDelSol/Interior/Recuperacion_20261117/Regresion_evidencia.ps1'
```

**Estado:** ida A03→R21 acreditada con el Warrior real; regreso R21→(174,-16,99) acreditado; llegada final a A03 pendiente de un tramo de 18 m. No es necesario rehacer todo el recorrido. Cuatro jugadores y ancho continuo de seis metros siguen fuera de lo acreditado. No se propone ampliar arquitectura a partir de esta captura.