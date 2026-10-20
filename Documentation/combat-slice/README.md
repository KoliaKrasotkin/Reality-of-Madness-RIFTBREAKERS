# Nertör — primera versión jugable

## Cómo probar

1. Abrir `Assets/ROM/Scenes/MainMenu.unity` y entrar en Play.
2. Abrir **CLASE** y elegir **NERTÖR · GUARDIAN** o **NERTÖR · RAVAGER**. Esto selecciona Lizardman Berserker y guarda la apariencia mediante el servicio social existente. Las otras cuatro clases siguen disponibles.
3. Pulsar JUGAR. Se conserva El Rift como único mapa y la cámara en tercera persona.
4. Acercarse al muñeco de entrenamiento del refugio. Clic izquierdo: ataque básico (12 Furia por impacto). F: Furia cortante (18 Furia por impacto, CD 4 s). Z: Rompetierra (25 Furia, CD 9 s, área y stagger). T: Rugido de honor (15 Furia, CD 15 s).
5. Guardian: 220 vida, 20 % armadura, amenaza multiplicada y rugido con provocación/40 % reducción temporal. Ravager: 170 vida, 8 % armadura, daño x1.3 y rugido con +30 % daño. Los ataques básicos y el cleave no cuestan Furia; permiten iniciar un combate desde cero.
6. Salir del refugio para encontrar grupos de melee y casters. La composición se controla con `Horde.MeleeEncounterFraction` (0.75 inicialmente), con reparto determinista por encuentro. Los guardianes existentes usan el arquetipo elite y conservan sus recompensas y escalado.
7. El portal del refugio está aproximadamente en `(35, 0, 24.5)`. Acercarse y pulsar E. Hay un portal de regreso junto al checkpoint de las ruinas occidentales. El servidor valida proximidad, vida y cooldown. No transporta automáticamente al rozar partículas.
8. Mirar hacia el norte/noreste para ver el Corazón Fracturado, suspendido en `(130, 330, 600)`. Su luz direccional coincide con su posición y conserva la noche/bruma. `FracturedHeart.Transition(0..1)` cambia lentamente color/intensidad; no se crean cientos de luces.

## Arquitectura y archivos

- `Assets/MultiFPS/Scripts/Gameplay/RiftCombat/`: AbilityDefinition, SpeciesDefinition, CombatClassDefinition, SpecializationDefinition, WeaponDefinition, EnemyArchetypeDefinition y PortalDefinition; definiciones independientes de las skins. SpeciesDefinition restringe los índices de skin permitidos; por ahora Nertör admite exclusivamente el Lizardman (1).
- `AbilityController`: NetworkBehaviour del PlayerInstance. Valida slot, clase/especialización, vida, zona segura, Furia, cooldown y ventana de ejecución. Sin daño enviado por el cliente. SyncVars/SyncList replican recurso, buffs, cast y cooldowns. El búfer de input final es de 180 ms. `Request(slot)` es compartido por teclado y botones UI.
- `CombatController`: adaptador sobre Health. Conserva la ruta de muerte, entrenamiento, protección, recompensas y RPC de vida. La mitigación y los eventos de amenaza se resuelven en servidor.
- `CombatMotion` / `CombatEquipment`: overlay de clips Samurai humanoides sobre el avatar nativo; armas con socket/escala/rotación propios. Se conservan la locomoción importada y las animaciones antiguas para clases con armas convencionales. Los golpes usan tiempos de impacto configurados en el SO, coherentes con la duración escalada de los clips; no se confía en AnimationEvents del cliente.
- `RiftEnemyCombat`: encuentros de exploración, navegación, separación sencilla, dos atacantes simultáneos por objetivo, anticipación, interrupción por stagger, amenaza/taunt y proyectiles con impacto validado. No reemplaza el muñeco ni los NPCs del refugio.
- `CombatVfxPool`: máximo 24 efectos transitorios activos, hasta seis instancias retenidas por prefab; partículas limitadas y luces retiradas de wrappers. Los VFX no calculan daño.
- `NertorHUD`: vida, Furia, habilidades, cooldowns, objetivo, aviso de cast enemigo y buff. Se compone con RiftWidgets y se oculta al pausar. Los botones llaman a la misma API de habilidades.
- `Assets/ROM/Combat/`: SO editables, wrappers, materiales propios, espadas normalizadas y malla del cristal. Los paquetes originales no se renombran ni sobrescriben.
- `Assets/ROM/Editor/NertorSliceBuilder.cs`: montaje reproducible; menú ROM > Combate > Construir prototipo Nertör. Los assets y escena ya están generados: no es necesario ejecutarlo para jugar.
- `Assets/ROM/Editor/NertorCombatProbe.cs`: prueba de integración en una partida host real. Informes y capturas en esta carpeta.
- Integración en CharacterMotor/CharacterInstance existentes a través del input y presentación, Health, PlayerInstance.Classes, RiftClassCombat, IDSGameplayPose, Horde.Exploration, RiftHub y RiftClassHUD. No se cambia la estructura de red ni se eliminan escenas/assets.

## Assets auditados

Inventario completo: `audit.md`. Informe de importación/configuración real: `asset-validation.txt`.

- RPG VFX Bundle: Punch Hit, Chain explosion y Buff palladin, mediante wrappers propios. HSFiles está instalado. Se conservan sus Shader Graphs URP; convertirlos a Lit genérico pierde transparencia y genera cuadros, por lo que no se hace.
- SpaceCrackTeleport: Effect2 del directorio BuiltIn. Sus materiales VFX_EffectV2 se adaptan a partículas URP en copias. Es una aproximación del efecto: no reproduce todas las funciones del shader original (distorsión/disolución).
- Free medieval weapons: Sword_OH y Sword_DH. También existen Axe_OS, Axe_DS, Arrow y Wooden Bow. No se encontraron escudos ni martillos; Guardian usa espada sin escudo y Ravager espada de dos manos.
- Samurai Animation Pack: clips Attack_Strike, Attack_Round, Attack_Strike_Perfect y Block_Idle; el builder verifica que sean humanoides.

## Alcance y límites de esta iteración

Es la primera vertical slice, no la implementación completa de todas las fases. Están activas tres habilidades y el ataque básico. Warborn Leap, Whirling Execution y Last Stand tienen definiciones reservadas y `Implemented=false`; el servidor rechaza ejecutarlas. No se anuncian como habilidades terminadas.

Pendientes: combos de varios golpes, dodge y lock-on, canales/ground targeting avanzado, resistencias elementales/DoT/escudos, inmunidad CC, amenaza por curación, audio/iconos específicos, ajuste fino de agarre de dos manos/IK, telegraphs y comportamiento elite diferenciados, y las otras especies/clases nuevas. Los cinco arquetipos son configurables; el comportamiento inicial comparte las variantes melee/caster.

La API de input y botones táctiles está preparada, pero todavía no hay joystick táctil/cámara gestual ni adaptación completa a gamepad. Los límites de partículas son preventivos: no equivalen a una medición de FPS ni una certificación en hardware móvil.

El proyecto real usa **Mirror**, no Photon Fusion 2. Se preserva Mirror y el movimiento de propietario existente; el nuevo combate tiene validación de servidor. La prueba ejecuta host con la ruta Command/RPC activa; falta una prueba con cliente remoto en otro proceso/equipo y pruebas de latencia. No se afirma que esa prueba remota haya pasado.

La transición del Corazón es presentación local configurable; sincronizar eventos de historia globales y persistencia del mundo será una iteración posterior. Los portales usan IDs definidos en la escena y destinos de navegación existentes; no son una red de transporte desbloqueable/persistente aún.

Persisten advertencias anteriores de BoxCollider con escala negativa en Sun Temple. No son errores de compilación del combate. No se mide rendimiento del mapa completo en esta prueba.
