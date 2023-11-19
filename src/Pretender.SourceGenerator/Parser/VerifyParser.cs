using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Emitter;
using static Pretender.SourceGenerator.PretenderSourceGenerator;

namespace Pretender.SourceGenerator.Parser
{
    internal class VerifyParser
    {
        private readonly VerifyInvocation _verifyInvocation;
        private readonly KnownTypeSymbols _knownTypeSymbols;

        public VerifyParser(VerifyInvocation verifyInvocation, CompilationData compilationData)
        {
            _knownTypeSymbols = compilationData.TypeSymbols!;
            _verifyInvocation = verifyInvocation;
        }

        public (VerifyEmitter? VerifyEmitter, ImmutableArray<Diagnostic>? Diagnostics) GetVerifyEmitter(CancellationToken cancellationToken)
        {
            var operation = _verifyInvocation.Operation;

            // Verify calls are expected to have 2 arguments, the first being the setup expression
            var setupArgument = operation.Arguments[0];

            // Verify calls are expected to be called from Pretend<T> so the type argument gives us the type we are pretending
            var pretendType = operation.TargetMethod.ContainingType.TypeArguments[0];

            // TODO: This doesn't exist yet
            var useSetMethod = operation.TargetMethod.Name == "VerifySet";

            // TODO: This should be done in a Parser type class as well
            var setupCreationSpec = new SetupCreationSpec(setupArgument, pretendType, useSetMethod);

            var returnType = setupArgument.Parameter!.Type.Name == "Func"
                ? ((INamedTypeSymbol)setupArgument.Parameter.Type).TypeArguments[1] // The Func variant is expected to have the return type in the second type argument
                : null;

            var emitter = new VerifyEmitter(pretendType, returnType, setupCreationSpec, _verifyInvocation.Operation);

            // TODO: Get diagnostics from elsewhere
            return (emitter, null);
        }
    }
}
