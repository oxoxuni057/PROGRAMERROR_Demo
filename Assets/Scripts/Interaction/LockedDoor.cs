using UnityEngine;

// 조사하면 "잠겨 있다." 같은 한 줄만 나오는 문 (문 연결 표의 종류 "잠김")
public class LockedDoor : Interactable
{
    [SerializeField, TextArea] string message = "잠겨 있다.";

    public void Setup(string text) => message = text;

    public override void Interact(PlayerController player)
    {
        var dm = DialogueManager.Instance;
        if (dm == null || dm.IsOpen) return;
        base.Interact(player);
        dm.StartDialogue(new[] { new DialogueLine { text = message, choices = new DialogueChoice[0] } });
    }
}
