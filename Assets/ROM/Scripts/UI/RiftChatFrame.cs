using UnityEngine;
using TMPro;
using MultiFPS.UI;
namespace ROM.UI.Sprint01 {
public sealed class RiftChatFrame : MonoBehaviour {
public TMP_Text Log;public CanvasGroup Frame;
void LateUpdate(){if(Frame)Frame.alpha=ChatUI.Instance&&ChatUI.Instance.ChatWriting?1:Log&&!string.IsNullOrEmpty(Log.text)?Log.color.a:0;}
}
}
