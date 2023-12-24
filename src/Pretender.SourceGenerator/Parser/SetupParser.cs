using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Invocation;

namespace Pretender.SourceGenerator.Parser
{
    internal class SetupParser
    {
        private readonly SetupInvocation _setupInvocation;
        private readonly KnownTypeSymbols _knownTypeSymbols;

        public SetupParser(SetupInvocation setupInvocation, KnownTypeSymbols knownTypeSymbols)
        {
            _setupInvocation = setupInvocation;
            _knownTypeSymbols = knownTypeSymbols;
        }

        public (SetupEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics) Parse(CancellationToken cancellationToken)
        {
            var operation = _setupInvocation.Operation;

            // Setup calls are expected to have a single argument, being the setup action argument
            var setupArgument = operation.Arguments[0];

            cancellationToken.ThrowIfCancellationRequested();

            // Setup calls are expected to be called from Pretend<T> so the type argument gives us the type we are pretending
            // TODO: Assert the containing type maybe?
            // This should be a safe cast
            var pretendType = (INamedTypeSymbol)operation.TargetMethod.ContainingType.TypeArguments[0];

            var useSetMethod = operation.TargetMethod.Name == "SetupSet";

            var parser = new SetupActionParser(setupArgument.Value, pretendType, useSetMethod, _knownTypeSymbols);

            var (setupActionEmitter, setupActionDiagnostics) = parser.Parse(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (setupActionEmitter == null)
            {
                return (null, setupActionDiagnostics);
            }

            return (new SetupEmitter(setupActionEmitter, operation), setupActionDiagnostics);
        }
    }
}
