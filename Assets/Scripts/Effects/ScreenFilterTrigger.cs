using UnityEngine;

// Inspector에서 색 필터를 쓰기 위한 부품
// GlitchTrigger(ProgramError)의 On Finished에 EnterInspection을 연결하면 → 암전이 풀릴 때부터 점검 세계 색
// EventZone의 On Enter에 SetNormal / SetInspection / SetFloor2를 연결하면 그 자리에서 색이 서서히 바뀜
public class ScreenFilterTrigger : MonoBehaviour
{
    [SerializeField] float fadeSeconds = 1f;   // 서서히 바뀌는 시간(초), 0이면 바로

    public void EnterInspection() => ScreenFilter.EnterInspection();
    public void SetNormal() => ScreenFilter.Set(ScreenFilter.Look.Normal, fadeSeconds);
    public void SetInspection() => ScreenFilter.Set(ScreenFilter.Look.Inspection, fadeSeconds);
    public void SetFloor2() => ScreenFilter.Set(ScreenFilter.Look.Floor2, fadeSeconds);
    public void ResetToAuto() { if (ScreenFilter.Instance != null) ScreenFilter.Instance.Refresh(); }
}
