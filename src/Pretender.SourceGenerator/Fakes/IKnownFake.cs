using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Parser;

namespace Pretender.SourceGenerator.Fakes
{
    internal interface IKnownFake
    {
        bool TryConstruct(INamedTypeSymbol typeSymbol, KnownTypeSymbols knownTypeSymbols, CancellationToken cancellationToken, out INamedTypeSymbol? fakeType);
    }
}