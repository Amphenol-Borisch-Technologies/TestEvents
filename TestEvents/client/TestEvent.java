package TestEvents.client;
import java.sql.DriverManager;
import java.sql.Connection;
import java.sql.Statement;
import java.sql.SQLException;
import java.io.BufferedReader;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.net.InetAddress;
import java.net.UnknownHostException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.nio.file.LinkOption;
import java.text.SimpleDateFormat;
import java.util.Date;
/*
 * @author Phillip Smelt
 * @version 1.0, 2/29/14
 */
public final class TestEvent {
    public static final  String TEST_EVENTS_HOME = "C:\\Program Files\\TestEvents\\";
    private static final String SAVED_EXCEPTIONS_FILE = TEST_EVENTS_HOME + "SavedExceptions.txt";
    private static final String SAVED_EVENTS_FILE = TEST_EVENTS_HOME + "SavedEvents.txt";
    private static final String RDBMS_DRIVER = "org.postgresql.Driver";
    private static final String RDBMS_LOGIN = "testevent";
    private static final String RDBMS_PASSWORD = "testevent";
    private static final String RDBMS_URL = "jdbc:postgresql://vm-int-pgsql-01.borisch.local:5432/test";
    // private static final String RDBMS_URL = "jdbc:postgresql://postgresql-test.borisch.local:5432/test";

    private TestEvent() {};

    public static void main(String[] args) {
        SaveEvent(args);
        InsertSavedEvents();
  	// 3/10/14 - TestEvents can occur too infrequently to queue them prior to insertion; need to insert them as they're created.
        // if (ReadyToInsertEvents()) InsertSavedEvents();
        if (Files.exists(Paths.get(SAVED_EXCEPTIONS_FILE), LinkOption.NOFOLLOW_LINKS)) InsertSavedExceptions();
    }

    public static void SaveException(String exception) {
        try (PrintWriter printWriter = new PrintWriter(new FileWriter(SAVED_EXCEPTIONS_FILE, true))) {
             String e = exception.substring(0,Math.min(255,exception.length()));  // Truncate exception to 255 characters, since its SQL field is 255 characters max.
             String f = e.replace("\"", "^");   // Replace " with ^ to avoid SQL insertion errors if truncation results in odd number of ".
             String g = f.replace ("'", "|");   // Replace ' with | to avoid SQL insertion errors if truncation results in odd number of '.
             printWriter.println("INSERT INTO test_events.exceptions (date, time, host_name, exception, user_dir) "  +
                                 "VALUES ('" + new SimpleDateFormat("MM/dd/yy").format(new Date()) + "','" + new SimpleDateFormat("h:mm:ss a").format(new Date()) + "','" + GetHostName() + "','" + g + "','" + System.getProperty("user.dir") + "');");
             printWriter.close();
        } catch (IOException e) {
            throw new RuntimeException(e.toString());
        }
    }

    private static void SaveEvent(String[] args) {
        String assembly_number = "ERROR-ASSEMBLY-NUMBER";  // Default value if args.length != 1, 2 or 3.
        String serial_number = "ERROR-SERIAL-NUMBER";      // Default value if args.length != 1 or 2.
        String event_code = "E";                           // Default value if args.length != 3; relates to "ERROR-EVENT" in RDBMS table test_events.event_codes.
        switch (args.length) {
            case 3: // Desired args.length, only executes if args.length == 3.
                event_code = args[2];
            case 2: // Executes if args.length == 2 or 3.
                serial_number = args[1];
            case 1: // Executes if args.length == 1, 2 or 3.
                assembly_number =  args[0];
        }
        try (PrintWriter printWriter = new PrintWriter(new FileWriter(SAVED_EVENTS_FILE, true), true)) {
	     printWriter.println("INSERT INTO test_events.events (assembly_number, serial_number, event_code, host_name, date, time) " +
                                 "VALUES ('" + assembly_number + "','" + serial_number + "','" + event_code + "','" + GetHostName()+ "','" + new SimpleDateFormat("MM/dd/yy").format(new Date()) + "','" + new SimpleDateFormat("h:mm:ss a").format(new Date()) + "');");
             printWriter.close();
        } catch (IOException e) {
            SaveException(e.toString());
        }
    }

    private static boolean ReadyToInsertEvents() {
        int savedEvents = 0;
        try (BufferedReader bufferedReader = new BufferedReader(new FileReader(SAVED_EVENTS_FILE))) {
            while (bufferedReader.readLine() != null) savedEvents++;  // Get # lines in file.
            bufferedReader.close();
        } catch (IOException e) {
            SaveException(e.toString());
        }            
        if ((savedEvents != 0) && ((savedEvents % 10) == 0)) return true; // Queue up multiples of 10 TestEvents before inserting them.
        else return false;
    }

    private static void InsertSavedEvents() {
        try {
            Class.forName(RDBMS_DRIVER);
            DriverManager.setLoginTimeout(3);
            try (Connection connection = DriverManager.getConnection(RDBMS_URL, RDBMS_LOGIN, RDBMS_PASSWORD); Statement statement = connection.createStatement()) {
                statement.setQueryTimeout(3);
                try (BufferedReader bufferedReader = new BufferedReader(new FileReader(SAVED_EVENTS_FILE))) {
                    String SQL;
                    while ((SQL = bufferedReader.readLine()) != null) if (statement.executeUpdate(SQL) != 1) SaveException("SQL Statement Failed: " + SQL);
                    bufferedReader.close();
                    Files.delete(Paths.get(SAVED_EVENTS_FILE));                     
                } catch (IOException | SQLException e) {
                    SaveException(e.toString());
                }
            } catch (SQLException e) {
                SaveException(e.toString());
            }
        } catch (ClassNotFoundException e) {
            SaveException(e.toString());
        }
    }

    private static String GetHostName() {
        try {
            return InetAddress.getLocalHost().getHostName();
        } catch (UnknownHostException e) {
            SaveException(e.toString());
            return "ERROR-HOST-NAME";
        }
    }

    private static void InsertSavedExceptions() {
        try {
            Class.forName(RDBMS_DRIVER);
            DriverManager.setLoginTimeout(3);
            try (Connection connection = DriverManager.getConnection(RDBMS_URL, RDBMS_LOGIN, RDBMS_PASSWORD); Statement statement = connection.createStatement()) {
                statement.setQueryTimeout(3);
                boolean insertErrors = false;
                try (BufferedReader bufferedReader = new BufferedReader(new FileReader(SAVED_EXCEPTIONS_FILE))) {
                    String SQL;
                    while ((SQL = bufferedReader.readLine()) != null) insertErrors = insertErrors || (statement.executeUpdate(SQL) != 1);
                    if (! insertErrors) {
                        bufferedReader.close();
                        Files.delete(Paths.get(SAVED_EXCEPTIONS_FILE));
                    } // If errors, retain SAVED_EXCEPTIONS_FILE for debugging.
                } catch (IOException | SQLException e) {
                    SaveException(e.toString());
                } 
            } catch (SQLException e) {
                SaveException(e.toString());
            }
        } catch (ClassNotFoundException e) {
            SaveException(e.toString());
        }
    }
}
