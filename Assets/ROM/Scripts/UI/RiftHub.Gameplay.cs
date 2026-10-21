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
        public void ShowGameplayPage(HubPage page){requestedGameplayPage=page;if(!content)Build();if(ArsenalPanel)ArsenalPanel.gameObject.SetActive(false);content.gameObject.SetActive(true);Navigate(page);}
        void GameplayPage(RectTransform body,HubPage page)
        {
            var player=ClientFrontend.ClientPlayerInstance;var actor=player?player.MyCharacter:null;if(!actor)return;var combat=player.GetComponent<AbilityController>();
            if(page==HubPage.Character){W.Label(body,actor.Health.CharacterName,20,10,900,60,32,W.Gold);W.Label(body,(combat&&combat.Prototype?"Warrior":player.ClassDefinition.Name)+"\nVida: "+actor.Health.CurrentHealth+" / "+actor.Health.MaxHealth,20,90,900,150,24);return;}
            if(page==HubPage.Inventory){W.Label(body,"ORO LOCAL  "+RiftExplorationProgress.Gold.ToString("N0"),20,10,900,50,26,W.Gold);var weapon=combat&&combat.Prototype?combat.Specialization.Weapon:null;W.Label(body,"EQUIPADO\n"+(weapon?weapon.DisplayName:"Equipo de personaje"),20,90,900,120,24);for(int i=0;i<2;i++)W.Label(body,RiftVillageSupplies.Name(i)+"  × "+RiftVillageSupplies.Count(i),20,240+i*65,900,50,24);return;}
            if(combat&&combat.Prototype){for(int i=0;i<7;i++){var a=combat.Definition(i);if(!a)continue;int row=i;W.Image(body,a.name,20,10+row*88,64,64,Color.white,a.Icon);W.Label(body,a.DisplayName+(!a.Implemented?" · PRÓXIMAMENTE":""),105,8+row*88,1300,35,24,W.Gold);W.Label(body,a.Description+"  ·  Furia: "+a.Cost+"  ·  Recarga: "+a.Cooldown+" s",105,43+row*88,1550,35,18);}}
            else if(player.ClassDefinition)W.Label(body,player.ClassDefinition.FirstAbility+" [F]\n"+player.ClassDefinition.SecondAbility+" [Z]",20,20,1200,200,26);
        }
    }
}
