using System.Collections.Generic;
using Mirror;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
namespace ROM.Progression {
public sealed partial class RiftJournalPlayer {
 public static event System.Action<Vector3,int> ExperienceAwarded;int pendingExperienceFeedback;
  [TargetRpc]void TargetExperienceAward(NetworkConnectionToClient connection,int amount){if(amount>0&&isLocalPlayer&&Player&&Player.MyCharacter)ExperienceAwarded?.Invoke(Player.MyCharacter.transform.position+Vector3.up*2.2f,amount);}
  void FlushExperienceFeedback(){int amount=pendingExperienceFeedback;pendingExperienceFeedback=0;if(amount>0&&connectionToClient!=null)TargetExperienceAward(connectionToClient,amount);}
  AbilityController Combat=>GetComponent<AbilityController>();
 void ApplyProgression(){if(isServer&&State!=null&&Combat){if(State.TalentRanks==null||State.TalentRanks.Length!=9)State.TalentRanks=new int[9];Combat.ServerProgression(State.Experience,State.TalentRanks,State.SwordAndShield);ApplyLootEquipment();}}
 void AwardExperience(int amount,List<string> notices){var tuning=WarriorProgressionDefinition.Load;int level=tuning.Level(State.Experience);int previousExperience=State.Experience;if(State.Experience<tuning.MaximumExperience)State.Experience=(int)System.Math.Min(tuning.MaximumExperience,System.Math.Max(0,(long)State.Experience)+System.Math.Max(0,amount));pendingExperienceFeedback+=Mathf.Max(0,State.Experience-previousExperience);int after=tuning.Level(State.Experience);if(after>level)notices.Add("Nivel "+after+" · "+(after-level)+" punto(s) de talento");}
 void AwardProgressEvent(RiftProgressEvent kind,bool fresh,List<string> notices){var t=WarriorProgressionDefinition.Load;int xp=kind==RiftProgressEvent.Kill?t.KillExperience:kind==RiftProgressEvent.MiniBoss?t.BossExperience:kind==RiftProgressEvent.Encounter?t.EncounterExperience:kind==RiftProgressEvent.Checkpoint&&fresh?t.CrystalExperience:0;AwardExperience(xp,notices);}
 [Server]public void ServerGrantExperience(int amount){if(!Ensure())return;var notices=new List<string>();AwardExperience(amount,notices);Persist();Push(string.Join("\n",notices));}
 [Command]public void CmdTalent(int index){if(!ServerTalent(index))Report("No podés aprender ese talento: revisá nivel, puntos, requisitos y combate.");}
 [Server]public bool ServerTalent(int index){if(!Ensure()||!Combat||!Combat.Prototype||!Combat.CanChangeBuild)return false;var t=Combat.Progression;if(index<0||index>=t.Talents.Length)return false;var node=t.Talents[index];int spent=0;foreach(var rank in State.TalentRanks)spent+=rank;if(State.TalentRanks[index]>=node.MaximumRank||t.Level(State.Experience)<node.RequiredLevel||spent>=t.Level(State.Experience)-1||(node.Parent>=0&&State.TalentRanks[node.Parent]<t.Talents[node.Parent].MaximumRank))return false;var old=JsonUtility.ToJson(State);State.TalentRanks[index]++;if(!Persist()){State=JsonUtility.FromJson<JournalState>(old);return false;}Push("Talento mejorado: "+node.Name);return true;}
 [Command]public void CmdResetTalents(){if(!Ensure()||!Combat||!Combat.Prototype||!Combat.CanChangeBuild){Report("Volvé al refugio o salí de combate para reiniciar talentos.");return;}var old=JsonUtility.ToJson(State);State.TalentRanks=new int[9];if(!Persist()){State=JsonUtility.FromJson<JournalState>(old);return;}Push("Talentos reiniciados · puntos devueltos");}
 [Command]public void CmdWarriorWeapon(bool shield){if(!ServerWarriorWeapon(shield))Report("Cambiá equipo en el refugio o fuera de combate.");}
 [Server]public bool ServerWarriorWeapon(bool shield){if(!Ensure()||!Combat||!Combat.Prototype||!Combat.CanChangeBuild)return false;bool old=State.SwordAndShield;string oldLoot=State.EquippedLootAxe;State.SwordAndShield=shield;State.EquippedLootAxe="";if(!Persist()){State.SwordAndShield=old;State.EquippedLootAxe=oldLoot;return false;}Push(shield?"Espada y escudo equipados":"Hacha a dos manos equipada");return true;}
}}
