using UnityEngine;

// Inspector에서 짧은 화면 효과를 쓰기 위한 부품
// EventZone의 On Enter나 Interactable의 On Interact에 ScreenFxTrigger.Play를 연결하면 됨
public class ScreenFxTrigger : MonoBehaviour
{
    public enum Kind
    {
        TVSpike,   // 아날로그 TV 효과가 순간 최대로 치솟음
        Flash,     // 화면이 한 색으로 번쩍
        RedEdge,   // 가장자리가 빨갛게 번졌다 사라짐
    }

    [SerializeField] Kind kind = Kind.TVSpike;
    [SerializeField, Range(0f, 1f)] float strength = 1f;    // 세기 (Flash는 사용 안 함)
    [SerializeField] float seconds = 0.6f;                  // 효과 시간(초). RedEdge는 빨간 상태로 머무는 시간
    [SerializeField] Color flashColor = new Color(0.7f, 0f, 0.05f, 0.6f);   // Flash 색 (A 값 = 진하기)

    public void Play()
    {
        switch (kind)
        {
            case Kind.TVSpike: ScreenFx.TVSpike(strength, seconds); break;
            case Kind.Flash: ScreenFx.Flash(flashColor, seconds); break;
            case Kind.RedEdge: ScreenFx.RedEdge(strength, 0.4f, seconds, 1f); break;
        }
    }
}
