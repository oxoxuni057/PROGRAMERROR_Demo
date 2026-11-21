using System.Collections.Generic;
using UnityEngine;

// 규칙 표(CSV): 지침서에 들어갈 규칙 내용을 한곳에 적어 두는 곳
// 대사 표의 rule:규칙id 가 씬에서 RuleGiver를 못 찾으면 이 표에서 찾아서 줌
// → 규칙 종이를 맵에 하나하나 놓지 않아도 대사로 규칙을 줄 수 있음
//
// 파일: Assets/Resources/Dialogue/rules.csv  (CSV UTF-8)
// 열:  id | 구역 | 번호 | 내용 | 필체 | 줄긋기 | 덧붙임 | 덧붙임필체
//  - 구역 : 공통 지침 / 위층 강의실 / 계단 / 2층 / 1층 로비 (비우면 구역 없음)
//  - 필체 : 인쇄 / 손글씨 / 빨강  (비우면 인쇄)
//  - 줄긋기 : "예"면 처음부터 줄이 그어진 규칙 (규칙 라)
//  - 덧붙임 : 다른 필체로 덧붙인 글 (비우면 없음)
public static class RuleTable
{
    const string ResourcePath = "Dialogue/rules";

    class Row
    {
        public string id, section, label, text, note;
        public RuleStyle style, noteStyle;
        public bool struck;
    }

    static Dictionary<string, Row> table;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() => table = null;

    public static bool Has(string id)
    {
        Load();
        return table.ContainsKey(id);
    }

    // 규칙 주기. 표에 없으면 false
    public static bool Give(string id)
    {
        Load();
        if (!table.TryGetValue(id, out var r)) return false;
        RuleBook.Add(r.id, r.section, r.label, r.text, r.style, r.struck);
        if (!string.IsNullOrEmpty(r.note)) RuleBook.SetNote(r.id, r.note, r.noteStyle);
        return true;
    }

    static RuleStyle StyleOf(string s, RuleStyle fallback)
    {
        switch (s)
        {
            case "인쇄": return RuleStyle.Printed;
            case "손글씨": return RuleStyle.Handwritten;
            case "빨강": return RuleStyle.Red;
            default: return fallback;
        }
    }

    static void Load()
    {
        if (table != null) return;
        table = new Dictionary<string, Row>();
        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null) { Debug.LogWarning("[규칙 표] Assets/Resources/Dialogue/rules.csv 파일이 없어요"); return; }

        var records = DialogueTable.ParseCsv(asset.text);
        for (int i = 1; i < records.Count; i++)   // 첫 줄은 제목 줄
        {
            var rec = records[i];
            string Get(int c) => c < rec.Count ? rec[c].Trim() : "";
            string id = Get(0);
            if (id == "" || id.StartsWith("#")) continue;
            table[id] = new Row
            {
                id = id, section = Get(1), label = Get(2), text = Get(3),
                style = StyleOf(Get(4), RuleStyle.Printed),
                struck = Get(5) == "예",
                note = Get(6),
                noteStyle = StyleOf(Get(7), RuleStyle.Handwritten),
            };
        }
    }
}
