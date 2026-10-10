using UnityEngine;

// On Interact, On Dialogue End 같은 이벤트 칸에서 플래그를 켜고 끌 때 사용
public class FlagSetter : MonoBehaviour
{
    public void SetFlag(string flag) => GameFlags.Set(flag);
    public void ClearFlag(string flag) => GameFlags.Clear(flag);
}