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
        public void ShowGameplayPage(HubPage page){if(page==HubPage.Character)page=HubPage.Inventory;requestedGameplayPage=page;if(!content)Build();if(ArsenalPanel)ArsenalPanel.gameObject.SetActive(false);content.gameObject.SetActive(true);Navigate(page);}
        void BuildSkillNavigation(RectTransform panel,bool talents){
            float width=panel.rect.width;float tab=(width-136)/4;
            string[] titles={"PERSONAJE","INVENTARIO","TALENTOS","HABILIDADES"};
            System.Action[] actions={()=>ShowGameplayPage(HubPage.Character),()=>ShowGameplayPage(HubPage.Inventory),ShowTalents,ShowSkills};
            for(int i=0;i<4;i++){var index=i;var b=RiftSkillStyle.Button(panel,titles[i],16+i*tab,14,tab-8,56,()=>actions[index](),i==(talents?2:3));var label=b.GetComponentInChildren<UnityEngine.UI.Text>();label.rectTransform.anchoredPosition=new Vector2(48,0);label.rectTransform.sizeDelta=new Vector2(tab-70,56);var icon=W.Rect(b.transform,"Navigation icon",14,16,24,24).gameObject.AddComponent<ROM.UI.Gameplay.RiftHUDGraphic>();icon.Shape=new[]{ROM.UI.Gameplay.HUDShape.Helmet,ROM.UI.Gameplay.HUDShape.Bag,ROM.UI.Gameplay.HUDShape.Diamond,ROM.UI.Gameplay.HUDShape.Skill}[i];icon.color=RiftSkillStyle.Muted;icon.raycastTarget=false;var tip=b.gameObject.AddComponent<ROM.UI.Gameplay.RiftTooltipTrigger>();tip.Title=titles[i];tip.Description="Abrir panel";tip.ActionId=new[]{"Character","Inventory","Talents","Abilities"}[i];if(i==(talents?2:3))W.Image(b.transform,"Active tab underline",12,53,tab-32,2,RiftSkillStyle.Violet);}
            var close=RiftSkillStyle.Button(panel,"×",width-94,14,72,56,()=>ResumeRequested?.Invoke());close.gameObject.AddComponent<ROM.UI.Gameplay.RiftTooltipTrigger>().ActionId="Pause";
            W.Image(panel,"Navigation divider",20,84,width-40,1,new Color(.4f,.38f,.5f,.55f));
            if(talents){BuildTalentBackdrop(panel);talentShowcase=RiftWarriorShowcase.Create(panel,ClientFrontend.ClientPlayerInstance);}
        }
        void GameplayPage(RectTransform body,HubPage page)
        {
            var player=ClientFrontend.ClientPlayerInstance;var actor=player?player.MyCharacter:null;if(!actor)return;var combat=player.GetComponent<AbilityController>();
            if(page==HubPage.Character){CompactCharacter(body,player,combat);return;}
            if(page==HubPage.Inventory){BuildInventory(body,player,combat);return;}
            if(abilityTab=="Talents"){TalentTree(body);return;}
            body.gameObject.AddComponent<RiftSpellbookView>().Build(player,combat,this);
        }
    }
}
