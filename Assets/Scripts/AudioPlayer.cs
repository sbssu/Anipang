using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource popSource;

    const float MAX_TIME = 2.0f;
    const float MAX_PITCH = 4;

    float timer = 0.0f;     // ���� �ð� �ȿ� Pop ȿ������ ����ϸ� pitch�� ������.
    int pitch = 0;          // �� 4�ܰ���� �����Ѵ�.


    private void Update()
    {
        // pitch �ʱ�ȭ ����.
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
