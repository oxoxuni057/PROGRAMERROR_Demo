using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float walkSpeed = 3f;
    [SerializeField] float runSpeed = 5.5f;
    [SerializeField] bool fourWay = true;   // 4방향 이동 (끄면 예전처럼 8방향)

    // ───── 이동 잠금 ─────
    // 여러 시스템(대화, 지침서, 씬 이동, ROLLBACK…)이 동시에 잠글 수 있어서
    // "누가 잠갔는지" 목록으로 관리함. 목록이 비어야 움직일 수 있음
    static readonly HashSet<object> locks = new HashSet<object>();
    static bool legacyLock;   // 예전 방식(CanMove = false)으로 잠근 것

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLocks() { locks.Clear(); legacyLock = false; }

    // 잠금 걸기/풀기: PlayerController.SetLock(this, true / false)
    public static void SetLock(object who, bool locked)
    {
        if (who == null) return;
        if (locked) locks.Add(who); else locks.Remove(who);
    }

    // 예전 방식도 계속 동작 (EventZone, GlitchTrigger 등). 단, 다른 시스템의 잠금은 못 풂
    public bool CanMove
    {
        get => !legacyLock && locks.Count == 0;
        set => legacyLock = !value;
    }
    public bool IsRunning { get; private set; }        // 감지 시스템에서 사용
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    Rigidbody2D rb;
    Vector2 moveInput;
    bool lastHorizontal;   // 두 방향을 같이 누르면 마지막에 누른 쪽(가로/세로)을 따름

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

        // 방금 누른 키가 가로인지 세로인지 기억
        if (kb.aKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
            kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) lastHorizontal = true;
        if (kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame ||
            kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) lastHorizontal = false;

        // 4방향: 가로·세로를 같이 누르면 마지막에 누른 쪽만 사용
        if (fourWay && input.x != 0 && input.y != 0)
        {
            if (lastHorizontal) input.y = 0;
            else input.x = 0;
        }

        moveInput = input.normalized;  // 8방향일 때 대각선이 더 빠르지 않게
        IsRunning = IsMoving && kb.leftShiftKey.isPressed;
        if (IsMoving) FacingDirection = moveInput;
    }

    void FixedUpdate()
    {
        float speed = IsRunning ? runSpeed : walkSpeed;
        rb.linearVelocity = moveInput * speed;
    }
}