using System.ComponentModel;

namespace Microsoft.DotNetCore.CodeFormatting
{
    public interface IRuleMetadata
    {
        [DefaultValue("")]
        string Name { get; }

        [DefaultValue("")]
        string Description { get; }

        [DefaultValue(int.MaxValue)]
        int Order { get; }

        [DefaultValue(true)]
        bool DefaultRule { get; }
    }
}
