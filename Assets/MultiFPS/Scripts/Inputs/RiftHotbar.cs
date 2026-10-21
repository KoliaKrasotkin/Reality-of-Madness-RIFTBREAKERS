using System;
using UnityEngine;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
namespace MultiFPS {
// Local presentation only. The server still validates immutable ability IDs and cooldowns.
public static class RiftHotbar {
 [Serializable]sealed class Layout {public int[] Order;}
 static string loadedKey;static int[] order;public static bool Dragging;public static event Action Changed;
 public static string PreferenceKey {get{var player=ClientFrontend.ClientPlayerInstance;var combat=player?player.GetComponent<AbilityController>():null;return "ROM.Hotbar.v1."+PlayerPrefs.GetString("ROM.Character.ActiveId",UserSettings.UserNickname)+"."+(combat&&combat.Prototype?"warrior":player?"class-"+player.ClassId:"default");}}
 static void Ensure(){string key=PreferenceKey;if(order!=null&&loadedKey==key)return;loadedKey=key;order=new[]{1,2,3,4,5,6,7};try{var saved=JsonUtility.FromJson<Layout>(PlayerPrefs.GetString(key,""));if(saved?.Order==null||saved.Order.Length!=7)return;int mask=0;foreach(var id in saved.Order){if(id<1||id>7||(mask&(1<<id))!=0)return;mask|=1<<id;}order=(int[])saved.Order.Clone();}catch{}}
 public static int Resolve(int slot){Ensure();return slot>=0&&slot<7?order[slot]:0;}
 public static int Slot(int ability){Ensure();return Array.IndexOf(order,ability);}
 public static void Swap(int from,int to){Ensure();if(from<0||from>=7||to<0||to>=7||from==to)return;int previous=order[from];order[from]=order[to];order[to]=previous;Save();}
 public static void Reset(){loadedKey=null;order=null;PlayerPrefs.DeleteKey(PreferenceKey);PlayerPrefs.Save();Ensure();Changed?.Invoke();}
 public static void Reload(){loadedKey=null;order=null;Ensure();}
 static void Save(){PlayerPrefs.SetString(loadedKey,JsonUtility.ToJson(new Layout{Order=order}));PlayerPrefs.Save();Changed?.Invoke();}
}}
