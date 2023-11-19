using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.SetupArguments
{
    internal class LiteralArgumentSpec : SetupArgumentSpec
    {
        private readonly ILiteralOperation _literalOperation;

        public LiteralArgumentSpec(ILiteralOperation literalOperation, IArgumentOperation originalArgument, int argumentPlacement)
            : base(originalArgument, argumentPlacement)
        {
            _literalOperation = literalOperation;
        }

        public override int NeededMatcherStatements => 2;

        public override ImmutableArray<StatementSyntax> CreateMatcherStatements(CancellationToken cancellationToken)
        {
            var (argumentName, localDeclaration) = CreateArgumentAccessor();
            var ifCheck = CreateIfCheck(IdentifierName(argumentName), _literalOperation.ToLiteralExpression());
            return ImmutableArray.Create<StatementSyntax>([localDeclaration, ifCheck]);
        }

        public override int GetHashCode()
        {
            return _literalOperation.ConstantValue.HasValue
                ? _literalOperation.ConstantValue.Value?.GetHashCode() ?? 41602 // TODO: Magic value?
                : 1337; // TODO: Magic value?
        }
    }
}
