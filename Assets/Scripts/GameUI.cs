using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] Text scoreText;
    [SerializeField] Text timerText;
    [SerializeField] Image timerImage;

    RectTransform timerRect;
    float maxTimerWidth;

    public void UpdateScore(int score)
    {
        scoreText.text = score.ToString();
    }
    public void UpdateTimer(float time, float max)
    {
        if (!timerRect)
        {
            // �ʱⰪ ����
            timerRect = timerImage.GetComponent<RectTransform>();
            maxTimerWidth = timerRect.parent.GetComponent<RectTransform>().sizeDelta.x;
        }
        
        // Ÿ�̸��� �ؽ�Ʈ�� width�� ������ �°� ������Ʈ
        float ratio = time / max;
        timerText.text = Mathf.CeilToInt(time).ToString();
        timerRect.sizeDelta = new Vector2(ratio * maxTimerWidth, timerRect.sizeDelta.y);
    }
}
