using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource popSource;

    const float MAX_PITCH = 4;

    float timer = 0.0f;     // 일정 시간 안에 Pop 효과음을 재생하면 pitch를 높힌다.

    public void SwitchBGM(bool isPlay)
    {
        if (isPlay)
            bgmSource.Play();
        else
            bgmSource.Stop();
    }
    public void PopBlock()
    {
        int pitch = (int)Mathf.Repeat(GameManager.Instance.Combo, MAX_PITCH);
        popSource.pitch = 1.0f + 0.1f * pitch;
        popSource.Play();
    }
}
