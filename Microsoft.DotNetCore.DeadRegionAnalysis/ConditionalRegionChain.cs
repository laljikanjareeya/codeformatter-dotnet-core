using System;
using System.Collections.Immutable;

namespace Microsoft.DotNetCore.DeadRegionAnalysis
{
    public struct ConditionalRegionChain : IComparable<ConditionalRegionChain>
    {
        private ImmutableArray<ConditionalRegion> _regions;

        public bool IsDefault { get { return _regions == null; } }

        public ImmutableArray<ConditionalRegion> Regions { get { return _regions; } }

        public int SpanStart { get { return _regions != null ? _regions[0].SpanStart : -1; } }

        public int SpanEnd { get { return _regions != null ? _regions[_regions.Length - 1].SpanEnd : -1; } }

        internal ConditionalRegionChain(ImmutableArray<ConditionalRegion> regions)
        {
            if (regions.IsDefaultOrEmpty)
            {
                throw new ArgumentException("regions");
            }

            _regions = regions;
        }

        public int CompareTo(ConditionalRegionChain other)
        {
            int result = IsDefault.CompareTo(other.IsDefault);

            if (result == 0)
            {
                result = Regions.Length - other.Regions.Length;
                if (result == 0)
                {
                    result = SpanStart - other.SpanStart;
                    if (result == 0)
                    {
                        return SpanEnd - other.SpanEnd;
                    }
                }
            }

            return result;
        }
    }
}
