using UnityEngine;

namespace ROM.Horde
{
    public sealed class RiftVillageNpc : MonoBehaviour
    {
        public string Id, DisplayName, Role;
        [TextArea] public string Greeting;
        [TextArea] public string Advice;
        public bool Trades;
        public float InteractionRadius = 4.5f;
        public MultiFPS.Gameplay.RiftWorldTarget Target {get;private set;}
        TextMesh questMarker;float nextMarker;
        void Awake(){ConfigureTarget();}
        void Start(){var go=new GameObject("Quest offer marker");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*2.95f;questMarker=go.AddComponent<TextMesh>();questMarker.fontSize=60;questMarker.characterSize=.075f;questMarker.anchor=TextAnchor.MiddleCenter;questMarker.color=new Color(1,.8f,.25f);}
        void LateUpdate(){if(!questMarker)return;if(Time.unscaledTime>=nextMarker){nextMarker=Time.unscaledTime+.35f;questMarker.text=ROM.Progression.RiftJournalView.Offer(Id);}if(MultiFPS.GameplayCamera._instance)questMarker.transform.rotation=MultiFPS.GameplayCamera._instance.transform.rotation;}

        public void ConfigureTarget(){Target=GetComponent<MultiFPS.Gameplay.RiftWorldTarget>();if(!Target)Target=gameObject.AddComponent<MultiFPS.Gameplay.RiftWorldTarget>();Target.Id=Id;Target.DisplayName=DisplayName;Target.Role=Role;Target.InteractionRange=InteractionRadius;Target.InteractRequested-=Interact;Target.InteractRequested+=Interact;}
        void Interact(){if(ROM.UI.Sprint01.RiftVillageUI.Instance)ROM.UI.Sprint01.RiftVillageUI.Instance.Open(this);}
        void OnDestroy(){if(Target)Target.InteractRequested-=Interact;}

    }
}
