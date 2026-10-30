using UnityEngine;
using UnityEngine.Events;

// 구역 안에 오래 있을수록 화면이 점점 더 깨짐 (RGB·노이즈·TV 효과), 나가면 서서히 돌아옴
// 2층 화장실 거울처럼 "오래 머물수록 위험해진다"는 시간 압박용
// 콜라이더(Is Trigger)가 있는 오브젝트에 붙이면 플레이어가 들어온 동안 작동
[RequireComponent(typeof(Collider2D))]
public class StayIntensify : MonoBehaviour
{
    [SerializeField] float secondsToMax = 8f;               // 최대로 깨질 때까지 걸리는 시간
    [SerializeField] float recoverSeconds = 1.5f;           // 나간 뒤 원래대로 돌아오는 시간
    [SerializeField, Range(0f, 1f)] float maxTV = 1f;       // 최대일 때 TV 효과 추가량
    [SerializeField, Range(0f, 1f)] float maxRGB = 1f;      // 최대일 때 RGB 어긋남 추가량
    [SerializeField, Range(0f, 1f)] float maxGrain = 0.5f;  // 최대일 때 노이즈 추가량
    [SerializeField] UnityEvent onMax;                      // 최대까지 버티면 실행 (예: 규칙 위반 처리 연결용)

    bool inside;
    bool fired;
    float level;   // 0~1

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null) inside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null) { inside = false; fired = false; }
    }

    // 코드나 테스트에서 직접 켜고 끄기
    public void SetInside(bool value) { inside = value; if (!value) fired = false; }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        level = inside ? level + dt / Mathf.Max(0.01f, secondsToMax)
                       : level - dt / Mathf.Max(0.01f, recoverSeconds);
        level = Mathf.Clamp01(level);
        if (level <= 0f) return;

        float a = level * level;   // 처음엔 살짝, 갈수록 확 심해짐
        ScreenFilter.AddBoost(tv: maxTV * a, rgb: maxRGB * a, grain: maxGrain * a);

        if (inside && level >= 1f && !fired)
        {
            fired = true;
            onMax?.Invoke();
        }
    }
}
