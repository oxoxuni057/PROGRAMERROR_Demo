using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 대사 표(CSV)를 읽어서 대화창에 넘겨 주는 곳
// 파일: Assets/Resources/Dialogue/dialogue.csv  (엑셀·구글 시트에서 "CSV UTF-8"로 저장)
//
// 열:  id | 조건 | 화자 | 대사 | 옵션 | 실행
//  - id   : 캐릭터(Talker)의 Talk Id. 비우면 윗줄과 같은 id
//  - 조건 : 이 플래그가 켜져 있을 때만 이 대사 묶음 사용 (여러 개는 띄어쓰기, 꺼져 있어야 하면 !플래그)
//           같은 id에 조건이 다른 묶음이 여러 개면, 조건이 맞는 것 중 조건이 가장 많은 묶음을 씀
//  - 화자 : 이름. "선택지"라고 쓰면 윗줄 대사에 붙는 선택지가 됨 (이때 조건 = 선택지가 보일 조건)
//  - 옵션 : 속마음 / 끊김 / 대기:1   (여러 개는 띄어쓰기)
//  - 실행 : 이 줄을 넘길 때(선택지는 고를 때) 실행. 여러 개는 ; 로 구분
//           flag:이름  unflag:이름  rule:규칙id  strike:규칙id  rollback:이유  glitch  glitch:글자
//           goto:다른id (대화가 끝나고 이어서)   event:이름 (Talker의 Events에 연결한 동작)
//  - id 칸이 #으로 시작하면 메모(무시)
public static class DialogueTable
{
    const string ResourcePath = "Dialogue/dialogue";
    const string ChoiceSpeaker = "선택지";

    class Row
    {
        public string id, cond, speaker, text, option, action;
        public int lineNo;
    }

    class Variant
    {
        public string cond;
        public readonly List<Row> rows = new List<Row>();
    }

    static Dictionary<string, List<Variant>> table;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() => table = null;   // Play할 때마다 파일을 새로 읽음

    public static bool Has(string id)
    {
        Load();
        return !string.IsNullOrEmpty(id) && table.ContainsKey(id);
    }

    // id의 대화 재생. source = 말을 건 캐릭터 (event: 실행용, 없으면 null)
    public static void Play(string id, Talker source = null, string fallbackSpeaker = "")
    {
        Load();
        var dm = DialogueManager.Instance;
        if (dm == null || dm.IsOpen) return;

        if (string.IsNullOrEmpty(id) || !table.TryGetValue(id, out var variants))
        {
            Debug.LogWarning($"[대사 표] 대사 미정: {id}");
            dm.StartDialogue(new[] { new DialogueLine { speaker = fallbackSpeaker, text = $"(대사 미정: {id})" } });
            return;
        }

        var v = Pick(variants);
        if (v == null)
        {
            Debug.LogWarning($"[대사 표] {id}: 조건이 맞는 대사가 없어요");
            return;
        }

        var lines = new List<DialogueLine>();
        string gotoId = null;
        foreach (var row in v.rows)
        {
            if (row.speaker == ChoiceSpeaker)
            {
                if (lines.Count == 0) { Debug.LogWarning($"[대사 표] {row.lineNo}번째 줄: 선택지 위에 대사가 없어요"); continue; }
                AddChoice(lines[lines.Count - 1], row, source);
                continue;
            }

            var line = new DialogueLine { speaker = row.speaker, text = row.text, choices = new DialogueChoice[0] };
            ApplyOptions(line, row.option);

            string lineGoto = null;
            line.afterLine = BuildActions(row.action, source, ref lineGoto);
            lines.Add(line);
            if (lineGoto != null) { gotoId = lineGoto; break; }   // goto 뒤의 줄은 안 씀
        }
        if (lines.Count == 0) return;

        string next = gotoId;
        dm.StartDialogue(lines.ToArray(), next == null ? (Action)null : () => Play(next, source, fallbackSpeaker));
    }

    // ───── 조건 ─────
    static Variant Pick(List<Variant> variants)
    {
        Variant best = null;
        int bestCount = -1;
        foreach (var v in variants)
        {
            if (!Check(v.cond)) continue;
            int count = Split(v.cond, ' ').Count;
            if (count > bestCount) { best = v; bestCount = count; }
        }
        return best;
    }

    static bool Check(string cond)
    {
        foreach (var token in Split(cond, ' '))
        {
            bool not = token.StartsWith("!");
            string flag = not ? token.Substring(1) : token;
            if (GameFlags.Has(flag) == not) return false;
        }
        return true;
    }

    // ───── 옵션 ─────
    static void ApplyOptions(DialogueLine line, string option)
    {
        foreach (var token in Split(option, ' '))
        {
            if (token == "속마음") line.style = LineStyle.Thought;
            else if (token == "끊김") line.interrupted = true;
            else if (token.StartsWith("대기:") && float.TryParse(token.Substring(3), out float sec)) line.waitBefore = sec;
            else Debug.LogWarning($"[대사 표] 모르는 옵션: {token}");
        }
    }

    // ───── 선택지 ─────
    static void AddChoice(DialogueLine line, Row row, Talker source)
    {
        string cond = row.cond;
        string gotoId = null;
        Action actions = BuildActions(row.action, source, ref gotoId);
        string next = gotoId;

        var choice = new DialogueChoice
        {
            text = row.text,
            visibleIf = string.IsNullOrEmpty(cond) ? (Func<bool>)null : () => Check(cond),
            picked = () =>
            {
                actions?.Invoke();
                if (next != null) Play(next, source);
            },
        };
        var list = new List<DialogueChoice>(line.choices ?? new DialogueChoice[0]) { choice };
        line.choices = list.ToArray();
    }

    // ───── 실행 ─────
    static Action BuildActions(string action, Talker source, ref string gotoId)
    {
        Action result = null;
        foreach (var raw in Split(action, ';'))
        {
            int colon = raw.IndexOf(':');
            string name = (colon < 0 ? raw : raw.Substring(0, colon)).Trim();
            string arg = colon < 0 ? "" : raw.Substring(colon + 1).Trim();

            switch (name)
            {
                case "goto":     gotoId = arg; break;
                case "flag":     result += () => GameFlags.Set(arg); break;
                case "unflag":   result += () => GameFlags.Clear(arg); break;
                case "strike":   result += () => RuleBook.Strike(arg, true); break;
                case "rule":     result += () => GiveRule(arg); break;
                case "rollback": result += () => Rollback.Play(arg); break;
                case "glitch":   result += () => { if (GlitchEffect.Instance != null) GlitchEffect.Instance.Play(0.8f, arg, 0.15f); }; break;
                case "event":    result += () => { if (source != null) source.RunEvent(arg); }; break;
                default: Debug.LogWarning($"[대사 표] 모르는 실행: {raw}"); break;
            }
        }
        return result;
    }

    // 씬에 있는 RuleGiver 중 Rule Id가 같은 것으로 규칙 주기 (규칙 내용은 RuleGiver에 적음)
    static void GiveRule(string id)
    {
        foreach (var g in UnityEngine.Object.FindObjectsByType<RuleGiver>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (g.ruleId == id) { g.GiveRule(); return; }
        Debug.LogWarning($"[대사 표] rule:{id} — 씬에 Rule Id가 {id}인 RuleGiver가 없어요");
    }

    // ───── 파일 읽기 ─────
    static void Load()
    {
        if (table != null) return;
        table = new Dictionary<string, List<Variant>>();

        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("[대사 표] Assets/Resources/Dialogue/dialogue.csv 파일이 없어요");
            return;
        }

        var records = ParseCsv(asset.text);
        if (records.Count == 0) return;

        // 첫 줄은 제목 줄
        var header = records[0];
        int Col(string name, int fallback)
        {
            for (int i = 0; i < header.Count; i++) if (header[i].Trim() == name) return i;
            return fallback;
        }
        int cId = Col("id", 0), cCond = Col("조건", 1), cSpeaker = Col("화자", 2),
            cText = Col("대사", 3), cOption = Col("옵션", 4), cAction = Col("실행", 5);

        string lastId = null;
        Variant current = null;
        for (int r = 1; r < records.Count; r++)
        {
            var rec = records[r];
            string Get(int col) => col < rec.Count ? rec[col].Trim() : "";

            var row = new Row
            {
                id = Get(cId), cond = Get(cCond), speaker = Get(cSpeaker),
                text = Get(cText), option = Get(cOption), action = Get(cAction), lineNo = r + 1,
            };
            if (row.id.StartsWith("#")) continue;
            if (row.id == "" && row.speaker == "" && row.text == "") continue;   // 빈 줄
            if (row.id == "") row.id = lastId;
            if (row.id == null) continue;

            bool isChoice = row.speaker == ChoiceSpeaker;
            bool newVariant = row.id != lastId || current == null || (!isChoice && row.cond != current.cond);
            if (newVariant)
            {
                if (!table.TryGetValue(row.id, out var list)) table[row.id] = list = new List<Variant>();
                current = new Variant { cond = isChoice ? "" : row.cond };
                list.Add(current);
            }
            current.rows.Add(row);
            lastId = row.id;
        }
        Debug.Log($"[대사 표] {table.Count}개 id 읽음");
    }

    // 따옴표·쉼표·줄바꿈이 들어간 칸도 읽는 CSV 해석
    static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false;
        if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);   // UTF-8 BOM 제거

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (quoted)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else quoted = false;
                }
                else cell.Append(ch);
            }
            else if (ch == '"') quoted = true;
            else if (ch == ',') { row.Add(cell.ToString()); cell.Clear(); }
            else if (ch == '\n' || ch == '\r')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString()); cell.Clear();
                rows.Add(row); row = new List<string>();
            }
            else cell.Append(ch);
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
        return rows;
    }

    static List<string> Split(string s, char sep)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(s)) return result;
        foreach (var part in s.Split(sep))
        {
            var t = part.Trim();
            if (t.Length > 0) result.Add(t);
        }
        return result;
    }
}
