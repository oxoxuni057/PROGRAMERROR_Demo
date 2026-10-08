using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// [ROLLBACK]: 규칙 위반 → 치지직 + [ROLLBACK] → 암전 → 마지막 체크포인트로 되돌아감
// 어디서든 Rollback.Play() 한 줄로 실행 (규칙 판정 쪽에서 이걸 부르면 됨)
public class Rollback : MonoBehaviour
{
    public static Rollback Instance { get; private set; }
    public static event Action OnRollback;   // 되돌아간 직후 알림 (EventZone 등이 다시 밟을 수 있게 초기화)

    [SerializeField] float glitchSeconds = 0.9f;   // 치지직 + [ROLLBACK] 시간
    [SerializeField] float blackSeconds = 0.6f;    // 암전 유지 시간

    bool busy;                 // 실행 중에 또 불리는 것 방지
    bool hasCheckpoint;        // 체크포인트를 밟은 적 있는지
    string checkpointScene;    // 체크포인트가 있는 씬 이름
    Vector3 checkpointPos;     // 체크포인트 위치

    public bool IsPlaying => busy;

    // ① 게임이 시작될 때 자동으로 딱 하나 만들어짐 (씬에 직접 배치할 필요 없음)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("Rollback").AddComponent<Rollback>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ② 체크포인트 저장 (Checkpoint가 부름)
    public static void SetCheckpoint(Vector3 position)
    {
        if (Instance == null) return;
        Instance.hasCheckpoint = true;
        Instance.checkpointScene = SceneManager.GetActiveScene().name;
        Instance.checkpointPos = position;
        Debug.Log($"[체크포인트] {Instance.checkpointScene} {position}");
    }

    // ③ ROLLBACK 실행
    public static void Play(string reason = "")
    {
        if (Instance == null || Instance.busy) return;
        if (!string.IsNullOrEmpty(reason)) Debug.Log($"[ROLLBACK] {reason}");
        Instance.StartCoroutine(Instance.PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        busy = true;
        SetPlayerControl(false);

        // 치지직 + [ROLLBACK]
        var fx = GlitchEffect.Instance;
        bool glitchDone = false;
        fx.Play(glitchSeconds, "[ROLLBACK]", 0.25f, () => glitchDone = true);
        float waited = 0f;   // 다른 글리치가 끼어들어도 멈추지 않게 시간 제한
        while (!glitchDone && waited < glitchSeconds + 0.5f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        fx.ShowBlackout();

        // 되돌아가기: 체크포인트가 같은 씬이면 위치만 옮기고, 없거나 다른 씬이면 그 씬을 다시 불러옴
        string current = SceneManager.GetActiveScene().name;
        if (hasCheckpoint && checkpointScene == current)
        {
            MovePlayer(checkpointPos);
        }
        else
        {
            string target = hasCheckpoint ? checkpointScene : current;
            yield return SceneManager.LoadSceneAsync(target);
            if (hasCheckpoint) MovePlayer(checkpointPos);
            SetPlayerControl(false);   // 새로 생긴 플레이어도 잠금
        }

        OnRollback?.Invoke();

        yield return new WaitForSecondsRealtime(blackSeconds);
        fx.HideBlackout();
        SetPlayerControl(true);
        busy = false;
    }

    static void MovePlayer(Vector3 pos)
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p == null) return;
        p.transform.position = pos;
        var rb = p.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = pos;
            rb.linearVelocity = Vector2.zero;
        }
    }

    static void SetPlayerControl(bool on)
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) p.CanMove = on;
    }
}
