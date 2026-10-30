using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// 에디터 Scene 화면에 문 연결 표(scene_links.csv)의 문·도착 위치를 미리 그려 줌 (Play 안 해도 보임)
//  초록 상자 = 문, 회색 상자 = 잠긴 문, 하늘색 원 = 도착 위치, 빨간 상자 = 금지조사·금지구역
[InitializeOnLoad]
static class SceneLinksPreview
{
    const string Path = "Assets/Resources/Map/scene_links.csv";
    static List<SceneLinks.Row> rows = new List<SceneLinks.Row>();
    static System.DateTime loadedAt;

    static SceneLinksPreview()
    {
        SceneView.duringSceneGui += Draw;
    }

    static void Reload()
    {
        if (!File.Exists(Path)) { rows.Clear(); return; }
        var t = File.GetLastWriteTimeUtc(Path);
        if (t == loadedAt) return;
        loadedAt = t;
        rows = SceneLinks.ParseText(File.ReadAllText(Path));
    }

    static void Draw(SceneView view)
    {
        if (Application.isPlaying) return;
        Reload();
        string scene = SceneManager.GetActiveScene().name;
        var style = new GUIStyle(EditorStyles.boldLabel);

        foreach (var r in rows)
        {
            if (r.scene != scene) continue;
            Vector3 p = r.pos;
            switch (r.kind)
            {
                case "문":
                    Handles.color = Color.green;
                    Handles.DrawWireCube(p, r.size);
                    style.normal.textColor = Color.green;
                    Handles.Label(p + Vector3.up * (r.size.y * 0.5f + 0.4f), $"{r.name} → {r.toScene} ({r.toSpawn})", style);
                    break;
                case "잠김":
                    Handles.color = Color.gray;
                    Handles.DrawWireCube(p, r.size);
                    style.normal.textColor = Color.gray;
                    Handles.Label(p + Vector3.up * (r.size.y * 0.5f + 0.4f), $"{r.name} (잠김)", style);
                    break;
                case "금지조사":
                case "금지구역":
                    Handles.color = Color.red;
                    Handles.DrawWireCube(p, r.size);
                    style.normal.textColor = Color.red;
                    Handles.Label(p + Vector3.up * (r.size.y * 0.5f + 0.4f), $"{r.kind}: {r.name}", style);
                    break;
                case "도착":
                    Handles.color = Color.cyan;
                    Handles.DrawWireDisc(p, Vector3.forward, 0.35f);
                    style.normal.textColor = Color.cyan;
                    Handles.Label(p + Vector3.right * 0.4f, r.name, style);
                    break;
            }
        }
    }
}
