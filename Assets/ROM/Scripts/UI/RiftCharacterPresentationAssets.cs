using System;using UnityEngine;
namespace ROM.UI.Sprint01 {
[CreateAssetMenu(menuName="ROM/UI/Character presentation")]
public sealed class RiftCharacterPresentationAssets:ScriptableObject {
 public Sprite NertorPortrait;
 [TextArea]public string Lore="Nertör cruza Crelium con la fuerza de un guerrero. Al entrar en El Rift, deberá recuperar el poder que le fue arrebatado.";
 [TextArea]public string Style="Combate cuerpo a cuerpo\nGenerá furia al luchar y gastala en golpes poderosos.\n\nControl del encuentro\nCombiná ataques de área, movilidad y defensa.\n\nEquipo\nHacha a dos manos o espada y escudo.";
 public static RiftCharacterPresentationAssets Load=>Resources.Load<RiftCharacterPresentationAssets>("RiftMenu/CharacterPresentationAssets");
}
public static class RiftCharacterPortraitChoice {
 public static string Key(string id)=>"ROM.CharacterPortrait.v1."+id;
 public static bool UsesNertor(string id)=>!string.IsNullOrEmpty(id)&&PlayerPrefs.GetString(Key(id),"")=="nertor";
 public static void SetNertor(string id,bool value){if(!Guid.TryParse(id,out _))return;PlayerPrefs.SetString(Key(id),value?"nertor":"class");PlayerPrefs.Save();}
}
}
