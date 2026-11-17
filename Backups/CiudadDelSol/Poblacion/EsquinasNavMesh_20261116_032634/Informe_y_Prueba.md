# Ciudad del Sol — impactos de raycast en esquinas

## Evidencia real y alcance

Editor_Baseline.log conserva el registro de Unity anterior a esta intervención: 23 encuentros, 1 aprobado, 22 bloqueados, 1/34 puestos. La mayoría de rechazos se producen al salir de una esquina del camino completo calculado por Unity:

- Anterior (30.700,0.050,-136.200), siguiente (30.700,0.050,-136.107), límite informado igual al anterior. Tramo 3, compartido por Pórtico/Jardines.
- Anterior (108.200,4.050,-117.600), siguiente próximo a (108.263,4.050,-117.538), límite informado igual al anterior. Tramo 5 o 6, compartido por los otros sectores.

El código descartaba la ruta ante cualquier true de NavMesh.Raycast. No registraba la prueba recíproca ni la conectividad local, y redondeaba posiciones a tres decimales. El patrón es compatible con una elección ambigua de polígono al iniciar el raycast sobre un vértice. NO demuestra por sí solo ni un corte físico real ni que los dos puntos concretos puedan aceptarse: falta ejecutar las pruebas recíprocas sobre el NavMesh real.

No se ejecutó Unity ni Play Mode. La escena conserva dos referencias NavMeshSurface (manual global y patio anterior); no se desactivó ni modificó ninguna. La referencia legacy del NavMesh de escena es nula. No se atribuyó el fallo a una superposición de NavMeshData sin evidencia.

## Corrección del validador

Archivo: Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolEncounterValidation.cs.

ConnectedStep mantiene la comprobación directa. Cuando el raycast informa un impacto:

1. El impacto debe coincidir a menos de 1 mm con un extremo del paso, y ese extremo con una esquina ORIGINAL del camino completo de Unity. Si es un impacto intermedio o simplemente un punto próximo a una esquina, se rechaza.
2. El raycast inverso debe estar libre sobre el mismo agente y máscara. Si también se bloquea, se rechaza; no se salta un supuesto hueco por ser pequeño.
3. Los caminos locales en ambos sentidos deben ser PathComplete, con los extremos coincidentes, todas las esquinas sobre el segmento y longitud sin desvío. Se permite solo igualdad numérica de 1 mm y hasta 2 mm de diferencia acumulada de longitud; esto no modifica radio, escalón, pendiente, márgenes ni separación entre plantas.
4. Solo después continúan TODAS las comprobaciones físicas existentes: apoyo exterior, diferencia NavMesh/suelo, continuidad de peldaños, pendiente, cápsula y nivel final. No se ignora ningún collider.

No hay coordenadas especiales codificadas, enlaces artificiales, traslaciones de la ruta ni aumento de tolerancias generales. Tampoco se acepta una ruta local que rodee un obstáculo para justificar un segmento recto bloqueado.

ESQUINAS_VERIFICADAS se emite solo al aprobar la ruta completa e indica los puntos cuya ambigüedad fue comprobada con evidencia recíproca. No certifica combate o movimiento dinámico. Los rechazos ahora muestran seis decimales, máscara, esquinas originales, distancias al impacto y resultado de la comprobación inversa/local. Si ambos sentidos fallan, el grupo permanece bloqueado.

## Santuario: nuevo candidato de autoría, no aprobación automática

Archivo: Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset.

Solo se cambió santuario-sur-herald: (135,4,-108) → (134,4,-108), un metro al oeste. El resto de configuración no cambió.

Evidencia:

- Registro real anterior: nav (135,4.05,-108), borde (135.4,4.05,-108), distancia 0,40 m, requisito 0,60 m. El margen no se redujo.
- El candidato se aleja de ese borde hacia el lado donde estaba el punto navegable. La distancia a ESE borde sería 1,40 m; no se supone que sea el borde más próximo en la nueva posición.
- El generador de Santuario reserva el corredor local X=-12, Z=-12, ancho 10 y largo 72, equivalente a X=133..143 y Z=-126..-54 sobre el suelo Y=4. El candidato está dentro de ese corredor. No se ejecutó ni modificó el generador.
- Atlas aprobado: candidato y entorno a Y=4, RGB(171,141,89). Los 97 píxeles dentro de 0,60 m son de ese suelo con navegación confirmada en el atlas; primer píxel distinto aproximadamente a 1,315 m. SantuarioCandidateAtlas.json conserva la lectura. La resolución del atlas y su antigüedad impiden usarlo como prueba física definitiva.

La nueva posición DEBE pasar Placement y accesibilidad en la escena real. Si tiene otro borde cercano, geometría, cadáver, jugador, otra planta o ruta inválida, no aparece. No se añadió búsqueda automática de otras posiciones, no se fuerza el spawn y no se declara físicamente validado hasta Play.

ExteriorPlan_Updated.json conserva una copia de autoría que incluye este candidato y el descanso de Pórtico ya corregido en intervenciones anteriores. Los planes históricos no se sobrescribieron.

## Verificaciones realizadas

- Respaldo de los dos archivos antes de editar, registro completo y SHA-256 Antes.json/Despues.json. Entre los archivos vigilados solo cambiaron el validador y el asset de población. Escena, datos de navegación, etapa 1, prefabs, definiciones nativas, HUD, combate, MainMenu y Horde conservan sus hashes.
- Compilación Roslyn de Unity 6000.3.23f1: ejecución y Editor, salida 0, sin errores. Persisten solo advertencias anteriores de CriliumRiftFall y RiftHub.Characters. No se escribieron DLL en Library ni se ejecutó el Editor.
- 24 comprobaciones de contrato contra el método REAL compilado con dobles de consultas: 24 aprobadas. La versión anterior falla en las cuatro pruebas de ambigüedad de esquina (origen/destino y sus retornos) bajo el mismo modelo.
- Se comprueban también impactos en medio de segmento, origen próximo pero no coincidente con esquina, raycast bloqueado en ambos sentidos, caminos locales parciales, desvíos, extremos desplazados, huecos, otra planta, recinto interior, pendientes/peldaños representados, cápsulas bajas y obstáculos.
- Estas pruebas NO ejecutan PhysX, NavMeshData, Mirror ni agentes reales. Son regresiones del contrato del código. Resultados: RouteChecks.log, RouteChecksBefore.log; fuente: RouteContractChecks.cs, fuera de Assets.
- Comparación de cambios: en PopulationExterior.asset solo varía la coordenada X citada. Sin problemas de espacios en la revisión del código; avisos normales LF/CRLF de Git. Ninguna operación de staging, commit o push.

Documentación consultada: AI Navigation 2.0.14, Documentation~/NavInnerWorkings.md, y las referencias oficiales de Unity 6000.3:
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/ai/navmesh/raycast
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/ai/navmesh/calculatepath

## Una prueba en Unity

1. Fuera de Play, esperar compilación/importación. Mantener el modo automático y limpiar Console. No hornear ni guardar la escena.
2. Presionar Play una vez y esperar PREFLIGHT_TERMINADO. Guardar el log completo.
3. Buscar ESQUINAS_VERIFICADAS para comprobar si los puntos repetidos realmente superan raycast inverso, caminos locales completos y física. Solo esos mensajes permiten confirmar que eran falsos positivos en la escena real.
4. Si siguen bloqueados, el mensaje debe mostrar si el impacto no coincide con la esquina, si también falla el inverso, si el camino local es parcial o si necesita un desvío. No ampliar la precisión ni ignorar el fallo para obtener 23/23.
5. Revisar santuario-sur: nuevo punto (134,4,-108), margen medido >=0,60 m, cápsula y ruta aprobadas. Si falla, conservar su diagnóstico; no recolocar automáticamente.
6. En grupos habilitados, revisar visualmente posiciones, recorrido y ausencia de actores en ruinas intermedias/interior. Probar una patrulla y el regreso; validar combate/respawn después. Salir sin guardar objetos temporales.

Pendiente: la ejecución de esta prueba, validación física del candidato y cualquier corte real que aparezca después. No se declara aprobado ningún encuentro sin ese resultado real.
