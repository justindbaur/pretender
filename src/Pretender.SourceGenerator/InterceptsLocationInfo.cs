using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator
{
    internal class InterceptsLocationInfo
    {
        public InterceptsLocationInfo(IInvocationOperation invocationOperation)
        {
            var memberSyntax = (MemberAccessExpressionSyntax)((InvocationExpressionSyntax)invocationOperation.Syntax).Expression;
            var operationSyntaxTree = invocationOperation.Syntax.SyntaxTree;
            var resolver = invocationOperation.SemanticModel?.Compilation.Options.SourceReferenceResolver;
            FilePath = resolver?.NormalizePath(operationSyntaxTree.FilePath, null) ?? operationSyntaxTree.FilePath;

            var linePosSpan = operationSyntaxTree.GetLineSpan(memberSyntax.Name.Span);
            LineNumber = linePosSpan.StartLinePosition.Line + 1;
            CharacterNumber = linePosSpan.StartLinePosition.Character + 1;
        }

        public string FilePath { get; }
        public int LineNumber { get; }
        public int CharacterNumber { get; }

        public AttributeSyntax ToAttributeSyntax()
        {
            return Attribute(IdentifierName("InterceptsLocation"))
                    .WithArgumentList(AttributeArgumentList(SeparatedList(new[]
                    {
                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(FilePath))),

                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(LineNumber))),

                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(CharacterNumber))),
                    })));
        }
    }
}
