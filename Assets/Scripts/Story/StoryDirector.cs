using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 이야기 진행 담당: 게임이 시작될 때 자동으로 하나 만들어짐 (씬에 배치할 필요 없음)
//
// ① 씬에 들어가면 대사 자동 시작
//    파일: Assets/Resources/Dialogue/scene_start.csv  (CSV UTF-8)
//    열:  씬 | 조건 | 대사 | 컷신배경
//     - 씬     : 씬 이름 (예: 18관_3층)
//     - 조건   : 이 플래그가 켜져 있을 때만 (여러 개는 띄어쓰기, 꺼져 있어야 하면 !플래그). 비우면 항상
//     - 대사   : 대사 표(dialogue.csv)의 id. 한 번 나온 대사는 다시 안 나옴 (타이틀로 돌아가면 초기화)
//     - 컷신배경 : "예"면 맵을 가리는 검은 배경 위에 대사가 나옴 (오프닝처럼). 대사 표의 cutscene_off나 wake로 걷힘
//
// ② 대사 표(dialogue.csv)의 실행 칸에서 쓰는 명령 (DialogueTable이 부름)
//     scene:씬이름  scene:씬이름/도착이름   다른 씬으로 이동
//     cutscene_on / cutscene_off              검은 컷신 배경 켜기·끄기
//     wake:초                                 N초 뒤 PROGRAM ERROR 암전과 컷신 배경을 걷음 (점검 세계로 깨어남)
//     checkpoint                              지금 위치·플래그·지침서를 체크포인트로 저장
public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance { get; private set; }

    const string ResourcePath = "Dialogue/scene_start";
    const float StartDelay = 0.6f;   // 씬이 밝아진 뒤 대사가 나올 때까지 (SceneTransition 페이드 0.5초)

    class Row { public string scene, cond, talkId; public bool cutscene; }

    static List<Row> rows;
    static readonly HashSet<string> played = new HashSet<string>();   // 이미 나온 시작 대사

    Image cutsceneBlack;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { rows = null; played.Clear(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("StoryDirector").AddComponent<StoryDirector>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCanvas();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    // ───── ① 씬 시작 대사 ─────

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Title") { played.Clear(); SetCutscene(false); return; }   // 새로 시작하면 처음부터

        Load();
        foreach (var r in rows)
        {
            if (r.scene != scene.name || played.Contains(r.talkId)) continue;
            if (!DialogueTable.CheckCondition(r.cond)) continue;

            played.Add(r.talkId);
            if (r.cutscene) SetCutscene(true);   // 씬이 밝아지기 전에 바로 가림
            StartCoroutine(PlayLater(r.talkId));
            return;   // 한 씬에 한 번에 하나만
        }
    }

    IEnumerator PlayLater(string talkId)
    {
        yield return new WaitForSecondsRealtime(StartDelay);
        // 다른 대화가 열려 있으면 끝날 때까지 기다림
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) yield return null;
        Debug.Log($"[이야기] 시작 대사: {talkId}");
        DialogueTable.Play(talkId);
    }

    // ───── ② 대사 표 명령 ─────

    public static void SetCutscene(bool on)
    {
        if (Instance != null) Instance.cutsceneBlack.enabled = on;
    }

    public static void Wake(float seconds)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.WakeRoutine(seconds));
    }

    IEnumerator WakeRoutine(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        SetCutscene(false);
        if (GlitchEffect.Instance != null) GlitchEffect.Instance.HideBlackout();
    }

    // "씬이름" 또는 "씬이름/도착이름"
    public static void GoToScene(string arg)
    {
        if (string.IsNullOrEmpty(arg) || SceneTransition.Instance == null) return;
        int slash = arg.IndexOf('/');
        string sceneName = slash < 0 ? arg : arg.Substring(0, slash);
        string spawn = slash < 0 ? "" : arg.Substring(slash + 1);
        SceneTransition.Instance.LoadScene(sceneName.Trim(), spawn.Trim());
    }

    public static void SaveCheckpoint()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) Rollback.SetCheckpoint(p.transform.position);
    }

    // ───── 화면 ─────

    // 대화창(500) 아래, 맵 위에 깔리는 검은 배경
    void BuildCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;

        var go = new GameObject("컷신 배경");
        go.transform.SetParent(transform, false);
        cutsceneBlack = go.AddComponent<Image>();
        cutsceneBlack.color = Color.black;
        cutsceneBlack.raycastTarget = false;
        var rt = cutsceneBlack.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        cutsceneBlack.enabled = false;
    }

    // ───── 파일 읽기 ─────

    static void Load()
    {
        if (rows != null) return;
        rows = new List<Row>();
        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null) { Debug.LogWarning("[이야기] Assets/Resources/Dialogue/scene_start.csv 파일이 없어요"); return; }

        var records = DialogueTable.ParseCsv(asset.text);
        for (int i = 1; i < records.Count; i++)   // 첫 줄은 제목 줄
        {
            var rec = records[i];
            string Get(int c) => c < rec.Count ? rec[c].Trim() : "";
            string scene = Get(0);
            if (scene == "" || scene.StartsWith("#") || Get(2) == "") continue;
            rows.Add(new Row { scene = scene, cond = Get(1), talkId = Get(2), cutscene = Get(3) == "예" });
        }
    }
}
