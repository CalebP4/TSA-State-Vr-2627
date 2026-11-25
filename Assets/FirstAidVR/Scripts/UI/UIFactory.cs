using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FirstAidVR
{
    /// <summary>
    /// Small helper for building world-space VR screens from code. Positions are in
    /// canvas pixels measured from the top-left corner; 1000 px = 1 metre.
    /// </summary>
    public static class UIFactory
    {
        public const float MetersPerPixel = 0.001f;

        public static Canvas WorldCanvas(string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector2 sizePx)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizePx;
            rt.localPosition = localPos;
            rt.localRotation = localRot;
            rt.localScale = Vector3.one * MetersPerPixel;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 3f;
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            go.AddComponent<CanvasGroup>();
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            Place(rt, x, y, w, h);
            return rt;
        }

        /// <summary>Top-left anchored placement. Negative w/h stretch to the parent's edge minus that margin.</summary>
        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            var parent = rt.parent as RectTransform;
            if (parent != null)
            {
                if (w <= 0) w = parent.rect.width - x + w;
                if (h <= 0) h = parent.rect.height - y + h;
            }
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Image Panel(Transform parent, float x, float y, float w, float h, Color color, float radius = 1f, string name = "Panel")
        {
            var rt = Rect(name, parent, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, radius);
            img.color = color;
            return img;
        }

        /// <summary>Full-size rounded background with a thin border.</summary>
        public static Image Card(RectTransform canvas, Color? fill = null)
        {
            var border = Panel(canvas, 0, 0, canvas.sizeDelta.x, canvas.sizeDelta.y, Theme.Border, 1f, "Border");
            Fill(border.rectTransform);
            var bg = Panel(border.transform, 0, 0, 10, 10, fill ?? Theme.Panel, 1f, "Background");
            Fill(bg.rectTransform);
            bg.rectTransform.offsetMin = new Vector2(3, 3);
            bg.rectTransform.offsetMax = new Vector2(-3, -3);
            return bg;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float x, float y, float w, float h,
            float size = 32, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, bool bold = false)
        {
            var rt = Rect("Text", parent, x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Theme.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color ?? Theme.Text;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Image Icon(Transform parent, Sprite sprite, float x, float y, float size, Color color)
        {
            var rt = Rect("Icon", parent, x, y, size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>The red-and-white cross logo.</summary>
        public static RectTransform Logo(Transform parent, float x, float y, float size)
        {
            var bg = Panel(parent, x, y, size, size, Theme.Red, 0.7f, "Logo");
            Icon(bg.transform, Theme.Cross, size * 0.15f, size * 0.15f, size * 0.7f, Color.white);
            return bg.rectTransform;
        }

        public static Button Button(Transform parent, string label, float x, float y, float w, float h, Action onClick,
            Color? color = null, float textSize = 32, string name = null)
        {
            var img = Panel(parent, x, y, w, h, Color.white, 0.6f, name ?? "Button " + label);
            var btn = img.gameObject.AddComponent<Button>();
            var c = color ?? Theme.PanelLight;
            var cb = btn.colors;
            cb.normalColor = c;
            cb.highlightedColor = Color.Lerp(c, Color.white, 0.18f);
            cb.pressedColor = Color.Lerp(c, Color.black, 0.2f);
            cb.selectedColor = c;
            cb.disabledColor = c.WithAlpha(0.35f);
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            btn.targetGraphic = img;
            img.CrossFadeColor(c, 0f, true, true); // show the normal colour straight away
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            var t = Label(img.transform, label, 16, 0, w - 32, h, textSize, Theme.Text, TextAlignmentOptions.Center, true);
            t.overflowMode = TextOverflowModes.Overflow;
            btn.onClick.AddListener(() =>
            {
                Sfx.Play(Sound.Click);
                onClick?.Invoke();
            });
            var trigger = img.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { if (btn.interactable) Sfx.Play(Sound.Hover); });
            trigger.triggers.Add(enter);
            return btn;
        }

        public static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = text;
        }

        /// <summary>A horizontal bar. Returns the fill image; set fillAmount (0..1).</summary>
        public static Image Bar(Transform parent, float x, float y, float w, float h, Color color, float value)
        {
            var bg = Panel(parent, x, y, w, h, Theme.Background, 0.5f, "Bar");
            var fill = Panel(bg.transform, 0, 0, w, h, color, 0.5f, "Fill");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = Mathf.Clamp01(value);
            fill.sprite = Theme.Rounded;
            return fill;
        }

        /// <summary>A circular score ring with the number in the middle.</summary>
        public static Image ScoreRing(Transform parent, float x, float y, float size, int score, string caption)
        {
            var root = Rect("Score Ring", parent, x, y, size, size);
            var bg = Icon(root, Theme.Ring, 0, 0, size, Theme.Background);
            var fill = Icon(root, Theme.Ring, 0, 0, size, Theme.ScoreColor(score));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = score / 100f;
            Label(root, score.ToString(), 0, size * 0.24f, size, size * 0.4f, size * 0.3f, Theme.Text, TextAlignmentOptions.Center, true);
            Label(root, caption, 0, size * 0.6f, size, size * 0.2f, size * 0.09f, Theme.Muted, TextAlignmentOptions.Center);
            return fill;
        }

        public static void SetVisible(Canvas c, bool visible)
        {
            if (c == null) return;
            c.gameObject.SetActive(visible);
        }
    }
}
