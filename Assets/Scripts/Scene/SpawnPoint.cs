using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] string id = "default";   // 이 위치의 이름표
    public string Id => id;
    public void Setup(string newId) => id = newId;   // 문 연결 표(SceneLinks)에서 코드로 만들 때

    // Scene 화면에 하늘색 원으로 위치를 보여줌 (게임 화면엔 안 보임)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}