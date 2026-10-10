using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 선택지 하나
[Serializable]
public class DialogueChoice
{
    public string text;

    [Tooltip("이 플래그가 있어야 선택지가 보임. 비우면 항상 보임")]
    public string requiredFlag;

    [Tooltip("이 선택지를 고르면 켜지는 플래그")]
    public string setFlag;

    [Tooltip("고른 뒤 이어질 대화. 비우면 대화 끝")]
    public DialogueTrigger next;

    [Tooltip("그 밖에 실행할 동작 (나중에 ROLLBACK 연결)")]
    public UnityEvent onChosen;
}

// 대사 한 줄
[Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 4)] public string text;

    [Tooltip("Normal: 보통 대사 / Thought: 속마음 (괄호 + 기울임 + 흐린 색)")]
    public LineStyle style = LineStyle.Normal;

    [Tooltip("체크하면 이 대사가 끝까지 나온 뒤 키를 안 눌러도 바로 다음 줄로 넘어감 (말이 끊기는 연출: \"그게 무슨—\")")]
    public bool interrupted;

    [Tooltip("이 줄을 보여주기 전에 기다릴 시간(초). 글리치 같은 연출이 끝나길 기다릴 때")]
    public float waitBefore;

    [Tooltip("이 줄이 시작될 때 실행할 동작 (예: GlitchTrigger.Play)")]
    public UnityEvent onLineStart;

    [Tooltip("이 대사가 끝나면 보여줄 선택지 (없으면 비워두기)")]
    public DialogueChoice[] choices;
}

public enum LineStyle { Normal, Thought }

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] float charsPerSecond = 30f;
    [SerializeField] float interruptDelay = 0.35f;   // 끊기는 대사가 끝나고 다음 줄로 넘어가기까지

    static readonly Color NormalColor = Color.white;
    static readonly Color ThoughtColor = new Color(0.7f, 0.75f, 0.85f);

    public bool IsOpen { get; private set; }

    // UI
    GameObject root;
    Text nameText;
    Text bodyText;
    Text nextHint;
    GameObject choiceRoot;
    readonly Text[] choiceTexts = new Text[4];

    // 진행 상태
    DialogueLine[] lines;
    int index;
    bool typing;
    int openedFrame;
    Coroutine typeRoutine;
    Action onEnd;

    // 선택지 상태
    bool choosing;
    int selected;
    readonly List<DialogueChoice> visibleChoices = new List<DialogueChoice>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("DialogueManager").AddComponent<DialogueManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    // ───────── 대화 시작 ─────────
    public void StartDialogue(DialogueLine[] newLines, Action whenFinished = null)
    {
        if (newLines == null || newLines.Length == 0) return;

        lines = newLines;
        index = 0;
        onEnd = whenFinished;
        IsOpen = true;
        openedFrame = Time.frameCount;
        root.SetActive(true);

        SetPlayerControl(false);
        ShowLine();
    }

    // ───────── 입력 ─────────
    void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool confirm = kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;

        if (choosing)
        {
            // W/S 또는 ↑/↓ 로 고르고, E/Space/Enter 로 결정
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) MoveSelection(-1);
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) MoveSelection(1);
            if (confirm) Choose(visibleChoices[selected]);
            return;
        }

        if (confirm)
        {
            if (typing) CompleteLine();
            else NextLine();
        }
    }

    // ───────── 대사 출력 ─────────
    void ShowLine()
    {
        HideChoices();

        var line = lines[index];
        nameText.text = line.speaker;
        nameText.gameObject.SetActive(!string.IsNullOrEmpty(line.speaker));

        bool thought = line.style == LineStyle.Thought;
        bodyText.fontStyle = thought ? FontStyle.Italic : FontStyle.Normal;
        bodyText.color = thought ? ThoughtColor : NormalColor;

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(PlayLine(line));
    }

    // 화면에 보일 글자 (속마음은 괄호로 감쌈)
    static string DisplayText(DialogueLine line)
    {
        string t = line.text ?? "";
        return line.style == LineStyle.Thought ? "(" + t + ")" : t;
    }

    IEnumerator PlayLine(DialogueLine line)
    {
        typing = true;
        nextHint.enabled = false;
        bodyText.text = "";

        line.onLineStart?.Invoke();                       // 줄 시작 연출 (글리치 등)
        if (line.waitBefore > 0f) yield return new WaitForSeconds(line.waitBefore);

        foreach (char c in DisplayText(line))
        {
            bodyText.text += c;
            yield return new WaitForSeconds(1f / charsPerSecond);
        }

        OnLineFinished();
    }

    void CompleteLine()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        bodyText.text = DisplayText(lines[index]);
        OnLineFinished();
    }

    // 한 줄이 다 나왔을 때: 선택지가 있으면 보여주고, 없으면 ▼ 표시
    void OnLineFinished()
    {
        typing = false;
        if (lines[index].interrupted)
        {
            StartCoroutine(AutoNext(index));   // 끊기는 대사: 키 없이 다음 줄로
            return;
        }
        if (!TryShowChoices()) nextHint.enabled = true;
    }

    IEnumerator AutoNext(int lineIndex)
    {
        yield return new WaitForSeconds(interruptDelay);
        if (IsOpen && index == lineIndex && !typing && !choosing) NextLine();
    }

    void NextLine()
    {
        index++;
        if (index >= lines.Length) EndDialogue();
        else ShowLine();
    }

    // ───────── 선택지 ─────────
    bool TryShowChoices()
    {
        var choices = lines[index].choices;
        if (choices == null || choices.Length == 0) return false;

        visibleChoices.Clear();
        foreach (var c in choices)
        {
            // 조건 플래그가 없거나, 플래그가 켜져 있을 때만 보여줌
            if (string.IsNullOrEmpty(c.requiredFlag) || GameFlags.Has(c.requiredFlag))
                visibleChoices.Add(c);
            if (visibleChoices.Count == choiceTexts.Length) break;
        }
        if (visibleChoices.Count == 0) return false;

        choosing = true;
        selected = 0;
        nextHint.enabled = false;
        choiceRoot.SetActive(true);
        RefreshChoices();
        return true;
    }

    void MoveSelection(int dir)
    {
        selected = (selected + dir + visibleChoices.Count) % visibleChoices.Count;
        RefreshChoices();
    }

    void RefreshChoices()
    {
        for (int i = 0; i < choiceTexts.Length; i++)
        {
            bool show = i < visibleChoices.Count;
            choiceTexts[i].gameObject.SetActive(show);
            if (!show) continue;

            bool isSel = i == selected;
            choiceTexts[i].text = (isSel ? "▶ " : "   ") + visibleChoices[i].text;
            choiceTexts[i].color = isSel ? new Color(1f, 0.85f, 0.4f) : Color.white;
        }
    }

    void HideChoices()
    {
        choosing = false;
        choiceRoot.SetActive(false);
    }

    void Choose(DialogueChoice choice)
    {
        HideChoices();
        EndDialogue();                       // 지금 대화를 닫고

        GameFlags.Set(choice.setFlag);       // 플래그 켜고
        choice.onChosen?.Invoke();           // 연결된 동작 실행하고
        if (choice.next != null)             // 이어지는 대화가 있으면 시작
            choice.next.Play();
    }

    // ───────── 대화 끝 ─────────
    void EndDialogue()
    {
        IsOpen = false;
        root.SetActive(false);
        StartCoroutine(EnablePlayerNextFrame());

        var callback = onEnd;
        onEnd = null;
        callback?.Invoke();
    }

    IEnumerator EnablePlayerNextFrame()
    {
        yield return null;
        if (!IsOpen) SetPlayerControl(true);   // 바로 다음 대화가 이어지면 계속 잠금
    }

    void SetPlayerControl(bool on)
    {
        PlayerController.SetLock(this, !on);
    }

    // ───────── UI 만들기 ─────────
    void BuildUI()
    {
        root = new GameObject("DialogueCanvas");
        root.transform.SetParent(transform, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 대사 상자
        var panel = new GameObject("Panel").AddComponent<Image>();
        panel.transform.SetParent(root.transform, false);
        panel.color = new Color(0f, 0f, 0f, 0.85f);
        SetAnchors(panel.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.30f));

        nameText = CreateText("Name", panel.transform, font, 40, FontStyle.Bold,
            new Color(1f, 0.85f, 0.4f), new Vector2(0.03f, 0.72f), new Vector2(0.97f, 0.95f), TextAnchor.UpperLeft);

        bodyText = CreateText("Body", panel.transform, font, 36, FontStyle.Normal,
            Color.white, new Vector2(0.03f, 0.10f), new Vector2(0.97f, 0.70f), TextAnchor.UpperLeft);

        nextHint = CreateText("NextHint", panel.transform, font, 30, FontStyle.Normal,
            new Color(1f, 1f, 1f, 0.7f), new Vector2(0.90f, 0.03f), new Vector2(0.98f, 0.20f), TextAnchor.LowerRight);
        nextHint.text = "▼";

        // 선택지 상자 (대사 상자 오른쪽 위)
        var choicePanel = new GameObject("ChoicePanel").AddComponent<Image>();
        choicePanel.transform.SetParent(root.transform, false);
        choicePanel.color = new Color(0f, 0f, 0f, 0.85f);
        SetAnchors(choicePanel.rectTransform, new Vector2(0.60f, 0.32f), new Vector2(0.95f, 0.62f));
        choiceRoot = choicePanel.gameObject;

        for (int i = 0; i < choiceTexts.Length; i++)
        {
            float top = 0.95f - i * 0.23f;
            choiceTexts[i] = CreateText("Choice" + i, choicePanel.transform, font, 34, FontStyle.Normal,
                Color.white, new Vector2(0.05f, top - 0.22f), new Vector2(0.95f, top), TextAnchor.MiddleLeft);
        }

        choiceRoot.SetActive(false);
        root.SetActive(false);
    }

    Text CreateText(string objName, Transform parent, Font font, int size, FontStyle style,
                    Color color, Vector2 min, Vector2 max, TextAnchor align)
    {
        var t = new GameObject(objName).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        SetAnchors(t.rectTransform, min, max);
        return t;
    }

    static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}