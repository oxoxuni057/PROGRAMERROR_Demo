using UnityEngine;

// 오브젝트에 붙여서 On Interact / On Dialogue End 같은 이벤트에서 규칙을 주거나 줄 긋기
public class RuleGiver : MonoBehaviour
{
    [Tooltip("규칙 구분용 짧은 영어 id (예: a, b, 01). 규칙을 받으면 플래그 rule_<id>가 켜짐")]
    public string ruleId;

    [TextArea(2, 4)]
    public string ruleText;

    public RuleStyle style = RuleStyle.Printed;

    public void GiveRule()               => RuleBook.Add(ruleId, ruleText, style);
    public void StrikeRule(string id)    => RuleBook.Strike(id, true);
    public void UnstrikeRule(string id)  => RuleBook.Strike(id, false);
}