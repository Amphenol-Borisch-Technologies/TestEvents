using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Npgsql;
using System.Diagnostics;

namespace SpeaTestDataTestEvents {
    public static class SpeaTestDataTestEvents {
        private static readonly bool debug = false;
        public static void Main() {
            try {
                string[] testResults = File.ReadAllLines(TestEvent.TEST_EVENTS_HOME + "TestResult.txt");
                if (testResults.Length == 0) throw new Exception("Spea Test Result File was empty.");
                string partNumber = GetResult(testResults, "PartNumber");
                int numberOfBoards = int.Parse(GetResult(testResults, "NumberOfBoards"));
                Debug.WriteLineIf(debug, "Main!");
                for (int i = 1; i <= numberOfBoards; i++) {
                    string testEvent = TransformBoardResultToTestEvent(GetResult(testResults, "BoardResult" + i));
                    string cdColl = GetResult(testResults, "CDCOLL" + i);
                    Debug.WriteLineIf(debug, "testEvent(" + i.ToString() + "): " + testEvent);
                    Debug.WriteLineIf(debug, "cdColl(" + i.ToString() + "): " + cdColl);
                    if (cdColl != "") FileRename(cdColl, testEvent);
                    string serialNumber = GetResult(testResults, "SN" + i);
                    if ((testEvent != "") && (serialNumber != "")) TestEvent.PseudoMain(new string[] { partNumber, serialNumber, testEvent });
                }
            } catch (Exception e) {
                TestEvent.SaveException(e.ToString());
            }
        }

        private static string GetResult(string[] testResults, string resultName) {
            string[] result;
            for (int i = 0; i < testResults.Length; i++) {
                result = testResults[i].Split('=');
                if (resultName.ToLower().Contains("cdcoll") && result[0].ToLower() == resultName.ToLower()) {
                    if (! result[1].ToLower().Contains("interrupted")) return result[1].Trim();
                } else
                    if (result[0].ToLower() == resultName.ToLower()) return result[1].Trim();
            }
            return "";
        }

        private static string TransformBoardResultToTestEvent(string boardResult) {
            switch (boardResult.ToUpper()) {
                case "FAIL":        return "F";     // "F" = TestEvent "Failed".
                case "NONE":        return "A";     // "A" = TestEvent "Aborted".
                case "PASS":        return "P";     // "P" = TestEvent "Passed".
                default:            return "";
                    // Default testEvent = "", applied to the below BoardResults indiscriminately:
                    // - CCAs inside a panel that aren't selected for testing, which returns "None".
                    // - Fiducial alignment failures, which returns "Align Fail".
                    // - All other BoardResults that aren't explicitly included in the above Switch statement.
                    // - I'd detail them, except I can't find an exhaustive list, and the above two are all I currently know.
                    // - Unknown Spea BoardResults might actually be better categorized as "A" (Aborted), "E" (ERROR-EVENT-CODE) or "F" (Failed) TestEvents,
                    //   but without knowing them, "NA" must suffice.
            }
        }

        private static void FileRename(string cdColl, string testEvent) {
            // Assuming cdColl = P:\Test\TDR\S3AS0300B-D10\S3AS0300B-D10_BRH2212-100_103000.txt...
            string TDR = cdColl.Substring(0, cdColl.LastIndexOf(@"\") + 1);
            // TDR = P:\Test\TDR\S3AS0300B-D10\
            Debug.WriteLineIf(debug, "TDR: " + TDR);
            string cdCollNew = cdColl.Substring(0, cdColl.Length - "_HMS.txt".Length);
            // cdCollNew = P:\Test\TDR\S3AS0300B-D10\S3AS0300B-D10_BRH2212-100
            Debug.WriteLineIf(debug, "cdCollNew_a: " + cdCollNew);
            cdCollNew = cdCollNew.Substring(TDR.Length);
            // cdCollNew = S3AS0300B-D10_BRH2212-100
            Debug.WriteLineIf(debug, "cdCollNew_b: " + cdCollNew);

            Regex re = new Regex(@"_\d{6}\.txt$");
            var matches = from file in Directory.GetFiles(TDR, cdCollNew + "_??????.txt", SearchOption.TopDirectoryOnly)
                          where re.IsMatch(file)
                          select file;
            // matches contains just files of the correct length, with filenames ending with "_######.txt", where # is any digit in range {0,9}.  Example; "_123456.txt", created at 12:34:56 PM.
            Debug.WriteLineIf(debug, "matches.Count()_a: " + matches.Count());
            if (matches.Count() > 1) TestEvent.SaveException("Too many matches!  Want 1, have " + matches.Count());
            if (matches.Count() >= 1) {
                string newestFile = "";
                DateTime dt = new DateTime(); // Default constructor initializes dt to 1/1/0001 12:00:00 AM.
                foreach (string file in matches) {
                    Debug.WriteLineIf(debug, "file_a: " + file);
                    if (File.GetCreationTime(file) > dt) {
                        dt = File.GetCreationTime(file);
                        newestFile = file;
                    }
                }
                Debug.WriteLineIf(debug, "newestFile: " + newestFile);
                matches = from file in Directory.GetFiles(TDR, cdCollNew + "_*.txt", SearchOption.TopDirectoryOnly) select file;
                Debug.WriteLineIf(debug, "matches.Count()_b: " + matches.Count());
                cdCollNew = TDR + cdCollNew + "_" + matches.Count().ToString() + "_" + testEvent + ".txt";
                // cdCollNew = P:\Test\TDR\S3AS0300B-D10\S3AS0300B-D10_BRH2212-100_1_P.txt, if BRH2212-100 passed the first run.
                Debug.WriteLineIf(debug, "cdCollNew_c: " + cdCollNew);
                File.Move(newestFile, cdCollNew);
                // Above File.Move() renames hypothetical file:
                // "P:\Test\TDR\S3AS0300B-D10\S3AS0300B-D10_BRH2212-100_103000.txt" to
                // "P:\Test\TDR\S3AS0300B-D10\S3AS0300B-D10_BRH2212-100_1_P.txt".
                string[] testData = File.ReadAllLines(cdCollNew);
                // Spea's CDCOLL version 6.0 or higher supports Panelized test results, but it's counterproductive to ABT's needs, which are individual test data files.
                // Below code resolves Spea's Panelization implementation issues.
                // Assumes CDCOLL version 6.0 or higher, and configuring all Panelized test data to be separated into individual data files.
                testData = testData.Where(line => !line.ToUpper().StartsWith("PANEL;")).ToArray();
                // Remove all lines that begin with "PANEL;".
                // There should always/only be 1 "PANEL;" line; >= CDCOLL 6.0 always creates a "PANEL;" line.
                // "PANEL;" is:
                //  - The Serial Number of the Panel if there's a Panel and if the Panel's Serial Number is operator entered.
                //  - The 1st CCA's Serial Number if there's a Panel and it's Serial Number isn't entered, which is always the case for Panels at ABT.
                //  - Blank if there's no Panel.
                // Regardless, we don't want a Panel's Serial Number when we're deliberately separating test data into individual files; it's confusing, since they will typically be different 2 Serial Numbers.
                testData = testData.Where(line => !line.ToUpper().StartsWith("END;")).ToArray();
                // There should always/only be 1 "END;" line.
                // The "END;" reult isn't the individual board's test result (saved as "BOARDRESULT;"), but is instead the Panel's result:
                // A Panel only Passes if all it's individual board's all Pass.  If any individual board Fails, the Panel Fails, and the "END;" result is Fail.
                // Can't have "BOARDRESULT;PASS" followed by "END;FAIL" in test data.
                File.WriteAllLines(cdCollNew, testData);
            }
        }
    }
    public static class TestEvent {
        public static readonly string TEST_EVENTS_HOME = "C:\\Program Files\\SpeaTestDataTestEvents\\";
        private static readonly string SAVED_EVENTS_FILE = TEST_EVENTS_HOME + "SavedEvents.txt";
        private static readonly string SAVED_EXCEPTIONS_FILE = TEST_EVENTS_HOME + "SavedExceptions.txt";

        public static void PseudoMain(string[] args) {
            SaveEvent(args);
            InsertSQL(SAVED_EVENTS_FILE);
            if (File.Exists(SAVED_EXCEPTIONS_FILE)) InsertSQL(SAVED_EXCEPTIONS_FILE);
        }

        public static void SaveEvent(string[] args) {
            string assembly_number = "ERROR-ASSEMBLY-NUMBER";   // Default value if args.length != 1, 2 or 3.
            string serial_number = "ERROR-SERIAL-NUMBER";       // Default value if args.length != 1 or 2.
            string event_code = "E";                            // Default value if args.length != 3; relates to "ERROR-EVENT" in RDBMS table test_events.event_codes.
            switch (args.Length) {
                case 1: assembly_number = args[0]; break;
                case 2: assembly_number = args[0]; serial_number = args[1]; break;
                case 3: assembly_number = args[0]; serial_number = args[1]; event_code = args[2]; break;
            }
            try {
                string sql = "INSERT INTO test_events.events (assembly_number, serial_number, event_code, host_name, date, time) " +
                             "VALUES ('" + assembly_number + "','" + serial_number + "','" + event_code + "','" + System.Environment.MachineName + "','" + DateTime.Now.ToString("MM/dd/yy") + "','" + DateTime.Now.ToString("hh:mm:ss") + "');" + Environment.NewLine;
                File.AppendAllText(SAVED_EVENTS_FILE, sql);
            } catch (Exception e) {
                SaveException(e.ToString());
            }
        }

        public static void SaveException(string exception) {
            string e = exception.Substring(0, Math.Min(5120, exception.Length));  // Truncate exception to 5120 characters, since its SQL field is 5120 characters max.
            e = e.Replace("\"", "^");   // Replace " with ^ to avoid SQL insertion errors if truncation results in odd number of ".
            e = e.Replace("'", "|");    // Replace ' with | to avoid SQL insertion errors if truncation results in odd number of '.
            e = e.Replace("\n", "");    // Eliminate newlines & carriage returns, so forthcoming sql string is contiguous on one line; multiple lines cause Exceptions.
            e = e.Replace("\r", "");
            string sql = "INSERT INTO test_events.exceptions (date, time, host_name, exception, user_dir) " +
                         "VALUES ('" + DateTime.Now.ToString("MM/dd/yy") + "','" + DateTime.Now.ToString("hh:mm:ss") + "','" + System.Environment.MachineName + "','" + e + "','" + System.Environment.CurrentDirectory + "');" + Environment.NewLine;
            File.AppendAllText(SAVED_EXCEPTIONS_FILE, sql);
        }

        private static void InsertSQL(string fileName) {
            NpgsqlConnection npgsqlConnection = new NpgsqlConnection("Host=vm-int-pgsql-01.borisch.local;Username=testevent;Password=testevent;Database=test;Port=5432;SSLMode=Prefer");
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
