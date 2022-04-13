@echo off
"C:\Program Files\Java\jre7\bin\java.exe" -classpath "C:\Program Files\TestEvents;C:\Program Files\Java\jre7\lib;C:\Program Files\Java\jre7\lib\ext;C:\Program Files\TestEvents\TestEvents.jar" TestEvents.client.SpeaWrapper
REM
REM  This is a Microsoft Windows command prompt batch file.
REM 
REM  This script invokes the Java Virtual Machine with class 
REM  TestEvents.client.TestEvent.class.  It also passes in an Assembly Number (%1),
REM  a Serial Number (%2) and an Event Code (%3) as parameters.
REM 
REM  This script exists solely to de-couple the calling interface from the 
REM  underlying implementation.  In other words, if it's necessary to change
REM  the actual TestEvent program, perhaps to migrate it to a different language,
REM  or place the executable in a different path, only this script has to change.
REM  Thus, no client ICT test programs have to be modified to invoke the
REM  modified executable correctly.
REM  
REM  Another benefit of this script is that if the implementation fails 
REM  catastrophically for some reason (for example, the RDBMS server dies)
REM  then this script can quickly be re-configured to simply exit, without
REM  invoking TestEvents. 
REM
REM  The Agilent 3070 system is bundled with it's own older version of java.
REM  Agilent's version is incompatible with TestEvents, causes a
REM  class version error, and is the default java version used when invoked 
REM  from an Agilent testplan.  This is why the full path to the java 
REM  executable ("C:\Program Files\Java\jre7\bin\java.exe") is used
REM  to run TestEvents, and why the classpath is specified explicitly.

