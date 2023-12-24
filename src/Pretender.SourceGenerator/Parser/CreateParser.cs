using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Invocation;

namespace Pretender.SourceGenerator.Parser
{
    internal class CreateParser
    {
        private readonly CreateInvocation _createInvocation;
        private readonly ImmutableArray<InterceptsLocationInfo> _locations;
        private readonly int _index;
        private readonly KnownTypeSymbols _knownTypeSymbols;

        public CreateParser(CreateInvocation createInvocation, ImmutableArray<InterceptsLocationInfo> locations, int index, KnownTypeSymbols knownTypeSymbols)
        {
            _createInvocation = createInvocation;
            _locations = locations;
            _index = index;
            _knownTypeSymbols = knownTypeSymbols;
        }

        public (CreateEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics) Parse(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // TODO: Do deeper introspection to make sure the supplied args match with supplied type arguments
            // and we should provide the constructor to use to the emitter maybe
            var emitter = new CreateEmitter(
                _createInvocation.Operation,
                _knownTypeSymbols,
                _createInvocation.TypeArguments,
                _locations,
                _index);

            return (emitter, null);
        }
    }
}
