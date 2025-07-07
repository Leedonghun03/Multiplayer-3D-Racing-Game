using TMPro;
using UnityEngine;

public class LapManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TextMeshProUGUI bestLapTimeText;
    [SerializeField] private float bestLapTime = 0.0f;
    
    [SerializeField] private TimerManager stopwatch;
    [SerializeField] private int nextSector;
    [SerializeField] private int sectorCount;
    [SerializeField] private bool lapAction;
    
    void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        
        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        if (uiManager && !bestLapTimeText)
        {
            this.bestLapTimeText = uiManager.bestLabTimerText;
        }
        
        stopwatch = GameObject.Find("TimerManager").GetComponent<TimerManager>();
        sectorCount = GameObject.Find("CheckPoint").transform.childCount;
        lapAction = false;
    }
    
    public void ReportPass(int sectorIndex)
    {
        if (!lapAction && sectorIndex == 1)
        {
            StartLap();
            return;
        }

        if (sectorIndex != nextSector)
        {
            Debug.Log($"체크포인트 순서 오류 [sectorIndex : {sectorIndex}, nextSector : {nextSector}]");
            return;
        }

        if (nextSector == 1)
        {
            CompleteLap();
            return;
        }

        nextSector = (nextSector % sectorCount) + 1;
    }

    private void StartLap()
    {
        lapAction = true;
        nextSector = 2;
        stopwatch.SetTimer();
    }

    private void CompleteLap()
    {
        float lapTime = stopwatch.RestartTimerAndGetTimer();
        Debug.Log($"완주 타이머 : {(int)lapTime/60} : {lapTime % 60 : 00.000}");

        if (bestLapTime == 0.0f || bestLapTime > lapTime)
        {
            bestLapTime = lapTime;
            bestLapTimeText.text = $"{(int)lapTime / 60 : 00}:{lapTime % 60:  00.000}";
            networkManager.SendToServer_BestLapTimeData(bestLapTime);
        }

        StartLap();
    }
}
