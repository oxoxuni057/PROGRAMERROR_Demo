using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 형광등이나 문틈 빛이 지지직 깜빡임 (18217 불 켜진 랩실, 2층 복도 형광등 등)
// 2D 조명(Light 2D)이나 스프라이트(빛 모양 그림)가 붙은 오브젝트에 같이 붙이면 됨
public class LightFlicker : MonoBehaviour
{
    [SerializeField] bool playOnStart = true;          // 시작하자마자 깜빡이기
    [SerializeField] Vector2 calmSeconds = new Vector2(1f, 4f);   // 멀쩡히 켜져 있는 시간 (최소~최대)
    [SerializeField] Vector2Int burstCount = new Vector2Int(2, 6); // 한 번 깜빡일 때 몇 번 꺼졌다 켜지는지
    [SerializeField, Range(0f, 1f)] float dimLevel = 0.1f;        // 꺼질 때 밝기 (0이면 완전히 꺼짐)
    [SerializeField, Range(0f, 0.5f)] float hum = 0.05f;          // 켜져 있을 때 미세한 떨림

    Light2D light2D;
    SpriteRenderer sprite;
    float baseIntensity = 1f;
    Color baseColor = Color.white;
    Coroutine running;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        sprite = GetComponent<SpriteRenderer>();
        if (light2D != null) baseIntensity = light2D.intensity;
        if (sprite != null) baseColor = sprite.color;
    }

    void Start() { if (playOnStart) StartFlicker(); }
    void OnDisable() { running = null; }   // 꺼지면 코루틴도 멈추니까 다시 켤 수 있게

    public void StartFlicker()
    {
        if (running == null) running = StartCoroutine(FlickerRoutine());
    }

    public void StopFlicker()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        SetLevel(1f);
    }

    // 완전히 꺼 두기 (반복이 멈추는 순간 등)
    public void TurnOff()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        SetLevel(0f);
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // 멀쩡한 구간: 미세하게 웅웅 떨림
            float calm = Random.Range(calmSeconds.x, calmSeconds.y);
            for (float t = 0f; t < calm; t += Time.deltaTime)
            {
                SetLevel(1f - hum * Random.value);
                yield return null;
            }

            // 지지직 구간: 빠르게 몇 번 꺼졌다 켜짐
            int n = Random.Range(burstCount.x, burstCount.y + 1);
            for (int i = 0; i < n; i++)
            {
                SetLevel(dimLevel);
                yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
                SetLevel(1f);
                yield return new WaitForSeconds(Random.Range(0.03f, 0.15f));
            }
        }
    }

    void SetLevel(float k)
    {
        if (light2D != null) light2D.intensity = baseIntensity * k;
        if (sprite != null) sprite.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * k);
    }
}
