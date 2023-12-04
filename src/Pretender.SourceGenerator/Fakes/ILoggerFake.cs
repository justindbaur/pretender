using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Parser;

namespace Pretender.SourceGenerator.Fakes
{
    internal class ILoggerFake : IKnownFake
    {
        public bool TryConstruct(INamedTypeSymbol typeSymbol, KnownTypeSymbols knownTypeSymbols, CancellationToken cancellationToken, out ITypeSymbol? fakeType)
        {
            fakeType = null;
            if (SymbolEqualityComparer.Default.Equals(typeSymbol, knownTypeSymbols.MicrosoftExtensionsLoggingILogger))
            {
                // TODO: Support this
            }

            return false;
        }
    }
}
