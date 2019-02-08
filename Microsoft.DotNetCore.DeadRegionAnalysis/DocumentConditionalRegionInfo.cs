using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Microsoft.DotNetCore.DeadRegionAnalysis
{
    public class DocumentConditionalRegionInfo : IComparable<DocumentConditionalRegionInfo>, IEquatable<DocumentConditionalRegionInfo>
    {
        public Document Document { get; private set; }

        public ImmutableArray<ConditionalRegionChain> Chains { get; private set; }

        internal DocumentConditionalRegionInfo(Document document, ImmutableArray<ConditionalRegionChain> chains)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            if (chains.IsDefault)
            {
                throw new ArgumentException("chains");
            }

            Document = document;
            Chains = chains;
        }

        public int CompareTo(DocumentConditionalRegionInfo other)
        {
            if (other == null)
            {
                return 1;
            }

            int result = string.Compare(Document.FilePath, other.Document.FilePath, StringComparison.InvariantCultureIgnoreCase);
            if (result == 0)
            {
                return Chains.Length - other.Chains.Length;
            }

            return result;
        }

        public bool Equals(DocumentConditionalRegionInfo other)
        {
            return CompareTo(other) == 0;
        }
    }
}
