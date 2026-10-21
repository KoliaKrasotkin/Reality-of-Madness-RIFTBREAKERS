using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay {
public sealed class TrainingTargetDisplay : MonoBehaviour {
    TextMesh label;Health health;
    void Start(){health=GetComponent<Health>();var go=new GameObject("Training target label");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*3.1f;label=go.AddComponent<TextMesh>();label.anchor=TextAnchor.MiddleCenter;label.characterSize=.045f;label.fontSize=48;label.color=new Color(1,.85f,.35f);}
    void LateUpdate(){if(!label)return;if(GameplayCamera._instance)label.transform.rotation=GameplayCamera._instance.transform.rotation;bool recent=NetworkTime.time-health.TrainingLastHit<10;label.text="MUÑECO DE ENTRENAMIENTO\n"+(recent?"DAÑO: "+health.TrainingLastDamage+"  ·  TOTAL: "+health.TrainingTotalDamage:"ARMAS + HABILIDADES F / Z")+"\nNo ataca · No da recompensas";}
}
}
