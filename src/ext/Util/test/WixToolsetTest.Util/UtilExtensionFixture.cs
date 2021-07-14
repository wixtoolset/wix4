// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Util
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Util;
    using Xunit;

    public class UtilExtensionFixture
    {
        [Fact]
        public void CanBuildUsingFileShare()
        {
            var folder = TestData.Get(@"TestData\UsingFileShare");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "Binary", "CustomAction", "Wix4FileShare", "Wix4FileSharePermissions");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X86\t[Binary data]",
                "CustomAction:Wix4ConfigureSmbInstall_X86\t1\tWix4UtilCA_X86\tConfigureSmbInstall\t",
                "CustomAction:Wix4ConfigureSmbUninstall_X86\t1\tWix4UtilCA_X86\tConfigureSmbUninstall\t",
                "CustomAction:Wix4CreateSmb_X86\t11265\tWix4UtilCA_X86\tCreateSmb\t",
                "CustomAction:Wix4CreateSmbRollback_X86\t11585\tWix4UtilCA_X86\tDropSmb\t",
                "CustomAction:Wix4DropSmb_X86\t11265\tWix4UtilCA_X86\tDropSmb\t",
                "CustomAction:Wix4DropSmbRollback_X86\t11585\tWix4UtilCA_X86\tCreateSmb\t",
                "Wix4FileShare:ExampleFileShare\texample\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tAn example file share\tINSTALLFOLDER",
                "Wix4FileSharePermissions:ExampleFileShare\tEveryone\t1",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildUsingFileShareX64()
        {
            var folder = TestData.Get(@"TestData\UsingFileShare");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "Wix4FileShare", "Wix4FileSharePermissions");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64\t[Binary data]",
                "CustomAction:Wix4ConfigureSmbInstall_X64\t1\tWix4UtilCA_X64\tConfigureSmbInstall\t",
                "CustomAction:Wix4ConfigureSmbUninstall_X64\t1\tWix4UtilCA_X64\tConfigureSmbUninstall\t",
                "CustomAction:Wix4CreateSmb_X64\t11265\tWix4UtilCA_X64\tCreateSmb\t",
                "CustomAction:Wix4CreateSmbRollback_X64\t11585\tWix4UtilCA_X64\tDropSmb\t",
                "CustomAction:Wix4DropSmb_X64\t11265\tWix4UtilCA_X64\tDropSmb\t",
                "CustomAction:Wix4DropSmbRollback_X64\t11585\tWix4UtilCA_X64\tCreateSmb\t",
                "Wix4FileShare:ExampleFileShare\texample\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tAn example file share\tINSTALLFOLDER",
                "Wix4FileSharePermissions:ExampleFileShare\tEveryone\t1",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildCloseApplication()
        {
            var folder = TestData.Get(@"TestData\CloseApplication");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Binary", "CustomAction", "Wix4CloseApplication");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_A64\t[Binary data]",
                "CustomAction:Wix4CheckRebootRequired_A64\t65\tWix4UtilCA_A64\tWixCheckRebootRequired\t",
                "CustomAction:Wix4CloseApplications_A64\t1\tWix4UtilCA_A64\tWixCloseApplications\t",
                "CustomAction:Wix4CloseApplicationsDeferred_A64\t3073\tWix4UtilCA_A64\tWixCloseApplicationsDeferred\t",
                "Wix4CloseApplication:CloseMyApp\texplorer.exe\t\t\t3\t\tMYAPPISRUNNING\t\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildInternetShortcutInProduct()
        {
            var folder = TestData.Get(@"TestData\InternetShortcut");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "RemoveFile", "Wix4InternetShortcut");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64\t[Binary data]",
                "CustomAction:Wix4CreateInternetShortcuts_X64\t3073\tWix4UtilCA_X64\tWixCreateInternetShortcuts\t",
                "CustomAction:Wix4RollbackInternetShortcuts_X64\t3329\tWix4UtilCA_X64\tWixRollbackInternetShortcuts\t",
                "CustomAction:Wix4SchedInternetShortcuts_X64\t1\tWix4UtilCA_X64\tWixSchedInternetShortcuts\t",
                "RemoveFile:uisdCsU32.1i4Hebrg1N7E194zJQ8Y\tPackage.ico\thoiptxrr.url|WiX Toolset (url).url\tINSTALLFOLDER\t2",
                "RemoveFile:uisjV.q0ROZZYR3h_lkpbkZtLtPH0A\tPackage.ico\tjcxd1dwf.lnk|WiX Toolset (link).lnk\tINSTALLFOLDER\t2",
                "Wix4InternetShortcut:uisdCsU32.1i4Hebrg1N7E194zJQ8Y\tPackage.ico\tINSTALLFOLDER\tWiX Toolset (url).url\thttps://wixtoolset.org\t1\t[#Package.ico]\t0",
                "Wix4InternetShortcut:uisjV.q0ROZZYR3h_lkpbkZtLtPH0A\tPackage.ico\tINSTALLFOLDER\tWiX Toolset (link).lnk\thttps://wixtoolset.org\t0\t[#Package.ico]\t0",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildInternetShortcutInMergeModule()
        {
            var folder = TestData.Get(@"TestData\InternetShortcutModule");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder }, "test.msm");

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "RemoveFile", "Wix4InternetShortcut");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t[Binary data]",
                "CustomAction:Wix4CreateInternetShortcuts_X64\t3073\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixCreateInternetShortcuts\t",
                "CustomAction:Wix4RollbackInternetShortcuts_X64\t3329\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixRollbackInternetShortcuts\t",
                "CustomAction:Wix4SchedInternetShortcuts_X64\t1\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixSchedInternetShortcuts\t",
                "RemoveFile:uisdCsU32.1i4Hebrg1N7E194zJQ8Y.047730A5_30FE_4A62_A520_DA9381B8226A\tPackage.ico.047730A5_30FE_4A62_A520_DA9381B8226A\thoiptxrr.url|WiX Toolset (url).url\tINSTALLFOLDER.047730A5_30FE_4A62_A520_DA9381B8226A\t2",
                "RemoveFile:uisjV.q0ROZZYR3h_lkpbkZtLtPH0A.047730A5_30FE_4A62_A520_DA9381B8226A\tPackage.ico.047730A5_30FE_4A62_A520_DA9381B8226A\tjcxd1dwf.lnk|WiX Toolset (link).lnk\tINSTALLFOLDER.047730A5_30FE_4A62_A520_DA9381B8226A\t2",
                "Wix4InternetShortcut:uisdCsU32.1i4Hebrg1N7E194zJQ8Y.047730A5_30FE_4A62_A520_DA9381B8226A\tPackage.ico.047730A5_30FE_4A62_A520_DA9381B8226A\tINSTALLFOLDER.047730A5_30FE_4A62_A520_DA9381B8226A\tWiX Toolset (url).url\thttps://wixtoolset.org\t1\t[#Package.ico.047730A5_30FE_4A62_A520_DA9381B8226A]\t0",
                "Wix4InternetShortcut:uisjV.q0ROZZYR3h_lkpbkZtLtPH0A.047730A5_30FE_4A62_A520_DA9381B8226A\tPackage.ico.047730A5_30FE_4A62_A520_DA9381B8226A\tINSTALLFOLDER.047730A5_30FE_4A62_A520_DA9381B8226A\tWiX Toolset (link).lnk\thttps://wixtoolset.org\t0\t[#Package.ico.047730A5_30FE_4A62_A520_DA9381B8226A]\t0",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildWithPermissionEx()
        {
            var folder = TestData.Get(@"TestData\PermissionEx");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Wix4SecureObject");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4SecureObject:ExampleRegistryKey\tRegistry\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix4SecureObject:filF5_pLhBuF5b4N9XEo52g_hUM5Lo\tFile\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix4SecureObject:INSTALLFOLDER\tCreateFolder\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix4SecureObject:regL6DnQ9yJpDJH5OdcVji4YXsdX2c\tRegistry\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix4SecureObject:testsvc\tServiceInstall\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildRemoveRegistryKeyExInMergeModule()
        {
            var folder = TestData.Get(@"TestData", "RemoveRegistryKeyEx");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder }, "test.msm");

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "RemoveRegistry", "Wix4RemoveRegistryKeyEx");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t[Binary data]",
                "CustomAction:Wix4RemoveRegistryKeysEx_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t65\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixRemoveRegistryKeysEx\t",
                "Wix4RemoveRegistryKeyEx:rrxfcDhR4HhE3v3rYiQcNtQjyahQNg.047730A5_30FE_4A62_A520_DA9381B8226A\tfilh4juyUVjoUcWWtcQmd5L07FoON4.047730A5_30FE_4A62_A520_DA9381B8226A\t2\tSOFTWARE\\Example\t1\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildRemoveFolderExInMergeModule()
        {
            var folder = TestData.Get(@"TestData\RemoveFolderEx");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder }, "test.msm");

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "RemoveFile", "Wix4RemoveFolderEx");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t[Binary data]",
                "CustomAction:Wix4RemoveFoldersEx_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t65\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixRemoveFoldersEx\t",
                "Wix4RemoveFolderEx:wrf5qCm1SE.zp8djrlk78l1IYFXsEw.047730A5_30FE_4A62_A520_DA9381B8226A\tfilh4juyUVjoUcWWtcQmd5L07FoON4.047730A5_30FE_4A62_A520_DA9381B8226A\tRemoveProp.047730A5_30FE_4A62_A520_DA9381B8226A\t3\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildWithEventManifest()
        {
            var folder = TestData.Get(@"TestData\EventManifest");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Binary", "CustomAction", "Wix4EventManifest", "Wix4XmlFile");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_A64\t[Binary data]",
                "CustomAction:Wix4ConfigureEventManifestRegister_A64\t1\tWix4UtilCA_A64\tConfigureEventManifestRegister\t",
                "CustomAction:Wix4ConfigureEventManifestUnregister_A64\t1\tWix4UtilCA_A64\tConfigureEventManifestUnregister\t",
                "CustomAction:Wix4ExecXmlFile_A64\t11265\tWix4UtilCA_A64\tExecXmlFile\t",
                "CustomAction:Wix4ExecXmlFileRollback_A64\t11521\tWix4UtilCA_A64\tExecXmlFileRollback\t",
                "CustomAction:Wix4RegisterEventManifest_A64\t3073\tWix4UtilCA_A64\tWixQuietExec\t",
                "CustomAction:Wix4RollbackRegisterEventManifest_A64\t3393\tWix4UtilCA_A64\tWixQuietExec\t",
                "CustomAction:Wix4RollbackUnregisterEventManifest_A64\t3329\tWix4UtilCA_A64\tWixQuietExec\t",
                "CustomAction:Wix4SchedXmlFile_A64\t1\tWix4UtilCA_A64\tSchedXmlFile\t",
                "CustomAction:Wix4UnregisterEventManifest_A64\t3137\tWix4UtilCA_A64\tWixQuietExec\t",
                "Wix4EventManifest:Manifest.dll\t[#Manifest.dll]",
                "Wix4XmlFile:Config_Manifest.dllMessageFile\t[#Manifest.dll]\t/*/*/*/*[\\[]@messageFileName[\\]]\tmessageFileName\t[Manifest.dll]\t4100\tManifest.dll\t",
                "Wix4XmlFile:Config_Manifest.dllResourceFile\t[#Manifest.dll]\t/*/*/*/*[\\[]@resourceFileName[\\]]\tresourceFileName\t[Manifest.dll]\t4100\tManifest.dll\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildWithQueries()
        {
            var folder = TestData.Get(@"TestData\Queries");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Binary", "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_A64\t[Binary data]",
                "CustomAction:Wix4BroadcastEnvironmentChange_A64\t65\tWix4UtilCA_A64\tWixBroadcastEnvironmentChange\t",
                "CustomAction:Wix4BroadcastSettingChange_A64\t65\tWix4UtilCA_A64\tWixBroadcastSettingChange\t",
                "CustomAction:Wix4CheckRebootRequired_A64\t65\tWix4UtilCA_A64\tWixCheckRebootRequired\t",
                "CustomAction:Wix4QueryOsDriverInfo_A64\t257\tWix4UtilCA_A64\tWixQueryOsDriverInfo\t",
                "CustomAction:Wix4QueryOsInfo_A64\t257\tWix4UtilCA_A64\tWixQueryOsInfo\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildWithXmlConfig()
        {
            var folder = TestData.Get(@"TestData", "XmlConfig");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Wix4XmlConfig");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4XmlConfig:DelElement\t[INSTALLFOLDER]my.xml\t\t//root/sub\txxx\t\t\t289\tDel\t1",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildModuleWithXmlConfig()
        {
            var folder = TestData.Get(@"TestData", "XmlConfigModule");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Wix4XmlConfig");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4XmlConfig:AddElement.047730A5_30FE_4A62_A520_DA9381B8226A\t[my.xml.047730A5_30FE_4A62_A520_DA9381B8226A]\t\t//root/sub\txxx\t\t\t273\tParent.047730A5_30FE_4A62_A520_DA9381B8226A\t1",
                "Wix4XmlConfig:ChildElement.047730A5_30FE_4A62_A520_DA9381B8226A\t[my.xml.047730A5_30FE_4A62_A520_DA9381B8226A]\tAddElement.047730A5_30FE_4A62_A520_DA9381B8226A\t\txxx\t\t\t0\tChild.047730A5_30FE_4A62_A520_DA9381B8226A\t1",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildBundleWithSearches()
        {
            var burnStubPath = TestData.Get(@"TestData\.Data\burn.exe");
            var folder = TestData.Get(@"TestData\BundleWithSearches");
            var rootFolder = TestData.Get();
            var wixext = Path.Combine(rootFolder, "WixToolset.Util.wixext.dll");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-ext", wixext,
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));
#if TODO
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
#endif

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var bundleExtensionDatas = extractResult.SelectBundleExtensionDataNodes("/be:BundleExtensionData/be:BundleExtension[@Id='Wix4UtilBundleExtension_X86']");
                Assert.Equal(1, bundleExtensionDatas.Count);
                Assert.Equal("<BundleExtension Id='Wix4UtilBundleExtension_X86'>" +
                    "<WixWindowsFeatureSearch Id='DetectSHA2SupportId' Type='sha2CodeSigning' />" +
                    "</BundleExtension>", bundleExtensionDatas[0].GetTestXml());

                var utilSearches = extractResult.SelectManifestNodes("/burn:BurnManifest/*[self::burn:ExtensionSearch or self::burn:FileSearch or self::burn:MsiProductSearch or self::burn:RegistrySearch]");
                Assert.Equal(5, utilSearches.Count);
                Assert.Equal("<ExtensionSearch Id='DetectSHA2SupportId' Variable='IsSHA2Supported' " +
                    "ExtensionId='Wix4UtilBundleExtension_X86' />", utilSearches[0].GetTestXml());
                Assert.Equal("<FileSearch Id='FileSearchId' Variable='FileSearchVariable' " +
                    $@"Path='%windir%\System32\mscoree.dll' Type='exists' />", utilSearches[1].GetTestXml());
                Assert.Equal("<MsiProductSearch Id='ProductSearchId' Variable='ProductSearchVariable' Condition='1 &amp; 2 &lt; 3' " +
                    "UpgradeCode='{738D02BF-E231-4370-8209-E9FD4E1BE2A1}' Type='version' />", utilSearches[2].GetTestXml());
                Assert.Equal("<RegistrySearch Id='RegistrySearchId' Variable='RegistrySearchVariable' " +
                    @"Root='HKLM' Key='SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' Value='Release' Type='value' VariableType='string' />", utilSearches[3].GetTestXml());
                Assert.Equal("<RegistrySearch Id='RegistrySearchId64' Variable='RegistrySearchVariable64' " +
                    @"Root='HKLM' Key='SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' Value='Release' Type='value' Win64='yes' VariableType='string' />", utilSearches[4].GetTestXml());
            }
        }

        [Fact]
        public void CanCreateUserAccountWithComment()
        {
            var folder = TestData.Get(@"TestData\CreateUser");
            var rootFolder = TestData.Get();
            var wixext = Path.Combine(rootFolder, "WixToolset.Util.wixext.dll");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                baseFolder = Path.Combine(baseFolder, "Desktop", "CreateUser");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-ext", wixext,
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"test.wixpdb")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"test.wixpdb"));

                Assert.False(intermediate.HasLevel(WixToolset.Data.IntermediateLevels.Compiled));
                Assert.True(intermediate.HasLevel(WixToolset.Data.IntermediateLevels.Linked));
                Assert.True(intermediate.HasLevel(WixToolset.Data.IntermediateLevels.Resolved));
                Assert.True(intermediate.HasLevel(WixToolset.Data.WindowsInstaller.IntermediateLevels.FullyBound));
            }
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }

        private static void BuildX64(string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Add("-platform");
            newArgs.Add("x64");
            newArgs.Add("-sw1072");

            var result = WixRunner.Execute(newArgs.ToArray());
            result.AssertSuccess();
        }

        private static void BuildARM64(string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Add("-platform");
            newArgs.Add("arm64");

            var result = WixRunner.Execute(newArgs.ToArray());
            result.AssertSuccess();
        }
    }
}
