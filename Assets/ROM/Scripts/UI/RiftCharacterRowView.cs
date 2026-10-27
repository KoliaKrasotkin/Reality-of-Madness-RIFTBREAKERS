using System;
using UnityEngine;
using UnityEngine.UI;
using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
public sealed class RiftCharacterRowView : MonoBehaviour {
 public Button Select; public Text Name,Details,Status; public Image Portrait;
 public RiftHUDGraphic Placeholder; public RiftMapSurface Surface;
 public Color NormalFill=new Color(.035f,.035f,.06f,.95f),SelectedFill=new Color(.12f,.065f,.2f,.95f);
 Action select;
 void Awake(){Select.onClick.AddListener(()=>select?.Invoke());}
 public void Bind(RiftPlayerCharacter record,int slot,bool selected,string details,Action action){
  select=action;Name.text=record.DisplayName+" · "+slot;Details.text=details;
  Status.text=!record.Available?"Clase no disponible":selected?"SELECCIONADO":"";
  Select.interactable=record.Available;Surface.Fill=selected?SelectedFill:NormalFill;Surface.SetVerticesDirty();
  var art=RiftCharacterPresentationAssets.Load;bool portrait=record.Skin==1&&art&&art.NertorPortrait;
  Portrait.gameObject.SetActive(portrait);if(portrait)Portrait.sprite=art.NertorPortrait;Placeholder.gameObject.SetActive(!portrait);
 }
}
}
