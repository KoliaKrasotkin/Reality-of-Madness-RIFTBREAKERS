# Bestiario de El Rift, mapa y pociones

## Contenido

Los originales de Sculptiq Studios y Forest Animals se conservan separados del código y los prefabs del juego. Los 12 prefabs nuevos, sus definiciones, materiales URP y controladores están en `Assets/ROM/Enemies/Bestiary`. Se registran en el NetworkManager del juego y de las escenas. Cada enemigo utiliza su propio esqueleto, avatar, clips y armas originales; no usa CharacterInstance, BotAI ni CharacterAnimator del template.

| Enemigo | Ataque especial |
|---|---|
| Oráculo de la Brasa Rota | Proyectil de brasa con viaje y aviso previo |
| Quebrantacadenas de Ceniza | Cleave frontal con hacha |
| Juramentado del Eclipse | Golpe cercano que drena vida |
| Regente de la Corona Hueca | Martillo con área anunciada en la posición objetivo; miniboss |
| Heraldo del Corazón Fracturado | Nova circular de vacío |

Se mezclan con los diablillos en los encuentros existentes; el Regente ocupa los encuentros de miniboss. No se agregan islas. Las definiciones permiten ajustar vida, daño, rango, tiempo de aviso, recarga, color, XP y oro. Los avisos/proyectiles son visuales locales agrupados en un pool de 32, sin partículas en red ni cálculo de daño en VFX. El daño y la validación ocurren en el servidor Mirror que realmente usa este proyecto.

Fauna: Oso del Sotobosque, Jabalí de Ceniza y Alce de Raíces Viejas se defienden cuando son atacados. Ciervo del Eco, Zorro Umbrío y Liebre del Velo huyen. Lobo de las Fracturas es hostil. La fauna recorre zonas existentes con navegación; comparte el presupuesto global de 24 NPC vivos. Da XP configurable, sin avanzar las misiones de matar enemigos de la legión. Las zonas seguras impiden aparición y daño hostil.

## UI

`M`: mapa completo del atlas de El Rift, posición y orientación del jugador, cristales/refugios y encuentros. Rueda para zoom (1–4), arrastrar para desplazar; click sobre marcador muestra nombre/coordenadas. M o Escape cierra. Usa la textura horneada existente, sin cámara adicional de minimapa.

Dos frascos compactos a los lados de la hotbar: poción normal (40%, H) y mayor (75%, Shift+H), cantidades, tooltips y recarga compartida de 5 s. Los botones y las teclas usan la misma validación de servidor. No se consume a vida completa ni durante recarga. Se compran en la boticaria con oro local. Las dos teclas y el mapa aparecen en Ajustes > Controles y pueden reasignarse.

El inventario de provisiones conserva la limitación existente: es local, utilizable por el host. No se habilitó una cartera confiable para clientes remotos ni un backend persistente multijugador. La IA, vida, movimiento y ataques sí conservan el modelo de autoridad Mirror. Falta una prueba en dos computadoras para certificar presentación y sincronización remotas.

El prefab editable `Assets/ROM/UI/GameplayHUD/Resources/RiftFieldUI.prefab` también está colocado en `Assets/ROM/Levels/map_rom_horde.unity`. No es una imagen superpuesta.

## Abrir, generar y validar

Abrir `Assets/ROM/Scenes/MainMenu.unity`, Play, entrar a El Rift y explorar fuera del refugio. Las zonas se activan por proximidad y reponen su población tras la recarga del encuentro.

- `ROM > Enemies > Build Rift bestiary and field UI`: reconstruye los assets propios y conexiones.
- `ROM.EditorTools.RiftJournalProbe.RunBestiary`: prueba Play/host real, equipamiento, materiales, mapa, consumo y habilidades.
- `ROM.EditorTools.RiftBestiaryReview.Run`: revisa navegación/zonas seguras y genera previews de los modelos.

Resultados en `Documentation/quest-system/bestiary-validation.txt`, `Documentation/bestiary/spawn-validation.txt` y `Logs/Bestiary-Test.log`. Las pruebas usan un personaje desechable y restauran nombre, personaje y controles originales.

## Validación realizada

Unity 6000.3.23f1: compilación correcta y 97 comprobaciones en Play con host Mirror. Incluye los cinco ataques especiales con daño de servidor, los siete animales en navegación/red, comportamiento hostil/defensivo/huida, mapa M y cambio a F9, pociones y cambio a F7. Revisión adicional de las 25 zonas: todos sus puntos permiten navegación y están fuera de refugios. Previews propios de los 12 modelos y capturas de mapa/pociones revisados. Log de la pasada final: `Logs/Bestiary-Final.log`.

Esto no sustituye una prueba de cliente remoto ni un perfil de rendimiento en dispositivo móvil. El balance y los VFX son iniciales.
