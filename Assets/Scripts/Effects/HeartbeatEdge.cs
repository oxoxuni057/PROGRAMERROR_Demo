using UnityEngine;

// 심장이 뛰듯 화면 가장자리가 쿵쿵 어두워졌다 밝아짐 ('뚝, 뚝' 소리가 다가올 때 등)
// EventZone의 On Enter에 Begin, 끝나는 곳(또는 컷신 끝)에 End를 연결
public class HeartbeatEdge : MonoBehaviour
{
    [SerializeField] float bpm = 70f;                        // 1분에 몇 번 뛰는지 (빠를수록 긴박)
    [SerializeField, Range(0f, 1f)] float strength = 0.4f;   // 가장자리가 어두워지는 정도
    [SerializeField] float rampSeconds = 3f;                 // Begin 후 최대 세기까지 걸리는 시간 (다가오는 느낌)
    [SerializeField] bool red = false;                       // 켜면 가장자리가 빨갛게 (그녀가 가까울 때)
    [SerializeField] bool playOnEnable = false;              // 오브젝트가 켜지면 바로 시작

    bool beating;
    float level;     // 0~1, 지금 세기
    float phase;     // 박자 위치

    void OnEnable() { if (playOnEnable) Begin(); }

    public void Begin() { beating = true; }
    public void End() { beating = false; }

    // 빠르게: bpm을 바꿈 (소리가 가까워질 때 On Enter에서 SetBpm 120 같은 식으로)
    public void SetBpm(float value) { bpm = value; }

    void Update()
    {
        float target = beating ? 1f : 0f;
        float speed = beating ? 1f / Mathf.Max(0.01f, rampSeconds) : 1f;   // 끝날 때는 1초 안에 사라짐
        level = Mathf.MoveTowards(level, target, speed * Time.unscaledDeltaTime);
        if (level <= 0f) { phase = 0f; return; }

        phase += Time.unscaledDeltaTime * bpm / 60f;
        float p = phase % 1f;
        // 쿵-쿵 두 번 뛰고 쉬는 모양
        float beat = Mathf.Exp(-p * 18f) + (p > 0.18f ? 0.6f * Mathf.Exp(-(p - 0.18f) * 18f) : 0f);
        float a = strength * level * beat;

        ScreenFilter.AddBoost(vignette: a, tv: a * 0.3f, redEdge: red ? a * 2f : 0f);
    }
}
