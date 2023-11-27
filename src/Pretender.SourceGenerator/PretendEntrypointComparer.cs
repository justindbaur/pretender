using Microsoft.CodeAnalysis;

namespace Pretender.SourceGenerator
{
    internal class PretendEntrypointComparer : IEqualityComparer<PretendEntrypoint>
    {
        public static readonly PretendEntrypointComparer TypeSymbol = new PretendEntrypointComparer();

        public bool Equals(PretendEntrypoint x, PretendEntrypoint y)
        {
            return SymbolEqualityComparer.Default.Equals(x.TypeToPretend, y.TypeToPretend);
        }

        public int GetHashCode(PretendEntrypoint obj)
        {
            return SymbolEqualityComparer.Default.GetHashCode(obj.TypeToPretend);
        }
    }
}
