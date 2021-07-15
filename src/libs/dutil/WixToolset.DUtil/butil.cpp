// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// Exit macros
#define ButilExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_BUTIL, p, x, e, s, __VA_ARGS__)
#define ButilExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_BUTIL, p, x, s, __VA_ARGS__)
#define ButilExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_BUTIL, p, x, e, s, __VA_ARGS__)
#define ButilExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_BUTIL, p, x, s, __VA_ARGS__)
#define ButilExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_BUTIL, e, x, s, __VA_ARGS__)

// constants
// From engine/registration.h
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE = L"BundleUpgradeCode";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY = L"BundleProviderKey";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_VARIABLE_KEY = L"variables";

// Forward declarations.
/********************************************************************
OpenBundleKey - Opens the bundle uninstallation key for a given bundle

NOTE: caller is responsible for closing key
********************************************************************/
static HRESULT OpenBundleKey(
    __in_z LPCWSTR wzBundleId,
    __in BUNDLE_INSTALL_CONTEXT context,
    __in_opt LPCWSTR szSubKey,
    __inout HKEY* key);

/********************************************************************
BundleGetBundleInfo - Read the registration data for a gven bundle
********************************************************************/
extern "C" HRESULT DAPI BundleGetBundleInfo(
  __in_z LPCWSTR wzBundleId,
  __in_z LPCWSTR wzAttribute,
  __out_ecount_opt(*pcchValueBuf) LPWSTR lpValueBuf,
  __inout_opt LPDWORD pcchValueBuf
  )
{
    Assert(wzBundleId && wzAttribute);

    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    HKEY hkBundle = NULL;
    DWORD cchSource = 0;
    DWORD dwType = 0;
    DWORD dwValue = 0;

    if ((lpValueBuf && !pcchValueBuf) || !wzBundleId || !wzAttribute)
    {
        ButilExitOnFailure(hr = E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    if (FAILED(hr = OpenBundleKey(wzBundleId, BUNDLE_INSTALL_CONTEXT_MACHINE, NULL, &hkBundle)) &&
        FAILED(hr = OpenBundleKey(wzBundleId, BUNDLE_INSTALL_CONTEXT_USER, NULL, &hkBundle)))
    {
        ButilExitOnFailure(E_FILENOTFOUND == hr ? HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) : hr, "Failed to locate bundle uninstall key path.");
    }

    // If the bundle doesn't have the property defined, return ERROR_UNKNOWN_PROPERTY
    hr = RegGetType(hkBundle, wzAttribute, &dwType);
    ButilExitOnFailure(E_FILENOTFOUND == hr ? HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY) : hr, "Failed to locate bundle property.");

    switch (dwType)
    {
        case REG_SZ:
            hr = RegReadString(hkBundle, wzAttribute, &sczValue);
            ButilExitOnFailure(hr, "Failed to read string property.");
            break;
        case REG_DWORD:
            hr = RegReadNumber(hkBundle, wzAttribute, &dwValue);
            ButilExitOnFailure(hr, "Failed to read dword property.");

            hr = StrAllocFormatted(&sczValue, L"%d", dwValue);
            ButilExitOnFailure(hr, "Failed to format dword property as string.");
            break;
        default:
            ButilExitOnFailure(hr = E_NOTIMPL, "Reading bundle info of type 0x%x not implemented.", dwType);

    }

    hr = ::StringCchLengthW(sczValue, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchSource));
    ButilExitOnFailure(hr, "Failed to calculate length of string");

    if (lpValueBuf)
    {
        // cchSource is the length of the string not including the terminating null character
        if (*pcchValueBuf <= cchSource)
        {
            *pcchValueBuf = ++cchSource;
            ButilExitOnFailure(hr = HRESULT_FROM_WIN32(ERROR_MORE_DATA), "A buffer is too small to hold the requested data.");
        }

        hr = ::StringCchCatNExW(lpValueBuf, *pcchValueBuf, sczValue, cchSource, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
        ButilExitOnFailure(hr, "Failed to copy the property value to the output buffer.");
        
        *pcchValueBuf = cchSource++;        
    }

LExit:
    ReleaseRegKey(hkBundle);
    ReleaseStr(sczValue);

    return hr;
}

/********************************************************************
********************************************************************/

extern "C" HRESULT DAPI BundleEnumRelatedBundle(
  __in_z LPCWSTR wzUpgradeCode,
  __in BUNDLE_INSTALL_CONTEXT context,
  __inout PDWORD pdwStartIndex,
  __out_ecount(MAX_GUID_CHARS+1) LPWSTR lpBundleIdBuf
    )
{
    HRESULT hr = S_OK;
    HKEY hkRoot = BUNDLE_INSTALL_CONTEXT_USER == context ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
    HKEY hkUninstall = NULL;
    HKEY hkBundle = NULL;
    LPWSTR sczUninstallSubKey = NULL;
    DWORD cchUninstallSubKey = 0;
    LPWSTR sczUninstallSubKeyPath = NULL;
    LPWSTR sczValue = NULL;
    DWORD dwType = 0;

    LPWSTR* rgsczBundleUpgradeCodes = NULL;
    DWORD cBundleUpgradeCodes = 0;
    BOOL fUpgradeCodeFound = FALSE;

    if (!wzUpgradeCode || !lpBundleIdBuf || !pdwStartIndex)
    {
        ButilExitOnFailure(hr = E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    hr = RegOpen(hkRoot, BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, KEY_READ, &hkUninstall);
    ButilExitOnFailure(hr, "Failed to open bundle uninstall key path.");

    for (DWORD dwIndex = *pdwStartIndex; !fUpgradeCodeFound; dwIndex++)
    {
        hr = RegKeyEnum(hkUninstall, dwIndex, &sczUninstallSubKey);
        ButilExitOnFailure(hr, "Failed to enumerate bundle uninstall key path.");

        hr = StrAllocFormatted(&sczUninstallSubKeyPath, L"%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, sczUninstallSubKey);
        ButilExitOnFailure(hr, "Failed to allocate bundle uninstall key path.");
        
        hr = RegOpen(hkRoot, sczUninstallSubKeyPath, KEY_READ, &hkBundle);
        ButilExitOnFailure(hr, "Failed to open uninstall key path.");

        // If it's a bundle, it should have a BundleUpgradeCode value of type REG_SZ (old) or REG_MULTI_SZ
        hr = RegGetType(hkBundle, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &dwType);
        if (FAILED(hr))
        {
            ReleaseRegKey(hkBundle);
            ReleaseNullStr(sczUninstallSubKey);
            ReleaseNullStr(sczUninstallSubKeyPath);
            // Not a bundle
            continue;
        }

        switch (dwType)
        {
            case REG_SZ:
                hr = RegReadString(hkBundle, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &sczValue);
                ButilExitOnFailure(hr, "Failed to read BundleUpgradeCode string property.");
                if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczValue, -1, wzUpgradeCode, -1))
                {
                    *pdwStartIndex = dwIndex;
                    fUpgradeCodeFound = TRUE;
                    break;
                }

                ReleaseNullStr(sczValue);

                break;
            case REG_MULTI_SZ:
                hr = RegReadStringArray(hkBundle, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &rgsczBundleUpgradeCodes, &cBundleUpgradeCodes);
                ButilExitOnFailure(hr, "Failed to read BundleUpgradeCode  multi-string property.");

                for (DWORD i = 0; i < cBundleUpgradeCodes; i++)
                {
                    LPWSTR wzBundleUpgradeCode = rgsczBundleUpgradeCodes[i];
                    if (wzBundleUpgradeCode && *wzBundleUpgradeCode)
                    {
                        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzBundleUpgradeCode, -1, wzUpgradeCode, -1))
                        {
                            *pdwStartIndex = dwIndex;
                            fUpgradeCodeFound = TRUE;
                            break;
                        }
                    }
                }
                ReleaseNullStrArray(rgsczBundleUpgradeCodes, cBundleUpgradeCodes);

                break;

            default:
                ButilExitOnFailure(hr = E_NOTIMPL, "BundleUpgradeCode of type 0x%x not implemented.", dwType);

        }

        if (fUpgradeCodeFound)
        {
            if (lpBundleIdBuf)
            {
                hr = ::StringCchLengthW(sczUninstallSubKey, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchUninstallSubKey));
                ButilExitOnFailure(hr, "Failed to calculate length of string");

                hr = ::StringCchCopyNExW(lpBundleIdBuf, MAX_GUID_CHARS + 1, sczUninstallSubKey, cchUninstallSubKey, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
                ButilExitOnFailure(hr, "Failed to copy the property value to the output buffer.");
            }

            break;
        }

        // Cleanup before next iteration
        ReleaseRegKey(hkBundle);
        ReleaseNullStr(sczUninstallSubKey);
        ReleaseNullStr(sczUninstallSubKeyPath);
    }

LExit:
    ReleaseStr(sczValue);
    ReleaseStr(sczUninstallSubKey);
    ReleaseStr(sczUninstallSubKeyPath);
    ReleaseRegKey(hkBundle);
    ReleaseRegKey(hkUninstall);
    ReleaseStrArray(rgsczBundleUpgradeCodes, cBundleUpgradeCodes);

    return hr;
}

/********************************************************************
BundleGetBundleVariable - Queries the bundle installation metadata for a given variable,
the caller is expected to free the memory returned vis psczValue
RETURNS:
S_OK
 Success, if the variable had a value, it's returned in psczValue
E_INVALIDARG
 An invalid parameter was passed to the function.
HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT)
 The bundle is not installed
HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY)
 The variable is unrecognized
E_NOTIMPL:
 Tried to read a bundle variable for a type which has not been implemented

All other returns are unexpected returns from other dutil methods.
********************************************************************/

extern "C" HRESULT DAPI BundleGetBundleVariable(
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzVariable,
    __deref_out_z LPWSTR * psczValue
)
{
    Assert(wzBundleId && wzVariable);

    HRESULT hr = S_OK;
    BUNDLE_INSTALL_CONTEXT context = BUNDLE_INSTALL_CONTEXT_MACHINE;
    HKEY hkBundle = NULL;
    DWORD dwType = 0;

    if (!wzBundleId || !wzVariable || !psczValue)
    {
        ButilExitOnFailure(hr = E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    if (FAILED(hr = OpenBundleKey(wzBundleId, context = BUNDLE_INSTALL_CONTEXT_MACHINE, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_VARIABLE_KEY, &hkBundle)) &&
        FAILED(hr = OpenBundleKey(wzBundleId, context = BUNDLE_INSTALL_CONTEXT_USER, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_VARIABLE_KEY, &hkBundle)))
    {
        ButilExitOnFailure(E_FILENOTFOUND == hr ? HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) : hr, "Failed to locate bundle uninstall key variable path.");
    }

    // If the bundle doesn't have the shared variable defined, return ERROR_UNKNOWN_PROPERTY
    hr = RegGetType(hkBundle, wzVariable, &dwType);
    ButilExitOnFailure(E_FILENOTFOUND == hr ? HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY) : hr, "Failed to locate bundle variable.");

    switch (dwType)
    {
    case REG_SZ:
        hr = RegReadString(hkBundle, wzVariable, psczValue);
        ButilExitOnFailure(hr, "Failed to read string shared variable.");
        break;
    case REG_NONE:
        hr = S_OK;
        break;
    default:
        ButilExitOnFailure(hr = E_NOTIMPL, "Reading bundle variable of type 0x%x not implemented.", dwType);

    }

LExit:
    ReleaseRegKey(hkBundle);

    return hr;

}
/********************************************************************
*
********************************************************************/
HRESULT OpenBundleKey(
    __in_z LPCWSTR wzBundleId,
    __in BUNDLE_INSTALL_CONTEXT context,
    __in_opt LPCWSTR szSubKey,
    __inout HKEY* key)
{
    Assert(key && wzBundleId);
    AssertSz(NULL == *key, "*key should be null");

    HRESULT hr = S_OK;
    HKEY hkRoot = BUNDLE_INSTALL_CONTEXT_USER == context ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
    LPWSTR sczKeypath = NULL;

    if (szSubKey)
    {
        hr = StrAllocFormatted(&sczKeypath, L"%ls\\%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, wzBundleId, szSubKey);
    }
    else
    {
        hr = StrAllocFormatted(&sczKeypath, L"%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, wzBundleId);
    }
    ButilExitOnFailure(hr, "Failed to allocate bundle uninstall key path.");

    hr = RegOpen(hkRoot, sczKeypath, KEY_READ, key);
    ButilExitOnFailure(hr, "Failed to open bundle uninstall key path.");

LExit:
    ReleaseStr(sczKeypath);

    return hr;
}

