using Microsoft.CodeAnalysis;

namespace Microsoft.DotNetCore.CodeFormatting
{
    internal interface IFormattingFilter
    {
        bool ShouldBeProcessed(Document document);
    }
}
