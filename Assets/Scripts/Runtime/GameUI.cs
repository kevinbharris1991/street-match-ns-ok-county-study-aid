using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SherrysGame
{
    internal static class GameUI
    {
        internal static readonly Color Ink = new Color32(18, 24, 43, 255);
        internal static readonly Color Panel = new Color32(29, 38, 61, 255);
        internal static readonly Color Card = new Color32(40, 52, 79, 255);
        internal static readonly Color Mint = new Color32(111, 235, 193, 255);
        internal static readonly Color Lavender = new Color32(190, 175, 255, 255);
        internal static readonly Color White = new Color32(242, 245, 255, 255);
        internal static readonly Color Muted = new Color32(167, 180, 207, 255);
        internal static readonly Color Coral = new Color32(255, 159, 151, 255);
        private static Sprite rounded;
        private static Font font;

        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        internal static Image Surface(RectTransform rect, Color color, bool roundedCorners = true)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            if (roundedCorners)
            {
                image.sprite = Rounded;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        internal static RectTransform Column(string name, Transform parent, int padding = 0, float spacing = 12)
        {
            RectTransform rect = Rect(name, parent);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        internal static RectTransform Row(string name, Transform parent, float spacing = 12)
        {
            RectTransform rect = Rect(name, parent);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return rect;
        }

        internal static LayoutElement Size(RectTransform rect, float height = -1, float flex = 0, float width = -1)
        {
            var size = rect.GetComponent<LayoutElement>();
            if (size == null) size = rect.gameObject.AddComponent<LayoutElement>();
            size.preferredHeight = height;
            size.flexibleHeight = flex;
            size.preferredWidth = width;
            return size;
        }

        internal static void FlexWidth(RectTransform rect, float weight = 1)
        {
            Size(rect).flexibleWidth = weight;
        }

        internal static Text Label(string text, Transform parent, int fontSize = 22,
            Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            RectTransform rect = Rect("Label", parent);
            var label = rect.gameObject.AddComponent<Text>();
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color ?? White;
            label.alignment = alignment;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = fontSize;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        internal static Button Button(string text, Transform parent, UnityAction action,
            Color? background = null, Color? foreground = null)
        {
            RectTransform rect = Rect(text, parent);
            Surface(rect, background ?? Card);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.13f, 1.13f, 1.13f, 1);
            colors.pressedColor = new Color(.8f, .8f, .8f, 1);
            colors.disabledColor = new Color(.48f, .48f, .48f, .7f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Text label = Label(text, rect, 20, foreground ?? White, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 8);
            Size(rect, 48);
            return button;
        }

        internal static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64;
                const float radius = 15;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "Generated rounded card";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - (32 - radius), 0);
                    float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - (32 - radius), 0);
                    pixels[y * size + x] = new Color(1, 1, 1,
                        Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy)));
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                rounded = Sprite.Create(texture, new Rect(0, 0, size, size),
                    new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect,
                    new Vector4(16, 16, 16, 16));
                return rounded;
            }
        }
    }
}
