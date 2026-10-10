using UnityEngine;

// 플레이어가 밟으면 "[ROLLBACK] 때 여기로 돌아옴" 위치가 저장되는 바닥
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] Transform respawnPoint;   // 돌아올 위치 (비워두면 이 물체 위치)

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        Rollback.SetCheckpoint(respawnPoint != null ? respawnPoint.position : transform.position);
    }

    // Scene 화면에 초록 상자로 보여줌
    void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
