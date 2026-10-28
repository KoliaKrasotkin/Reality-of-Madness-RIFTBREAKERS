# Aldren — MVP de Fe y Rezar

Balance provisional. ID estable `paladin-prototype`, skin2; sin migración de
personajes, diarios o equipo. Sin Aura, Lluvia, teletransporte ni talentos nuevos.

## Edición desde Unity

Máximo de Fe100 y referencias ordenadas: `Assets/ROM/Characters/PaladinPrototype/PaladinPrototypeClass.asset`.
Nombre, descripción, Fe inicial100 y regeneración5/s:
`Assets/ROM/Resources/PaladinPrototypeSettings.asset`.
El runtime no reconstruye ni sobrescribe esos assets.

| Asset en Characters/PaladinPrototype | Efecto | Fe | CD | Alcance | Valor provisional |
|---|---|---:|---:|---:|---|
| SacredStrike.asset | Golpe Sagrado | cuesta10 | 3s | 2,8m | 100% del arma, un enemigo frontal |
| HealingLight.asset | Luz Sanadora | cuesta30 | 6s | 15m | cura25% de vida máxima del receptor |
| RadiantShield.asset | Defensa Radiante | cuesta40 | 15s | 15m | absorbe25% de vida máxima, 6s |
| Prayer.asset | Rezar | gratis | 30s | propio | llena Fe instantáneamente |

AbilityDefinitions: coste, cooldown, alcance/arco, ejecución/impacto, recuperación,
coeficiente de arma, HealthFraction y BuffDuration. En Rezar mantener Cost0:
el MVP define recarga gratuita; editar su cooldown. IDs/PaladinEffect identifican
comportamiento, no balance. Iconos provisionales reutilizan arte existente.

## Recurso y ciclo de vida

Primera aparición en la sesión: Fe100. Regenera5/s mientras está vivo, incluso
en combate, limitada a0–100. Golpe y autoataque no generan Fe. Sin regeneración
fuera de línea. Abrir UI o reconfigurar el mismo actor no rellena el recurso.
Muerte deja Fe0 y cancela efectos pendientes; reaparecer conserva0 y retoma
regeneración. No hay recarga gratuita por morir. Rezar tiene CD30 independiente
y nunca ejecuta Salto de guerra aunque ocupe la ranura4. Con Fe completa se
rechaza sin iniciar cooldown.

Memoria del servidor por CharacterId existente conserva Fe, cooldowns de todas
las habilidades y recuperación global si se recrea el controlador o reconecta
al mismo servidor. Coste/CD de un lanzamiento aceptado se conservan aunque se
interrumpa por desconexión. No hay guardado portátil/backend nuevo. Reiniciar el
servidor/proceso inicia otra sesión; no se promete persistencia entre servidores.
Se usa la identidad existente, no autenticación nueva. Snapshot no equivale a una
prueba de dos clientes remotos.

## Objetivos y efectos

Cura y escudo: aliado vivo seleccionado (incluye NPC con Health aliado); sin
selección, vos. Con enemigo seleccionado se rechazan sin autocuración silenciosa.
Para seleccionarte bajo ataque, retrato o Seleccionarme (F6 por defecto): el
autoobjetivo defensivo puede elegir al atacante si la selección queda vacía.

Servidor valida registro de red, escena, vida, relación, alcance y LOS. Cura con
vida completa o escudo activo se rechazan sin coste/CD. El escudo no acumula ni
se renueva mientras quede absorción. Coste y CD se pagan al aceptar; un objetivo
que se vuelve inválido durante la ejecución no recibe efecto ni devolución.
Rezar no depende del objetivo seleccionado.

Absorción en Health después de mitigación y antes de descontar vida; sobrante
sí daña. No reutiliza DefensiveUntil ni cambia Última Resistencia de Warrior.
Muerte/reaparición del receptor elimina la protección anterior.

## UI y validación

Cuatro skills desde nivel1 para este MVP, primeras cuatro ranuras en una barra
nueva. Asignación/bindings/libro/tooltips/clics existentes. No borrar distribuciones
guardadas del usuario. HUD y objetivo muestran Fe y costes actuales.

Tests con perfil/diario aislados, posiciones de fixture, control de tiempo y
aplicación inmediata separan AI/escalado de vida. No son campaña manual ni
certificación remota/WebGL. Clics reales anteriores confirmaron las tres primeras
habilidades; la revisión Fe/Rezar necesita QA visual cuando la PC esté libre.
La entrega distingue compilación, Host, snapshot y UI no verificada.
