using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MultiFPS.UI.HUD
{
// Presentation only. The existing game mode owns death, control and respawn timing.
public class UIRespawnCooldown : CharacterHud
{
    [SerializeField] Image _bar;
    [SerializeField] Text _msg;
    [Header("Death screen presentation")]
    [SerializeField] Font titleFont;
    [SerializeField] string title = "HAS CAÍDO";
    [SerializeField] string defeatedLabel = "Te derrotó";
    [SerializeField] string unknownAttacker = "El entorno";
    [SerializeField] string countdownFormat = "Reaparecés en {0} s";
    [SerializeField] Color accent = new Color(.66f, .36f, .94f, 1);
    [SerializeField] Vector2 contentSize = new Vector2(680, 360);
    Coroutine countdown;
    RectTransform content;
    Text enemyName;
    bool built;
    Sprite shardSprite, vignetteSprite;
    public float Remaining { get; private set; }
    public string DefeatedBy { get; private set; }
    protected override void Awake() { base.Awake(); gameObject.SetActive(false); }
    protected override void AssignOwnedCharacterForUI(CharacterInstance actor)
    {
        HideUI(); actor.Health.Client_OnHealthDepleted += OnOwnedCharacterDied;
        actor.Health.Client_Resurrect += OnResurrect;
    }
    protected override void DeassignCurrentlyOwnedCharacterFromUI(CharacterInstance actor)
    {
        actor.Health.Client_OnHealthDepleted -= OnOwnedCharacterDied;
        actor.Health.Client_Resurrect -= OnResurrect; HideUI();
    }
    public static bool UsesUnifiedPresentation(CharacterInstance character)
    {
        foreach (var screen in FindObjectsByType<UIRespawnCooldown>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (screen._myOwnedCharacter == character) return true;
        return false;
    }
    void OnResurrect(int health) { HideUI(); }
    void OnOwnedCharacterDied(CharacterPart part, byte attacker)
    {
        var killer = GameSync.Singleton ? GameSync.Singleton.Healths.GetObj(attacker) : null;
        DefeatedBy = killer && killer != _myOwnedCharacter.Health && !string.IsNullOrWhiteSpace(killer.CharacterName)
            ? killer.CharacterName.Trim() : unknownAttacker;
        if (GameManager.Gamemode.LetPlayersSpawnOnTheirOwn) StartUI(RoomSetup.Properties.P_RespawnCooldown);
    }
    RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
        r.anchoredPosition = position; r.sizeDelta = size; return r;
    }
    Image Image(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false; return image;
    }
    Text Label(string name, string value, Font font, int size, Vector2 position, Vector2 dimensions)
    {
        var text = Rect(content, name, position, dimensions).gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = size; text.text = value; text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(.96f, .94f, .97f); text.raycastTarget = false; text.supportRichText = false;
        return text;
    }
    void Build()
    {
        if (built) return; built = true;
        Font bodyFont = _msg && _msg.font ? _msg.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (Transform child in transform) child.gameObject.SetActive(false);
        var root = (RectTransform)transform; var canvas = GetComponentInParent<Canvas>();
        if (canvas) root.SetParent(canvas.transform, false);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
        root.localScale = Vector3.one;
        var oldSurface = GetComponent<Image>(); if (oldSurface) { oldSurface.enabled = false; oldSurface.raycastTarget = false; }
        var backdrop = Rect(root, "Death soft vignette", Vector2.zero, Vector2.zero);
        backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        var shade = backdrop.gameObject.AddComponent<Image>(); vignetteSprite = MakeDecoration(false); shade.sprite = vignetteSprite; shade.raycastTarget = false;
        content = Rect(root, "Death presentation", Vector2.zero, contentSize);
        content.anchorMin = content.anchorMax = new Vector2(.5f, .66f);
        var shard = Rect(content, "Rift shard", new Vector2(0, 128), new Vector2(40, 74)).gameObject.AddComponent<Image>();
        shardSprite = MakeDecoration(true); shard.sprite = shardSprite; shard.color = accent; shard.raycastTarget = false;
        Image(content, "Left ornament", new Vector2(-112, 127), new Vector2(150, 1), new Color(.65f, .46f, .77f, .46f));
        Image(content, "Right ornament", new Vector2(112, 127), new Vector2(150, 1), new Color(.65f, .46f, .77f, .46f));
        Label("Death title", string.Join(" ", title.ToCharArray()), titleFont ? titleFont : bodyFont, 54, new Vector2(0, 43), new Vector2(660, 76));
        var caption = Label("Defeated caption", defeatedLabel, titleFont ? titleFont : bodyFont, 25, new Vector2(0, -12), new Vector2(600, 34));
        caption.color = new Color(.76f, .73f, .78f);
        enemyName = Label("Defeated by", "", titleFont ? titleFont : bodyFont, 36, new Vector2(0, -58), new Vector2(650, 63));
        enemyName.resizeTextForBestFit = true; enemyName.resizeTextMinSize = 22; enemyName.resizeTextMaxSize = 36;
        enemyName.horizontalOverflow = HorizontalWrapMode.Wrap; enemyName.verticalOverflow = VerticalWrapMode.Truncate;
        _msg = Label("Respawn countdown", "", bodyFont, 23, new Vector2(0, -122), new Vector2(650, 34));
        var track = Image(content, "Respawn indicator track", new Vector2(0, -162), new Vector2(400, 7), new Color(.18f, .14f, .24f, .9f));
        _bar = Image(track.transform, "Respawn indicator", Vector2.zero, new Vector2(400, 7), accent);
        _bar.rectTransform.anchorMin = Vector2.zero; _bar.rectTransform.anchorMax = Vector2.one;
        _bar.rectTransform.offsetMin = _bar.rectTransform.offsetMax = Vector2.zero;
        var group = GetComponent<CanvasGroup>(); if (!group) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1; group.blocksRaycasts = group.interactable = false;
        Fit();
    }
    Sprite MakeDecoration(bool shard)
    {
        const int width = 96, height = 128;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = shard ? "Rift death shard" : "Rift death vignette";
        texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            float u = (x + .5f) / width, v = (y + .5f) / height;
            Color pixel;
            if (shard)
            {
                float edge = 1 - Mathf.Abs(u * 2 - 1) - Mathf.Abs(v * 2 - 1);
                float alpha = Mathf.Clamp01(edge * 70);
                if (Mathf.Abs(u - .5f) < .025f || Mathf.Abs(v - .5f) < .018f) alpha = 0;
                float facet = u < .5f ? (v > .5f ? 1 : .7f) : (v > .5f ? .63f : .48f);
                pixel = new Color(facet, facet, 1, alpha);
            }
            else
            {
                float edge = Mathf.Clamp01(new Vector2((u - .5f) * 1.25f, v - .5f).magnitude);
                float cloud = Mathf.Exp(-((u - .5f) * (u - .5f) * 13 + (v - .66f) * (v - .66f) * 32));
                pixel = new Color(.025f + cloud * .025f, .014f, .045f + cloud * .045f, .28f + edge * .33f + cloud * .2f);
            }
            texture.SetPixel(x, y, pixel);
        }
        texture.Apply(false, true);
        return Sprite.Create(texture, new UnityEngine.Rect(0, 0, width, height), Vector2.one * .5f, 100);
    }
    protected override void OnDestroy()
    {
        if (_myOwnedCharacter) DeassignCurrentlyOwnedCharacterFromUI(_myOwnedCharacter);
        foreach (var sprite in new[] { shardSprite, vignetteSprite }) if (sprite) { Destroy(sprite.texture); Destroy(sprite); }
        base.OnDestroy();
    }
    void Fit()
    {
        if (!content) return;
        var root = (RectTransform)transform;
        float scale = Mathf.Min(1, root.rect.width / (contentSize.x + 48), root.rect.height / 600);
        content.localScale = Vector3.one * Mathf.Max(.3f, scale);
    }
    void LateUpdate() { Fit(); }
    void Draw(float duration)
    {
        _msg.text = string.Format(countdownFormat, Mathf.CeilToInt(Remaining));
        float ratio = duration > 0 ? Mathf.Clamp01(Remaining / duration) : 0;
        _bar.fillAmount = ratio; _bar.rectTransform.anchorMax = new Vector2(ratio, 1); _bar.gameObject.SetActive(ratio > 0);
    }
    public void StartUI(float cooldown)
    {
        HideUI(); if (float.IsNaN(cooldown) || float.IsInfinity(cooldown) || cooldown <= 0) return;
        Build(); enemyName.text = string.IsNullOrWhiteSpace(DefeatedBy) ? unknownAttacker : DefeatedBy;
        Remaining = cooldown; gameObject.SetActive(true); Draw(cooldown); countdown = StartCoroutine(Count(cooldown));
    }
    IEnumerator Count(float duration)
    {
        float started = Time.time;
        while ((Remaining = Mathf.Max(0, duration - (Time.time - started))) > 0) { Draw(duration); yield return null; }
        Remaining = 0; Draw(duration); countdown = null; gameObject.SetActive(false);
    }
    public void HideUI()
    {
        if (countdown != null) { StopCoroutine(countdown); countdown = null; }
        Remaining = 0; if (_bar) _bar.gameObject.SetActive(false); gameObject.SetActive(false);
    }
}

}
