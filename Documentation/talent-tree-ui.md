# Árbol de talentos Warrior

La UI usa el modelo existente de WarriorProgression: nueve talentos, tres cadenas independientes de tres y seis dependencias Parent. No se alteraron RequiredLevel, MaximumRank, puntos por nivel ni fórmulas de combate. No se agregaron talentos para imitar el tamaño del árbol de Diablo.

## Representación

Árbol radial con un núcleo visual Warrior. Las guías punteadas unen visualmente las ramas al núcleo; no son requisitos. Las rutas continuas corresponden exactamente al Parent real. Ruta cálida gruesa: hijo con puntos invertidos. Bronce: padre completo que permite avanzar. Gris: requisito todavía sin cumplir. Los nodos muestran icono, rango actual/máximo y marcas de rangos invertidos.

Los iconos ilustrados se reutilizan de las habilidades existentes; no son nueve ilustraciones nuevas. No se reemplazaron los tres iconos Warrior restaurados ni el controlador de Gori. Carbón/bronce/ámbar sustituye acentos procedurales magenta de marcos, paneles y botones.

## Uso

P o el binding guardado de Talentos abre el árbol. Clic / Enter en un nodo disponible compra un rango por la ruta CmdTalent existente. Los nodos bloqueados permiten inspeccionar su tooltip y explican el requisito. Hover y foco muestran efecto actual, siguiente rango, costo y requisitos. Las fórmulas de texto se centralizan en RiftTalentPresentation y reflejan AbilityController.Progression/CombatController actuales; al cambiar balance, actualizar ambos.

Arrastrar mueve el árbol. La rueda y +/- cambian zoom con límites 0.3–1.3. Centrar vuelve a Warrior; Ver todo muestra el conjunto. La selección por teclado lleva el nodo enfocado al área visible. Comprar no reconstruye ni recentra el árbol. Reiniciar exige confirmación y usa el comando de servidor existente. Escape vuelve al juego.

La autoridad sigue en servidor: nivel, puntos, máximo rango, padre completo y CanChangeBuild. Cambios en refugio o fuera de combate; no durante una habilidad. El tooltip describe contribución del talento, no estadísticas totales del personaje.

## Validación

ROM.EditorTools.RiftIdentityReview.Run -riftTalentReview usa cuenta y diario aislados, ruta real menú→Warrior→El Rift, y restaura preferencias/bindings. El nivel diez de las capturas se obtiene solo para ese personaje de prueba usando la ruta de XP del servidor. No se guardan escenas ni se modifica progreso del usuario.

La corrida radial prueba compras reales, límites y requisitos, tooltips, confirmación/reset, Ver todo, rueda y pan por arrastre continuo. La corrida previa de columnas no valida la versión radial; consultar el reporte actual Logs/IdentityReview/rift-talent-validation.txt y las capturas asociadas.

No cliente remoto separado, móvil ni build standalone. Este árbol muestra la mecánica real limitada a nueve talentos; más bifurcaciones requieren diseño de progresión adicional. La composición con personaje de cuerpo entero y la referencia Beast of Reincarnation quedan para una fase visual posterior. Inventario general/paperdoll, mapa y flamas no forman parte de este cambio.

Resultado radial final: 64 checks PASS, incluyendo 29 de identidad/entrada y 35 de árbol/progresión/navegación. Compilación Unity correcta, pan continuo real, zoom y overview comprobados. La primera prueba de pan enviaba un solo movimiento: se corrigió el gesto del harness y se repitió; no se cambió ScrollRect para ocultar ese fallo. Capturas radiales finales 1280x720 revisadas. Overview sirve para orientación: los rótulos son pequeños y requieren zoom. Piloto con Warrior de cuerpo entero, ficha compacta y más transparencias autorizado para la siguiente composición, todavía no aplicado en este checkpoint.

## Piloto de personaje y transparencia — validado localmente

La composición está habilitada en RiftHub: personaje real a la izquierda, árbol central ampliado y ficha derecha con efecto actual/siguiente, rango, costo y requisitos. La cámara de inspección copia solo la presentación del actor actual, conservando skin y equipo activo; retira scripts, audio, colisiones y cuerpos físicos del clon. Arrastrar el personaje permite girar la vista sin girar el actor controlado. Cambiar a espada/escudo o restaurar hacha actualiza la presentación. Cerrar o cambiar de página desactiva o destruye la cámara propia. No pausa el mundo ni el tiempo de red.

El fallo de fondo opaco se debía a RiftVideoSettings.OnCamera: forzaba postprocesado/AA sobre toda cámara Game, incluso la inspección, después de su configuración local. El marcador RiftInspectionCamera excluye exclusivamente esa cámara de la política global. Su RenderTexture R16G16B16A16_SFloat conserva alpha; clear color transparente, sin postprocesado ni AA. No se modifican shaders, renderer assets, pipeline global ni la cámara de gameplay. El fondo del árbol es translúcido; ficha, títulos y leyendas tienen respaldo más sólido para contraste. El ángulo inicial de inspección muestra el hacha en lugar de dejar su filo de canto.

Validación final: RiftIdentityReview.Run -riftTalentPilotReview, 55 PASS y 0 FAIL (29 de identidad/entrada y 26 de composición/equipo/compra/ciclo). Unity compila; ruta real menú -> Warrior -> El Rift; compra desde ficha por CmdTalent, cambio real de equipo, alpha de esquinas 0/0/0, simulación continua, cierre y reapertura sin clones duplicados. Capturas 1280x720 inspeccionadas: juego real, ficha larga y pruebas explícitas de contraste claro/oscuro. Los fondos planos de contraste son fixtures identificados, no cambios de iluminación de El Rift. Los 64 checks anteriores corresponden al radial base y no se suman al resultado del piloto.

El radial compacto anterior permanece en la rama alternativa de composición, seleccionable deshabilitando talentCompositionPilotEnabled. No se añaden talentos, requisitos, balance, iconos Warrior ni cambios al controlador de Gori. El árbol sigue teniendo nueve talentos y seis dependencias; requiere pan/zoom para inspeccionar las ramas completas. No se probaron cliente remoto separado, móvil ni build standalone. El nombre visible sigue siendo el del catálogo actual: el cambio solicitado a Nertör quedó sin ejecutar por rechazo de revisión automática pendiente de confirmación específica. Inventario general y pantalla de carga siguen fuera de este bloque.
Actualización de nombre: tras confirmación explícita posterior del usuario, el catálogo visible se cambió a Nertör sin modificar IDs ni carteras. Verificado en la prueba de inventario de 69 PASS.
