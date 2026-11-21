using UnityEngine;

// 문 연결 표의 종류 "구역대사": 밟으면 대사 표의 [메시지] id 대화가 한 번 나옴 ([조건]일 때만)
// 한 번 나오면 플래그 zone_<id>가 켜져서 다시 안 나옴. ROLLBACK으로 되돌아가면 다시 나옴
public class TableTalkZone : MonoBehaviour
{
    [SerializeField] string talkId;
    [SerializeField] string condition;

    public void Setup(string id, string cond)
    {
        talkId = id;
        condition = cond;
    }

    string DoneFlag => "zone_" + talkId;

    // 대화창이 열려 있거나 이동 중이면 다음 프레임에 다시 시도 (OnTriggerStay)
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (GameFlags.Has(DoneFlag) || !GameFlags.Check(condition)) return;
        var dm = DialogueManager.Instance;
        if (dm == null || dm.IsOpen) return;
        if (Rollback.Instance != null && Rollback.Instance.IsPlaying) return;

        GameFlags.Set(DoneFlag);
        Debug.Log($"[구역 대사] {talkId}");
        DialogueTable.Play(talkId, null, name);
    }
}
