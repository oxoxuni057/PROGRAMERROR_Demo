using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

// 문 연결 표(CSV)를 읽어서, 씬이 열릴 때 문·도착 위치를 자동으로 만들어 줌
// 맵 씬(타일맵만 있는 씬)에 플레이어·카메라가 없으면 그것도 자동으로 만듦
// → 맵 담당이 만든 씬 파일을 건드리지 않고 이동을 연결할 수 있음
//
// 파일: Assets/Resources/Map/scene_links.csv  (CSV UTF-8)
// 열:  씬 | 종류 | 이름 | x | y | 너비 | 높이 | 가는 씬 | 가는 도착 | 표시 | 메시지 | 조건 | 시간 | 이유
//  - 종류 문   : E로 조사하면 [가는 씬]의 [가는 도착] 위치로 이동. 표시 = 머리 위 글자 (들어가기 등)
//  - 종류 도착 : 이 씬에 들어왔을 때 나타나는 위치. 이름 = 도착 이름 (다른 문의 "가는 도착"에 씀)
//              이름이 default인 도착 = 처음 시작하거나 따로 정하지 않았을 때의 위치
//  - 종류 잠김 : 조사하면 [메시지]만 나오는 문
//  - 종류 금지조사 : 조사하면 규칙 위반 → [메시지] 한 줄 뒤 ROLLBACK ([조건]일 때만, [이유]는 Console용)
//  - 종류 금지구역 : 들어가면 규칙 위반 → ROLLBACK. [시간]초 넘게 머물 때만 위반하게 할 수도 있음
//  - 좌표는 Unity 월드 좌표 (1 = 한 칸). 씬 칸이 #으로 시작하면 메모
public static class SceneLinks
{
    const string ResourcePath = "Map/scene_links";
    const string PlayerPrefab = "Player";
    const float CameraSize = 5.625f;   // 640×360, 32px 타일 기준 (세로 11.25칸)

    public class Row
    {
        public string scene, kind, name, toScene, toSpawn, show, message, condition, reason;
        public float seconds;
        public Vector2 pos, size;
    }

    static Dictionary<string, List<Row>> table;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() => table = null;

    public static List<Row> RowsFor(string sceneName)
    {
        Load();
        return table.TryGetValue(sceneName, out var list) ? list : null;
    }

    // SceneTransition이 씬을 열 때마다 부름
    public static void Build(Scene scene)
    {
        var rows = RowsFor(scene.name);
        if (rows == null || rows.Count == 0) return;   // 표에 없는 씬은 건드리지 않음

        var root = new GameObject("_SceneLinks (문 연결 표에서 자동 생성)");
        SceneManager.MoveGameObjectToScene(root, scene);

        Vector3? defaultSpawn = null, firstSpawn = null;
        foreach (var r in rows)
        {
            var go = new GameObject($"{r.kind} {r.name}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = r.pos;

            switch (r.kind)
            {
                case "문":
                {
                    AddTrigger(go, r.size);
                    var door = go.AddComponent<SceneDoor>();
                    door.Setup(r.toScene, r.toSpawn);
                    door.SetPrompt(string.IsNullOrEmpty(r.show) ? "들어가기" : r.show);
                    break;
                }
                case "잠김":
                {
                    AddTrigger(go, r.size);
                    var locked = go.AddComponent<LockedDoor>();
                    locked.Setup(string.IsNullOrEmpty(r.message) ? "잠겨 있다." : r.message);
                    locked.SetPrompt(string.IsNullOrEmpty(r.show) ? "조사하기" : r.show);
                    break;
                }
                case "금지조사":
                {
                    AddTrigger(go, r.size);
                    var f = go.AddComponent<ForbiddenInteract>();
                    f.Setup(r.condition, r.message, string.IsNullOrEmpty(r.reason) ? r.name : r.reason);
                    f.SetPrompt(string.IsNullOrEmpty(r.show) ? "조사하기" : r.show);
                    break;
                }
                case "금지구역":
                {
                    AddTrigger(go, r.size);
                    go.AddComponent<ForbiddenZone>().Setup(r.condition, r.seconds, r.message,
                        string.IsNullOrEmpty(r.reason) ? r.name : r.reason);
                    break;
                }
                case "도착":
                {
                    go.AddComponent<SpawnPoint>().Setup(r.name);
                    if (firstSpawn == null) firstSpawn = r.pos;
                    if (r.name == "default") defaultSpawn = r.pos;
                    break;
                }
                default:
                    Debug.LogWarning($"[문 연결 표] {r.scene}: 모르는 종류 '{r.kind}'");
                    break;
            }
        }

        EnsurePlayerAndCamera(scene, defaultSpawn ?? firstSpawn ?? Vector3.zero);
    }

    static void AddTrigger(GameObject go, Vector2 size)
    {
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(Mathf.Max(size.x, 0.5f), Mathf.Max(size.y, 0.5f));
        col.isTrigger = true;   // 길을 막지 않고, E 조사만 되게
    }

    static void EnsurePlayerAndCamera(Scene scene, Vector3 spawn)
    {
        if (Object.FindAnyObjectByType<PlayerController>() == null)
        {
            var prefab = Resources.Load<GameObject>(PlayerPrefab);
            if (prefab == null) Debug.LogWarning("[문 연결 표] Assets/Resources/Player.prefab이 없어요");
            else
            {
                var player = Object.Instantiate(prefab, spawn, Quaternion.identity);
                player.name = "Player";
                SceneManager.MoveGameObjectToScene(player, scene);
            }
        }

        if (Camera.main == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(spawn.x, spawn.y, -10f);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<CameraFollow>();
            SceneManager.MoveGameObjectToScene(camGO, scene);
        }
    }

    // ───── 파일 읽기 ─────
    static void Load()
    {
        if (table != null) return;
        table = new Dictionary<string, List<Row>>();
        foreach (var r in ParseText(Resources.Load<TextAsset>(ResourcePath)?.text))
        {
            if (!table.TryGetValue(r.scene, out var list)) table[r.scene] = list = new List<Row>();
            list.Add(r);
        }
    }

    // 에디터 미리보기에서도 같이 씀
    public static List<Row> ParseText(string text)
    {
        var result = new List<Row>();
        if (string.IsNullOrEmpty(text)) return result;

        var records = DialogueTable.ParseCsv(text);
        if (records.Count == 0) return result;

        var header = records[0];
        int Col(string name, int fallback)
        {
            for (int i = 0; i < header.Count; i++) if (header[i].Trim() == name) return i;
            return fallback;
        }
        int cScene = Col("씬", 0), cKind = Col("종류", 1), cName = Col("이름", 2), cX = Col("x", 3), cY = Col("y", 4),
            cW = Col("너비", 5), cH = Col("높이", 6), cTo = Col("가는 씬", 7), cToSpawn = Col("가는 도착", 8),
            cShow = Col("표시", 9), cMsg = Col("메시지", 10), cCond = Col("조건", 11), cSec = Col("시간", 12),
            cReason = Col("이유", 13);

        for (int i = 1; i < records.Count; i++)
        {
            var rec = records[i];
            string Get(int c) => c < rec.Count ? rec[c].Trim() : "";
            float Num(int c, float def) =>
                float.TryParse(Get(c), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : def;

            string scene = Get(cScene);
            if (scene == "" || scene.StartsWith("#")) continue;
            result.Add(new Row
            {
                scene = scene, kind = Get(cKind), name = Get(cName),
                pos = new Vector2(Num(cX, 0f), Num(cY, 0f)), size = new Vector2(Num(cW, 1f), Num(cH, 1f)),
                toScene = Get(cTo), toSpawn = Get(cToSpawn), show = Get(cShow), message = Get(cMsg),
                condition = Get(cCond), seconds = Num(cSec, 0f), reason = Get(cReason),
            });
        }
        return result;
    }
}
