# Warrior e identidad visual

Solo Warrior es jugable. Los perfiles legacy se conservan como no disponibles; no se convierten ni se borran. Crear o seleccionar un Warrior permite entrar desde Jugar o Unirse. El servidor exige Warrior, skin admitida, prefab nativo e ID GUID válido; no cae al prefab FPS. Los comandos de cambio legacy y desactivar combate nativo se rechazan.

El nombre visible proviene de Resources/RiftSkinNames.json (skin 1: Lizardman Berserker). No hay campo de nombre ni renombrado. CharacterId sigue siendo independiente. El nombre social/de cuenta se conserva. Futuras skins requieren datos y compatibilidad visual/servidor; no hay tienda ni efectos nuevos.

Oro y pociones locales usan ROM.CharacterWallet.v1.<CharacterId>. Los perfiles antiguos importan una sola vez su cartera por nombre, con marca de propietario de origen. Las claves antiguas se conservan. Nunca se sobrescribe una cartera de destino existente. Personajes nuevos no importan saldos por nombre. Dos personajes de la misma skin tienen carteras separadas. Este almacenamiento local no es inventario online autoritativo.

Validación: ROM.EditorTools.RiftIdentityReview.Run. Cuenta/perfiles aislados, creación mediante botón real, Deploy bloqueado para legacy, dos Warriors de mismo nombre con ID/cartera distintos, migración sin duplicación, menú→confirmación→host El Rift, hacha visible, cero slots FPS y GUID del controlador Gori 6738116e27198d24fb997657d9a43cdb. Rechazo de datos de entrada manipulados y habilidades legacy. Restaura preferencias al salir; capturas y reporte en Logs/IdentityReview. No guardar escenas. No se probó cliente remoto separado ni build móvil.
