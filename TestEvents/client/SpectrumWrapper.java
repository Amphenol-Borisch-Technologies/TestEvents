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
        try { TestEvent.main(args); }
        catch (Exception e) { TestEvent.SaveException(e.toString()); }
        finally { 
            File dataLogSpectrum = new File("C:\\TPD\\" + args[0] + "\\" + args[0] + ".datalog.dat");
            // args[0] is the active Test Program folder name passed by TestEventsGFI.cpp from Teradyne GFI function gfiGetOpenProgramPath().
            // We use the Test Program folder name as the Assembly Number, which isn't entirely accurate, but is sufficient.
            if(dataLogSpectrum.isFile()) {
                String TDR = "C:\\TDR\\" + args[0] + "\\";
                File folderTDR = new File(TDR);
                FileFilter fileFilter = new WildcardFileFilter(args[0] + "_" + args[1] + "_*.txt");
                // args[1] is the Serial Number.
                String newLogNumber = Integer.toString(folderTDR.listFiles(fileFilter).length + 1);
                File newLogFile = new File(TDR + args[0] + "_" + args[1] + "_" + newLogNumber + "_" + GetHostName() + "_" + args[2] + ".txt");
                // args[2] is the Test Event.
                try {
                    FileUtils.copyFile(dataLogSpectrum, newLogFile);
                    // Example: file "C:\TPD\G77066A-D10\G77066A-D10.datalog.dat" copied to "C:\TDR\G77066A-D10\G77066A-D10_ABT2208-006_3_Spectrum8852-1_P.txt".
                    // Supposedly creates folder G77066A-D10 if non-existent, per FileUtils documentation.
                } catch (Exception e) {
                    TestEvent.SaveException(e.toString());
                } finally {
                    try { FileUtils.forceDelete(dataLogSpectrum); }
                        // This butt-ugly triply indented try/catch/finally exists to desperately attempt to delete dataLogSpectrum, which occasionally
                        // is still open or in-use when this code runs, causing an exception.
                        // Spectrum app automatically recreates file "C:\TPD\G77066A-D10\G77066A-D10.datalog.dat" during next test run.
                    catch (Exception e) { TestEvent.SaveException(e.toString()); }
                }
            }
        }
    }

    private static String GetHostName() {
        try {
            return InetAddress.getLocalHost().getHostName();
        } catch (UnknownHostException e) {
            TestEvent.SaveException(e.toString());
            return "ERROR-HOST-NAME";
        }
    }
}
