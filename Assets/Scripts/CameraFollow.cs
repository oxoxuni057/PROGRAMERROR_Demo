using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;          // 따라갈 대상 (비워두면 자동으로 플레이어를 찾음)
    [SerializeField] float smoothTime = 0.15f;  // 따라가는 부드러움 (작을수록 빨리 붙음)

    Vector3 velocity;                           // SmoothDamp가 내부적으로 쓰는 값

    void LateUpdate()
    {
        // 1) 따라갈 대상이 없으면 씬에서 PlayerController가 붙은 물체를 찾는다
        if (target == null)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;          // 플레이어가 없으면 아무것도 안 함
            target = player.transform;
        }

        // 2) 목표 위치 = 플레이어의 x, y + 카메라 원래 z
        Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);

        // 3) 현재 위치에서 목표 위치로 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}