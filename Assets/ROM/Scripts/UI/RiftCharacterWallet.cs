using UnityEngine;
namespace ROM.UI.Sprint01 {
public static class RiftCharacterWallet {
 public static string Id=>PlayerPrefs.GetString("ROM.Character.ActiveId","");
 public static string GoldKey=>"ROM.CharacterWallet.v1."+Id+".Gold";
 public static string PotionKey(int id)=>"ROM.CharacterWallet.v1."+Id+".Potion."+id;
 public static void ImportLegacy(RiftPlayerCharacter character){
  if(character.WalletVersion>0)return;
  string claim="ROM.CharacterWallet.LegacyClaim."+character.Name;
  // Never copy an old named wallet into two characters. Keep old keys untouched.
  if(!PlayerPrefs.HasKey(claim)||PlayerPrefs.GetString(claim)==character.Id){
   string gold="ROM.CharacterWallet.v1."+character.Id+".Gold";
   if(!PlayerPrefs.HasKey(gold))PlayerPrefs.SetInt(gold,PlayerPrefs.GetInt("ROM.Exploration.Gold."+character.Name,0));
   for(int i=0;i<2;i++){string key="ROM.CharacterWallet.v1."+character.Id+".Potion."+i;if(!PlayerPrefs.HasKey(key))PlayerPrefs.SetInt(key,PlayerPrefs.GetInt("ROM.Exploration.Potion."+character.Name+"."+i,0));}
   PlayerPrefs.SetString(claim,character.Id);
  }
  character.WalletVersion=1;PlayerPrefs.Save();
 }
}
}
