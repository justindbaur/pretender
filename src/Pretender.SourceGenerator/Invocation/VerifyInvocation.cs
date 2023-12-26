using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Parser;

namespace Pretender.SourceGenerator.Invocation
{
    // This should be a simple class just holding some information, deeper introspection to find diagnostics should
    // be done with a type cache
    internal class VerifyInvocation
    {
        public VerifyInvocation(IInvocationOperation operation, Location location)
        {
            Operation = operation;
            Location = location;
        }

        public IInvocationOperation Operation { get; }
        public Location Location { get; }

        public static bool IsCandidateSyntaxNode(SyntaxNode node)
        {
            return node is InvocationExpressionSyntax
            {
                // pretend.Verify(i => i.Something(), 2);
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "Verify", // TODO: or VerifySet
                },
                ArgumentList.Arguments.Count: 2
            };
        }

        public static VerifyInvocation? Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(IsCandidateSyntaxNode(context.Node));
            var invocationSyntax = (InvocationExpressionSyntax)context.Node;

            return context.SemanticModel.GetOperation(invocationSyntax, cancellationToken) is IInvocationOperation operation
                && IsVerifyOperation(operation)
                ? new VerifyInvocation(operation, invocationSyntax.GetLocation())
                : null;
        }

        private static bool IsVerifyOperation(IInvocationOperation operation)
        {
            // TODO: Verify ALL of the things, no false positives should escape here
            // but we should do it all with string comparisons
            if (operation.TargetMethod is not IMethodSymbol
                {
                    // TODO: The name has already been asserted, do I need to do this again?
                    Name: "Verify", // TODO: or VerifySet,
                    ContainingType: INamedTypeSymbol namedTypeSymbol
                } || !KnownTypeSymbols.IsPretend(namedTypeSymbol))
            {
                return false;
            }

            return true;
        }
    }
}