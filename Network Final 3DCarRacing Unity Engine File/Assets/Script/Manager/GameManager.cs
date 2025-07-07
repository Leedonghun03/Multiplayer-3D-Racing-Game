using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("자동차 프리팹")]
    [SerializeField] private GameObject carPrefab;

    [Header("플레이어 스폰 위치")]
    [SerializeField] private GameObject[] playerSpawnPos;
    
    private readonly Dictionary<byte, GameObject> players = new Dictionary<byte, GameObject>();

    private void Awake()
    {
        // 만약 로그인 창이나 씬 전환할 때 중복 생성 방지를 위해서 만들었음
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 서버로부터 받은 ClientID 기반으로 로컬 또는 원격 차량 Spawn
    // 이미 생성된 ID라면 아무 작업할 필요가 없음
    // isLocal = true(로컬) -> 사용자 입력 받아 물리적으로 움직임
    // isLocal = false(원격) -> 입력 받지 않고 서버에서 받은 위치값만 저장해야함
    public void SpawnPlayer(byte clientID, bool isLocal)
    {
        // 이미 생성된 클라이언트는 return
        if(players.ContainsKey(clientID))
            return;

        // 서버에서는 1부터 등록함, plyaerSpawnPos의 배열을 넘지 않기 위한 Clamp
        int index = Mathf.Clamp(clientID - 1, 0, playerSpawnPos.Length - 1);
        Transform spawnPos = playerSpawnPos[index].transform;

        // 프리팹 자동차 배치
        GameObject carObject = Instantiate(carPrefab, spawnPos.position, spawnPos.rotation);
        // isLocal의 정보로 오브젝트 이름, 태그 변경
        if (isLocal)
        {
            carObject.name = $"LocalCar_{clientID}";
        }
        else
        {
            carObject.name = $"RemoteCar_{clientID}";
            carObject.tag = "RemotePlayer";
        }

        CarNetInfo carNetInfo = carObject.GetComponent<CarNetInfo>();
        carNetInfo.CarSetting(isLocal);
        
        players.Add(clientID, carObject);
        Debug.Log($"[GameManager] SpawnPlayer : ID = {clientID}, isLocal = {isLocal}");
    }

    public void UpdateRemotePlayerPosition(byte clientID, Vector3 position, Vector3 bodyEuler, float[] quatX, float[] quatY, float[] quatZ, float[] quatW)
    {
        if (!players.TryGetValue(clientID, out GameObject playerCar))
        {
            return;
        }
        
        // 차량 위치, 회전 동기화
        playerCar.transform.SetPositionAndRotation(position, Quaternion.Euler(bodyEuler));    

        // 차량 바퀴 회전, 조향 동기화
        Transform[] wheels = playerCar.GetComponent<CarController>().wheelTransforms;
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].localRotation = new Quaternion(quatX[i], quatY[i], quatZ[i], quatW[i]);
        }
    }

    public GameObject GetLocalPlayerCar(byte clinetID)
    {
        if (players.TryGetValue(clinetID, out GameObject playerCar))
        {
            return playerCar;
        }
        else
        {
            return null;
        }
    }

    public void RemovePlayer(byte clientID)
    {
        players.Remove(clientID);
    }

    public bool HasPlayer(byte clientID)
    {
        return players.ContainsKey(clientID);
    }
}
