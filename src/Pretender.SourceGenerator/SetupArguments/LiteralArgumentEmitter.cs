using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.SetupArguments
{
    internal class LiteralArgumentEmitter : SetupArgumentEmitter
    {
        private readonly ILiteralOperation _literalOperation;

        public LiteralArgumentEmitter(ILiteralOperation literalOperation, SetupArgumentSpec argumentSpec)
            : base(argumentSpec)
        {
            _literalOperation = literalOperation;
        }

        public override void EmitArgumentMatcher(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            EmitArgumentAccessor(writer);
            EmitIfReturnFalseCheck(writer,
                $"{ArgumentSpec.Parameter.Name}_arg",
                CSharpSyntaxUtilities.FormatLiteral(_literalOperation.ConstantValue.Value, ArgumentSpec.Parameter.Type));
        }

        public override int GetHashCode()
        {
            return _literalOperation.ConstantValue.HasValue
                ? _literalOperation.ConstantValue.Value?.GetHashCode() ?? 41602 // TODO: Magic value?
                : 1337; // TODO: Magic value?
        }
    }
}
