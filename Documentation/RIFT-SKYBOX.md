# Cielo dimensional de El Rift

Generado con la herramienta integrada image_gen, mediante la habilidad imagegen.

- Textura: Assets/ROM/Rendering/Skyboxes/RiftVoid_Panorama.png (1774 × 887, panorama 2:1).
- Material: Assets/ROM/Rendering/Skyboxes/RiftVoid_Skybox.mat, shader Skybox/Panoramic, proyección equirectangular 360°.
- Escena: Assets/ROM/Levels/map_rom_horde.unity.
- Ajustes editables: exposición 1.15 y rotación 25° en el material. Textura sin compresión, con mipmaps, repetición horizontal y clamp vertical.
- Se armonizaron el ambiente, la luz direccional y la niebla violeta conservando iluminación suficiente para recorrer las islas.
- Menú de editor: ROM > El Rift > Aplicar cielo de grieta dimensional. El comando de iluminación/postprocesado también conserva este cielo.
- Es una primera versión estática. No contiene relámpagos animados ni nubes volumétricas. La resolución es la devuelta por el generador; no es una textura HDR.

Verificación: compilación de Unity, capturas desde refugio, Sun Temple y cenit; reapertura de la escena guardada para comprobar referencias de material y textura. Capturas en Documentation/map-visual-review/rift-sky-*.png.

## Prompt utilizado

Use case: stylized-concept. Asset type: production Unity skybox texture, seamless 360-degree equirectangular spherical panorama, 2:1 aspect ratio, highest available resolution. Create the sky viewed from INSIDE a vast dimensional rift for dark fantasy science fiction floating islands. Entire image is sky/outer-dimensional void, with NO ground, NO buildings, NO islands, NO characters, NO text, NO UI. Deep midnight indigo and charcoal cosmic void with enormous swirling smoky violet nebula clouds, subtle crimson-magenta energy filaments and a few sharp electric cyan cracks in fractured space. One sweeping immense luminous broken arc spans the upper sky like the edge of a torn dimensional veil, not a circular portal sitting on a horizon. Rich volumetric cloud detail, cinematic game environment, restrained luminous highlights and broad quiet dark areas so gameplay stays readable. Horizon is diffuse violet atmospheric haze, no earthly sun, no blue daytime sky. Correct equirectangular projection: full 360 horizontal and 180 vertical, continuous left/right edges, stretched convergent zenith/nadir poles, no visible seam, no panorama borders. Balanced exposure rather than pitch black or blown-out neon. This is a texture sheet for wrapping on the inside of a sphere, not a framed perspective landscape illustration.
