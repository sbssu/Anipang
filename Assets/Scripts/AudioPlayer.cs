using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource popSource;

    const float MAX_TIME = 2.0f;
    const float MAX_PITCH = 4;

    float timer = 0.0f;     // 일정 시간 안에 Pop 효과음을 재생하면 pitch를 높힌다.
    int pitch = 0;          // 총 4단계까지 존재한다.


    private void Update()
    {
        // pitch 초기화 로직.
        timer = Mathf.Clamp(timer - Time.deltaTime, 0.0f, MAX_TIME);
        if (pitch > 0 && timer <= 0.0f)
            pitch = 0;
    }

    public void SwitchBGM(bool isPlay)
    {
        if (isPlay)
            bgmSource.Play();
        else
            bgmSource.Stop();
    }
    public void PopBlock()
    {
        timer = MAX_TIME;
        pitch = (int)Mathf.Clamp(pitch + 1, 0, MAX_PITCH);

        popSource.pitch = 0.9f + 0.1f * pitch;
        popSource.Play();

        if (pitch >= MAX_PITCH)
            pitch = 0;
    }
}
