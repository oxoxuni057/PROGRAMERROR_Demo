using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float radius = 0.8f;          // 조사할 수 있는 거리
    [SerializeField] float forwardOffset = 0.3f;   // 바라보는 방향으로 얼마나 앞을 볼지

    PlayerController player;
    public Interactable Current { get; private set; }  // 지금 조사 가능한 물체

    void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        // 1) 매 프레임 가까운 조사 대상 찾기
        Current = FindNearest();

        // 2) 움직일 수 없는 상태(대화 중 등)거나 대상이 없으면 끝
        if (!player.CanMove || Current == null) return;

        // 3) E 또는 Space를 누른 순간 조사
        var kb = Keyboard.current;
        if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
        {
            Current.Interact(player);
        }
    }

    Interactable FindNearest()
    {
        // 플레이어가 바라보는 방향 살짝 앞을 중심으로 원을 그려서 검사
        Vector2 origin = (Vector2)transform.position + player.FacingDirection * forwardOffset;

        Interactable best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in Physics2D.OverlapCircleAll(origin, radius))
        {
            var it = hit.GetComponentInParent<Interactable>();
            if (it == null) continue;              // Interactable이 없는 물체(벽 등)는 무시

            float d = ((Vector2)it.transform.position - origin).sqrMagnitude;
            if (d < bestDist)                      // 가장 가까운 것 하나만 고름
            {
                bestDist = d;
                best = it;
            }
        }
        return best;
    }

    // Scene 화면에서 Player를 선택하면 조사 범위가 노란 원으로 보임
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}