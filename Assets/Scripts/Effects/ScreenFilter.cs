using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 화면 색 필터: 정상 세계 / 점검 세계(차갑고 어두운 청회색, 아날로그 TV 느낌) / 2층(더 어둡게)
// 오프닝의 PROGRAM ERROR 암전 중에 ScreenFilter.EnterInspection() → 다시 켜지면 점검 세계 색
// 그 뒤로는 씬이 바뀌어도 알아서 점검 색 (2층 씬만 2층 색), ROLLBACK으로 되돌아가면 색도 같이 되돌아감
public class ScreenFilter : MonoBehaviour
{
    public static ScreenFilter Instance { get; private set; }

    public const string InspectionFlag = "점검세계";   // 이 플래그가 켜져 있으면 점검 세계
    static readonly int AnalogTVId = Shader.PropertyToID("_AnalogTV");

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
        public float rgbSplit;    // 가장자리 RGB 어긋남 (0~1)
        public float analogTV;    // 아날로그 TV 셰이더 세기 (0~1, Assets/Effects/AnalogTV)
    }

    static readonly Preset NormalPreset = new Preset
    {
        filter = Color.white, saturation = 0f, contrast = 0f, exposure = 0f, vignette = 0.15f, grain = 0f, rgbSplit = 0f, analogTV = 0f,
    };
    static readonly Preset InspectionPreset = new Preset   // 점검: 차갑고 어두운 청회색 + 노이즈 + RGB 어긋남
    {
        filter = new Color(0.70f, 0.86f, 0.92f), saturation = -55f, contrast = 20f, exposure = -0.4f, vignette = 0.45f, grain = 0.9f, rgbSplit = 0.6f, analogTV = 0.5f,
    };
    static readonly Preset Floor2Preset = new Preset       // 2층: 점검보다 더 차갑고 어둡게, RGB 더 많이
    {
        filter = new Color(0.58f, 0.76f, 0.86f), saturation = -65f, contrast = 25f, exposure = -0.8f, vignette = 0.55f, grain = 1f, rgbSplit = 1f, analogTV = 1f,
    };

    // 순간 효과(TV 튐, 쿵쿵, 머물수록 심해짐 등)가 이번 프레임에 더하는 양. 매 프레임 다시 채워짐
    struct Boost { public float tv, rgb, grain, vignette, redEdge; }
    static Boost boost;
    static readonly Color RedEdgeColor = new Color(0.55f, 0f, 0.04f);

    ColorAdjustments color;
    Vignette vignette;
    FilmGrain grain;
    ChromaticAberration rgb;
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
        Shader.SetGlobalFloat(AnalogTVId, 0f);   // 플레이 끝나면 에디터 화면은 원래대로
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

    // ⑤ 효과 부품들이 매 프레임 부르는 것: 이번 프레임에만 효과를 더 세게 (ScreenFx, HeartbeatEdge, StayIntensify 등)
    public static void AddBoost(float tv = 0f, float rgb = 0f, float grain = 0f, float vignette = 0f, float redEdge = 0f)
    {
        boost.tv += tv;
        boost.rgb += rgb;
        boost.grain += grain;
        boost.vignette += vignette;
        boost.redEdge = Mathf.Max(boost.redEdge, redEdge);
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
            rgbSplit = Mathf.Lerp(a.rgbSplit, b.rgbSplit, t),
            analogTV = Mathf.Lerp(a.analogTV, b.analogTV, t),
        };
    }

    void Apply(Preset p)
    {
        current = p;
        Push();
    }

    // 기본 색(current) + 이번 프레임 순간 효과(boost)를 실제 화면에 반영
    void Push()
    {
        var p = current;
        color.colorFilter.Override(p.filter);
        color.saturation.Override(p.saturation);
        color.contrast.Override(p.contrast);
        color.postExposure.Override(p.exposure);

        float red = Mathf.Clamp01(boost.redEdge);
        vignette.intensity.Override(Mathf.Clamp01(p.vignette + boost.vignette + red * 0.35f));
        vignette.color.Override(Color.Lerp(Color.black, RedEdgeColor, red));

        float g = Mathf.Clamp01(p.grain + boost.grain);
        grain.intensity.Override(g);
        grain.active = g > 0f;

        float r = Mathf.Clamp01(p.rgbSplit + boost.rgb);
        rgb.intensity.Override(r);
        rgb.active = r > 0f;

        Shader.SetGlobalFloat(AnalogTVId, Mathf.Clamp01(p.analogTV + boost.tv));
    }

    // 매 프레임: 기본 색 + 순간 효과를 화면에 반영하고, 카메라 확인
    void LateUpdate()
    {
        Push();
        boost = default;   // 순간 효과는 매 프레임 새로 더해져야 유지됨

        // 카메라가 바뀌면 그 카메라의 화면 효과(Post Processing)를 켜 줌 (씬마다 체크 안 해도 되게)
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
        rgb = profile.Add<ChromaticAberration>(true);
        volume.profile = profile;
    }
}
