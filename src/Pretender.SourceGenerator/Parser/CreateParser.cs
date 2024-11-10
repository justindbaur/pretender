using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Invocation;

namespace Pretender.SourceGenerator.Parser
{
    internal class CreateParser
    {
        private readonly CreateInvocation _createInvocation;
#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly ImmutableArray<InterceptableLocation> _locations;
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly int _index;
        private readonly KnownTypeSymbols _knownTypeSymbols;

#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public CreateParser(CreateInvocation createInvocation, ImmutableArray<InterceptableLocation> locations, int index, KnownTypeSymbols knownTypeSymbols)
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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