package TestEvents.client;
import java.io.IOException;
import java.nio.file.*;
import java.io.File;
import java.io.FileFilter;
import java.net.InetAddress;
import java.net.UnknownHostException;
import org.apache.commons.io.filefilter.WildcardFileFilter;
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
        Path dataLogSpectrum = Paths.get("C:\\TPD\\" + args[0] + "\\" + args[0] + ".datalog.dat");
        // args[0] is the active Test Program folder name passed by TestEventsGFI.cpp from Teradyne GFI function gfiGetOpenProgramPath().
        // We use the Test Program folder name as the Assembly Number, which isn't entirely accurate, but is adequate.
        if(Files.exists(dataLogSpectrum)) {
            // Not all Spectrum programs have datalogging enabled.  If not, dataLogSpectrum won't exist.
            String TDR = "C:\\TDR\\" + args[0] + "\\";
            File folderTDR = new File(TDR);
            FileFilter fileFilter = new WildcardFileFilter(args[0] + "_" + args[1] + "_*.txt");
            // args[1] is the Serial Number.
            String newLogNumber = Integer.toString(folderTDR.listFiles(fileFilter).length + 1);
            Path newLogFile = Paths.get(TDR + args[0] + "_" + args[1] + "_" + newLogNumber + "_" + GetHostName() + "_" + args[2] + ".txt");
            // args[2] is the Test Event, from set {'A','F','P'} for Abort, Fail & Pass respectively.
            do {
                try { Files.move(dataLogSpectrum, newLogFile, StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING); }
                    // Example: move file "C:\TPD\CCA123\CCA123.datalog.dat" to "C:\TDR\CCA123\CCA123_ABT2208-006_3_Spectrum8852-1_P.txt".
                    // Spectrum app automatically recreates datalog file "C:\TPD\CCA123\CCA123.datalog.dat" during next test run.
                    // File dataLogSpectrum may occasionally still be open/in-use when move() runs, though per
                    // Teradyne's Spectrum 8800-Series Client Application Tools manual, it's implied it should be closed:
                    //   MPTEV_GFI_POST _EXECUTE This event is broadcast at the end of each test program cycle 
                    //   and is useful for moving and reading datalog files, compiling test statistics, communicating with 
                    //   robots, releasing vacuums, and so forth.
                catch (IOException io) {
                    TestEvent.SaveException(io.toString());
                    TestEvent.SaveException("File " + dataLogSpectrum.toString() + " possibly not moved to " + newLogFile.toString() + ".");
                }
                try { Thread.sleep(500); }     
                catch (InterruptedException ie) { TestEvent.SaveException(ie.toString()); }
            } while (Files.exists(dataLogSpectrum));
        }
    }
    
    private static String GetHostName() {
        try { return InetAddress.getLocalHost().getHostName(); } 
        catch (UnknownHostException e) {
            TestEvent.SaveException(e.toString());
            return "ERROR-HOST-NAME";
        }
    }
}
