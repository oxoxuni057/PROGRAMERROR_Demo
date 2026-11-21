using System;
using System.Collections.Generic;
using UnityEngine;

// 효과음 이름표: 이름 → 소리 파일
// 파일: Assets/Resources/Sound/SoundLibrary.asset (Project 창에서 클릭하면 Inspector에서 목록을 볼 수 있음)
// 새 효과음을 넣으면 Sounds 목록 맨 아래 + 를 눌러 이름을 적고 소리 파일을 끌어다 놓기
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "PROGRAM ERROR/효과음 이름표")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string name;     // 코드나 SoundTrigger에서 부를 이름 (예: glitch_short)
        public AudioClip clip;  // 소리 파일
    }

    public List<Entry> sounds = new List<Entry>();

    public AudioClip Find(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return null;
        foreach (var e in sounds)
            if (e != null && e.name == soundName) return e.clip;
        return null;
    }
}
