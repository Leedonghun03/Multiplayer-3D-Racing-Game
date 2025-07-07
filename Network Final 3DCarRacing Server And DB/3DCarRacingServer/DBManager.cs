using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;

namespace _3DCarRacingServer
{
    internal class DBManager
    {
        static string Database = "Net3DRacingGame";

        private static string strConn = $"Server = localhost; Database = {Database}; Uid = root; Pwd = Dlwmsk051216!";

        public static void UpdateBestLapTime(int clientID, float bestLapTime)
        {
            try
            {
                if(IsPlayerExistence(clientID))
                {
                    EditBestLapTime(clientID, bestLapTime);
                }
                else
                {
                    InsertBestLapTime(clientID, bestLapTime);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"DB 오류 : {e.Message}");
            }
        }

        public static void InsertBestLapTime(int clientID, float bestLapTime)
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(strConn);

                conn.Open();
                string sql = $"INSERT INTO Best_Lap_Time_Rank_Tbl VALUES (null, {clientID}, {bestLapTime});";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"DB 오류 : {e.Message}");
            }
        }

        public static void EditBestLapTime(int clientID, float bestLapTime)
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(strConn);

                conn.Open();
                string sql = $"UPDATE Best_Lap_Time_Rank_Tbl SET BestLapTime = {bestLapTime} WHERE Client_ID = {clientID}";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"DB 오류 : {e.Message}");
            }
        }

        public static bool IsPlayerExistence(int clientID)
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(strConn);

                conn.Open();
                string sql = $"SELECT * FROM Best_Lap_Time_Rank_Tbl WHERE Client_ID = {clientID}";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    if ((int)reader[1] == clientID)
                    {
                        return true;
                    }
                }

                reader.Close();
                conn.Close();
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"DB 오류: {e.Message}");
                return false;
            }
        }

        public static void GetRanking(out int[] playerIDs, out float[] bestLapDatas, out byte listCount)
        {
            List<int> IDList = new List<int>(8);
            List<float> bestLapList = new List<float>(8);

            try
            {
                MySqlConnection conn = new MySqlConnection(strConn);

                conn.Open();
                string sql = $"SELECT * FROM Best_Lap_Time_Rank_Tbl ORDER BY BestLapTime ASC LIMIT 8;";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    IDList.Add(reader.GetInt32(1));
                    bestLapList.Add(reader.GetFloat(2));
                }

                reader.Close();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"DB 오류 : {e.Message}");
            }

            playerIDs = IDList.ToArray();
            bestLapDatas = bestLapList.ToArray();
            listCount = (byte)playerIDs.Length;
        }

        public static void ResetData()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(strConn);
                conn.Open();

                string sql = "TRUNCATE TABLE Best_Lap_Time_Rank_Tbl;";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
                Console.WriteLine("랭킹 테이블 초기화 완료.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 초기화 실패: {ex.Message}");
            }
        }
    }
}
