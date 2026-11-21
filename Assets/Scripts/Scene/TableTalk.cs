using UnityEngine;

// 문 연결 표의 종류 "대화"·"조사": E로 조사하면 대사 표(dialogue.csv)의 [메시지] id 대화가 나옴
public class TableTalk : Interactable
{
    [SerializeField] string talkId;

    public void Setup(string id) => talkId = id;

    public override void Interact(PlayerController player)
    {
        var dm = DialogueManager.Instance;
        if (dm == null || dm.IsOpen) return;
        if (Rollback.Instance != null && Rollback.Instance.IsPlaying) return;
        base.Interact(player);
        DialogueTable.Play(talkId, null, name);
    }
}
