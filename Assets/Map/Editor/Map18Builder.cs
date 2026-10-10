using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Map18Builder
{
    const string Root = "Assets/Map";
    const string SpriteDir = "Assets/Map/Tiles/Sprites";
    const string TileDir = "Assets/Map/Tiles";
    const string SceneDir = "Assets/Scenes/Map";
    const string PlayerPrefab = "Assets/Prefabs/Player.prefab";
    const int W = 50;
    const int H = 32;
    const int P = 32;

    static readonly HashSet<char> Solid = new HashSet<char> { '#', 'w', 'L', 'v', 'g', 'x', 'E', 'M' };

    class Mark
    {
        public string Name;
        public int X0, Y0, X1, Y1;
        public char Kind;
        public Color Color;
    }

    class Plan
    {
        public string Scene;
        public char[,] G = new char[W, H];
        public List<Mark> Rooms = new List<Mark>();
        public List<Mark> Events = new List<Mark>();
        public List<KeyValuePair<Vector2Int, string>> Decor = new List<KeyValuePair<Vector2Int, string>>();
        public Mark Stairs;
        public string StairsTarget;
        public Vector2Int Spawn;
        public List<Vector2Int> Patrol = new List<Vector2Int>();

        public Plan(string scene)
        {
            Scene = scene;
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    G[x, y] = (x == 0 || y == 0 || x == W - 1 || y == H - 1) ? '#' : 'w';
        }

        public void Fill(int x0, int y0, int x1, int y1, char c)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    G[x, y] = c;
        }

        public void Room(string name, int x0, int y0, int x1, int y1, char c)
        {
            Fill(x0, y0, x1, y1, c);
            Rooms.Add(new Mark { Name = name, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Kind = c, Color = new Color(0.4f, 0.9f, 1f) });
        }

        public void Label(string name, int x0, int y0, int x1, int y1)
        {
            Rooms.Add(new Mark { Name = name, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Kind = '-', Color = new Color(0.4f, 0.9f, 1f) });
        }

        public void Event(string name, int x0, int y0, int x1, int y1)
        {
            Events.Add(new Mark { Name = name, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Kind = 'e', Color = new Color(1f, 0.85f, 0.2f) });
        }

        public void Door(char c, int x, int y0, int y1)
        {
            for (int y = y0; y <= y1; y++) G[x, y] = c;
        }

        public void DoorRow(char c, int y, params int[] xs)
        {
            foreach (var x in xs) G[x, y] = c;
        }

        public void Put(string tile, int x, int y)
        {
            Decor.Add(new KeyValuePair<Vector2Int, string>(new Vector2Int(x, y), tile));
        }

        public void Corridors()
        {
            Fill(1, 8, 48, 10, '.');
            Fill(1, 22, 48, 24, '.');
            Fill(9, 8, 11, 24, '.');
            Fill(38, 8, 40, 24, '.');
        }
    }

    [MenuItem("PROGRAMERROR/맵/18관 1층 만들기")]
    public static void Build1F()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildFloor(Floor1());
    }

    [MenuItem("PROGRAMERROR/맵/18관 2층 만들기")]
    public static void Build2F()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildFloor(Floor2());
    }

    [MenuItem("PROGRAMERROR/맵/18관 1층 + 2층 모두 만들기")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildFloor(Floor2());
        BuildFloor(Floor1());
    }

    static Plan Floor1()
    {
        var p = new Plan("18관_1층");
        p.Corridors();

        p.Room("18113~18115 (잠김)", 1, 1, 3, 6, 'L');
        p.Room("18107 학생휴게실", 5, 1, 8, 6, 'u');
        p.Room("CU 편의점", 10, 1, 14, 6, 'u');
        p.Room("탈의실 (잠김)", 16, 1, 17, 3, 'L');
        p.Room("영양사실 (잠김)", 16, 5, 17, 6, 'L');
        p.Room("주방 (잠김)", 19, 1, 36, 6, 'L');
        p.Room("전기실 (잠김)", 38, 1, 41, 6, 'L');
        p.Room("18119 기계실 (잠김)", 43, 1, 48, 6, 'L');
        p.DoorRow('d', 7, 6, 7, 11, 12);
        p.DoorRow('x', 7, 2, 16, 27, 39, 45);

        p.Room("18106", 1, 12, 7, 16, 'c');
        p.Room("18105", 1, 18, 7, 20, 'c');
        p.Door('d', 8, 13, 15);
        p.Door('d', 8, 18, 20);

        p.Room("학생식당", 13, 11, 36, 15, 'k');
        p.Door('d', 12, 12, 14);
        p.Door('d', 37, 12, 14);
        p.Fill(13, 16, 36, 21, 'g');
        p.Label("잔디밭 (뚫림)", 13, 16, 21, 21);
        p.Fill(22, 16, 27, 16, 'w');
        p.Room("계단 (2층으로)", 22, 17, 27, 21, 's');
        p.Stairs = p.Rooms[p.Rooms.Count - 1];
        p.StairsTarget = "18관_2층";

        p.Room("18110", 42, 12, 48, 13, 'c');
        p.Room("18111", 42, 15, 48, 17, 'c');
        p.Room("18112", 42, 19, 48, 20, 'c');
        p.Door('d', 41, 12, 13);
        p.Door('d', 41, 15, 17);
        p.Door('d', 41, 19, 20);

        p.Room("여자화장실", 1, 26, 3, 30, 'r');
        p.Room("남자화장실", 5, 26, 6, 30, 'r');
        p.Room("18104", 8, 26, 13, 30, 'c');
        p.Room("18103", 15, 26, 20, 30, 'c');
        p.Room("로비", 22, 25, 30, 30, 'o');
        p.Room("18102", 32, 26, 37, 30, 'c');
        p.Room("18101", 39, 26, 44, 30, 'c');
        p.Room("남자화장실 (닫힘)", 46, 26, 46, 30, 'r');
        p.Room("여자화장실 (닫힘)", 48, 26, 48, 30, 'r');
        p.DoorRow('d', 25, 2, 3, 5, 6, 9, 10, 16, 17, 33, 34, 40, 41);
        p.DoorRow('x', 25, 46, 48);

        p.DoorRow('M', 31, 24, 25, 26);
        p.Door('E', 0, 8, 10);
        p.Door('E', 0, 23, 23);
        p.Door('E', 49, 8, 10);
        p.Door('E', 49, 23, 23);

        for (int x = 15; x <= 34; x += 5)
        {
            p.Put("table", x, 12); p.Put("table", x + 1, 12); p.Put("table", x + 2, 12);
            p.Put("table", x, 14); p.Put("table", x + 1, 14); p.Put("table", x + 2, 14);
        }
        p.Put("table", 6, 3); p.Put("table", 7, 3); p.Put("table", 6, 5); p.Put("table", 7, 5);
        for (int x = 11; x <= 14; x++) { p.Put("shelf", x, 2); p.Put("shelf", x, 4); }
        p.Put("board", 23, 26);

        p.Event("1 게시판: 읽을수록 바뀌는 공지 (끝까지 읽으면 ROLLBACK)", 23, 26, 23, 26);
        p.Event("2 정문: 열어도 다시 로비 → 문 닫고 기다리기", 24, 30, 26, 30);
        p.Event("3 게시판 아래 접힌 종이: 10번 규칙", 26, 26, 26, 26);
        p.Event("4 ??? 와 마주침 (1챕터 엔딩)", 20, 23, 20, 23);
        p.Event("5 학생식당: 오프닝 학식 장면", 17, 13, 17, 13);
        p.Event("(제안) 1층 구역 지침 쪽지", 29, 27, 29, 27);

        p.Spawn = new Vector2Int(24, 23);
        return p;
    }

    static Plan Floor2()
    {
        var p = new Plan("18관_2층");
        p.Corridors();

        p.Room("18210", 1, 1, 10, 6, 'c');
        p.Room("18211", 12, 1, 19, 6, 'c');
        p.Room("18211-1", 21, 1, 24, 6, 'c');
        p.Room("18212", 26, 1, 33, 6, 'c');
        p.Room("18212-1 (잠김)", 35, 1, 38, 6, 'L');
        p.Room("18213 (잠김)", 40, 1, 41, 6, 'L');
        p.Room("18213-1 (잠김)", 43, 1, 45, 6, 'L');
        p.Room("18214 (잠김)", 47, 1, 48, 6, 'L');
        p.DoorRow('d', 7, 5, 6, 8, 9, 14, 15, 17, 18, 22, 23, 28, 29, 31, 32);
        p.DoorRow('x', 7, 36, 40, 44, 47);

        p.Room("18209", 1, 12, 7, 14, 'c');
        p.Room("18208", 1, 16, 7, 17, 'c');
        p.Room("18207", 1, 19, 7, 20, 'c');
        p.Door('d', 8, 12, 14);
        p.Door('d', 8, 16, 17);
        p.Door('d', 8, 19, 20);

        p.Fill(12, 11, 37, 21, 'v');
        p.Label("중정 (뚫린 공간, 난간)", 12, 11, 21, 16);
        p.Room("계단 (1층으로)", 22, 17, 27, 21, 's');
        p.Stairs = p.Rooms[p.Rooms.Count - 1];
        p.StairsTarget = "18관_1층";

        p.Room("18215 랩실", 42, 12, 48, 13, 'l');
        p.Room("18216 랩실", 42, 15, 48, 15, 'l');
        p.Room("18217 불 켜진 랩실", 42, 17, 48, 18, 'y');
        p.Room("18218 랩실", 42, 20, 48, 20, 'l');
        p.Door('x', 41, 12, 12);
        p.Door('x', 41, 15, 15);
        p.Door('d', 41, 17, 18);
        p.Door('x', 41, 20, 20);

        p.Room("여자화장실", 1, 26, 3, 30, 'r');
        p.Room("남자화장실", 5, 26, 6, 30, 'r');
        p.Room("18206-2", 8, 26, 8, 30, 'L');
        p.Room("18206-1", 10, 26, 10, 30, 'L');
        p.Room("18206", 12, 26, 12, 30, 'L');
        p.Room("18205-2", 14, 26, 14, 30, 'L');
        p.Room("18205-1", 16, 26, 16, 30, 'L');
        p.Room("18205", 18, 26, 18, 30, 'L');
        p.Room("18204-1", 20, 26, 21, 30, 'c');
        p.Room("18204", 23, 26, 25, 30, 'c');
        p.Room("18203-2", 27, 26, 28, 30, 'L');
        p.Room("18203-1", 30, 26, 30, 30, 'L');
        p.Room("18203", 32, 26, 33, 30, 'L');
        p.Room("18202-1", 35, 26, 35, 30, 'L');
        p.Room("18202", 37, 26, 38, 30, 'L');
        p.Room("18201-1", 40, 26, 40, 30, 'L');
        p.Room("18201", 42, 26, 42, 30, 'L');
        p.Room("남자화장실", 44, 26, 45, 30, 'r');
        p.Room("여자화장실", 47, 26, 48, 30, 'r');
        p.DoorRow('d', 25, 2, 3, 5, 6, 20, 21, 23, 24, 44, 45, 47, 48);
        p.DoorRow('x', 25, 8, 10, 12, 14, 16, 18, 27, 30, 32, 35, 37, 40, 42);

        p.Door('E', 0, 8, 10);
        p.Door('E', 49, 8, 10);

        p.Put("extinguisher", 11, 8);
        p.Put("extinguisher", 38, 8);
        p.Put("extinguisher", 38, 22);
        p.Put("mirror", 48, 26);

        p.Event("1 계단에서 2층 도착", 23, 22, 26, 22);
        p.Event("2 '뚝, 뚝' 소리 → 18204-1에 숨기 (규칙 가, 나)", 18, 23, 18, 23);
        p.Event("3 불 켜진 랩실 18217 (규칙 마: 들어가지 않기)", 40, 17, 40, 18);
        p.Event("4 여자화장실 거울: 몸이 꼬여 보임 (규칙 바)", 47, 26, 48, 26);
        p.Event("5 18209 창가: 끊긴 빨간 쪽지 \"그를 믿지—\"", 5, 12, 5, 12);
        p.Event("6 18212: 줄 그어진 규칙 라 쪽지", 31, 2, 31, 2);
        p.Event("(제안) 규칙 가·나·다 쪽지", 29, 23, 29, 23);
        p.Event("(제안) 규칙 아 쪽지", 27, 2, 27, 2);
        p.Event("(제안) 그녀와 마주치는 곳", 10, 16, 10, 16);

        p.Patrol.Add(new Vector2Int(10, 9));
        p.Patrol.Add(new Vector2Int(39, 9));
        p.Patrol.Add(new Vector2Int(39, 23));
        p.Patrol.Add(new Vector2Int(10, 23));

        p.Spawn = new Vector2Int(25, 23);
        return p;
    }

    static Vector3Int Cell(int x, int y)
    {
        return new Vector3Int(x, -y - 1, 0);
    }

    static Vector3 Center(Mark m)
    {
        return new Vector3((m.X0 + m.X1 + 1) * 0.5f, -(m.Y0 + m.Y1 + 1) * 0.5f, 0f);
    }

    static Vector3 Center(Vector2Int c)
    {
        return new Vector3(c.x + 0.5f, -c.y - 0.5f, 0f);
    }

    static void BuildFloor(Plan p)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var t = PrepareTiles();

        var grid = new GameObject("Grid").AddComponent<Grid>();
        var floor = MakeMap(grid, "Floor", -20, true);
        var decor = MakeMap(grid, "Decor", -10, true);
        var walls = MakeMap(grid, "Walls", -5, true);
        var col = MakeMap(grid, "Collision", 0, false);

        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                char c = p.G[x, y];
                var pos = Cell(x, y);
                floor.SetTile(pos, t[FloorName(c)]);
                string wall = WallName(c);
                if (wall != null) walls.SetTile(pos, t[wall]);
                if (Solid.Contains(c)) col.SetTile(pos, t["collision"]);
            }
        }

        foreach (var r in p.Rooms) AutoDecor(p, r);
        foreach (var d in p.Decor) decor.SetTile(Cell(d.Key.x, d.Key.y), t[d.Value]);

        col.gameObject.AddComponent<TilemapCollider2D>();

        var rooms = new GameObject("Rooms (방 이름표)").transform;
        foreach (var r in p.Rooms)
        {
            var go = new GameObject(r.Name);
            go.transform.SetParent(rooms, false);
            go.transform.position = Center(r);
            var mk = go.AddComponent<MapMarker>();
            mk.label = r.Name;
            mk.color = r.Color;
            mk.size = new Vector2(r.X1 - r.X0 + 1, r.Y1 - r.Y0 + 1);
        }

        var events = new GameObject("Events (이벤트 위치)").transform;
        foreach (var e in p.Events)
        {
            var go = new GameObject("EV " + e.Name);
            go.transform.SetParent(events, false);
            go.transform.position = Center(e);
            var size = new Vector2(e.X1 - e.X0 + 1, e.Y1 - e.Y0 + 1);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            go.AddComponent<EventZone>();
            var mk = go.AddComponent<MapMarker>();
            mk.label = e.Name;
            mk.color = e.Color;
            mk.size = size;
        }

        if (p.Patrol.Count > 0)
        {
            var path = new GameObject("PatrolPath 그녀 순찰 경로").transform;
            for (int i = 0; i < p.Patrol.Count; i++)
            {
                var wp = new GameObject("Waypoint " + (i + 1));
                wp.transform.SetParent(path, false);
                wp.transform.position = Center(p.Patrol[i]);
                var mk = wp.AddComponent<MapMarker>();
                mk.label = "순찰 " + (i + 1);
                mk.color = new Color(0.9f, 0.1f, 0.15f);
                mk.size = new Vector2(0.6f, 0.6f);
            }
        }

        var stairs = new GameObject("Stairs → " + p.StairsTarget);
        stairs.transform.position = Center(p.Stairs) + new Vector3(0f, 1.5f, 0f);
        var sbox = stairs.AddComponent<BoxCollider2D>();
        sbox.isTrigger = true;
        sbox.size = new Vector2(6f, 2f);
        var door = stairs.AddComponent<SceneDoor>();
        var so = new SerializedObject(door);
        so.FindProperty("targetScene").stringValue = p.StairsTarget;
        so.FindProperty("targetSpawnId").stringValue = "stairs";
        so.ApplyModifiedPropertiesWithoutUndo();

        var spawnPos = Center(p.Spawn);
        MakeSpawn("Spawn stairs", "stairs", spawnPos);
        MakeSpawn("Spawn default", "default", spawnPos);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        if (prefab != null)
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = spawnPos;
        }

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(spawnPos.x, spawnPos.y, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(14, 13, 8, 255);
        var ppc = camGo.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = P;
        ppc.refResolutionX = 640;
        ppc.refResolutionY = 360;
        camGo.AddComponent<CameraFollow>();

        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        Directory.CreateDirectory(SceneDir);
        string scenePath = SceneDir + "/" + p.Scene + ".unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AddToBuild(scenePath);
        Debug.Log("[맵] " + scenePath + " 생성 완료");
    }

    static void MakeSpawn(string name, string id, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sp = go.AddComponent<SpawnPoint>();
        var so = new SerializedObject(sp);
        so.FindProperty("id").stringValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AutoDecor(Plan p, Mark r)
    {
        int w = r.X1 - r.X0 + 1;
        int h = r.Y1 - r.Y0 + 1;
        if (r.Kind == 'c')
        {
            int ys = h >= 3 ? r.Y0 + 1 : r.Y0;
            int ye = h >= 3 ? r.Y1 - 1 : r.Y0;
            int xs = w >= 3 ? r.X0 + 1 : r.X0;
            int xe = w >= 3 ? r.X1 - 1 : r.X1;
            for (int y = ys; y <= ye; y += 2)
                for (int x = xs; x <= xe; x += 2)
                    p.Put("desk", x, y);
        }
        else if (r.Kind == 'r')
        {
            for (int x = r.X0; x <= r.X1; x++) p.Put("stall", x, r.Y1);
        }
        else if (r.Kind == 'l' || r.Kind == 'y')
        {
            int y = r.Y0 + (h - 1) / 2;
            for (int x = r.X0 + 1; x <= r.X1 - 1; x++)
                if ((x - r.X0) % 4 != 0) p.Put("bench", x, y);
        }
    }

    static string FloorName(char c)
    {
        switch (c)
        {
            case 'c': return "floor_classroom";
            case 'k': return "floor_cafeteria";
            case 'r': return "floor_toilet";
            case 'o': return "floor_lobby";
            case 'u': return "floor_lounge";
            case 'l': return "floor_lab";
            case 'y': return "floor_lab_lit";
            case 'L': return "locked";
            case 'v': return "void";
            case 'g': return "grass";
            case 's': return "stairs";
            case 'd': return "door";
            default: return "floor_corridor";
        }
    }

    static string WallName(char c)
    {
        switch (c)
        {
            case '#': return "wall_outer";
            case 'w': return "wall";
            case 'x': return "door_locked";
            case 'E': return "exit";
            case 'M': return "main_door";
            default: return null;
        }
    }

    static Tilemap MakeMap(Grid grid, string name, int order, bool visible)
    {
        var go = new GameObject(name);
        go.transform.SetParent(grid.transform, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = order;
        r.enabled = visible;
        return tm;
    }

    static void AddToBuild(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == path)) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    class Pix
    {
        public Color32[] A = new Color32[P * P];

        public void Set(int x, int y, Color32 c)
        {
            if (x >= 0 && y >= 0 && x < P && y < P) A[y * P + x] = c;
        }

        public void Rect(int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    Set(x, y, c);
        }

        public void Fill(Color32 c)
        {
            Rect(0, 0, P - 1, P - 1, c);
        }

        public void Frame(int x0, int y0, int x1, int y1, int t, Color32 c)
        {
            Rect(x0, y0, x1, y0 + t - 1, c);
            Rect(x0, y1 - t + 1, x1, y1, c);
            Rect(x0, y0, x0 + t - 1, y1, c);
            Rect(x1 - t + 1, y0, x1, y1, c);
        }
    }

    static Color32 Hex(string h)
    {
        Color c;
        ColorUtility.TryParseHtmlString(h, out c);
        return c;
    }

    static Dictionary<string, TileBase> PrepareTiles()
    {
        Directory.CreateDirectory(SpriteDir);
        Directory.CreateDirectory(SceneDir);
        AssetDatabase.Refresh();

        var wall = Hex("#4A4A3C");
        var shadow = Hex("#3A3A2E");
        var floorC = Hex("#A9A28C");
        var light = Hex("#D9D2B6");
        var wood = Hex("#8A6E52");
        var paper = Hex("#EEF0D8");

        var t = new Dictionary<string, TileBase>();

        t["floor_corridor"] = MakeTile("floor_corridor", p =>
        {
            p.Fill(floorC);
            p.Rect(0, 0, 31, 0, Hex("#9C957F"));
            p.Rect(0, 0, 0, 31, Hex("#9C957F"));
        }, false);

        t["floor_classroom"] = MakeTile("floor_classroom", p =>
        {
            p.Fill(wood);
            for (int y = 0; y < P; y += 8) p.Rect(0, y, 31, y, Hex("#7A604A"));
            p.Rect(10, 1, 10, 7, Hex("#7A604A"));
            p.Rect(24, 9, 24, 15, Hex("#7A604A"));
            p.Rect(6, 17, 6, 23, Hex("#7A604A"));
            p.Rect(20, 25, 20, 31, Hex("#7A604A"));
        }, false);

        t["floor_cafeteria"] = MakeTile("floor_cafeteria", p =>
        {
            p.Fill(floorC);
            p.Rect(0, 0, 15, 15, Hex("#B9B29B"));
            p.Rect(16, 16, 31, 31, Hex("#B9B29B"));
        }, false);

        t["floor_toilet"] = MakeTile("floor_toilet", p =>
        {
            p.Fill(light);
            for (int x = 0; x < P; x += 8)
                for (int y = 0; y < P; y += 8)
                    if (((x + y) / 8) % 2 == 0) p.Rect(x, y, x + 7, y + 7, Hex("#C6BFA3"));
        }, false);

        t["floor_lobby"] = MakeTile("floor_lobby", p =>
        {
            p.Fill(light);
            p.Rect(0, 0, 31, 0, Hex("#BDB69C"));
            p.Rect(0, 0, 0, 31, Hex("#BDB69C"));
        }, false);

        t["floor_lounge"] = MakeTile("floor_lounge", p =>
        {
            p.Fill(Hex("#9C957F"));
            for (int x = 3; x < P; x += 8)
                for (int y = 3; y < P; y += 8)
                    p.Set(x, y, wood);
        }, false);

        t["floor_lab"] = MakeTile("floor_lab", p =>
        {
            p.Fill(Hex("#8F8875"));
            p.Rect(0, 0, 31, 0, Hex("#7E7866"));
            p.Rect(0, 0, 0, 31, Hex("#7E7866"));
        }, false);

        t["floor_lab_lit"] = MakeTile("floor_lab_lit", p =>
        {
            p.Fill(Hex("#C9BE96"));
            p.Rect(0, 0, 31, 0, Hex("#B3A985"));
            p.Rect(0, 0, 0, 31, Hex("#B3A985"));
        }, false);

        t["locked"] = MakeTile("locked", p =>
        {
            p.Fill(wall);
            for (int x = 0; x < P; x++)
                for (int y = 0; y < P; y++)
                    if ((x + y) % 6 == 0) p.Set(x, y, Hex("#5A5A4A"));
        }, false);

        t["void"] = MakeTile("void", p =>
        {
            p.Fill(Hex("#141410"));
        }, false);

        t["grass"] = MakeTile("grass", p =>
        {
            p.Fill(Hex("#3F4A32"));
            for (int x = 0; x < P; x++)
                for (int y = 0; y < P; y++)
                    if ((x * 7 + y * 3) % 11 == 0) p.Set(x, y, Hex("#4F5C3A"));
        }, false);

        t["stairs"] = MakeTile("stairs", p =>
        {
            p.Fill(wood);
            for (int y = 0; y < P; y += 6) p.Rect(0, y, 31, y + 1, Hex("#6E5640"));
            p.Rect(0, 0, 1, 31, wall);
            p.Rect(30, 0, 31, 31, wall);
        }, false);

        t["door"] = MakeTile("door", p =>
        {
            p.Fill(floorC);
            p.Rect(2, 2, 29, 29, wood);
            p.Frame(2, 2, 29, 29, 2, Hex("#6E5640"));
        }, false);

        t["wall"] = MakeTile("wall", p =>
        {
            p.Fill(wall);
            p.Rect(0, 29, 31, 31, Hex("#5E5E4E"));
            p.Rect(0, 0, 31, 1, shadow);
        }, false);

        t["wall_outer"] = MakeTile("wall_outer", p =>
        {
            p.Fill(shadow);
            for (int y = 0; y < P; y += 8)
            {
                p.Rect(0, y, 31, y, Hex("#2E2E24"));
                int off = (y / 8) % 2 == 0 ? 8 : 24;
                p.Rect(off, y, off, y + 7, Hex("#2E2E24"));
            }
        }, false);

        t["door_locked"] = MakeTile("door_locked", p =>
        {
            p.Fill(Hex("#6E5A40"));
            p.Frame(0, 0, 31, 31, 2, wall);
            p.Rect(24, 14, 26, 17, light);
        }, false);

        t["exit"] = MakeTile("exit", p =>
        {
            p.Fill(wall);
            p.Rect(6, 10, 25, 21, Hex("#3E7A4A"));
            p.Rect(13, 12, 17, 19, paper);
        }, false);

        t["main_door"] = MakeTile("main_door", p =>
        {
            p.Fill(Hex("#2A2E2C"));
            p.Frame(0, 0, 31, 31, 3, wall);
            p.Rect(15, 0, 16, 31, light);
        }, false);

        t["desk"] = MakeTile("desk", p =>
        {
            p.Rect(4, 12, 27, 25, wood);
            p.Rect(4, 23, 27, 25, Hex("#9F8263"));
            p.Rect(11, 3, 20, 9, wall);
        }, false);

        t["table"] = MakeTile("table", p =>
        {
            p.Rect(0, 8, 31, 23, wood);
            p.Rect(0, 21, 31, 23, Hex("#9F8263"));
        }, false);

        t["shelf"] = MakeTile("shelf", p =>
        {
            p.Rect(0, 6, 31, 25, floorC);
            p.Frame(0, 6, 31, 25, 2, wood);
            p.Rect(4, 10, 9, 21, light);
            p.Rect(13, 10, 18, 21, paper);
            p.Rect(22, 10, 27, 21, light);
        }, false);

        t["stall"] = MakeTile("stall", p =>
        {
            p.Frame(1, 0, 30, 31, 2, Hex("#C6BFA3"));
            p.Rect(11, 6, 20, 18, paper);
            p.Rect(13, 8, 18, 16, Hex("#BDB69C"));
        }, false);

        t["bench"] = MakeTile("bench", p =>
        {
            p.Rect(0, 10, 31, 21, Hex("#6E6E60"));
            p.Rect(0, 19, 31, 21, Hex("#85857A"));
        }, false);

        t["board"] = MakeTile("board", p =>
        {
            p.Rect(2, 6, 29, 27, light);
            p.Frame(2, 6, 29, 27, 2, wood);
            p.Rect(6, 10, 13, 18, paper);
            p.Rect(16, 14, 25, 23, paper);
            p.Rect(8, 20, 14, 24, paper);
        }, false);

        t["extinguisher"] = MakeTile("extinguisher", p =>
        {
            p.Rect(12, 4, 19, 22, Hex("#A0602A"));
            p.Rect(14, 23, 17, 27, wall);
        }, false);

        t["mirror"] = MakeTile("mirror", p =>
        {
            p.Rect(4, 14, 27, 29, Hex("#BDB69C"));
            p.Rect(6, 16, 25, 27, Hex("#D8DCD0"));
        }, false);

        t["collision"] = MakeTile("collision", p =>
        {
            p.Fill(new Color32(255, 0, 255, 255));
        }, true);

        AssetDatabase.SaveAssets();
        return t;
    }

    static TileBase MakeTile(string name, System.Action<Pix> paint, bool collide)
    {
        string png = SpriteDir + "/" + name + ".png";
        if (!File.Exists(png))
        {
            var pix = new Pix();
            paint(pix);
            var tex = new Texture2D(P, P, TextureFormat.RGBA32, false);
            tex.SetPixels32(pix.A);
            tex.Apply();
            File.WriteAllBytes(png, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(png);
            var imp = (TextureImporter)AssetImporter.GetAtPath(png);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = P;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }

        string path = TileDir + "/" + name + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);
            tile.colliderType = collide ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
        }
        else if (tile.sprite == null)
        {
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);
            EditorUtility.SetDirty(tile);
        }
        return tile;
    }
}
