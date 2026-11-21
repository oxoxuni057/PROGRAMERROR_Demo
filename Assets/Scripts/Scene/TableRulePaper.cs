using UnityEngine;

// 문 연결 표의 종류 "규칙": 바닥에 떨어진 종이. 주우면 한 줄이 나오고 규칙 표(rules.csv)의 규칙이 지침서에 적힘
// 주운 뒤에는 사라짐 (플래그 rule_규칙id). ROLLBACK으로 되돌아가면 다시 나타남
public class TableRulePaper : Interactable
{
    [SerializeField] string ruleId;
    [SerializeField, TextArea] string pickupLine = "낡은 종이가 떨어져 있다. 무언가 적혀 있다.";

    public string RuleId => ruleId;

    public void Setup(string id, string line)
    {
        ruleId = id;
        if (!string.IsNullOrEmpty(line)) pickupLine = line;
    }

    public override void Interact(PlayerController player)
    {
        var dm = DialogueManager.Instance;
        if (dm == null || dm.IsOpen) return;
        base.Interact(player);
        GameSound.Play("footstep_1", 0.4f);   // 종이 줍는 소리 대신 (나중에 바꾸기)
        string id = ruleId;
        dm.StartDialogue(new[] { new DialogueLine { text = pickupLine, choices = new DialogueChoice[0] } },
            () => { if (!RuleTable.Give(id)) Debug.LogWarning($"[문 연결 표] 규칙 표에 {id}가 없어요"); });
    }
}
