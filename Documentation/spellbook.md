# Libro de habilidades Warrior

K o el binding guardado de Habilidades abre el libro lateral. Las tarjetas usan los iconos existentes. Todas / Ataque / Apoyo filtran por daño positivo o técnica sin daño, sin modificar reglas. La rueda desplaza el catálogo para ver las siete técnicas. Disponibilidad usa Implemented y AllowedSpecializations; no se agregan requisitos de nivel ficticios.

## Asignación y uso

- Clic / Enter selecciona una técnica. Clic sobre slot o tecla del slot la asigna. El keybind pertenece a la posición, no a la habilidad.
- Arrastrar del libro asigna; arrastrar entre slots intercambia. Una técnica ya ubicada en otra posición intercambia posiciones para evitar duplicados.
- Clic derecho vacía el slot. Quitar slot vacía la posición seleccionada o acepta arrastrar un slot sobre él. La técnica sigue aprendida y visible en el libro.
- Soltar sobre destino inválido cancela. Escape cancela primero el arrastre; el siguiente Escape cierra el libro. El foco de texto bloquea asignación por tecla.
- La barra editable del libro representa exactamente el layout del HUD, que reaparece al cerrar. El autoataque sigue independiente.

## Persistencia y servidor

ROM.Hotbar.v2.<CharacterId>.warrior guarda AbilityDefinition.Id y cadenas vacías para slots vacíos. V1 se lee sin borrar ni sobrescribir su clave. La primera edición guarda v2. Otro personaje mantiene un layout independiente. No se pierden habilidades, XP ni items al quitar un slot.

Request envía un ID estable a CmdUseAbility. El servidor resuelve contra la clase y valida disponibilidad, recargas propia/global, recursos, vida y reglas de zona. Remapear no altera cooldowns ni estadísticas. El comando numérico anterior conserva validación de servidor por compatibilidad. El layout es local: no hay sincronización entre dispositivos.

## Validación

Compilación previa de MultiFPS, Assembly-CSharp y Editor con salida temporal: PASS. Compilación Unity/Mirror y ruta real de menú a El Rift: PASS. ROM.EditorTools.RiftIdentityReview.Run -riftSpellbookReview ejecuta 68 checks totales (incluye los 29 de identidad). Perfiles y diarios aislados, bindings restaurados y sin guardar escenas.

Incluye drag/drop real, asignación por clic y tecla, remover sin olvidar, slots vacíos persistentes, migración v1, aislamiento por personaje, destino inválido, cancelación con Escape, tecla de slot vacío sin autoataque, ejecución del ID remapeado mediante tecla y clic real del HUD, rechazo de cooldown/recursos/ID inválido. Un clon runtime no implementado verifica bloqueo sin modificar el asset original. Capturas reales 1280x720 revisadas: libro, layout remapeado y HUD con habilidad ejecutada. Reporte en Logs/IdentityReview/rift-spellbook-validation.txt.

No cliente remoto separado, build standalone ni móvil. Las categorías usan lógica simple; no se agregan reglas de desbloqueo nuevas. Inventario general de botín, diario con misión activa y rediseño de talentos siguen pendientes aparte. No cambios de balance ni de controlador Gori ni iconos nuevos.
