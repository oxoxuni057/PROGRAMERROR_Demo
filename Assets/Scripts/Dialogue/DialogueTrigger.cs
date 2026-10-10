using UnityEngine;
using UnityEngine.Events;

public class DialogueTrigger : Interactable
{
    [SerializeField] DialogueLine[] lines;
    [SerializeField] bool onlyOnce = false;
    [SerializeField] UnityEvent onDialogueEnd;

    bool done;
    bool doneAtCheckpoint;   // 체크포인트 때의 done (ROLLBACK 되면 이걸로 되돌림)

    void OnEnable()
    {
        Rollback.OnCheckpoint += SaveState;
        Rollback.OnRollback += RestoreState;
    }

    void OnDisable()
    {
        Rollback.OnCheckpoint -= SaveState;
        Rollback.OnRollback -= RestoreState;
    }

    void SaveState() => doneAtCheckpoint = done;
    void RestoreState() => done = doneAtCheckpoint;

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