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

    [Tooltip("이 대사가 끝나면 보여줄 선택지 (없으면 비워두기)")]
    public DialogueChoice[] choices;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] float charsPerSecond = 30f;

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

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeText(line.text));
    }

    IEnumerator TypeText(string text)
    {
        typing = true;
        nextHint.enabled = false;
        bodyText.text = "";

        foreach (char c in text)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(1f / charsPerSecond);
        }

        OnLineFinished();
    }

    void CompleteLine()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        bodyText.text = lines[index].text;
        OnLineFinished();
    }

    // 한 줄이 다 나왔을 때: 선택지가 있으면 보여주고, 없으면 ▼ 표시
    void OnLineFinished()
    {
        typing = false;
        if (!TryShowChoices()) nextHint.enabled = true;
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