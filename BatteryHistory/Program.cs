using System;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BatteryHistory
{
    internal static class Program
    {
        private static readonly PowerStatus PowerStatus = SystemInformation.PowerStatus;
        private static string _previousState = "";

        private static readonly SQLiteCommand InsertHistorySql =
            new SQLiteCommand("INSERT INTO history (battery_level, is_discharging) VALUES (?, ?);");
        private static readonly SQLiteCommand InsertSleepSql =
            new SQLiteCommand("INSERT INTO sleep (is_sleeping) VALUES (?);");

        public static void Main(string[] args)
        {
            Setup();
            
            var timer = new Timer();
            timer.Tick += SaveBatteryState;
            timer.Interval = 1000;
            timer.Start();

            SystemEvents.PowerModeChanged += SaveSleepState;
            
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
            InsertSleepSql.Connection = connection;

            new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS history (id INTEGER PRIMARY KEY, battery_level REAL NOT NULL, is_discharging INTEGER NOT NULL, utc_time TEXT DEFAULT CURRENT_TIMESTAMP);",
                connection
            ).ExecuteNonQuery();
            new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS sleep (id INTEGER PRIMARY KEY, utc_time DEFAULT CURRENT_TIMESTAMP, is_sleeping INTEGER NOT NULL);",
                connection
            ).ExecuteNonQuery();
        }

        private static void SaveBatteryState(object sender, EventArgs args)
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

        private static void SaveSleepState(object sender, PowerModeChangedEventArgs e)
        {
            var isSleepingStatus = IsSleeping(e) ? 1 : 0;
            var state = $"Saved state: {isSleepingStatus}";
            
            if (state.Equals(_previousState)) return;
            
            InsertSleepSql.Parameters.Clear();
            InsertSleepSql.Parameters.Add(new SQLiteParameter("is_sleeping", isSleepingStatus));

            InsertSleepSql.ExecuteNonQuery();
            
            Console.WriteLine(state);
            _previousState = state;
        }

        private static bool IsSleeping(PowerModeChangedEventArgs e) => e.Mode == PowerModes.Suspend;

        private static bool IsDischarging() => PowerStatus.PowerLineStatus == PowerLineStatus.Offline &&
                                               PowerStatus.PowerLineStatus != PowerLineStatus.Unknown;
    }
}