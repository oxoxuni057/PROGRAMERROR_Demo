using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }  // 어디서든 SceneTransition.Instance로 부를 수 있음

    [SerializeField] float fadeDuration = 0.5f;   // 어두워지는 데 걸리는 시간(초)

    CanvasGroup fade;      // 검은 화면의 투명도 조절용
    bool busy;             // 이동 중에 또 누르는 것 방지
    string nextSpawnId;    // 다음 씬에서 나타날 위치 이름

    // ① 게임이 시작될 때 자동으로 딱 하나 만들어짐 (씬에 직접 배치할 필요 없음)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("SceneTransition").AddComponent<SceneTransition>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // ② 씬이 바뀌어도 사라지지 않음
        BuildFadeCanvas();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ③ 문에서 이 함수를 부름
    public void LoadScene(string sceneName, string spawnId = "")
    {
        if (!busy) StartCoroutine(LoadRoutine(sceneName, spawnId));
    }

    IEnumerator LoadRoutine(string sceneName, string spawnId)
    {
        busy = true;
        SetPlayerControl(false);          // 못 움직이게
        yield return Fade(1f);            // 화면 까맣게

        nextSpawnId = spawnId;
        yield return SceneManager.LoadSceneAsync(sceneName);   // 씬 이동
        SetPlayerControl(false);          // 새 씬의 플레이어도 잠깐 잠금

        yield return Fade(0f);            // 화면 다시 밝게
        SetPlayerControl(true);           // 다시 움직일 수 있게
        busy = false;
    }

    // ④ 새 씬이 열리면 플레이어를 지정한 SpawnPoint 위치로 옮김
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(nextSpawnId)) return;

        var player = FindAnyObjectByType<PlayerController>();
        foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp.Id == nextSpawnId && player != null)
            {
                player.transform.position = sp.transform.position;
                break;
            }
        }
        nextSpawnId = null;
    }

    IEnumerator Fade(float target)
    {
        float start = fade.alpha;
        float t = 0f;
        fade.blocksRaycasts = true;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;   // 한 프레임 기다렸다가 계속
        }
        fade.alpha = target;
        fade.blocksRaycasts = target > 0.5f;
    }

    void SetPlayerControl(bool on)
    {
        PlayerController.SetLock(this, !on);
    }

    // ⑤ 화면 전체를 덮는 검은 이미지를 코드로 만듦
    void BuildFadeCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;   // 모든 UI보다 위에

        fade = gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0f;              // 처음엔 투명
        fade.blocksRaycasts = false;

        var img = new GameObject("Black").AddComponent<Image>();
        img.transform.SetParent(transform, false);
        img.color = Color.black;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}