using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using MySqlX.XDevAPI;
using System.Diagnostics;
using Org.BouncyCastle.Ocsp;

public enum MsgType : byte
{
    Hello = 0,
    Bye = 1,
    Pos = 2,
    BestLapTime = 3,
    RankingRequest = 4,
    RankingTable = 5
}

struct HelloPacket
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

namespace _3DCarRacingServer
{
    internal class RacingGameServer
    {
        const int serverPort = 9000;

        static Dictionary<IPEndPoint, byte> clients = new Dictionary<IPEndPoint, byte>();
        static byte nextId = 1;
        static byte maxPlayer = 8;

        static byte[] recvBytes = new byte[1024];
        static bool isRunning = false;

        // ===== 서버 송수신용 직렬화, 역직렬화 =====
        static byte[] Serialize_HelloPacket(HelloPacket packet)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((byte)packet.msgType);
            bw.Write(packet.clientID);
            return ms.ToArray();
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

            return new ByePacket
            {
                msgType = (MsgType)br.ReadByte(),
                clientID = br.ReadByte()
            };
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

        static BestLapTimePacket Deserialize_BestLapTimePacket(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            BinaryReader br = new BinaryReader(ms);

            BestLapTimePacket bestLapTimePacket = new BestLapTimePacket()
            {
                msgType = (MsgType)br.ReadByte(),
                clientID = br.ReadByte(),
                lapTime = br.ReadSingle(),
            };

            return bestLapTimePacket;
        }

        static byte[] Serialize_RankingTablePacket(RankingTablePacket rankingTablePacket)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((byte)rankingTablePacket.msgType);
            bw.Write(rankingTablePacket.RankCount);

            for (int i = 0; i < rankingTablePacket.RankCount; i++)
            {
                bw.Write(rankingTablePacket.PlayerIDs[i]);
                bw.Write(rankingTablePacket.BestLapTimes[i]);
            }

            return ms.ToArray();
        }
        // ========================================

        static void Main(string[] args)
        {
            Thread serverThread = new Thread(ServerLoop);
            serverThread.Start();

            Console.WriteLine("아무키를 눌러서 서버 종료");
            Console.ReadLine();

            DBManager.ResetData();
            serverThread.Join();
        }

        static void ServerLoop()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
            serverSocket.Bind(endPoint);
            Console.WriteLine($"[SERVER] UDP {serverPort} 대기 시작");

            EndPoint clientEP = new IPEndPoint(IPAddress.None, 0);
            isRunning = true;

            while (isRunning)
            {
                try
                {
                    int len = serverSocket.ReceiveFrom(recvBytes, ref clientEP);
                    var ep = (IPEndPoint)clientEP;
                    MsgType type = (MsgType)recvBytes[0];

                    switch (type)
                    {
                        // 클라이언트 접속 후 ID 배정
                        case MsgType.Hello:
                            {
                                if (!clients.TryGetValue(ep, out byte id))
                                {
                                    id = GetAvailableLowestId();
                                    clients[ep] = id;
                                    Console.WriteLine($"[HELLO] {ep} → ID {id}");
                                }

                                var resp = new HelloPacket { msgType = MsgType.Hello, clientID = clients[ep] };
                                serverSocket.SendTo(Serialize_HelloPacket(resp), ep);
                                break;
                            }

                        case MsgType.Bye:
                            {
                                clients.Remove(ep);
                                Console.WriteLine($"[Bye] {ep} 연결 종료");

                                ByePacket byePacket = Deserialize_ByePacket(recvBytes);
                                byte[] sendByeData = Serialize_ByePacket(byePacket);

                                foreach (var client in clients)
                                {
                                    serverSocket.SendTo(sendByeData, client.Key);
                                }
                                break;
                            }
                        

                        // 위치 브로드캐스트
                        case MsgType.Pos:
                            {
                                PosPacket posPacket = Deserialize_PosPacket(recvBytes);
                                byte[] sendPosData = Serialize_PosPacket(posPacket);

                                foreach (var client in clients)
                                {
                                    if (client.Key.Equals(ep))
                                    {
                                        continue;
                                    }
                                    serverSocket.SendTo(sendPosData, client.Key);
                                }
                                break;
                            }
                            

                        case MsgType.BestLapTime:
                            {
                                BestLapTimePacket bestLapTimePacket = Deserialize_BestLapTimePacket(recvBytes);
                                DBManager.UpdateBestLapTime(bestLapTimePacket.clientID, bestLapTimePacket.lapTime);


                                DBManager.GetRanking(out int[] playerIDs, out float[] bestLapDatas, out byte listCount);
                                RankingTablePacket rankingTablePacket = new RankingTablePacket
                                {
                                    msgType = MsgType.RankingTable,
                                    RankCount = listCount,
                                    PlayerIDs = playerIDs,
                                    BestLapTimes = bestLapDatas
                                };

                                byte[] sendRakingTableData = Serialize_RankingTablePacket(rankingTablePacket);

                                foreach (var client in clients)
                                {
                                    serverSocket.SendTo(sendRakingTableData, client.Key);
                                }
                                break;
                            }
                            
                        case MsgType.RankingRequest:
                            {
                                DBManager.GetRanking(out int[] playerIDs, out float[] bestLapDatas, out byte listCount);
                                RankingTablePacket rankingTablePacket = new RankingTablePacket
                                {
                                    msgType = MsgType.RankingTable,
                                    RankCount = listCount,
                                    PlayerIDs = playerIDs,
                                    BestLapTimes = bestLapDatas
                                };

                                byte[] sendRakingTableData = Serialize_RankingTablePacket(rankingTablePacket);

                                serverSocket.SendTo(sendRakingTableData, ep);
                                break;
                            }
                    }
                }
                catch (SocketException se)
                {
                    Console.WriteLine(se.ErrorCode + ": " + se.Message);
                    isRunning = false;
                }
            }
        }

        private static byte GetAvailableLowestId()
        {
            for(byte i = 1; i <= maxPlayer; i++)
            {
                if (!clients.ContainsValue(i))
                {
                    return i;
                }
            }

            throw new Exception("모든 ID가 사용 중입니다.");
        }
    }
}
