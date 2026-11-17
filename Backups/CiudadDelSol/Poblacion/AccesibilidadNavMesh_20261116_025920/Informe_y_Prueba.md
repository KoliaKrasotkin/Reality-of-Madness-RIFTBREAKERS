# Ciudad del Sol — accesibilidad sobre el NavMesh existente

## Resultado y límites

Se corrigió únicamente Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolEncounterValidation.cs. No se ejecutó Unity ni Play Mode. No se afirma que los 23 encuentros hayan pasado ni que los 34 puestos puedan aparecer. Escena, colliders, NavMeshData, prefabs, configuración de población y combate permanecen intactos respecto del respaldo de esta intervención.

## Registro real

Editor_Baseline.log conserva el registro completo; PreflightOriginal.txt extrae sus mensajes de encuentros. Incluye una ejecución completa con 1 encuentro aprobado, 22 bloqueados, 1/34 puestos. El final del registro contiene otra ejecución parcial con el mismo patrón; no se la contabilizó como una validación completa nueva.

La entrada aprobada no cruza la escalinata. Los restantes recorridos que llegan a evaluarse obtienen PathComplete antes de fallar en la comprobación adicional. Ejemplos de fallos físicos locales al tramo común:

- NavMesh no encontrado a 0,25 m de (31.483,-2,-160.165), (28.480,-2,-160.158) y (27.856,-2,-160.177).
- Cápsula de jabalí/lobo obstruida cerca de (27.844,-2,-160.276) y (31.479,-2,-160.264).
- Collider informado: Salida_Meridional_Generated/02 - Escalinata monumental - 8 peldanos/Platform_Base_01, Layer 0, normal horizontal.

Por tanto el log no demuestra una desconexión del NavMesh: el rechazo se produce después de obtener el camino completo. Tampoco demuestra por sí solo que la escalinata sea físicamente segura para todos los cuerpos. Eso lo debe confirmar el nuevo contraste de navegación y física.

## Causa del validador anterior

La versión anterior ya no utilizaba la altura interpolada para el raycast, pero elegía el suelo desde la altura del apoyo anterior y después buscaba NavMesh a solo 0,25 m de ese suelo. El límite previsto de discrepancia NavMesh/suelo era, sin embargo, 0,36 m. Una superficie de navegación que aproxima peldaños como una pendiente puede cumplir el límite de 0,36 m y fallar antes por el radio de 0,25 m. Además, un origen de raycast bajo puede quedar dentro del siguiente bloque y volver a detectar el inferior. La cápsula se colocaba en ese apoyo inferior en lugar de la altura real por donde circularía el NavMeshAgent.

CiudadDelSolSouthernExitGenerator documenta ocho peldaños de 0,25 m, con módulos de 6 m solapados bajo las huellas. Este caso exige distinguir el apoyo físico de la superficie de navegación suavizada. No se ejecutó ese generador ni se modificaron sus fuentes.

La altura real del NavMesh en los puntos que fallaban no estaba registrada: SIN_NAVMESH_LOCAL solo imprimía el suelo. Por ello la magnitud exacta de esa discrepancia en la escena sigue pendiente de observar en Unity; no se dedujo una altura ficticia de los datos de interpolación.

## Corrección

Se mantiene CalculatePath con el filtro del agente y PathComplete obligatorio en cada sentido. Se recorre cada segmento del camino devuelto por Unity, no la recta entrada-enemigo. En cada avance horizontal de hasta 0,10 m:

1. Se consulta la superficie nativa cerca de la posición NavMesh anterior. La altura no procede del suelo inferior ni de una recta interpolada.
2. El alcance de búsqueda deriva del escalón real del agente y del avance horizontal, más 0,03 m de margen de consulta. Esto NO amplía la aceptación de escalones o plantas. La desviación horizontal máxima es 0,025 m y la variación vertical queda limitada al escalón configurado.
3. NavMesh.Raycast entre posiciones consecutivas debe confirmar continuidad. Un límite, hueco o área no permitida bloquea el grupo. No se aceptan rutas parciales ni se utilizan NavMeshLinks.
4. Desde la altura nativa se busca el apoyo físico en una ventana vertical acotada. Se exige procedencia exterior, diferencia NavMesh/suelo <=0,36 m, cambio de apoyo <= escalón del agente y pendiente permitida.
5. Se comprueba la cápsula a la altura del NavMeshAgent y su baseOffset original. Se conserva el radio del cuerpo y la banda de escalón previa; no se ignoran colliders ni obstrucciones. Esto también evita hundir artificialmente cuerpos bajos de 0,90 m en el peldaño inferior.
6. Se exige alcanzar el destino y su nivel físico; las patrullas siguen sin poder cruzar recuperaciones.

Project para referencias de origen/recuperación y Placement para spawns NO se modificaron. No se ensancharon máscaras ni se admitieron ruinas intermedias o interiores. El filtro de ataque entre plantas tampoco se modificó.

La documentación de Unity confirma que NavMesh.Raycast comprueba obstáculos de navegación en 2,5D, incluidos huecos y pendientes: https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/ai/navmesh/raycast . También se consultó AI Navigation 2.0.14, Documentation~/NavInnerWorkings.md. Se evaluó NavMeshQuery.MoveLocation, pero no se incorporó esa API experimental obsoleta ni se añadieron paquetes.

## Santuario, separado de accesibilidad

El puesto santuario-sur-herald en (135,4,-108) tiene navegación local a (135,4.05,-108), pero el borde está en (135.4,4.05,-108): distancia 0,40 m frente al mínimo 0,60 m del agente (0,35 m + reserva 0,25 m).

Es un incumplimiento medido del margen de colocación, no una ruta parcial ni una ausencia de suelo. No demuestra por sí solo que el borde sea un precipicio: puede ser un borde alrededor de un obstáculo. Se conserva el criterio, la posición y el bloqueo. Cambiar esa posición requiere elegir y verificar otro punto físico; no se hizo una búsqueda automática ni se habilitó el puesto por excepción. Por ello no debe esperarse 23/23 mientras esa ubicación siga sin cumplir su margen.

## Verificaciones ejecutadas

- Respaldo previo del código, configuración y log en esta carpeta.
- SHA-256 Antes.json/Despues.json: solo difiere CiudadDelSolEncounterValidation.cs entre los archivos vigilados. Exterior/interior, escena, NavMesh, configuración, recursos originales y agente nativo protegidos permanecen iguales.
- Compilación con Roslyn de Unity 6000.3.23f1: ejecución y Editor, salida 0, sin errores. Advertencias previas CS0108 CriliumRiftFall y CS0414 RiftHub.Characters. No se sobrescribieron DLL de Unity.
- 14 comprobaciones de contrato contra el método Route REAL compilado con dobles de consulta: 0 fallos. Incluyen subida/bajada, curva con esquina, cuerpo bajo, PathPartial, corte nativo, hueco físico, otra planta, recinto interior, escalón excesivo, obstáculo y falta de NavMesh.
- Se compiló el método anterior respaldado con el mismo modelo: falla en 11/14, incluidos los tres recorridos válidos y sus retornos. La regresión reproduce el rechazo por consulta de 0,25 m aun cuando la separación de alturas permanece dentro de 0,36 m.
- Estas pruebas son un modelo de consultas, NO PhysX, NavMeshData real, movimiento de agentes, Mirror o Play Mode. Código, resultados y response files están en RouteContractChecks.cs, RouteChecks*.log y RouteChecks*.rsp, fuera de Assets.
- Unity CLI no encuentra un Editor conectado a Pipeline. No se instaló nada, no se inició Play ni Host y no se horneó navegación.
- Revisión de diferencias respecto del respaldo: sin errores de espacios; solo avisos de normalización LF/CRLF. No se hicieron operaciones de staging, commit o push.

## Una prueba en Unity

1. Fuera de Play, esperar compilación/importación, limpiar Console y conservar habilitado el modo automático existente. No hornear ni guardar escenas.
2. Presionar Play una vez y esperar PREFLIGHT_TERMINADO. Guardar el log completo, no solo el resumen.
3. Revisar primero ACCESIBILIDAD_ENTRADA: ahora distinguirá RUTA_SIN_NAVMESH, RUTA_PROYECCION_FUERA_DEL_CORREDOR, RUTA_NAVMESH_INTERRUMPIDA, RUTA_SIN_APOYO, RUTA_APOYO_INCOMPATIBLE y RUTA_CAPSULA_OBSTRUIDA. Los mensajes muestran posiciones nativas, suelo, tramo, límites y collider cuando corresponde.
4. Si persiste un fallo común en la escalera, comprobar exactamente sus posiciones de NavMesh y suelo, sin ampliar tolerancias ni desactivar seguridad. El nuevo log debe demostrar si queda un problema físico, de navegación o de consulta.
5. Mantener separado SPAWN_MARGEN_INSUFICIENTE del Santuario, que continuará siendo un bloqueo legítimo del criterio vigente.
6. En encuentros habilitados, observar patrullas y recorrer la escalinata con el Warrior. Mantener las pruebas de movimiento de enemigos, combate, respawn y cuatro jugadores como validaciones funcionales posteriores; el preflight no las certifica.

Pendiente: ejecución real de esta prueba, cualquier obstrucción física que revele y nueva ubicación segura del puesto del Santuario. El coste del muestreo se concentra en el preflight de arranque, un grupo por tick; requiere observarse en Unity. No se declara que los 23 encuentros hayan quedado resueltos.
