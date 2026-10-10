using System.Collections.Generic;
using UnityEngine;

// 게임 진행 상황을 "이름표(플래그)"로 기억하는 곳
// 예: GameFlags.Set("rule_a_found");  →  GameFlags.Has("rule_a_found") 가 true
public static class GameFlags
{
    static readonly HashSet<string> flags = new HashSet<string>();

    // ▶ 재생할 때마다 깨끗하게 비움
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() => flags.Clear();

    public static bool Has(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    public static void Set(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return;
        if (flags.Add(flag)) Debug.Log($"[플래그 ON] {flag}");
    }

    public static void Clear(string flag)
    {
        if (flags.Remove(flag)) Debug.Log($"[플래그 OFF] {flag}");
    }

    // 나중에 ROLLBACK(체크포인트로 되돌리기)에서 사용
    public static List<string> Snapshot() => new List<string>(flags);

    public static void Restore(IEnumerable<string> saved)
    {
        flags.Clear();
        foreach (var f in saved) flags.Add(f);
    }
}