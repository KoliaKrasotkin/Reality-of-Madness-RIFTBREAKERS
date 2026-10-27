using System;
using UnityEngine;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
namespace MultiFPS {
// Client presentation. Persistent strings identify abilities, never hotbar positions.
public static class RiftHotbar {
 [Serializable]sealed class Layout {public int[] Order;}
 [Serializable]sealed class LayoutV2 {public string[] Abilities;}
 static bool Prologue=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Crilium";
 static string loadedKey;static string[] order;public static bool Dragging;public static event Action Changed;
 static AbilityController Combat {get{var player=ClientFrontend.ClientPlayerInstance;return player?player.GetComponent<AbilityController>():null;}}
 static string CharacterId=>PlayerPrefs.GetString("ROM.Character.ActiveId",UserSettings.UserNickname);
 static string ClassKey {get{var c=Combat;var p=ClientFrontend.ClientPlayerInstance;return c&&c.Prototype?"warrior":p?"class-"+p.ClassId:"default";}}
 public static string PreferenceKey=>Prologue?"Crilium.Session.Hotbar."+ClassKey:"ROM.Hotbar.v2."+CharacterId+"."+ClassKey;
 public static string LegacyPreferenceKey=>"ROM.Hotbar.v1."+CharacterId+"."+ClassKey;
 static string Id(int ability){var c=Combat;var a=c?c.Definition(ability):null;return a&&!string.IsNullOrEmpty(a.Id)?a.Id:"ability-"+ability;}
 static int Index(string id){if(string.IsNullOrEmpty(id))return 0;var c=Combat;if(c){int found=c.AbilityIndex(id);if(found>0)return found;}for(int i=1;i<=7;i++)if(id=="ability-"+i)return i;return 0;}
 static void Ensure(){string key=PreferenceKey;if(order!=null&&loadedKey==key)return;loadedKey=key;order=new string[7];for(int i=0;i<7;i++)order[i]=Id(i+1);
  if(Prologue)return;
  if(PlayerPrefs.HasKey(key)){try{var saved=JsonUtility.FromJson<LayoutV2>(PlayerPrefs.GetString(key));if(saved?.Abilities==null||saved.Abilities.Length!=7)return;var seen=new System.Collections.Generic.HashSet<string>();for(int i=0;i<7;i++){string id=saved.Abilities[i];order[i]=string.IsNullOrEmpty(id)||!seen.Add(id)?"":id;}}catch{}return;}
  // Read old permutation without deleting or rewriting it. First edit writes v2.
  try{var old=JsonUtility.FromJson<Layout>(PlayerPrefs.GetString(LegacyPreferenceKey,""));if(old?.Order==null||old.Order.Length!=7)return;int mask=0;foreach(int id in old.Order){if(id<1||id>7||(mask&(1<<id))!=0)return;mask|=1<<id;}for(int i=0;i<7;i++)order[i]=Id(old.Order[i]);}catch{}
 }
 public static int Resolve(int slot){Ensure();return slot>=0&&slot<7?Index(order[slot]):0;}
 public static int Slot(int ability){Ensure();return Array.IndexOf(order,Id(ability));}
 public static bool Assign(int ability,int slot){Ensure();var c=Combat;if(ability<1||ability>7||slot<0||slot>=7||!c||!c.IsAbilityLearned(ability))return false;int previous=Slot(ability);if(previous>=0){Swap(previous,slot);return true;}order[slot]=Id(ability);Save();return true;}
 public static void Clear(int slot){Ensure();if(slot<0||slot>=7)return;order[slot]="";Save();}
 public static void Swap(int from,int to){Ensure();if(from<0||from>=7||to<0||to>=7||from==to)return;string previous=order[from];order[from]=order[to];order[to]=previous;Save();}
 public static void Reset(){loadedKey=null;order=null;if(!Prologue){PlayerPrefs.DeleteKey(PreferenceKey);PlayerPrefs.Save();}Ensure();for(int i=0;i<7;i++)order[i]=Id(i+1);Save();}
 public static void Reload(){loadedKey=null;order=null;Ensure();Changed?.Invoke();}
 static void Save(){if(Prologue){Changed?.Invoke();return;}PlayerPrefs.SetString(loadedKey,JsonUtility.ToJson(new LayoutV2{Abilities=order}));PlayerPrefs.Save();Changed?.Invoke();}
}}
