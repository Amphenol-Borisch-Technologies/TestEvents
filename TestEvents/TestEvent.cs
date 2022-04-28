using System;
using System.IO;
using Npgsql;

namespace TestEvents {
    public static class TestEvent {
        public static readonly string TEST_EVENTS_HOME = "C:\\Program Files\\TestEvents\\";
        private static readonly string SAVED_EVENTS_FILE = TEST_EVENTS_HOME + "SavedEvents.txt";
        private static readonly string SAVED_EXCEPTIONS_FILE = TEST_EVENTS_HOME + "SavedExceptions.txt";

        public static void Main(string[] args) {
            SaveEvent(args);
            InsertSQL(SAVED_EVENTS_FILE);
            if (File.Exists(SAVED_EXCEPTIONS_FILE)) InsertSQL(SAVED_EXCEPTIONS_FILE);
        }

        internal static void SaveEvent(string[] args) {
            string assembly_number = "ERROR-ASSEMBLY-NUMBER";   // Default value if args.length != 1, 2 or 3.
            string serial_number = "ERROR-SERIAL-NUMBER";       // Default value if args.length != 1 or 2.
            string event_code = "E";                            // Default value if args.length != 3; relates to "ERROR-EVENT" in RDBMS table test_events.event_codes.
            switch (args.Length) {
                case 1:
                    assembly_number = args[0]; break;
                case 2:
                    assembly_number = args[0]; serial_number = args[1]; break;
                case 3:
                    assembly_number = args[0]; serial_number = args[1]; event_code = args[2]; break;
            }
            try {
                string sql = "INSERT INTO test_events.events (assembly_number, serial_number, event_code, host_name, date, time) " +
                             "VALUES ('" + assembly_number + "','" + serial_number + "','" + event_code + "','" + System.Environment.MachineName + "','" + DateTime.Now.ToString("MM/dd/yy") + "','" + DateTime.Now.ToString("hh:mm:ss")+ "');";
                File.AppendAllText(SAVED_EVENTS_FILE, sql);
            } catch (Exception e) {
                SaveException(e.ToString());
            }
        }

        public static void SaveException(string exception) {
            string e = exception.Substring(0, Math.Min(255, exception.Length));  // Truncate exception to 255 characters, since its SQL field is 255 characters max.
            e = e.Replace("\"", "^");   // Replace " with ^ to avoid SQL insertion errors if truncation results in odd number of ".
            e = e.Replace("'", "|");    // Replace ' with | to avoid SQL insertion errors if truncation results in odd number of '.
            e = e.Replace("\n", "");    // Eliminate newlines & carriage returns, so forthcoming sql string is contiguous on one line; multiple lines cause Exceptions.
            e = e.Replace("\r", "");
            string sql = "INSERT INTO test_events.exceptions (date, time, host_name, exception, user_dir) " +
                         "VALUES ('" + DateTime.Now.ToString("MM/dd/yy") + "','" + DateTime.Now.ToString("hh:mm:ss") + "','" + System.Environment.MachineName + "','" + e + "','" + System.Environment.CurrentDirectory + "');";
            File.AppendAllText(SAVED_EXCEPTIONS_FILE, sql);
        }

        private static void InsertSQL(string fileName) {
            NpgsqlConnection npgsqlConnection = new NpgsqlConnection();
            npgsqlConnection.ConnectionString = "Host=vm-int-pgsql-01.borisch.local;Username=testevent;Password=testevent;Database=test;Port=5432;SSLMode=Prefer";
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
            try {
                npgsqlConnection.Open();
                npgsqlCommand.Connection = npgsqlConnection;
                npgsqlCommand.CommandTimeout = 10;
                string[] savedEvents = File.ReadAllLines(fileName);
                foreach (string savedEvent in savedEvents) {
                    npgsqlCommand.CommandText = savedEvent;
                    npgsqlCommand.ExecuteNonQuery();
                }
                File.Delete(fileName);
            } catch (Exception e) {
                SaveException(e.ToString());
            } finally {
                npgsqlCommand.Dispose();
                npgsqlConnection.Close();
                npgsqlConnection.Dispose();
            }
        }
    }
}
