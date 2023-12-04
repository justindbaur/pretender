using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pretender.SourceGenerator.Emitter
{
    internal class GrandEmitter
    {
        private readonly ImmutableArray<PretendEmitter> _pretendEmitters;

        public GrandEmitter(ImmutableArray<PretendEmitter> pretendEmitters)
        {
            _pretendEmitters = pretendEmitters;
        }

        public CompilationUnitSyntax Emit(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
