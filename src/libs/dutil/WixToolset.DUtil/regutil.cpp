// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define RegExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_REGUTIL, p, x, e, s, __VA_ARGS__)
#define RegExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_REGUTIL, p, x, s, __VA_ARGS__)
#define RegExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_REGUTIL, p, x, e, s, __VA_ARGS__)
#define RegExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_REGUTIL, p, x, s, __VA_ARGS__)
#define RegExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_REGUTIL, e, x, s, __VA_ARGS__)
#define RegExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_REGUTIL, g, x, s, __VA_ARGS__)

static PFN_REGCREATEKEYEXW vpfnRegCreateKeyExW = ::RegCreateKeyExW;
static PFN_REGOPENKEYEXW vpfnRegOpenKeyExW = ::RegOpenKeyExW;
static PFN_REGDELETEKEYEXW vpfnRegDeleteKeyExW = NULL;
static PFN_REGDELETEKEYEXW vpfnRegDeleteKeyExWFromLibrary = NULL;
static PFN_REGDELETEKEYW vpfnRegDeleteKeyW = ::RegDeleteKeyW;
static PFN_REGENUMKEYEXW vpfnRegEnumKeyExW = ::RegEnumKeyExW;
static PFN_REGENUMVALUEW vpfnRegEnumValueW = ::RegEnumValueW;
static PFN_REGQUERYINFOKEYW vpfnRegQueryInfoKeyW = ::RegQueryInfoKeyW;
static PFN_REGQUERYVALUEEXW vpfnRegQueryValueExW = ::RegQueryValueExW;
static PFN_REGSETVALUEEXW vpfnRegSetValueExW = ::RegSetValueExW;
static PFN_REGDELETEVALUEW vpfnRegDeleteValueW = ::RegDeleteValueW;

static HMODULE vhAdvApi32Dll = NULL;
static BOOL vfRegInitialized = FALSE;

static HRESULT WriteStringToRegistry(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD dwType
);

/********************************************************************
 RegInitialize - initializes regutil

*********************************************************************/
extern "C" HRESULT DAPI RegInitialize(
    )
{
    HRESULT hr = S_OK;

    hr = LoadSystemLibrary(L"AdvApi32.dll", &vhAdvApi32Dll);
    RegExitOnFailure(hr, "Failed to load AdvApi32.dll");

    // ignore failures - if this doesn't exist, we'll fall back to RegDeleteKeyW
    vpfnRegDeleteKeyExWFromLibrary = reinterpret_cast<PFN_REGDELETEKEYEXW>(::GetProcAddress(vhAdvApi32Dll, "RegDeleteKeyExW"));

    if (NULL == vpfnRegDeleteKeyExW)
    {
        vpfnRegDeleteKeyExW = vpfnRegDeleteKeyExWFromLibrary;
    }

    vfRegInitialized = TRUE;

LExit:
    return hr;
}


/********************************************************************
 RegUninitialize - uninitializes regutil

*********************************************************************/
extern "C" void DAPI RegUninitialize(
    )
{
    if (vhAdvApi32Dll)
    {
        ::FreeLibrary(vhAdvApi32Dll);
        vhAdvApi32Dll = NULL;
        vpfnRegDeleteKeyExWFromLibrary = NULL;
        vpfnRegDeleteKeyExW = NULL;
    }

    vfRegInitialized = FALSE;
}


/********************************************************************
 RegFunctionOverride - overrides the registry functions. Typically used
                       for unit testing.

*********************************************************************/
extern "C" void DAPI RegFunctionOverride(
    __in_opt PFN_REGCREATEKEYEXW pfnRegCreateKeyExW,
    __in_opt PFN_REGOPENKEYEXW pfnRegOpenKeyExW,
    __in_opt PFN_REGDELETEKEYEXW pfnRegDeleteKeyExW,
    __in_opt PFN_REGENUMKEYEXW pfnRegEnumKeyExW,
    __in_opt PFN_REGENUMVALUEW pfnRegEnumValueW,
    __in_opt PFN_REGQUERYINFOKEYW pfnRegQueryInfoKeyW,
    __in_opt PFN_REGQUERYVALUEEXW pfnRegQueryValueExW,
    __in_opt PFN_REGSETVALUEEXW pfnRegSetValueExW,
    __in_opt PFN_REGDELETEVALUEW pfnRegDeleteValueW
    )
{
    vpfnRegCreateKeyExW = pfnRegCreateKeyExW ? pfnRegCreateKeyExW : ::RegCreateKeyExW;
    vpfnRegOpenKeyExW = pfnRegOpenKeyExW ? pfnRegOpenKeyExW : ::RegOpenKeyExW;
    vpfnRegDeleteKeyExW = pfnRegDeleteKeyExW ? pfnRegDeleteKeyExW : vpfnRegDeleteKeyExWFromLibrary;
    vpfnRegEnumKeyExW = pfnRegEnumKeyExW ? pfnRegEnumKeyExW : ::RegEnumKeyExW;
    vpfnRegEnumValueW = pfnRegEnumValueW ? pfnRegEnumValueW : ::RegEnumValueW;
    vpfnRegQueryInfoKeyW = pfnRegQueryInfoKeyW ? pfnRegQueryInfoKeyW : ::RegQueryInfoKeyW;
    vpfnRegQueryValueExW = pfnRegQueryValueExW ? pfnRegQueryValueExW : ::RegQueryValueExW;
    vpfnRegSetValueExW = pfnRegSetValueExW ? pfnRegSetValueExW : ::RegSetValueExW;
    vpfnRegDeleteValueW = pfnRegDeleteValueW ? pfnRegDeleteValueW : ::RegDeleteValueW;
}


/********************************************************************
 RegCreate - creates a registry key.

*********************************************************************/
extern "C" HRESULT DAPI RegCreate(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in DWORD dwAccess,
    __out HKEY* phk
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegCreateKeyExW(hkRoot, wzSubKey, 0, NULL, REG_OPTION_NON_VOLATILE, dwAccess, NULL, phk, NULL);
    RegExitOnWin32Error(er, hr, "Failed to create registry key.");

LExit:
    return hr;
}


/********************************************************************
 RegCreate - creates a registry key with extra options.

*********************************************************************/
HRESULT DAPI RegCreateEx(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in DWORD dwAccess,
    __in BOOL fVolatile,
    __in_opt SECURITY_ATTRIBUTES* pSecurityAttributes,
    __out HKEY* phk,
    __out_opt BOOL* pfCreated
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwDisposition;

    er = vpfnRegCreateKeyExW(hkRoot, wzSubKey, 0, NULL, fVolatile ? REG_OPTION_VOLATILE : REG_OPTION_NON_VOLATILE, dwAccess, pSecurityAttributes, phk, &dwDisposition);
    RegExitOnWin32Error(er, hr, "Failed to create registry key.");

    if (pfCreated)
    {
        *pfCreated = (REG_CREATED_NEW_KEY == dwDisposition);
    }

LExit:
    return hr;
}


/********************************************************************
 RegOpen - opens a registry key.

*********************************************************************/
extern "C" HRESULT DAPI RegOpen(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in DWORD dwAccess,
    __out HKEY* phk
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegOpenKeyExW(hkRoot, wzSubKey, 0, dwAccess, phk);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to open registry key.");

LExit:
    return hr;
}


/********************************************************************
 RegDelete - deletes a registry key (and optionally it's whole tree).

*********************************************************************/
extern "C" HRESULT DAPI RegDelete(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in REG_KEY_BITNESS kbKeyBitness,
    __in BOOL fDeleteTree
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR pszEnumeratedSubKey = NULL;
    LPWSTR pszRecursiveSubKey = NULL;
    HKEY hkKey = NULL;
    REGSAM samDesired = 0;

    if (!vfRegInitialized && REG_KEY_DEFAULT != kbKeyBitness)
    {
        hr = E_INVALIDARG;
        RegExitOnFailure(hr, "RegInitialize must be called first in order to RegDelete() a key with non-default bit attributes!");
    }

    switch (kbKeyBitness)
    {
    case REG_KEY_32BIT:
        samDesired = KEY_WOW64_32KEY;
        break;
    case REG_KEY_64BIT:
        samDesired = KEY_WOW64_64KEY;
        break;
    case REG_KEY_DEFAULT:
        // Nothing to do
        break;
    }

    if (fDeleteTree)
    {
        hr = RegOpen(hkRoot, wzSubKey, KEY_READ | samDesired, &hkKey);
        if (E_FILENOTFOUND == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        RegExitOnFailure(hr, "Failed to open this key for enumerating subkeys: %ls", wzSubKey);

        // Yes, keep enumerating the 0th item, because we're deleting it every time
        while (E_NOMOREITEMS != (hr = RegKeyEnum(hkKey, 0, &pszEnumeratedSubKey)))
        {
            RegExitOnFailure(hr, "Failed to enumerate key 0");

            hr = PathConcat(wzSubKey, pszEnumeratedSubKey, &pszRecursiveSubKey);
            RegExitOnFailure(hr, "Failed to concatenate paths while recursively deleting subkeys. Path1: %ls, Path2: %ls", wzSubKey, pszEnumeratedSubKey);

            hr = RegDelete(hkRoot, pszRecursiveSubKey, kbKeyBitness, fDeleteTree);
            RegExitOnFailure(hr, "Failed to recursively delete subkey: %ls", pszRecursiveSubKey);
        }

        hr = S_OK;
    }

    if (NULL != vpfnRegDeleteKeyExW)
    {
        er = vpfnRegDeleteKeyExW(hkRoot, wzSubKey, samDesired, 0);
        if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
        {
            ExitFunction1(hr = E_FILENOTFOUND);
        }
        RegExitOnWin32Error(er, hr, "Failed to delete registry key (ex).");
    }
    else
    {
        er = vpfnRegDeleteKeyW(hkRoot, wzSubKey);
        if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
        {
            ExitFunction1(hr = E_FILENOTFOUND);
        }
        RegExitOnWin32Error(er, hr, "Failed to delete registry key.");
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(pszEnumeratedSubKey);
    ReleaseStr(pszRecursiveSubKey);

    return hr;
}


/********************************************************************
 RegKeyEnum - enumerates child registry keys.

*********************************************************************/
extern "C" HRESULT DAPI RegKeyEnum(
    __in HKEY hk,
    __in DWORD dwIndex,
    __deref_out_z LPWSTR* psczKey
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    SIZE_T cb = 0;
    DWORD cch = 0;

    if (psczKey && *psczKey)
    {
        hr = StrMaxLength(*psczKey, &cb);
        RegExitOnFailure(hr, "Failed to determine length of string.");

        cch = (DWORD)min(DWORD_MAX, cb);
    }

    if (2 > cch)
    {
        cch = 2;

        hr = StrAlloc(psczKey, cch);
        RegExitOnFailure(hr, "Failed to allocate string to minimum size.");
    }

    er = vpfnRegEnumKeyExW(hk, dwIndex, *psczKey, &cch, NULL, NULL, NULL, NULL);
    if (ERROR_MORE_DATA == er)
    {
        er = vpfnRegQueryInfoKeyW(hk, NULL, NULL, NULL, NULL, &cch, NULL, NULL, NULL, NULL, NULL, NULL);
        RegExitOnWin32Error(er, hr, "Failed to get max size of subkey name under registry key.");

        ++cch; // add one because RegQueryInfoKeyW() returns the length of the subkeys without the null terminator.
        hr = StrAlloc(psczKey, cch);
        RegExitOnFailure(hr, "Failed to allocate string bigger for enum registry key.");

        er = vpfnRegEnumKeyExW(hk, dwIndex, *psczKey, &cch, NULL, NULL, NULL, NULL);
    }
    else if (ERROR_NO_MORE_ITEMS == er)
    {
        ExitFunction1(hr = E_NOMOREITEMS);
    }
    RegExitOnWin32Error(er, hr, "Failed to enum registry key.");

    // Always ensure the registry key name is null terminated.
#pragma prefast(push)
#pragma prefast(disable:26018)
    (*psczKey)[cch] = L'\0'; // note that cch will always be one less than the size of the buffer because that's how RegEnumKeyExW() works.
#pragma prefast(pop)

LExit:
    return hr;
}


/********************************************************************
 RegValueEnum - enumerates registry values.

*********************************************************************/
HRESULT DAPI RegValueEnum(
    __in HKEY hk,
    __in DWORD dwIndex,
    __deref_out_z LPWSTR* psczName,
    __out_opt DWORD *pdwType
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD cbValueName = 0;

    er = vpfnRegQueryInfoKeyW(hk, NULL, NULL, NULL, NULL, NULL, NULL, NULL, &cbValueName, NULL, NULL, NULL);
    RegExitOnWin32Error(er, hr, "Failed to get max size of value name under registry key.");

    // Add one for null terminator
    ++cbValueName;

    hr = StrAlloc(psczName, cbValueName);
    RegExitOnFailure(hr, "Failed to allocate array for registry value name");

    er = vpfnRegEnumValueW(hk, dwIndex, *psczName, &cbValueName, NULL, pdwType, NULL, NULL);
    if (ERROR_NO_MORE_ITEMS == er)
    {
        ExitFunction1(hr = E_NOMOREITEMS);
    }
    RegExitOnWin32Error(er, hr, "Failed to enumerate registry value");

LExit:
    return hr;
}

/********************************************************************
 RegGetType - reads a registry key value type.
 *********************************************************************/
HRESULT DAPI RegGetType(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __out DWORD *pdwType
     )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegQueryValueExW(hk, wzName, NULL, pdwType, NULL, NULL);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to read registry value.");
LExit:

    return hr;
}

/********************************************************************
 RegReadBinary - reads a registry key binary value.
 NOTE: caller is responsible for freeing *ppbBuffer
*********************************************************************/
HRESULT DAPI RegReadBinary(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __deref_out_bcount_opt(*pcbBuffer) BYTE** ppbBuffer,
    __out SIZE_T *pcbBuffer
     )
{
    HRESULT hr = S_OK;
    LPBYTE pbBuffer = NULL;
    DWORD er = ERROR_SUCCESS;
    DWORD cb = 0;
    DWORD dwType = 0;

    er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, NULL, &cb);
    RegExitOnWin32Error(er, hr, "Failed to get size of registry value.");

    // Zero-length binary values can exist
    if (0 < cb)
    {
        pbBuffer = static_cast<LPBYTE>(MemAlloc(cb, FALSE));
        RegExitOnNull(pbBuffer, hr, E_OUTOFMEMORY, "Failed to allocate buffer for binary registry value.");

        er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, pbBuffer, &cb);
        if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
        {
            ExitFunction1(hr = E_FILENOTFOUND);
        }
        RegExitOnWin32Error(er, hr, "Failed to read registry value.");
    }

    if (REG_BINARY == dwType)
    {
        *ppbBuffer = pbBuffer;
        pbBuffer = NULL;
        *pcbBuffer = cb;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Error reading binary registry value due to unexpected data type: %u", dwType);
    }

LExit:
    ReleaseMem(pbBuffer);

    return hr;
}


/********************************************************************
 RegReadString - reads a registry key value as a string.

*********************************************************************/
extern "C" HRESULT DAPI RegReadString(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __deref_out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    SIZE_T cbValue = 0;
    DWORD cch = 0;
    DWORD cb = 0;
    DWORD dwType = 0;
    LPWSTR sczExpand = NULL;

    if (psczValue && *psczValue)
    {
        hr = StrMaxLength(*psczValue, &cbValue);
        RegExitOnFailure(hr, "Failed to determine length of string.");

        cch = (DWORD)min(DWORD_MAX, cbValue);
    }

    if (2 > cch)
    {
        cch = 2;

        hr = StrAlloc(psczValue, cch);
        RegExitOnFailure(hr, "Failed to allocate string to minimum size.");
    }

    cb = sizeof(WCHAR) * (cch - 1); // subtract one to ensure there will be a space at the end of the string for the null terminator.
    er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, reinterpret_cast<LPBYTE>(*psczValue), &cb);
    if (ERROR_MORE_DATA == er)
    {
        cch = cb / sizeof(WCHAR) + 1; // add one to ensure there will be space at the end for the null terminator
        hr = StrAlloc(psczValue, cch);
        RegExitOnFailure(hr, "Failed to allocate string bigger for registry value.");

        er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, reinterpret_cast<LPBYTE>(*psczValue), &cb);
    }
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to read registry key.");

    if (REG_SZ == dwType || REG_EXPAND_SZ == dwType)
    {
        // Always ensure the registry value is null terminated.
        (*psczValue)[cch - 1] = L'\0';

        if (REG_EXPAND_SZ == dwType)
        {
            hr = StrAllocString(&sczExpand, *psczValue, 0);
            RegExitOnFailure(hr, "Failed to copy registry value to expand.");

            hr = PathExpand(psczValue, sczExpand, PATH_EXPAND_ENVIRONMENT);
            RegExitOnFailure(hr, "Failed to expand registry value: %ls", *psczValue);
        }
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Error reading string registry value due to unexpected data type: %u", dwType);
    }

LExit:
    ReleaseStr(sczExpand);

    return hr;
}


/********************************************************************
 RegReadStringArray - reads a registry key value REG_MULTI_SZ value as a string array.

*********************************************************************/
HRESULT DAPI RegReadStringArray(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __deref_out_ecount_opt(*pcStrings) LPWSTR** prgsczStrings,
    __out DWORD *pcStrings
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwNullCharacters = 0;
    DWORD dwType = 0;
    DWORD cb = 0;
    DWORD cch = 0;
    LPCWSTR wzSource = NULL;
    LPWSTR sczValue = NULL;

    er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, reinterpret_cast<LPBYTE>(sczValue), &cb);
    if (0 < cb)
    {
        cch = cb / sizeof(WCHAR);
        hr = StrAlloc(&sczValue, cch);
        RegExitOnFailure(hr, "Failed to allocate string for registry value.");

        er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, reinterpret_cast<LPBYTE>(sczValue), &cb);
    }
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to read registry key.");

    if (cb / sizeof(WCHAR) != cch)
    {
        hr = E_UNEXPECTED;
        RegExitOnFailure(hr, "The size of registry value %ls unexpected changed between 2 reads", wzName);
    }

    if (REG_MULTI_SZ != dwType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Tried to read string array, but registry value %ls is of an incorrect type", wzName);
    }

    // Value exists, but is empty, so no strings to return.
    if (2 > cch)
    {
        *prgsczStrings = NULL;
        *pcStrings = 0;
        ExitFunction1(hr = S_OK);
    }

    // The docs specifically say if the value was written without double-null-termination, it'll get read back without it too.
    if (L'\0' != sczValue[cch-1] || L'\0' != sczValue[cch-2])
    {
        hr = E_INVALIDARG;
        RegExitOnFailure(hr, "Tried to read string array, but registry value %ls is invalid (isn't double-null-terminated)", wzName);
    }

    cch = cb / sizeof(WCHAR);
    for (DWORD i = 0; i < cch; ++i)
    {
        if (L'\0' == sczValue[i])
        {
            ++dwNullCharacters;
        }
    }

    // There's one string for every null character encountered (except the extra 1 at the end of the string)
    *pcStrings = dwNullCharacters - 1;
    hr = MemEnsureArraySize(reinterpret_cast<LPVOID *>(prgsczStrings), *pcStrings, sizeof(LPWSTR), 0);
    RegExitOnFailure(hr, "Failed to resize array while reading REG_MULTI_SZ value");

#pragma prefast(push)
#pragma prefast(disable:26010)
    wzSource = sczValue;
    for (DWORD i = 0; i < *pcStrings; ++i)
    {
        hr = StrAllocString(&(*prgsczStrings)[i], wzSource, 0);
        RegExitOnFailure(hr, "Failed to allocate copy of string");

        // Skip past this string
        wzSource += lstrlenW(wzSource) + 1;
    }
#pragma prefast(pop)

LExit:
    ReleaseStr(sczValue);

    return hr;
}


/********************************************************************
 RegReadVersion - reads a registry key value as a version.

*********************************************************************/
extern "C" HRESULT DAPI RegReadVersion(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __out DWORD64* pdw64Version
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwType = 0;
    DWORD cb = 0;
    LPWSTR sczVersion = NULL;

    cb = sizeof(DWORD64);
    er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, reinterpret_cast<LPBYTE>(*pdw64Version), &cb);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    if (REG_SZ == dwType || REG_EXPAND_SZ == dwType)
    {
        hr = RegReadString(hk, wzName, &sczVersion);
        RegExitOnFailure(hr, "Failed to read registry version as string.");

        hr = FileVersionFromStringEx(sczVersion, 0, pdw64Version);
        RegExitOnFailure(hr, "Failed to convert registry string to version.");
    }
    else if (REG_QWORD == dwType)
    {
        RegExitOnWin32Error(er, hr, "Failed to read registry key.");
    }
    else // unexpected data type
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Error reading version registry value due to unexpected data type: %u", dwType);
    }

LExit:
    ReleaseStr(sczVersion);

    return hr;
}

/********************************************************************
 RegReadNone - reads a NONE registry key value.

*********************************************************************/
extern "C" HRESULT DAPI RegReadNone(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwType = 0;

    er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, NULL, NULL);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to query registry key value.");

    if (REG_NONE != dwType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Error reading version registry value due to unexpected data type: %u", dwType);
    }

LExit:
    return hr;
}

/********************************************************************
 RegReadNumber - reads a DWORD registry key value as a number.

*********************************************************************/
extern "C" HRESULT DAPI RegReadNumber(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __out DWORD* pdwValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwType = 0;
    DWORD cb = sizeof(DWORD);

    er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, reinterpret_cast<LPBYTE>(pdwValue), &cb);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to query registry key value.");

    if (REG_DWORD != dwType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Error reading version registry value due to unexpected data type: %u", dwType);
    }

LExit:
    return hr;
}


/********************************************************************
 RegReadQword - reads a QWORD registry key value as a number.

*********************************************************************/
extern "C" HRESULT DAPI RegReadQword(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __out DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwType = 0;
    DWORD cb = sizeof(DWORD64);

    er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, reinterpret_cast<LPBYTE>(pqwValue), &cb);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to query registry key value.");

    if (REG_QWORD != dwType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Error reading version registry value due to unexpected data type: %u", dwType);
    }

LExit:
    return hr;
}


/********************************************************************
 RegWriteBinary - writes a registry key value as a binary.

*********************************************************************/
HRESULT DAPI RegWriteBinary(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_bcount(cbBuffer) const BYTE *pbBuffer,
    __in DWORD cbBuffer
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_BINARY, pbBuffer, cbBuffer);
    RegExitOnWin32Error(er, hr, "Failed to write binary registry value with name: %ls", wzName);

LExit:
    return hr;
}


/********************************************************************
RegWriteExpandString - writes a registry key value as an expand string.

Note: if wzValue is NULL the value will be removed.
*********************************************************************/
extern "C" HRESULT DAPI RegWriteExpandString(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_z_opt LPCWSTR wzValue
)
{
    return WriteStringToRegistry(hk, wzName, wzValue, REG_EXPAND_SZ);
}


/********************************************************************
 RegWriteString - writes a registry key value as a string.

 Note: if wzValue is NULL the value will be removed.
*********************************************************************/
extern "C" HRESULT DAPI RegWriteString(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_z_opt LPCWSTR wzValue
    )
{
    return WriteStringToRegistry(hk, wzName, wzValue, REG_SZ);
}


/********************************************************************
 RegWriteStringFormatted - writes a registry key value as a formatted string.

*********************************************************************/
extern "C" HRESULT DAPI RegWriteStringFormatted(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in __format_string LPCWSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    va_list args;

    va_start(args, szFormat);
    hr = StrAllocFormattedArgs(&sczValue, szFormat, args);
    va_end(args);
    RegExitOnFailure(hr, "Failed to allocate %ls value.", wzName);

    hr = WriteStringToRegistry(hk, wzName, sczValue, REG_SZ);

LExit:
    ReleaseStr(sczValue);

    return hr;
}


/********************************************************************
 RegWriteStringArray - writes an array of strings as a REG_MULTI_SZ value

*********************************************************************/
HRESULT DAPI RegWriteStringArray(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_ecount(cValues) LPWSTR *rgwzValues,
    __in DWORD cValues
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR wzCopyDestination = NULL;
    LPCWSTR wzWriteValue = NULL;
    LPWSTR sczWriteValue = NULL;
    DWORD dwTotalStringSize = 0;
    DWORD cbTotalStringSize = 0;
    DWORD dwTemp = 0;

    if (0 == cValues)
    {
        wzWriteValue = L"\0";
    }
    else
    {
        // Add space for the null terminator
        dwTotalStringSize = 1;

        for (DWORD i = 0; i < cValues; ++i)
        {
            dwTemp = dwTotalStringSize;
            hr = ::DWordAdd(dwTemp, 1 + lstrlenW(rgwzValues[i]), &dwTotalStringSize);
            RegExitOnFailure(hr, "DWORD Overflow while adding length of string to write REG_MULTI_SZ");
        }

        hr = StrAlloc(&sczWriteValue, dwTotalStringSize);
        RegExitOnFailure(hr, "Failed to allocate space for string while writing REG_MULTI_SZ");

        wzCopyDestination = sczWriteValue;
        dwTemp = dwTotalStringSize;
        for (DWORD i = 0; i < cValues; ++i)
        {
            hr = ::StringCchCopyW(wzCopyDestination, dwTotalStringSize, rgwzValues[i]);
            RegExitOnFailure(hr, "failed to copy string: %ls", rgwzValues[i]);

            dwTemp -= lstrlenW(rgwzValues[i]) + 1;
            wzCopyDestination += lstrlenW(rgwzValues[i]) + 1;
        }

        wzWriteValue = sczWriteValue;
    }

    hr = ::DWordMult(dwTotalStringSize, sizeof(WCHAR), &cbTotalStringSize);
    RegExitOnFailure(hr, "Failed to get total string size in bytes");

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_MULTI_SZ, reinterpret_cast<const BYTE *>(wzWriteValue), cbTotalStringSize);
    RegExitOnWin32Error(er, hr, "Failed to set registry value to array of strings (first string of which is): %ls", wzWriteValue);

LExit:
    ReleaseStr(sczWriteValue);

    return hr;
}

/********************************************************************
 RegWriteNone - writes a registry key value as none.

*********************************************************************/
extern "C" HRESULT DAPI RegWriteNone(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_NONE, NULL, NULL);
    RegExitOnWin32Error(er, hr, "Failed to set %ls value.", wzName);

LExit:
    return hr;
}

/********************************************************************
 RegWriteNumber - writes a registry key value as a number.

*********************************************************************/
extern "C" HRESULT DAPI RegWriteNumber(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in DWORD dwValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_DWORD, reinterpret_cast<const BYTE *>(&dwValue), sizeof(dwValue));
    RegExitOnWin32Error(er, hr, "Failed to set %ls value.", wzName);

LExit:
    return hr;
}

/********************************************************************
 RegWriteQword - writes a registry key value as a Qword.

*********************************************************************/
extern "C" HRESULT DAPI RegWriteQword(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_QWORD, reinterpret_cast<const BYTE *>(&qwValue), sizeof(qwValue));
    RegExitOnWin32Error(er, hr, "Failed to set %ls value.", wzName);

LExit:
    return hr;
}

/********************************************************************
 RegQueryKey - queries the key for the number of subkeys and values.

*********************************************************************/
extern "C" HRESULT DAPI RegQueryKey(
    __in HKEY hk,
    __out_opt DWORD* pcSubKeys,
    __out_opt DWORD* pcValues
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegQueryInfoKeyW(hk, NULL, NULL, NULL, pcSubKeys, NULL, NULL, pcValues, NULL, NULL, NULL, NULL);
    RegExitOnWin32Error(er, hr, "Failed to get the number of subkeys and values under registry key.");

LExit:
    return hr;
}

/********************************************************************
RegKeyReadNumber - reads a DWORD registry key value as a number from
a specified subkey.

*********************************************************************/
extern "C" HRESULT DAPI RegKeyReadNumber(
    __in HKEY hk,
    __in_z LPCWSTR wzSubKey,
    __in_z_opt LPCWSTR wzName,
    __in BOOL f64Bit,
    __out DWORD* pdwValue
    )
{
    HRESULT hr = S_OK;
    HKEY hkKey = NULL;

    hr = RegOpen(hk, wzSubKey, KEY_READ | f64Bit ? KEY_WOW64_64KEY : 0, &hkKey);
    RegExitOnFailure(hr, "Failed to open key: %ls", wzSubKey);

    hr = RegReadNumber(hkKey, wzName, pdwValue);
    RegExitOnFailure(hr, "Failed to read value: %ls/@%ls", wzSubKey, wzName);

LExit:
    ReleaseRegKey(hkKey);

    return hr;
}

/********************************************************************
RegValueExists - determines whether a named value exists in a 
specified subkey.

*********************************************************************/
extern "C" BOOL DAPI RegValueExists(
    __in HKEY hk,
    __in_z LPCWSTR wzSubKey,
    __in_z_opt LPCWSTR wzName,
    __in BOOL f64Bit
    )
{
    HRESULT hr = S_OK;
    HKEY hkKey = NULL;
    DWORD dwType = 0;

    hr = RegOpen(hk, wzSubKey, KEY_READ | f64Bit ? KEY_WOW64_64KEY : 0, &hkKey);
    RegExitOnFailure(hr, "Failed to open key: %ls", wzSubKey);

    hr = RegGetType(hkKey, wzName, &dwType);
    RegExitOnFailure(hr, "Failed to read value type: %ls/@%ls", wzSubKey, wzName);

LExit:
    ReleaseRegKey(hkKey);

    return SUCCEEDED(hr);
}

static HRESULT WriteStringToRegistry(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD dwType
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    size_t cbValue = 0;

    if (wzValue)
    {
        hr = ::StringCbLengthW(wzValue, STRSAFE_MAX_CCH * sizeof(TCHAR), &cbValue);
        RegExitOnFailure(hr, "Failed to determine length of registry value: %ls", wzName);

        er = vpfnRegSetValueExW(hk, wzName, 0, dwType, reinterpret_cast<const BYTE *>(wzValue), static_cast<DWORD>(cbValue));
        RegExitOnWin32Error(er, hr, "Failed to set registry value: %ls", wzName);
    }
    else
    {
        er = vpfnRegDeleteValueW(hk, wzName);
        if (ERROR_FILE_NOT_FOUND == er || ERROR_PATH_NOT_FOUND == er)
        {
            er = ERROR_SUCCESS;
        }
        RegExitOnWin32Error(er, hr, "Failed to delete registry value: %ls", wzName);
    }

LExit:
    return hr;
}
