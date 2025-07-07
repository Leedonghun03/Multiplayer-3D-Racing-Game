using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public enum MsgType : byte
{
    Hello = 0,
    Bye = 1,
    Pos = 2,
    BestLapTime = 3,
    RankingRequest = 4,
    RankingTable = 5
}

public struct HelloPacket
{
    public MsgType msgType;
    public byte clientID;
}

struct ByePacket
{
    public MsgType msgType;
    public byte clientID;
}

struct PosPacket
{
    public MsgType msgType;
    public byte clientID;
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ;
    public float[] quatX;
    public float[] quatY;
    public float[] quatZ;
    public float[] quatW;
}

struct BestLapTimePacket
{
    public MsgType msgType;
    public byte clientID;
    public float lapTime;
}

struct RankingTablePacket
{
    public MsgType msgType;
    public byte RankCount;
    public int[] PlayerIDs;
    public float[] BestLapTimes;
}

public class NetworkManager : MonoBehaviour
{
    private UIManager uiManager;
    
    [Header("네트워크")]
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 9000;
    
    private UdpClient udp;
    private IPEndPoint serverEndPoint;
    [SerializeField] private byte myID;
    private bool connected;
    
    // 서버 송수신 Thread
    private Thread recvThread;
    private bool isRunning;
    // 서버에서 받은 데이터들 담기 위한 Queue
    private readonly ConcurrentQueue<byte[]> recvQueue = new ConcurrentQueue<byte[]>();

    // ===== 서버 송수신용 직렬화, 역직렬화 =====
    static byte[] Serialize_HelloPacket(HelloPacket packet)
    {
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        
        bw.Write((byte)packet.msgType);
        bw.Write(packet.clientID);
        
        return ms.ToArray();
    }
    
    static HelloPacket Deserialize_HelloPacket(byte[] data)
    {
        MemoryStream ms = new MemoryStream(data);
        BinaryReader br = new BinaryReader(ms);

        return new HelloPacket { msgType = (MsgType)br.ReadByte(), clientID = br.ReadByte() };
    }
    
    static byte[] Serialize_ByePacket(ByePacket packet)
    {
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        
        bw.Write((byte)packet.msgType);
        bw.Write(packet.clientID);
        
        return ms.ToArray();
    }

    static ByePacket Deserialize_ByePacket(byte[] data)
    {
        MemoryStream ms = new MemoryStream(data);
        BinaryReader br = new BinaryReader(ms);

        return new ByePacket { msgType = (MsgType)br.ReadByte(), clientID = br.ReadByte() };
    }
    
    static byte[] Serialize_PosPacket(PosPacket posPacket)
    {
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        
        bw.Write((byte)posPacket.msgType);
        bw.Write(posPacket.clientID);
        bw.Write(posPacket.posX); bw.Write(posPacket.posY); bw.Write(posPacket.posZ);
        bw.Write(posPacket.rotX); bw.Write(posPacket.rotY); bw.Write(posPacket.rotZ);

        for (int i = 0; i < 4; i++)
        {
            bw.Write(posPacket.quatX[i]); bw.Write(posPacket.quatY[i]); bw.Write(posPacket.quatZ[i]);
            bw.Write(posPacket.quatW[i]);
        }

        return ms.ToArray();
    }

    static PosPacket Deserialize_PosPacket(byte[] data)
    {
        MemoryStream ms = new MemoryStream(data);
        BinaryReader br = new BinaryReader(ms);

        PosPacket posPacket = new PosPacket
        {
            msgType = (MsgType)br.ReadByte(),
            clientID = br.ReadByte(),
            posX = br.ReadSingle(),
            posY = br.ReadSingle(),
            posZ = br.ReadSingle(),
            rotX = br.ReadSingle(),
            rotY = br.ReadSingle(),
            rotZ = br.ReadSingle(),
            quatX = new float[4],
            quatY = new float[4],
            quatZ = new float[4],
            quatW = new float[4]
        };
        
        for (int i = 0; i < 4; i++)
        {
            posPacket.quatX[i] = br.ReadSingle(); posPacket.quatY[i] = br.ReadSingle(); posPacket.quatZ[i] = br.ReadSingle();
            posPacket.quatW[i] = br.ReadSingle();
        }

        return posPacket;
    }

    static byte[] Serialize_BestLapTimePacket(BestLapTimePacket bestLapTimePacket)
    {
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        
        bw.Write((byte)bestLapTimePacket.msgType);
        bw.Write(bestLapTimePacket.clientID);
        bw.Write(bestLapTimePacket.lapTime);
        
        return ms.ToArray();
    }
    
    static RankingTablePacket Deserialize_RankingTablePacket(byte[] data)
    {
        MemoryStream ms = new MemoryStream(data);
        BinaryReader br = new BinaryReader(ms);

        RankingTablePacket rankingTablePacket = new RankingTablePacket
        {
            msgType = (MsgType)br.ReadByte(),
            RankCount = br.ReadByte(),
            PlayerIDs = new int[8],
            BestLapTimes = new float[8],
        };

        for (int i = 0; i < rankingTablePacket.RankCount; i++)
        {
            rankingTablePacket.PlayerIDs[i] = br.ReadInt32();
            rankingTablePacket.BestLapTimes[i] = br.ReadSingle();
        }

        return rankingTablePacket;
    }
    // =====================================    
    
    private void Start()
    {
        if (!uiManager)
        {
            uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        }
        
        try
        {
            udp = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
        
            // === 클라이언트 ID 부여 받는 구간 ===
            // 2초 이내 응답이 없으면 예외
            udp.Client.ReceiveTimeout = 2000;
            // Hello Server에게 송신
            byte[] sendHelloPacket = Serialize_HelloPacket(new HelloPacket{ msgType = MsgType.Hello, clientID = 0});
            udp.Send(sendHelloPacket, sendHelloPacket.Length, serverEndPoint);
        
            // 서버한테 Hello 응답 받기
            byte[] recvData = udp.Receive(ref serverEndPoint);
            HelloPacket recvHelloPacket = Deserialize_HelloPacket(recvData);
            if (recvHelloPacket.msgType == MsgType.Hello)
            {
                myID = recvHelloPacket.clientID;
                Debug.Log($"[Client] 할당받은 내 ID = {myID}");
                
                // 아이디를 받았으면 바로 GameManager한테 차 생성해달라고 요청하는 부분
                GameManager.Instance.SpawnPlayer(myID, true);
                connected = true;
            }
            else
            {
                Debug.LogError("[Client] 예상치 못한 Hello 응답이 들어옴");
                return;
            }
            
            if(recvHelloPacket.msgType == MsgType.Hello)
                myID = recvHelloPacket.clientID;
            // ===============================

            if (connected)
            {
                byte[] rankingRep = new byte[] { (byte)MsgType.RankingRequest };
                udp.Send(rankingRep, rankingRep.Length, serverEndPoint);
            }
            
            isRunning = true;
            recvThread = new Thread(ReceiveLoop);
            recvThread.Start();
        }
        catch (SocketException se)
        {
            Console.WriteLine(se.ErrorCode + ": " +  se.Message);
        }
    }

    // recvThread : 서버와 송수신 하는 부분
    void ReceiveLoop()
    {
        while (isRunning)
        {
            try
            {
                byte[] data = udp.Receive(ref serverEndPoint);

                if (data.Length > 0)
                {
                    recvQueue.Enqueue(data);
                }
            }
            catch (SocketException se)
            {
                if (!isRunning)
                {
                    break;
                }
                Console.WriteLine(se.ErrorCode + ": " +  se.Message);
            }
        }
    }
    
    void Update()
    {
        while (recvQueue.TryDequeue(out byte[] recvData))
        {
            if (recvData.Length < 1)
            {
                continue;
            }

            MsgType type = (MsgType)recvData[0];

            switch (type)
            {
                case MsgType.Pos:
                    PosPacket recvPosPacket = Deserialize_PosPacket(recvData);
                
                    if (recvPosPacket.clientID == myID)
                    {
                        break;
                    }
                    
                    GameManager gameManager = GameManager.Instance;
                    if (!gameManager.HasPlayer(recvPosPacket.clientID))
                    {
                        gameManager.SpawnPlayer(recvPosPacket.clientID, false);
                    }
                    
                    Vector3 pos = new Vector3(recvPosPacket.posX, recvPosPacket.posY, recvPosPacket.posZ);
                    Vector3 rot = new Vector3(recvPosPacket.rotX, recvPosPacket.rotY, recvPosPacket.rotZ);

                    gameManager.UpdateRemotePlayerPosition(recvPosPacket.clientID, pos, rot, recvPosPacket.quatX, recvPosPacket.quatY, recvPosPacket.quatZ, recvPosPacket.quatW);
                    break;
                
                case MsgType.Bye:
                    ByePacket recvByePacket = Deserialize_ByePacket(recvData);
                    
                    GameManager.Instance.RemovePlayer(recvByePacket.clientID);
                    
                    GameObject deleteRemoteCar = GameObject.Find($"RemoteCar_{recvByePacket.clientID}");
                    Destroy(deleteRemoteCar);
                    break;
                
                case MsgType.RankingTable:
                    RankingTablePacket recvRankingTablePacket = Deserialize_RankingTablePacket(recvData);
                    uiManager.UpdateRankingUI(recvRankingTablePacket.PlayerIDs, recvRankingTablePacket.BestLapTimes,  recvRankingTablePacket.RankCount);
                    
                    break;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!connected)
        {
            return;
        }

        GameObject myCar = GameManager.Instance.GetLocalPlayerCar(myID);
        if (!myCar)
        {
            return;
        }

        CarController carCtrl = myCar.GetComponent<CarController>();
        Transform[] wheels = carCtrl.wheelTransforms;

        PosPacket posPacket = new PosPacket
        {
            msgType = MsgType.Pos,
            clientID = myID,
            posX = myCar.transform.position.x,
            posY = myCar.transform.position.y,
            posZ = myCar.transform.position.z,
            rotX = myCar.transform.eulerAngles.x,
            rotY = myCar.transform.eulerAngles.y,
            rotZ = myCar.transform.eulerAngles.z,
            quatX = new float[4],
            quatY = new float[4],
            quatZ = new float[4],
            quatW = new float[4]
        };

        for (int i = 0; i < wheels.Length; i++)
        {
            Quaternion wheelQuat = wheels[i].localRotation;
            posPacket.quatX[i] = wheelQuat.x;
            posPacket.quatY[i] = wheelQuat.y;
            posPacket.quatZ[i] = wheelQuat.z;
            posPacket.quatW[i] = wheelQuat.w;
        }
            
        byte[] sendPosData = Serialize_PosPacket(posPacket);
        udp.Send(sendPosData, sendPosData.Length, serverEndPoint);
    }

    public void SendToServer_BestLapTimeData(float bestLapTime)
    {
        if (!connected)
        {
            return;
        }

        BestLapTimePacket bestLapTimePacket = new BestLapTimePacket
        {
            msgType = MsgType.BestLapTime,
            clientID = myID,
            lapTime =  bestLapTime,
        };
        
        byte[] sendBestLapTimeData = Serialize_BestLapTimePacket(bestLapTimePacket);
        udp.Send(sendBestLapTimeData, sendBestLapTimeData.Length, serverEndPoint);
    }
    
    void OnApplicationQuit()
    {
        isRunning = false;

        ByePacket byePacket = new ByePacket()
        {
            msgType = MsgType.Bye,
            clientID = myID
        };
        byte[] sendByeData = Serialize_ByePacket(byePacket);
        udp.Send(sendByeData, sendByeData.Length, serverEndPoint);
        
        if (udp != null)
        {
            udp.Close();
        }

        if (recvThread != null && recvThread.IsAlive)
        {
            recvThread.Join();
        }
    }
}
