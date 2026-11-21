using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 효과음 재생기: 게임이 시작될 때 자동으로 하나 만들어짐 (씬에 배치할 필요 없음)
//
// 자동으로 나는 소리
//  - 점검 세계 배경 잡음: 색 필터가 점검/2층이면 ambience_static이 서서히 커지고, 정상 세계면 꺼짐 (2층은 더 크게)
//  - 발소리: 플레이어가 걸으면 footsteps_walk_loop, 달리면(Shift) footsteps_run_loop. 대화·컷신으로 못 움직일 땐 안 남
//  - 글리치 소리: GlitchEffect가 치지직할 때 glitch_short, PROGRAM ERROR 때 glitch_big (GlitchEffect에서 부름)
//
// 직접 부르기 (이름은 Assets/Resources/Sound/SoundLibrary.asset에 적힌 이름)
//  GameSound.Play("door_open");                 한 번 재생
//  GameSound.StartLoop("light_hum_loop", 0.5f);  계속 반복 (같은 이름으로 StopLoop 하면 꺼짐)
//  GameSound.StopLoop("light_hum_loop");
//  Inspector에서 쓰려면 SoundTrigger 부품을 쓰기
public class GameSound : MonoBehaviour
{
    public static GameSound Instance { get; private set; }

    const string LibraryPath = "Sound/SoundLibrary";
    const float FadeSpeed = 1.5f;   // 반복 소리가 커지고 작아지는 속도 (1초에 1.5만큼)

    [Header("점검 세계 배경 잡음")]
    public string ambienceSound = "ambience_static";
    [Range(0f, 1f)] public float ambienceInspection = 0.25f;   // 점검 세계 크기
    [Range(0f, 1f)] public float ambienceFloor2 = 0.4f;        // 2층 크기

    [Header("발소리")]
    public string walkSound = "footsteps_walk_loop";
    public string runSound = "footsteps_run_loop";
    [Range(0f, 1f)] public float footstepVolume = 0.45f;

    [Header("전체")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    // 이름이 붙은 반복 소리 하나 (배경 잡음, 형광등 웅웅 등)
    class Loop
    {
        public AudioSource source;
        public float target;   // 목표 크기 (0이면 서서히 꺼지고 멈춤)
    }

    SoundLibrary library;
    AudioSource oneShot;
    AudioSource footsteps;
    readonly Dictionary<string, Loop> loops = new Dictionary<string, Loop>();
    PlayerController player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("GameSound").AddComponent<GameSound>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        library = Resources.Load<SoundLibrary>(LibraryPath);
        if (library == null) Debug.LogWarning("[효과음] Assets/Resources/Sound/SoundLibrary.asset이 없어요");

        oneShot = MakeSource("한 번 재생", false);
        footsteps = MakeSource("발소리", true);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => player = null;   // 새 씬의 플레이어를 다시 찾게

    AudioSource MakeSource(string label, bool loop)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;   // 2D 소리 (거리와 상관없이 같은 크기)
        return s;
    }

    // ───── 직접 부르는 기능 ─────

    public static AudioClip Clip(string soundName)
    {
        if (Instance == null || Instance.library == null) return null;
        var clip = Instance.library.Find(soundName);
        if (clip == null) Debug.LogWarning($"[효과음] 이름표에 '{soundName}'이 없어요");
        return clip;
    }

    public static void Play(string soundName, float volume = 1f) => Play(Clip(soundName), volume);

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (Instance == null || clip == null) return;
        Instance.oneShot.PlayOneShot(clip, volume * Instance.masterVolume);
    }

    // 반복 소리 켜기. fadeSeconds 동안 서서히 커짐 (0이면 바로)
    public static void StartLoop(string soundName, float volume = 1f, float fadeSeconds = 1f)
    {
        if (Instance == null) return;
        Instance.SetLoop(soundName, volume, fadeSeconds);
    }

    // 반복 소리 끄기. 서서히 작아지다 멈춤
    public static void StopLoop(string soundName, float fadeSeconds = 1f)
    {
        if (Instance == null) return;
        Instance.SetLoop(soundName, 0f, fadeSeconds);
    }

    void SetLoop(string soundName, float volume, float fadeSeconds)
    {
        if (!loops.TryGetValue(soundName, out var loop))
        {
            if (volume <= 0f) return;   // 켜진 적 없는 소리를 끄라고 하면 무시
            var clip = Clip(soundName);
            if (clip == null) return;
            loop = new Loop { source = MakeSource("반복 " + soundName, true) };
            loop.source.clip = clip;
            loop.source.volume = 0f;
            loops[soundName] = loop;
        }
        loop.target = volume;
        if (volume > 0f && !loop.source.isPlaying) loop.source.Play();
        if (fadeSeconds <= 0f) loop.source.volume = volume * masterVolume;
    }

    // ───── 매 프레임 ─────

    void Update()
    {
        EnsureListener();
        UpdateAmbience();
        UpdateFootsteps();
        FadeLoops();
    }

    // 소리를 듣는 귀(AudioListener)가 씬에 하나도 없으면 소리가 안 나서, 없을 때만 GameSound에 임시로 붙임
    // 카메라 쪽에 귀가 생기면 임시 귀는 뗌 (귀가 둘이면 Unity가 경고를 띄움)
    AudioListener ownListener;
    float listenerCheck;

    void EnsureListener()
    {
        listenerCheck -= Time.unscaledDeltaTime;
        if (listenerCheck > 0f) return;
        listenerCheck = 0.5f;

        bool other = false;
        foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (l != ownListener && l.isActiveAndEnabled) { other = true; break; }

        if (other && ownListener != null) { Destroy(ownListener); ownListener = null; }
        else if (!other && ownListener == null) ownListener = gameObject.AddComponent<AudioListener>();
    }

    // 색 필터를 보고 배경 잡음 크기를 정함
    void UpdateAmbience()
    {
        if (ScreenFilter.Instance == null || string.IsNullOrEmpty(ambienceSound)) return;
        float target = 0f;
        switch (ScreenFilter.Instance.CurrentLook)
        {
            case ScreenFilter.Look.Inspection: target = ambienceInspection; break;
            case ScreenFilter.Look.Floor2: target = ambienceFloor2; break;
        }
        if (target > 0f || loops.ContainsKey(ambienceSound)) SetLoop(ambienceSound, target, 1f);
    }

    void UpdateFootsteps()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        bool walking = player != null && player.isActiveAndEnabled && player.CanMove && player.IsMoving && Time.timeScale > 0f;
        if (!walking)
        {
            if (footsteps.isPlaying) footsteps.Stop();
            return;
        }

        var clip = Clip(player.IsRunning ? runSound : walkSound);
        if (clip == null) return;
        if (footsteps.clip != clip) { footsteps.clip = clip; footsteps.Play(); }
        if (!footsteps.isPlaying) footsteps.Play();
        footsteps.volume = footstepVolume * masterVolume;
    }

    void FadeLoops()
    {
        float step = FadeSpeed * Time.unscaledDeltaTime;
        foreach (var loop in loops.Values)
        {
            float goal = loop.target * masterVolume;
            loop.source.volume = Mathf.MoveTowards(loop.source.volume, goal, step);
            if (goal <= 0f && loop.source.volume <= 0f && loop.source.isPlaying) loop.source.Stop();
        }
    }
}
