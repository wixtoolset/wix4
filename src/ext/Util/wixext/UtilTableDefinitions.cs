// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data.WindowsInstaller;

    public static class UtilTableDefinitions
    {
        public static readonly TableDefinition Wix4CloseApplication = new TableDefinition(
            "Wix4CloseApplication",
            UtilSymbolDefinitions.WixCloseApplication,
            new[]
            {
                new ColumnDefinition("Wix4CloseApplication", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token in table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Target", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of executable to ensure is closed.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Description", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Description string displayed to user when executable is in use.", modularizeType: ColumnModularizeType.Property, forceLocalizable: true),
                new ColumnDefinition("Condition", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the closing.", modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied."),
                new ColumnDefinition("Sequence", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 1, maxValue: 2147483647, description: "Sequence to order the closings by."),
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Optional property that is set to the number of running instances of the app.", modularizeType: ColumnModularizeType.Property, forceLocalizable: true),
                new ColumnDefinition("TerminateExitCode", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Exit code to return from a terminated application."),
                new ColumnDefinition("Timeout", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 1, maxValue: 2147483647, description: "Timeout in milliseconds before scheduling restart or terminating application."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4RemoveFolderEx = new TableDefinition(
            "Wix4RemoveFolderEx",
            UtilSymbolDefinitions.WixRemoveFolderEx,
            new[]
            {
                new ColumnDefinition("Wix4RemoveFolderEx", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Identifier for the WixRemoveFolderEx row in the package.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "Name of Property that contains the root of the directory tree to remove.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("InstallMode", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 3, description: "1 == Remove only when the associated component is being installed (msiInstallStateLocal or msiInstallStateSource), 2 == Remove only when the associated component is being removed (msiInstallStateAbsent), 3 = Remove in either of the above cases."),
                new ColumnDefinition("Condition", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the removing of folders.", modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4RemoveRegistryKeyEx = new TableDefinition(
            "Wix4RemoveRegistryKeyEx",
            UtilSymbolDefinitions.WixRemoveRegistryKeyEx,
            new[]
            {
                new ColumnDefinition("Wix4RemoveRegistryKeyEx", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Identifier for the Wix4RemoveRegistryKeyEx row in the package.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Root", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -1, maxValue: 3, description: "The predefined root key for the registry value, one of rrkEnum."),
                new ColumnDefinition("Key", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.RegPath, description: "The key for the registry value.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("InstallMode", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 3, description: "1 == Remove only when the associated component is being installed (msiInstallStateLocal or msiInstallStateSource), 2 == Remove only when the associated component is being removed (msiInstallStateAbsent), 3 = Remove in either of the above cases."),
                new ColumnDefinition("Condition", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression to control whether the registry key is removed.", modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4RestartResource = new TableDefinition(
            "Wix4RestartResource",
            UtilSymbolDefinitions.WixRestartResource,
            new[]
            {
                new ColumnDefinition("Wix4RestartResource", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized identifier.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table used to determine install state.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Resource", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The resource to be registered with the Restart Manager.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the type of resource and flags used for processing."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4FileShare = new TableDefinition(
            "Wix4FileShare",
            UtilSymbolDefinitions.FileShare,
            new[]
            {
                new ColumnDefinition("Wix4FileShare", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized identifier", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ShareName", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The actual share name used"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Description string displayed for the file share"),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Foreign key referencing directory that the share is created on", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4FileSharePermissions = new TableDefinition(
            "Wix4FileSharePermissions",
            UtilSymbolDefinitions.FileSharePermissions,
            new[]
            {
                new ColumnDefinition("Wix4FileShare_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "FileShare", keyColumn: 1, description: "FileShare that these premissions are to be applied to.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Wix4User_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4User", description: "User that these premissions are to apply to.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Permissions", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Permissions int, as in EXPLICIT_ACCESS.grfAccessPermissions in MSDN"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Wix4Group = new TableDefinition(
            "Wix4Group",
            UtilSymbolDefinitions.Group,
            new[]
            {
                new ColumnDefinition("Wix4Group", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "Component", keyColumn: 1, description: "Foreign key, Component used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Group name", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Domain", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Group domain", modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4InternetShortcut = new TableDefinition(
            "Wix4InternetShortcut",
            UtilSymbolDefinitions.WixInternetShortcut,
            new[]
            {
                new ColumnDefinition("Wix4InternetShortcut", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token in table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Text, keyTable: "Component", keyColumn: 1, description: "Foreign key, Component used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Foreign key referencing directory that the shortcut is created in", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Name used for shortcut.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Target", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "URL target."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Attribute flags that control how the shortcut is created."),
                new ColumnDefinition("IconFile", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Icon file for shortcut", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("IconIndex", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Index of the icon being referenced."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4PerformanceCategory = new TableDefinition(
            "Wix4PerformanceCategory",
            UtilSymbolDefinitions.PerformanceCategory,
            new[]
            {
                new ColumnDefinition("Wix4PerformanceCategory", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token in table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Component used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 80, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Name of the performance counter category."),
                new ColumnDefinition("IniData", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Data that goes into the performance counter .ini file."),
                new ColumnDefinition("ConstantData", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Data that goes into the performance counter .h file."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4Perfmon = new TableDefinition(
            "Wix4Perfmon",
            UtilSymbolDefinitions.Perfmon,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Component used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "Name of .INI file", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Service name in registry"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Wix4PerfmonManifest = new TableDefinition(
            "Wix4PerfmonManifest",
            UtilSymbolDefinitions.PerfmonManifest,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Component used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "Name of perfmon manifest file", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("ResourceFileDirectory", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "The path of the Resource File Directory"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Wix4EventManifest = new TableDefinition(
            "Wix4EventManifest",
            UtilSymbolDefinitions.EventManifest,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Component used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "Name of event manifest file", modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Wix4SecureObject = new TableDefinition(
            "Wix4SecureObject",
            UtilSymbolDefinitions.SecureObjects,
            new[]
            {
                new ColumnDefinition("Wix4SecureObject", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token in Table", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Table SecureObject should be securing"),
                new ColumnDefinition("Domain", ColumnType.String, 255, primaryKey: true, nullable: true, ColumnCategory.Text, description: "Domain half of user account to secure", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("User", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "User name half of user account to secure", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Integer, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied."),
                new ColumnDefinition("Permission", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Permissions to grant to User"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table used to determine install state", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Wix4ServiceConfig = new TableDefinition(
            "Wix4ServiceConfig",
            UtilSymbolDefinitions.ServiceConfig,
            new[]
            {
                new ColumnDefinition("ServiceName", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "Primary key, non-localized token"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key, Component used to determine install state ", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("NewService", ColumnType.Number, 1, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Whether the affected service is being installed or already exists."),
                new ColumnDefinition("FirstFailureActionType", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Text, description: "First failure action type for configured service to take."),
                new ColumnDefinition("SecondFailureActionType", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Second failure action type for configured service to take."),
                new ColumnDefinition("ThirdFailureActionType", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Third failure action type for configured service to take."),
                new ColumnDefinition("ResetPeriodInDays", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Integer, minValue: 0, description: "Period after which to reset the failure count for the service."),
                new ColumnDefinition("RestartServiceDelayInSeconds", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Integer, minValue: 0, description: "Period after which to restart the service after a given failure."),
                new ColumnDefinition("ProgramCommandLine", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Command line for program to run if failure action is RUN_COMMAND."),
                new ColumnDefinition("RebootMessage", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Message to show to users when rebooting if failure action is REBOOT."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Wix4TouchFile = new TableDefinition(
            "Wix4TouchFile",
            UtilSymbolDefinitions.WixTouchFile,
            new[]
            {
                new ColumnDefinition("Wix4TouchFile", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Identifier for the Wix4TouchFile row in the package.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Path", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Formatted column that resolves to the path to touch.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 63, description: "1 == Touch only when the associated component is being installed, 2 == Touch only when the associated component is being repaired , 4 == Touch only when the associated component is being removed, 16 = path is in 64-bit location, 32 = touching the file is vital."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4User = new TableDefinition(
            "Wix4User",
            UtilSymbolDefinitions.User,
            new[]
            {
                new ColumnDefinition("Wix4User", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "Component", keyColumn: 1, description: "Foreign key, Component used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "User name", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Domain", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "User domain", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Password", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "User password", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Comment", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "User comment", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 65535, description: "Attributes describing how to create the user"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4UserGroup = new TableDefinition(
            "Wix4UserGroup",
            UtilSymbolDefinitions.UserGroup,
            new[]
            {
                new ColumnDefinition("Wix4User_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4User", keyColumn: 1, description: "User to be joined to a Group.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Wix4Group_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4Group", keyColumn: 1, description: "Group to join User to.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Wix4XmlFile = new TableDefinition(
            "Wix4XmlFile",
            UtilSymbolDefinitions.XmlFile,
            new[]
            {
                new ColumnDefinition("Wix4XmlFile", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .XML file in which to write the information", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("ElementPath", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .XML file element to modify.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The .XML file node to set/add in the element.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value to be written.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Flags", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 70143, description: "Flags"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the installing of the .XML value.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Order to execute the XML modifications."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4XmlConfig = new TableDefinition(
            "Wix4XmlConfig",
            UtilSymbolDefinitions.XmlConfig,
            new[]
            {
                new ColumnDefinition("Wix4XmlConfig", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .XML file in which to write the information", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("ElementId", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Wix4XmlConfig", keyColumn: 1, description: "A foreign key reference to another Wix4XmlConfig row if no attributes are set and the row referenced is a create element row.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ElementPath", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The XPATH query for an element to modify or add children to. Must be null if ElementId is provided", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("VerifyPath", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The XPATH query run from ElementPath to verify whether a repair is necessary.  Also used to uninstall.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The .XML file node to set/add in the element.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value to be written.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Flags", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 65536, description: "Element=1,Value=2,Document=4,Create=16,Delete=32,Install=256,Uninstall=512"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the installing of the .XML value.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Order to execute the XML modifications."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Wix4FormatFile = new TableDefinition(
            "Wix4FormatFile",
            UtilSymbolDefinitions.WixFormatFiles,
            new[]
            {
                new ColumnDefinition("Binary_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Binary", keyColumn: 1, description: "Binary data to be formatted.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "File whose component controls the custom action and where the formatted data is written.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition[] All = new[]
        {
            Wix4CloseApplication,
            Wix4RemoveFolderEx,
            Wix4RemoveRegistryKeyEx,
            Wix4RestartResource,
            Wix4FileShare,
            Wix4FileSharePermissions,
            Wix4Group,
            Wix4InternetShortcut,
            Wix4PerformanceCategory,
            Wix4Perfmon,
            Wix4PerfmonManifest,
            Wix4EventManifest,
            Wix4SecureObject,
            Wix4ServiceConfig,
            Wix4TouchFile,
            Wix4User,
            Wix4UserGroup,
            Wix4XmlFile,
            Wix4XmlConfig,
            Wix4FormatFile,
        };
    }
}
