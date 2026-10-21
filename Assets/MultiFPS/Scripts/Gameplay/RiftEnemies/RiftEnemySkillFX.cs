using UnityEngine;
namespace MultiFPS.Gameplay.RiftEnemies {
// Bounded local-only VFX pool. Damage is exclusively in the server NPC simulation.
public sealed class RiftEnemySkillFX:MonoBehaviour {
 static RiftEnemySkillFX[] pool=new RiftEnemySkillFX[32];static Material material;
 LineRenderer line;Vector3 from,to;float began,duration,radius;bool projectile;
 static RiftEnemySkillFX Take(){for(int i=0;i<pool.Length;i++){if(!pool[i]){var go=new GameObject("Enemy skill telegraph");pool[i]=go.AddComponent<RiftEnemySkillFX>();var l=pool[i].line=go.AddComponent<LineRenderer>();l.useWorldSpace=false;l.widthMultiplier=.05f;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;if(!material)material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));l.sharedMaterial=material;go.SetActive(false);}if(!pool[i].gameObject.activeSelf)return pool[i];}return null;}
 public static void Ring(Vector3 point,float radius,float seconds,Color color){var fx=Take();if(!fx)return;fx.projectile=false;fx.radius=radius;fx.transform.position=point+Vector3.up*.08f;fx.line.loop=true;fx.line.positionCount=48;for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;fx.line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}fx.Begin(seconds,color);}
 public static void Projectile(Vector3 from,Vector3 to,float seconds,Color color){var fx=Take();if(!fx)return;fx.projectile=true;fx.from=from;fx.to=to;fx.line.loop=false;fx.line.positionCount=2;fx.line.SetPosition(0,Vector3.zero);fx.line.SetPosition(1,Vector3.back*.4f);fx.Begin(seconds,color);}
 void Begin(float seconds,Color color){began=Time.time;duration=Mathf.Max(.1f,seconds);var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color*1.7f);line.SetPropertyBlock(block);gameObject.SetActive(true);}
 void Update(){float t=(Time.time-began)/duration;if(t>=1){gameObject.SetActive(false);return;}if(projectile){transform.position=Vector3.Lerp(from,to,t);transform.rotation=Quaternion.LookRotation(to-from);}}
}
}
