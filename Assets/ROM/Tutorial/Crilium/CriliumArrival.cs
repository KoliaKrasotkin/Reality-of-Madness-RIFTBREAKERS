using UnityEngine;using ROM.Horde;using ROM.UI.Sprint01;
namespace ROM.Tutorial.Crilium {
public sealed class CriliumArrival:MonoBehaviour {
 public RiftVillageNpc Guide;
 void Start(){if(!CriliumSession.Active)return;if(!RiftVillageUI.Instance){var ui=new GameObject("Crelium village dialogue",typeof(RectTransform)).AddComponent<RiftVillageUI>();ui.transform.SetParent(transform,false);}if(Guide)Guide.ConfigureTarget();}
}
}
