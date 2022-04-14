package TestEvents.client;
import java.io.File;
import java.io.FileFilter;
import java.net.InetAddress;
import java.net.UnknownHostException;
import org.apache.commons.io.filefilter.WildcardFileFilter;
import org.apache.commons.io.FileUtils;
/*
 * @author Phillip Smelt
 * @version 1.0, 4/5/2022
 */
public final class SpectrumWrapper {
    private SpectrumWrapper() {};

    public static void main(String[] args) {
        try     { TestEvent.main(args); }
        catch   (Exception e) { TestEvent.SaveException(e.toString()); }
        finally { ProcessDataLog(args); }
    }
    
    private static void ProcessDataLog(String[] args) {
        File dataLogSpectrum = new File("C:\\TPD\\" + args[0] + "\\" + args[0] + ".datalog.dat");
        // args[0] is the active Test Program folder name passed by TestEventsGFI.cpp from Teradyne GFI function gfiGetOpenProgramPath().
        // We use the Test Program folder name as the Assembly Number, which isn't entirely accurate, but is adequate.
        if(dataLogSpectrum.isFile()) {
            // Not all Spectrum programs have datalogging enabled.  If not, dataLogSpectrum won't exist.
            String TDR = "C:\\TDR\\" + args[0] + "\\";
            File folderTDR = new File(TDR);
            FileFilter fileFilter = new WildcardFileFilter(args[0] + "_" + args[1] + "_*.txt");
            // args[1] is the Serial Number.
            String newLogNumber = Integer.toString(folderTDR.listFiles(fileFilter).length + 1);
            File newLogFile = new File(TDR + args[0] + "_" + args[1] + "_" + newLogNumber + "_" + GetHostName() + "_" + args[2] + ".txt");
            // args[2] is the Test Event, from set {'A','F','P'} for Abort, Fail & Pass respectively.
            try {
                FileUtils.moveFile(dataLogSpectrum, newLogFile);
                // Example: move file "C:\TPD\CCA123\CCA123.datalog.dat" to "C:\TDR\CCA123\CCA123_ABT2208-006_3_Spectrum8852-1_P.txt".
                // Spectrum app automatically recreates example datalog file "C:\TPD\CCA123\CCA123.datalog.dat" during next test run.
                // File dataLogSpectrum may occasionally still be open/in-use when moveFile() runs, though per
                //  Teradyne's Spectrum 8800-Series Client Application Tools manual, it's implied it should be closed:
                    // MPTEV_GFI_POST _EXECUTE This event is broadcast at the end of each test program cycle 
                    // and is useful for moving and reading datalog files, compiling test statistics, communicating with 
                    // robots, releasing vacuums, and so forth.
                // If moveFile() fails, then during next test iteration dataLogSpectrum will contain test data results from both the
                // present and prior test runs.  MoveFailed() documents if this is occurring, and attempts recovery.
            }
            catch (Exception e) {
                MoveFailed(dataLogSpectrum, newLogFile);
            }
        }
    }
    
    private static void MoveFailed(File dataLogSpectrum, File newLogFile) {
        TestEvent.SaveException("File " + dataLogSpectrum.toString() + " not moved to " + newLogFile.toString() + " on first attempt.");
        try {
            Thread.sleep(1500);
            FileUtils.moveFile(dataLogSpectrum, newLogFile);
        }
        catch (Exception e) {
            TestEvent.SaveException(e.toString());
            TestEvent.SaveException("File " + dataLogSpectrum.toString() + " not moved to " + newLogFile.toString() + " on second attempt!");
        }
    }
    
    private static String GetHostName() {
        try {
            return InetAddress.getLocalHost().getHostName();
        } 
        catch (UnknownHostException e) {
            TestEvent.SaveException(e.toString());
            return "ERROR-HOST-NAME";
        }
    }
}
