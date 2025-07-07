using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("타이머 UI")]
    public List<GameObject> sectorsPanel;
    public TextMeshProUGUI labTimerText;
    public TextMeshProUGUI bestLabTimerText;
    
    [Header("차량 대쉬보드 UI")]
    public TextMeshProUGUI dashBoardText;
    
    [Header("랭크 UI")]
    public List<TextMeshProUGUI> rankText;
    private int rankingTextCount;

    void Awake()
    {
        if (sectorsPanel != null)
        {
            int sectorCount = GameObject.Find("CheckPoint").transform.childCount;

            if (sectorCount > 0)
            {
                for (int i = 0; i < sectorCount; i++)
                {
                    sectorsPanel.Add(GameObject.Find($"Canvas/TimerPanel/S{i + 1}Panel"));
                }
            }
        }

        if (!labTimerText)
        {
            labTimerText = GameObject.Find("Canvas/TimerPanel/LabTimer").GetComponent<TextMeshProUGUI>();
        }

        if (!bestLabTimerText)
        {
            bestLabTimerText = GameObject.Find("Canvas/TimerPanel/BestLabTimer").GetComponent<TextMeshProUGUI>();
        }

        if (!dashBoardText)
        {
            dashBoardText = GameObject.Find("Canvas/DashBoard").GetComponent<TextMeshProUGUI>();
        }

        if (rankText != null)
        {
            rankingTextCount = GameObject.Find("Canvas/RankingPanel").transform.childCount;
            
            if (rankingTextCount > 0)
            {
                for (int i = 0; i < rankingTextCount; i++)
                {
                    rankText.Add(GameObject.Find($"Canvas/RankingPanel/RankText_{i+1}").GetComponent<TextMeshProUGUI>());
                }
            }
        }
    }

    public void UpdateRankingUI(int[] playerIDs, float[] bestLapTimes, int rankDataCount)
    {
        for (int i = 0; i < rankingTextCount; i++)
        {
            if (i < rankDataCount)
            {
                rankText[i].text = $"{playerIDs[i]}P. {(int)bestLapTimes[i] / 60 : 00}:{bestLapTimes[i] % 60 : 00.000}";
            }
            else
            {
                rankText[i].text = $"{i + 1}. ---";
            }
        }
    }
}
