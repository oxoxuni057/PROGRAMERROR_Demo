using System.Collections;
using UnityEngine;

// 짧은 화면 효과 모음 (코드에서 한 줄로)
// ScreenFx.TVSpike()  : 아날로그 TV 효과가 순간 최대로 치솟았다 돌아옴 (게시판 명단, 10번 규칙 발견)
// ScreenFx.Flash()    : 화면이 한 색으로 번쩍 (그녀 등장, 빨간 쪽지)
// ScreenFx.RedEdge()  : 화면 가장자리가 빨갛게 번졌다 사라짐 (그녀가 가까이 있을 때)
public static class ScreenFx
{
    public static readonly Color Red = new Color(0.7f, 0f, 0.05f, 0.6f);

    public static void TVSpike(float strength = 1f, float seconds = 0.6f)
    {
        Run(TVSpikeRoutine(strength, seconds));
    }

    public static void Flash(Color color, float seconds = 0.3f)
    {
        if (GlitchEffect.Instance != null) GlitchEffect.Instance.FlashColor(color, seconds);
    }

    public static void RedEdge(float strength = 1f, float fadeIn = 0.4f, float hold = 1.5f, float fadeOut = 1f)
    {
        Run(RedEdgeRoutine(strength, fadeIn, hold, fadeOut));
    }

    static void Run(IEnumerator routine)
    {
        if (ScreenFilter.Instance != null) ScreenFilter.Instance.StartCoroutine(routine);
    }

    static IEnumerator TVSpikeRoutine(float strength, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / seconds;
            float jitter = 0.6f + 0.4f * Random.value;   // 고르게 줄지 않고 덜덜거리며 줄어듦
            float a = strength * k * k * jitter;
            ScreenFilter.AddBoost(tv: a, rgb: a, grain: a * 0.5f);
            yield return null;
        }
    }

    static IEnumerator RedEdgeRoutine(float strength, float fadeIn, float hold, float fadeOut)
    {
        float total = fadeIn + hold + fadeOut;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            float k = t < fadeIn ? t / fadeIn
                    : t < fadeIn + hold ? 1f
                    : 1f - (t - fadeIn - hold) / fadeOut;
            ScreenFilter.AddBoost(redEdge: strength * k, rgb: 0.3f * strength * k);
            yield return null;
        }
    }
}
