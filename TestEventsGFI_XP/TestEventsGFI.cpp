#include "stdafx.h"
#include <afxwin.h>
#include "gficlnt.l"
#pragma comment( lib, "CLIB")
#include <clib.l>
#include "resource.h"
#include <string>
#include <iostream>
#include <msclr\marshal_cppstd.h>
#using "SpectrumTestDataFile.dll"
using namespace System;
// When compiling/building this code with Visual Studio 2022, received the following 2 errors:
//		1 > C:\Program Files(x86)\Teradyne\Spectrum\5.2\TOOLKIT\INCLUDE\dhc_api.h(400, 31) : error C2365 : 'PS_NONE' : redefinition; previous definition was 'enumerator'
//		1 > C:\Program Files(x86)\Windows Kits\10\Include\10.0.19041.0\um\shobjidl_core.h(3851) : message: see declaration of 'PS_NONE'
//
// Resolved above by egregious hack of replacing all instances of PS_NONE with PTS_NONE in below 4 files:
//		Searching for: PS_NONE
//		C:\Program Files(x86)\Teradyne\Spectrum\5.2\TOOLKIT\INCLUDE\CLIB.H: 2
//		C:\Program Files(x86)\Teradyne\Spectrum\5.2\TOOLKIT\INCLUDE\CLIB.L: 3
//		C:\Program Files(x86)\Teradyne\Spectrum\5.2\TOOLKIT\INCLUDE\dhc_api.h: 1
//		C:\Program Files(x86)\Teradyne\Spectrum\5.2\TOOLKIT\INCLUDE\SPECTRUMCLIB.H: 3
//		Found 9 occurrence(s) in 4 file(s), 9378 ms
// While above hack works to compile this code, it would likely fail when referencine anything to do with the Spectrum 8852 Power Supplies, of which PS_NONE is an identifier.
// There's likely elegant resolutions to such header file conflicts, but I couldn't find any via Google, and this worked.  Sigh.
void Initialize(void);
void Terminate(void);
void PostExecuteCallback(MPTEV_EVENT event, LPCTSTR sender, int* iret, LPCTSTR command, long scope, BOOL IsMaster);
void ExitCallback(MPTEV_EVENT event, LPCTSTR sender, int* iret, LPCTSTR command, long scope, BOOL IsMaster);

int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
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
	gfiRegisterInitAndTermHandlers("TestEventsGFI", Initialize, Terminate);
	gfiStartEventLoop();
	return 0;
}

void Initialize(void) {
	gfiAddHandler(MPTEV_GFI_POST_EXECUTE, (void*)PostExecuteCallback);
	gfiAddHandler(MPTEV_EXIT, (void*)ExitCallback);
}

void Terminate(void) {
	gfiRemoveHandler(MPTEV_GFI_POST_EXECUTE);
	gfiRemoveHandler(MPTEV_EXIT);
}

void PostExecuteCallback(MPTEV_EVENT event, LPCTSTR sender, int* iret, LPCTSTR command, long scope, BOOL IsMaster) {
	if ((int)scope == UNIT_PROGRAM_TYPE) {											// MPTEV_GFI_PRE_EXECUTE event at Program level.
		char serial_number[256] = "";
		if (gfiGetSystemVarString(CLIB_SERIALNUMBER, serial_number)) {
			if (strlen(serial_number) != 0) {
				char* event_code = NULL;
				if (gfiGetSysFlag(SF_CANCEL_ABORT))				event_code = "A";	// CancelAbort == TRUE, like Trailer'S CANCELLED Page; "A" is the event code for test Abort.
				else if (gfiGetSysFlag(SF_GENERAL_FAILURE))		event_code = "F";	// (GENERALFAILURE && ! CancelAbort) == TRUE, like Trailer's FAILED Pagel "F" is the event code for test Failed.
				else											event_code = "P";	// (! GENERALFAILURE && ! CancelAbort) == TRUE, like Trailer's PASSED Page; "P" is the event code for test Passed.
				unsigned char programUC[256] = "";		gfiGetOpenProgramPath(programUC);	char* programCP = (char*)programUC;
				char* sub_path = strtok(programCP, "\\");	char* last_sub_path = sub_path;		while ((sub_path = strtok(NULL, "\\")) != NULL) last_sub_path = sub_path;
				char assembly_number[256] = "";		strcpy_s(assembly_number, last_sub_path);
				// C++/CLI code below, invokes C# class SpectrumTestDataFile to do file operations more easily handled by .Net.
				System::String^ an = msclr::interop::marshal_as<System::String^>(assembly_number);
				System::String^ sn = msclr::interop::marshal_as<System::String^>(serial_number);
				System::String^ ec = msclr::interop::marshal_as<System::String^>(event_code);
				SpectrumTDR::SpectrumTestDataFile::Move(an, sn, ec);
			}
		}
	}
}

void ExitCallback(MPTEV_EVENT event, LPCTSTR sender, int* iret, LPCTSTR command, long scope, BOOL IsMaster) {} // Per Teradyne, must register this event even if doing nothing with it.
