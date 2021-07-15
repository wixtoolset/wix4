#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

typedef enum BUNDLE_INSTALL_CONTEXT
{
    BUNDLE_INSTALL_CONTEXT_MACHINE,
    BUNDLE_INSTALL_CONTEXT_USER,
} BUNDLE_INSTALL_CONTEXT;


/********************************************************************
BundleGetBundleInfo - Queries the bundle installation metadata for a given property

RETURNS:
    E_INVALIDARG
        An invalid parameter was passed to the function.
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT)
        The bundle is not installed
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY)
        The property is unrecognized
    HRESULT_FROM_WIN32(ERROR_MORE_DATA)
        A buffer is too small to hold the requested data.
    E_NOTIMPL:
        Tried to read a bundle attribute for a type which has not been implemented

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
HRESULT DAPI BundleGetBundleInfo(
  __in_z LPCWSTR szBundleId,                             // Bundle code
  __in_z LPCWSTR szAttribute,                            // attribute name
  __out_ecount_opt(*pcchValueBuf) LPWSTR lpValueBuf,     // returned value, NULL if not desired
  __inout_opt                     LPDWORD pcchValueBuf   // in/out buffer character count
    );

/********************************************************************
BundleEnumRelatedBundle - Queries the bundle installation metadata for installs with the given upgrade code

NOTE: lpBundleIdBuff is a buffer to receive the bundle GUID. This buffer must be 39 characters long. 
        The first 38 characters are for the GUID, and the last character is for the terminating null character.
RETURNS:
    E_INVALIDARG
        An invalid parameter was passed to the function.

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
HRESULT DAPI BundleEnumRelatedBundle(
  __in_z   LPCWSTR lpUpgradeCode,
  __in     BUNDLE_INSTALL_CONTEXT context,
  __inout  PDWORD pdwStartIndex,
  __out_ecount(MAX_GUID_CHARS+1)  LPWSTR lpBundleIdBuf
    );

HRESULT DAPI BundleGetBundleVariable(
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzVariable,
    __deref_out_z LPWSTR* psczValue
);


#ifdef __cplusplus
}
#endif
