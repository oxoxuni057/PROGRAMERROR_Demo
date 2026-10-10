using UnityEngine;
using UnityEngine.UI;

// 조사할 수 있는 물체 앞에 서면 물체 위에 "E 조사하기"를 띄움
// 게임 시작 시 자동 생성 (씬에 배치할 필요 없음)
// 띄울 글자는 각 물체의 Interactable → Prompt 칸 (비워 두면 안 띄움)
public class InteractPrompt : MonoBehaviour
{
    public static InteractPrompt Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null) new GameObject("InteractPrompt").AddComponent<InteractPrompt>();
    }

    const float OffsetY = 12f;   // 물체 윗부분에서 얼마나 위에 띄울지 (화면 기준)

    RectTransform canvasRect;
    RectTransform box;
    Text label;

    PlayerInteractor interactor;
    PlayerController player;
    string shownText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        box.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void LateUpdate()
    {
        // 씬이 바뀌면 플레이어를 다시 찾음
        if (interactor == null)
        {
            interactor = FindAnyObjectByType<PlayerInteractor>();
            player = interactor != null ? interactor.GetComponent<PlayerController>() : null;
        }

        var target = interactor != null ? interactor.Current : null;
        var cam = Camera.main;

        // 대화·지침서·씬 이동·ROLLBACK 중(=움직일 수 없을 때)에는 숨김
        bool show = target != null && player != null && player.CanMove
                    && !string.IsNullOrEmpty(target.Prompt) && cam != null;
        if (box.gameObject.activeSelf != show) box.gameObject.SetActive(show);
        if (!show) return;

        string text = "<color=#FFD54A>E</color>   " + target.Prompt;
        if (text != shownText)
        {
            shownText = text;
            label.text = text;
            box.sizeDelta = new Vector2(label.preferredWidth + 36f, 46f);
        }

        // 물체 머리 위 위치를 화면 좌표로 바꿔서 그 자리에 표시
        Vector2 screen = cam.WorldToScreenPoint(TopOf(target));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
        box.anchoredPosition = local + new Vector2(0f, OffsetY);
    }

    static Vector3 TopOf(Interactable it)
    {
        var col = it.GetComponentInChildren<Collider2D>();
        if (col != null) return new Vector3(col.bounds.center.x, col.bounds.max.y, 0f);
        return it.transform.position;
    }

    // ───── UI 코드로 만들기 ─────
    void BuildUI()
    {
        var canvasGO = new GameObject("InteractPromptCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;   // 지침서(400)·대화창(500)보다 아래
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = canvasGO.GetComponent<RectTransform>();

        // 반투명 검은 상자
        var boxGO = new GameObject("Box", typeof(RectTransform), typeof(Image));
        boxGO.transform.SetParent(canvasGO.transform, false);
        var img = boxGO.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.75f);
        img.raycastTarget = false;
        box = boxGO.GetComponent<RectTransform>();
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
        box.pivot = new Vector2(0.5f, 0f);   // 상자 아래쪽 가운데가 물체 머리 위에 오도록

        // 글자
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(boxGO.transform, false);
        label = labelGO.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 26;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = true;
        label.raycastTarget = false;
        var rt = label.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
