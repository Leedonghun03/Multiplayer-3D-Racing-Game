using UnityEngine;
using System.Collections;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [Header("사용하는 UI 목록")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TextMeshProUGUI labTimerText;
    
    [Header("타이머 관련")]
    [SerializeField] private float time;
    private Coroutine timerCoroutine;
    
    private void Start()
    {
        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        if (uiManager && !labTimerText)
        {
            this.labTimerText = uiManager.labTimerText;
        }
    }

    public void SetTimer()
    {
        if(timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(Stopwatch());
    }

    public float RestartTimerAndGetTimer()
    {
        float completeLapTime = time;
        SetTimer();

        return completeLapTime;
    }
    
    private IEnumerator Stopwatch()
    {
        time = 0.0f;
        
        while (true)
        {
            time += Time.deltaTime;

            labTimerText.text = $"{(int)time / 60 : 00}:{time % 60 : 00.000}";
            yield return null;
        }
    }
}
