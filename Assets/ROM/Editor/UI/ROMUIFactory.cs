using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ROM.UI.EditorTools
{
    /// <summary>
    /// Piezas de UI con el estilo de ROM, para armar prefabs desde el editor
    /// (ROMUIBuilder). En runtime no se usa: los prefabs ya quedan creados y se
    /// editan a mano desde ahí.
    ///
    /// Convención de posiciones: el pivote de cada elemento coincide con su ancla,
    /// así anchoredPosition se mide desde la esquina o el borde anclado.
    /// </summary>
    public static class ROMUIFactory
    {
        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; //UI
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static void Stretch(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 position, Vector2 size, bool preserveAspect = false)
        {
            RectTransform rect = Rect(parent, name, anchor, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        public static Image Fill(Transform parent, string name, Sprite sprite, Color color)
        {
            Image image = Img(parent, name, sprite, color, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(image.rectTransform);
            return image;
        }

        /// <summary>
        /// Imagen que cubre todo el padre manteniendo proporción (fondo "cover").
        /// </summary>
        public static Image Cover(Transform parent, string name, Sprite sprite, Color color)
        {
            Image image = Fill(parent, name, sprite, color);

            if (sprite)
            {
                AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }

            return image;
        }

        public static Text Txt(Transform parent, string name, Font font, int size, Color color, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 rectSize, string value = "")
        {
            RectTransform rect = Rect(parent, name, anchor, position, rectSize);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            return text;
        }

        public static void Shadow(Graphic graphic, float distance = 1.5f, float alpha = 0.6f)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, alpha);
            shadow.effectDistance = new Vector2(distance, -distance);
        }

        public static CanvasGroup Group(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(parent, name, anchor, position, size);
            CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        public static Canvas Canvas(GameObject go, int sortingOrder, bool raycaster)
        {
            go.layer = 5;
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (raycaster)
                go.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        /// <summary>
        /// Panel oscuro con título y línea de acento arriba. Devuelve el contenedor
        /// del contenido (debajo del título).
        /// </summary>
        public static RectTransform Card(Transform parent, ROMUITheme theme, string name, string title, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform card = Rect(parent, name, anchor, position, size);

            Image background = card.gameObject.AddComponent<Image>();
            background.color = theme.Panel;
            background.raycastTarget = true; //que los clics no atraviesen a lo de atrás

            Img(card, "Accent", theme.AccentBar, theme.Accent, new Vector2(0, 1), Vector2.zero, new Vector2(size.x, 3));

            if (theme.FrameCorners)
                Stretch(Img(card, "Corners", theme.FrameCorners, new Color(1, 1, 1, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).rectTransform, -6f);

            Txt(card, "Title", theme.Title, 34, theme.Text, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(28, -14), new Vector2(size.x - 56, 44), title);

            return Rect(card, "Content", new Vector2(0, 1), new Vector2(28, -70), new Vector2(size.x - 56, size.y - 92));
        }

        public static Text Label(Transform parent, ROMUITheme theme, string value, Vector2 position, float width)
        {
            return Txt(parent, $"Label_{value}", theme.Body, 15, theme.Muted, TextAnchor.LowerLeft, new Vector2(0, 1), position, new Vector2(width, 20), value.ToUpperInvariant());
        }

        public static Button Button(Transform parent, ROMUITheme theme, string name, string label, bool primary, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(parent, name, anchor, position, size);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = Colors(primary ? theme.Accent : new Color(1, 1, 1, 0.08f), primary ? Color.Lerp(theme.Accent, Color.white, 0.25f) : theme.AccentDark);

            if (!primary)
            {
                Outline outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.6f);
                outline.effectDistance = new Vector2(1, -1);
            }

            Text text = Txt(rect, "Label", theme.Title, primary ? 30 : 24, theme.Text, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size, label.ToUpperInvariant());
            Shadow(text, 1f, 0.4f);

            return button;
        }

        public static ColorBlock Colors(Color normal, Color highlighted)
        {
            return new ColorBlock
            {
                normalColor = normal,
                highlightedColor = highlighted,
                selectedColor = highlighted,
                pressedColor = Color.Lerp(highlighted, Color.black, 0.25f),
                disabledColor = new Color(normal.r, normal.g, normal.b, normal.a * 0.35f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
        }

        public static InputField Input(Transform parent, ROMUITheme theme, string name, string placeholder, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0, 1), position, size);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;

            InputField input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.colors = Colors(theme.Field, new Color(1, 1, 1, 0.12f));

            Text text = Txt(rect, "Text", theme.Body, 20, theme.Text, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(14, 0);
            text.rectTransform.offsetMax = new Vector2(-14, 0);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text hint = Txt(rect, "Placeholder", theme.Body, 20, new Color(theme.Muted.r, theme.Muted.g, theme.Muted.b, 0.5f), TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, placeholder);
            Stretch(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(14, 0);
            hint.rectTransform.offsetMax = new Vector2(-14, 0);
            hint.fontStyle = FontStyle.Italic;

            input.textComponent = text;
            input.placeholder = hint;
            input.selectionColor = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.45f);
            input.caretColor = theme.Accent;
            input.customCaretColor = true;

            Img(rect, "Underline", null, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.6f), new Vector2(0, 0), Vector2.zero, new Vector2(size.x, 2));

            return input;
        }

        /// <summary>
        /// Selector ◀ valor ▶ con su componente ROMSelector ya cableado.
        /// </summary>
        public static ROMSelector Selector(Transform parent, ROMUITheme theme, string name, Vector2 position, Vector2 size, int fontSize = 30)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0, 1), position, size);

            Image background = rect.gameObject.AddComponent<Image>();
            background.color = theme.Field;

            Text value = Txt(rect, "Value", theme.Title, fontSize, theme.Text, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 2 * size.y, size.y));
            value.horizontalOverflow = HorizontalWrapMode.Overflow;

            Button previous = Arrow(rect, theme, "Previous", theme.ArrowLeft, "<", new Vector2(0, 0.5f), size.y);
            Button next = Arrow(rect, theme, "Next", theme.ArrowRight, ">", new Vector2(1, 0.5f), size.y);

            Img(rect, "Underline", null, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.6f), new Vector2(0, 0), Vector2.zero, new Vector2(size.x, 2));

            ROMSelector selector = rect.gameObject.AddComponent<ROMSelector>();
            Wire(selector, "_value", value);
            Wire(selector, "_previous", previous);
            Wire(selector, "_next", next);

            return selector;
        }

        static Button Arrow(RectTransform parent, ROMUITheme theme, string name, Sprite sprite, string fallback, Vector2 anchor, float size)
        {
            RectTransform rect = Rect(parent, name, anchor, Vector2.zero, new Vector2(size, size));

            Image hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.white;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.colors = Colors(new Color(1, 1, 1, 0f), new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.35f));

            if (sprite)
                Img(rect, "Icon", sprite, theme.Text, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.32f, size * 0.5f), true);
            else
                Txt(rect, "Icon", theme.Title, 30, theme.Text, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), fallback);

            return button;
        }

        public static Toggle Toggle(Transform parent, ROMUITheme theme, string name, string label, Vector2 position, float width)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0, 1), position, new Vector2(width, 34));

            Image box = Img(rect, "Box", null, Color.white, new Vector2(0, 0.5f), Vector2.zero, new Vector2(26, 26));
            box.raycastTarget = true;

            Image check = Img(box.rectTransform, "Check", null, theme.Accent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));

            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.colors = Colors(theme.Field, new Color(1, 1, 1, 0.16f));

            Txt(rect, "Label", theme.Body, 18, theme.Text, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(width - 40, 34), label.ToUpperInvariant());

            return toggle;
        }

        /// <summary>
        /// Asigna un campo serializado (privado) de un componente.
        /// </summary>
        public static void Wire(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);

            if (property == null)
            {
                Debug.LogError($"[ROM] {target.GetType().Name} no tiene el campo {field}.");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
