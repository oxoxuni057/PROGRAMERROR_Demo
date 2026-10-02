using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [SerializeField] string prompt = "조사하기";   // 나중에 화면에 "E: 조사하기"로 표시할 글자
    [SerializeField] UnityEvent onInteract;        // 조사했을 때 실행할 동작 (Inspector에서 연결)

    public string Prompt => prompt;

    // 플레이어가 E를 누르면 이 함수가 실행됨
    public virtual void Interact(PlayerController player)
    {
        Debug.Log($"[상호작용] {name}");          // Console에 "[상호작용] 물체이름" 출력
        onInteract?.Invoke();                      // Inspector에 연결한 동작 실행
    }
}