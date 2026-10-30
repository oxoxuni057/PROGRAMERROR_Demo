using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 화면 색 필터: 정상 세계 / 점검 세계(누런 겨자색) / 2층(더 어둡게)
// 오프닝의 PROGRAM ERROR 암전 중에 ScreenFilter.EnterInspection() → 다시 켜지면 점검 세계 색
// 그 뒤로는 씬이 바뀌어도 알아서 점검 색 (2층 씬만 2층 색), ROLLBACK으로 되돌아가면 색도 같이 되돌아감
public class ScreenFilter : MonoBehaviour
{
    public static ScreenFilter Instance { get; private set; }

    public const string InspectionFlag = "점검세계";   // 이 플래그가 켜져 있으면 점검 세계

    public enum Look { Normal, Inspection, Floor2 }

    // 색 하나에 들어가는 값들
    struct Preset
    {
        public Color filter;      // 화면에 곱하는 색 (흰색 = 그대로)
        public float saturation;  // 채도 (-100 흑백 ~ 0 그대로)
        public float contrast;    // 대비
        public float exposure;    // 밝기 (음수면 어둡게)
        public float vignette;    // 가장자리 어둡게 (0~1)
        public float grain;       // 필름 노이즈 (0~1)
    }

    static readonly Preset NormalPreset = new Preset
    {
        filter = Color.white, saturation = 0f, contrast = 0f, exposure = 0f, vignette = 0.15f, grain = 0f,
    };
    static readonly Preset InspectionPreset = new Preset   // 무드보드 겨자색 #C9B65A 쪽으로
    {
        filter = new Color(0.95f, 0.89f, 0.62f), saturation = -35f, contrast = 10f, exposure = -0.1f, vignette = 0.35f, grain = 0.3f,
    };
    static readonly Preset Floor2Preset = new Preset       // 2층: 어두운 갈색·올리브
    {
        filter = new Color(0.78f, 0.70f, 0.48f), saturation = -45f, contrast = 15f, exposure = -0.45f, vignette = 0.5f, grain = 0.4f,
    };

    ColorAdjustments color;
    Vignette vignette;
    FilmGrain grain;
    Preset current = NormalPreset;
    Coroutine fading;
    Camera lastCamera;

    public Look CurrentLook { get; private set; } = Look.Normal;

    // ① 게임이 시작될 때 자동으로 딱 하나 만들어짐 (씬에 직접 배치할 필요 없음)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("ScreenFilter").AddComponent<ScreenFilter>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildVolume();
        Apply(NormalPreset);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Rollback.OnRollback += Refresh;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Rollback.OnRollback -= Refresh;
    }

    // ② 점검 세계 시작: 플래그 켜고 바로 색 바꿈 (암전 중에 부르면 켜질 때부터 누런 화면)
    public static void EnterInspection()
    {
        GameFlags.Set(InspectionFlag);
        if (Instance != null) Instance.Refresh();
    }

    // ③ 이벤트에서 직접 색 바꾸기 (seconds 동안 서서히). 다음 씬으로 넘어가면 다시 자동 색으로
    public static void Set(Look look, float seconds = 0f)
    {
        if (Instance == null) return;
        Instance.FadeTo(look, seconds);
    }

    // ④ 지금 상황(플래그, 씬 이름)에 맞는 색으로 바로 맞춤
    public void Refresh()
    {
        FadeTo(AutoLook(), 0f);
    }

    static Look AutoLook()
    {
        if (!GameFlags.Has(InspectionFlag)) return Look.Normal;
        return SceneManager.GetActiveScene().name.Contains("_2층") ? Look.Floor2 : Look.Inspection;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Refresh();
    }

    void FadeTo(Look look, float seconds)
    {
        CurrentLook = look;
        if (fading != null) StopCoroutine(fading);
        var target = PresetOf(look);
        if (seconds <= 0f) { Apply(target); return; }
        fading = StartCoroutine(FadeRoutine(current, target, seconds));
    }

    IEnumerator FadeRoutine(Preset from, Preset to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            Apply(Lerp(from, to, t / seconds));
            yield return null;
        }
        Apply(to);
        fading = null;
    }

    static Preset PresetOf(Look look)
    {
        switch (look)
        {
            case Look.Inspection: return InspectionPreset;
            case Look.Floor2: return Floor2Preset;
            default: return NormalPreset;
        }
    }

    static Preset Lerp(Preset a, Preset b, float t)
    {
        return new Preset
        {
            filter = Color.Lerp(a.filter, b.filter, t),
            saturation = Mathf.Lerp(a.saturation, b.saturation, t),
            contrast = Mathf.Lerp(a.contrast, b.contrast, t),
            exposure = Mathf.Lerp(a.exposure, b.exposure, t),
            vignette = Mathf.Lerp(a.vignette, b.vignette, t),
            grain = Mathf.Lerp(a.grain, b.grain, t),
        };
    }

    void Apply(Preset p)
    {
        current = p;
        color.colorFilter.Override(p.filter);
        color.saturation.Override(p.saturation);
        color.contrast.Override(p.contrast);
        color.postExposure.Override(p.exposure);
        vignette.intensity.Override(p.vignette);
        grain.intensity.Override(p.grain);
        grain.active = p.grain > 0f;
    }

    // 카메라가 바뀌면 그 카메라의 화면 효과(Post Processing)를 켜 줌 (씬마다 체크 안 해도 되게)
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || cam == lastCamera) return;
        lastCamera = cam;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    // 화면 전체에 걸리는 Volume을 코드로 만듦
    void BuildVolume()
    {
        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;   // 씬에 다른 Volume이 있어도 이게 우선

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        color = profile.Add<ColorAdjustments>(true);
        vignette = profile.Add<Vignette>(true);
        vignette.color.Override(Color.black);
        vignette.smoothness.Override(0.45f);
        grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium3);
        grain.response.Override(0.6f);
        volume.profile = profile;
    }
}
