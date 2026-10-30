using UnityEngine;

// 조사하면 규칙 위반 → [ROLLBACK]
// 예: 칠판의 이름 지우기, 멈춘 친구에게 말 걸기(공통 1번), 확인하러 다시 올라가기
//  - Condition: 이 조건일 때만 위반 (비우면 항상 위반). 예: "!passed_attendance"
//  - Message: 위반하기 직전에 나오는 한 줄 (비우면 바로 ROLLBACK)
//  - Safe Message: 조건이 아닐 때 조사하면 나오는 한 줄 (비우면 아무 일 없음)
public class ForbiddenInteract : Interactable
{
    [SerializeField] string condition = "";
    [SerializeField, TextArea] string message = "";
    [SerializeField, TextArea] string safeMessage = "";
    [SerializeField] string reason = "금지된 물체를 조사함";

    public void Setup(string cond, string msg, string why, string safe = "")
    {
        condition = cond;
        message = msg;
        reason = string.IsNullOrEmpty(why) ? reason : why;
        safeMessage = safe;
    }

    public override void Interact(PlayerController player)
    {
        var dm = DialogueManager.Instance;
        if (dm == null || dm.IsOpen) return;
        if (Rollback.Instance != null && Rollback.Instance.IsPlaying) return;
        base.Interact(player);

        if (!GameFlags.Check(condition))
        {
            if (!string.IsNullOrEmpty(safeMessage)) dm.StartDialogue(Line(safeMessage));
            return;
        }

        if (string.IsNullOrEmpty(message)) Rollback.Play(reason);
        else dm.StartDialogue(Line(message), () => Rollback.Play(reason));
    }

    static DialogueLine[] Line(string text) =>
        new[] { new DialogueLine { text = text, choices = new DialogueChoice[0] } };
}
