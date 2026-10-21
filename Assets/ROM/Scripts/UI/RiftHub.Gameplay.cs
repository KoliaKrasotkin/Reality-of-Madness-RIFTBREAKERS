using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01
{
    public sealed partial class RiftHub
    {
        HubPage? requestedGameplayPage;
        public void ShowSkills(){abilityTab="Skills";ShowGameplayPage(HubPage.Abilities);}
        public void ShowPartyChat(){peer="party";ShowGameplayPage(HubPage.Social);}
        public void ShowTalents(){abilityTab="Talents";ShowGameplayPage(HubPage.Abilities);}
        public void ShowGameplayPage(HubPage page){requestedGameplayPage=page;if(!content)Build();if(ArsenalPanel)ArsenalPanel.gameObject.SetActive(false);content.gameObject.SetActive(true);Navigate(page);}
        void GameplayPage(RectTransform body,HubPage page)
        {
            var player=ClientFrontend.ClientPlayerInstance;var actor=player?player.MyCharacter:null;if(!actor)return;var combat=player.GetComponent<AbilityController>();
            if(page==HubPage.Character){if(combat&&combat.Prototype){W.Button(body,combat.SwordAndShield?"EQUIPAR HACHA A DOS MANOS":"EQUIPAR ESPADA Y ESCUDO",1000,90,680,60,()=>player.GetComponent<ROM.Progression.RiftJournalPlayer>().CmdWarriorWeapon(!combat.SwordAndShield));W.Label(body,"Nivel "+combat.Level+" · XP "+combat.Progression.InLevel(combat.Experience)+" / "+combat.Progression.Required(combat.Level)+"\nDaño +"+((combat.PowerMultiplier-1)*100).ToString("0")+"% · Crítico "+(combat.CriticalChance*100).ToString("0")+"%\nEquipo: "+(combat.SwordAndShield?"Espada y escudo":"Hacha a dos manos"),20,270,1600,160,24);}W.Label(body,actor.Health.CharacterName,20,10,900,60,32,W.Gold);W.Label(body,(combat&&combat.Prototype?"Warrior":player.ClassDefinition.Name)+"\nVida: "+actor.Health.CurrentHealth+" / "+actor.Health.MaxHealth,20,90,900,150,24);return;}
            if(page==HubPage.Inventory){BuildInventory(body,player,combat);return;}
            W.Button(body,abilityTab=="Talents"?"VER HABILIDADES":"TALENTOS",1250,0,450,45,()=>{abilityTab=abilityTab=="Talents"?"Skills":"Talents";Navigate(HubPage.Abilities);});if(abilityTab=="Talents"){TalentTree(body);return;}
            W.Button(body,"RESTAURAR BARRA",900,0,320,45,()=>{RiftHotbar.Reset();Notify("Barra restaurada a su orden original.");});
            if(combat){var auto=W.Label(body,"AUTOATAQUE ["+RiftControls.Label("AutoAttack")+"] · "+combat.AutoProfile.DisplayName+" · "+combat.AutoProfile.Interval+" s · Crítico "+(combat.AutoProfile.CriticalChance*100).ToString("0")+"%",20,0,850,45,20,W.Gold);auto.raycastTarget=true;auto.gameObject.AddComponent<ROM.UI.Gameplay.RiftTooltipTrigger>().Ability=0;}
            if(combat&&combat.Prototype){for(int i=0;i<7;i++){var a=combat.Definition(i+1);if(!a)continue;int row=i;var icon=W.Image(body,a.name,20,65+row*88,64,64,Color.white,a.Icon);icon.raycastTarget=true;var hover=icon.gameObject.AddComponent<ROM.UI.Gameplay.RiftTooltipTrigger>();hover.Ability=i+1;W.Label(body,a.DisplayName+(!a.Implemented?" · PRÓXIMAMENTE":""),105,63+row*88,1300,35,24,W.Gold);W.Label(body,a.Description+"  ·  Furia: "+a.Cost+"  ·  Recarga: "+a.Cooldown+" s",105,98+row*88,1550,35,18);}}
            else if(player.ClassDefinition)W.Label(body,player.ClassDefinition.FirstAbility+" ["+RiftControls.Label("Ability0")+"]\n"+player.ClassDefinition.SecondAbility+" ["+RiftControls.Label("Ability1")+"]",20,70,1200,200,26);
        }
    }
}
