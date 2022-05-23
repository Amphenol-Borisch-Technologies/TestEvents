using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TestEvents;
using System.Diagnostics;

namespace SpeaWrapper {
    public static class SpeaWrapper {
        private static readonly bool debug = false;
        public static void Main(string[] args) {
            try {
                string[] testResults = File.ReadAllLines(TestEvent.TEST_EVENTS_HOME + "TestResult.txt");
                if (testResults.Length == 0) throw new Exception("Spea Test Result File was empty.");
                string partNumber = getResult(testResults, "PartNumber");
                int numberOfBoards = int.Parse(getResult(testResults, "NumberOfBoards"));
                Debug.WriteLineIf(debug, "Main!");
                for (int i = 1; i <= numberOfBoards; i++) {
                    string testEvent = transformBoardResultToTestEvent(getResult(testResults, "BoardResult" + i));
                    string cdColl = getResult(testResults, "CDCOLL" + i);
                    Debug.WriteLineIf(debug, "testEvent(" + i.ToString() + "): " + testEvent);
                    Debug.WriteLineIf(debug, "cdColl(" + i.ToString() + "): " + cdColl);
                    if (cdColl != "") FileRename(cdColl, testEvent);
                    string serialNumber = getResult(testResults, "SN" + i);
                    if (serialNumber == "") serialNumber = "NA";
                    if (testEvent != "NA") TestEvent.Main(new string[] { partNumber, serialNumber, testEvent }); // Don't record TestEvents for BoardResults that are "NA" or Not Applicable.
                }
            } catch (Exception e) {
                TestEvent.SaveException(e.ToString());
            }
        }

        private static string getResult(string[] testResults, string resultName) {
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

        private static string transformBoardResultToTestEvent(string boardResult) {
            switch (boardResult.ToUpper()) {
                case "FAIL":        return "F";     // "F" = TestEvent "Failed".
                case "NONE":        return "A";     // "A" = TestEvent "Aborted".
                case "PASS":        return "P";     // "P" = TestEvent "Passed".
                default:            return "NA";
                    // Default testEvent = "NA" for "Not Applicable".  "NA" is applied to the below BoardResults indiscriminately:
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
                re = new Regex(@"_\d{1,3}_.\.txt$");
                matches = from file in Directory.GetFiles(TDR, cdCollNew + "_???.txt", SearchOption.TopDirectoryOnly)
                          where re.IsMatch(file)
                          select file;
                Debug.WriteLineIf(debug, "matches.Count()_b: " + matches.Count());
                int i = 0, maxInt = 0;
                string s = "";
                foreach (string file in matches) {
                    Debug.WriteLineIf(debug, "file_b: " + file);
                    s = file.Replace(".txt", "");
                    s = s.Replace(TDR + cdCollNew + "_", "");
                    s = s.Substring(0, s.Length - 2);
                    Debug.WriteLineIf(debug, "s: " + s);
                    int.TryParse(s, out i);
                    if (i > maxInt) maxInt = i;
                }
                maxInt++;

                Debug.WriteLineIf(debug, "maxInt: " + maxInt);
                cdCollNew = TDR + cdCollNew + "_" + maxInt.ToString() + "_" + testEvent + ".txt";
                // cdCollNew = P:\Test\TDR\S3AS0300B-D10\S3AS0300B-D10_BRH2212-100_1_P.txt, if BRH2212-100 passed the first run.
                Debug.WriteLineIf(debug, "cdCollNew_c: " + cdCollNew);
                File.Move(newestFile, cdCollNew);
                // Above File.Move() renames hypothetical file:
                // "P:\Test\TDR\S3AS0300B-D10\S3AS0300B-D10_BRH2212-100_103000.txt" to
                // "P:\Test\TDR\S3AS0300B-D10\S3AS0300B-D10_BRH2212-100_1_P.txt".
            }
        }
    }
}
