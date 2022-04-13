/*
** UCIAPI.H - Header for users of the RCall Interface module.
*/

#ifndef __UCIAPI_H__
#define __UCIAPI_H__


#ifdef __cplusplus
extern "C" {
#endif

struct uciServerData;
struct uciCallData;

typedef struct uciServerData * UCISERVERHANDLE;
typedef struct uciCallData * UCIPARMHANDLE;

typedef int UCIRESULT;
typedef void (* UCIFUNCPTR)(UCIPARMHANDLE);

#define UCI_SUCCESS           0
#define UCI_EINTERNAL         1
#define UCI_EBADHANDLE        2
#define UCI_EMEMALLOC         3
#define UCI_ENAMELENGTH       4
#define UCI_ENULLPARM         5
#define UCI_EFUNCEXISTS       6
#define UCI_EPARMTYPE         7
#define UCI_ENETWORK          8
#define UCI_EBADPARMINDEX     9
#define UCI_EINITINFO        10
#define UCI_EALREADYERROR    11 /* Keep UCI_LASTERRORVALUE updated! */
#define UCI_LASTERRORVALUE   11

/* When a new result value is added, update uciDescribeResult() */


#define UCI_MAX_FUNCNAME_LEN 32

/*======================================================================*/
/* All info about any possible simple data type encoded in 32-bit word. */

#define UCI_DA_UNSIGNED                0x00000001
#define UCI_DA_FLOAT                   0x00000002
#define UCI_DA_TEXTSTRING              0x00000004
#define UCI_DA_ZEROTERM                0x00000008
#define UCI_DA_ISARRAY                 0x00000010

#define UCI_DA_BYTESFIELDOFFSET        16
#define UCI_DA_BYTESFIELD              0x00010000
#define UCI_DA_BYTESFIELDMASK          0x00FF0000

#define UCI_DA_DIMSFIELDOFFSET         24
#define UCI_DA_DIMSFIELD               0x01000000
#define UCI_DA_DIMSFIELDMASK           0x0F000000


#define UCI_TYPE_INT32 \
  (UCI_DA_BYTESFIELD * 4)
#define UCI_TYPE_REAL64 \
  (UCI_DA_FLOAT + UCI_DA_BYTESFIELD * 8)
#define UCI_TYPE_ZSTRING \
  (UCI_DA_TEXTSTRING + UCI_DA_BYTESFIELD * 1 + UCI_DA_ZEROTERM)
#define UCI_TYPE_REAL64_ARRAY1D \
  (UCI_DA_FLOAT + UCI_DA_BYTESFIELD * 8 + UCI_DA_ISARRAY + UCI_DA_DIMSFIELD * 1)

#define UCI_DA_GETITEMSIZE(t) \
  ( ((t) & UCI_DA_BYTESFIELDMASK) >> UCI_DA_BYTESFIELDOFFSET )

/*======================================================================*/

UCIRESULT uciCreateServer(UCISERVERHANDLE * phServer, int argc, char * argv[]);

UCIRESULT uciShutdownServer(UCISERVERHANDLE hServer);

UCIRESULT uciRegisterFunction(UCISERVERHANDLE hServer, const char * funcName,
  UCIFUNCPTR pFunction);

UCIRESULT uciHandleFuncCalls(UCISERVERHANDLE hServer);

UCIRESULT uciRaiseError(UCIPARMHANDLE hParms, const char * errorDesc);

UCIRESULT uciGetNumParms(UCIPARMHANDLE hParms, int * pNumParms);

UCIRESULT uciGetParmType(UCIPARMHANDLE hParms, int parmIndex, int * pParmType);

UCIRESULT uciGetReal64(UCIPARMHANDLE hParms, int parmIndex, double * pParmVal);

UCIRESULT uciSetReal64(UCIPARMHANDLE hParms, int parmIndex, double parmVal);

UCIRESULT uciGetCString(UCIPARMHANDLE hParms, int parmIndex,
  const char ** ppString);

UCIRESULT uciSetCString(UCIPARMHANDLE hParms, int parmIndex,
  const char * pString);

UCIRESULT uciGetArrayNumItems(UCIPARMHANDLE hParms, int parmIndex,
  int * pNumItems);

UCIRESULT uciGetArrayReadPtr(UCIPARMHANDLE hParms, int parmIndex,
  const void ** ppData);

UCIRESULT uciGetArrayWritePtr(UCIPARMHANDLE hParms, int parmIndex,
  void ** ppData);

const char * uciDescribeResult(UCIRESULT resultVal);

#ifdef __cplusplus
} /* extern "C" */
#endif


#endif /* ndef __UCIAPI_H__ */


