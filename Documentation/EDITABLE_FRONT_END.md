# Menú, selección y creación editables

## Dónde editar

- Escena: `Assets/ROM/Scenes/MainMenu.unity`.
- En la jerarquía: `ROM_RiftHub/RiftFrontEnd` (instancia de prefab).
- Prefab: `Assets/ROM/UI/FrontEnd/Prefabs/RiftFrontEnd.prefab`.
- Componente de enlaces: `RiftFrontEndView`; `RiftHub.FrontEnd` referencia esa instancia.

Dentro de `SafeAreaDesign` existen tres vistas persistentes: `MainMenu`,
`CharacterSelection` y `CharacterCreation`. Sus RectTransforms, marcos, fuentes,
colores, imágenes, botones y textos estáticos se editan normalmente en Unity.
Los overrides de la instancia de escena también se conservan. Runtime no llama
al generador ni vuelve a crear estas vistas al navegar.

Se trabaja en coordenadas de diseño 1920 × 1080; sólo la raíz de diseño se escala
para ajustarse al área segura de pantalla. No editar su escala para recolocar un
botón: editar el RectTransform del elemento. El fondo usa EnvelopeParent para
cubrir todo el viewport sin deformarse.

Para ver selección o creación fuera de Play, usar los comandos `Vista previa`
del menú contextual de `RiftFrontEndView`. Los modelos se renderizan en una
textura transitoria dentro de los viewports serializados. No son personajes de
partida ni tienen identidad de red. `Limpiar modelos transitorios` retira esa
previsualización; no borra el diseño.

## Qué texto cambia al jugar

Los campos del Inspector agrupados como **Dynamic** son enlaces a contenido:
nombre/clase/nivel/tiempo, descripción de clase, retrato, estado de selección,
versión y acción de continuar. El controlador actualiza su texto/contenido, pero
no sus posiciones, dimensiones, fuentes ni colores. No usar esos campos para
una leyenda estática nueva: crear un Text separado en el prefab.

La lista de personajes instancia `Character row template (dynamic data)` para
cada registro; editar ese template modifica todas las filas futuras. Su
VerticalLayoutGroup/ContentSizeFitter controla la distribución de filas.
No convertir el template en un personaje guardado: sus textos de muestra sólo
sirven para editar la composición.

El lore/retrato de Warrior proviene de `RiftCharacterPresentationAssets`.
El nombre visible **Aldren** es `CharacterName` en
`Assets/ROM/Resources/PaladinPrototypeSettings.asset`; la descripción también se
edita allí. El nombre no es el ID de la clase ni el ID del personaje. Cambiarlo
no renombra, convierte ni borra los registros guardados.

## Botones y previews

Los botones de acción están referenciados explícitamente en `RiftFrontEndView`.
No se buscan por su texto ni por su posición. Se pueden mover o cambiar sus
leyendas sin perder la navegación. No quitar su referencia al duplicar o
reemplazar un botón. La lógica de navegación/datos sigue en
`RiftHub.AuthoredFrontEnd.cs`.

Los RawImages `HomePreview`, `SelectionPreview` y `CreationPreview` son el área
interactiva de los modelos. Mantener el viewport de creación debajo de las
pestañas de clase: los píxeles transparentes también reciben raycasts. El
renderizador usa su tamaño serializado; no recoloca ni redimensiona el viewport.

`RiftFrontEndAuthoring.Create` fue una migración explícita de una sola vez. Se
niega a sobrescribir un prefab existente. **No regenerar para editar el menú**;
editar el prefab o su instancia directamente. La composición vieja serializada
se conservó desactivada como respaldo en la escena.

## Alcance y pendientes

Migradas: página principal, selector, creación Warrior/Paladín y template de
filas. El flujo conserva la progresión de Crelium/El Rift, cancelación, volver,
cerrar y borrado recuperable existentes. Aldren continúa siendo un prototipo
con locomoción/ataque básico; este cambio no añade un kit de habilidades.

No migradas a prefabs persistentes en esta tanda: Ajustes, Más opciones,
diálogos de confirmación/conexión, comercio/social y pantallas de gameplay
(HUD, bolsa, habilidades/talentos, diario, ayuda). Usan aún el contenido runtime
heredado. No se afirma que todo el proyecto sea editable visualmente sin código.

El mapa M conserva configuración en `RiftPresentationSettings.asset`: vista
general de siete islas, fondo hasta el borde interior y zoom/pan de detalle.

## Validación de entrega

Consultar el resultado de la tanda en la entrega del commit: compilación no
sustituye la prueba por clics del usuario. La comprobación de persistencia debe
editar texto/posición estáticos en Edit Mode, guardar, comprobarlos en Play y
al volver, y restaurar sólo esa edición de prueba.
