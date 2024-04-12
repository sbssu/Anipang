using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] Text scoreText;
    [SerializeField] Text timerText;
    [SerializeField] Image timerImage;
    [SerializeField] Image bombImage;
    [SerializeField] Image popupImage;
    [SerializeField] Text popupText;

    RectTransform timerRect;
    float maxTimerWidth;

    RectTransform bombRect;
    float maxBombWidth;

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
    public void UpdateBomb(float amount, float max)
    {
        if(!bombRect)
        {
            // �ʱⰪ ����
            bombRect = bombImage.GetComponent<RectTransform>();
            maxBombWidth = bombRect.parent.GetComponent<RectTransform>().sizeDelta.x;
        }

        float ratio = amount / max;
        bombRect.sizeDelta = new Vector2(ratio * maxBombWidth, bombRect.sizeDelta.y);
        
    }

    public void UpdateStartTimer(float time)
    {
        popupImage.gameObject.SetActive(time > 0);
        popupText.text = Mathf.CeilToInt(time).ToString();
        popupText.transform.localScale = Vector3.one * (time % 1f);
    }
    public void UpdatePopup(string text)
    {
        popupImage.gameObject.SetActive(!string.IsNullOrEmpty(text));
        popupText.text = text;
        popupText.transform.localScale = Vector3.one;
    }
}
