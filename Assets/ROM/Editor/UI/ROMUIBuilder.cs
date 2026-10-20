using System.Collections.Generic;
using System.IO;
using System.Linq;
using MultiFPS;
using ROM.Horde;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using F = ROM.UI.EditorTools.ROMUIFactory;

namespace ROM.UI.EditorTools
{
    /// <summary>
    /// Arma la UI de ROM como prefabs y la deja instalada en las escenas, para que
    /// todo se vea y se edite desde la Hierarchy. Nada de esto corre en el juego.
    ///
    /// - Assets/ROM/UI/ROM_MainMenu.prefab: menú principal (en el hub).
    /// - Assets/ROM/UI/ROM_LoadingScreen.prefab: pantalla de carga (en el hub, se
    ///   vuelve persistente al arrancar).
    /// - Assets/ROM/UI/HordeHUD.prefab: HUD del modo Horda (en el mapa Horda).
    ///
    /// Los prefabs se crean solo si no existen: después de editarlos a mano, volver
    /// a correr "Construir e instalar" no pisa nada. Para empezar de cero está
    /// "Regenerar prefabs", que pide confirmación.
    /// </summary>
    public static class ROMUIBuilder
    {
        const string UIRoot = "Assets/ROM/UI";
        const string MenuPrefabPath = UIRoot + "/ROM_MainMenu.prefab";
        const string LoadingPrefabPath = UIRoot + "/ROM_LoadingScreen.prefab";
        const string HordeHUDPrefabPath = UIRoot + "/HordeHUD.prefab";
        const string HordeThemePath = "Assets/ROM/Levels/Horde/HordeHUDTheme.asset";
        const string HubScenePath = "Assets/ROM/Scenes/MainMenu.unity";
        const string HordeScenePath = "Assets/ROM/Levels/map_rom_horde.unity";
        const string KitUIPrefabs = "Assets/MultiFPS/Levels/Prefabs/UI";

        [MenuItem("ROM/UI/Construir e instalar la UI")]
        public static void BuildAndInstall()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string previousScene = SceneManager.GetActiveScene().path;

            EnsurePrefabs(false);
            InstallInHub();
            InstallInHordeMap();
            int fonts = ApplyKitFonts();

            if (!string.IsNullOrEmpty(previousScene))
                EditorSceneManager.OpenScene(previousScene);

            EditorUtility.DisplayDialog("ROM UI",
                "Listo.\n\n" +
                "· Hub: ROM_MainMenu y ROM_LoadingScreen en la Hierarchy; la UI vieja del kit queda con sus Canvas desactivados.\n" +
                "· Mapa Horda: HordeHUD en la Hierarchy y asignado al gamemode.\n" +
                $"· Fuentes ROM aplicadas en {fonts} textos de los prefabs de UI del kit.\n\n" +
                "Los prefabs están en Assets/ROM/UI.", "OK");
        }

        [MenuItem("ROM/UI/Regenerar prefabs (pisa los cambios hechos a mano)")]
        public static void RegeneratePrefabs()
        {
            if (!EditorUtility.DisplayDialog("ROM UI",
                    "Esto vuelve a generar ROM_MainMenu, ROM_LoadingScreen y HordeHUD desde cero.\n" +
                    "Se pierden los cambios hechos a mano en esos prefabs.", "Regenerar", "Cancelar"))
                return;

            EnsurePrefabs(true);
        }

        public static void EnsurePrefabs(bool overwrite)
        {
            EnsureFolder(UIRoot);

            ROMUITheme theme = ROMUITheme.Load();

            if (overwrite || !File.Exists(MenuPrefabPath))
                SavePrefab(BuildMainMenu(theme), MenuPrefabPath);

            if (overwrite || !File.Exists(LoadingPrefabPath))
                SavePrefab(BuildLoadingScreen(theme), LoadingPrefabPath);

            if (overwrite || !File.Exists(HordeHUDPrefabPath))
                SavePrefab(BuildHordeHUD(EnsureHordeTheme()), HordeHUDPrefabPath);

            AssetDatabase.SaveAssets();
        }

        public static GameObject HordeHUDPrefab()
        {
            EnsurePrefabs(false);
            return AssetDatabase.LoadAssetAtPath<GameObject>(HordeHUDPrefabPath);
        }

        #region menú principal
        static GameObject BuildMainMenu(ROMUITheme t)
        {
            GameObject go = new GameObject("ROM_MainMenu", typeof(RectTransform));
            F.Canvas(go, 10, true);
            RectTransform root = (RectTransform)go.transform;
            ROMMainMenu menu = go.AddComponent<ROMMainMenu>();
            F.Wire(menu, "_theme", t);

            //fondo: cielo de la grieta, oscurecido
            RectTransform background = F.Rect(root, "Background", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            F.Stretch(background);
            F.Fill(background, "Black", null, Color.black);
            F.Cover(background, "Sky", t.MenuBackground, Color.white);
            F.Fill(background, "Darken", null, new Color(0.02f, 0.01f, 0.05f, 0.45f));
            F.Img(background, "BottomShade", t.Shade, new Color(0, 0, 0, 0.85f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(4000, 300));

            //marca
            RectTransform brand = F.Rect(root, "Brand", new Vector2(0, 1), Vector2.zero, new Vector2(1300, 260));
            F.Img(brand, "Logo", t.Logo, Color.white, new Vector2(0, 1), new Vector2(70, -30), new Vector2(250, 250), true);
            F.Shadow(F.Txt(brand, "Title", t.Title, 96, t.Text, TextAnchor.LowerLeft, new Vector2(0, 1), new Vector2(330, -70), new Vector2(900, 110), "RIFTBREAKERS"), 2f);
            F.Txt(brand, "Subtitle", t.Body, 20, t.Accent, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(336, -182), new Vector2(900, 30), "REALITY OF MADNESS  ·  THE BATTLE FOR ZHION");
            F.Img(brand, "Accent", t.AccentBar, t.Accent, new Vector2(0, 1), new Vector2(336, -222), new Vector2(320, 4));

            BuildOperator(root, t, menu);
            BuildCreate(root, t, menu);
            BuildJoin(root, t, menu);

            RectTransform footer = F.Rect(root, "Footer", new Vector2(0, 0), Vector2.zero, new Vector2(1920, 140));
            F.Txt(footer, "Controls", t.Body, 15, t.Muted, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(70, 34), new Vector2(1300, 24),
                "WASD  MOVERSE     SHIFT  CORRER     CTRL  AGACHARSE     E  AGARRAR     G  SOLTAR     R  RECARGAR     1 · 2  ARMAS     ESC  PAUSA");
            F.Img(root, "StudioLogo", t.StudioLogo, new Color(1, 1, 1, 0.55f), new Vector2(1, 0), new Vector2(-70, 16), new Vector2(90, 90), true);

            return go;
        }

        static void BuildOperator(RectTransform root, ROMUITheme t, ROMMainMenu menu)
        {
            RectTransform card = F.Card(root, t, "Card_Operador", "OPERADOR", new Vector2(0, 1), new Vector2(70, -310), new Vector2(560, 470));

            RectTransform frame = F.Rect(card, "Portrait", new Vector2(0, 1), new Vector2(0, -6), new Vector2(190, 190));
            frame.gameObject.AddComponent<Image>().color = new Color(t.Accent.r, t.Accent.g, t.Accent.b, 0.25f);
            Outline outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = t.Accent;
            outline.effectDistance = new Vector2(2, -2);

            F.Wire(menu, "_portraitFallback", F.Img(frame, "Emblem", t.Emblem, new Color(1, 1, 1, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 110), true));
            F.Wire(menu, "_portrait", F.Img(frame, "Art", null, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(186, 186), true));

            const float x = 214f;
            const float width = 290f;

            F.Label(card, t, "Nombre de operador", new Vector2(x, -6), width);
            InputField username = F.Input(card, t, "Username", "Escribí tu nombre", new Vector2(x, -32), new Vector2(width, 46));
            username.characterLimit = 20;
            F.Wire(menu, "_username", username);

            F.Label(card, t, "Apariencia", new Vector2(x, -100), width);
            F.Wire(menu, "_skin", F.Selector(card, t, "Skin", new Vector2(x, -126), new Vector2(width, 54)));

            F.Txt(card, "Hint", t.Body, 15, t.Muted, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, -216), new Vector2(504, 60),
                "Tu apariencia y nombre se guardan para las próximas partidas.");

            F.Label(card, t, "Apariencia de armas", new Vector2(0, -262), 504);

            //acá el instalador mueve la grilla de apariencias de armas del kit
            F.Rect(card, "WeaponSkins", new Vector2(0, 1), new Vector2(0, -290), new Vector2(504, 110));
        }

        static void BuildCreate(RectTransform root, ROMUITheme t, ROMMainMenu menu)
        {
            const float cardWidth = 600f;
            RectTransform card = F.Card(root, t, "Card_CrearPartida", "CREAR PARTIDA", new Vector2(1, 1), new Vector2(-70, -60), new Vector2(cardWidth, 600));
            float full = cardWidth - 56f;
            float half = (full - 20f) * 0.5f;

            F.Label(card, t, "Mapa", new Vector2(0, -6), full);
            F.Wire(menu, "_mapIndex", F.Txt(card, "MapIndex", t.Body, 15, t.Muted, TextAnchor.LowerRight, new Vector2(0, 1), new Vector2(0, -6), new Vector2(full, 20)));
            F.Wire(menu, "_map", F.Selector(card, t, "Map", new Vector2(0, -32), new Vector2(full, 62), 36));

            F.Label(card, t, "Modo de juego", new Vector2(0, -112), full);
            F.Wire(menu, "_mode", F.Selector(card, t, "Mode", new Vector2(0, -138), new Vector2(full, 54)));
            F.Wire(menu, "_modeDescription", F.Txt(card, "ModeDescription", t.Body, 16, t.Muted, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, -200), new Vector2(full, 44)));

            F.Label(card, t, "Jugadores", new Vector2(0, -256), half);
            F.Wire(menu, "_players", F.Selector(card, t, "Players", new Vector2(0, -282), new Vector2(half, 50)));

            F.Label(card, t, "Duración", new Vector2(half + 20, -256), half);
            F.Wire(menu, "_duration", F.Selector(card, t, "Duration", new Vector2(half + 20, -282), new Vector2(half, 50)));

            F.Wire(menu, "_bots", F.Toggle(card, t, "Bots", "Completar con bots", new Vector2(0, -352), half));

            F.Label(card, t, "Puerto", new Vector2(half + 20, -340), half);
            InputField port = F.Input(card, t, "HostPort", "7777", new Vector2(half + 20, -364), new Vector2(half, 40));
            port.contentType = InputField.ContentType.IntegerNumber;
            F.Wire(menu, "_hostPort", port);

            F.Wire(menu, "_host", F.Button(card, t, "Host", "Iniciar partida", true, new Vector2(0, 1), new Vector2(0, -428), new Vector2(full, 64)));
            F.Wire(menu, "_server", F.Button(card, t, "DedicatedServer", "Servidor dedicado", false, new Vector2(0, 1), new Vector2(0, -504), new Vector2(full, 40)));
        }

        static void BuildJoin(RectTransform root, ROMUITheme t, ROMMainMenu menu)
        {
            const float cardWidth = 600f;
            RectTransform card = F.Card(root, t, "Card_Unirse", "UNIRSE A UNA PARTIDA", new Vector2(1, 1), new Vector2(-70, -690), new Vector2(cardWidth, 250));
            float full = cardWidth - 56f;
            float addressWidth = full - 180f;

            F.Label(card, t, "Dirección", new Vector2(0, -6), addressWidth);
            F.Wire(menu, "_address", F.Input(card, t, "Address", "localhost", new Vector2(0, -30), new Vector2(addressWidth - 20, 44)));

            F.Label(card, t, "Puerto", new Vector2(addressWidth, -6), 180);
            InputField port = F.Input(card, t, "Port", "7777", new Vector2(addressWidth, -30), new Vector2(180, 44));
            port.contentType = InputField.ContentType.IntegerNumber;
            F.Wire(menu, "_connectPort", port);

            F.Wire(menu, "_connect", F.Button(card, t, "Connect", "Conectar", true, new Vector2(0, 1), new Vector2(0, -96), new Vector2(full, 54)));
        }
        #endregion

        #region pantalla de carga
        static GameObject BuildLoadingScreen(ROMUITheme t)
        {
            GameObject go = new GameObject("ROM_LoadingScreen", typeof(RectTransform));
            F.Canvas(go, 1000, true);
            ROMLoadingScreen screen = go.AddComponent<ROMLoadingScreen>();

            CanvasGroup group = F.Group(go.transform, "Root", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform root = (RectTransform)group.transform;
            F.Stretch(root);
            F.Wire(screen, "_group", group);

            //negro primero: si no hay arte, igual tapa el corte
            F.Fill(root, "Black", null, Color.black).raycastTarget = true;
            F.Cover(root, "Art", t.LoadingBackground, new Color(0.75f, 0.7f, 0.8f, 1f));
            F.Fill(root, "Shade", t.Shade, new Color(0, 0, 0, 0.75f));
            F.Img(root, "BottomShade", null, new Color(0, 0, 0, 0.7f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(4000, 260));

            F.Img(root, "Logo", t.Logo, Color.white, new Vector2(0f, 1f), new Vector2(90, -70), new Vector2(330, 330), true);

            Text title = F.Txt(root, "Title", t.Title, 120, t.Text, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(110, 330), new Vector2(1500, 140), "CARGANDO");
            F.Shadow(title, 2f);
            F.Wire(screen, "_title", title);
            F.Wire(screen, "_subtitle", F.Txt(root, "Subtitle", t.Body, 26, t.Accent, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(116, 290), new Vector2(1500, 40), "PLATAFORMA DE LA GRIETA"));
            F.Img(root, "Accent", t.AccentBar, t.Accent, new Vector2(0f, 0f), new Vector2(116, 270), new Vector2(380, 4));

            F.Txt(root, "TipLabel", t.Body, 15, t.Muted, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(116, 150), new Vector2(1200, 20), "CONSEJO");
            F.Wire(screen, "_tip", F.Txt(root, "Tip", t.Body, 24, t.Text, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(116, 112), new Vector2(1200, 36), "Las armas pesadas esperan en el centro de la plataforma."));

            const float barWidth = 1920f - 232f;
            F.Img(root, "BarBackground", null, new Color(1, 1, 1, 0.12f), new Vector2(0f, 0f), new Vector2(116, 60), new Vector2(barWidth, 4));
            F.Wire(screen, "_bar", F.Img(root, "Bar", t.AccentBar, t.Accent, new Vector2(0f, 0f), new Vector2(116, 60), new Vector2(barWidth, 4)).rectTransform);

            F.Wire(screen, "_percent", F.Txt(root, "Percent", t.Title, 34, t.Text, TextAnchor.MiddleRight, new Vector2(1f, 0f), new Vector2(-200, 100), new Vector2(200, 50), "100%"));

            if (t.Spinner)
            {
                RectTransform spinner = F.Img(root, "Spinner", t.Spinner, Color.white, new Vector2(1f, 0f), Vector2.zero, new Vector2(64, 64), true).rectTransform;
                spinner.pivot = new Vector2(0.5f, 0.5f);
                spinner.anchoredPosition = new Vector2(-148, 124);
                F.Wire(screen, "_spinner", spinner);
            }

            F.Img(root, "StudioLogo", t.StudioLogo, new Color(1, 1, 1, 0.5f), new Vector2(1f, 1f), new Vector2(-70, -60), new Vector2(150, 150), true);

            //guardada invisible para no tapar el menú mientras se edita el hub;
            //para verla, subir el Alpha del CanvasGroup de Root
            group.alpha = 0f;

            return go;
        }
        #endregion

        #region HUD de la Horda
        static GameObject BuildHordeHUD(HordeHUDTheme t)
        {
            Font title = t.TitleFont ? t.TitleFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Font body = t.BodyFont ? t.BodyFont : title;

            GameObject go = new GameObject("HordeHUD", typeof(RectTransform));
            F.Canvas(go, 5, false);
            RectTransform root = (RectTransform)go.transform;
            HordeHUD hud = go.AddComponent<HordeHUD>();
            F.Wire(hud, "_theme", t);

            //estado (esquina superior izquierda)
            CanvasGroup status = F.Group(root, "Status", new Vector2(0, 1), new Vector2(36, -30), new Vector2(420, 150));
            F.Wire(hud, "_status", status);
            F.Img(status.transform, "WaveBadge", t.WaveBadge, t.Accent, new Vector2(0, 1), Vector2.zero, new Vector2(84, 96), true);
            F.Wire(hud, "_waveNumber", Shadowed(F.Txt(status.transform, "WaveNumber", title, 50, t.Text, TextAnchor.MiddleCenter, new Vector2(0, 1), Vector2.zero, new Vector2(84, 96), "7")));
            F.Wire(hud, "_statusLabel", Shadowed(F.Txt(status.transform, "Label", body, 15, t.Muted, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(104, -8), new Vector2(300, 22), "ENEMIGOS RESTANTES")));
            F.Wire(hud, "_statusIcon", F.Img(status.transform, "EnemyIcon", t.EnemyIcon, t.Text, new Vector2(0, 1), new Vector2(104, -36), new Vector2(24, 28), true));
            F.Wire(hud, "_statusValue", Shadowed(F.Txt(status.transform, "Value", title, 38, t.Text, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(136, -28), new Vector2(280, 44), "12")));
            F.Img(status.transform, "BarBackground", null, new Color(1, 1, 1, 0.15f), new Vector2(0, 1), new Vector2(104, -78), new Vector2(200, 3));
            F.Wire(hud, "_statusBar", F.Img(status.transform, "Bar", t.AccentBar, t.Accent, new Vector2(0, 1), new Vector2(104, -78), new Vector2(200, 3)).rectTransform);
            F.Img(status.transform, "LivesIcon", t.LivesIcon, t.Good, new Vector2(0, 1), new Vector2(4, -112), new Vector2(18, 18), true);
            F.Wire(hud, "_lives", Shadowed(F.Txt(status.transform, "Lives", body, 18, t.Good, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(30, -108), new Vector2(160, 26), "3  RESERVAS")));
            F.Wire(hud, "_modifier", Shadowed(F.Txt(status.transform, "Modifier", body, 16, t.Accent, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(150, -108), new Vector2(300, 26), "FRENESÍ")));

            //aviso central
            CanvasGroup banner = F.Group(root, "Banner", new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(1000, 170));
            F.Wire(hud, "_banner", banner);
            F.Wire(hud, "_bannerContent", banner.transform);
            F.Img(banner.transform, "Background", t.BannerBackground, new Color(0, 0, 0, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1000, 120));
            F.Wire(hud, "_bannerEmblem", F.Img(banner.transform, "Emblem", t.Emblem, t.Danger, new Vector2(0.5f, 1f), new Vector2(0, 40), new Vector2(70, 56), true));
            F.Wire(hud, "_bannerTitle", Shadowed(F.Txt(banner.transform, "Title", title, 76, t.Text, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(1000, 90), "OLEADA 7")));
            F.Wire(hud, "_bannerAccent", F.Img(banner.transform, "Accent", t.AccentBar, t.Accent, new Vector2(0.5f, 0.5f), new Vector2(0, -36), new Vector2(420, 4)));
            F.Wire(hud, "_bannerSubtitle", Shadowed(F.Txt(banner.transform, "Subtitle", body, 22, t.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1000, 30), "LA GRIETA SE ABRE")));

            //avisos del jugador
            CanvasGroup protection = F.Group(root, "SpawnProtection", new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(400, 40));
            F.Wire(hud, "_protection", protection);
            Shadowed(F.Txt(protection.transform, "Label", body, 16, t.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(400, 24), "PROTECCIÓN DE APARICIÓN"));
            F.Img(protection.transform, "BarBackground", null, new Color(1, 1, 1, 0.15f), new Vector2(0.5f, 0f), new Vector2(0, 6), new Vector2(260, 3));
            RectTransform protectionBar = F.Img(protection.transform, "Bar", t.AccentBar, t.Accent, new Vector2(0, 0), new Vector2(70, 6), new Vector2(260, 3)).rectTransform;
            F.Wire(hud, "_protectionBar", protectionBar);

            F.Wire(hud, "_outOfLives", Shadowed(F.Txt(root, "OutOfLives", body, 22, t.Danger, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 240), new Vector2(1200, 34),
                "SIN RESERVAS  ·  AGUANTÁ HASTA QUE TU ESCUADRA SUPERE LA OLEADA")));

            //intro (va al final para quedar arriba de todo)
            BuildHordeIntro(root, t, hud, title, body);

            return go;
        }

        static void BuildHordeIntro(RectTransform root, HordeHUDTheme t, HordeHUD hud, Font title, Font body)
        {
            CanvasGroup intro = F.Group(root, "Intro", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform rect = (RectTransform)intro.transform;
            F.Stretch(rect);
            F.Wire(hud, "_intro", intro);

            F.Fill(rect, "Shade", null, new Color(0.02f, 0.01f, 0.04f, 0.72f));
            F.Img(rect, "BottomStrip", t.BannerBackground, new Color(0, 0, 0, 0.7f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920, 230));
            F.Img(rect, "Logo", t.Logo, new Color(1, 1, 1, 0.55f), new Vector2(1f, 1f), new Vector2(-70, -60), new Vector2(240, 190), true);

            RectTransform block = F.Rect(rect, "Block", new Vector2(0f, 0.5f), new Vector2(150, 90), new Vector2(1100, 520));
            F.Img(block, "Frame", t.FrameCorners, new Color(1, 1, 1, 0.25f), new Vector2(0, 1), new Vector2(-50, 40), new Vector2(1180, 400));
            F.Img(block, "ModeIcon", t.ModeIcon, t.Accent, new Vector2(0, 1), Vector2.zero, new Vector2(44, 44), true);
            F.Txt(block, "Mode", body, 22, t.Accent, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(60, 0), new Vector2(800, 44), "MODO  ·  HORDA COOPERATIVA");
            F.Wire(hud, "_introMapName", Shadowed(F.Txt(block, "MapName", title, 120, t.Text, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(-4, -50), new Vector2(1400, 140), "PLATAFORMA DE LA GRIETA")));
            F.Img(block, "Accent", t.AccentBar, t.Accent, new Vector2(0, 1), new Vector2(0, -196), new Vector2(560, 6));
            F.Txt(block, "ObjectiveLabel", body, 18, t.Muted, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, -226), new Vector2(900, 26), "OBJETIVO");
            F.Txt(block, "Objective", body, 28, t.Text, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, -256), new Vector2(1100, 40),
                "Sobreviví a la grieta. El arsenal está en el centro de la plataforma.");
            F.Txt(block, "Rules", body, 20, t.Muted, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, -320), new Vector2(1100, 140),
                "—  Las primeras oleadas vienen a cuchillo: no las dejes acercarse.\n" +
                "—  Reaparecer durante una oleada consume una reserva de la escuadra.\n" +
                "—  Cada oleada superada: +1 reserva, los caídos regresan y el arsenal se repone.\n" +
                "—  Al aparecer tenés unos segundos de protección.");

            RectTransform squad = F.Rect(rect, "Squad", new Vector2(1f, 0.5f), new Vector2(-150, 60), new Vector2(360, 300));
            F.Txt(squad, "Label", body, 18, t.Muted, TextAnchor.UpperRight, new Vector2(1, 1), Vector2.zero, new Vector2(360, 26), "ESCUADRA");
            F.Img(squad, "Accent", t.AccentBar, t.Accent, new Vector2(1, 1), new Vector2(0, -32), new Vector2(200, 3));
            Text names = F.Txt(squad, "Names", title, 34, t.Text, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(0, -46), new Vector2(360, 260), "JUGADOR");
            names.lineSpacing = 0.9f;
            F.Wire(hud, "_introSquad", names);

            F.Wire(hud, "_introCountdownLabel", F.Txt(rect, "CountdownLabel", body, 20, t.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 186), new Vector2(900, 30), "LA PRIMERA OLEADA COMIENZA EN"));
            F.Wire(hud, "_introCountdown", Shadowed(F.Txt(rect, "Countdown", title, 84, t.Text, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 76), new Vector2(600, 100), "0:15")));
            F.Img(rect, "BarBackground", null, new Color(1, 1, 1, 0.15f), new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(620, 4));
            RectTransform bar = F.Img(rect, "Bar", t.AccentBar, t.Accent, new Vector2(0.5f, 0f), new Vector2(-310, 52), new Vector2(620, 4)).rectTransform;
            bar.pivot = new Vector2(0f, 0f);
            F.Wire(hud, "_introBar", bar);
        }

        static Text Shadowed(Text text)
        {
            F.Shadow(text);
            return text;
        }

        /// <summary>
        /// Tema del HUD de la Horda con el arte de UI del proyecto. Si ya existe se
        /// respeta tal cual.
        /// </summary>
        public static HordeHUDTheme EnsureHordeTheme()
        {
            HordeHUDTheme theme = AssetDatabase.LoadAssetAtPath<HordeHUDTheme>(HordeThemePath);
            if (theme)
                return theme;

            EnsureFolder(Path.GetDirectoryName(HordeThemePath).Replace('\\', '/'));

            const string ui = "Assets/IDS ASSETS/UI NEW";

            theme = ScriptableObject.CreateInstance<HordeHUDTheme>();
            theme.TitleFont = AssetDatabase.LoadAssetAtPath<Font>($"{ui}/OdibeeSans-Regular.ttf");
            theme.BodyFont = AssetDatabase.LoadAssetAtPath<Font>($"{ui}/Inter-VariableFont_slnt,wght.ttf");
            theme.WaveBadge = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/Group 11.png");
            theme.EnemyIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/Vector (2).png");
            theme.LivesIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/Vector.png");
            theme.ModeIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/Group 18.png");
            theme.Emblem = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/Untitled-4 1.png");
            theme.DeathEmblem = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/Group 61.png");
            theme.Logo = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/tbfz white 1.png");
            theme.AccentBar = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/Rectangle 18.png");
            theme.BannerBackground = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/Rectangle 19.png");
            theme.FrameCorners = AssetDatabase.LoadAssetAtPath<Sprite>($"{ui}/Group 24.png");

            AssetDatabase.CreateAsset(theme, HordeThemePath);
            return theme;
        }
        #endregion

        #region instalación en escenas
        static void InstallInHub()
        {
            Scene hub = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);

            ROMMainMenu menu = FindInScene<ROMMainMenu>(hub) ?? Instantiate<ROMMainMenu>(MenuPrefabPath, hub);
            if (!FindInScene<ROMLoadingScreen>(hub))
                Instantiate<ROMLoadingScreen>(LoadingPrefabPath, hub);

            RoomCreator creator = FindInScene<RoomCreator>(hub);
            RoomConnector connector = FindInScene<RoomConnector>(hub);
            UIUsernameSet account = FindInScene<UIUsernameSet>(hub);

            F.Wire(menu, "_creator", creator);
            F.Wire(menu, "_connector", connector);

            if (account)
            {
                SerializedObject accountSo = new SerializedObject(account);
                F.Wire(menu, "_kitUsername", accountSo.FindProperty("UsernameField").objectReferenceValue);
                F.Wire(menu, "_kitSkin", accountSo.FindProperty("CharacterSkinSelector").objectReferenceValue);
            }

            //la UI vieja sigue viva para que sus scripts funcionen, pero sin dibujarse ni recibir clics
            foreach (Component kit in new Component[] { creator, connector, account })
            {
                if (!kit) continue;

                Canvas canvas = kit.GetComponentInParent<Canvas>(true);
                if (!canvas) continue;

                canvas = canvas.rootCanvas;
                canvas.enabled = false;
                EditorUtility.SetDirty(canvas);

                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster)
                {
                    raycaster.enabled = false;
                    EditorUtility.SetDirty(raycaster);
                }
            }

            MoveWeaponSkinGrid(hub, menu);

            EditorSceneManager.MarkSceneDirty(hub);
            EditorSceneManager.SaveScene(hub);
        }

        /// <summary>
        /// Las apariencias de armas las genera el kit en runtime dentro de una grilla
        /// propia, a partir de un elemento plantilla. Movemos la grilla a la tarjeta
        /// del operador y le damos la paleta de ROM a la plantilla, así los elementos
        /// que genere el kit ya salen con el estilo nuevo.
        /// </summary>
        static void MoveWeaponSkinGrid(Scene hub, ROMMainMenu menu)
        {
            UISkinSelector skins = FindInScene<UISkinSelector>(hub);
            if (!skins)
                return;

            SerializedObject so = new SerializedObject(skins);
            Transform grid = so.FindProperty("_itemSkinSelectorGridParent").objectReferenceValue as Transform;
            GameObject template = so.FindProperty("_itemSelectorPrefab").objectReferenceValue as GameObject;

            Transform slot = menu.transform.Find("Card_Operador/Content/WeaponSkins");

            if (grid && slot && grid.parent != slot)
            {
                RectTransform rect = (RectTransform)grid;
                rect.SetParent(slot, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one * 1.25f;
                EditorUtility.SetDirty(rect);
            }

            if (template)
                RestyleKitControls(template.transform);
        }

        static void RestyleKitControls(Transform root)
        {
            ROMUITheme t = ROMUITheme.Load();

            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                text.font = t.Body;
                text.color = t.Text;
                EditorUtility.SetDirty(text);
            }

            foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable.targetGraphic is Image image)
                {
                    image.sprite = null;
                    image.color = Color.white;
                    EditorUtility.SetDirty(image);
                }

                selectable.colors = F.Colors(t.Field, new Color(t.Accent.r, t.Accent.g, t.Accent.b, 0.35f));
                EditorUtility.SetDirty(selectable);
            }

            foreach (Dropdown dropdown in root.GetComponentsInChildren<Dropdown>(true))
            {
                if (dropdown.template && dropdown.template.TryGetComponent(out Image templateImage))
                {
                    templateImage.sprite = null;
                    templateImage.color = new Color(0.06f, 0.03f, 0.1f, 0.97f);
                    EditorUtility.SetDirty(templateImage);
                }
            }
        }

        static void InstallInHordeMap()
        {
            if (!File.Exists(HordeScenePath))
                return;

            Scene scene = EditorSceneManager.OpenScene(HordeScenePath, OpenSceneMode.Single);
            Horde.Horde horde = FindInScene<Horde.Horde>(scene);

            if (!horde)
                return;

            HordeHUD hud = FindInScene<HordeHUD>(scene) ?? Instantiate<HordeHUD>(HordeHUDPrefabPath, scene);
            F.Wire(horde, "_hud", hud);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Instancia el HUD de la Horda en la escena abierta y lo asigna al gamemode.
        /// Lo usa el generador del mapa.
        /// </summary>
        public static void InstallHordeHUD(Horde.Horde horde)
        {
            GameObject prefab = HordeHUDPrefab();
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, horde.gameObject.scene);
            F.Wire(horde, "_hud", instance.GetComponent<HordeHUD>());
        }
        #endregion

        #region fuentes del kit
        /// <summary>
        /// Pasa los prefabs de UI del kit a la tipografía de ROM: Orbitron Black a la
        /// fuente de títulos; Orbitron Light y Arial a la de cuerpo. El resto de las
        /// fuentes no se toca. Devuelve cuántos textos cambió.
        /// </summary>
        [MenuItem("ROM/UI/Aplicar fuentes ROM a la UI del kit")]
        public static int ApplyKitFonts()
        {
            ROMUITheme t = ROMUITheme.Load();
            if (!t.TitleFont && !t.BodyFont)
                return 0;

            int changed = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { KitUIPrefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                int before = changed;

                foreach (Text text in contents.GetComponentsInChildren<Text>(true))
                {
                    if (!text.font)
                        continue;

                    Font replacement = KitFontReplacement(text.font.name, t);
                    if (replacement && replacement != text.font)
                    {
                        text.font = replacement;
                        changed++;
                    }
                }

                if (changed != before)
                    PrefabUtility.SaveAsPrefabAsset(contents, path);

                PrefabUtility.UnloadPrefabContents(contents);
            }

            return changed;
        }

        static Font KitFontReplacement(string fontName, ROMUITheme t)
        {
            if (fontName.Contains("Orbitron Black"))
                return t.Title;

            if (fontName.Contains("Orbitron") || fontName == "Arial" || fontName == "LegacyRuntime")
                return t.Body;

            return null;
        }
        #endregion

        #region helpers
        static T FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }

        static T Instantiate<T>(string prefabPath, Scene scene) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            return instance.GetComponent<T>();
        }

        static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        #endregion
    }
}
