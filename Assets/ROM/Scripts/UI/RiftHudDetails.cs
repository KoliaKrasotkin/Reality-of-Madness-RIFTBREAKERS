using UnityEngine;
using UnityEngine.UI;
using MultiFPS.UI;
namespace ROM.UI.Sprint01 {
public sealed class RiftHudDetails : MonoBehaviour {
public Text WeaponName,Status;
void LateUpdate(){var actor=ClientFrontend.ObservedCharacterInstance;if(WeaponName)WeaponName.text=actor&&actor.CharacterItemManager.CurrentlyUsedItem?actor.CharacterItemManager.CurrentlyUsedItem.ItemName.ToUpperInvariant():"DESARMADO";if(Status)Status.text=actor&&actor.IsReloading?"RECARGANDO…":"";}
}
}
