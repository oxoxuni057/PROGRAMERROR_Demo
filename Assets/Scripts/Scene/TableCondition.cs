using UnityEngine;

// 문 연결 표에서 만든 물체를 [조건]일 때만 보이게 함 (조건이 아니면 그림과 충돌을 끔)
// ROLLBACK으로 플래그가 되돌아가면 물체도 다시 맞춰짐
public class TableCondition : MonoBehaviour
{
    string condition;
    Renderer[] renderers;
    Collider2D[] colliders;
    bool applied, visible;

    public void Setup(string cond)
    {
        condition = cond;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
        applied = false;
        Apply();
    }

    void Update() => Apply();

    void Apply()
    {
        if (renderers == null) return;
        bool show = GameFlags.Check(condition);
        if (applied && show == visible) return;
        applied = true;
        visible = show;
        foreach (var r in renderers) if (r != null) r.enabled = show;
        foreach (var c in colliders) if (c != null) c.enabled = show;
    }
}
