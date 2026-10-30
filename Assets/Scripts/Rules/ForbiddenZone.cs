using UnityEngine;

// 들어가면 규칙 위반 → [ROLLBACK]
// 예: 불 켜진 랩실 안, 계단에서 다시 올라가는 쪽, 화장실 거울 앞에 오래 머물기
//  - Condition: 이 조건일 때만 위반 (비우면 항상). 예: "rule_ma" (규칙 마를 안 뒤부터만)
//  - Stay Seconds: 0이면 들어가자마자 위반. 2면 2초 넘게 머물면 위반 (나가면 초기화)
//  - Message: 위반하기 직전에 나오는 한 줄 (비우면 바로 ROLLBACK)
[RequireComponent(typeof(Collider2D))]
public class ForbiddenZone : MonoBehaviour
{
    [SerializeField] string condition = "";
    [SerializeField] float staySeconds = 0f;
    [SerializeField, TextArea] string message = "";
    [SerializeField] string reason = "금지된 구역에 들어감";

    PlayerController inside;
    float timer;

    public void Setup(string cond, float stay, string msg, string why)
    {
        condition = cond;
        staySeconds = stay;
        message = msg;
        reason = string.IsNullOrEmpty(why) ? reason : why;
    }

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerController>();
        if (p == null) return;
        inside = p;
        timer = 0f;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null) inside = null;
    }

    void Update()
    {
        if (inside == null) return;
        if (Rollback.Instance != null && Rollback.Instance.IsPlaying) { inside = null; return; }
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return;   // 대화 중엔 멈춤
        if (!GameFlags.Check(condition)) return;

        timer += Time.deltaTime;
        if (timer < staySeconds) return;

        inside = null;
        Violate();
    }

    void Violate()
    {
        var dm = DialogueManager.Instance;
        if (string.IsNullOrEmpty(message) || dm == null) Rollback.Play(reason);
        else dm.StartDialogue(new[] { new DialogueLine { text = message, choices = new DialogueChoice[0] } },
                              () => Rollback.Play(reason));
    }

    void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
