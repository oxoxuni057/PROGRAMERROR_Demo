using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float walkSpeed = 3f;
    [SerializeField] float runSpeed = 5.5f;

    public bool CanMove { get; set; } = true;          // 대화·컷신 중엔 false
    public bool IsRunning { get; private set; }        // 감지 시스템에서 사용
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    Rigidbody2D rb;
    Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;      // 탑다운이라 중력 없음
        rb.freezeRotation = true;  // 부딪혀도 회전 안 함
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (!CanMove || kb == null)
        {
            moveInput = Vector2.zero;
            IsRunning = false;
            return;
        }

        Vector2 input = Vector2.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;

        if (input.x != 0) input.y = 0;
        moveInput = input.normalized;  // 대각선이 더 빠르지 않게
        IsRunning = IsMoving && kb.leftShiftKey.isPressed;
        if (IsMoving) FacingDirection = moveInput;
    }

    void FixedUpdate()
    {
        float speed = IsRunning ? runSpeed : walkSpeed;
        rb.linearVelocity = moveInput * speed;
    }
}