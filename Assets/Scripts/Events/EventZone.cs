using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class EventZone : MonoBehaviour
{
    [SerializeField] bool playOnce = true;      // 한 번만 실행할지 (끄면 밟을 때마다 실행)
    [SerializeField] float lockSeconds = 0f;    // 밟은 뒤 플레이어를 몇 초 못 움직이게 할지 (0이면 잠그지 않음)
    [SerializeField] UnityEvent onEnter;        // 밟았을 때 실행할 동작 (Inspector에서 연결)

    bool played;             // 이미 실행했는지
    PlayerController player; // 마지막으로 밟은 플레이어

    // ① 컴포넌트를 처음 붙일 때 콜라이더를 자동으로 "밟을 수 있는 구역(Is Trigger)"으로 바꿈
    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // ② 무언가 구역에 들어오면 실행됨
    void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerController>();
        if (p == null) return;              // 플레이어가 아니면 무시
        if (playOnce && played) return;     // 이미 한 번 실행했으면 무시

        played = true;
        player = p;
        Debug.Log($"[이벤트] {name}");      // Console에 "[이벤트] 구역이름" 출력

        if (lockSeconds > 0f) StartCoroutine(LockFor(lockSeconds));
        onEnter?.Invoke();                  // Inspector에 연결한 동작 실행
    }

    IEnumerator LockFor(float seconds)
    {
        LockPlayer();
        yield return new WaitForSeconds(seconds);
        UnlockPlayer();
    }

    // ③ On Enter에서 직접 부를 수도 있는 잠금/해제 (컷신이 끝날 때 UnlockPlayer를 연결하는 식으로)
    public void LockPlayer()
    {
        var p = FindPlayer();
        if (p != null) p.CanMove = false;
    }

    public void UnlockPlayer()
    {
        var p = FindPlayer();
        if (p != null) p.CanMove = true;
    }

    PlayerController FindPlayer()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        return player;
    }

    // ④ 다시 밟을 수 있게 되돌림 (ROLLBACK 때 사용 예정)
    public void ResetZone()
    {
        played = false;
    }

    // Scene 화면에 노란 상자로 구역을 보여줌 (게임 화면엔 안 보임)
    void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = played ? Color.gray : Color.yellow;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
