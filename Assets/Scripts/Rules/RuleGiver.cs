using UnityEngine;

// 지침서 구역 (Inspector에서 고르기 쉽게). 새 건물이 생기면 여기에 추가
public enum RuleSection { None, Common, UpperClassroom, Stairs, Floor2, Lobby1 }

// 오브젝트에 붙여서 On Interact / On Dialogue End 같은 이벤트에서 규칙을 주거나 줄 긋기
public class RuleGiver : MonoBehaviour
{
    [Tooltip("규칙 구분용 짧은 영어 id (예: common_1, 2f_a). 규칙을 받으면 플래그 rule_<id>가 켜짐")]
    public string ruleId;

    [Tooltip("지침서에서 묶일 구역")]
    public RuleSection section = RuleSection.None;

    [Tooltip("번호 또는 글자 (예: 1, 10, 가, 아). 비우면 표시 안 함")]
    public string label;

    [TextArea(2, 4)]
    public string ruleText;

    public RuleStyle style = RuleStyle.Printed;

    [Tooltip("체크하면 처음 발견할 때부터 줄이 그어져 있음 (규칙 \"라\")")]
    public bool alreadyStruck;

    [Header("덧붙임 (다른 필체로 적힌 글)")]
    [TextArea(2, 4)]
    public string note;
    public RuleStyle noteStyle = RuleStyle.Handwritten;

    // 규칙 주기 (덧붙임이 있으면 같이 적힘)
    public void GiveRule()
    {
        RuleBook.Add(ruleId, SectionName(section), label, ruleText, style, alreadyStruck);
        if (!string.IsNullOrEmpty(note)) RuleBook.SetNote(ruleId, note, noteStyle);
    }

    // 나중에 덧붙임만 따로 나타나게 할 때 (이 RuleGiver의 Note 내용으로)
    public void AddNote() => RuleBook.SetNote(ruleId, note, noteStyle);

    public void StrikeRule(string id)   => RuleBook.Strike(id, true);
    public void UnstrikeRule(string id) => RuleBook.Strike(id, false);

    public static string SectionName(RuleSection s)
    {
        switch (s)
        {
            case RuleSection.Common:         return RuleBook.CommonSection;
            case RuleSection.UpperClassroom: return "위층 강의실";
            case RuleSection.Stairs:         return "계단";
            case RuleSection.Floor2:         return "2층";
            case RuleSection.Lobby1:         return "1층 로비";
            default:                         return "";
        }
    }
}
