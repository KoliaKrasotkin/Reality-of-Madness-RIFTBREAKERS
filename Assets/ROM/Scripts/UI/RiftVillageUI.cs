using System.Linq;
using Mirror;
using MultiFPS.UI;
using ROM.Horde;
using UnityEngine;
using UnityEngine.UI;
using W = ROM.UI.Sprint01.RiftWidgets;

namespace ROM.UI.Sprint01
{
    [DefaultExecutionOrder(500)]
    public sealed partial class RiftVillageUI : MonoBehaviour
    {
        public static RiftVillageUI Instance { get; private set; }
        public RiftVillageNpc TalkingTo { get; private set; }
        RectTransform dialog, hint;
        Text prompt, supplies, balance, speech, notice;
        readonly Text[] stock = new Text[2];
        bool previousPauseAllowed, cursorHeld;
        double noticeUntil;
        static string lastNotice;

        void Start()
        {
            Instance = this;ROM.Progression.RiftJournalView.Changed+=JournalChanged;
            if (!W.Body) W.Body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!W.Title) W.Title = W.Body;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 180;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            hint = W.Panel(transform, "Interacción contextual", 0, 0, 380, 40);
            hint.anchorMin = hint.anchorMax = new Vector2(.5f, .32f); hint.pivot = new Vector2(.5f, .5f); hint.anchoredPosition = Vector2.zero;
            prompt = W.Label(hint, "", 12, 3, 356, 34, 17);
            supplies = W.Label(hint, "", 0, 0, 1, 1, 12, W.Muted);supplies.gameObject.SetActive(false);
        }
        void Update()
        {
            if (!hint) return;
            var player = ClientFrontend.ClientPlayerInstance;
            bool alive = player && player.MyCharacter && player.MyCharacter.Health.CurrentHealth > 0 && !ClientFrontend.Hub;
            if (!alive || ClientFrontend.Pause) { Close(); hint.gameObject.SetActive(false); return; }
            if(cursorHeld&&!TalkingTo){Close();return;}
            if (TalkingTo)
            {
                if(CinematicDialogue&&!MultiFPS.RiftControls.TextEntryFocused&&(MultiFPS.RiftControls.Held("Forward")||MultiFPS.RiftControls.Held("Backward")||MultiFPS.RiftControls.Held("StrafeLeft")||MultiFPS.RiftControls.Held("StrafeRight")||MultiFPS.RiftControls.Pressed("Jump"))){Close();return;}
                VendorTick();
                if (Vector3.Distance(player.MyCharacter.transform.position, TalkingTo.transform.position) > TalkingTo.InteractionRadius + 1 || MultiFPS.RiftControls.Pressed("Pause")) { MultiFPS.RiftControls.EscapeConsumedFrame=Time.frameCount;Close(); return; }
                balance.text = "ORO DEL JUEGO: " + RiftExplorationProgress.Gold;
                for (int i = 0; i < 2; i++) if (stock[i]) stock[i].text = "En la mochila: " + RiftVillageSupplies.Count(i);
                if (notice) notice.text = NetworkTime.time < noticeUntil ? lastNotice : "Rueda para leer · Esc para cerrar";
                hint.gameObject.SetActive(false); return;
            }
            bool free = UICursorManager.GamePlayInput();
            RiftVillageNpc nearest=null;float nearestDistance=float.PositiveInfinity;
            if(free)foreach(var npc in RiftVillageNpc.Active){if(!npc)continue;float distance=(npc.transform.position-player.MyCharacter.transform.position).sqrMagnitude;if(distance<=npc.InteractionRadius*npc.InteractionRadius&&distance<nearestDistance){nearest=npc;nearestDistance=distance;}}
            var selected=MultiFPS.Gameplay.RiftMMOController.Instance?MultiFPS.Gameplay.RiftMMOController.Instance.SelectedWorld:null;var selectedNpc=selected?selected.GetComponent<RiftVillageNpc>():null;if(selectedNpc&&Vector3.Distance(selectedNpc.transform.position,player.MyCharacter.transform.position)<=selectedNpc.InteractionRadius)nearest=selectedNpc;
            
            var selectedItem=selected?selected.GetComponent<ROM.Progression.RiftQuestCollectible>():null;if(selectedItem)nearest=null;
            bool message = NetworkTime.time < noticeUntil;
            hint.gameObject.SetActive(free && !(UIPauseMenu._instance&&UIPauseMenu._instance.InterfaceShowing) && !MultiFPS.Gameplay.RiftMMOController.PointerOnUI && (message || !selectedNpc && nearest && nearest.Id!="alma"));
            prompt.text = message ? lastNotice : selectedItem ? "["+MultiFPS.RiftControls.Label("Interact")+"] Recoger "+selectedItem.DisplayName : nearest ? "["+MultiFPS.RiftControls.Label("Interact")+"] Hablar con " + nearest.DisplayName + "" : "["+MultiFPS.RiftControls.Label("Potion")+"] Beber poción de vida";
            supplies.text = "Pociones: " + RiftVillageSupplies.Count(0) + " / mayores: " + RiftVillageSupplies.Count(1) +
                (player.GetComponent<RiftVillagePlayer>().PotionReadyAt > NetworkTime.time ? " · Recarga " + Mathf.CeilToInt((float)(player.GetComponent<RiftVillagePlayer>().PotionReadyAt - NetworkTime.time)) + " s" : " · "+MultiFPS.RiftControls.Label("Potion")+" para usar");
            if (free && nearest && MultiFPS.RiftControls.Pressed("Interact")) Open(nearest);
            // Potion shortcuts are handled by the compact gameplay consumable bar.
        }
        public void Open(RiftVillageNpc npc)
        {
            var player = ClientFrontend.ClientPlayerInstance;
            if (!npc || !player || !player.MyCharacter || Vector3.Distance(player.MyCharacter.transform.position, npc.transform.position) > npc.InteractionRadius) return;
            if(ROM.UI.Gameplay.RiftSessionChatHUD.Instance&&ROM.UI.Gameplay.RiftSessionChatHUD.Instance.Writing)ROM.UI.Gameplay.RiftSessionChatHUD.Instance.EndTyping(false);if(UIPauseMenu._instance&&UIPauseMenu._instance.InterfaceShowing)UIPauseMenu._instance.ShowInterface(false);Close();var dialogueCamera=MultiFPS.GameplayCamera._instance?MultiFPS.GameplayCamera._instance.GetComponent<MultiFPS.Gameplay.RiftThirdPersonCamera>():null;if(dialogueCamera&&!dialogueCamera.BeginConversation(npc.transform,npc.Target?npc.Target.Head:null,npc.InteractionRadius,Close))return; TalkingTo = npc;var journal=player.GetComponent<ROM.Progression.RiftJournalPlayer>();if(journal)journal.CmdTalk(npc.Id); hint.gameObject.SetActive(false);
            if (UIPauseMenu._instance) { previousPauseAllowed = UIPauseMenu._instance.AbleToPause; UIPauseMenu._instance.AbleToPause = false; }
            UICursorManager.RequestCursor(gameObject); cursorHeld = true;
            ROM.UI.Gameplay.RiftAbilityTooltip.HideAll();
            dialog = W.Panel(transform, "Dialogo de aldea", 0, 0, DialogueWidth, DialogueHeight);
            dialog.anchorMin=dialog.anchorMax=dialog.pivot=new Vector2(.5f,0);dialog.anchoredPosition=new Vector2(0,8);dialog.GetComponent<Image>().raycastTarget=true;
            W.Label(dialog,npc.DisplayName,20,10,540,30,23,W.White,true);
            W.Label(dialog,npc.Role,600,16,300,20,14,W.Muted);
            balance=W.Label(dialog,"",0,0,1,1,12);balance.gameObject.SetActive(false);
            notice=W.Label(dialog,"",20,134,820,18,13,W.Muted);
            var close=W.Button(dialog,"CERRAR · ESC",960,10,180,28,Close);close.GetComponentInChildren<Text>().fontSize=14;
            dialogueQuest=null;ShowQuests(npc);RefreshDialogueHUD();LayoutDialogue();

        }
        public void ShowGoods(RiftVillageNpc npc)
        {
            if (TalkingTo != npc || !npc.Trades) return;
            ShowVendor(npc);
        }

        public void Close()
        {
            RestoreDialogueHUD();
            CloseVendor();
            var dialogueCamera=MultiFPS.GameplayCamera._instance?MultiFPS.GameplayCamera._instance.GetComponent<MultiFPS.Gameplay.RiftThirdPersonCamera>():null;if(dialogueCamera)dialogueCamera.EndConversation();
            if (cursorHeld)
            {
                cursorHeld = false;
                UICursorManager.ReleaseCursor(gameObject);
                if (UIPauseMenu._instance) UIPauseMenu._instance.AbleToPause = previousPauseAllowed;
            }
            TalkingTo = null;
            if (dialog) Destroy(dialog.gameObject);
            dialog = null;
        }
        public static void Notify(string message)
        {
            lastNotice = message;
            if (Instance) Instance.noticeUntil = NetworkTime.time + 5;
        }
        void OnDestroy(){ROM.Progression.RiftJournalView.Changed-=JournalChanged;}
        void OnDisable() { Close(); if (Instance == this) Instance = null; }
    }
}
