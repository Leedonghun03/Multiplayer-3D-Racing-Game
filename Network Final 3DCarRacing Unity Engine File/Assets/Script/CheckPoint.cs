using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public int sectorIndex;
    public LapManager lapManager;

    private void Start()
    {
        if(!lapManager)
            lapManager = GameObject.Find("LapManager").GetComponent<LapManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        lapManager.ReportPass(sectorIndex);
    }
}
