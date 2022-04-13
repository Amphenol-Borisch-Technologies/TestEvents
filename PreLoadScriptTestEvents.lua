local Event = {PASS = "P", ABORT = "A", FAIL = "F"}  -- These are also the EventCodes used by Test Events, for compatability.
local NA_SERIAL_NUMBER = "NA" -- "NA" for "Not Applicable".  Change to something more suitable if desired.
local TimeStart

-- Exclusively using setpersist() and getpersist() functions to persist crucial data in the Windows Registry, rather than using global variables in this script
-- to retain state between executions:
  -- The setpersist/getpersist functions are *explicitly* guaranteed by Cabletest to retain state for the duration of their Discovery session,
  -- and be accessible from any Lua code, which is exactly what we need.

function mpt.OnStart()
  TimeStart = os.time()
  local OK_Clicked, KeyboardInput, ReturnCode
  if (getpersist("ProjectNamePrevious") ~= Project.Name()) then
    -- True if 1st execution of this MPT Project's test program, therefore prompt for TechnicianNumber.
    local TechnicianNumber = "" ; if getpersist("TechnicianNumber") ~= nil then TechnicianNumber = getpersist("TechnicianNumber") end
    repeat
      repeat
        OK_Clicked, KeyboardInput = win.InputBox("", "Enter Tech #/Stamp #; e.g. \'T123\'", TechnicianNumber)
        -- Put the Technician Number in the Input Box, and completely select the Technician Number's text so that the test
        -- operator can simply click Okay for a re-test, or can type a new Passed Test Stamp #, erasing the previous one.
        KeyboardInput = string.upper(trim(KeyboardInput))
      until OK_Clicked and (KeyboardInput ~= "") -- Ignore the Cancel Button & disallow blank Technician Numbers.
      ReturnCode = win.MessageBox("You entered \'"..KeyboardInput.."\'.  Is this correct?\n\nClick Yes if Okay, No to reenter Technician #.","Technician # is \'"..KeyboardInput.."\'", win.MB_YESNO + win.MB_ICONQUESTION + win.MB_SETFOREGROUND)
    until ReturnCode == win.IDYES
    setpersist("TechnicianNumber",KeyboardInput)
    win.MessageBox("To change the Technician #, run a different program or close/reopen MPT Discovery.","Technician # is \'"..getpersist("TechnicianNumber").."\'", win.MB_OK + win.MB_ICONINFORMATION + win.MB_SETFOREGROUND)
  end

  local SerialNumberPrevious = "" ; if  getpersist("ProjectNamePrevious") == Project.Name() then SerialNumberPrevious = getpersist("SerialNumberPrevious") end
  -- True if not 1st execution of this MPT Project's test program.
  repeat
    OK_Clicked, KeyboardInput = win.InputBox("", "Enter Serial #, \'NA\' if none", SerialNumberPrevious)
    -- Put the previous Serial Number in the Input Box, and completely select the Serial Number's text so that the test
    -- operator can simply click Okay for a re-test, or can scan/type a new assembly's bar-coded Serial Number, erasing the previous one.
    KeyboardInput = string.upper(trim(KeyboardInput))
  until (not OK_Clicked) or (KeyboardInput ~= "") or (KeyboardInput == NA_SERIAL_NUMBER) -- Allow Cancel Button, disallow blank Serial Numbers, allow NA_SERIAL_NUMBER.
  if OK_Clicked then
    setpersist("SerialNumber",KeyboardInput)
    printtodevices(CON, "Technician # :   "..getpersist("TechnicianNumber"))
    printtodevices(CON, "Serial #     :   "..getpersist("SerialNumber"))
    printtodevices(CON, "Date/Time    :   "..os.date())
    printtodevices(CON, "")
  else
    setpersist("SerialNumber",NA_SERIAL_NUMBER)
  end
  setpersist("SerialNumberCancelled", not (OK_Clicked))
  -- Informs the MPT program if test operator cancelled entry of a Serial Number.
  -- Use with "Lua(if getpersist("SerialNumberCancelled") then AbortTest() end);" as first line of an MPT program; if operator chooses to cancel, let's honor their cancellation.
  -- This works even when getpersist("SerialNumberCancelled") == nil, as nil evaluates to false for logical operations.
end -- mpt.OnStart()


function mpt.OnPass()
  ProcessEvent(Event.PASS)
end -- mpt.OnPass()


function mpt.OnAbort()
  ProcessEvent(Event.ABORT) -- Incidentally, the Abort Event prevents the Stop Event from occurring, so can't use function mpt.OnStop() after an Abort Event.
end -- mpt.OnAbort()


function mpt.OnFail()
  ProcessEvent(Event.FAIL)
end -- mpt.OnFail()


function ProcessEvent(EventCode)
  local NumberAssembly, NumberSerial = "", ""
  if getpersist("MultipleAssemblyProjectName") == Project.Name()
    then NumberAssembly = getpersist("AssemblyNumber")
    else NumberAssembly = Project.Name()
  end

  printtodevices(CON,"") -- \n doesn't work in printtodevices().
  printtodevices(CON, "Test Time: "..os.date("!%X",os.difftime(os.time(), TimeStart))) -- Prints elaspsed test time in hours:minutes:seconds format (HH:MM:SS).
  if     EventCode == Event.PASS  then printtodevices(CON, "PASSED!   :-)")  -- Final line of test report is result.
  elseif EventCode == Event.ABORT then printtodevices(CON, "Aborted.  :-|")
  elseif EventCode == Event.FAIL  then printtodevices(CON, "Failed.   :-(")
  end

  NumberSerial = getpersist("SerialNumber") ; if (NumberSerial == nil) or (NumberSerial == "") then NumberSerial = NA_SERIAL_NUMBER end
  -- Should never be such, but check for nil or null, set to NA if so.
  local PathTDR = "P:\\Test\\TDR\\"..NumberAssembly
  os.mkdir(PathTDR)  -- Creates if non-existent, does nothing if pre-existent.
  local PathPartial = PathTDR.."\\"..NumberAssembly.."_"..NumberSerial.."_"
  SaveConsoleToFile(PathPartial..NextSequenceNumber(PathPartial).."_"..EventCode..".txt")
  -- SaveConsoleToFile() saves the console's contents to an ASCII text file, much more readable than CDA's Comma Separated Values (.csv) file.
  -- Why Cabletest doesn't document it is a mystery.
  setpersist("SerialNumberPrevious",NumberSerial)

  require "luacom" -- Load the C:\MPT\lib\luacom.dll module, available from http://www.tecgraf.puc-rio.br/~rcerq/luacom.
  local sh = luacom.CreateObject "WScript.Shell" -- Use LuaCOM to create a Windows Scripting (Visual Basic Script or VBS) Shell.
  sh:Run ("\"C:\\Program Files\\TestEvents\\TestEvent.bat\" \""..NumberAssembly.."\" \""..NumberSerial.."\" "..EventCode,0)
  -- Run the TestEvents batch file in the Windows Scripting shell because the WScript shell can execute silently
  -- and invisibly, unlike using os.execute(), which opens a Windows command prompt while TestEvents.bat runs.
  -- This gets fairly annoying and distracting, hence usage of LuaCOM and WScript.

  -- String "\"C:\\Program Files\\TestEvents\\TestEvent.bat\" \""..NumberAssembly.."\" \""..NumberSerial.."\" "..EventCode
  -- is distilled by Lua to "C:\Program Files\TestEvents\TestEvent.bat" "Assy 1" "Serial 1" F
  -- when NumberAssembly = "Assy 1", NumberSerial = "Serial 1" and EventCode = F.
  -- Thus embedded spaces, although discouraged, are supported.
  -- Because of the space in "C:\\Program Files\\", Lua must pass a quoted string to the Windows Scripting shell, accomplished by escaping the quotes; \".
  -- Lua also needs to escape the Windows \ path separator; \\.
  setpersist("EventCodePrevious",EventCode)
  setpersist("ProjectNamePrevious",Project.Name())
end -- ProcessEvent(EventCode)


function trim(s)  -- Strip leading/trailing spaces from string "s".  Shamelessly stolen off the Internet...
  return s:find"^%s*$" and '' or s:match"^%s*(.*%S)"
end -- trim(s)


function NextSequenceNumber(testPath)
  local SequenceNumber = 1
  while true do
    local fe = false
    fe = fe or FileExists(testPath..SequenceNumber.."_"..Event.PASS..".txt")
    fe = fe or FileExists(testPath..SequenceNumber.."_"..Event.ABORT..".txt")
    fe = fe or FileExists(testPath..SequenceNumber.."_"..Event.FAIL..".txt")
    if fe then
      SequenceNumber = SequenceNumber + 1
    else
      return SequenceNumber -- Have the first unused Sequence Number, return to calling routine.
    end
  end
end -- NextSequenceNumber(testPath)


function FileExists(filePath)
  local f = io.open(filePath, "r")
  if (f ~= nil) then
    f:close()
    return true
  else
    return false
  end
end -- FileExists(filePath)
