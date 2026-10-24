# Guardar y sacar el arma del Warrior

El prefab `Assets/ROM/Characters/Warrior/WarriorPlayer.prefab` usa el parámetro
bool `WeaponDrawn` del Animator. Se inicia armado.

Desde otro script:

```csharp
using MultiFPS.Gameplay.RiftCombat;

WarriorAvatar warrior = character.GetComponent<WarriorAvatar>();
warrior.SetWeaponDrawn(false); // Guardar, por ejemplo al entrar a comercio.
warrior.SetWeaponDrawn(true);  // Sacar, por ejemplo al salir.
```

Llamar desde el servidor o desde el cliente dueño de ese personaje. En el cliente
dueño, la función envía un Command al servidor; el estado se replica mediante
SyncVar y también se aplica a clientes que entran más tarde. Funciona offline.
Un cliente no puede modificar el arma de un personaje ajeno con esta función.

- `WeaponDrawn`: estado solicitado (puede estar en transición).
- `CanUseWeapon`: false mientras está guardada o termina de sacarla.

No cambiar solamente el bool del Animator desde código de gameplay: usar la
función pública mantiene coherentes la presentación, la red y el combate.
Esta API no implementa una zona comercial ni una restricción persistente de
zona; el controlador de la zona debe llamar a la función según sus propias reglas.

## Animator

- `DrawWeapon`: `2Hand-Sword-Unsheath-Back-Unarmed` del pack.
- `SheathWeapon`: copia del mismo clip con sus curvas invertidas en el tiempo.
  Se genera como `.anim` para conservar las salidas e interrupciones del Animator.
- `UnarmedLocomotion`: Idle y cuatro direcciones Run Unarmed.
- `UnarmedJump` / `UnarmedAir`: Jump y Fall Unarmed. Vuelven a locomoción al tocar suelo.
- La capa `Weapon Presentation` libera el IK de los brazos al guardar y recupera
  el agarre al finalizar la extracción. El hacha permanece visible.
- La misma hacha pasa de la mano derecha a `Weapon Back Socket`, sujeto al pecho.
  Las curvas `rightHandFollow` y `backWeight` coordinan el traslado en el animation
  stream. El socket sigue la espalda al caminar y saltar, sin reparentar en runtime.
  Su posición y rotación se pueden ajustar en el prefab.
- El hacha se guarda en diagonal con la hoja hacia abajo y el mango sobre el
  hombro. La sujeción de extracción se calibra al fotograma de alcance del clip
  (`40%`): la mano derecha toma el mango y después se recupera el agarre a dos manos.
- El equipo alternativo de espada/escudo se oculta al guardar; la sujeción en la
  espalda corresponde al hacha.
- Guardar interrumpe la presentación del ataque y el servidor cancela el cast.
  El servidor rechaza nuevas habilidades mientras no pueda utilizar el arma.

Los originales FBX no se modifican. El clip invertido y los cuatro clips de presentación
están en `Assets/ROM/Animation/WarriorWeaponStates`.

## Validación

`ROM > Animation Rigging > Validate Warrior Weapon States` comprueba las
transiciones, las cinco direcciones, el salto desarmado, la inversión de una
transición, la salida de defensa, la visibilidad, los pesos del rig, la sujeción a
la espalda, las curvas invertidas y la API offline.
No sustituye una prueba de dos clientes conectados.

El Inspector de WeaponIKController incluye las animaciones Unarmed y de
guardar/sacar para previsualizar. Al detener la preview se restauran poses,
visibilidad y pesos originales. `Validate Warrior Animation Grips` conserva las
comprobaciones de las nueve animaciones de ataque.

## Triggers de prueba junto al spawn

En `Assets/ROM/Levels/map_rom_horde.unity`, buscar el grupo
`Warrior Weapon Triggers - Spawn`:

- `Guardar arma`: portal naranja, llama a `SetWeaponDrawn(false)` al entrar.
- `Sacar arma`: portal verde, llama a `SetWeaponDrawn(true)` al entrar.

Ambos usan `WarriorWeaponTrigger`. En el Inspector, `Action = Holster` guarda y
`Action = Draw` saca. Se pueden duplicar o mover para usarlos en otros lugares.
El BoxCollider es trigger y el Rigidbody es cinemático, sin gravedad. Los marcos
son visuales, sin colisión; los portales usan la capa `trigger`, habilitada para
detectar la capa `character` en la matriz de física del proyecto. `Awake` asegura
esa capa también en portales ya existentes.

La acción ocurre al entrar desde cualquier lado. Salir no hace lo contrario.
En red, el servidor aplica el cambio; el cliente dueño también puede detectar
su entrada y solicitarla mediante el Command de WarriorAvatar. Las réplicas
ajenas no envían solicitudes. Offline también funciona. Se resuelve el
Warrior desde el collider principal o desde una hitbox hija y se ignoran
objetos ajenos y entradas duplicadas que no cambiarían el estado.

`ROM > Animation Rigging > Place Weapon Triggers Near Spawn` reconstruye esos
dos portales y valida su configuración y la integración con el prefab.
