# TestEvents
- Record Test Events from automated test equipment such as Keysight i3070™, Nexeya MPT-5000™ & Teradyne Spectrum 8800™ into a PostgreSQL™ RDBMS.
- Connect to the PostgreSQL TestEvents database via Microsoft Access™ to run queries & reports.
- Tested with Microsoft Windows™ 7 32-bit.

# TestEvents client application
Common items for all TestEvents clients:
- TestEvent.java
- SpeaWrapper.java
- TestEvent.bat
- PostgreSQL TestEvents schema
- Microsoft Access front-end database

# Keysight i3070
- TestEventsUCI.cpp
- testmain_ABT_with_TestEvents_and_Datalogging

# Nexeya MPT-5000
- PreLoadScriptTestEvents.lua

# Teradyne Spectrum 8800
- TestEventsGFI.cpp
- To register the GFI, reference:
  - MPTTOOL.TLR.TXT
  - Spectrum 8800 TestEvents GFI Registration.jpg
