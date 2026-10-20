# Auditoría previa — combate Nertör

Unity 6000.3.23f1; URP 17.3.0; Input System 1.20.0; Mirror. No hay Photon Fusion en Packages/manifest.json ni entre los assemblies importados. Se conserva Mirror.

Reutilizar: CharacterMotor (movimiento relativo a cámara, aceleración y saltos), CharacterInstance/IDSGameplayPose (skins humanoides y locomoción), Health (vida/RPC/muerte), Horde.Exploration (encuentros, recompensas, refugios/checkpoints), RiftHub/RiftWidgets (menú y UI), RiftNightAtmosphere (noche y bruma).

Extender: definiciones SO independientes de cosméticos; AbilityController sobre el PlayerInstance de red; adaptador de daño sobre Health; ventanas temporizadas de clips humanoides; amenazas por daño/taunt; IA de encuentros sin sustituir entrenamiento; HUD e input compartidos por botones y teclado. Desactivar disparo y habilidades anteriores únicamente durante el modo Nertör.

Faltantes detectados en armas: no hay escudo ni martillo. Guardian usará espada de una mano sin escudo; Ravager espada de dos manos. SpaceCrack ofrece prefabs BuiltIn, se crearán copias de materiales compatibles, sin editar originales. Samurai ya tiene importación humanoide preparada por integración anterior.

El recurso de Furia, cooldowns y daño se resuelven en el servidor. Se conserva el modelo de movimiento propietario actual; no se presenta como movimiento con autoridad de servidor.

## Assets/Hovl Studio

{'.unity': 8, '.lighting': 3, '.pdf': 1, '.asset': 5, '.png': 233, '.exr': 12, '.prefab': 260, '.txt': 5, '.anim': 1, '.mat': 247, '.fbx': 36, '.cs': 14, '.shadergraph': 10, '.wav': 12, '.PNG': 1}

Assets/Hovl Studio/3D Lasers Pack/Demo scene lasers/Demo lasers.unity
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 1 nature.prefab
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 10 fire.prefab
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 2 electro.prefab
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 3 dark fire.prefab
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 4 red.prefab
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 5 magic.prefab
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 6 crystal.prefab
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 7 sparks.prefab
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 8 soul.prefab
Assets/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 9 ray.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Demo scenes/Demo files/SceneSmoke.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Demo scenes/Demo projectiles 2D.unity
Assets/Hovl Studio/AAA Projectiles Vol 1/Demo scenes/Demo projectiles simple spawning.unity
Assets/Hovl Studio/AAA Projectiles Vol 1/Demo scenes/Demo projectiles(Full particles).unity
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Dragon punch flash.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 1 nature arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 10 blue laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 11 orange arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 12 slime.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 13 red laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 14 blue rapid.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 15 pink crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 16 fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 17 nova violet.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 18 nova orange.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 19 circle bomb.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 2 electro.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 22 cute star.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 23 cube.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 24 green explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 25 orange explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 26 blue crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 27 heart.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 3 black fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 4 yellow arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 5 red.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 6 blue fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 7 pink.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 9 water.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 1 nature arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 10 blue laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 11 orange arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 12 slime.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 13 red laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 14 blue rapid.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 15 pink crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 16 fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 17 nova violet.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 18 nova orange.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 19 circle bomb.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 2 electro.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 20 pink arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 21 red arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 22 cute star.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 23 cube.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 24 green explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 25 orange explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 26 blue crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 27 heart.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 3 black fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 4 yellow arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 5 red.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 6 blue fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 7 pink.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 8 dagger.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 9 water.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 1 nature arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 10 blue laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 11 orange arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 12 slime.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 13 red laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 14 blue rapid.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 15 pink crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 16 fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 17 nova violet.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 18 nova orange.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 19 circle bomb.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 2 electro.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 20 pink arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 21 red arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 22 cute star.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 23 cube.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 24 green explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 25 orange explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 26 blue diamond.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 27 heart.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 3 black fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 4 yellow arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 5 red.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 6 blue fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 7 pink.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 8 dagger.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 9 water.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile dragon punch.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 1 nature arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 10 blue laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 11 orange arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 12 slime.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 13 red laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 14 blue rapid.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 15 pink crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 16 fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 17 nova violet.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 18 nova orange.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 19 circle bomb.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 2 electro.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 20 pink arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 21 red arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 22 cute star.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 23 cube.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 24 green explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 25 orange explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 26 blue crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 27 heart.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 3 black fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 4 yellow arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 5 red.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 6 blue fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 7 pink.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 8 dagger.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles 2D/2D Projectile 9 water.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Dragon punch projectile.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 1 nature arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 10 blue laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 11 orange arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 12 slime.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 13 red laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 14 blue rapid.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 15 pink crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 16 fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 17 nova violet.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 18 nova orange.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 19 circle bomb.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 2 electro.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 20 pink arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 21 red arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 22 cute star.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 23 cube.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 24 green explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 25 orange explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 26 blue diamond.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 27 heart.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 3 black fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 4 yellow arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 5 red.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 6 blue fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 7 pink.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 8 dagger.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles with logic/Projectile 9 water.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 1 nature arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 10 blue laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 11 orange arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 12 slime.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 13 red laser.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 14 blue rapid.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 15 pink crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 16 fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 17 nova violet.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 18 nova orange.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 19 circle bomb.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 2 electro.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 20 pink arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 21 red arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 22 cute star.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 23 cube.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 24 green explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 25 orange explosion.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 26 blue crystal.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 27 hear.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 3 black fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 4 yellow arrow.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 5 red.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 6 blue fire.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 7 pink.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 8 dagger.prefab
Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(Particle collision)/Projectile 9 water.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Demo AOE skills/Demo AOE skills.unity
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Energy explosion.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Flower slash.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Front spikes attack.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Ise attack.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Knife hit.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Knives.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Leaves buff.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Lightning hit.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Lightning strike.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Magic attack.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Magic hit.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Meteor 2.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Meteor hit 2.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Meteor hit.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Meteor shower 2.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Meteor shower.prefab
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Meteor.prefab
Assets/Hovl Studio/HSFiles/Models/BeamMesh.fbx
Assets/Hovl Studio/HSFiles/Models/Bullet.fbx
Assets/Hovl Studio/HSFiles/Models/CenterPlate.fbx
Assets/Hovl Studio/HSFiles/Models/Cone12.fbx
Assets/Hovl Studio/HSFiles/Models/Conus.fbx
Assets/Hovl Studio/HSFiles/Models/Crystal.fbx
Assets/Hovl Studio/HSFiles/Models/CylinderFromCenter2.fbx
Assets/Hovl Studio/HSFiles/Models/CylinderFromGround.fbx
Assets/Hovl Studio/HSFiles/Models/CylinderFromGround4.fbx
Assets/Hovl Studio/HSFiles/Models/CylindricalCone.fbx
Assets/Hovl Studio/HSFiles/Models/CylindricalCone2.fbx
Assets/Hovl Studio/HSFiles/Models/DragonHead.fbx
Assets/Hovl Studio/HSFiles/Models/Flower.fbx
Assets/Hovl Studio/HSFiles/Models/HalfCylinder2.fbx
Assets/Hovl Studio/HSFiles/Models/HalfSphere2.fbx
Assets/Hovl Studio/HSFiles/Models/HalfSphere3.fbx
Assets/Hovl Studio/HSFiles/Models/HalfSphereCut.fbx
Assets/Hovl Studio/HSFiles/Models/HOVLCrystal.fbx
Assets/Hovl Studio/HSFiles/Models/HOVLCrystalPiese.fbx
Assets/Hovl Studio/HSFiles/Models/MagicCircle26.fbx
Assets/Hovl Studio/HSFiles/Models/NoiseSphere1.fbx
Assets/Hovl Studio/HSFiles/Models/Path15.fbx
Assets/Hovl Studio/HSFiles/Models/Path4.fbx
Assets/Hovl Studio/HSFiles/Models/Path6.fbx
Assets/Hovl Studio/HSFiles/Models/Petal.fbx
Assets/Hovl Studio/HSFiles/Models/Plate1.fbx
Assets/Hovl Studio/HSFiles/Models/PositionPlate.fbx
Assets/Hovl Studio/HSFiles/Models/Projectile1.fbx
Assets/Hovl Studio/HSFiles/Models/QuadCV.fbx
Assets/Hovl Studio/HSFiles/Models/QuadToCircle.fbx
Assets/Hovl Studio/HSFiles/Models/QuadToCircle2.fbx
Assets/Hovl Studio/HSFiles/Models/Scythe.fbx
Assets/Hovl Studio/HSFiles/Models/ShellCircle.fbx
Assets/Hovl Studio/HSFiles/Models/ShellQuad.fbx
Assets/Hovl Studio/HSFiles/Models/ShellQuad2.fbx
Assets/Hovl Studio/HSFiles/Models/Vortex2.fbx
Assets/Hovl Studio/HSFiles/Scripts/For demo scenes/HS_CameraHolder.cs
Assets/Hovl Studio/HSFiles/Scripts/For demo scenes/HS_DemoLasers.cs
Assets/Hovl Studio/HSFiles/Scripts/For demo scenes/HS_DemoShooting.cs
Assets/Hovl Studio/HSFiles/Scripts/For demo scenes/HS_DemoShooting2D.cs
Assets/Hovl Studio/HSFiles/Scripts/For demo scenes/HS_RaycastInstance.cs
Assets/Hovl Studio/HSFiles/Scripts/HS_CallBackParent.cs
Assets/Hovl Studio/HSFiles/Scripts/HS_EffectOnDie.cs
Assets/Hovl Studio/HSFiles/Scripts/HS_EffectSound.cs
Assets/Hovl Studio/HSFiles/Scripts/HS_Enemy.cs
Assets/Hovl Studio/HSFiles/Scripts/HS_Laser.cs
Assets/Hovl Studio/HSFiles/Scripts/HS_ParticleCollisionInstance.cs
Assets/Hovl Studio/HSFiles/Scripts/HS_ProjectileMover.cs
Assets/Hovl Studio/HSFiles/Scripts/HS_ProjectileMover2D.cs
Assets/Hovl Studio/HSFiles/Scripts/HS_Rotator.cs
Assets/Hovl Studio/HSFiles/Shaders/HS_Blend_CG.shadergraph
Assets/Hovl Studio/HSFiles/Shaders/HS_Blend_TwoSides.shadergraph
Assets/Hovl Studio/HSFiles/Shaders/HS_BlendDistort.shadergraph
Assets/Hovl Studio/HSFiles/Shaders/HS_ChannelCut.shadergraph
Assets/Hovl Studio/HSFiles/Shaders/HS_DissolveNoise.shadergraph
Assets/Hovl Studio/HSFiles/Shaders/HS_Distortion.shadergraph
Assets/Hovl Studio/HSFiles/Shaders/HS_Electricity.shadergraph
Assets/Hovl Studio/HSFiles/Shaders/HS_LightGlow.shadergraph
Assets/Hovl Studio/HSFiles/Shaders/HS_LitFresnel.shadergraph
Assets/Hovl Studio/HSFiles/Shaders/HS_Trail.shadergraph
Assets/Hovl Studio/Magic circles/Demo magic circles Vol 1/Demo magic circles.unity
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic circle blood loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic circle electro loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic circle fire call loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic circle fire loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic circle forest archer loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic circle sun loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic circle suns loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic circle water loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic shield holy loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic shield sacura loop.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle blood.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle dark star.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle electro.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle fire call.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle fire.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle forest archer.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle octagon.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle pink.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle sun sparks.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle sun.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic circle water.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic shield holy.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic shield runes.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic shield sakura.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic shield sparks.prefab
Assets/Hovl Studio/Magic circles/Prefabs/Magic shield yingyang.prefab
Assets/Hovl Studio/Map track markers VFX/Demo scene/Demo scene MTM.unity
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 1 arrows Loop.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 1 arrows.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 2 Pointer Loop.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 2 Pointer.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 3 Zone Loop.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 3 Zone.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 4 Pillar Loop.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 4 Pillar.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 5 Circle Loop.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 5 Circle.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 6 Arrows Loop.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 6 Arrows.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 7 Danger zone Loop.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 7 Danger zone.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 8 Safe zone Loop.prefab
Assets/Hovl Studio/Map track markers VFX/Prefabs/Marker 8 Safe zone.prefab
Assets/Hovl Studio/RPG VFX Bundle/Demo scene/Demo random effects.unity
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Blue capture.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Buff chain.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Buff heal.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Buff orange circle.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Buff orange shot.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Buff orbital.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Buff palladin.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Buff white twist.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Chain explosion.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Chain strike.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Cute buff Yorai.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Debuff chain.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Debuff scythe.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Dragon punch.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Druid aura.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Electro splash.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Gold dot.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Hyperdymension circle.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Lvl up.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Magic Sparks.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Mountains shield.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Punch Hit.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Snowflake trails.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Soft blue buff.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Water flaw.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Wheat arrow hit.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Wheat explosion.prefab
Assets/Hovl Studio/RPG VFX Bundle/Random effect prefabs/Yellow Flash.prefab

## Assets/SpaceCrackTeleport

{'.anim': 1, '.mat': 28, '.fbx': 2, '.FBX': 5, '.prefab': 3, '.unity': 1, '.playable': 1, '.asset': 1, '.shader': 1, '.unitypackage': 1, '.png': 30}

Assets/SpaceCrackTeleport/Models/EffectModel3_2.fbx
Assets/SpaceCrackTeleport/Models/EffectModel5.fbx
Assets/SpaceCrackTeleport/Prefabs/SpaceCrackTeleport_BuiltIn/Effect1.prefab
Assets/SpaceCrackTeleport/Prefabs/SpaceCrackTeleport_BuiltIn/Effect2.prefab
Assets/SpaceCrackTeleport/Prefabs/SpaceCrackTeleport_BuiltIn/Effect3.prefab
Assets/SpaceCrackTeleport/Scene/BuiltIn/DemoScene_BuiltIn.unity
Assets/SpaceCrackTeleport/Shader/VFX_EffectV2.shader

## Assets/Free medieval weapons

{'.mat': 6, '.fbx': 4, '.prefab': 6, '.txt': 1, '.unity': 1, '.png': 24}

Assets/Free medieval weapons/Models/Arrow_stick.fbx
Assets/Free medieval weapons/Models/arrowhead.fbx
Assets/Free medieval weapons/Models/Weapons.fbx
Assets/Free medieval weapons/Models/Wooden Bow.fbx
Assets/Free medieval weapons/Prefabs/Arrow.prefab
Assets/Free medieval weapons/Prefabs/Axe_DS.prefab
Assets/Free medieval weapons/Prefabs/Axe_OS.prefab
Assets/Free medieval weapons/Prefabs/Sword_DH.prefab
Assets/Free medieval weapons/Prefabs/Sword_OH.prefab
Assets/Free medieval weapons/Prefabs/Wooden Bow.prefab
Assets/Free medieval weapons/Scenes/Free medieval weapons.unity
