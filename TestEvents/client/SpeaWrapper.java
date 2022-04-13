package TestEvents.client;
import java.nio.charset.Charset;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.List;
/*
 * @author Phillip Smelt
 * @version 1.0, 12/5/2016
 */
public final class SpeaWrapper {
    private SpeaWrapper() {};

    public static void main(String[] args) {
        try {
            List<String> testResults = Files.readAllLines(Paths.get(TestEvent.TEST_EVENTS_HOME + "TestResult.txt"), Charset.defaultCharset());
            if (testResults == null) throw new Exception("Spea Test Result File was empty.");
            String partNumber = getResult(testResults, "PartNumber");
            int numberOfBoards = Integer.parseInt(getResult(testResults, "NumberOfBoards"));
            String serialNumber, boardResult, testEvent;
            for (int i = 1; i <= numberOfBoards ; i++) {
                serialNumber = getResult(testResults, "SN" + i);
                if (serialNumber.equals("")) serialNumber = "NA";
                boardResult = getResult(testResults, "BoardResult" + i);
                testEvent = transformBoardResultToTestEvent(boardResult);
                if (! testEvent.equals("NA")) TestEvent.main(new String[] {partNumber, serialNumber, testEvent});
                // Don't record TestEvents for BoardResults that are "NA" or Not Applicable.
            }
        }
        catch (Exception e) {
            TestEvent.SaveException(e.toString());
        }
    }

    private static String getResult(List<String> testResults, String resultName) {
        String[] result = new String[2];
        for (String line : testResults) {
            result = line.split("=", 2);
            if (result[0].equalsIgnoreCase(resultName)) return result[1].trim();
        }
        return "";
    }
    
    private static String transformBoardResultToTestEvent(String boardResult) {
        switch (boardResult) {
            case "Fail":
                return "F";     // "F" = TestEvent "Failed".
            case "Interrupted":
                return "A";     // "A" = TestEvent "Aborted".
            case "Pass":
                return "P";     // "P" = TestEvent "Passed".
            default:
                return "NA";
                // Default testEvent = "NA" for "Not Applicable".  "NA" is applied to the below BoardResults indiscriminately:
                    // - CCAs inside a panel that aren't selected for testing, which returns "None".
                    // - Fiducial alignment failures, which returns "Align Fail".
                    // - All other BoardResults that aren't explicitly included in the above Switch statement.
                        // - I'd detail them, except I can't find an exhaustive list, and the above two are all I currently know.
                        // - Unknown Spea BoardResults might actually be better categorized as "A" (Aborted), "E" (ERROR-EVENT-CODE) or "F" (Failed) TestEvents, but without knowing them, "NA" must suffice.
        }
    }
}
