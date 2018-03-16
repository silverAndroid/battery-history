using System;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace BatteryHistory
{
    internal static class Program
    {
        private static readonly PowerStatus PowerStatus = SystemInformation.PowerStatus;
        private static string _previousState = "";

        private static readonly SQLiteCommand InsertHistorySql =
            new SQLiteCommand("INSERT INTO history (battery_level, is_discharging) VALUES (?, ?);");

        public static void Main(string[] args)
        {
            Setup();
            
            var timer = new Timer();
            timer.Tick += SaveState;
            timer.Interval = 1000;
            timer.Start();
            Application.Run();
        }

        private static void Setup()
        {
            const string fileName = "db.sqlite";
            if (!File.Exists(fileName))
            {
                SQLiteConnection.CreateFile(fileName);
            }

            var connection = new SQLiteConnection("Data Source=db.sqlite;Version=3;");
            connection.Open();
            InsertHistorySql.Connection = connection;

            new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS history (id INTEGER PRIMARY KEY, battery_level REAL NOT NULL, is_discharging INTEGER NOT NULL, utc_time TEXT DEFAULT CURRENT_TIMESTAMP);",
                connection
            ).ExecuteNonQuery();
        }

        private static void SaveState(object sender, EventArgs args)
        {
            var batteryLife = PowerStatus.BatteryLifePercent;
            var isDischargingStatus = IsDischarging() ? 1 : 0;
            var state = $"Saved state: {batteryLife} | {isDischargingStatus}";
            
            if (state.Equals(_previousState)) return;

            InsertHistorySql.Parameters.Clear();
            InsertHistorySql.Parameters.Add(new SQLiteParameter("battery_level", batteryLife));
            InsertHistorySql.Parameters.Add(new SQLiteParameter("is_discharging", isDischargingStatus));

            InsertHistorySql.ExecuteNonQuery();
            
            Console.WriteLine(state);
            _previousState = state;
        }

        private static bool IsDischarging() => PowerStatus.PowerLineStatus == PowerLineStatus.Offline &&
                                               PowerStatus.PowerLineStatus != PowerLineStatus.Unknown;
    }
}