using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Parser;

namespace Pretender.SourceGenerator
{
    internal class SetupInvocation
    {
        public SetupInvocation(IInvocationOperation operation, Location location)
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
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "Setup" or "SetupSet",
                },
                ArgumentList.Arguments.Count: 1,
            };
        }

        public static SetupInvocation? Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(IsCandidateSyntaxNode(context.Node));
            var invocationSyntax = (InvocationExpressionSyntax)context.Node;

            return context.SemanticModel.GetOperation(invocationSyntax, cancellationToken) is IInvocationOperation operation
                && IsSetupOperation(operation)
                ? new SetupInvocation(operation, invocationSyntax.GetLocation())
                : null;
        }

        private static bool IsSetupOperation(IInvocationOperation operation)
        {
            if (operation.TargetMethod is not IMethodSymbol
                {
                    Name: "Setup" or "SetupSet",
                    ContainingType: INamedTypeSymbol namedTypeSymbol
                } || !KnownTypeSymbols.IsPretend(namedTypeSymbol))
            {
                return false;
            }

            return true;
        }
    }
}
