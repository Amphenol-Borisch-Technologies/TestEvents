	// The main function registers this Teradyne Spectrum 8800 GFI (General Factory Interface) program and calls the StartEventLoop call. It exits when that call returns.
	//
	// This TestEventsGFI prompts the Spectrum 8800 test operator to enter assembly serial numbers prior to each test run, and invokes C:\Program Files\TestEvents\TestEvent.bat
	// to insert the test results into a PostgreSQL relational database where they're readily available to interested parties.
	//
	// Similar applications are under development for the Agilent 3070 ICT, the CableTest MPT-5000 and the ABT HP-VEE test systems.  Eventually, hopefully, all Windows based test
	// systems will save test results in this fashion, and this data will be easily available to Quality, Production, Test Engineering and Test Technicians for their various nefarious purposes.
	//
	// TestEventsGFI runs as an invisible background process because it's a standard Win32 Application without any displayable components.  This is ideal for our purposes.
	// A Win32 Console Application always displays a "DOS box" console, a useless confusing distraction to a test operator since it'd just sit there, not doing/displaying anything,
	// but closing said console would terminate its TestEventsGFI application, which we absolutely don't want to do.
	//
	// I deliberately coded with emphasis on horizontal usage; most of the code below extends far past the standard 80 columns of text.  Current computer screens have 16 to 9 horizontal to vertical
	// aspect ratios, so it's sensible to program short blocks of wide code instead of the traditional tall blocks of narrow code.  This way, you can see much more of the program in one screen,
	// rather than continuously scrolling up and down to view it; I find it very helpful to see as much of a program as possible without scrolling.  Therefore, don't be surprised to see multiple
	// statements of logically connected code on one line, like sentences in a paragraph, separated by blank lines, like paragraphs on a page.
	//
	// Phillip Smelt, 3/7/14
