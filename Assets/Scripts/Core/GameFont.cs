using UnityEngine;

// 게임 전체 글꼴: 깍두기 도면체 (Assets/Resources/Fonts, SIL OFL 1.1 → 같은 폴더 OFL.txt)
// 사용법: text.font = GameFont.Regular;  (제목·강조는 GameFont.Bold)
// 폰트 파일이 없으면 Unity 기본 글꼴로 대신 나옴
public static class GameFont
{
    static Font regular, bold;

    public static Font Regular => regular != null ? regular : (regular = Load("Fonts/KkakdugiDrafting-Regular"));
    public static Font Bold => bold != null ? bold : (bold = Load("Fonts/KkakdugiDrafting-Bold"));

    static Font Load(string path)
    {
        var font = Resources.Load<Font>(path);
        if (font == null)
        {
            Debug.LogWarning($"[글꼴] {path} 없음 → 기본 글꼴 사용");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return font;
    }
}
