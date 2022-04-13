#include "stdafx.h"
#include "stdio.h"
#include "C:\Agilent_ICT\standard\uciapi.h"
#include "TestEventsUCI.h"
#include "resource.h"
#include <Windows.h>
#include <ShellAPI.h>

#ifndef UNICODE
#define UNICODE
#endif
#define DEFAULT_STRING 256

wchar_t serial_number[DEFAULT_STRING] = L"";
wchar_t previous_serial_number[DEFAULT_STRING] = L"";
HWND hWnd = NULL;
HINSTANCE hWinMainInstance;

PCHAR* CommandLineToArgvA(PCHAR CmdLine, int* _argc);
void ANSI_encode(LPWSTR UTF16_In, PSZ ANSI_Out);
void GetSerialNumber(UCIPARMHANDLE ph);
LRESULT CALLBACK DlgProc(HWND hWndDlg, UINT Msg, WPARAM wParam, LPARAM lParam);

int APIENTRY _tWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpCmdLine, int nCmdShow) {
	// The main function creates an Agilent 3070 UCI Server (Universal C Interface) and registers its functions.
	//
	// This TestEventsUCI prompts the Agilent 3070 test operator to enter assembly Serial Numbers prior to each test run, which are passed as one of the parameters for the
	// C:\Program Files\TestEvents\TestEvent.bat batch file when a test run is completed.
	// TestEvent.bat inserts test results into a PostgreSQL relational database where they're readily available to interested parties.
	//
	// Similar applications are under development for the Spectrum 8800, the CableTest MPT-5000 and the ABT HP-VEE test systems.  Eventually, hopefully, all Windows based test
	// systems will save test results in this fashion, and this data will be easily available to Quality, Production, Test Engineering and Test Technicians for their various nefarious purposes.
	//
	// TestEventsUCI runs as an invisible background process because it's a standard Win32 Application without any displayable components.  This is ideal for our purposes.
	// A Win32 Console Application always displays a "DOS box" console, a useless confusing distraction to a test operator since it'd just sit there, not doing/displaying anything,
	// but closing said console would terminate its TestEventsUCI application, which we absolutely don't want to do.
	//
	// I deliberately coded with emphasis on horizontal usage; most of the code below extends far past the standard 80 columns of text.  Current computer screens have 16 to 9 horizontal to vertical
	// aspect ratios, so it's sensible to program short blocks of wide code instead of the traditional tall blocks of narrow code.  This way, you can see much more of the program in one screen,
	// rather than continuously scrolling up and down to view it; I find it very helpful to see as much of a program as possible without scrolling.  Therefore, don't be surprised to see multiple
	// statements of logically connected code on one line, like sentences in a paragraph, separated by blank lines, like paragraphs on a page.
	//
	// Phillip Smelt, 3/26/14

	UNREFERENCED_PARAMETER(hPrevInstance);
	UNREFERENCED_PARAMETER(nCmdShow);
	hWinMainInstance = hInstance;

	LPWSTR *argvW;												// Convert from UTF-16 argvW to ANSI argv.
	int argc;
	argvW = CommandLineToArgvW(GetCommandLineW(), &argc);		// Get full command line as argvW[argc] array of UTF-16 strings.
//for (int i=0; i<argc; i++) MessageBox(NULL, argvW[i], L"argvW[i]", MB_OK);
	wchar_t fullCmdLineW[DEFAULT_STRING] = L"";
	wcscpy_s(fullCmdLineW, argvW[0]);							// argvW[0] is the complete program path.
	wcscat_s(fullCmdLineW, L" ");								// Add space as parameter separator.
	wcscat_s(fullCmdLineW, lpCmdLine);							// lpCmdLine excludes complete program path, only contains parameters.
																// Adding to argvW[0] yields full command line as UTF-16 string.
//MessageBox(NULL, fullCmdLineW, L"fullCmdLineW", MB_OK);
	char fullCmdLineA[DEFAULT_STRING] = "";						// ANSI string.
	PSZ ptrfullCmdLineA = fullCmdLineA;
	ANSI_encode(fullCmdLineW, ptrfullCmdLineA);					// Encode UTF-16 command line into ANSI.
//MessageBoxA(NULL, fullCmdLineA, "fullCmdLineA", MB_OK);
	PCHAR* argv = CommandLineToArgvA(ptrfullCmdLineA, &argc);	// Separate ANSI command line into array of ANSI strings.
//for (int i=0; i<argc; i++) MessageBoxA(NULL, argv[i], "argv[i]", MB_OK);

	UCISERVERHANDLE sh = 0;
	if (UCI_SUCCESS != uciCreateServer(&sh, argc, argv)) {		// Here's why we converted UTF-16 argvW to ANSI argv; uciCreateServer needs ANSI or UTF-8.
		MessageBox(NULL, L"Error: Couldn't create UCI server.", L"Oops!", MB_OK);
		return 1;
	}

	if (UCI_SUCCESS != uciRegisterFunction(sh, "GetSerialNumber", GetSerialNumber)) {
		MessageBox(NULL, L"Error registering functions.", L"Oops!", MB_OK);
		return 1;
	}

	if (UCI_SUCCESS != uciHandleFuncCalls(sh)) {
		MessageBox(NULL, L"Error processing calls.", L"Oops!", MB_OK);
		return 1;
	}
	return 0;
}

void ANSI_encode(LPWSTR UTF16_In,		// Unicode input string
				 PSZ ANSI_Out) {		// ANSI output string

	int len_UTF16_In = lstrlenW( UTF16_In );
	int len_ANSI_Out = WideCharToMultiByte( CP_ACP,					// ANSI Code Page
											0,						// No special handling of unmapped chars
											UTF16_In,				// Wide-character string to be converted
											len_UTF16_In,			// length of wide-character string to be converted
											NULL, 0,				// No output buffer since we are calculating length
											NULL, NULL );			// Unrepresented char replacement - Use Default
	ANSI_Out[len_ANSI_Out] = '\0';
	WideCharToMultiByte(CP_ACP,					// ANSI Code Page
						0,						// No special handling of unmapped chars
						UTF16_In,				// Wide-character string to be converted
						len_UTF16_In,			// length of wide-character string to be converted
						ANSI_Out, len_ANSI_Out,	// Output string & length
						NULL, NULL );			// Unrepresented char replacement - Use Default
}

void GetSerialNumber(UCIPARMHANDLE ph) {
	DialogBox(hWinMainInstance, MAKEINTRESOURCE(IDD_SERIAL_NUMBER), hWnd, reinterpret_cast<DLGPROC>(DlgProc));
	char serial_number_A[DEFAULT_STRING] = "";					// ANSI serial_number.
	PSZ ptrserial_number_A = serial_number_A;
	ANSI_encode(serial_number, ptrserial_number_A);				// Encode UTF-16 serial_number into ANSI.
	UCIRESULT uciresult = uciSetCString(ph, 1, serial_number_A);
}

PCHAR* CommandLineToArgvA(PCHAR CmdLine, int* _argc) {
    PCHAR* argv;
    PCHAR  _argv;
    ULONG   len;
    ULONG   argc;
    CHAR   a;
    ULONG   i, j;

    BOOLEAN  in_QM;
    BOOLEAN  in_TEXT;
    BOOLEAN  in_SPACE;

    len = strlen(CmdLine);
    i = ((len+2)/2)*sizeof(PVOID) + sizeof(PVOID);

    argv = (PCHAR*)GlobalAlloc(GMEM_FIXED, i + (len+2)*sizeof(CHAR));
	_argv = (PCHAR)(((PUCHAR)argv)+i);

    argc = 0;
    argv[argc] = _argv;
    in_QM = FALSE;
    in_TEXT = FALSE;
    in_SPACE = TRUE;
    i = 0;
    j = 0;

    while( a = CmdLine[i] ) {
        if(in_QM) {
            if(a == '\"') {
                in_QM = FALSE;
            } else {
                _argv[j] = a;
                j++;
            }
        } else {
            switch(a) {
            case '\"':
                in_QM = TRUE;
                in_TEXT = TRUE;
                if(in_SPACE) {
                    argv[argc] = _argv+j;
                    argc++;
                }
                in_SPACE = FALSE;
                break;
            case ' ':
            case '\t':
            case '\n':
            case '\r':
                if(in_TEXT) {
                    _argv[j] = '\0';
                    j++;
                }
                in_TEXT = FALSE;
                in_SPACE = TRUE;
                break;
            default:
                in_TEXT = TRUE;
                if(in_SPACE) {
                    argv[argc] = _argv+j;
                    argc++;
                }
                _argv[j] = a;
                j++;
                in_SPACE = FALSE;
                break;
            }
        }
        i++;
    }
    _argv[j] = '\0';
    argv[argc] = NULL;

    (*_argc) = argc;
    return argv;
}

LRESULT CALLBACK DlgProc(HWND hWndDlg, UINT Msg, WPARAM wParam, LPARAM lParam) {
	switch(Msg)	{
	case WM_INITDIALOG:
		// Center the Dialog Box in the Agilent 3070's application window, rather than it's default position in the upper-left corner, which really looks quirky & odd.
		HWND hwndOwner;		if ((hwndOwner = GetParent(hWndDlg))== NULL)	hwndOwner = GetDesktopWindow();
		RECT rc, rcDlg, rcOwner;		GetWindowRect(hwndOwner, &rcOwner);		GetWindowRect(hWndDlg, &rcDlg);		CopyRect(&rc, &rcOwner);
		// Offset the owner and dialog box rectangles so that right and bottom values represent the width and height, and then offset the owner again to discard space taken up by the dialog box.
		OffsetRect(&rcDlg, -rcDlg.left, -rcDlg.top);		OffsetRect(&rc, -rc.left, -rc.top);		OffsetRect(&rc, -rcDlg.right, -rcDlg.bottom);
		// The new position is the sum of half the remaining space and the owner's original position; this centers the Dialog Box in the screen.
		SetWindowPos(hWndDlg, HWND_TOP, rcOwner.left + (rc.right / 2), rcOwner.top + (rc.bottom / 2), 0, 0, SWP_NOSIZE);

		wcscpy_s(serial_number, L"");                                   // Set old serial number to an empty string.
		SetDlgItemText(hWndDlg, IDC_EDIT, previous_serial_number);		// Initialize Dialog Box's text box with the previous Serial Number, so operator can re-test without re-scanning; just press Enter key.
		SetFocus(GetDlgItem(hWndDlg, IDC_EDIT));						// Set focus to the Diaglog Box's text box, so the operator doesn't have to mouse in and click inside.
		SendDlgItemMessage(hWndDlg, IDC_EDIT, EM_SETSEL, 0, -1L);		// Select the previous Serial Number text, so operator can scan new bar-code, automatically replacing old.
		return FALSE;													// False necessary to prevent Windows from setting focus to the default control, which is the IDC_OK button..
	case WM_COMMAND:
		switch(wParam) {
		case IDC_OK:
			GetDlgItemText(hWndDlg, IDC_EDIT, serial_number, DEFAULT_STRING);
			if (wcscmp(serial_number, L"") == 0) 
				break;
			else {
				wcscpy_s(previous_serial_number, serial_number);
				EndDialog(hWndDlg, 0);
				return TRUE;
			}
		case IDC_CANCEL:
			wcscpy_s(serial_number, L"Cancelled");
			EndDialog(hWndDlg, 0);
			return TRUE;
		}
		break;
	}
	return FALSE;
}
