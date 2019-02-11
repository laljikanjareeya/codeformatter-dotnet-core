using Microsoft.CodeAnalysis;
using System;
using System.Composition;
using System.IO;

namespace Microsoft.DotNetCore.CodeFormatting.Filters
{
    [Export(typeof(IFormattingFilter))]
    internal sealed class FilenameFilter : IFormattingFilter
    {
        private readonly Options _options;
        [ImportingConstructor]
        public FilenameFilter(Options options)
        {
            _options = options;
        }

        public bool ShouldBeProcessed(Document document)
        {
            var fileNames = _options.FileNames;
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
