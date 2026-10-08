using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]   // 카메라가 플레이어를 따라간 "다음"에 흔들어야 해서 늦게 실행
public class GlitchEffect : MonoBehaviour
{
    public static GlitchEffect Instance { get; private set; }  // 어디서든 GlitchEffect.Instance로 부를 수 있음

    public const string ProgramErrorText = "[PROGRAM ERROR]\n프로그램을 종료합니다.";

    const int BarCount = 14;    // 화면을 가로지르는 노이즈 줄 개수
    static readonly Color[] BarColors =
    {
        new Color(1f, 0.1f, 0.2f, 0.55f),   // 빨강
        new Color(0f, 1f, 1f, 0.45f),       // 하늘
        new Color(1f, 1f, 1f, 0.6f),        // 흰색
        new Color(0f, 0f, 0f, 0.8f),        // 검정
    };

    Image[] bars;          // 노이즈 줄들
    Text centerText;       // 가운데 큰 글자 ([PROGRAM ERROR] 등)
    Text cornerText;       // 오른쪽 위 작은 글자 ([ERROR DETECTED] 등)
    Image blackout;        // 암전용 검은 화면
    Coroutine running;     // 지금 실행 중인 효과

    float shakeStrength;   // 카메라 흔들림 세기 (0이면 안 흔듦)
    Transform shakenCam;   // 이번 프레임에 흔든 카메라
    Vector3 shakeOffset;   // 이번 프레임에 흔든 만큼

    public bool IsPlaying { get; private set; }

    // ① 게임이 시작될 때 자동으로 딱 하나 만들어짐 (씬에 직접 배치할 필요 없음)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("GlitchEffect").AddComponent<GlitchEffect>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // 씬이 바뀌어도 사라지지 않음
        BuildCanvas();
    }

    // ② 화면 전체 치지직: 노이즈 줄 + 흔들림 + (있으면) 가운데 글자
    public void Play(float duration, string message = "", float shake = 0.15f, Action onDone = null)
    {
        Restart(GlitchRoutine(duration, message, shake, onDone));
    }

    // ③ 오른쪽 위에 글자만 아주 짧게 깜빡 (오프닝 장면 3의 [ERROR DETECTED])
    public void FlashCorner(string message, float duration = 0.4f, Action onDone = null)
    {
        Restart(CornerRoutine(message, duration, onDone));
    }

    // ④ 오프닝 장면 7: 크게 치지직 + [PROGRAM ERROR] → 암전 (암전은 HideBlackout 전까지 유지)
    public void PlayProgramError(float duration = 1.5f, Action onDone = null)
    {
        Restart(GlitchRoutine(duration, ProgramErrorText, 0.3f, () =>
        {
            blackout.enabled = true;
            onDone?.Invoke();
        }));
    }

    // ⑤ 암전 켜기 / 풀기
    public void ShowBlackout()
    {
        blackout.enabled = true;
    }

    public void HideBlackout()
    {
        blackout.enabled = false;
    }

    void Restart(IEnumerator routine)
    {
        if (running != null) StopCoroutine(running);
        ResetVisuals();
        running = StartCoroutine(routine);
    }

    IEnumerator GlitchRoutine(float duration, string message, float shake, Action onDone)
    {
        IsPlaying = true;
        shakeStrength = shake;
        centerText.text = message;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            RandomizeBars();

            // 글자는 가끔 사라졌다 나타나고, 살짝씩 어긋나게
            bool hasText = !string.IsNullOrEmpty(message);
            centerText.enabled = hasText && UnityEngine.Random.value > 0.2f;
            centerText.rectTransform.anchoredPosition = UnityEngine.Random.insideUnitCircle * 12f;
            centerText.color = UnityEngine.Random.value > 0.5f ? Color.white : new Color(1f, 0.2f, 0.25f);

            yield return null;   // 한 프레임 기다렸다가 계속
        }

        Finish(onDone);
    }

    IEnumerator CornerRoutine(string message, float duration, Action onDone)
    {
        IsPlaying = true;
        cornerText.text = message;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cornerText.enabled = UnityEngine.Random.value > 0.35f;   // 지직거리며 깜빡
            yield return null;
        }

        Finish(onDone);
    }

    void Finish(Action onDone)
    {
        ResetVisuals();
        IsPlaying = false;
        running = null;
        onDone?.Invoke();
    }

    void ResetVisuals()
    {
        shakeStrength = 0f;
        foreach (var b in bars) b.enabled = false;
        centerText.enabled = false;
        cornerText.enabled = false;
    }

    void RandomizeBars()
    {
        foreach (var b in bars)
        {
            b.enabled = UnityEngine.Random.value > 0.45f;
            if (!b.enabled) continue;

            float y = UnityEngine.Random.value;
            float h = UnityEngine.Random.Range(0.005f, 0.06f);
            float x = UnityEngine.Random.Range(-0.15f, 0.15f);   // 줄이 옆으로 밀린 느낌
            var rt = b.rectTransform;
            rt.anchorMin = new Vector2(x, y);
            rt.anchorMax = new Vector2(1f + x, y + h);
            b.color = BarColors[UnityEngine.Random.Range(0, BarColors.Length)];
        }
    }

    // ⑥ 흔들기: Update에서 지난 프레임에 흔든 만큼 되돌리고, LateUpdate(카메라가 따라간 뒤)에 다시 흔듦
    //    → CameraFollow 코드를 건드리지 않고도 흔들 수 있음
    void Update()
    {
        if (shakenCam != null) shakenCam.position -= shakeOffset;
        shakenCam = null;
        shakeOffset = Vector3.zero;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (shakeStrength <= 0f || cam == null) return;

        shakeOffset = UnityEngine.Random.insideUnitCircle * shakeStrength;
        cam.transform.position += shakeOffset;
        shakenCam = cam.transform;
    }

    // ⑦ 효과용 화면을 코드로 만듦 (장면 전환의 검은 화면보다는 아래)
    void BuildCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        bars = new Image[BarCount];
        for (int i = 0; i < BarCount; i++)
        {
            bars[i] = NewImage("Bar" + i);
            bars[i].enabled = false;
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        centerText = NewText("CenterText", font, 64, TextAnchor.MiddleCenter);
        Stretch(centerText.rectTransform);

        cornerText = NewText("CornerText", font, 28, TextAnchor.UpperRight);
        cornerText.color = new Color(1f, 0.2f, 0.25f);
        var rt = cornerText.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-30f, -24f);
        rt.sizeDelta = new Vector2(600f, 60f);

        blackout = NewImage("Blackout");   // 맨 마지막에 만들어서 가장 위에 그려짐
        blackout.color = Color.black;
        Stretch(blackout.rectTransform);
        blackout.enabled = false;

        centerText.enabled = cornerText.enabled = false;
    }

    Image NewImage(string objName)
    {
        var img = new GameObject(objName).AddComponent<Image>();
        img.transform.SetParent(transform, false);
        img.raycastTarget = false;
        return img;
    }

    Text NewText(string objName, Font font, int size, TextAnchor align)
    {
        var txt = new GameObject(objName).AddComponent<Text>();
        txt.transform.SetParent(transform, false);
        txt.font = font;
        txt.fontSize = size;
        txt.alignment = align;
        txt.color = Color.white;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
