using UnityEngine;
namespace MultiFPS.Gameplay {
    public static class RiftAbilityEffect {
        public static void Show(Vector3 from,Vector3 to,float radius,int id){
            var rules=RiftClassRules.Load();if(!rules||!rules.EffectMaterial)return;
            var def=rules.Find(id);var material=new Material(rules.EffectMaterial);material.color=def?def.Color:Color.cyan;
            var go=new GameObject("Class ability");var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.widthMultiplier=.08f;line.loop=true;line.positionCount=48;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,to+new Vector3(Mathf.Cos(a),.1f,Mathf.Sin(a))*radius);}
            if(Vector3.Distance(from,to)>4){var ray=new GameObject("Spell trace").AddComponent<LineRenderer>();ray.transform.SetParent(go.transform);ray.sharedMaterial=material;ray.widthMultiplier=.13f;ray.positionCount=2;ray.SetPosition(0,from);ray.SetPosition(1,to+Vector3.up);}
            Object.Destroy(go,.45f);Object.Destroy(material,.5f);
        }
    }
}
