using UnityEngine;

// Inspector에서 효과음을 쓰기 위한 부품
// EventZone의 On Enter나 Interactable의 On Interact에 SoundTrigger.Play를 연결하면 됨
// 소리는 Sound Name에 이름표 이름(예: door_open)을 적거나, Clip 칸에 소리 파일을 직접 끌어다 놓기 (Clip이 우선)
public class SoundTrigger : MonoBehaviour
{
    public enum Mode
    {
        Once,       // 한 번 재생
        StartLoop,  // 반복 재생 시작 (형광등 웅웅, 물방울 뚝뚝 등)
        StopLoop,   // 반복 재생 멈추기 (같은 Sound Name을 적어야 함)
    }

    [SerializeField] Mode mode = Mode.Once;
    [SerializeField] string soundName = "door_open";
    [SerializeField] AudioClip clip;
    [SerializeField, Range(0f, 1f)] float volume = 1f;
    [SerializeField] float fadeSeconds = 1f;   // 반복 소리가 커지고 작아지는 시간 (Once는 사용 안 함)

    public void Play()
    {
        switch (mode)
        {
            case Mode.Once:
                if (clip != null) GameSound.Play(clip, volume);
                else GameSound.Play(soundName, volume);
                break;
            case Mode.StartLoop: GameSound.StartLoop(soundName, volume, fadeSeconds); break;
            case Mode.StopLoop: GameSound.StopLoop(soundName, fadeSeconds); break;
        }
    }
}
