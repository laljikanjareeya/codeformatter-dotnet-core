using Microsoft.CodeAnalysis;
using System;
using System.Composition;
using System.IO;

namespace Microsoft.DotNetCore.CodeFormatting.Filters
{
    [Export(typeof(IFormattingFilter))]
    internal sealed class FilenameFilter : IFormattingFilter
    {
        public bool ShouldBeProcessed(Document document)
        {
            var fileNames = Options.FileNames;
            if (fileNames.IsDefaultOrEmpty)
            {
                return true;
            }

            string docFilename = Path.GetFileName(document.FilePath);
            foreach (var filename in fileNames)
            {
                if (filename.Equals(docFilename, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
