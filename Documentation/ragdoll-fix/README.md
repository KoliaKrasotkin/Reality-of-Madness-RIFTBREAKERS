# Corrección de ragdoll de personajes IDS

El Editor.log de la sesión del usuario contenía 180 errores asociados a tres muertes: 59 huesos utilizados no encontrados más un error de aplicación de skin por muerte. El origen era HumanoidDeath.SpawnRagdoll → RagDoll.ApplySkin → SkinBoneMapper: se intentaba vincular la malla de Lizardman al rig_soldier del ragdoll del template. Ese rig no contiene pelvis, mandíbula, dedos ni cola con los nombres del avatar nativo.

La ruta de muerte de personajes con IDSGameplayPose ahora conserva una copia completa de su visual y esqueleto humanoide nativo. La malla mantiene sus bindposes, materiales y referencias a huesos propios. Un controlador de presentación conecta las articulaciones humanoides a la física del ragdoll existente mediante su función anatómica y compensaciones de orientación; los huesos adicionales de cola/mandíbula permanecen en su jerarquía nativa.

La física comienza en la pose real del personaje. Se deshabilita la malla del soldado y se eliminan los scripts de gameplay de la copia para que no sigan al personaje vivo. Los colliders visuales no interfieren con el proxy físico. La muerte por cabeza conserva el comportamiento de ocultar la cabeza nativa.

Se mantienen los siete cuerpos y cuatro flexores del protocolo Mirror actual; no se agregan objetos de red ni se modifican el controlador, rig o animaciones del Warrior nativo. Las skins antiguas sin visual IDS conservan su ruta original. También se protegieron las referencias al atacante en impulso y sangre cuando ese atacante ya no existe.

Archivos:
- `Assets/MultiFPS/Scripts/Gameplay/Character/RiftNativeRagdoll.cs`
- `RagDoll.cs`
- `HumanoidDeath.cs`
- `Assets/ROM/Editor/RiftJournalProbe.cs`, modo `RunRagdoll`.

Validación: 31 comprobaciones aprobadas en un host local con muertes autoritativas reales de dos skins IDS. Se verificaron cola, todas las piezas renderizadas, huesos internos al clon, geometría horneada sin explosiones, ausencia de scripts vivos, cabeza y formato de sincronización. El log `Logs/Ragdoll-Fix-Test.log` no contiene errores de compilación, excepciones ni los mensajes de mapeo de la captura. No se validó un cliente remoto. Los huesos adicionales siguen la jerarquía del cadáver y no reciben simulación física individual.
