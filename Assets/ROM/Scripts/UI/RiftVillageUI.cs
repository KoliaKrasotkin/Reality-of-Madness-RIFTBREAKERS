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
    public sealed class RiftVillageUI : MonoBehaviour
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
            Instance = this;
            if (!W.Body) W.Body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!W.Title) W.Title = W.Body;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 180;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            hint = W.Panel(transform, "Interaccion y pociones", 0, 0, 650, 66);
            hint.anchorMin = hint.anchorMax = new Vector2(.5f, .25f); hint.pivot = new Vector2(.5f, .5f); hint.anchoredPosition = Vector2.zero;
            prompt = W.Label(hint, "", 15, 0, 620, 32, 20);
            supplies = W.Label(hint, "", 15, 32, 620, 30, 18, W.Muted);
        }
        void Update()
        {
            if (!hint) return;
            var player = ClientFrontend.ClientPlayerInstance;
            bool alive = player && player.MyCharacter && player.MyCharacter.Health.CurrentHealth > 0 && !ClientFrontend.Hub;
            if (!alive || ClientFrontend.Pause) { Close(); hint.gameObject.SetActive(false); return; }
            if (TalkingTo)
            {
                if (Vector3.Distance(player.MyCharacter.transform.position, TalkingTo.transform.position) > TalkingTo.InteractionRadius + 1 || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
                balance.text = "ORO DEL JUEGO: " + RiftExplorationProgress.Gold;
                for (int i = 0; i < 2; i++) if (stock[i]) stock[i].text = "En la mochila: " + RiftVillageSupplies.Count(i);
                if (notice) notice.text = NetworkTime.time < noticeUntil ? lastNotice : "";
                hint.gameObject.SetActive(false); return;
            }
            bool free = UICursorManager.GamePlayInput();
            var nearest = free ? FindObjectsByType<RiftVillageNpc>(FindObjectsSortMode.None)
                .Where(n => Vector3.Distance(n.transform.position, player.MyCharacter.transform.position) <= n.InteractionRadius)
                .OrderBy(n => Vector3.Distance(n.transform.position, player.MyCharacter.transform.position)).FirstOrDefault() : null;
            int total = RiftVillageSupplies.Count(0) + RiftVillageSupplies.Count(1);
            bool message = NetworkTime.time < noticeUntil;
            hint.gameObject.SetActive(free && (nearest || total > 0 || message));
            prompt.text = message ? lastNotice : nearest ? "["+MultiFPS.RiftControls.Label("Interact")+"] Hablar con " + nearest.DisplayName + " · " + nearest.Role : "["+MultiFPS.RiftControls.Label("Potion")+"] Beber poción de vida";
            supplies.text = "Pociones: " + RiftVillageSupplies.Count(0) + " / mayores: " + RiftVillageSupplies.Count(1) +
                (player.GetComponent<RiftVillagePlayer>().PotionReadyAt > NetworkTime.time ? " · Recarga " + Mathf.CeilToInt((float)(player.GetComponent<RiftVillagePlayer>().PotionReadyAt - NetworkTime.time)) + " s" : " · "+MultiFPS.RiftControls.Label("Potion")+" para usar");
            if (free && nearest && MultiFPS.RiftControls.Pressed("Interact")) Open(nearest);
            if (free && MultiFPS.RiftControls.Pressed("Potion")) player.GetComponent<RiftVillagePlayer>().CmdDrinkVillagePotion();
        }
        public void Open(RiftVillageNpc npc)
        {
            var player = ClientFrontend.ClientPlayerInstance;
            if (!npc || !player || !player.MyCharacter || Vector3.Distance(player.MyCharacter.transform.position, npc.transform.position) > npc.InteractionRadius) return;
            Close(); TalkingTo = npc; hint.gameObject.SetActive(false);
            if (UIPauseMenu._instance) { previousPauseAllowed = UIPauseMenu._instance.AbleToPause; UIPauseMenu._instance.AbleToPause = false; }
            UICursorManager.RequestCursor(gameObject); cursorHeld = true;
            dialog = W.Panel(transform, "Dialogo de aldea", 0, 0, 980, 670);
            dialog.anchorMin = dialog.anchorMax = dialog.pivot = new Vector2(.5f, .5f); dialog.anchoredPosition = Vector2.zero;
            dialog.GetComponent<Image>().raycastTarget = true;
            W.Label(dialog, npc.DisplayName + " · " + npc.Role, 30, 20, 920, 45, 30, W.White, true);
            speech = W.Label(dialog, npc.Greeting, 30, 80, 920, 100, 23);
            balance = W.Label(dialog, "ORO DEL JUEGO: " + RiftExplorationProgress.Gold, 30, 185, 920, 35, 23, W.Gold);
            W.Button(dialog, "HABLAR DEL REFUGIO", 30, 235, 450, 48, () => speech.text = npc.Advice);
            if (npc.Trades) W.Button(dialog, "VER MERCANCÍA", 500, 235, 450, 48, () => ShowGoods(npc));
            notice = W.Label(dialog, "", 30, 535, 920, 60, 19, W.Muted);
            W.Button(dialog, "CERRAR · ESC", 630, 605, 320, 45, Close);
            if (!npc.Trades) W.Label(dialog, "Explorá las islas y activá sus cristales para establecer checkpoints.", 30, 325, 920, 110, 23, W.Muted);
        }
        public void ShowGoods(RiftVillageNpc npc)
        {
            if (TalkingTo != npc || !npc.Trades) return;
            var old = dialog.Find("Mercancia"); if (old) Destroy(old.gameObject);
            var goods = W.Rect(dialog, "Mercancia", 30, 300, 920, 230);
            for (int i = 0; i < 2; i++)
            {
                int id = i; float y = i * 105;
                W.Label(goods, RiftVillageSupplies.Name(i) + " · recupera " + Mathf.RoundToInt(RiftVillageSupplies.Healing(i) * 100) + "% de vida", 0, y, 900, 33, 23);
                stock[i] = W.Label(goods, "En la mochila: " + RiftVillageSupplies.Count(i), 0, y + 40, 350, 35, 20, W.Muted);
                W.Button(goods, "COMPRAR · " + RiftVillageSupplies.Price(i) + " ORO", 370, y + 37, 265, 43, () => ClientFrontend.ClientPlayerInstance.GetComponent<RiftVillagePlayer>().CmdVillageTrade(npc.Id, id, false));
                W.Button(goods, "VENDER · " + RiftVillageSupplies.Resale(i) + " ORO", 650, y + 37, 270, 43, () => ClientFrontend.ClientPlayerInstance.GetComponent<RiftVillagePlayer>().CmdVillageTrade(npc.Id, id, true));
            }
        }
        public void Close()
        {
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
        void OnDisable() { Close(); if (Instance == this) Instance = null; }
    }
}
