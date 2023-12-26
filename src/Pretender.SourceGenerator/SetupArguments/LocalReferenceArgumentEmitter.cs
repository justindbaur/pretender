using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.SetupArguments
{
    internal class LocalReferenceArgumentEmitter : SetupArgumentEmitter
    {
        private readonly ILocalReferenceOperation _localReferenceOperation;

        public LocalReferenceArgumentEmitter(ILocalReferenceOperation localReferenceOperation, SetupArgumentSpec argumentSpec) : base(argumentSpec)
        {
            _localReferenceOperation = localReferenceOperation;
        }

        public override void EmitArgumentMatcher(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            var localVariableName = $"{ArgumentSpec.Parameter.Name}_local";
            EmitArgumentAccessor(writer);
            writer.WriteLine(@$"var {localVariableName} = target.GetType().GetField(""{_localReferenceOperation.Local.Name}"").GetValue(target);");
            EmitIfReturnFalseCheck(writer, $"{ArgumentSpec.Parameter.Name}_arg", localVariableName);
        }

        public override int GetHashCode()
        {
            return SymbolEqualityComparer.Default.GetHashCode(_localReferenceOperation.Local);
        }
    }
}