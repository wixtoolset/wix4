// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Util.Symbols;

    /// <summary>
    /// The compiler for the WiX Toolset Utility Extension.
    /// </summary>
    public sealed class UtilCompiler : BaseCompilerExtension
    {
        // user creation attributes definitions (from sca.h)
        internal const int UserDontExpirePasswrd = 0x00000001;
        internal const int UserPasswdCantChange = 0x00000002;
        internal const int UserPasswdChangeReqdOnLogin = 0x00000004;
        internal const int UserDisableAccount = 0x00000008;
        internal const int UserFailIfExists = 0x00000010;
        internal const int UserUpdateIfExists = 0x00000020;
        internal const int UserLogonAsService = 0x00000040;
        internal const int UserLogonAsBatchJob = 0x00000080;

        internal const int UserDontRemoveOnUninstall = 0x00000100;
        internal const int UserDontCreateUser = 0x00000200;
        internal const int UserNonVital = 0x00000400;

        private static readonly Regex FindPropertyBrackets = new Regex(@"\[(?!\\|\])|(?<!\[\\\]|\[\\|\\\[)\]", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/util";

        /// <summary>
        /// Types of Internet shortcuts.
        /// </summary>
        public enum InternetShortcutType
        {
            /// <summary>Create a .lnk file.</summary>
            Link = 0,

            /// <summary>Create a .url file.</summary>
            Url,
        }

        /// <summary>
        /// Types of permission setting methods.
        /// </summary>
        private enum PermissionType
        {
            /// <summary>LockPermissions (normal) type permission setting.</summary>
            LockPermissions,

            /// <summary>FileSharePermissions type permission setting.</summary>
            FileSharePermissions,

            /// <summary>SecureObjects type permission setting.</summary>
            SecureObjects,
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            this.ParsePossibleKeyPathElement(intermediate, section, parentElement, element, context);
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override IComponentKeyPath ParsePossibleKeyPathElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            IComponentKeyPath possibleKeyPath = null;

            switch (parentElement.Name.LocalName)
            {
                case "CreateFolder":
                    var createFolderId = context["DirectoryId"];
                    var createFolderComponentId = context["ComponentId"];

                    // If this doesn't parse successfully, something really odd is going on, so let the exception get thrown
                    var createFolderWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "PermissionEx":
                            this.ParsePermissionExElement(intermediate, section, element, createFolderId, createFolderComponentId, createFolderWin64, "CreateFolder");
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Component":
                    var componentId = context["ComponentId"];
                    var directoryId = context["DirectoryId"];
                    var componentWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "EventSource":
                            possibleKeyPath = this.ParseEventSourceElement(intermediate, section, element, componentId);
                            break;
                        case "FileShare":
                            this.ParseFileShareElement(intermediate, section, element, componentId, directoryId);
                            break;
                        case "InternetShortcut":
                            this.ParseInternetShortcutElement(intermediate, section, element, componentId, directoryId);
                            break;
                        case "PerformanceCategory":
                            this.ParsePerformanceCategoryElement(intermediate, section, element, componentId);
                            break;
                        case "RemoveFolderEx":
                            this.ParseRemoveFolderExElement(intermediate, section, element, componentId);
                            break;
                        case "RemoveRegistryKey":
                            this.ParseRemoveRegistryKeyExElement(intermediate, section, element, componentId);
                            break;
                        case "RestartResource":
                            this.ParseRestartResourceElement(intermediate, section, element, componentId);
                            break;
                        case "ServiceConfig":
                            this.ParseServiceConfigElement(intermediate, section, element, componentId, "Component", null);
                            break;
                        case "TouchFile":
                            this.ParseTouchFileElement(intermediate, section, element, componentId, componentWin64);
                            break;
                        case "User":
                            this.ParseUserElement(intermediate, section, element, componentId);
                            break;
                        case "XmlFile":
                            this.ParseXmlFileElement(intermediate, section, element, componentId);
                            break;
                        case "XmlConfig":
                            this.ParseXmlConfigElement(intermediate, section, element, componentId, false);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "File":
                    var fileId = context["FileId"];
                    var fileComponentId = context["ComponentId"];

                    // If this doesn't parse successfully, something really odd is going on, so let the exception get thrown
                    var fileWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "PerfCounter":
                            this.ParsePerfCounterElement(intermediate, section, element, fileComponentId, fileId);
                            break;
                        case "PermissionEx":
                            this.ParsePermissionExElement(intermediate, section, element, fileId, fileComponentId, fileWin64, "File");
                            break;
                        case "PerfCounterManifest":
                            this.ParsePerfCounterManifestElement(intermediate, section, element, fileComponentId, fileId);
                            break;
                        case "EventManifest":
                            this.ParseEventManifestElement(intermediate, section, element, fileComponentId, fileId);
                            break;
                        case "FormatFile":
                            this.ParseFormatFileElement(intermediate, section, element, fileId, fileWin64);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Bundle":
                case "Fragment":
                case "Module":
                case "Package":
                    switch (element.Name.LocalName)
                    {
                        case "CloseApplication":
                            this.ParseCloseApplicationElement(intermediate, section, element);
                            break;
                        case "Group":
                            this.ParseGroupElement(intermediate, section, element, null);
                            break;
                        case "RestartResource":
                            // Currently not supported for Bundles.
                            if (parentElement.Name.LocalName != "Bundle")
                            {
                                this.ParseRestartResourceElement(intermediate, section, element, null);
                            }
                            else
                            {
                                this.ParseHelper.UnexpectedElement(parentElement, element);
                            }
                            break;
                        case "User":
                            this.ParseUserElement(intermediate, section, element, null);
                            break;
                        case "BroadcastEnvironmentChange":
                        case "BroadcastSettingChange":
                        case "CheckRebootRequired":
                        case "ExitEarlyWithSuccess":
                        case "FailWhenDeferred":
                        case "QueryWindowsDirectories":
                        case "QueryWindowsDriverInfo":
                        case "QueryWindowsSuiteInfo":
                        case "QueryWindowsWellKnownSIDs":
                        case "WaitForEvent":
                        case "WaitForEventDeferred":
                            this.AddCustomActionReference(intermediate, section, element, parentElement);
                            break;
                        case "ComponentSearch":
                        case "ComponentSearchRef":
                        case "DirectorySearch":
                        case "DirectorySearchRef":
                        case "FileSearch":
                        case "FileSearchRef":
                        case "ProductSearch":
                        case "ProductSearchRef":
                        case "RegistrySearch":
                        case "RegistrySearchRef":
                        case "WindowsFeatureSearch":
                        case "WindowsFeatureSearchRef":
                            // These will eventually be supported under Module/Product, but are not yet.
                            if (parentElement.Name.LocalName == "Bundle" || parentElement.Name.LocalName == "Fragment")
                            {
                                // TODO: When these are supported by all section types, move
                                // these out of the nested switch and back into the surrounding one.
                                switch (element.Name.LocalName)
                                {
                                    case "ComponentSearch":
                                        this.ParseComponentSearchElement(intermediate, section, element);
                                        break;
                                    case "ComponentSearchRef":
                                        this.ParseComponentSearchRefElement(intermediate, section, element);
                                        break;
                                    case "DirectorySearch":
                                        this.ParseDirectorySearchElement(intermediate, section, element);
                                        break;
                                    case "DirectorySearchRef":
                                        this.ParseWixSearchRefElement(intermediate, section, element);
                                        break;
                                    case "FileSearch":
                                        this.ParseFileSearchElement(intermediate, section, element);
                                        break;
                                    case "FileSearchRef":
                                        this.ParseWixSearchRefElement(intermediate, section, element);
                                        break;
                                    case "ProductSearch":
                                        this.ParseProductSearchElement(intermediate, section, element);
                                        break;
                                    case "ProductSearchRef":
                                        this.ParseWixSearchRefElement(intermediate, section, element);
                                        break;
                                    case "RegistrySearch":
                                        this.ParseRegistrySearchElement(intermediate, section, element);
                                        break;
                                    case "RegistrySearchRef":
                                        this.ParseWixSearchRefElement(intermediate, section, element);
                                        break;
                                    case "WindowsFeatureSearch":
                                        this.ParseWindowsFeatureSearchElement(intermediate, section, element);
                                        break;
                                    case "WindowsFeatureSearchRef":
                                        this.ParseWindowsFeatureSearchRefElement(intermediate, section, element);
                                        break;
                                }
                            }
                            else
                            {
                                this.ParseHelper.UnexpectedElement(parentElement, element);
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Registry":
                case "RegistryKey":
                case "RegistryValue":
                    var registryId = context["RegistryId"];
                    var registryComponentId = context["ComponentId"];

                    // If this doesn't parse successfully, something really odd is going on, so let the exception get thrown
                    var registryWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "PermissionEx":
                            this.ParsePermissionExElement(intermediate, section, element, registryId, registryComponentId, registryWin64, "Registry");
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "ServiceInstall":
                    var serviceInstallId = context["ServiceInstallId"];
                    var serviceInstallName = context["ServiceInstallName"];
                    var serviceInstallComponentId = context["ServiceInstallComponentId"];

                    // If this doesn't parse successfully, something really odd is going on, so let the exception get thrown
                    var serviceInstallWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "PermissionEx":
                            this.ParsePermissionExElement(intermediate, section, element, serviceInstallId, serviceInstallComponentId, serviceInstallWin64, "ServiceInstall");
                            break;
                        case "ServiceConfig":
                            this.ParseServiceConfigElement(intermediate, section, element, serviceInstallComponentId, "ServiceInstall", serviceInstallName);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "UI":
                    switch (element.Name.LocalName)
                    {
                        case "BroadcastEnvironmentChange":
                        case "BroadcastSettingChange":
                        case "CheckRebootRequired":
                        case "ExitEarlyWithSuccess":
                        case "FailWhenDeferred":
                        case "QueryWindowsDirectories":
                        case "QueryWindowsDriverInfo":
                        case "QueryWindowsSuiteInfo":
                        case "QueryWindowsWellKnownSIDs":
                        case "WaitForEvent":
                        case "WaitForEventDeferred":
                            this.AddCustomActionReference(intermediate, section, element, parentElement);
                            break;
                    }
                    break;
                default:
                    this.ParseHelper.UnexpectedElement(parentElement, element);
                    break;
            }

            return possibleKeyPath;
        }

        private void AddCustomActionReference(Intermediate intermediate, IntermediateSection section, XElement element, XElement parentElement)
        {
            // These elements are not supported for bundles.
            if (parentElement.Name.LocalName == "Bundle")
            {
                this.ParseHelper.UnexpectedElement(parentElement, element);
                return;
            }

            var customAction = element.Name.LocalName;
            switch (element.Name.LocalName)
            {
                case "BroadcastEnvironmentChange":
                case "BroadcastSettingChange":
                case "CheckRebootRequired":
                case "ExitEarlyWithSuccess":
                case "FailWhenDeferred":
                case "WaitForEvent":
                case "WaitForEventDeferred":
                    //default: customAction = element.Name.LocalName;
                    break;
                case "QueryWindowsDirectories":
                    customAction = "QueryOsDirs";
                    break;
                case "QueryWindowsDriverInfo":
                    customAction = "QueryOsDriverInfo";
                    break;
                case "QueryWindowsSuiteInfo":
                    customAction = "QueryOsInfo";
                    break;
                case "QueryWindowsWellKnownSIDs":
                    customAction = "QueryOsWellKnownSID";
                    break;
            }

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    // no attributes today
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4" + customAction, this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
        }

        /// <summary>
        /// Parses the common search attributes shared across all searches.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="attrib">Attribute to parse.</param>
        /// <param name="id">Value of the Id attribute.</param>
        /// <param name="variable">Value of the Variable attribute.</param>
        /// <param name="condition">Value of the Condition attribute.</param>
        /// <param name="after">Value of the After attribute.</param>
        private void ParseCommonSearchAttributes(SourceLineNumber sourceLineNumbers, XAttribute attrib, ref Identifier id, ref string variable, ref string condition, ref string after)
        {
            switch (attrib.Name.LocalName)
            {
                case "Id":
                    id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                    break;
                case "Variable":
                    variable = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                    // TODO: handle standard bundle variables
                    break;
                case "Condition":
                    condition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                    break;
                case "After":
                    after = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
        }

        /// <summary>
        /// Parses a ComponentSearch element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        private void ParseComponentSearchElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string guid = null;
            string productCode = null;
            var attributes = WixComponentSearchAttributes.KeyPath;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "Guid":
                            guid = this.ParseHelper.GetAttributeGuidValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            productCode = this.ParseHelper.GetAttributeGuidValue(sourceLineNumbers, attrib);
                            break;
                        case "Result":
                            var result = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (result)
                            {
                                case "directory":
                                    attributes = WixComponentSearchAttributes.WantDirectory;
                                    break;
                                case "keyPath":
                                    attributes = WixComponentSearchAttributes.KeyPath;
                                    break;
                                case "state":
                                    attributes = WixComponentSearchAttributes.State;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName, result, "directory", "keyPath", "state"));
                                    break;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == guid)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Guid"));
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("wcs", variable, condition, after, guid, productCode, attributes.ToString());
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, element.Name.LocalName, id, variable, condition, after, null);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixComponentSearchSymbol(sourceLineNumbers, id)
                {
                    Guid = guid,
                    ProductCode = productCode,
                    Attributes = attributes,
                });
            }
        }

        /// <summary>
        /// Parses a ComponentSearchRef element
        /// </summary>
        /// <param name="element">Element to parse.</param>
        private void ParseComponentSearchRefElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string refId = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            refId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixComponentSearch, refId);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);
        }

        /// <summary>
        /// Parses a WindowsFeatureSearch element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        private void ParseWindowsFeatureSearchElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string feature = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "Feature":
                            feature = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (feature)
                            {
                                case "sha2CodeSigning":
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Feature", feature, "sha2CodeSigning"));
                                    break;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (id == null)
            {
                id = this.ParseHelper.CreateIdentifier("wwfs", variable, condition, after);
            }

            if (feature == null)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Feature"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            var bundleExtensionId = this.ParseHelper.CreateIdentifierValueFromPlatform("Wix4UtilBundleExtension", this.Context.Platform, BurnPlatforms.X86 | BurnPlatforms.X64 | BurnPlatforms.ARM64);
            if (bundleExtensionId == null)
            {
                this.Messaging.Write(ErrorMessages.UnsupportedPlatformForElement(sourceLineNumbers, this.Context.Platform.ToString(), element.Name.LocalName));
            }

            this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, element.Name.LocalName, id, variable, condition, after, bundleExtensionId);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixWindowsFeatureSearchSymbol(sourceLineNumbers, id)
                {
                    Type = feature,
                });
            }
        }

        /// <summary>
        /// Parses a WindowsFeatureSearchRef element
        /// </summary>
        /// <param name="element">Element to parse.</param>
        private void ParseWindowsFeatureSearchRefElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            var refId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, UtilSymbolDefinitions.WixWindowsFeatureSearch, refId);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);
        }

        /// <summary>
        /// Parses an event source element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private IComponentKeyPath ParseEventSourceElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string sourceName = null;
            string logName = null;
            string categoryMessageFile = null;
            var categoryCount = CompilerConstants.IntegerNotSet;
            string eventMessageFile = null;
            string parameterMessageFile = null;
            int typesSupported = 0;
            var isKeyPath = false;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "CategoryCount":
                            categoryCount = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                            break;
                        case "CategoryMessageFile":
                            categoryMessageFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EventMessageFile":
                            eventMessageFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "KeyPath":
                            isKeyPath = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Log":
                            logName = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("Security" == logName)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, logName, "Application", "System", "<customEventLog>"));
                            }
                            break;
                        case "Name":
                            sourceName = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ParameterMessageFile":
                            parameterMessageFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SupportsErrors":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x01; // EVENTLOG_ERROR_TYPE
                            }
                            break;
                        case "SupportsFailureAudits":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x10; // EVENTLOG_AUDIT_FAILURE
                            }
                            break;
                        case "SupportsInformationals":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x04; // EVENTLOG_INFORMATION_TYPE
                            }
                            break;
                        case "SupportsSuccessAudits":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x08; // EVENTLOG_AUDIT_SUCCESS
                            }
                            break;
                        case "SupportsWarnings":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x02; // EVENTLOG_WARNING_TYPE
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == sourceName)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            if (null == logName)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "EventLog"));
            }

            if (null == eventMessageFile)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "EventMessageFile"));
            }

            if (null == categoryMessageFile && 0 < categoryCount)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, element.Name.LocalName, "CategoryCount", "CategoryMessageFile"));
            }

            if (null != categoryMessageFile && CompilerConstants.IntegerNotSet == categoryCount)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, element.Name.LocalName, "CategoryMessageFile", "CategoryCount"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            string eventSourceKey = $@"SYSTEM\CurrentControlSet\Services\EventLog\{logName}\{sourceName}";
            var id = this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, eventSourceKey, "EventMessageFile", String.Concat("#%", eventMessageFile), componentId, false);

            if (null != categoryMessageFile)
            {
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, eventSourceKey, "CategoryMessageFile", String.Concat("#%", categoryMessageFile), componentId, false);
            }

            if (CompilerConstants.IntegerNotSet != categoryCount)
            {
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, eventSourceKey, "CategoryCount", String.Concat("#", categoryCount), componentId, false);
            }

            if (null != parameterMessageFile)
            {
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, eventSourceKey, "ParameterMessageFile", String.Concat("#%", parameterMessageFile), componentId, false);
            }

            if (0 != typesSupported)
            {
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, eventSourceKey, "TypesSupported", String.Concat("#", typesSupported), componentId, false);
            }

            var componentKeyPath = this.CreateComponentKeyPath();
            componentKeyPath.Id = id.Id;
            componentKeyPath.Explicit = isKeyPath;
            componentKeyPath.Type = PossibleKeyPathType.Registry;
            return componentKeyPath;
        }

        /// <summary>
        /// Parses a close application element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        private void ParseCloseApplicationElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string condition = null;
            string description = null;
            string target = null;
            string property = null;
            Identifier id = null;
            int attributes = 2; // default to CLOSEAPP_ATTRIBUTE_REBOOTPROMPT enabled
            var sequence = CompilerConstants.IntegerNotSet;
            var terminateExitCode = CompilerConstants.IntegerNotSet;
            var timeout = CompilerConstants.IntegerNotSet;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Property":
                            property = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Sequence":
                            sequence = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                            break;
                        case "Timeout":
                            timeout = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                            break;
                        case "Target":
                            target = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CloseMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 1; // CLOSEAPP_ATTRIBUTE_CLOSEMESSAGE
                            }
                            else
                            {
                                attributes &= ~1; // CLOSEAPP_ATTRIBUTE_CLOSEMESSAGE
                            }
                            break;
                        case "EndSessionMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 8; // CLOSEAPP_ATTRIBUTE_ENDSESSIONMESSAGE
                            }
                            else
                            {
                                attributes &= ~8; // CLOSEAPP_ATTRIBUTE_ENDSESSIONMESSAGE
                            }
                            break;
                        case "PromptToContinue":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x40; // CLOSEAPP_ATTRIBUTE_PROMPTTOCONTINUE
                            }
                            else
                            {
                                attributes &= ~0x40; // CLOSEAPP_ATTRIBUTE_PROMPTTOCONTINUE
                            }
                            break;
                        case "RebootPrompt":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 2; // CLOSEAPP_ATTRIBUTE_REBOOTPROMPT
                            }
                            else
                            {
                                attributes &= ~2; // CLOSEAPP_ATTRIBUTE_REBOOTPROMPT
                            }
                            break;
                        case "ElevatedCloseMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 4; // CLOSEAPP_ATTRIBUTE_ELEVATEDCLOSEMESSAGE
                            }
                            else
                            {
                                attributes &= ~4; // CLOSEAPP_ATTRIBUTE_ELEVATEDCLOSEMESSAGE
                            }
                            break;
                        case "ElevatedEndSessionMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x10; // CLOSEAPP_ATTRIBUTE_ELEVATEDENDSESSIONMESSAGE
                            }
                            else
                            {
                                attributes &= ~0x10; // CLOSEAPP_ATTRIBUTE_ELEVATEDENDSESSIONMESSAGE
                            }
                            break;
                        case "TerminateProcess":
                            terminateExitCode = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                            attributes |= 0x20; // CLOSEAPP_ATTRIBUTE_TERMINATEPROCESS
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == target)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Target"));
            }
            else if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("ca", target);
            }

            if (String.IsNullOrEmpty(description) && 0x40 == (attributes & 0x40))
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, element.Name.LocalName, "PromptToContinue", "yes", "Description"));
            }

            if (0x22 == (attributes & 0x22))
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "TerminateProcess", "RebootPrompt", "yes"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4CloseApplications", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new WixCloseApplicationSymbol(sourceLineNumbers, id)
                {
                    Target = target,
                    Description = description,
                    Condition = condition,
                    Attributes = attributes,
                    Property = property,
                });
                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    symbol.Sequence = sequence;
                }
                if (CompilerConstants.IntegerNotSet != terminateExitCode)
                {
                    symbol.TerminateExitCode = terminateExitCode;
                }
                if (CompilerConstants.IntegerNotSet != timeout)
                {
                    symbol.Timeout = timeout * 1000; // make the timeout milliseconds in the table.
                }
            }
        }

        /// <summary>
        /// Parses a DirectorySearch element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        private void ParseDirectorySearchElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string path = null;
            var attributes = WixFileSearchAttributes.IsDirectory;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "Path":
                            path = this.ParseHelper.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                            break;
                        case "Result":
                            var result = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (result)
                            {
                                case "exists":
                                    attributes |= WixFileSearchAttributes.WantExists;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName, result, "exists"));
                                    break;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == path)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Path"));
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("wds", variable, condition, after, path, attributes.ToString());
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, element.Name.LocalName, id, variable, condition, after, null);

            if (!this.Messaging.EncounteredError)
            {
                this.CreateWixFileSearchRow(section, sourceLineNumbers, id, path, attributes);
            }
        }

        /// <summary>
        /// Parses a DirectorySearchRef, FileSearchRef, ProductSearchRef, and RegistrySearchRef elements
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseWixSearchRefElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            var refId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixSearch, refId);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, node, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);
        }

        /// <summary>
        /// Parses a FileSearch element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseFileSearchElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string path = null;
            var attributes = WixFileSearchAttributes.Default;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "Path":
                            path = this.ParseHelper.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                            break;
                        case "Result":
                            string result = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (result)
                            {
                                case "exists":
                                    attributes |= WixFileSearchAttributes.WantExists;
                                    break;
                                case "version":
                                    attributes |= WixFileSearchAttributes.WantVersion;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName, result, "exists", "version"));
                                    break;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, node, attrib);
                }
            }

            if (null == path)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Path"));
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("wfs", variable, condition, after, path, attributes.ToString());
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, node.Name.LocalName, id, variable, condition, after, null);

            if (!this.Messaging.EncounteredError)
            {
                this.CreateWixFileSearchRow(section, sourceLineNumbers, id, path, attributes);
            }
        }

        /// <summary>
        /// Creates a row in the WixFileSearch table.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="id">Identifier of the search (key into the WixSearch table)</param>
        /// <param name="path">File/directory path to search for.</param>
        /// <param name="attributes"></param>
        private void CreateWixFileSearchRow(IntermediateSection section, SourceLineNumber sourceLineNumbers, Identifier id, string path, WixFileSearchAttributes attributes)
        {
            section.AddSymbol(new WixFileSearchSymbol(sourceLineNumbers, id)
            {
                Path = path,
                Attributes = attributes,
            });
        }

        /// <summary>
        /// Parses a file share element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="directoryId">Identifier of referred to directory.</param>
        private void ParseFileShareElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string directoryId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string description = null;
            string name = null;
            Identifier id = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("ufs", componentId, name);
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            if (!element.Elements().Any())
            {
                this.Messaging.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, element.Name.LocalName, "FileSharePermission"));
            }

            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "FileSharePermission":
                            this.ParseFileSharePermissionElement(intermediate, section, child, id);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigureSmbInstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigureSmbUninstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new FileShareSymbol(sourceLineNumbers, id)
                {
                    ShareName = name,
                    ComponentRef = componentId,
                    Description = description,
                    DirectoryRef = directoryId,
                });
            }
        }

        /// <summary>
        /// Parses a FileSharePermission element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="fileShareId">The identifier of the parent FileShare element.</param>
        private void ParseFileSharePermissionElement(Intermediate intermediate, IntermediateSection section, XElement element, Identifier fileShareId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            var bits = new BitArray(32);
            string user = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "User":
                            user = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, UtilSymbolDefinitions.User, user);
                            break;
                        default:
                            var attribValue = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (!this.TrySetBitFromName(UtilConstants.StandardPermissions, attrib.Name.LocalName, attribValue, bits, 16))
                            {
                                if (!this.TrySetBitFromName(UtilConstants.GenericPermissions, attrib.Name.LocalName, attribValue, bits, 28))
                                {
                                    if (!this.TrySetBitFromName(UtilConstants.FolderPermissions, attrib.Name.LocalName, attribValue, bits, 0))
                                    {
                                        this.ParseHelper.UnexpectedAttribute(element, attrib);
                                        break;
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            var permission = this.CreateIntegerFromBitArray(bits);

            if (null == user)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "User"));
            }

            if (Int32.MinValue == permission) // just GENERIC_READ, which is MSI_NULL
            {
                this.Messaging.Write(ErrorMessages.GenericReadNotAllowed(sourceLineNumbers));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new FileSharePermissionsSymbol(sourceLineNumbers)
                {
                    FileShareRef = fileShareId.Id,
                    UserRef = user,
                    Permissions = permission,
                });
            }
        }

        /// <summary>
        /// Parses a group element.
        /// </summary>
        /// <param name="element">Node to be parsed.</param>
        /// <param name="componentId">Component Id of the parent component of this element.</param>
        private void ParseGroupElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string domain = null;
            string name = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Domain":
                            domain = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("ugr", componentId, domain, name);
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new GroupSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Name = name,
                    Domain = domain,
                });
            }
        }

        /// <summary>
        /// Parses a GroupRef element
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="userId">Required user id to be joined to the group.</param>
        private void ParseGroupRefElement(Intermediate intermediate, IntermediateSection section, XElement element, string userId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string groupId = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            groupId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, UtilSymbolDefinitions.Group, groupId);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new UserGroupSymbol(sourceLineNumbers)
                {
                    UserRef = userId,
                    GroupRef = groupId,
                });
            }
        }

        /// <summary>
        /// Parses an InternetShortcut element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="defaultTarget">Default directory if none is specified on the InternetShortcut element.</param>
        private void ParseInternetShortcutElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string defaultTarget)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string name = null;
            string target = null;
            string directoryId = null;
            string type = null;
            string iconFile = null;
            int iconIndex = 0;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Directory":
                            directoryId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Target":
                            target = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            type = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IconFile":
                            iconFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IconIndex":
                            iconIndex = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            // If there was no directoryId specified on the InternetShortcut element, default to the one on
            // the parent component.
            if (null == directoryId)
            {
                directoryId = defaultTarget;
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("uis", componentId, directoryId, name, target);
            }

            // In theory this can never be the case, since InternetShortcut can only be under
            // a component element, and if the Directory wasn't specified the default will come
            // from the component. However, better safe than sorry, so here's a check to make sure
            // it didn't wind up being null after setting it to the defaultTarget.
            if (null == directoryId)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Directory"));
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            if (null == target)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Target"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            var shortcutType = InternetShortcutType.Link;
            if (String.Equals(type, "url", StringComparison.OrdinalIgnoreCase))
            {
                shortcutType = InternetShortcutType.Url;
            }

            if (!this.Messaging.EncounteredError)
            {
                this.CreateWixInternetShortcut(section, sourceLineNumbers, componentId, directoryId, id, name, target, shortcutType, iconFile, iconIndex);
            }
        }

        /// <summary>
        /// Creates the rows needed for WixInternetShortcut to work.
        /// </summary>
        /// <param name="core">The CompilerCore object used to create rows.</param>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="directoryId">Identifier of directory containing shortcut.</param>
        /// <param name="id">Identifier of shortcut.</param>
        /// <param name="name">Name of shortcut without extension.</param>
        /// <param name="target">Target URL of shortcut.</param>
        private void CreateWixInternetShortcut(IntermediateSection section, SourceLineNumber sourceLineNumbers, string componentId, string directoryId, Identifier shortcutId, string name, string target, InternetShortcutType type, string iconFile, int iconIndex)
        {
            // add the appropriate extension based on type of shortcut
            name = String.Concat(name, InternetShortcutType.Url == type ? ".url" : ".lnk");

            section.AddSymbol(new WixInternetShortcutSymbol(sourceLineNumbers, shortcutId)
            {
                ComponentRef = componentId,
                DirectoryRef = directoryId,
                Name = name,
                Target = target,
                Attributes = (int)type,
                IconFile = iconFile,
                IconIndex = iconIndex,
            });

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedInternetShortcuts", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

            // make sure we have a CreateFolder table so that the immediate CA can add temporary rows to handle installation and uninstallation
            this.ParseHelper.EnsureTable(section, sourceLineNumbers, "CreateFolder");

            // use built-in MSI functionality to remove the shortcuts rather than doing so via CA
            section.AddSymbol(new RemoveFileSymbol(sourceLineNumbers, shortcutId)
            {
                ComponentRef = componentId,
                DirPropertyRef = directoryId,
                OnUninstall = true,
                FileName = name,
            });
        }

        /// <summary>
        /// Parses a performance category element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParsePerformanceCategoryElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string name = null;
            string help = null;
            var multiInstance = YesNoType.No;
            int defaultLanguage = 0x09; // default to "english"

            var parsedPerformanceCounters = new List<ParsedPerformanceCounter>();

            // default to managed performance counter
            var library = "netfxperf.dll";
            var openEntryPoint = "OpenPerformanceData";
            var collectEntryPoint = "CollectPerformanceData";
            var closeEntryPoint = "ClosePerformanceData";

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Close":
                            closeEntryPoint = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Collect":
                            collectEntryPoint = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultLanguage":
                            defaultLanguage = this.GetPerformanceCounterLanguage(sourceLineNumbers, attrib);
                            break;
                        case "Help":
                            help = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Library":
                            library = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MultiInstance":
                            multiInstance = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Open":
                            openEntryPoint = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id && null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, element.Name.LocalName, "Id", "Name"));
            }
            else if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("upc", componentId, name);
            }
            else if (null == name)
            {
                name = id.Id;
            }

            // Process the child counter elements.
            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "PerformanceCounter":
                            var counter = this.ParsePerformanceCounterElement(intermediate, section, child, defaultLanguage);
                            if (null != counter)
                            {
                                parsedPerformanceCounters.Add(counter);
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }


            if (!this.Messaging.EncounteredError)
            {
                // Calculate the ini and h file content.
                var objectName = "OBJECT_1";
                var objectLanguage = defaultLanguage.ToString("D3", CultureInfo.InvariantCulture);

                var sbIniData = new StringBuilder();
                sbIniData.AppendFormat("[info]\r\ndrivername={0}\r\nsymbolfile=wixperf.h\r\n\r\n[objects]\r\n{1}_{2}_NAME=\r\n\r\n[languages]\r\n{2}=LANG{2}\r\n\r\n", name, objectName, objectLanguage);
                sbIniData.AppendFormat("[text]\r\n{0}_{1}_NAME={2}\r\n", objectName, objectLanguage, name);
                if (null != help)
                {
                    sbIniData.AppendFormat("{0}_{1}_HELP={2}\r\n", objectName, objectLanguage, help);
                }

                int symbolConstantsCounter = 0;
                var sbSymbolicConstants = new StringBuilder();
                sbSymbolicConstants.AppendFormat("#define {0}    {1}\r\n", objectName, symbolConstantsCounter);

                var sbCounterNames = new StringBuilder("[~]");
                var sbCounterTypes = new StringBuilder("[~]");
                for (int i = 0; i < parsedPerformanceCounters.Count; ++i)
                {
                    var counter = parsedPerformanceCounters[i];
                    var counterName = String.Concat("DEVICE_COUNTER_", i + 1);

                    sbIniData.AppendFormat("{0}_{1}_NAME={2}\r\n", counterName, counter.Language, counter.Name);
                    if (null != counter.Help)
                    {
                        sbIniData.AppendFormat("{0}_{1}_HELP={2}\r\n", counterName, counter.Language, counter.Help);
                    }

                    symbolConstantsCounter += 2;
                    sbSymbolicConstants.AppendFormat("#define {0}    {1}\r\n", counterName, symbolConstantsCounter);

                    sbCounterNames.Append(UtilCompiler.FindPropertyBrackets.Replace(counter.Name, this.EscapeProperties));
                    sbCounterNames.Append("[~]");
                    sbCounterTypes.Append(counter.Type);
                    sbCounterTypes.Append("[~]");
                }

                sbSymbolicConstants.AppendFormat("#define LAST_{0}_COUNTER_OFFSET    {1}\r\n", objectName, symbolConstantsCounter);

                // Add the calculated INI and H strings to the PerformanceCategory table.
                section.AddSymbol(new PerformanceCategorySymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Name = name,
                    IniData = sbIniData.ToString(),
                    ConstantData = sbSymbolicConstants.ToString(),
                });

                // Set up the application's performance key.
                var escapedName = UtilCompiler.FindPropertyBrackets.Replace(name, this.EscapeProperties);
                var linkageKey = String.Format(@"SYSTEM\CurrentControlSet\Services\{0}\Linkage", escapedName);
                var performanceKey = String.Format(@"SYSTEM\CurrentControlSet\Services\{0}\Performance", escapedName);

                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, linkageKey, "Export", escapedName, componentId, false);
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, performanceKey, "-", null, componentId, false);
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, performanceKey, "Library", library, componentId, false);
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, performanceKey, "Open", openEntryPoint, componentId, false);
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, performanceKey, "Collect", collectEntryPoint, componentId, false);
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, performanceKey, "Close", closeEntryPoint, componentId, false);
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, performanceKey, "IsMultiInstance", YesNoType.Yes == multiInstance ? "#1" : "#0", componentId, false);
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, performanceKey, "Counter Names", sbCounterNames.ToString(), componentId, false);
                this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, RegistryRootType.LocalMachine, performanceKey, "Counter Types", sbCounterTypes.ToString(), componentId, false);
            }

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4InstallPerfCounterData", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4UninstallPerfCounterData", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
        }

        /// <summary>
        /// Gets the performance counter language as a decimal number.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>Numeric representation of the language as per WinNT.h.</returns>
        private int GetPerformanceCounterLanguage(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            int language = 0;
            if (String.Empty == attribute.Value)
            {
                this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
            }
            else
            {
                switch (attribute.Value)
                {
                    case "afrikaans":
                        language = 0x36;
                        break;
                    case "albanian":
                        language = 0x1c;
                        break;
                    case "arabic":
                        language = 0x01;
                        break;
                    case "armenian":
                        language = 0x2b;
                        break;
                    case "assamese":
                        language = 0x4d;
                        break;
                    case "azeri":
                        language = 0x2c;
                        break;
                    case "basque":
                        language = 0x2d;
                        break;
                    case "belarusian":
                        language = 0x23;
                        break;
                    case "bengali":
                        language = 0x45;
                        break;
                    case "bulgarian":
                        language = 0x02;
                        break;
                    case "catalan":
                        language = 0x03;
                        break;
                    case "chinese":
                        language = 0x04;
                        break;
                    case "croatian":
                        language = 0x1a;
                        break;
                    case "czech":
                        language = 0x05;
                        break;
                    case "danish":
                        language = 0x06;
                        break;
                    case "divehi":
                        language = 0x65;
                        break;
                    case "dutch":
                        language = 0x13;
                        break;
                    case "piglatin":
                    case "english":
                        language = 0x09;
                        break;
                    case "estonian":
                        language = 0x25;
                        break;
                    case "faeroese":
                        language = 0x38;
                        break;
                    case "farsi":
                        language = 0x29;
                        break;
                    case "finnish":
                        language = 0x0b;
                        break;
                    case "french":
                        language = 0x0c;
                        break;
                    case "galician":
                        language = 0x56;
                        break;
                    case "georgian":
                        language = 0x37;
                        break;
                    case "german":
                        language = 0x07;
                        break;
                    case "greek":
                        language = 0x08;
                        break;
                    case "gujarati":
                        language = 0x47;
                        break;
                    case "hebrew":
                        language = 0x0d;
                        break;
                    case "hindi":
                        language = 0x39;
                        break;
                    case "hungarian":
                        language = 0x0e;
                        break;
                    case "icelandic":
                        language = 0x0f;
                        break;
                    case "indonesian":
                        language = 0x21;
                        break;
                    case "italian":
                        language = 0x10;
                        break;
                    case "japanese":
                        language = 0x11;
                        break;
                    case "kannada":
                        language = 0x4b;
                        break;
                    case "kashmiri":
                        language = 0x60;
                        break;
                    case "kazak":
                        language = 0x3f;
                        break;
                    case "konkani":
                        language = 0x57;
                        break;
                    case "korean":
                        language = 0x12;
                        break;
                    case "kyrgyz":
                        language = 0x40;
                        break;
                    case "latvian":
                        language = 0x26;
                        break;
                    case "lithuanian":
                        language = 0x27;
                        break;
                    case "macedonian":
                        language = 0x2f;
                        break;
                    case "malay":
                        language = 0x3e;
                        break;
                    case "malayalam":
                        language = 0x4c;
                        break;
                    case "manipuri":
                        language = 0x58;
                        break;
                    case "marathi":
                        language = 0x4e;
                        break;
                    case "mongolian":
                        language = 0x50;
                        break;
                    case "nepali":
                        language = 0x61;
                        break;
                    case "norwegian":
                        language = 0x14;
                        break;
                    case "oriya":
                        language = 0x48;
                        break;
                    case "polish":
                        language = 0x15;
                        break;
                    case "portuguese":
                        language = 0x16;
                        break;
                    case "punjabi":
                        language = 0x46;
                        break;
                    case "romanian":
                        language = 0x18;
                        break;
                    case "russian":
                        language = 0x19;
                        break;
                    case "sanskrit":
                        language = 0x4f;
                        break;
                    case "serbian":
                        language = 0x1a;
                        break;
                    case "sindhi":
                        language = 0x59;
                        break;
                    case "slovak":
                        language = 0x1b;
                        break;
                    case "slovenian":
                        language = 0x24;
                        break;
                    case "spanish":
                        language = 0x0a;
                        break;
                    case "swahili":
                        language = 0x41;
                        break;
                    case "swedish":
                        language = 0x1d;
                        break;
                    case "syriac":
                        language = 0x5a;
                        break;
                    case "tamil":
                        language = 0x49;
                        break;
                    case "tatar":
                        language = 0x44;
                        break;
                    case "telugu":
                        language = 0x4a;
                        break;
                    case "thai":
                        language = 0x1e;
                        break;
                    case "turkish":
                        language = 0x1f;
                        break;
                    case "ukrainian":
                        language = 0x22;
                        break;
                    case "urdu":
                        language = 0x20;
                        break;
                    case "uzbek":
                        language = 0x43;
                        break;
                    case "vietnamese":
                        language = 0x2a;
                        break;
                    default:
                        this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
                        break;
                }
            }

            return language;
        }

        /// <summary>
        /// Parses a performance counter element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="defaultLanguage">Default language for the performance counter.</param>
        private ParsedPerformanceCounter ParsePerformanceCounterElement(Intermediate intermediate, IntermediateSection section, XElement element, int defaultLanguage)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            ParsedPerformanceCounter parsedPerformanceCounter = null;
            string name = null;
            string help = null;
            var type = System.Diagnostics.PerformanceCounterType.NumberOfItems32;
            int language = defaultLanguage;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Help":
                            help = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            type = this.GetPerformanceCounterType(sourceLineNumbers, attrib);
                            break;
                        case "Language":
                            language = this.GetPerformanceCounterLanguage(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            if (null == help)
            {
                this.Messaging.Write(UtilWarnings.RequiredAttributeForWindowsXP(sourceLineNumbers, element.Name.LocalName, "Help"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                parsedPerformanceCounter = new ParsedPerformanceCounter(name, help, type, language);
            }

            return parsedPerformanceCounter;
        }

        /// <summary>
        /// Gets the performance counter type.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>Numeric representation of the language as per WinNT.h.</returns>
        private System.Diagnostics.PerformanceCounterType GetPerformanceCounterType(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            var type = System.Diagnostics.PerformanceCounterType.NumberOfItems32;
            if (String.Empty == attribute.Value)
            {
                this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
            }
            else
            {
                switch (attribute.Value)
                {
                    case "averageBase":
                        type = System.Diagnostics.PerformanceCounterType.AverageBase;
                        break;
                    case "averageCount64":
                        type = System.Diagnostics.PerformanceCounterType.AverageCount64;
                        break;
                    case "averageTimer32":
                        type = System.Diagnostics.PerformanceCounterType.AverageTimer32;
                        break;
                    case "counterDelta32":
                        type = System.Diagnostics.PerformanceCounterType.CounterDelta32;
                        break;
                    case "counterTimerInverse":
                        type = System.Diagnostics.PerformanceCounterType.CounterTimerInverse;
                        break;
                    case "sampleFraction":
                        type = System.Diagnostics.PerformanceCounterType.SampleFraction;
                        break;
                    case "timer100Ns":
                        type = System.Diagnostics.PerformanceCounterType.Timer100Ns;
                        break;
                    case "counterTimer":
                        type = System.Diagnostics.PerformanceCounterType.CounterTimer;
                        break;
                    case "rawFraction":
                        type = System.Diagnostics.PerformanceCounterType.RawFraction;
                        break;
                    case "timer100NsInverse":
                        type = System.Diagnostics.PerformanceCounterType.Timer100NsInverse;
                        break;
                    case "counterMultiTimer":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiTimer;
                        break;
                    case "counterMultiTimer100Ns":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiTimer100Ns;
                        break;
                    case "counterMultiTimerInverse":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiTimerInverse;
                        break;
                    case "counterMultiTimer100NsInverse":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiTimer100NsInverse;
                        break;
                    case "elapsedTime":
                        type = System.Diagnostics.PerformanceCounterType.ElapsedTime;
                        break;
                    case "sampleBase":
                        type = System.Diagnostics.PerformanceCounterType.SampleBase;
                        break;
                    case "rawBase":
                        type = System.Diagnostics.PerformanceCounterType.RawBase;
                        break;
                    case "counterMultiBase":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiBase;
                        break;
                    case "rateOfCountsPerSecond64":
                        type = System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64;
                        break;
                    case "rateOfCountsPerSecond32":
                        type = System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond32;
                        break;
                    case "countPerTimeInterval64":
                        type = System.Diagnostics.PerformanceCounterType.CountPerTimeInterval64;
                        break;
                    case "countPerTimeInterval32":
                        type = System.Diagnostics.PerformanceCounterType.CountPerTimeInterval32;
                        break;
                    case "sampleCounter":
                        type = System.Diagnostics.PerformanceCounterType.SampleCounter;
                        break;
                    case "counterDelta64":
                        type = System.Diagnostics.PerformanceCounterType.CounterDelta64;
                        break;
                    case "numberOfItems64":
                        type = System.Diagnostics.PerformanceCounterType.NumberOfItems64;
                        break;
                    case "numberOfItems32":
                        type = System.Diagnostics.PerformanceCounterType.NumberOfItems32;
                        break;
                    case "numberOfItemsHEX64":
                        type = System.Diagnostics.PerformanceCounterType.NumberOfItemsHEX64;
                        break;
                    case "numberOfItemsHEX32":
                        type = System.Diagnostics.PerformanceCounterType.NumberOfItemsHEX32;
                        break;
                    default:
                        this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
                        break;
                }
            }

            return type;
        }

        /// <summary>
        /// Parses a perf counter element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileId">Identifier of referenced file.</param>
        private void ParsePerfCounterElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string fileId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string name = null;

            this.Messaging.Write(UtilWarnings.DeprecatedPerfCounterElement(sourceLineNumbers));

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new PerfmonSymbol(sourceLineNumbers)
                {
                    ComponentRef = componentId,
                    File = $"[#{fileId}]",
                    Name = name,
                });
            }

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigurePerfmonInstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigurePerfmonUninstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
        }


        /// <summary>
        /// Parses a perf manifest element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileId">Identifier of referenced file.</param>
        private void ParsePerfCounterManifestElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string fileId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string resourceFileDirectory = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "ResourceFileDirectory":
                            resourceFileDirectory = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new PerfmonManifestSymbol(sourceLineNumbers)
                {
                    ComponentRef = componentId,
                    File = $"[#{fileId}]",
                    ResourceFileDirectory = resourceFileDirectory,
                });
            }

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigurePerfmonManifestRegister", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigurePerfmonManifestUnregister", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
        }

        /// <summary>
        /// Parses a format files element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="fileId">Identifier of referenced file.</param>
        /// <param name="win64">Flag to determine whether the component is 64-bit.</param>
        private void ParseFormatFileElement(Intermediate intermediate, IntermediateSection section, XElement element, string fileId, bool win64)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string binaryId = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "BinaryRef":
                            binaryId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (null == binaryId)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "BinaryRef"));
            }

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedFormatFiles", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

                section.AddSymbol(new WixFormatFilesSymbol(sourceLineNumbers)
                {
                    BinaryRef = binaryId,
                    FileRef = fileId,
                });

                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Binary, binaryId);
            }
        }

        /// <summary>
        /// Parses a event manifest element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileId">Identifier of referenced file.</param>
        private void ParseEventManifestElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string fileId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string messageFile = null;
            string resourceFile = null;
            string parameterFile = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "MessageFile":
                            messageFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ResourceFile":
                            resourceFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ParameterFile":
                            parameterFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new EventManifestSymbol(sourceLineNumbers)
                {
                    ComponentRef = componentId,
                    File = $"[#{fileId}]",
                });

                if (null != messageFile)
                {
                    section.AddSymbol(new XmlFileSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, $"Config_{fileId}MessageFile"))
                    {
                        File = $"[#{fileId}]",
                        ElementPath = "/*/*/*/*[\\[]@messageFileName[\\]]",
                        Name = "messageFileName",
                        Value = messageFile,
                        Flags = 4 | 0x00001000,  //bulk write | preserve modified date
                        ComponentRef = componentId,
                    });
                }
                if (null != parameterFile)
                {
                    section.AddSymbol(new XmlFileSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, $"Config_{fileId}ParameterFile"))
                    {
                        File = $"[#{fileId}]",
                        ElementPath = "/*/*/*/*[\\[]@parameterFileName[\\]]",
                        Name = "parameterFileName",
                        Value = parameterFile,
                        Flags = 4 | 0x00001000,  //bulk write | preserve modified date
                        ComponentRef = componentId,
                    });
                }
                if (null != resourceFile)
                {
                    section.AddSymbol(new XmlFileSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, $"Config_{fileId}ResourceFile"))
                    {
                        File = $"[#{fileId}]",
                        ElementPath = "/*/*/*/*[\\[]@resourceFileName[\\]]",
                        Name = "resourceFileName",
                        Value = resourceFile,
                        Flags = 4 | 0x00001000,  //bulk write | preserve modified date
                        ComponentRef = componentId,
                    });
                }

            }

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigureEventManifestRegister", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigureEventManifestUnregister", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

            if (null != messageFile || null != parameterFile || null != resourceFile)
            {
                this.AddReferenceToSchedXmlFile(sourceLineNumbers, section);
            }
        }

        /// <summary>
        /// Parses a PermissionEx element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="objectId">Identifier of object to be secured.</param>
        /// <param name="componentId">Identifier of component, used to determine install state.</param>
        /// <param name="win64">Flag to determine whether the component is 64-bit.</param>
        /// <param name="tableName">Name of table that contains objectId.</param>
        private void ParsePermissionExElement(Intermediate intermediate, IntermediateSection section, XElement element, string objectId, string componentId, bool win64, string tableName)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            var bits = new BitArray(32);
            string domain = null;
            string[] specialPermissions = null;
            string user = null;
            var attributes = WixPermissionExAttributes.Inheritable; // default to inheritable.

            var permissionType = PermissionType.SecureObjects;

            switch (tableName)
            {
                case "CreateFolder":
                    specialPermissions = UtilConstants.FolderPermissions;
                    break;
                case "File":
                    specialPermissions = UtilConstants.FilePermissions;
                    break;
                case "Registry":
                    specialPermissions = UtilConstants.RegistryPermissions;
                    if (String.IsNullOrEmpty(objectId))
                    {
                        this.Messaging.Write(UtilErrors.InvalidRegistryObject(sourceLineNumbers, element.Parent.Name.LocalName));
                    }
                    break;
                case "ServiceInstall":
                    specialPermissions = UtilConstants.ServicePermissions;
                    permissionType = PermissionType.SecureObjects;
                    break;
                default:
                    this.ParseHelper.UnexpectedElement(element.Parent, element);
                    break;
            }

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Domain":
                            if (PermissionType.FileSharePermissions == permissionType)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, element.Parent.Name.LocalName));
                            }
                            domain = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Inheritable":
                            if (this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) == YesNoType.No)
                            {
                                attributes &= ~WixPermissionExAttributes.Inheritable;
                            }
                            break;
                        case "User":
                            user = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            var attribValue = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (!this.TrySetBitFromName(UtilConstants.StandardPermissions, attrib.Name.LocalName, attribValue, bits, 16))
                            {
                                if (!this.TrySetBitFromName(UtilConstants.GenericPermissions, attrib.Name.LocalName, attribValue, bits, 28))
                                {
                                    if (!this.TrySetBitFromName(specialPermissions, attrib.Name.LocalName, attribValue, bits, 0))
                                    {
                                        this.ParseHelper.UnexpectedAttribute(element, attrib);
                                        break;
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            var permission = this.CreateIntegerFromBitArray(bits);

            if (null == user)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "User"));
            }

            if (Int32.MinValue == permission) // just GENERIC_READ, which is MSI_NULL
            {
                this.Messaging.Write(ErrorMessages.GenericReadNotAllowed(sourceLineNumbers));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedSecureObjects", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

                var id = this.ParseHelper.CreateIdentifier("sec", objectId, tableName, domain, user);
                section.AddSymbol(new SecureObjectsSymbol(sourceLineNumbers, id)
                {
                    SecureObject = objectId,
                    Table = tableName,
                    Domain = domain,
                    User = user,
                    Attributes = attributes,
                    Permission = permission,
                    ComponentRef = componentId,
                });
            }
        }

        /// <summary>
        /// Parses a ProductSearch element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        private void ParseProductSearchElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string productCode = null;
            string upgradeCode = null;
            var attributes = WixProductSearchAttributes.Version;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "ProductCode":
                            productCode = this.ParseHelper.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.ParseHelper.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Result":
                            var result = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (result)
                            {
                                case "version":
                                    attributes = WixProductSearchAttributes.Version;
                                    break;
                                case "language":
                                    attributes = WixProductSearchAttributes.Language;
                                    break;
                                case "state":
                                    attributes = WixProductSearchAttributes.State;
                                    break;
                                case "assignment":
                                    attributes = WixProductSearchAttributes.Assignment;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName, result, "version", "language", "state", "assignment"));
                                    break;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == upgradeCode && null == productCode)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "ProductCode", "UpgradeCode", true));
            }

            if (null != upgradeCode && null != productCode)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "UpgradeCode", "ProductCode"));
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("wps", variable, condition, after, (productCode == null ? upgradeCode : productCode), attributes.ToString());
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, element.Name.LocalName, id, variable, condition, after, null);

            if (!this.Messaging.EncounteredError)
            {
                // set an additional flag if this is an upgrade code
                if (null != upgradeCode)
                {
                    attributes |= WixProductSearchAttributes.UpgradeCode;
                }

                section.AddSymbol(new WixProductSearchSymbol(sourceLineNumbers, id)
                {
                    Guid = productCode ?? upgradeCode,
                    Attributes = attributes,
                });
            }
        }

        /// <summary>
        /// Parses a RegistrySearch element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        private void ParseRegistrySearchElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            RegistryRootType? root = null;
            string key = null;
            string value = null;
            var expand = YesNoType.NotSet;
            var win64 = this.Context.IsCurrentPlatform64Bit;
            var attributes = WixRegistrySearchAttributes.Raw | WixRegistrySearchAttributes.WantValue;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "Bitness":
                            var bitnessValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (bitnessValue)
                            {
                                case "always32":
                                    win64 = false;
                                    break;
                                case "always64":
                                    win64 = true;
                                    break;
                                case "default":
                                case "":
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, attrib.Name.LocalName, attrib.Name.LocalName, bitnessValue, "default", "always32", "always64"));
                                    break;
                            }
                            break;
                        case "Root":
                            root = this.ParseHelper.GetAttributeRegistryRootValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Key":
                            key = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ExpandEnvironmentVariables":
                            expand = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Format":
                            string format = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (format)
                            {
                                case "raw":
                                    attributes |= WixRegistrySearchAttributes.Raw;
                                    break;
                                case "compatible":
                                    attributes |= WixRegistrySearchAttributes.Compatible;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName, format, "raw", "compatible"));
                                    break;
                            }
                            break;
                        case "Result":
                            var result = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (result)
                            {
                                case "exists":
                                    attributes |= WixRegistrySearchAttributes.WantExists;
                                    break;
                                case "value":
                                    attributes |= WixRegistrySearchAttributes.WantValue;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName, result, "exists", "value"));
                                    break;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (!root.HasValue)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Root"));
            }

            if (null == key)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Key"));
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("wrs", variable, condition, after, root.ToString(), key, value, attributes.ToString());
            }

            if (expand == YesNoType.Yes)
            {
                if (0 != (attributes & WixRegistrySearchAttributes.WantExists))
                {
                    this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "ExpandEnvironmentVariables", expand.ToString(), "Result", "exists"));
                }

                attributes |= WixRegistrySearchAttributes.ExpandEnvironmentVariables;
            }

            if (win64)
            {
                attributes |= WixRegistrySearchAttributes.Win64;
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, element.Name.LocalName, id, variable, condition, after, null);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixRegistrySearchSymbol(sourceLineNumbers, id)
                {
                    Root = root.Value,
                    Key = key,
                    Value = value,
                    Attributes = attributes,
                });
            }
        }

        /// <summary>
        /// Parses a RemoveFolderEx element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseRemoveFolderExElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            var mode = WixRemoveFolderExInstallMode.Uninstall;
            string property = null;
            string condition = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Condition":
                            condition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "On":
                            var onValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (onValue.Length == 0)
                            {
                            }
                            else
                            {
                                switch (onValue)
                                {
                                    case "install":
                                        mode = WixRemoveFolderExInstallMode.Install;
                                        break;
                                    case "uninstall":
                                        mode = WixRemoveFolderExInstallMode.Uninstall;
                                        break;
                                    case "both":
                                        mode = WixRemoveFolderExInstallMode.Both;
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "On", onValue, "install", "uninstall", "both"));
                                        break;
                                }
                            }
                            break;
                        case "Property":
                            property = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (String.IsNullOrEmpty(property))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Property"));
            }

            if (id == null)
            {
                id = this.ParseHelper.CreateIdentifier("wrf", componentId, property, mode.ToString());
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4RemoveFoldersEx", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

                section.AddSymbol(new WixRemoveFolderExSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Property = property,
                    InstallMode = mode,
                    Condition = condition
                });

                this.ParseHelper.EnsureTable(section, sourceLineNumbers, "RemoveFile");
            }
        }

        /// <summary>
        /// Parses a RemoveRegistryKeyEx element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseRemoveRegistryKeyExElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            var mode = WixRemoveRegistryKeyExInstallMode.Uninstall;
            string condition = null;
            RegistryRootType? root = null;
            string key = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Condition":
                            condition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "On":
                            var actionValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (actionValue)
                            {
                                case "":
                                    break;
                                case "install":
                                    mode = WixRemoveRegistryKeyExInstallMode.Install;
                                    break;
                                case "uninstall":
                                    mode = WixRemoveRegistryKeyExInstallMode.Uninstall;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "On", actionValue, "install", "uninstall"));
                                    break;
                            }
                            break;
                        case "Root":
                            root = this.ParseHelper.GetAttributeRegistryRootValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Key":
                            key = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (!root.HasValue)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Root"));
            }

            if (key == null)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Key"));
            }

            if (id == null)
            {
                id = this.ParseHelper.CreateIdentifier("rrx", componentId, condition, root.ToString(), key, mode.ToString());
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.EnsureTable(section, sourceLineNumbers, "Registry");
                this.ParseHelper.EnsureTable(section, sourceLineNumbers, "RemoveRegistry");
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4RemoveRegistryKeysEx", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

                section.AddSymbol(new WixRemoveRegistryKeyExSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Root = root.Value,
                    Key = key,
                    InstallMode = mode,
                    Condition = condition
                });
            }
        }

        /// <summary>
        /// Parses a RestartResource element.
        /// </summary>
        /// <param name="element">The element to parse.</param>
        /// <param name="componentId">The identity of the parent component.</param>
        private void ParseRestartResourceElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string resource = null;
            WixRestartResourceAttributes? attributes = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;

                        case "Path":
                            resource = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes = WixRestartResourceAttributes.Filename;
                            break;

                        case "ProcessName":
                            resource = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes = WixRestartResourceAttributes.ProcessName;
                            break;

                        case "ServiceName":
                            resource = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes = WixRestartResourceAttributes.ServiceName;
                            break;

                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            // Validate the attribute.
            if (id == null)
            {
                id = this.ParseHelper.CreateIdentifier("wrr", componentId, resource, attributes.ToString());
            }

            if (!attributes.HasValue)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, element.Name.LocalName, "Path", "ProcessName", "ServiceName"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4RegisterRestartResources", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

                section.AddSymbol(new WixRestartResourceSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Resource = resource,
                    Attributes = attributes,
                });
            }
        }

        /// <summary>
        /// Parses a service configuration element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="parentTableName">Name of parent element.</param>
        /// <param name="parentTableServiceName">Optional name of service </param>
        private void ParseServiceConfigElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string parentTableName, string parentTableServiceName)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string firstFailureActionType = null;
            var newService = false;
            string programCommandLine = null;
            string rebootMessage = null;
            var resetPeriod = CompilerConstants.IntegerNotSet;
            var restartServiceDelay = CompilerConstants.IntegerNotSet;
            string secondFailureActionType = null;
            string serviceName = null;
            string thirdFailureActionType = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "FirstFailureActionType":
                            firstFailureActionType = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProgramCommandLine":
                            programCommandLine = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RebootMessage":
                            rebootMessage = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ResetPeriodInDays":
                            resetPeriod = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                            break;
                        case "RestartServiceDelayInSeconds":
                            restartServiceDelay = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                            break;
                        case "SecondFailureActionType":
                            secondFailureActionType = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ServiceName":
                            serviceName = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThirdFailureActionType":
                            thirdFailureActionType = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            // if this element is a child of ServiceInstall then ignore the service name provided.
            if ("ServiceInstall" == parentTableName)
            {
                // TODO: the ServiceName attribute should not be allowed in this case (the overwriting behavior may confuse users)
                serviceName = parentTableServiceName;
                newService = true;
            }
            else
            {
                // not a child of ServiceInstall, so ServiceName must have been provided
                if (null == serviceName)
                {
                    this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "ServiceName"));
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedServiceConfig", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

                section.AddSymbol(new ServiceConfigSymbol(sourceLineNumbers)
                {
                    ServiceName = serviceName,
                    ComponentRef = componentId,
                    NewService = newService ? 1 : 0,
                    FirstFailureActionType = firstFailureActionType,
                    SecondFailureActionType = secondFailureActionType,
                    ThirdFailureActionType = thirdFailureActionType,
                    ResetPeriodInDays = resetPeriod,
                    RestartServiceDelayInSeconds = restartServiceDelay,
                    ProgramCommandLine = programCommandLine,
                    RebootMessage = rebootMessage,
                });
            }
        }

        /// <summary>
        /// Parses a touch file element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="win64">Indicates whether the path is a 64-bit path.</param>
        private void ParseTouchFileElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, bool win64)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string path = null;
            var onInstall = YesNoType.NotSet;
            var onReinstall = YesNoType.NotSet;
            var onUninstall = YesNoType.NotSet;
            var nonvital = YesNoType.NotSet;
            int attributes = 0;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Path":
                            path = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "OnInstall":
                            onInstall = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "OnReinstall":
                            onReinstall = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "OnUninstall":
                            onUninstall = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Nonvital":
                            nonvital = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == path)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Path"));
            }

            // If none of the scheduling actions are set, default to touching on install and reinstall.
            if (YesNoType.NotSet == onInstall && YesNoType.NotSet == onReinstall && YesNoType.NotSet == onUninstall)
            {
                onInstall = YesNoType.Yes;
                onReinstall = YesNoType.Yes;
            }

            attributes |= YesNoType.Yes == onInstall ? 0x1 : 0;
            attributes |= YesNoType.Yes == onReinstall ? 0x2 : 0;
            attributes |= YesNoType.Yes == onUninstall ? 0x4 : 0;
            attributes |= win64 ? 0x10 : 0;
            attributes |= YesNoType.Yes == nonvital ? 0 : 0x20;

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("tf", path, attributes.ToString());
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixTouchFileSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Path = path,
                    Attributes = attributes,
                });

                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4TouchFileDuringInstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            }
        }

        /// <summary>
        /// Parses an user element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Optional identifier of parent component.</param>
        private void ParseUserElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int attributes = 0;
            string domain = null;
            string name = null;
            string comment = null;
            string password = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "CanNotChangePassword":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserPasswdCantChange;
                            }
                            break;
                        case "Comment":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            comment = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CreateUser":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.No == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserDontCreateUser;
                            }
                            break;
                        case "Disabled":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserDisableAccount;
                            }
                            break;
                        case "Domain":
                            domain = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "FailIfExists":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserFailIfExists;
                            }
                            break;
                        case "LogonAsService":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserLogonAsService;
                            }
                            break;
                        case "LogonAsBatchJob":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserLogonAsBatchJob;
                            }
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Password":
                            password = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PasswordExpired":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserPasswdChangeReqdOnLogin;
                            }
                            break;
                        case "PasswordNeverExpires":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserDontExpirePasswrd;
                            }
                            break;
                        case "RemoveOnUninstall":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.No == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserDontRemoveOnUninstall;
                            }
                            break;
                        case "UpdateIfExists":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserUpdateIfExists;
                            }
                            break;
                        case "Vital":
                            if (null == componentId)
                            {
                                this.Messaging.Write(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.No == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserNonVital;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("usr", componentId, name);
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "GroupRef":
                            if (null == componentId)
                            {
                                var childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
                                this.Messaging.Write(UtilErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseGroupRefElement(intermediate, section, child, id.Id);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }

            if (null != componentId)
            {
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigureUsers", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            }

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new UserSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Name = name,
                    Domain = domain,
                    Password = password,
                    Comment = comment,
                    Attributes = attributes,
                });
            }
        }

        /// <summary>
        /// Parses a XmlFile element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseXmlFileElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string file = null;
            string elementPath = null;
            string name = null;
            string value = null;
            int sequence = -1;
            int flags = 0;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Action":
                            var actionValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (actionValue)
                            {
                                case "createElement":
                                    flags |= 0x00000001; // XMLFILE_CREATE_ELEMENT
                                    break;
                                case "deleteValue":
                                    flags |= 0x00000002; // XMLFILE_DELETE_VALUE
                                    break;
                                case "bulkSetValue":
                                    flags |= 0x00000004; // XMLFILE_BULKWRITE_VALUE
                                    break;
                                case "setValue":
                                    // no flag for set value since it's the default
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Action", actionValue, "createElement", "deleteValue", "setValue", "bulkSetValue"));
                                    break;
                            }
                            break;
                        case "SelectionLanguage":
                            string selectionLanguage = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (selectionLanguage)
                            {
                                case "XPath":
                                    flags |= 0x00000100; // XMLFILE_USE_XPATH
                                    break;
                                case "XSLPattern":
                                    // no flag for since it's the default
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "SelectionLanguage", selectionLanguage, "XPath", "XSLPattern"));
                                    break;
                            }
                            break;
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            file = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ElementPath":
                            elementPath = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Permanent":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 0x00010000; // XMLFILE_DONT_UNINSTALL
                            }
                            break;
                        case "Sequence":
                            sequence = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
                            break;
                        case "Value":
                            value = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PreserveModifiedDate":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 0x00001000; // XMLFILE_PRESERVE_MODIFIED
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("uxf", componentId, file, elementPath, name);
            }

            if (null == file)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "File"));
            }

            if (null == elementPath)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "ElementPath"));
            }

            if ((0x00000001 /*XMLFILE_CREATE_ELEMENT*/ & flags) != 0 && null == name)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, element.Name.LocalName, "Action", "Name"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new XmlFileSymbol(sourceLineNumbers, id)
                {
                    File = file,
                    ElementPath = elementPath,
                    Name = name,
                    Value = value,
                    Flags = flags,
                    ComponentRef = componentId,
                });
                if (-1 != sequence)
                {
                    symbol.Sequence = sequence;
                }
            }

            this.AddReferenceToSchedXmlFile(sourceLineNumbers, section);
        }

        /// <summary>
        /// Parses a XmlConfig element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="nested">Whether or not the element is nested.</param>
        private void ParseXmlConfigElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, bool nested)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string elementId = null;
            string elementPath = null;
            int flags = 0;
            string file = null;
            string name = null;
            var sequence = CompilerConstants.IntegerNotSet;
            string value = null;
            string verifyPath = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            if (nested)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, element.Parent.Name.LocalName));
                            }
                            else
                            {
                                string actionValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                                switch (actionValue)
                                {
                                    case "create":
                                        flags |= 0x10; // XMLCONFIG_CREATE
                                        break;
                                    case "delete":
                                        flags |= 0x20; // XMLCONFIG_DELETE
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, actionValue, "create", "delete"));
                                        break;
                                }
                            }
                            break;
                        case "ElementId":
                            elementId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ElementPath":
                            elementPath = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            file = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Node":
                            if (nested)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, element.Parent.Name.LocalName));
                            }
                            else
                            {
                                var nodeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                                switch (nodeValue)
                                {
                                    case "element":
                                        flags |= 0x1; // XMLCONFIG_ELEMENT
                                        break;
                                    case "value":
                                        flags |= 0x2; // XMLCONFIG_VALUE
                                        break;
                                    case "document":
                                        flags |= 0x4; // XMLCONFIG_DOCUMENT
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, nodeValue, "element", "value", "document"));
                                        break;
                                }
                            }
                            break;
                        case "On":
                            if (nested)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, element.Parent.Name.LocalName));
                            }
                            else
                            {
                                var onValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                                switch (onValue)
                                {
                                    case "install":
                                        flags |= 0x100; // XMLCONFIG_INSTALL
                                        break;
                                    case "uninstall":
                                        flags |= 0x200; // XMLCONFIG_UNINSTALL
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, onValue, "install", "uninstall"));
                                        break;
                                }
                            }
                            break;
                        case "PreserveModifiedDate":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 0x00001000; // XMLCONFIG_PRESERVE_MODIFIED
                            }
                            break;
                        case "Sequence":
                            sequence = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
                            break;
                        case "Value":
                            value = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "VerifyPath":
                            verifyPath = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("uxc", componentId, file, elementId, elementPath);
            }

            if (null == file)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "File"));
            }

            if (null == elementId && null == elementPath)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, element.Name.LocalName, "ElementId", "ElementPath"));
            }
            else if (null != elementId)
            {
                if (null != elementPath)
                {
                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "ElementId", "ElementPath"));
                }

                if (0 != flags)
                {
                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, "ElementId", "Action", "Node", "On"));
                }

                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, UtilSymbolDefinitions.XmlConfig, elementId);
            }

            // find unexpected child elements
            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "XmlConfig":
                            if (nested)
                            {
                                this.Messaging.Write(ErrorMessages.UnexpectedElement(sourceLineNumbers, element.Name.LocalName, child.Name.LocalName));
                            }
                            else
                            {
                                this.ParseXmlConfigElement(intermediate, section, child, componentId, true);
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new XmlConfigSymbol(sourceLineNumbers, id)
                {
                    File = file,
                    ElementId = elementId,
                    ElementPath = elementPath,
                    VerifyPath = verifyPath,
                    Name = name,
                    Value = value,
                    Flags = flags,
                    ComponentRef = componentId,
                });

                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    symbol.Sequence = sequence;
                }
            }

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedXmlConfig", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
        }

        /// <summary>
        /// Match evaluator to escape properties in a string.
        /// </summary>
        private string EscapeProperties(Match match)
        {
            string escape = null;
            switch (match.Value)
            {
                case "[":
                    escape = @"[\[]";
                    break;
                case "]":
                    escape = @"[\]]";
                    break;
            }

            return escape;
        }

        private int CreateIntegerFromBitArray(BitArray bits)
        {
            if (32 != bits.Length)
            {
                throw new ArgumentException(String.Format("Can only convert a bit array with 32-bits to integer. Actual number of bits in array: {0}", bits.Length), "bits");
            }

            var intArray = new int[1];
            bits.CopyTo(intArray, 0);

            return intArray[0];
        }

        private bool TrySetBitFromName(string[] attributeNames, string attributeName, YesNoType attributeValue, BitArray bits, int offset)
        {
            for (var i = 0; i < attributeNames.Length; i++)
            {
                if (attributeName.Equals(attributeNames[i], StringComparison.Ordinal))
                {
                    bits.Set(i + offset, YesNoType.Yes == attributeValue);
                    return true;
                }
            }

            return false;
        }

        private void AddReferenceToSchedXmlFile(SourceLineNumber sourceLineNumbers, IntermediateSection section)
        {
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedXmlFile", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
        }

        /// <summary>
        /// Private class that stores the data from a parsed PerformanceCounter element.
        /// </summary>
        private class ParsedPerformanceCounter
        {
            internal ParsedPerformanceCounter(string name, string help, System.Diagnostics.PerformanceCounterType type, int language)
            {
                this.Name = name;
                this.Help = help;
                this.Type = (int)type;
                this.Language = language.ToString("D3", CultureInfo.InvariantCulture);
            }

            internal string Name { get; }

            internal string Help { get; }

            internal int Type { get; }

            internal string Language { get; }
        }
    }
}
