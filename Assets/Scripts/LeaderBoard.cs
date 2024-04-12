using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

class Record
{
    public List<int> scoreList;
    public Record()
    {
        scoreList = new List<int>();
    }

    public int AddScore(int score)
    {
        scoreList.Add(score);
        if (scoreList.Count > 10)
            scoreList = scoreList.Take(10).ToList();
        scoreList = scoreList.OrderByDescending(x => x).ToList();
        return scoreList.IndexOf(score);
    }
}

public class LeaderBoard : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] RectTransform rankParent;
    [SerializeField] RectTransform textParent;

    Record record;

    Text[] rankTexts;
    Text[] scoreTexts;


    private void Start()
    {
        string json = PlayerPrefs.GetString("LeaderBoard", string.Empty);
        if (string.IsNullOrEmpty(json))
            record = new Record();
        else
            record = JsonUtility.FromJson<Record>(json);

        rankTexts = rankParent.GetComponentsInChildren<Text>();
        scoreTexts = textParent.GetComponentsInChildren<Text>();
        panel.SetActive(false);
    }

    public int AddScore(int score)
    {
        if (score <= 0)
            return -1;

        int myRank = record.AddScore(score);
        if (myRank >= 0)
            PlayerPrefs.SetString("LeaderBoard", JsonUtility.ToJson(record));

        return myRank;
    }
    public void SwitchScore(bool isOn, int myRank = -1)
    {
        if (isOn)
        {
            for (int i = 0; i < scoreTexts.Length; i++)
            {
                if (i < record.scoreList.Count)
                    scoreTexts[i].text = record.scoreList[i].ToString();
                else
                    scoreTexts[i].text = "0";

                Color textColor = (i == myRank) ? new Color(1f, 0.2f, 0.2f) : Color.black;
                rankTexts[i].color = textColor;
                scoreTexts[i].color = textColor;
            }
        }
        panel.SetActive(isOn);
    }
}

