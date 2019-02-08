// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.DotNetCore.CodeFormatting.Tests
{
    public sealed class CopyrightHeaderRuleTests : SyntaxRuleTestBase
    {
        internal override ISyntaxFormattingRule Rule
        {
            get { return new Rules.CopyrightHeaderRule(); }
        }

        [Fact]
        public void CSharpSimple()
        {
            Options.CopyrightHeader = ImmutableArray.Create("test");
            var source = @"
class C
{
}";

            var expected = @"// test

class C
{
}";
            Verify(source, expected);
        }

        [Fact]
        public void CSharpSimpleMultiline()
        {
            Options.CopyrightHeader = ImmutableArray.Create("test1", "test2");
            var source = @"
class C
{
}";

            var expected = @"// test1
// test2

class C
{
}";
            Verify(source, expected);
        }

        [Fact]
        public void CSharpPreserveExisting()
        {
            Options.CopyrightHeader= ImmutableArray.Create("test");
            var source = @"// test

class C
{
}";

            var expected = @"// test

class C
{
}";
            Verify(source, expected);
        }

        [Fact]
        public void CSharpPreserveExistingMultiline()
        {
            Options.CopyrightHeader = ImmutableArray.Create("test1", "test2");
            var source = @"// test1
// test2

class C
{
}";

            var expected = @"// test1
// test2

class C
{
}";
            Verify(source, expected);
        }

        [Fact]
        public void CSharpPreserveExistingWithCommentMultiline()
        {
            Options.CopyrightHeader = ImmutableArray.Create("test1", "test2");
            var source = @"// test1
// test2





// test3


class C
{
}";

            var expected = @"// test1
// test2





// test3


class C
{
}";
            Verify(source, expected);
        }

        [Fact]
        public void CSharpDontDoubleComment()
        {
            Options.CopyrightHeader = ImmutableArray.Create("// test");
            var source = @"
class C
{
}";

            var expected = @"// test

class C
{
}";
            Verify(source, expected);
        }

        [Fact]
        public void CSharpRemoveOlder()
        {
            Options.CopyrightHeader = ImmutableArray.Create("test");
            var source = @"// copyright

class C
{
}";

            var expected = @"// test

class C
{
}";
            Verify(source, expected);
        }

        [Fact]
        public void CSharpHeaderBeginsWithTargetHeader()
        {
            Options.CopyrightHeader = ImmutableArray.Create("// test", "// test2");
            var source = @"// test
// test2
// file summary

class C
{
}";

            var expected = @"// test
// test2
// file summary

class C
{
}";
            Verify(source, expected);
        }

        [Fact]
        public void VisualBasicSimple()
        {
            Options.CopyrightHeader = ImmutableArray.Create("test");
            var source = @"
Public Class C
End Class";

            var expected = @"' test

Public Class C
End Class";

            Verify(source, expected, languageName: LanguageNames.VisualBasic);
        }

        [Fact]
        public void VisualBasicNormalizeComment()
        {
            Options.CopyrightHeader = ImmutableArray.Create("// test");
            var source = @"
Public Class C
End Class";

            var expected = @"' test

Public Class C
End Class";

            Verify(source, expected, languageName: LanguageNames.VisualBasic);
        }

        [Fact]
        public void VisualBasicPreserveExisting()
        {
            Options.CopyrightHeader = ImmutableArray.Create("// test");
            var source = @"' test

Public Class C
End Class";

            var expected = @"' test

Public Class C
End Class";

            Verify(source, expected, languageName: LanguageNames.VisualBasic);
        }
    }
}
