using Microsoft.CodeAnalysis;
using System.Composition;
using System.IO;

namespace Microsoft.DotNetCore.CodeFormatting.Filters
{
    [Export(typeof(IFormattingFilter))]
    internal sealed class UsableFileFilter : IFormattingFilter
    {
        public bool ShouldBeProcessed(Document document)
        {
            if (document.FilePath == null)
            {
                return true;
            }

            var fileInfo = new FileInfo(document.FilePath);
            if (!fileInfo.Exists || fileInfo.IsReadOnly)
            {
                Options.FormatLogger.WriteLine("warning: skipping document '{0}' because it {1}.",
                    document.FilePath,
                    fileInfo.IsReadOnly ? "is read-only" : "does not exist");
                return false;
            }

            return true;
        }
    }
}
