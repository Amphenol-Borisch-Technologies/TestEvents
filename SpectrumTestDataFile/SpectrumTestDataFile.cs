using System.IO;
using System.Threading;

namespace SpectrumTDR {
    public class SpectrumTestDataFile {
        public static void Move(string assembly_number, string serial_number, string event_code) {
            string dataLogSpectrum = "C:\\TPD\\" + assembly_number + "\\" + assembly_number + ".datalog.dat";
            // assembly_number is the active Test Program folder name passed by TestEventsGFI.cpp from Teradyne GFI function gfiGetOpenProgramPath().
            // We use the Test Program folder name as the Assembly Number, which isn't entirely accurate, but is adequate.
            if (File.Exists(dataLogSpectrum)) {   // Not all Spectrum programs have datalogging enabled.  If not, dataLogSpectrum won't exist.
                string TDR = "C:\\TDR\\" + assembly_number + "\\";
                int newLogNumber = Directory.GetFiles(TDR, assembly_number + "_" + serial_number + "_*.txt", SearchOption.TopDirectoryOnly).Length + 1;
                // string newLogFile = TDR + assembly_number + "_" + serial_number + "_" + newLogNumber.ToString() + "_" + System.Environment.MachineName + "_" + event_code + ".txt";
                string newLogFile = TDR + assembly_number + "_" + serial_number + "_" + newLogNumber.ToString() + "_" + event_code + ".txt";
                // event_code is the Test Event, from set {"A","F","P"} for Abort, Fail & Pass respectively.
                do {
                    Thread.Sleep(100);
                    try { File.Move(dataLogSpectrum, newLogFile); } catch (IOException) { }
                    // Above File.Move() would move hypothetical file "C:\TPD\CCA123\CCA123.datalog.dat" to "C:\TDR\CCA123\CCA123_ABT2208-006_3_Spectrum8852-1_P.txt".
                    // Spectrum app automatically recreates datalog file "C:\TPD\CCA123\CCA123.datalog.dat" during next test run.
                    // File dataLogSpectrum may occasionally still be open/in-use when File.Move() runs, though per
                    // Teradyne's Spectrum 8800-Series Client Application Tools manual, it's implied dataLogSpectrum should be closed:
                    // "MPTEV_GFI_POST _EXECUTE This event is broadcast at the end of each test program cycle 
                    //  and is useful for moving and reading datalog files, compiling test statistics, communicating with 
                    //  robots, releasing vacuums, and so forth."
                } while (File.Exists(dataLogSpectrum));
            }
        }
    }
}
