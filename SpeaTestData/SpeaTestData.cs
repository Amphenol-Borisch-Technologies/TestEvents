using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
// using TestEvents;
// Couldn't successully invoke TestEvent.Main() without raising errors similar to below:
//      System.IO.FileLoadException:
//  	'System.Runtime.CompilerServices.Unsafe, Version=4.0.4.1, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' or one of its dependencies
//  	The located assembly's manifest definition does not match the assembly reference.
// There were variations of the above, depending on which Package versions I mixed & matched.
// May be worth revisiting this error with updated Packages eventually.  Until then, TestEvents related code is commented.

namespace SpeaTestData {
    public static class SpeaTestData {
        private static readonly bool debug = false;
        public static void Main() {
            string[] testResults = File.ReadAllLines(@"C:\Program Files\SpeaTestData\TestResult.txt");
            if (testResults.Length == 0) throw new Exception("Spea Test Result File was empty.");
            // string partNumber = GetResult(testResults, "PartNumber");
            int numberOfBoards = int.Parse(GetResult(testResults, "NumberOfBoards"));
            Debug.WriteLineIf(debug, "Main!");
            for (int i = 1; i <= numberOfBoards; i++) {
                string testEvent = TransformBoardResultToTestEvent(GetResult(testResults, "BoardResult" + i));
                string cdColl = GetResult(testResults, "CDCOLL" + i);
                Debug.WriteLineIf(debug, "testEvent(" + i.ToString() + "): " + testEvent);
                Debug.WriteLineIf(debug, "cdColl(" + i.ToString() + "): " + cdColl);
                if (cdColl != "") FileRename(cdColl, testEvent);
                // string serialNumber = GetResult(testResults, "SN" + i);
                // if ((testEvent != "A") && (serialNumber != "")) TestEvent.Main(new string[] { partNumber, serialNumber, testEvent });
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
                case "PASS":        return "P";     // "P" = TestEvent "Passed".
                case "FAIL":        return "F";     // "F" = TestEvent "Failed".
                case "NONE":        return "A";     // "A" = TestEvent "Aborted".
                default:            return "A";
                // When re-running selected Tasks from Test Select, boardResult is "None", which transforms to "A".
            }
        }

        private static void FileRename(string cdColl, string testEvent) {
            // Assuming cdColl = P:\Test\TDR\BoardNumber\BoardNumber_SerialNumber_103000.txt...
            string TDR = cdColl.Substring(0, cdColl.LastIndexOf(@"\") + 1);
            // TDR = P:\Test\TDR\BoardNumber\
            Debug.WriteLineIf(debug, "TDR: " + TDR);
            string cdCollNew = cdColl.Substring(0, cdColl.Length - "_HMS.txt".Length);
            // cdCollNew = P:\Test\TDR\BoardNumber\BoardNumber_SerialNumber
            Debug.WriteLineIf(debug, "cdCollNew_a: " + cdCollNew);
            cdCollNew = cdCollNew.Substring(TDR.Length);
            // cdCollNew = BoardNumber_SerialNumber
            Debug.WriteLineIf(debug, "cdCollNew_b: " + cdCollNew);

            Regex re = new Regex(@"_[0-9]{6}\.txt$");
            var matches = from file in Directory.GetFiles(TDR, cdCollNew + "_??????.txt", SearchOption.TopDirectoryOnly)
                          where re.IsMatch(file)
                          select file;
            // matches contains just files of the correct length, with filenames ending with "_######.txt", where # is any digit in range {0,9}.  Example; "_123456.txt", created at 12:34:56 PM.
            Debug.WriteLineIf(debug, "matches.Count()_a: " + matches.Count());
            if (matches.Count() >= 1) {
                string newestFile = "";
                DateTime dt = new DateTime(); // Default constructor initializes dt to 1/1/0001 12:00:00 AM.
                foreach (string s in matches) {
                    Debug.WriteLineIf(debug, "file_a: " + s);
                    if (File.GetCreationTime(s) > dt) {
                        dt = File.GetCreationTime(s);
                        newestFile = s;
                    }
                }
                Debug.WriteLineIf(debug, "newestFile: " + newestFile);
                re = new Regex(@"_[0-9]{1,}_[AFP]\.txt$");
                matches = from file in Directory.GetFiles(TDR, cdCollNew + "_*_?.txt", SearchOption.TopDirectoryOnly)
                          where re.IsMatch(file)
                          select file;
                // matches is set of all files like BoardNumber_SerialNumber_#_C.txt, where # is an integer ≥ 1 and C a single character in the set {'A','F','P'}.
                // Examples; BoardNumber_SerialNumber_1_P.txt
                //           BoardNumber_SerialNumber_3_A.txt
                //           BoardNumber_SerialNumber_15_F.txt.
                int maxNumber = 0;
                string fileName;
                char[] AFP = { 'A', 'F', 'P' };
                foreach (string s in matches) {                                                 // Assume a fileName = "P:\Test\TDR\BoardNumber\BoardNumber_SerialNumber_15_F.txt".
                    fileName = s;
                    fileName = fileName.Replace(TDR + cdCollNew, "");                           // fileName = "_15_F.txt".
                    fileName = fileName.Replace(".txt", "");                                    // fileName = "_15_F".
                    fileName = fileName.Replace("_", "");                                       // fileName = "15F".
                    fileName = fileName.TrimEnd(AFP);                                           // fileName = "15".
                    if (int.Parse(fileName) > maxNumber) maxNumber = int.Parse(fileName);       // maxNumber = 15.
                }
                int nextNumber = ++maxNumber;                                                   // nextNumber = 16, assuming 15 was the maxNumber from matches.
                Debug.WriteLineIf(debug, "nextNumber: " + nextNumber);
                cdCollNew = TDR + cdCollNew + "_" + nextNumber + "_" + testEvent + ".txt";
                // cdCollNew = P:\Test\TDR\BoardNumber\BoardNumber_SerialNumber_16_P.txt, if SerialNumber finally passed.
                Debug.WriteLineIf(debug, "cdCollNew_c: " + cdCollNew);
                File.Move(newestFile, cdCollNew);
                // Above File.Move() renames hypothetical file:
                // "P:\Test\TDR\BoardNumber\BoardNumber_SerialNumber_103000.txt" to
                // "P:\Test\TDR\BoardNumber\BoardNumber_SerialNumber_16_P.txt".
                string[] testData = File.ReadAllLines(cdCollNew);
                // Spea's CDCOLL version 6.0 or higher supports Panelized test results, but ABT needs individual test data files.
                // Below code removes Panelization records.
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
}
