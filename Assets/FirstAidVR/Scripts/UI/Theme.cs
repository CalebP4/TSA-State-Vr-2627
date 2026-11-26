using TMPro;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>The project's colours, font and generated sprites, so every screen matches.</summary>
    public static class Theme
    {
        public const string ProductName = "First Response VR";
        public const string Tagline = "Interactive First-Aid Training Simulator";

        public static readonly Color Background = Hex("0E1726");
        public static readonly Color Panel = Hex("152338");
        public static readonly Color PanelLight = Hex("1E3150");
        public static readonly Color Border = Hex("2B4266");
        public static readonly Color Text = Hex("F3F6FA");
        public static readonly Color Muted = Hex("9BAAC0");
        public static readonly Color Red = Hex("E5484D");
        public static readonly Color Green = Hex("2FBF71");
        public static readonly Color Amber = Hex("F5A524");
        public static readonly Color Blue = Hex("3B82F6");
        public static readonly Color Teal = Hex("14B8A6");
        public static readonly Color Purple = Hex("8B5CF6");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }

        /// <summary>Green for good scores, amber for okay, red for low.</summary>
        public static Color ScoreColor(int score) => score >= 80 ? Green : score >= 60 ? Amber : Red;

        static TMP_FontAsset font;
        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null)
                {
                    var theme = Resources.Load<UiThemeAsset>("FirstAidVR/UiTheme");
                    font = theme != null && theme.font != null ? theme.font : TMP_Settings.defaultFontAsset;
                }
                return font;
            }
        }

        static Sprite rounded, circle, cross, ring;

        /// <summary>9-sliced rounded rectangle (radius 24 px at multiplier 1).</summary>
        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeRounded(96, 32);
        public static Sprite Circle => circle != null ? circle : circle = MakeCircle(128, 0f);
        public static Sprite Ring => ring != null ? ring : ring = MakeCircle(256, 0.78f);
        /// <summary>White medical cross on transparent background.</summary>
        public static Sprite Cross => cross != null ? cross : cross = MakeCross(128);

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = NewTex(size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                float dy = Mathf.Max(0, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(255 * Mathf.Clamp01(radius - d + 0.5f));
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        static Sprite MakeCircle(int size, float hole)
        {
            var tex = NewTex(size);
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d);
                if (hole > 0) a *= Mathf.Clamp01(d - r * hole);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * a));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeCross(int size)
        {
            var tex = NewTex(size);
            var px = new Color32[size * size];
            float arm = size * 0.17f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Abs(x + 0.5f - size / 2f), cy = Mathf.Abs(y + 0.5f - size / 2f);
                bool inside = (cx < arm && cy < size * 0.4f) || (cy < arm && cx < size * 0.4f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(inside ? 255 : 0));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Texture2D NewTex(int size) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
    }
}
