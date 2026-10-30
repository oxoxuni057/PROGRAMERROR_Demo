using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// [테스트 전용] 색 필터·아날로그 TV·글리치·ROLLBACK을 숫자 키로 바로 확인 (에디터에서만 동작)
// 사용법: 아무 씬에 빈 오브젝트 → 이 스크립트 붙이기 → ▶ → 숫자 키 / 확인 후 씬은 저장하지 않기
// 1 정상 / 2 점검 / 3 2층 / 4 오프닝(치지직 → PROGRAM ERROR 암전 → 점검 세계) / 5 ROLLBACK
// 방향키: 카메라 이동, Q/E: 줌
public class FilterTestKeys : MonoBehaviour
{
    [SerializeField] float panSpeed = 12f;   // 방향키 카메라 이동 속도
    [SerializeField] float blackoutSeconds = 2f;   // 4번: 암전 유지 시간

    Camera cam;
    string status = "1 정상";
    GUIStyle style;

    void Awake()
    {
        if (!Application.isEditor) { Destroy(this); return; }
        DontDestroyOnLoad(gameObject);   // 5번(ROLLBACK)으로 씬이 다시 불려도 남아 있게
        cam = Camera.main;
        if (cam == null) cam = MakeCamera();
    }

    // 씬에 카메라가 없으면 맵 한가운데를 보는 카메라를 만듦
    Camera MakeCamera()
    {
        var go = new GameObject("TestCamera");
        go.transform.SetParent(transform, false);
        go.tag = "MainCamera";
        var c = go.AddComponent<Camera>();
        c.orthographic = true;
        c.orthographicSize = 8f;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = Color.black;

        var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        Vector3 center = Vector3.zero;
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            center = b.center;
        }
        go.transform.position = new Vector3(center.x, center.y, -10f);
        return c;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame)
        {
            GameFlags.Clear(ScreenFilter.InspectionFlag);
            ScreenFilter.Set(ScreenFilter.Look.Normal);
            status = "1 정상";
        }
        if (kb.digit2Key.wasPressedThisFrame) { ScreenFilter.Set(ScreenFilter.Look.Inspection, 1f); status = "2 점검 (TV 0.5)"; }
        if (kb.digit3Key.wasPressedThisFrame) { ScreenFilter.Set(ScreenFilter.Look.Floor2, 1f); status = "3 2층 (TV 1.0)"; }
        if (kb.digit4Key.wasPressedThisFrame) StartCoroutine(Opening());
        if (kb.digit5Key.wasPressedThisFrame) { Rollback.Play("테스트"); status = "5 ROLLBACK"; }

        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Vector3 move = Vector3.zero;
        if (kb.leftArrowKey.isPressed) move.x -= 1f;
        if (kb.rightArrowKey.isPressed) move.x += 1f;
        if (kb.upArrowKey.isPressed) move.y += 1f;
        if (kb.downArrowKey.isPressed) move.y -= 1f;
        cam.transform.position += move * panSpeed * Time.unscaledDeltaTime;
        if (kb.qKey.isPressed) cam.orthographicSize = Mathf.Max(2f, cam.orthographicSize - 10f * Time.unscaledDeltaTime);
        if (kb.eKey.isPressed) cam.orthographicSize = Mathf.Min(40f, cam.orthographicSize + 10f * Time.unscaledDeltaTime);
    }

    // 4번: 실제 오프닝 순서 그대로
    IEnumerator Opening()
    {
        GameFlags.Clear(ScreenFilter.InspectionFlag);
        ScreenFilter.Set(ScreenFilter.Look.Normal);
        status = "4 오프닝: 치지직...";
        bool done = false;
        GlitchEffect.Instance.PlayProgramError(1.5f, () => done = true);
        while (!done) yield return null;

        ScreenFilter.EnterInspection();   // 암전 중에 점검 세계로
        status = "4 오프닝: 암전";
        yield return new WaitForSecondsRealtime(blackoutSeconds);
        GlitchEffect.Instance.HideBlackout();
        status = "4 오프닝: 점검 세계 (" + ScreenFilter.Instance.CurrentLook + ")";
    }

    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { font = GameFont.Regular, fontSize = 22 };
            style.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(16, 12, 900, 30), "[필터 테스트] " + status, style);
        GUI.Label(new Rect(16, 42, 900, 30), "1 정상  2 점검  3 2층  4 오프닝  5 ROLLBACK  |  방향키 이동  Q/E 줌", style);
    }
}
