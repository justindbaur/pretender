﻿using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Parser;

namespace Pretender.SourceGenerator
{
    internal class PretendInvocation
    {
        public PretendInvocation(ITypeSymbol pretendType, Location location, bool fillExisting)
        {
            PretendType = pretendType;
            Location = location;
            FillExisting = fillExisting;
        }

        public ITypeSymbol PretendType { get; }
        public Location Location { get; }
        public bool FillExisting { get; }

        public static bool IsCandidateSyntaxNode(SyntaxNode node)
        {
            // Pretend.That<T>();
            if (node is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        // TODO: Will this work with a using static Pretender.Pretend
                        // ...
                        // That<IInterface>();
                        Expression: IdentifierNameSyntax { Identifier.ValueText: "Pretend" },
                        Name: GenericNameSyntax { Identifier.ValueText: "That", TypeArgumentList.Arguments.Count: 1 },
                    }
                })
            {
                return true;
            }

            // TODO: Also do Attribute

            return false;
        }

        public static PretendInvocation? Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(IsCandidateSyntaxNode(context.Node));
            var operation = context.SemanticModel.GetOperation(context.Node, cancellationToken);
            if (operation is IInvocationOperation invocation)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return CreateFromGeneric(invocation);
            }
            // TODO: Support attribute

            return null;
        }

        private static PretendInvocation? CreateFromGeneric(IInvocationOperation operation)
        {
            if (operation.TargetMethod is not IMethodSymbol
                {
                    Name: "That",
                    ContainingType: INamedTypeSymbol namedTypeSymbol,
                    TypeArguments.Length: 1,
                } || !KnownTypeSymbols.IsPretend(namedTypeSymbol))
            {
                return null;
            }

            return new PretendInvocation(operation.TargetMethod.TypeArguments[0], operation.Syntax.GetLocation(), false);
        }
    }
}
