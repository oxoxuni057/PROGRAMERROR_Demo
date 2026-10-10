using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// 규칙 필체: 인쇄체 / 손글씨(파란 잉크) / 붉은 글씨
public enum RuleStyle { Printed, Handwritten, Red }

[System.Serializable]
public class RuleEntry
{
    public string id;
    public string text;
    public RuleStyle style;
    public bool struck;   // 줄 그어졌는지

    public RuleEntry Clone()
    {
        return new RuleEntry { id = id, text = text, style = style, struck = struck };
    }
}

public class RuleBook : MonoBehaviour
{
    public static RuleBook Instance { get; private set; }

    static readonly List<RuleEntry> rules = new List<RuleEntry>();
    public static IReadOnlyList<RuleEntry> Rules => rules;
    public static bool IsOpen => Instance != null && Instance.open;

    const int RulesPerPage = 5;

    // ───── 게임 시작 시 초기화 + 자동 생성 ─────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        rules.Clear();
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        new GameObject("RuleBook").AddComponent<RuleBook>();
    }

    // ───── 다른 스크립트에서 쓰는 기능 ─────
    public static bool Has(string id) => Find(id) != null;

    public static bool IsStruck(string id)
    {
        var r = Find(id);
        return r != null && r.struck;
    }

    // 규칙 추가 (같은 id로 다시 부르면 내용/필체가 바뀜)
    public static void Add(string id, string text, RuleStyle style)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[지침서] 규칙 id가 비어 있어요");
            return;
        }

        var r = Find(id);
        if (r == null)
        {
            rules.Add(new RuleEntry { id = id, text = text, style = style });
            Debug.Log($"[지침서] 규칙 추가: {id}");
            Notify("지침서에 새 규칙이 적혔다    [Tab]");
        }
        else if (r.text != text || r.style != style)
        {
            r.text = text;
            r.style = style;
            Debug.Log($"[지침서] 규칙 변경: {id}");
            Notify("지침서의 글씨가… 바뀌어 있다    [Tab]");
        }
        else return;

        GameFlags.Set("rule_" + id);
        if (Instance != null && Instance.open) Instance.Refresh();
    }

    // 규칙에 줄 긋기 / 지우기
    public static void Strike(string id, bool value = true)
    {
        var r = Find(id);
        if (r == null) { Debug.LogWarning($"[지침서] 없는 규칙: {id}"); return; }
        if (r.struck == value) return;

        r.struck = value;
        Debug.Log($"[지침서] 규칙 {(value ? "줄 긋기" : "줄 지우기")}: {id}");
        if (value) Notify("지침서의 규칙 하나에 줄이 그어졌다    [Tab]");
        if (Instance != null && Instance.open) Instance.Refresh();
    }

    // ROLLBACK용 저장/복구
    public static List<RuleEntry> Snapshot()
    {
        var list = new List<RuleEntry>();
        foreach (var r in rules) list.Add(r.Clone());
        return list;
    }

    public static void Restore(List<RuleEntry> saved)
    {
        rules.Clear();
        if (saved != null) foreach (var r in saved) rules.Add(r.Clone());
        if (Instance != null && Instance.open) Instance.Refresh();
    }

    static RuleEntry Find(string id)
    {
        foreach (var r in rules) if (r.id == id) return r;
        return null;
    }

    static void Notify(string msg)
    {
        if (Instance != null) Instance.ShowToast(msg);
    }

    // ───── 화면 부분 ─────
    bool open;
    int page;

    Font font;
    GameObject bookRoot;
    Text pageText, emptyText;
    readonly Text[] rows = new Text[RulesPerPage];
    readonly List<Image>[] strikes = new List<Image>[RulesPerPage];
    readonly TextGenerator generator = new TextGenerator();

    CanvasGroup toastGroup;
    Text toastText;
    float toastTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
        bookRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        UpdateToast();

        var kb = Keyboard.current;
        if (kb == null) return;

        if (!open)
        {
            if (kb.tabKey.wasPressedThisFrame) Open();
            return;
        }

        if (kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame) { Close(); return; }
        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) { page--; Refresh(); }
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) { page++; Refresh(); }
    }

    void Open()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null && !player.CanMove) return;   // 대화 중·씬 이동 중엔 안 열림

        PlayerController.SetLock(this, true);

        open = true;
        page = Mathf.Max(0, (rules.Count - 1) / RulesPerPage);   // 최신 규칙이 있는 쪽부터
        bookRoot.SetActive(true);
        Refresh();
    }

    void Close()
    {
        open = false;
        bookRoot.SetActive(false);
        PlayerController.SetLock(this, false);
    }

    void Refresh()
    {
        int pageCount = Mathf.Max(1, (rules.Count + RulesPerPage - 1) / RulesPerPage);
        page = Mathf.Clamp(page, 0, pageCount - 1);
        emptyText.gameObject.SetActive(rules.Count == 0);

        for (int i = 0; i < RulesPerPage; i++)
        {
            int idx = page * RulesPerPage + i;
            bool has = idx < rules.Count;
            rows[i].gameObject.SetActive(has);
            if (!has) continue;

            var r = rules[idx];
            rows[i].text = $"{idx + 1}.  {r.text}";
            ApplyStyle(rows[i], r);
            DrawStrike(i, r.struck);
        }

        pageText.text = $"{page + 1} / {pageCount}        A·D 넘기기    Tab 닫기";
    }

    void ApplyStyle(Text t, RuleEntry r)
    {
        Color c;
        switch (r.style)
        {
            case RuleStyle.Handwritten:
                c = new Color(0.10f, 0.16f, 0.42f); t.fontStyle = FontStyle.Italic; break;
            case RuleStyle.Red:
                c = new Color(0.55f, 0.03f, 0.03f); t.fontStyle = FontStyle.BoldAndItalic; break;
            default:
                c = new Color(0.12f, 0.10f, 0.08f); t.fontStyle = FontStyle.Normal; break;
        }
        if (r.struck) c.a = 0.5f;
        t.color = c;
    }

    // 줄마다 글자 길이에 맞춰 빨간 선 긋기
    void DrawStrike(int row, bool struck)
    {
        foreach (var img in strikes[row]) img.gameObject.SetActive(false);
        if (!struck) return;

        var t = rows[row];
        var rt = t.rectTransform;
        generator.Populate(t.text, t.GetGenerationSettings(rt.rect.size));
        float ppu = t.pixelsPerUnit;
        var lines = generator.lines;
        var chars = generator.characters;
        string s = t.text;

        for (int l = 0; l < lines.Count; l++)
        {
            int start = lines[l].startCharIdx;
            int end = (l + 1 < lines.Count) ? lines[l + 1].startCharIdx : s.Length;
            float minX = float.MaxValue, maxX = float.MinValue;

            for (int c = start; c < end && c < s.Length && c < chars.Count; c++)
            {
                if (char.IsWhiteSpace(s[c])) continue;
                minX = Mathf.Min(minX, chars[c].cursorPos.x);
                maxX = Mathf.Max(maxX, chars[c].cursorPos.x + chars[c].charWidth);
            }
            if (minX > maxX) continue;

            while (strikes[row].Count <= l) strikes[row].Add(MakeStrike(rt));
            var line = strikes[row][l].rectTransform;
            float y = (lines[l].topY - lines[l].height * 0.5f) / ppu;
            line.anchoredPosition = new Vector2(minX / ppu - 6f, y);
            line.sizeDelta = new Vector2((maxX - minX) / ppu + 12f, 4f);
            strikes[row][l].gameObject.SetActive(true);
        }
    }

    Image MakeStrike(RectTransform parent)
    {
        var img = CreateImage("Strike", parent, new Color(0.45f, 0.02f, 0.02f, 0.9f));
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        return img;
    }

    void ShowToast(string msg)
    {
        toastText.text = msg;
        toastTimer = 2.5f;
        toastGroup.alpha = 1f;
    }

    void UpdateToast()
    {
        if (toastTimer <= 0f) return;
        toastTimer -= Time.unscaledDeltaTime;
        toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.5f);
    }

    // ───── UI 코드로 만들기 ─────
    void BuildUI()
    {
        var canvasGO = new GameObject("RuleBookCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 어두운 배경
        var dim = CreateImage("Dim", canvasGO.transform, new Color(0f, 0f, 0f, 0.75f));
        Stretch(dim.rectTransform);
        bookRoot = dim.gameObject;

        // 종이
        var paper = CreateImage("Paper", dim.transform, new Color(0.87f, 0.83f, 0.72f));
        Place(paper.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
              Vector2.zero, new Vector2(900, 960));

        var title = CreateText("Title", paper.transform, 52, TextAnchor.MiddleCenter, new Color(0.15f, 0.1f, 0.07f));
        title.text = "지  침  서";
        title.fontStyle = FontStyle.Bold;
        Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
              new Vector2(0, -40), new Vector2(0, 80));

        var bar = CreateImage("Line", paper.transform, new Color(0.3f, 0.22f, 0.15f, 0.6f));
        Place(bar.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
              new Vector2(0, -135), new Vector2(-120, 3));

        for (int i = 0; i < RulesPerPage; i++)
        {
            var row = CreateText("Rule" + i, paper.transform, 34, TextAnchor.MiddleLeft, Color.black);
            Place(row.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                  new Vector2(70, -160 - i * 140), new Vector2(-140, 130));
            rows[i] = row;
            strikes[i] = new List<Image>();
        }

        emptyText = CreateText("Empty", paper.transform, 32, TextAnchor.MiddleCenter, new Color(0.3f, 0.25f, 0.2f, 0.8f));
        emptyText.text = "아직 아무것도 적혀 있지 않다.";
        Place(emptyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
              Vector2.zero, new Vector2(800, 100));

        pageText = CreateText("Page", paper.transform, 26, TextAnchor.MiddleCenter, new Color(0.3f, 0.25f, 0.2f));
        Place(pageText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
              new Vector2(0, 30), new Vector2(0, 50));

        // 알림 (지침서를 닫아도 보이게)
        var toastBg = CreateImage("Toast", canvasGO.transform, new Color(0f, 0f, 0f, 0.8f));
        Place(toastBg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
              new Vector2(0, -40), new Vector2(900, 70));
        toastGroup = toastBg.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastText = CreateText("ToastText", toastBg.transform, 30, TextAnchor.MiddleCenter, Color.white);
        Stretch(toastText.rectTransform);
    }

    Text CreateText(string name, Transform parent, int size, TextAnchor align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = false;
        t.raycastTarget = false;
        return t;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}