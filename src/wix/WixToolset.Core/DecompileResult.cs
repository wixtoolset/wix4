// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{ 
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    internal class DecompileResult : IDecompileResult
    {
        public XDocument Document { get; set; }

        public IReadOnlyCollection<string> ExtractedFilePaths { get; set; }

        public Platform? Platform { get; set; }
    }
}
