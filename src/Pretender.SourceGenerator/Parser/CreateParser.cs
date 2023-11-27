using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Emitter;
using static Pretender.SourceGenerator.PretenderSourceGenerator;

namespace Pretender.SourceGenerator.Parser
{
    internal class CreateParser
    {
        private readonly CreateInvocation _createInvocation;
        private readonly ImmutableArray<InterceptsLocationInfo> _locations;
        private readonly int _index;
        private readonly bool _isLanguageVersionSupported;
        private readonly KnownTypeSymbols _knownTypeSymbols;

        public CreateParser(CreateInvocation createInvocation, ImmutableArray<InterceptsLocationInfo> locations, int index, CompilationData compilationData)
        {
            _createInvocation = createInvocation;
            _locations = locations;
            _index = index;
            _isLanguageVersionSupported = compilationData.LanguageVersionIsSupported;
            _knownTypeSymbols = compilationData.TypeSymbols!;
        }

        public (CreateEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics) Parse(CancellationToken cancellationToken)
        {
            if (!_isLanguageVersionSupported)
            {
                return (null, ImmutableArray.Create(Diagnostic.Create(DiagnosticDescriptors.UnsupportedLanguageVersion, null)));
            }

            // TODO:Do deeper introspection to make sure the supplied args match with supplied type arguments
            // and we should provide the constructor to use to the emitter maybe
            var emitter = new CreateEmitter(
                _createInvocation.Operation,
                _createInvocation.TypeArguments,
                _locations,
                _index);

            return (emitter, null);
        }
    }
}
