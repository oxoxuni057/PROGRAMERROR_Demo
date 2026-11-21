using UnityEngine;

// 문 연결 표에서 만든 "여기로 가세요" 표시
//  - 아래를 가리키는 노란 화살표가 위아래로 살짝 움직임
//  - 종류 표시 (문 앞 화살표), 종류 조사 (칠판·거울·게시판 위 작은 화살표)에 씀
//  - 종류 글씨 : 맵 위에 글자를 씀 (칠판의 이름 등)
public static class TableMarker
{
    static Sprite arrow;

    // 아래를 가리키는 화살표 그림 (코드로 그림, 9×6 칸)
    static Sprite Arrow()
    {
        if (arrow != null) return arrow;
        const int w = 9, h = 6;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // 위쪽이 넓고 아래로 갈수록 좁아지는 삼각형
                int half = y;   // y=0(맨 아래) → 폭 1칸, y=5 → 폭 11칸
                bool inside = Mathf.Abs(x - 4) <= half - 1 || (y == 0 && x == 4);
                tex.SetPixel(x, y, inside ? Color.white : clear);
            }
        tex.Apply();
        arrow = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        return arrow;
    }

    public static void AddArrow(GameObject go, Vector2 offset, float scale, Color color)
    {
        var child = new GameObject("화살표");
        child.transform.SetParent(go.transform, false);
        child.transform.localPosition = offset;
        child.transform.localScale = Vector3.one * scale;
        var sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = Arrow();
        sr.color = color;
        sr.sortingOrder = 200;   // 타일·캐릭터 위에 보이게
        child.AddComponent<Bob>();
    }

    public static void AddText(GameObject go, string text, Color color)
    {
        var child = new GameObject("글씨");
        child.transform.SetParent(go.transform, false);
        var tm = child.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = GameFont.Regular;
        tm.fontSize = 48;
        tm.characterSize = 0.06f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var mr = child.GetComponent<MeshRenderer>();
        if (tm.font != null) mr.sharedMaterial = tm.font.material;
        mr.sortingOrder = 150;
    }

    // 위아래로 살짝 움직이기
    class Bob : MonoBehaviour
    {
        Vector3 start;
        void Start() => start = transform.localPosition;
        void Update() => transform.localPosition = start + Vector3.up * (Mathf.Sin(Time.unscaledTime * 4f) * 0.08f);
    }
}
