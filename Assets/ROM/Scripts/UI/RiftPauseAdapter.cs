using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MultiFPS.UI;
namespace ROM.UI.Sprint01 {
public sealed class RiftPauseAdapter : MonoBehaviour {
public UIPauseMenu Pause;public RiftHub Hub;public Button Disconnect,ArsenalCancel;
void Awake(){Hub.InGame=true;Hub.ResumeRequested=()=>Pause.ShowPauseMenu(false);Hub.LeaveRequested=()=>Disconnect.onClick.Invoke();if(ArsenalCancel)ArsenalCancel.onClick.AddListener(Hub.ShowGameSettings);}
}
}
