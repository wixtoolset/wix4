// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition User = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.User.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(UserSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserSymbolFields.Domain), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserSymbolFields.Password), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserSymbolFields.Comment), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(UserSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum UserSymbolFields
    {
        ComponentRef,
        Name,
        Domain,
        Password,
        Comment,
        Attributes,
    }

    public class UserSymbol : IntermediateSymbol
    {
        public UserSymbol() : base(UtilSymbolDefinitions.User, null, null)
        {
        }

        public UserSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.User, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UserSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)UserSymbolFields.ComponentRef].AsString();
            set => this.Set((int)UserSymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)UserSymbolFields.Name].AsString();
            set => this.Set((int)UserSymbolFields.Name, value);
        }

        public string Domain
        {
            get => this.Fields[(int)UserSymbolFields.Domain].AsString();
            set => this.Set((int)UserSymbolFields.Domain, value);
        }

        public string Password
        {
            get => this.Fields[(int)UserSymbolFields.Password].AsString();
            set => this.Set((int)UserSymbolFields.Password, value);
        }

        public string Comment
        {
            get => this.Fields[(int)UserSymbolFields.Comment].AsString();
            set => this.Set((int)UserSymbolFields.Comment, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)UserSymbolFields.Attributes].AsNumber();
            set => this.Set((int)UserSymbolFields.Attributes, value);
        }
    }
}