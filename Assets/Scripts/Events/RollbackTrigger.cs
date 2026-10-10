using UnityEngine;

// Inspector에서 [ROLLBACK]을 실행하기 위한 부품
// EventZone의 On Enter 등에 RollbackTrigger.Play를 연결하면 됨 (테스트용 "위반" 구역 만들 때)
public class RollbackTrigger : MonoBehaviour
{
    [SerializeField] string reason = "";   // Console에 찍힐 위반 이유 (예: 칠판 이름 지움)

    public void Play()
    {
        Rollback.Play(reason);
    }
}
