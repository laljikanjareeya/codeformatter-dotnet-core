using Microsoft.CodeAnalysis;
using System;
using System.Composition;

namespace Microsoft.DotNetCore.CodeFormatting.Filters
{
    [Export(typeof(IFormattingFilter))]
    internal sealed class IgnoreGeneratedFilesFilter : IFormattingFilter
    {
        public bool ShouldBeProcessed(Document document)
        {
            if (document.FilePath == null)
            {
                return true;
            }

            if (document.FilePath.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
                document.FilePath.EndsWith(".Generated.cs", StringComparison.OrdinalIgnoreCase) ||
                document.FilePath.EndsWith(".Designer.vb", StringComparison.OrdinalIgnoreCase) ||
                document.FilePath.EndsWith(".Generated.vb", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
