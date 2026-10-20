# Integrar el sistema de dificultad y el de muerte-fallo a una escena

Qué hace falta para que los sistemas de T004 (Threat Levels) y T005 (caído,
revivir, fallo de incursión) funcionen en una escena del proyecto.

El código está en `main` y es inerte hasta que se cablea: no cambia el
comportamiento de ninguna escena por sí solo.

---

## 0. Antes del cableado: una decisión de arquitectura

El GDD define que V1 tiene **tres tipos de incursión: Raid, Recovery e
Investigation**. Es decir que **Raid *es* una incursión**, no un modo paralelo.

Hoy hay dos gamemodes que se superponen:

| | Qué aporta | ¿Estado de caído? |
|---|---|---|
| `Raid` (T007) | oleadas, enemigos, objetivo destructible | **no** — se reaparece |
| `IncursionGameMode` (T005) | sin reaparición, caído, revivir, fallo de escuadra | sí |

**Consecuencia concreta:** en el mapa "El Rift" tal como está configurado hoy,
el sistema de caído y revivir **no tiene efecto**. `Raid` deja activa la
reaparición del kit, y los sistemas de T005 solo se aplican bajo
`IncursionGameMode`.

Tres salidas posibles. La elección es de diseño, no técnica:

1. **`Raid` hereda de `IncursionGameMode`.** Es lo que describe el GDD: Raid
   aporta los objetivos, Incursion aporta las reglas de vida y muerte. La más
   limpia, y la que evita mantener dos modelos de muerte. Toca código de T007.
2. **Incursion como segundo gamemode del mapa.** Se elige uno u otro desde el
   menú. Rápido de hacer, pero deja dos conjuntos de reglas conviviendo.
3. **Una escena de pruebas aparte.** Deja T005 verificable sin tocar el mapa de
   nadie, y la integración real se decide más adelante.

Lo que sigue aplica igual a cualquiera de las tres.

---

## 1. Lo que ya está hecho y viaja solo

Nada que configurar:

| Qué | Dónde |
|---|---|
| `DownedState` y `ReviveInteraction` | ya están en el prefab `MultiFPS_Character` |
| `IncursionHudProvisional` | se instala solo al cargar la escena |
| `InputDiagnostics` | se instala solo |
| `threat_catalog` y `threat_I`, `threat_II`, `threat_III` | `Assets/ROM/Difficulty/` |

---

## 2. Lo que hay que agregar a la escena

Los tres componentes van sobre el **`ROOMMANAGER`**, como cualquier gamemode del
kit.

### `Threat Level Controller`

| Campo | Valor |
|---|---|
| **Catalog** | `threat_catalog` — **obligatorio** |
| **Starting Level** | `II` para probar, `I` para jugar |
| **Log Changes** | activado mientras se verifica |

Sin catálogo asignado, el estado de caído no se activa. Lo avisa por consola.

### `Revive Budget`

No tiene campos. Los números salen del Threat Level activo.

### `Incursion`

Solo si se usa el gamemode de T005 (opciones 2 o 3 de la sección 0).

| Campo | Valor |
|---|---|
| **Default Spawn Points** | el `SpawnpointsContainer` de la escena — **obligatorio** |
| **Squad Check Interval** | `0.5` |
| **Log Changes** | activado mientras se verifica |

Si queda vacío, **nadie puede entrar a la partida**. Lo avisa por consola.

---

## 3. Requisitos de la escena

| Requisito | Por qué |
|---|---|
| La escena en **Build Settings** | Mirror no puede cargar por red una escena que no esté listada |
| `Incursion` entre los `AvailableGamemodes` del `MapRepresenter` | si no, no aparece como opción en el menú |
| Un `SpawnpointsContainer` con al menos un punto | es de donde entra la escuadra |

---

## 4. Qué configura la dificultad

Todos estos valores se editan por nivel de amenaza, en los assets de
`Assets/ROM/Difficulty/`. No hay nada hardcodeado:

| Parámetro | Threat I | Threat II | Threat III |
|---|---|---|---|
| Sangrado | 90 s | 60 s | 45 s |
| Auto-revive | sí | no | no |
| Mantener para revivir | 3 s | 4 s | 5 s |
| Vida al revivir | 50 % | 40 % | 30 % |
| Revividas de escuadra | sin límite | 6 | 3 |

Por regla del GDD, **ningún nivel escala daño ni vida**: la dificultad sube por
presión y por margen de error.

---

## 5. Verificación

Con Threat II y dos jugadores:

| Paso | Qué tiene que pasar |
|---|---|
| Entrar a la partida | `[ROM] <nombre> entró a la incursión` |
| Quedarse sin vida | **CAÍDO** en pantalla, barra roja bajando desde 60 s |
| Un compañero se acerca | aparece `Mantené E para levantar a <nombre>` |
| Mantener E 4 s | se levanta con 40 % de vida, y el contador de revividas baja |
| Agotar el presupuesto | `la escuadra se quedó sin revividas` |
| Caer todos | `Incursión fallida` |

**Señal de alarma:** si aparece `de pie por una resurrección externa`, el
gamemode no cortó la reaparición del kit y las reglas no se están aplicando.

---

## 6. Pendientes conocidos

Declarados para que no se confundan con defectos:

- **Revividas por jugador** está en la estructura de datos pero no implementado.
  Falta decidir si consume del presupuesto compartido. Si se configura ese modo,
  loguea error y cae al pool de escuadra.
- **Auto-revive** (que Threat I habilita) no está implementado, por el mismo
  motivo.
- **El HUD es provisional.** Dibuja con la GUI legacy y no está integrado al
  canvas del kit. No sirve para móvil.
- **No hay pantalla de resultados ni extracción** tras el fallo. El kit base
  tampoco las tiene y el flujo del GDD no está desarrollado.
- **La distancia de revive se mide contra la cadera del ragdoll**, no contra el
  transform del personaje, porque una explosión separa los dos varios metros. El
  alcance está en 4 m para cubrir el caso normal.
