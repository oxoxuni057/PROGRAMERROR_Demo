using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// [ROLLBACK]: 규칙 위반 → 치지직 + [ROLLBACK] → 암전 → 마지막 체크포인트로 되돌아감
// 어디서든 Rollback.Play("이유") 한 줄로 실행 (규칙 판정 쪽에서 이걸 부르면 됨)
// 처음 버전: 윤영옥 (feature/event) / 상태 되돌리기·잠금 정리: 배수빈
public class Rollback : MonoBehaviour
{
    public static Rollback Instance { get; private set; }
    public static event Action OnCheckpoint;  // 체크포인트가 저장된 직후 알림 (각자 "지금 상태" 저장)
    public static event Action OnRollback;    // 되돌아간 직후 알림 (각자 "저장한 상태"로 복구)

    [SerializeField] float glitchSeconds = 0.9f;   // 치지직 + [ROLLBACK] 시간
    [SerializeField] float blackSeconds = 0.6f;    // 암전 유지 시간
    [SerializeField] bool checkpointOnSceneEnter = true;  // 씬에 들어온 위치를 자동으로 체크포인트로

    bool busy;                 // 실행 중에 또 불리는 것 방지
    bool hasCheckpoint;        // 체크포인트가 있는지
    string checkpointScene;    // 체크포인트가 있는 씬 이름
    Vector3 checkpointPos;     // 체크포인트 위치

    // 체크포인트 때 저장해 두는 게임 상태
    List<string> savedFlags = new List<string>();
    List<RuleEntry> savedRules = new List<RuleEntry>();

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ② 체크포인트 저장 (Checkpoint 바닥, 씬 입장 시 자동)
    public static void SetCheckpoint(Vector3 position)
    {
        if (Instance == null) return;
        Instance.hasCheckpoint = true;
        Instance.checkpointScene = SceneManager.GetActiveScene().name;
        Instance.checkpointPos = position;

        // 이 순간의 플래그·지침서 상태도 같이 저장
        Instance.savedFlags = GameFlags.Snapshot();
        Instance.savedRules = RuleBook.Snapshot();
        OnCheckpoint?.Invoke();

        Debug.Log($"[체크포인트] {Instance.checkpointScene} {position}");
    }

    // 씬에 들어오면 (ROLLBACK 중이 아닐 때) 들어온 위치를 체크포인트로
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (busy || !checkpointOnSceneEnter) return;
        StartCoroutine(CheckpointNextFrame());
    }

    IEnumerator CheckpointNextFrame()
    {
        yield return null;   // SceneTransition이 SpawnPoint로 옮긴 뒤에 저장
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) SetCheckpoint(p.transform.position);
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
        PlayerController.SetLock(this, true);

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
        }

        // 플래그·지침서를 체크포인트 때 상태로
        if (hasCheckpoint)
        {
            GameFlags.Restore(savedFlags);
            RuleBook.Restore(savedRules);
        }
        OnRollback?.Invoke();

        yield return new WaitForSecondsRealtime(blackSeconds);
        fx.HideBlackout();
        PlayerController.SetLock(this, false);
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
}
