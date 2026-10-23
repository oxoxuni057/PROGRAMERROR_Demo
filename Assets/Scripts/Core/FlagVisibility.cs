using UnityEngine;

// 플래그에 따라 물체를 보이거나 숨김
// 예: 쪽지에 붙이고 Flag = rule_a_found, Mode = HideWhenFlagOn → 읽으면 사라지고,
//     ROLLBACK으로 플래그가 되돌아가면 다시 나타남 (씬을 다시 불러와도 플래그대로 맞춰짐)
// GameObject를 끄지 않고 그림(Renderer)과 충돌(Collider2D)만 끄기 때문에 이 스크립트는 계속 동작함
public class FlagVisibility : MonoBehaviour
{
    public enum Mode
    {
        HideWhenFlagOn,   // 플래그가 켜지면 숨김 (주운 쪽지, 사라지는 물체)
        ShowWhenFlagOn,   // 플래그가 켜지면 나타남 (나중에 생기는 문, 쪽지)
    }

    [SerializeField] string flag;
    [SerializeField] Mode mode = Mode.HideWhenFlagOn;

    Renderer[] renderers;
    Collider2D[] colliders;
    bool applied;
    bool visible;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    void Start() => Apply();
    void Update() => Apply();

    void Apply()
    {
        bool on = GameFlags.Has(flag);
        bool shouldShow = mode == Mode.HideWhenFlagOn ? !on : on;
        if (applied && shouldShow == visible) return;

        applied = true;
        visible = shouldShow;
        foreach (var r in renderers) if (r != null) r.enabled = shouldShow;
        foreach (var c in colliders) if (c != null) c.enabled = shouldShow;
    }
}
