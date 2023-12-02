using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Invocation;
using static Pretender.SourceGenerator.PretenderSourceGenerator;

namespace Pretender.SourceGenerator.Parser
{
    internal class PretendParser
    {

        public PretendParser(PretendInvocation pretendInvocation, CompilationData compilationData)
        {
            PretendInvocation = pretendInvocation;
        }

        public PretendInvocation PretendInvocation { get; }

        public (PretendEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics) Parse(CancellationToken cancellationToken)
        {
            if (PretendInvocation.PretendType.IsSealed)
            {
                var sealedError = Diagnostic.Create(
                    DiagnosticDescriptors.UnableToPretendSealedType,
                    PretendInvocation.Location);
                return (null, ImmutableArray.Create(sealedError));
            }

            // TODO: If we are filling an existing time, check that it is partial

            // TODO: Do more error diagnostics

            // TODO: Warn about well known good fakes

            // TODO: Do a larger amount of parsing

            cancellationToken.ThrowIfCancellationRequested();

            return (new PretendEmitter(PretendInvocation.PretendType, PretendInvocation.FillExisting), null);
        }
    }
}
