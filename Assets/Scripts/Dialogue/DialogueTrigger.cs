using UnityEngine;
using UnityEngine.Events;

public class DialogueTrigger : Interactable
{
    [SerializeField] DialogueLine[] lines;
    [SerializeField] bool onlyOnce = false;
    [SerializeField] UnityEvent onDialogueEnd;

    bool done;

    // E키로 조사했을 때
    public override void Interact(PlayerController player)
    {
        if (DialogueManager.Instance.IsOpen) return;
        if (onlyOnce && done) return;

        base.Interact(player);
        Play();
    }

    // 선택지의 "next"나 다른 스크립트에서 대화를 바로 시작할 때
    public void Play()
    {
        if (onlyOnce && done) return;

        DialogueManager.Instance.StartDialogue(lines, () =>
        {
            done = true;
            onDialogueEnd?.Invoke();
        });
    }
}