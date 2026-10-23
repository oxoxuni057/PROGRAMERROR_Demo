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
    public string section;       // 구역 이름 (예: 공통 지침, 2층). 비우면 구역 없음
    public string label;         // 번호/글자 (예: 1, 가). 비우면 표시 안 함
    public string text;
    public RuleStyle style;
    public bool struck;          // 줄 그어졌는지
    public string note;          // 다른 필체로 덧붙은 글 (규칙 "라"의 "뛰지 마…")
    public RuleStyle noteStyle;

    public RuleEntry Clone() => (RuleEntry)MemberwiseClone();
}

public class RuleBook : MonoBehaviour
{
    public const string CommonSection = "공통 지침";   // 항상 맨 앞에 나오는 구역

    public static RuleBook Instance { get; private set; }

    static readonly List<RuleEntry> rules = new List<RuleEntry>();
    public static IReadOnlyList<RuleEntry> Rules => rules;
    public static bool IsOpen => Instance != null && Instance.open;
    public static int ClosedFrame { get; private set; } = -1;   // 마지막으로 닫힌 프레임 (Esc가 일시정지까지 여는 것 방지)

    static string focusId;   // 지침서를 열면 이 규칙이 있는 쪽부터 보여줌 (마지막으로 바뀐 규칙)

    // ───── 게임 시작 시 초기화 + 자동 생성 ─────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        rules.Clear();
        focusId = null;
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

    // 예전 방식 (구역·번호 없이)
    public static void Add(string id, string text, RuleStyle style) => Add(id, "", "", text, style);

    // 규칙 추가 (같은 id로 다시 부르면 내용/필체가 바뀜). struck = 처음부터 줄이 그어진 채로 발견
    public static void Add(string id, string section, string label, string text, RuleStyle style, bool struck = false)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[지침서] 규칙 id가 비어 있어요");
            return;
        }

        var r = Find(id);
        if (r == null)
        {
            rules.Add(new RuleEntry { id = id, section = section, label = label, text = text, style = style, struck = struck });
            Debug.Log($"[지침서] 규칙 추가: {id}");
            Notify("지침서에 새 규칙이 적혔다    [Tab]");
        }
        else if (r.text != text || r.style != style || r.section != section || r.label != label)
        {
            r.section = section;
            r.label = label;
            r.text = text;
            r.style = style;
            Debug.Log($"[지침서] 규칙 변경: {id}");
            Notify("지침서의 글씨가… 바뀌어 있다    [Tab]");
        }
        else return;

        focusId = id;
        GameFlags.Set("rule_" + id);
        if (Instance != null && Instance.open) Instance.Refresh(true);
    }

    // 규칙에 줄 긋기 / 지우기
    public static void Strike(string id, bool value = true)
    {
        var r = Find(id);
        if (r == null) { Debug.LogWarning($"[지침서] 없는 규칙: {id}"); return; }
        if (r.struck == value) return;

        r.struck = value;
        focusId = id;
        Debug.Log($"[지침서] 규칙 {(value ? "줄 긋기" : "줄 지우기")}: {id}");
        if (value) Notify("지침서의 규칙 하나에 줄이 그어졌다    [Tab]");
        if (Instance != null && Instance.open) Instance.Refresh(true);
    }

    // 규칙 아래에 다른 필체로 덧붙이기 (비우면 지움)
    public static void SetNote(string id, string note, RuleStyle style)
    {
        var r = Find(id);
        if (r == null) { Debug.LogWarning($"[지침서] 없는 규칙: {id}"); return; }
        if (r.note == note && r.noteStyle == style) return;

        r.note = note;
        r.noteStyle = style;
        focusId = id;
        Debug.Log($"[지침서] 덧붙임: {id}");
        if (!string.IsNullOrEmpty(note)) Notify("규칙 옆에 다른 글씨가 덧붙어 있다    [Tab]");
        if (Instance != null && Instance.open) Instance.Refresh(true);
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
        if (Instance != null && Instance.open) Instance.Refresh(false);
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

    // ───── 보여줄 순서: 공통 지침 → 나머지 구역(처음 나온 순서), 구역 안에서는 번호/글자 순 ─────
    static List<RuleEntry> Ordered()
    {
        var sections = new List<string>();
        foreach (var r in rules)
        {
            string s = r.section ?? "";
            if (!sections.Contains(s)) sections.Add(s);
        }
        sections.Sort((a, b) => SectionRank(a, sections).CompareTo(SectionRank(b, sections)));

        var result = new List<RuleEntry>();
        foreach (var s in sections)
        {
            var group = new List<RuleEntry>();
            foreach (var r in rules) if ((r.section ?? "") == s) group.Add(r);
            // 같은 구역 안: 번호는 숫자 크기대로, 글자(가나다)는 가나다순, 번호 없는 건 얻은 순서대로 뒤에
            var indexed = new List<KeyValuePair<int, RuleEntry>>();
            for (int i = 0; i < group.Count; i++) indexed.Add(new KeyValuePair<int, RuleEntry>(i, group[i]));
            indexed.Sort((x, y) =>
            {
                int c = CompareLabel(x.Value.label, y.Value.label);
                return c != 0 ? c : x.Key.CompareTo(y.Key);
            });
            foreach (var kv in indexed) result.Add(kv.Value);
        }
        return result;
    }

    static int SectionRank(string s, List<string> firstSeen)
    {
        if (s == CommonSection) return -1;
        return firstSeen.IndexOf(s);
    }

    static int CompareLabel(string a, string b)
    {
        bool ea = string.IsNullOrEmpty(a), eb = string.IsNullOrEmpty(b);
        if (ea || eb) return ea == eb ? 0 : (ea ? 1 : -1);
        bool na = int.TryParse(a, out int ia), nb = int.TryParse(b, out int ib);
        if (na && nb) return ia.CompareTo(ib);
        if (na != nb) return na ? -1 : 1;
        return string.CompareOrdinal(a, b);
    }

    // ───── 화면 부분 ─────
    const float AreaTop = -160f;      // 규칙이 시작되는 높이 (종이 위에서부터)
    const float AreaHeight = 690f;    // 한 쪽에 쓸 수 있는 높이
    const float Margin = 70f;         // 종이 좌우 여백
    const float PaperWidth = 900f;
    const float Spacing = 14f;        // 줄 사이 간격

    // 지침서에 그려지는 한 줄 (구역 제목 / 규칙 / 덧붙임)
    class Line
    {
        public string text;
        public int size;
        public FontStyle fontStyle;
        public Color color;
        public bool struck;
        public float indent;
        public float gapBefore;
        public string ruleId;
        public float height;
    }

    class Slot
    {
        public Text text;
        public readonly List<Image> strikes = new List<Image>();
    }

    bool open;
    int page;

    Font font;
    GameObject bookRoot;
    Transform paper;
    Text pageText, emptyText, measure;
    readonly List<Slot> slots = new List<Slot>();
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
        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) { page--; Refresh(false); }
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) { page++; Refresh(false); }
    }

    void Open()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null || !player.CanMove) return;   // 플레이어가 없거나(타이틀) 대화 중·씬 이동 중엔 안 열림

        PlayerController.SetLock(this, true);
        open = true;
        bookRoot.SetActive(true);
        Refresh(true);   // 마지막으로 바뀐 규칙이 있는 쪽부터
    }

    void Close()
    {
        open = false;
        ClosedFrame = Time.frameCount;
        bookRoot.SetActive(false);
        PlayerController.SetLock(this, false);
    }

    // 지침서 다시 그리기. jumpToFocus = 마지막으로 바뀐 규칙이 있는 쪽으로 이동
    void Refresh(bool jumpToFocus)
    {
        var lines = BuildLines();
        emptyText.gameObject.SetActive(lines.Count == 0);

        // 높이를 재서 쪽 나누기
        var pages = new List<List<Line>>();
        var current = new List<Line>();
        float y = 0f;
        int focusPage = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            var ln = lines[i];
            ln.height = Measure(ln);
            float need = (current.Count > 0 ? ln.gapBefore + Spacing : 0f) + ln.height;
            // 구역 제목만 쪽 끝에 홀로 남지 않게 다음 줄까지 같이 들어가는지 확인
            if (ln.ruleId == null && i + 1 < lines.Count) need += Spacing + Measure(lines[i + 1]);

            if (current.Count > 0 && y + need > AreaHeight)
            {
                pages.Add(current);
                current = new List<Line>();
                y = 0f;
            }
            y += (current.Count > 0 ? ln.gapBefore + Spacing : 0f) + ln.height;
            current.Add(ln);
            if (ln.ruleId != null && ln.ruleId == focusId && focusPage < 0) focusPage = pages.Count;
        }
        if (current.Count > 0 || pages.Count == 0) pages.Add(current);

        if (jumpToFocus) page = focusPage >= 0 ? focusPage : pages.Count - 1;
        page = Mathf.Clamp(page, 0, pages.Count - 1);

        // 이번 쪽 그리기
        var shown = pages[page];
        float top = AreaTop;
        for (int i = 0; i < shown.Count; i++)
        {
            var ln = shown[i];
            if (i > 0) top -= ln.gapBefore + Spacing;
            var slot = GetSlot(i);
            Apply(slot.text, ln);
            var rt = slot.text.rectTransform;
            rt.anchoredPosition = new Vector2(Margin + ln.indent, top);
            rt.sizeDelta = new Vector2(-(Margin * 2f + ln.indent), ln.height);
            slot.text.gameObject.SetActive(true);
            DrawStrike(slot, ln.struck);
            top -= ln.height;
        }
        for (int i = shown.Count; i < slots.Count; i++) slots[i].text.gameObject.SetActive(false);

        pageText.text = $"{page + 1} / {pages.Count}        A·D 넘기기    Tab 닫기";
    }

    List<Line> BuildLines()
    {
        var lines = new List<Line>();
        string lastSection = null;
        foreach (var r in Ordered())
        {
            string section = r.section ?? "";
            if (section != lastSection)
            {
                lastSection = section;
                if (section.Length > 0)
                    lines.Add(new Line
                    {
                        text = "[" + section + "]", size = 26, fontStyle = FontStyle.Bold,
                        color = new Color(0.35f, 0.25f, 0.15f), gapBefore = lines.Count > 0 ? 18f : 0f
                    });
            }

            string label = string.IsNullOrEmpty(r.label) ? "" : r.label + ".  ";
            var ruleLine = new Line { text = label + r.text, size = 32, struck = r.struck, ruleId = r.id };
            StyleOf(r.style, ruleLine);
            if (r.struck) ruleLine.color.a = 0.5f;
            lines.Add(ruleLine);

            if (!string.IsNullOrEmpty(r.note))
            {
                var noteLine = new Line { text = "→ " + r.note, size = 30, indent = 40f, gapBefore = -6f, ruleId = r.id };
                StyleOf(r.noteStyle, noteLine);
                lines.Add(noteLine);
            }
        }
        return lines;
    }

    static void StyleOf(RuleStyle style, Line ln)
    {
        switch (style)
        {
            case RuleStyle.Handwritten:
                ln.color = new Color(0.10f, 0.16f, 0.42f); ln.fontStyle = FontStyle.Italic; break;
            case RuleStyle.Red:
                ln.color = new Color(0.55f, 0.03f, 0.03f); ln.fontStyle = FontStyle.BoldAndItalic; break;
            default:
                ln.color = new Color(0.12f, 0.10f, 0.08f); ln.fontStyle = FontStyle.Normal; break;
        }
    }

    static void Apply(Text t, Line ln)
    {
        t.text = ln.text;
        t.fontSize = ln.size;
        t.fontStyle = ln.fontStyle;
        t.color = ln.color;
    }

    float Measure(Line ln)
    {
        Apply(measure, ln);
        float width = PaperWidth - Margin * 2f - ln.indent;
        var settings = measure.GetGenerationSettings(new Vector2(width, 0f));
        return generator.GetPreferredHeight(ln.text, settings) / measure.pixelsPerUnit;
    }

    Slot GetSlot(int i)
    {
        while (slots.Count <= i)
        {
            var t = CreateText("Line" + slots.Count, paper, 32, TextAnchor.UpperLeft, Color.black);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            slots.Add(new Slot { text = t });
        }
        return slots[i];
    }

    // 줄마다 글자 길이에 맞춰 빨간 선 긋기
    void DrawStrike(Slot slot, bool struck)
    {
        foreach (var img in slot.strikes) img.gameObject.SetActive(false);
        if (!struck) return;

        var t = slot.text;
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

            while (slot.strikes.Count <= l) slot.strikes.Add(MakeStrike(rt));
            var line = slot.strikes[l].rectTransform;
            float y = (lines[l].topY - lines[l].height * 0.5f) / ppu;
            line.anchoredPosition = new Vector2(minX / ppu - 6f, y);
            line.sizeDelta = new Vector2((maxX - minX) / ppu + 12f, 4f);
            slot.strikes[l].gameObject.SetActive(true);
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
        var paperImg = CreateImage("Paper", dim.transform, new Color(0.87f, 0.83f, 0.72f));
        Place(paperImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
              Vector2.zero, new Vector2(PaperWidth, 960));
        paper = paperImg.transform;

        var title = CreateText("Title", paper, 52, TextAnchor.MiddleCenter, new Color(0.15f, 0.1f, 0.07f));
        title.text = "지  침  서";
        title.fontStyle = FontStyle.Bold;
        Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
              new Vector2(0, -40), new Vector2(0, 80));

        var bar = CreateImage("Line", paper, new Color(0.3f, 0.22f, 0.15f, 0.6f));
        Place(bar.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
              new Vector2(0, -135), new Vector2(-120, 3));

        // 높이 재기 전용 (안 보이는 글자)
        measure = CreateText("Measure", paper, 32, TextAnchor.UpperLeft, Color.clear);
        Place(measure.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
              Vector2.zero, new Vector2(PaperWidth - Margin * 2f, 10));

        emptyText = CreateText("Empty", paper, 32, TextAnchor.MiddleCenter, new Color(0.3f, 0.25f, 0.2f, 0.8f));
        emptyText.text = "아직 아무것도 적혀 있지 않다.";
        Place(emptyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
              Vector2.zero, new Vector2(800, 100));

        pageText = CreateText("Page", paper, 26, TextAnchor.MiddleCenter, new Color(0.3f, 0.25f, 0.2f));
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
