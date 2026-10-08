using UnityEngine;
using UnityEngine.Events;

// Inspector에서 글리치 효과를 쓰기 위한 부품
// EventZone의 On Enter나 Interactable의 On Interact에 GlitchTrigger.Play를 연결하면 됨
public class GlitchTrigger : MonoBehaviour
{
    public enum Kind
    {
        Glitch,         // 화면 전체 치지직 (+ 글자)
        CornerFlash,    // 오른쪽 위에 글자만 짧게 깜빡
        ProgramError,   // 치지직 + [PROGRAM ERROR] → 암전
    }

    [SerializeField] Kind kind = Kind.Glitch;
    [SerializeField] float duration = 0.6f;           // 효과 시간(초)
    [SerializeField, TextArea] string message = "";   // 띄울 글자 (ProgramError는 자동)
    [SerializeField] float shake = 0.15f;             // 흔들림 세기 (Glitch만 사용, 0이면 안 흔듦)
    [SerializeField] bool lockPlayer = true;          // 효과 중 못 움직이게
    [SerializeField] UnityEvent onFinished;           // 효과가 끝나면 실행할 동작

    PlayerController player;

    public void Play()
    {
        var fx = GlitchEffect.Instance;
        if (fx == null) return;

        if (lockPlayer) SetPlayerControl(false);

        switch (kind)
        {
            case Kind.Glitch:
                fx.Play(duration, message, shake, Done);
                break;
            case Kind.CornerFlash:
                fx.FlashCorner(message, duration, Done);
                break;
            case Kind.ProgramError:
                fx.PlayProgramError(duration, () => onFinished?.Invoke());   // 암전 상태라 잠금은 풀지 않음
                break;
        }
    }

    // 암전 풀기 (ProgramError 뒤에 사용)
    public void HideBlackout()
    {
        if (GlitchEffect.Instance != null) GlitchEffect.Instance.HideBlackout();
    }

    void Done()
    {
        if (lockPlayer) SetPlayerControl(true);
        onFinished?.Invoke();
    }

    void SetPlayerControl(bool on)
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.CanMove = on;
    }
}
