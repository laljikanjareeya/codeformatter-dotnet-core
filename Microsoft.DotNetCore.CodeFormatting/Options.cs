using System.Collections.Immutable;
using System.Composition;

namespace Microsoft.DotNetCore.CodeFormatting
{
    /// <summary>
    /// This is a MEF importable type which contains all of the options for formatting
    /// </summary>
    [Export(typeof(Options)), Shared]
    internal sealed class Options
    {
        [ImportingConstructor]
        public Options()
        { }

        /// <summary>
        /// Gets or sets the copyright header.
        /// </summary>
        /// <value>
        /// The copyright header.
        /// </value>
        internal ImmutableArray<string> CopyrightHeader { get; set; } = FormattingDefaults.DefaultCopyrightHeader;
        /// <summary>
        /// Gets or sets the preprocessor configurations.
        /// </summary>
        /// <value>
        /// The preprocessor configurations.
        /// </value>
        internal ImmutableArray<string[]> PreprocessorConfigurations { get; set; } = ImmutableArray<string[]>.Empty;

        /// <summary>
        /// When non-empty the formatter will only process files with the specified name.
        /// </summary>
        internal ImmutableArray<string> FileNames { get; set; } = ImmutableArray<string>.Empty;
        /// <summary>
        /// Gets or sets the format logger.
        /// </summary>
        /// <value>
        /// The format logger.
        /// </value>
        internal IFormatLogger FormatLogger { get; set; } = new ConsoleFormatLogger();
    }
}
