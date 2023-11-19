using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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

        public (object? Emitter, ImmutableArray<Diagnostic>? Diagnostics) GetEmitter(CancellationToken cancellationToken)
        {

            return (null, null);
        }
    }
}
