using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 타이틀·일시정지에서 같이 쓰는 세로 메뉴 (W/S 또는 ↑/↓로 이동, E/Space/Enter로 선택)
public class MenuList
{
    readonly Text[] items;
    readonly string[] labels;
    int selected;

    static readonly Color On = new Color(1f, 0.84f, 0.29f);   // 선택된 항목 (노란색)
    static readonly Color Off = new Color(0.75f, 0.75f, 0.72f);

    public MenuList(Transform parent, Font font, string[] labels, int fontSize, float startY, float spacing)
    {
        this.labels = labels;
        items = new Text[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            var go = new GameObject("Item" + i, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, fontSize + 20);
            rt.anchoredPosition = new Vector2(0, startY - i * spacing);
            items[i] = t;
        }
        Refresh();
    }

    public void Reset() { selected = 0; Refresh(); }

    // 이번 프레임에 고른 항목 번호 (안 골랐으면 -1)
    public int HandleInput(Keyboard kb)
    {
        if (kb == null) return -1;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Move(-1);
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Move(1);
        if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            return selected;
        return -1;
    }

    void Move(int d)
    {
        selected = (selected + d + items.Length) % items.Length;
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < items.Length; i++)
        {
            bool on = i == selected;
            items[i].text = on ? "▶  " + labels[i] + "   " : labels[i];
            items[i].color = on ? On : Off;
        }
    }

    // ───── 공통 UI 도우미 ─────
    public static Canvas MakeCanvas(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static Image MakeFullImage(Transform parent, Color color)
    {
        var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    public static Text MakeText(Transform parent, Font font, string text, int size, float y, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1600, size + 40);
        rt.anchoredPosition = new Vector2(0, y);
        return t;
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // 에디터에서는 Play 멈춤
#else
        Application.Quit();
#endif
    }
}
