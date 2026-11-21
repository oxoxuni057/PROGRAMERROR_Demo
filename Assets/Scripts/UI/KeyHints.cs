using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 화면 왼쪽 위에 지금 누를 수 있는 키를 작게 보여 줌 (게임이 시작될 때 자동으로 만들어짐)
//  - 걸어 다닐 때 / 대화 중 / 지침서를 볼 때 내용이 바뀜
//  - 타이틀 화면, 일시정지 중에는 숨김
//  - H 키로 켜고 끄기
public class KeyHints : MonoBehaviour
{
    public static KeyHints Instance { get; private set; }

    const string Walk = "WASD·방향키 이동    Shift 달리기    E 조사·대화    Tab 지침서    Esc 일시정지    H 도움말 숨기기";
    const string Talk = "E·Space 다음    W·S 선택지 고르기";
    const string Book = "A·D 쪽 넘기기    Tab·Esc 닫기";

    Text label;
    bool hidden;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("KeyHints").AddComponent<KeyHints>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.hKey.wasPressedThisFrame) hidden = !hidden;

        string text = null;
        bool inTitle = SceneManager.GetActiveScene().name == "Title";
        if (!hidden && !inTitle && Time.timeScale > 0f)
        {
            if (RuleBook.IsOpen) text = Book;
            else if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) text = Talk;
            else text = Walk;
        }

        label.enabled = text != null;
        if (text != null && label.text != text) label.text = text;
    }

    void BuildUI()
    {
        var canvas = MenuList.MakeCanvas(transform, "KeyHintsCanvas", 550);   // 대화창(500) 위, 글리치(900) 아래
        var go = new GameObject("Hint", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(canvas.transform, false);
        label = go.GetComponent<Text>();
        label.font = GameFont.Regular;
        label.fontSize = 22;
        label.color = new Color(0.85f, 0.85f, 0.8f, 0.75f);
        label.alignment = TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);

        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -18f);
        rt.sizeDelta = new Vector2(1600f, 40f);
    }
}
