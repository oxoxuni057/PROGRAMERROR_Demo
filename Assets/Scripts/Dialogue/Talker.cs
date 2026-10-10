using System;
using UnityEngine;
using UnityEngine.Events;

// 대사 표(CSV)를 쓰는 캐릭터. Talk Id만 적으면 대사는 표에서 가져옴
// 대사가 아직 없으면 "(대사 미정: id)"가 나와서 캐릭터 배치를 먼저 해도 됨
public class Talker : Interactable
{
    [Tooltip("대사 표(dialogue.csv)의 id. 여러 캐릭터가 같은 id를 써도 됨")]
    [SerializeField] string talkId;

    [Tooltip("대사 표의 실행 칸에 event:이름 으로 부를 동작")]
    [SerializeField] NamedEvent[] events;

    [Serializable]
    public class NamedEvent
    {
        public string name;
        public UnityEvent action;
    }

    public string TalkId => talkId;

    // E키로 말 걸었을 때
    public override void Interact(PlayerController player)
    {
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsOpen) return;
        base.Interact(player);
        Play();
    }

    // 다른 이벤트(EventZone 등)에서 이 캐릭터 대화 시작
    public void Play() => DialogueTable.Play(talkId, this, name);

    // 다른 id의 대화를 이 캐릭터로 시작 (EventZone의 On Enter 등에서 Talker.PlayId("id"))
    public void PlayId(string id) => DialogueTable.Play(id, this, name);

    public void RunEvent(string eventName)
    {
        if (events != null)
            foreach (var e in events)
                if (e.name == eventName) { e.action?.Invoke(); return; }
        Debug.LogWarning($"[대사 표] {name}: event:{eventName}에 연결된 동작이 없어요");
    }
}
