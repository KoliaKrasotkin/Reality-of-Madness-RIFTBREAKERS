using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed class CombatProjectile:MonoBehaviour {
 Vector3 start,end;float began,duration;static Material material;
 public static void Show(Vector3 from,Vector3 to){var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(go.GetComponent<Collider>());go.name="Arcane projectile (presentation)";go.transform.localScale=Vector3.one*.25f;if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.6f,.1f,2));}go.GetComponent<Renderer>().sharedMaterial=material;var motion=go.AddComponent<CombatProjectile>();motion.start=from;motion.end=to;motion.began=Time.time;motion.duration=Vector3.Distance(from,to)/14;}
 void Update(){float t=(Time.time-began)/Mathf.Max(.05f,duration);transform.position=Vector3.Lerp(start,end,t);if(t>=1)Destroy(gameObject);}
}}
