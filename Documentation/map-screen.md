# Mapa M ilustrado — corte 22/10/2026

La ilustración dark está activa mediante `RiftCartographicProjection.IllustratedMapApproved`. Textura y calibración: `Assets/ROM/Resources/RiftWorldMap/`. El PNG y el recurso del atlas real permanecen intactos; el minimapa continúa usando su captura literal. M conserva fallback literal si faltan recursos o los bounds/triángulos de calibración son incompatibles.

## Presentación e interacción

`RiftModernMapFrame` es exclusivo del mapa: carbón translúcido, borde redondeado fino, glow violeta, iconos, toggles de cristales/refugios y encuentros y recenter. No hay blur ni cambios al tema global. Oculta el HUD de gameplay mediante CanvasGroup temporal y el botón MAPA antiguo durante M; restaura su estado al cerrar/deshabilitar. No modifica inventario.

M/Escape, rueda y drag mantienen foco/cursor. El receptor de clic reenvía explícitamente el drag al ScrollRect; un drag no selecciona coordenadas. El clic en terreno usa la inversa para mostrar X/Z reales, sin teletransportar ni ordenar movimiento. No se inventaron filtros de misiones, quests ni recompensas.

## Calibración

235 anclas y 464 triángulos; dominio UV inferior izquierdo [0,1]². Centro X/Z (103.7597046, -9.0951843), extensión (2027.888794, 2027.888794). `Projection.json` guarda anclas con nombres, triángulos y rutas medidas. Los contornos e interiores de soporte son correspondencias manuales cartográficas aproximadas, no colisionadores ni límites exactos de costa.

Los extremos y anchos de los nueve puentes se extrajeron de las mallas de la escena actual con `RiftMapCalibrationDump.Run`, que exporta a `Logs/MapCalibration/world-anchors.json` sin guardar escena. Hay anclas transversales en extremos/cuartos y muestras intermedias que no son anclas. Se excluyeron soportes aproximados que contradecían los corredores.

Las mismas coordenadas baricéntricas sirven en ambas direcciones. `RiftFieldUI.TryPosition`, `TryWorldPosition` y `Heading` centralizan jugador, pines, clic inverso y rumbo. La flecha proyecta un pequeño vector de avance; no reutiliza el ángulo del mundo sin transformarlo. Zoom/pan actúan después sobre todo el contenido. No se cambia el escenario para ajustarlo al arte.

La malla es positiva y manifold, con cuatro aristas externas y área unitaria en ambos dominios. Las consultas fuera de dominio fallan sin extrapolación; se oculta el marcador fuera del área cartografiada y se informa el estado. Una edición de geografía requiere recalibrar y repetir pruebas. El mapa M tiene proyección independiente del crop rectangular del minimapa.

## Pruebas y evidencia

Entrada independiente de inventario: `ROM.EditorTools.RiftAchievementStandaloneReview.Run -riftMapIntegrationReview`. `-riftMapOnlyReview` omite Logros; `-riftIllustratedMapReview` permite candidato antes de activar el flag, y no fue necesario en la corrida final.

- Editor D3D11, mapa dedicado: 62 PASS, 0 FAIL.
- Editor D3D12 habitual, Logros + mapa con arte activo normalmente: 79 PASS, 0 FAIL.
- 441 ida/vuelta; error máximo registrado 0.00027 m. Fuera de dominio rechazado.
- 41 posiciones por puente, cinco rumbos por posición; máximo 0.520 px contra los corredores ilustrados medidos, en arte de 1254².
- 112 centros de caminos reales renderizados tienen ida/vuelta dentro de 4 cm. Esto no prueba precisión pictórica por baldosa.
- 63 muestras del actor de servidor en los nueve puentes: posición y rumbo del marcador correctos. Se usó teleport controlado entre muestras, no conducción continua ni pathfinding.
- Clic inverso, mouse wheel, drag, filtros por clic/submit de teclado, recenter, M/Escape, reapertura y restauración del HUD.
- Escena fuente sin cambios; sin modificaciones de rigs, controladores, prefabs de personaje, IDs o balance.

Las trazas cyan son overlay temporal de diagnóstico Editor, ausente de UI normal. La revisión visual las sitúa sobre los nueve puentes pintados. La proximidad de píxeles beige solo apoya esa revisión y no identifica semánticamente todos los caminos. La ilustración abstrae edificios, vegetación y costa; no sustituye NavMesh o colisiones.

## Limitación visual abierta

La captura API de zoom D3D12 vuelve a perder texto de algunos controles; la equivalente D3D11 conserva esos textos. Los checks funcionales no resuelven esa diferencia ni demuestran qué muestra una captura independiente del escritorio. No se reabrieron fuentes compartidas/inventario ni se cambió el backend gráfico global.

Sin builds, multiplayer remoto, optimización, cleanup, login o push.
