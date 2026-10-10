using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapMarker : MonoBehaviour
{
    public string label;
    public Color color = Color.cyan;
    public Vector2 size = Vector2.one;

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
#if UNITY_EDITOR
        var style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = color;
        Handles.Label(transform.position + new Vector3(-size.x * 0.5f + 0.1f, size.y * 0.5f - 0.1f, 0f), label, style);
#endif
    }
}
