using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 타이틀 화면: Title 씬의 빈 오브젝트에 붙이면 화면을 코드로 만듦
// 새로 시작 → 진행 상황(플래그·지침서) 초기화 후 Start Scene으로 이동
public class TitleMenu : MonoBehaviour
{
    [SerializeField] string startScene = "Test_Subin";   // 새로 시작하면 갈 씬 (Build Profiles에 등록 필요)
    [SerializeField] string startSpawnId = "";            // 그 씬의 SpawnPoint Id (비우면 씬 기본 위치)

    MenuList menu;
    Text title;
    bool started;
    float glitchTimer;

    void Start()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = MenuList.MakeCanvas(transform, "TitleCanvas", 100);
        MenuList.MakeFullImage(canvas.transform, Color.black);
        title = MenuList.MakeText(canvas.transform, font, "PROGRAM ERROR", 110, 160, new Color(0.93f, 0.93f, 0.9f));
        title.fontStyle = FontStyle.Bold;
        MenuList.MakeText(canvas.transform, font, "W·S 이동    E 선택", 24, -420, new Color(0.45f, 0.45f, 0.42f));
        menu = new MenuList(canvas.transform, font, new[] { "새로 시작", "종료" }, 40, -80, 80);
        glitchTimer = Random.Range(2f, 5f);
    }

    void Update()
    {
        GlitchTitle();
        if (started) return;

        int chosen = menu.HandleInput(Keyboard.current);
        if (chosen == 0) StartGame();
        else if (chosen == 1) MenuList.QuitGame();
    }

    void StartGame()
    {
        started = true;
        GameFlags.Restore(new string[0]);   // 진행 상황 초기화
        RuleBook.Restore(null);             // 지침서 비우기
        SceneTransition.Instance.LoadScene(startScene, startSpawnId);
    }

    // 제목이 가끔 짧게 어긋나며 빨갛게 깜빡임
    void GlitchTitle()
    {
        glitchTimer -= Time.unscaledDeltaTime;
        bool glitching = glitchTimer < 0f;
        title.rectTransform.anchoredPosition = new Vector2(glitching ? Random.Range(-14f, 14f) : 0f, 160f);
        title.color = glitching && Random.value > 0.5f ? new Color(0.7f, 0.08f, 0.12f) : new Color(0.93f, 0.93f, 0.9f);
        if (glitchTimer < -0.25f) glitchTimer = Random.Range(2f, 5f);
    }
}
