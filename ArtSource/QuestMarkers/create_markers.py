import bpy, bmesh, math, random
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent/'art';OUT.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
random.seed(42)
def material(name,c,metal=.7,emit=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True
 s=m.node_tree.nodes.get('Principled BSDF');s.inputs['Base Color'].default_value=(*c,1);s.inputs['Metallic'].default_value=metal;s.inputs['Roughness'].default_value=.3
 s.inputs['Emission Color'].default_value=(*c,1);s.inputs['Emission Strength'].default_value=emit
 return m
dark=material('Dark bronze edge',(.055,.038,.025),.85)
palettes={}
for key,c in [('Available',(1,.48,.025)),('InProgress',(.65,.72,.8)),('TurnIn',(1,.67,.075)),('Tracked',(.5,.055,1))]:
 palettes[key]=[material(key+' facet '+str(i),tuple(min(1,v*f) for v in c),.6 if key=='InProgress' else .35,.28 if key=='InProgress' else .65) for i,f in enumerate([.72,.86,1,1.14])]
roots=[]
def solid(name,poly,parent,mats,depth=.12,z=0):
 # Triangular front fan creates visible crystal planes, extruded dark side.
 n=len(poly);cx=sum(v[0] for v in poly)/n;cy=sum(v[1] for v in poly)/n
 verts=[(x,y,z-depth/2) for x,y in poly]+[(x,y,z+depth/2) for x,y in poly]+[(cx,cy,z+depth*.95)]
 faces=[tuple(range(n-1,-1,-1))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]+[(n+i,n+(i+1)%n,2*n) for i in range(n)]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.parent=parent
 for m in mats:mesh.materials.append(m)
 for f in mesh.polygons:f.material_index=random.randrange(len(mats)) if f.index>n else 0
 return o
def piece(name,poly,parent,key):
 solid(name+' metal rim',[(x*1.065,y*1.025) for x,y in poly],parent,[dark],.17,-.035)
 return solid(name,poly,parent,palettes[key])
def bez(a,b,c,d,n=12):return [tuple((1-t)**3*a[j]+3*(1-t)**2*t*b[j]+3*(1-t)*t*t*c[j]+t**3*d[j] for j in range(2)) for t in [i/n for i in range(n)]]
def question(parent,key):
 pts=bez((-.36,.4),(-.36,.95),(.48,1.02),(.48,.46))+bez((.48,.46),(.48,.13),(.04,.13),(.04,-.29))+[(.04,-.38)]
 left=[];right=[]
 for i,p in enumerate(pts):
  d=Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(0,i-1)]);d.normalize();v=Vector((-d.y,d.x))*.105
  left.append(tuple(Vector(p)+v));right.append(tuple(Vector(p)-v))
 # One continuous ribbon; facets share vertices, avoiding a serrated/sawtooth outline.
 def ribbon(name,width,z,mats):
  verts=[]
  for i,p in enumerate(pts):
   tangent=Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(0,i-1)]);tangent.normalize();v=Vector((-tangent.y,tangent.x))*width
   verts.extend([(p[0]+v.x,p[1]+v.y,z),(p[0],p[1],z+.025),(p[0]-v.x,p[1]-v.y,z)])
  faces=[]
  for i in range(len(pts)-1):
   k=i*3;faces.extend([(k,k+3,k+4),(k,k+4,k+1),(k+1,k+4,k+5),(k+1,k+5,k+2)])
  mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
  if sum(f.normal.z for f in mesh.polygons)<0:mesh.flip_normals()
  o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.parent=parent
  for m in mats:mesh.materials.append(m)
  for f in mesh.polygons:f.material_index=random.randrange(len(mats))
  mod=o.modifiers.new('Solid metal depth','SOLIDIFY');mod.thickness=.10
 ribbon('Question dark continuous edge',.123,-.025,[dark]);ribbon('Question continuous crystal facets',.105,.025,palettes[key])
 piece('Diamond dot',[(0,-.48),(.16,-.66),(0,-.84),(-.16,-.66)],parent,key)
for key in palettes:
 root=bpy.data.objects.new(key,None);bpy.context.collection.objects.link(root);roots.append(root)
 if key=='Available':
  piece('Amber exclamation',[(0,.96),(.24,.72),(.10,-.36),(0,-.42),(-.10,-.36),(-.24,.72)],root,key)
  piece('Amber diamond',[(0,-.50),(.18,-.69),(0,-.88),(-.18,-.69)],root,key)
 elif key in ['InProgress','TurnIn']:
  question(root,key)
  if key=='TurnIn':piece('Delivery chevron',[(-.19,-.87),(0,-1.02),(.19,-.87),(.19,-.99),(0,-1.15),(-.19,-.99)],root,key)
 else:
  outer=[(0,.82),(.68,0),(0,-.82),(-.68,0)];inner=[(0,.60),(.49,0),(0,-.60),(-.49,0)]
  for i in range(4):piece('Violet diamond side '+str(i),[outer[i],outer[(i+1)%4],inner[(i+1)%4],inner[i]],root,key)
  bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.135,location=(0,0,.07));o=bpy.context.object;o.name='Objective center crystal';o.parent=root;o.data.materials.append(palettes[key][3])
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.film_transparent=True
scene.world.color=(.15,.15,.15);scene.view_settings.view_transform='Standard'
def area(loc,power,size):
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
area((-3,4,5),450,5);area((3,0,4),220,4)
bpy.ops.object.camera_add(location=(0,0,8));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=2.55;scene.camera=cam
scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
for root in roots:
 for r in roots:
  for o in r.children:o.hide_render=r!=root
 scene.render.filepath=str(OUT/(root.name+'.png'));bpy.ops.render.render(write_still=True)
 bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
 for o in root.children:o.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(OUT/(root.name+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
for i,r in enumerate(roots):
 r.location.x=(i-1.5)*1.6
 for o in r.children:o.hide_render=False
cam.data.ortho_scale=7.2;scene.render.resolution_x=1600;scene.render.resolution_y=650;scene.render.filepath=str(OUT/'Blender-four-markers.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RiftQuestMarkers.blend'))
print('MARKERS_COMPLETE',str(OUT))
