using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Invocation;

namespace Pretender.SourceGenerator.Parser
{
    internal class VerifyParser
    {
        private readonly VerifyInvocation _verifyInvocation;
        private readonly KnownTypeSymbols _knownTypeSymbols;

        public VerifyParser(VerifyInvocation verifyInvocation, KnownTypeSymbols knownTypeSymbols)
        {
            _verifyInvocation = verifyInvocation;
            _knownTypeSymbols = knownTypeSymbols;
        }

        public (VerifyEmitter? VerifyEmitter, ImmutableArray<Diagnostic>? Diagnostics) Parse(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var operation = _verifyInvocation.Operation;

            // Verify calls are expected to have 2 arguments, the first being the setup expression
            var setupArgument = operation.Arguments[0];

            cancellationToken.ThrowIfCancellationRequested();

            // Verify calls are expected to be called from Pretend<T> so the type argument gives us the type we are pretending
            var pretendType = operation.TargetMethod.ContainingType.TypeArguments[0];

            // TODO: This doesn't exist yet
            var useSetMethod = operation.TargetMethod.Name == "VerifySet";

            var parser = new SetupActionParser(setupArgument.Value, pretendType, useSetMethod, _knownTypeSymbols);

            var (setupActionEmitter, setupActionDiagnostics) = parser.Parse(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (setupActionEmitter == null)
            {
                return (null, setupActionDiagnostics);
            }

            var returnType = setupArgument.Parameter!.Type.Name == "Func"
                ? ((INamedTypeSymbol)setupArgument.Parameter.Type).TypeArguments[1] // The Func variant is expected to have the return type in the second type argument
                : null;

            cancellationToken.ThrowIfCancellationRequested();

            var emitter = new VerifyEmitter(pretendType, returnType, setupActionEmitter, _verifyInvocation.Operation);

            return (emitter, setupActionDiagnostics);
        }
    }
}
