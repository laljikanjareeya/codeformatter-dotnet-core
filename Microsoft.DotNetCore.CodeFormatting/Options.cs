using System.Collections.Immutable;

namespace Microsoft.DotNetCore.CodeFormatting
{
    /// <summary>
    /// This is a MEF importable type which contains all of the options for formatting
    /// </summary>
    static internal class Options
    {
        /// <summary>
        /// Gets or sets the copyright header.
        /// </summary>
        /// <value>
        /// The copyright header.
        /// </value>
        static internal ImmutableArray<string> CopyrightHeader { get; set; } = FormattingDefaults.DefaultCopyrightHeader;
        /// <summary>
        /// Gets or sets the preprocessor configurations.
        /// </summary>
        /// <value>
        /// The preprocessor configurations.
        /// </value>
        static internal ImmutableArray<string[]> PreprocessorConfigurations { get; set; } = ImmutableArray<string[]>.Empty;

        /// <summary>
        /// When non-empty the formatter will only process files with the specified name.
        /// </summary>
        static internal ImmutableArray<string> FileNames { get; set; } = ImmutableArray<string>.Empty;
        /// <summary>
        /// Gets or sets the format logger.
        /// </summary>
        /// <value>
        /// The format logger.
        /// </value>
        static internal IFormatLogger FormatLogger { get; set; } = new ConsoleFormatLogger();
    }
}
