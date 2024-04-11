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
            // 초기값 대입
            timerRect = timerImage.GetComponent<RectTransform>();
            maxTimerWidth = timerRect.parent.GetComponent<RectTransform>().sizeDelta.x;
        }
        
        // 타이머의 텍스트와 width를 비율에 맞게 업데이트
        float ratio = time / max;
        timerText.text = Mathf.CeilToInt(time).ToString();
        timerRect.sizeDelta = new Vector2(ratio * maxTimerWidth, timerRect.sizeDelta.y);
    }
}
