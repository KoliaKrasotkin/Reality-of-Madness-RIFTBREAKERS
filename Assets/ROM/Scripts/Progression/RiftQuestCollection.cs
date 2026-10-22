using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using MultiFPS.Gameplay;
using UnityEngine;
namespace ROM.Progression {
public sealed partial class JournalState {
 public bool CanCollect(RiftJournalCatalog catalog,string source,string key){return Quests.Any(q=>CollectObjectives(catalog,q,source,key).Any());}
 IEnumerable<int> CollectObjectives(RiftJournalCatalog catalog,QuestRecord q,string source,string key){
  if(q.Status!=QuestStatus.Active)yield break;var def=catalog.Quest(q.Id);if(!def||q.Progress==null)yield break;
  for(int i=0;i<def.Objectives.Length&&i<q.Progress.Length;i++){var objective=def.Objectives[i];if(objective.Matches(RiftProgressEvent.Collect,key)&&q.Progress[i]<objective.Required&&(q.Seen==null||!q.Seen.Contains("collect:"+i+":"+source)))yield return i;}
 }
 public List<string> RecordCollection(RiftJournalCatalog catalog,string source,string key){
  var notices=new List<string>();bool credited=false;
  foreach(var q in Quests){var indexes=CollectObjectives(catalog,q,source,key).ToArray();if(indexes.Length==0)continue;var def=catalog.Quest(q.Id);if(q.Seen==null)q.Seen=new List<string>();
   foreach(int i in indexes){q.Seen.Add("collect:"+i+":"+source);q.Progress[i]=Math.Min(def.Objectives[i].Required,q.Progress[i]+1);credited=true;}
   if(def.Objectives.Select((o,i)=>q.Progress[i]>=o.Required).All(v=>v)){q.Status=QuestStatus.Ready;notices.Add("Misión lista: "+def.Title);}
  }
  if(credited){AddCounter(RiftProgressEvent.Collect,"",1);if(key.Length>0)AddCounter(RiftProgressEvent.Collect,key,1);}
  return notices;
 }
}
public sealed partial class RiftJournalPlayer {
 [Command]public void CmdCollect(string id){if(!ServerCollect(id,out var reason))Report(reason);}
 [Server]public bool ServerCollect(string id,out string reason){
  reason="Necesitás una misión activa que requiera este objeto.";
  if(!Ensure()||string.IsNullOrEmpty(id)||id.Length>128)return false;
  var matches=RiftQuestCollectible.Active.Where(t=>t&&t.gameObject.activeInHierarchy&&t.StableId==id).ToArray();
  if(matches.Length!=1){reason="Objeto de misión no disponible o identificador duplicado.";return false;}
  var target=matches[0];if(string.IsNullOrEmpty(target.ItemKey))return false;var actor=Player.MyCharacter;
  if(!actor||actor.Health.CurrentHealth<=0||Player.BOT)return false;
  if(!State.CanCollect(Catalog,target.StableId,target.ItemKey))return false;
  if(Vector3.Distance(actor.transform.position,target.transform.position)>Mathf.Clamp(target.Range,1,5)){reason="Acercate al objeto para recogerlo.";return false;}
  if(!target.GetComponent<Collider>()){reason="El objeto no está configurado para interacción.";return false;}
  var origin=actor.transform.position+Vector3.up;var end=target.transform.position+Vector3.up*.15f;
  if(Physics.Linecast(origin,end,out var hit,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore)&&hit.transform!=target.transform&&!hit.transform.IsChildOf(target.transform)){reason="El objeto está detrás de un obstáculo.";return false;}
  if(NetworkTime.time<nextCommand){reason="Esperá un instante.";return false;}
  var old=JsonUtility.ToJson(State);var notices=State.RecordCollection(Catalog,target.StableId,target.ItemKey);nextCommand=NetworkTime.time+.15;
  if(!Persist()){State=JsonUtility.FromJson<JournalState>(old);reason="No se pudo guardar la recolección. Intentá otra vez.";return false;}
  reason="Recogiste "+target.DisplayName;Push(reason+(notices.Count>0?"\n"+string.Join("\n",notices):""));return true;
 }
}
}
