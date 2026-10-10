using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 일시정지 메뉴: 플레이 중 Esc → 계속하기 / 타이틀로 / 종료 (자동 생성)
// 대화·지침서·씬 이동·ROLLBACK 중에는 열리지 않음. 타이틀 씬은 Build Profiles의 맨 위(0번) 씬
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null) new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    GameObject root;
    MenuList menu;
    bool paused;
    int openedFrame;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = MenuList.MakeCanvas(transform, "PauseCanvas", 600);   // 대화창(500) 위, 글리치(900) 아래
        root = MenuList.MakeFullImage(canvas.transform, new Color(0f, 0f, 0f, 0.7f)).gameObject;
        MenuList.MakeText(root.transform, font, "일시정지", 56, 180, new Color(0.93f, 0.93f, 0.9f));
        menu = new MenuList(root.transform, font, new[] { "계속하기", "타이틀로", "종료" }, 38, 20, 75);
        root.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (!paused)
        {
            if (kb.escapeKey.wasPressedThisFrame && CanPause()) Pause();
            return;
        }

        if (Time.frameCount == openedFrame) return;
        if (kb.escapeKey.wasPressedThisFrame) { Resume(); return; }

        int chosen = menu.HandleInput(kb);
        if (chosen == 0) Resume();
        else if (chosen == 1) GoToTitle();
        else if (chosen == 2) MenuList.QuitGame();
    }

    bool CanPause()
    {
        if (Time.frameCount == RuleBook.ClosedFrame) return false;   // 같은 Esc로 지침서를 닫은 경우
        var p = FindAnyObjectByType<PlayerController>();
        return p != null && p.CanMove;   // 플레이어가 자유롭게 움직일 수 있을 때만
    }

    void Pause()
    {
        paused = true;
        openedFrame = Time.frameCount;
        Time.timeScale = 0f;
        PlayerController.SetLock(this, true);
        menu.Reset();
        root.SetActive(true);
    }

    void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        PlayerController.SetLock(this, false);
        root.SetActive(false);
    }

    void GoToTitle()
    {
        Resume();
        string path = SceneUtility.GetScenePathByBuildIndex(0);
        string titleScene = System.IO.Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrEmpty(titleScene)) { Debug.LogWarning("[일시정지] Build Profiles 0번 씬이 없어요"); return; }
        SceneTransition.Instance.LoadScene(titleScene);
    }
}
