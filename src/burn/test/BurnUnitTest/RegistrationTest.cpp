// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


#define ROOT_PATH L"SOFTWARE\\WiX_Burn_UnitTest"
#define HKLM_PATH L"SOFTWARE\\WiX_Burn_UnitTest\\HKLM"
#define HKCU_PATH L"SOFTWARE\\WiX_Burn_UnitTest\\HKCU"
#define REGISTRY_UNINSTALL_KEY L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
#define REGISTRY_RUN_KEY L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"
#define TEST_BUNDLE_ID L"{D54F896D-1952-43e6-9C67-B5652240618C}"
#define TEST_BUNDLE_UPGRADE_CODE L"{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}"

#define TEST_UNINSTALL_KEY L"HKEY_CURRENT_USER\\" HKCU_PATH L"\\" REGISTRY_UNINSTALL_KEY L"\\{D54F896D-1952-43e6-9C67-B5652240618C}"
#define TEST_RUN_KEY L"HKEY_CURRENT_USER\\" HKCU_PATH L"\\" REGISTRY_RUN_KEY
#define TEST_VARIABLE_KEY L"HKEY_CURRENT_USER\\" HKCU_PATH L"\\" REGISTRY_UNINSTALL_KEY L"\\{D54F896D-1952-43e6-9C67-B5652240618C}\\variables"


static LSTATUS APIENTRY RegistrationTest_RegCreateKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __reserved DWORD Reserved,
    __in_opt LPWSTR lpClass,
    __in DWORD dwOptions,
    __in REGSAM samDesired,
    __in_opt CONST LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    __out PHKEY phkResult,
    __out_opt LPDWORD lpdwDisposition
    );
static LSTATUS APIENTRY RegistrationTest_RegOpenKeyExW(
    __in HKEY hKey,
    __in_opt LPCWSTR lpSubKey,
    __reserved DWORD ulOptions,
    __in REGSAM samDesired,
    __out PHKEY phkResult
    );
static LSTATUS APIENTRY RegistrationTest_RegDeleteKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __in REGSAM samDesired,
    __reserved DWORD Reserved
    );

namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{
    using namespace Microsoft::Win32;
    using namespace System;
    using namespace System::IO;
    using namespace Xunit;

    public ref class RegistrationTest : BurnUnitTest
    {
    public:
        RegistrationTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact]
        void RegisterBasicTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));

            try
            {
                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43e6-9C67-B5652240618C}' UpgradeCode='{D54F896D-1952-43e6-9C67-B5652240618C}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='RegisterBasicTest' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                Assert::True(Directory::Exists(cacheDirectory));
                Assert::True(File::Exists(Path::Combine(cacheDirectory, gcnew String(L"setup.exe"))));

                Assert::Equal(Int32(BURN_RESUME_MODE_ACTIVE), (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal<String^>(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.runonce"), (String^)(Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr)));

                // end session
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                Assert::False(Directory::Exists(cacheDirectory));

                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

        [Fact]
        void RegisterArpMinimumTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));
            try
            {
                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43e6-9C67-B5652240618C}' UpgradeCode='{D54F896D-1952-43e6-9C67-B5652240618C}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='Product1' InProgressDisplayName='Product1 Installation' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                //
                // install
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                Assert::Equal<String^>(gcnew String(L"Product1 Installation"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"DisplayName"), nullptr));
                Assert::Equal(Int32(BURN_RESUME_MODE_ACTIVE), (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal<String^>(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.runonce"), (String^)Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // complete registration
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_ARP, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was updated
                Assert::Equal(Int32(BURN_RESUME_MODE_ARP), (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal(1, (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                //
                // uninstall
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, 0, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was updated
                Assert::Equal(Int32(BURN_RESUME_MODE_ACTIVE), (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal(1, (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::Equal<String^>(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.runonce"), (String^)Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // delete registration
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

        [Fact]
        void RegisterVariablesTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));
            try
            {
                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43e6-9C67-B5652240618C}' UpgradeCode='{D54F896D-1952-43e6-9C67-B5652240618C}' Tag='foo' ProviderKey='bar' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='Product1' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                //
                // install
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                Assert::Equal(Int32(BURN_RESUME_MODE_ACTIVE), (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal<String^>(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.runonce"), (String^)Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // complete registration
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_ARP, BOOTSTRAPPER_APPLY_RESTART_REQUIRED, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_FULL);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration variables were updated
                Assert::Equal<String^>(gcnew String(L"Product1"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"DisplayName"), nullptr));
                registration.fInstalled = TRUE;

                hr = RegistrationSetVariables(&registration, &variables);
                TestThrowOnFailure(hr, L"Failed to set registration variables.");

                Assert::Equal(1ll, VariableGetNumericHelper(&variables, BURN_BUNDLE_INSTALLED));
                Assert::Equal(1ll, VariableGetNumericHelper(&variables, BURN_REBOOT_PENDING));
                Assert::Equal<String^>(gcnew String(L"foo"), VariableGetStringHelper(&variables, BURN_BUNDLE_TAG));
                Assert::Equal<String^>(gcnew String(L"bar"), VariableGetStringHelper(&variables, BURN_BUNDLE_PROVIDER_KEY));
                Assert::Equal<String^>(gcnew String(L"1.0.0.0"), VariableGetVersionHelper(&variables, BURN_BUNDLE_VERSION));

                //
                // uninstall
                //

                // delete registration
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

        [Fact]
        void RegisterArpFullTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));
            try
            {
                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX UxDllPayloadId='ux.dll'>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43e6-9C67-B5652240618C}' UpgradeCode='{D54F896D-1952-43e6-9C67-B5652240618C}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' DisplayName='DisplayName1' DisplayVersion='1.2.3.4' Publisher='Publisher1' HelpLink='http://www.microsoft.com/help'"
                    L"             HelpTelephone='555-555-5555' AboutUrl='http://www.microsoft.com/about' UpdateUrl='http://www.microsoft.com/update'"
                    L"             Comments='Comments1' Contact='Contact1' DisableModify='yes' DisableRemove='yes' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                //
                // install
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                Assert::Equal(Int32(BURN_RESUME_MODE_ACTIVE), (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal<String^>(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.runonce"), (String^)Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // finish registration
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_ARP, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_FULL);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was updated
                Assert::Equal(Int32(BURN_RESUME_MODE_ARP), (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal(1, (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                Assert::Equal<String^>(gcnew String(L"DisplayName1"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"DisplayName"), nullptr));
                Assert::Equal<String^>(gcnew String(L"1.2.3.4"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"DisplayVersion"), nullptr));
                Assert::Equal<String^>(gcnew String(L"Publisher1"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Publisher"), nullptr));
                Assert::Equal<String^>(gcnew String(L"http://www.microsoft.com/help"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"HelpLink"), nullptr));
                Assert::Equal<String^>(gcnew String(L"555-555-5555"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"HelpTelephone"), nullptr));
                Assert::Equal<String^>(gcnew String(L"http://www.microsoft.com/about"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"URLInfoAbout"), nullptr));
                Assert::Equal<String^>(gcnew String(L"http://www.microsoft.com/update"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"URLUpdateInfo"), nullptr));
                Assert::Equal<String^>(gcnew String(L"Comments1"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Comments"), nullptr));
                Assert::Equal<String^>(gcnew String(L"Contact1"), (String^)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Contact"), nullptr));
                Assert::Equal(1, (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"NoModify"), nullptr));
                Assert::Equal(1, (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"NoRemove"), nullptr));

                //
                // uninstall
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, 0, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was updated
                Assert::Equal(Int32(BURN_RESUME_MODE_ACTIVE), (Int32)Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal<String^>(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.runonce"), (String^)Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // delete registration
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }
            
                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

        [Fact]
        void DUtilButilTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            LPWSTR sczValue = NULL;
            LPWSTR sczRelatedBundleId = NULL;
            DWORD dwRelatedBundleIndex = 0;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            BYTE* pbBuffer = NULL;
            SIZE_T cbBuffer = 0;
            
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));
            try
            {
                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <RelatedBundle Id='" TEST_BUNDLE_UPGRADE_CODE "' Action='Upgrade' />"
                    L"    <Registration Id='" TEST_BUNDLE_ID "' Tag='foo' ProviderKey='" TEST_BUNDLE_ID "' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='RegisterBasicTest' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"    <Variable Id='MyBurnVariable1' Type='numeric' Value='0' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable2' Type='string' Value='foo' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable3' Type='version' Value='v1.1-alpha' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable4' Type='string' Value='foo' Hidden='no' Persisted='no' />"
                    L"    <CommandLine Variables='upperCase' />"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = VariablesParseFromXml(&variables, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse variables from XML.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                // begin session
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                VariableSetNumericHelper(&variables, L"MyBurnVariable1", 42);
                VariableSetStringHelper(&variables, L"MyBurnVariable2", L"bar", FALSE);
                VariableSetVersionHelper(&variables, L"MyBurnVariable3", L"v1.0-beta");

                hr = VariableSerialize(&variables, TRUE, &pbBuffer, &cbBuffer);
                TestThrowOnFailure(hr, "Failed to serialize variables.");

                if (!Directory::Exists(cacheDirectory))
                {
                    Directory::CreateDirectory(cacheDirectory);
                }

                hr = RegistrationSaveState(&registration, pbBuffer, cbBuffer);
                TestThrowOnFailure(hr, L"Failed to save state.");

                ReleaseNullBuffer(pbBuffer);
                cbBuffer = 0;
                // Verify the variables exist
                Assert::Equal<String^>(gcnew String(L"42"), (String^)Registry::GetValue(gcnew String(TEST_VARIABLE_KEY), gcnew String(L"MyBurnVariable1"), nullptr));
                Assert::Equal<String^>(gcnew String(L"bar"), (String^)Registry::GetValue(gcnew String(TEST_VARIABLE_KEY), gcnew String(L"MyBurnVariable2"), nullptr));
                Assert::Equal<String^>(gcnew String(L"1.0-beta"), (String^)Registry::GetValue(gcnew String(TEST_VARIABLE_KEY), gcnew String(L"MyBurnVariable3"), nullptr));
                Assert::Empty((System::Collections::IEnumerable ^)Registry::GetValue(gcnew String(TEST_VARIABLE_KEY), gcnew String(L"WixBundleForcedRestartPackage"), nullptr));

                hr = StrAlloc(&sczRelatedBundleId, MAX_GUID_CHARS + 1);

                // Verify we can find ourself via the UpgradeCode
                hr = BundleEnumRelatedBundle(TEST_BUNDLE_UPGRADE_CODE, BUNDLE_INSTALL_CONTEXT_USER, &dwRelatedBundleIndex, sczRelatedBundleId);
                TestThrowOnFailure(hr, L"Failed to enumerate related bundle.");
                Assert::Equal<String^>(gcnew String(TEST_BUNDLE_ID), gcnew String(sczRelatedBundleId));

                // Verify we can read the bundle variables via the API
                hr = BundleGetBundleVariable(TEST_BUNDLE_ID, L"MyBurnVariable1", &sczValue);
                TestThrowOnFailure(hr, L"Failed to read MyBurnVariable1.");
                Assert::Equal<String^>(gcnew String(L"42"), gcnew String(sczValue));

                // end session
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");
            }
            finally
            {
                ReleaseStr(sczRelatedBundleId);
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

        [Fact]//(Skip = "Currently fails")]
        void ResumeTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            LPWSTR sczValue = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            BOOTSTRAPPER_RESUME_TYPE resumeType = BOOTSTRAPPER_RESUME_TYPE_NONE;
            BYTE* pbBuffer = NULL;
            SIZE_T cbBuffer = 0;
            SIZE_T piBuffer = 0;
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));
            try
            {
                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43e6-9C67-B5652240618C}' UpgradeCode='{D54F896D-1952-43e6-9C67-B5652240618C}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='RegisterBasicTest' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"    <Variable Id='MyBurnVariable1' Type='numeric' Value='0' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable2' Type='string' Value='foo' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable3' Type='version' Value='v1.1-alpha' Hidden='no' Persisted='yes' />"
                    L"    <CommandLine Variables='upperCase' />"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = VariablesParseFromXml(&variables, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse variables from XML.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                // read resume type before session
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read resume type.");

                Assert::Equal((int)BOOTSTRAPPER_RESUME_TYPE_NONE, (int)resumeType);

                // begin session
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                VariableSetNumericHelper(&variables, L"MyBurnVariable1", 42);
                VariableSetStringHelper(&variables, L"MyBurnVariable2", L"bar", FALSE);
                VariableSetVersionHelper(&variables, L"MyBurnVariable3", L"v1.0-beta");

                hr = VariableSerialize(&variables, TRUE, &pbBuffer, &cbBuffer);
                TestThrowOnFailure(hr, "Failed to serialize variables.");

                if (!Directory::Exists(cacheDirectory))
                {
                    Directory::CreateDirectory(cacheDirectory);
                }

                hr = RegistrationSaveState(&registration, pbBuffer, cbBuffer);
                TestThrowOnFailure(hr, L"Failed to save state.");

                ReleaseNullBuffer(pbBuffer);
                cbBuffer = 0;
                // Verify the variables exist
                Assert::Equal<String^>(gcnew String(L"42"), (String^)Registry::GetValue(gcnew String(TEST_VARIABLE_KEY), gcnew String(L"MyBurnVariable1"), nullptr));
                Assert::Equal<String^>(gcnew String(L"bar"), (String^)Registry::GetValue(gcnew String(TEST_VARIABLE_KEY), gcnew String(L"MyBurnVariable2"), nullptr));
                Assert::Equal<String^>(gcnew String(L"1.0-beta"), (String^)Registry::GetValue(gcnew String(TEST_VARIABLE_KEY), gcnew String(L"MyBurnVariable3"), nullptr));
                Assert::Empty((System::Collections::IEnumerable^)Registry::GetValue(gcnew String(TEST_VARIABLE_KEY), gcnew String(L"WixBundleForcedRestartPackage"), nullptr));

                hr = BundleGetBundleVariable(TEST_BUNDLE_ID, L"MyBurnVariable1", &sczValue);
                TestThrowOnFailure(hr, L"Failed to read MyBurnVariable1.");
                Assert::Equal<String^>(gcnew String(L"42"), gcnew String(sczValue));

                // read interrupted resume type
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read interrupted resume type.");

                Assert::Equal((int)BOOTSTRAPPER_RESUME_TYPE_INTERRUPTED, (int)resumeType);

                // suspend session
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_SUSPEND, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to suspend session.");

                // verify that run key was removed
                Assert::Equal((Object^)nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // read suspend resume type
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read suspend resume type.");

                Assert::Equal((int)BOOTSTRAPPER_RESUME_TYPE_SUSPEND, (int)resumeType);

                // read state back
                hr = RegistrationLoadState(&registration, &pbBuffer, &cbBuffer);
                TestThrowOnFailure(hr, L"Failed to load state.");

                hr = VariableDeserialize(&variables, TRUE, pbBuffer, cbBuffer, &piBuffer);
                TestThrowOnFailure(hr, L"Failed to deserialize variables.");

                //Assert::Equal((SIZE_T)sizeof(rgbData), cbBuffer);
                //Assert::True(0 == memcmp(pbBuffer, rgbData, sizeof(rgbData)));

                // write active resume mode
                hr = RegistrationSessionResume(&registration, &variables, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to write active resume mode.");

                // verify that run key was put back
                Assert::NotEqual((Object^)nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // end session
                hr = RegistrationSessionEnd(&registration, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // read resume type after session
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read resume type.");

                Assert::Equal((int)BOOTSTRAPPER_RESUME_TYPE_NONE, (int)resumeType);
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

    //BOOTSTRAPPER_RESUME_TYPE_NONE,
    //BOOTSTRAPPER_RESUME_TYPE_INVALID,        // resume information is present but invalid
    //BOOTSTRAPPER_RESUME_TYPE_UNEXPECTED,     // relaunched after an unexpected interruption
    //BOOTSTRAPPER_RESUME_TYPE_REBOOT_PENDING, // reboot has not taken place yet
    //BOOTSTRAPPER_RESUME_TYPE_REBOOT,         // relaunched after reboot
    //BOOTSTRAPPER_RESUME_TYPE_SUSPEND,        // relaunched after suspend
    //BOOTSTRAPPER_RESUME_TYPE_ARP,            // launched from ARP
    };
}
}
}
}
}


static LSTATUS APIENTRY RegistrationTest_RegCreateKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __reserved DWORD Reserved,
    __in_opt LPWSTR lpClass,
    __in DWORD dwOptions,
    __in REGSAM samDesired,
    __in_opt CONST LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    __out PHKEY phkResult,
    __out_opt LPDWORD lpdwDisposition
    )
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;
    HKEY hkRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        wzRoot = HKLM_PATH;
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        wzRoot = HKCU_PATH;
    }
    else
    {
        hkRoot = hKey;
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE, &hkRoot);
        if (ERROR_SUCCESS != ls)
        {
            ExitFunction();
        }
    }

    ls = ::RegCreateKeyExW(hkRoot, lpSubKey, Reserved, lpClass, dwOptions, samDesired, lpSecurityAttributes, phkResult, lpdwDisposition);

LExit:
    ReleaseRegKey(hkRoot);

    return ls;
}

static LSTATUS APIENTRY RegistrationTest_RegOpenKeyExW(
    __in HKEY hKey,
    __in_opt LPCWSTR lpSubKey,
    __reserved DWORD ulOptions,
    __in REGSAM samDesired,
    __out PHKEY phkResult
    )
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;
    HKEY hkRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        wzRoot = HKLM_PATH;
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        wzRoot = HKCU_PATH;
    }
    else
    {
        hkRoot = hKey;
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE, &hkRoot);
        if (ERROR_SUCCESS != ls)
        {
            ExitFunction();
        }
    }

    ls = ::RegOpenKeyExW(hkRoot, lpSubKey, ulOptions, samDesired, phkResult);

LExit:
    ReleaseRegKey(hkRoot);

    return ls;
}

static LSTATUS APIENTRY RegistrationTest_RegDeleteKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __in REGSAM samDesired,
    __reserved DWORD Reserved
    )
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;
    HKEY hkRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        wzRoot = HKLM_PATH;
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        wzRoot = HKCU_PATH;
    }
    else
    {
        hkRoot = hKey;
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE | samDesired, &hkRoot);
        if (ERROR_SUCCESS != ls)
        {
            ExitFunction();
        }
    }

    ls = ::RegDeleteKeyExW(hkRoot, lpSubKey, samDesired, Reserved);

LExit:
    ReleaseRegKey(hkRoot);

    return ls;
}
