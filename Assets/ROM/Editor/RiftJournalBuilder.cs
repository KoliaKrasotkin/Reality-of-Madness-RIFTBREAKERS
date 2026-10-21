using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using ROM.Progression;
using MultiFPS.Gameplay;
namespace ROM.EditorTools
{
    public static class RiftJournalBuilder
    {
        const string Root="Assets/ROM/Progression";
        static T Asset<T>(string path)where T:ScriptableObject{var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(!asset){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset;}
        static JournalObjective O(RiftProgressEvent kind,string label,int amount=1,string filter="",bool unique=false)=>new JournalObjective{Event=kind,Label=label,Required=amount,Filter=filter,Unique=unique};
        static RiftQuestDefinition Quest(string id,string title,string description,string giver,string prerequisite,int gold,params JournalObjective[] objectives){var q=Asset<RiftQuestDefinition>(Root+"/Quests/"+id+".asset");q.Id=id;q.Title=title;q.Description=description;q.Giver=q.TurnIn=giver;q.Prerequisite=prerequisite;q.Gold=gold;q.Objectives=objectives;EditorUtility.SetDirty(q);return q;}
        static RiftAchievementDefinition Achievement(string id,string title,string description,int points,JournalObjective criterion){var a=Asset<RiftAchievementDefinition>(Root+"/Achievements/"+id+".asset");a.Id=id;a.Title=title;a.Description=description;a.Points=points;a.Criterion=criterion;EditorUtility.SetDirty(a);return a;}
        [MenuItem("ROM/Progresión/Preparar misiones y logros")]
        public static void Build()
        {
            foreach(string folder in new[]{Root,Root+"/Resources",Root+"/Quests",Root+"/Achievements"})Directory.CreateDirectory(folder);AssetDatabase.Refresh();var catalog=Asset<RiftJournalCatalog>(Root+"/Resources/RiftJournalCatalog.asset");
            catalog.Quests=new[]{
                Quest("refuge-training","Antes de cruzar el puente","Iria quiere asegurarse de que estés preparado. Practicá con el muñeco de la plaza y hablá con Bram, el intendente.","iria","",60,O(RiftProgressEvent.TrainingHit,"Golpes al muñeco",3),O(RiftProgressEvent.Talk,"Hablar con Bram",1,"bram")),
                Quest("refuge-supplies","Provisiones para el camino","Alma te enseña a usar las provisiones. Comprá una poción y utilizala cuando hayas perdido vida.","alma","refuge-training",40,O(RiftProgressEvent.PotionBought,"Comprar una poción"),O(RiftProgressEvent.PotionUsed,"Usar una poción")),
                Quest("rift-scout","Un paso más allá","Salí del refugio, derrotá patrullas y activá un cristal de otra isla. Después regresá con Iria.","iria","refuge-training",120,O(RiftProgressEvent.Kill,"Enemigos derrotados",5),O(RiftProgressEvent.Checkpoint,"Activar un cristal de otra isla",1,"",true)),
                Quest("rift-guardian","Romper el asedio","Bram necesita que despejes un encuentro y derrotes a uno de los guardianes del Rift.","bram","rift-scout",200,O(RiftProgressEvent.MiniBoss,"Guardianes derrotados"),O(RiftProgressEvent.Encounter,"Encuentros despejados"))
            };
            catalog.Achievements=new[]{
                Achievement("first-training","Primeras armas","Golpeá el muñeco de entrenamiento.",5,O(RiftProgressEvent.TrainingHit,"Golpes",1)),
                Achievement("first-kill","Primera victoria","Derrotá un enemigo real de El Rift.",10,O(RiftProgressEvent.Kill,"Enemigos",1)),
                Achievement("ten-kills","Defensor del refugio","Derrotá diez enemigos de El Rift.",20,O(RiftProgressEvent.Kill,"Enemigos",10)),
                Achievement("three-crystals","Cartógrafo de fisuras","Descubrí tres cristales distintos fuera del refugio.",20,O(RiftProgressEvent.Checkpoint,"Cristales",3,"",true)),
                Achievement("first-boss","Caída de un guardián","Derrotá un miniboss de El Rift.",25,O(RiftProgressEvent.MiniBoss,"Guardianes",1)),
                Achievement("first-quest","Una promesa cumplida","Entregá tu primera misión.",10,O(RiftProgressEvent.QuestTurnedIn,"Misiones",1)),
                Achievement("village-friends","Voces del refugio","Hablá con Iria, Alma y Bram.",10,O(RiftProgressEvent.Talk,"Habitantes",3,"",true))
            };
            EditorUtility.SetDirty(catalog);string playerPath="Assets/MultiFPS/Levels/Prefabs/Gameplay/MultiFPS_PlayerInstance.prefab";var player=PrefabUtility.LoadPrefabContents(playerPath);try{var journal=player.GetComponent<RiftJournalPlayer>();if(!journal)journal=player.AddComponent<RiftJournalPlayer>();journal.Catalog=catalog;PrefabUtility.SaveAsPrefabAsset(player,playerPath);}finally{PrefabUtility.UnloadPrefabContents(player);}AssetDatabase.SaveAssets();RiftGameplayHUDBuilder.Build();
        }
        public static void Run(){try{Build();Debug.Log("JOURNAL BUILD PASSED");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
