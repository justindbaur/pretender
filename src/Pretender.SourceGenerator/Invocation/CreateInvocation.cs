using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Parser;

namespace Pretender.SourceGenerator.Invocation
{
    internal class CreateInvocation
    {
        public CreateInvocation(IInvocationOperation operation, ImmutableArray<ITypeSymbol>? typeArguments, InterceptsLocationInfo location)
        {
            Operation = operation;
            TypeArguments = typeArguments;
            Location = location;
        }

        public IInvocationOperation Operation { get; }
        public ImmutableArray<ITypeSymbol>? TypeArguments { get; }
        public InterceptsLocationInfo Location { get; }

        public static bool IsCandidateSyntaxNode(SyntaxNode node)
        {
            return node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "Create"
                },
            };
        }

        public static CreateInvocation? Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(IsCandidateSyntaxNode(context.Node));
            return context.SemanticModel.GetOperation(context.Node, cancellationToken) is IInvocationOperation operation
                && IsCreateOperation(operation, out var typeArguments)
                ? new CreateInvocation(operation, typeArguments, new InterceptsLocationInfo(operation))
                : null;
        }

        private static bool IsCreateOperation(IInvocationOperation operation, out ImmutableArray<ITypeSymbol>? typeArguments)
        {
            typeArguments = null;
            if (operation.Instance is null
                || operation.Instance.Type is not INamedTypeSymbol namedType
                || !KnownTypeSymbols.IsPretend(namedType))
            {
                return false;
            }

            // Are they in the params overload?
            if (operation.TargetMethod.Parameters.Length == 1
                && operation.TargetMethod.Parameters[0].IsParams)
            {
                return true;
            }

            typeArguments = operation.TargetMethod.TypeArguments;
            return true;
        }
    }

    public class CreateInvocationComparer : IEqualityComparer<CreateInvocation>
    {
        public static CreateInvocationComparer Instance = new();
        bool IEqualityComparer<CreateInvocation>.Equals(CreateInvocation x, CreateInvocation y)
        {
            return SymbolEqualityComparer.Default.Equals(x.Operation.TargetMethod.ReturnType, y.Operation.TargetMethod.ReturnType)
                && CompareTypeArguments(x.TypeArguments, y.TypeArguments);
        }

        private static bool CompareTypeArguments(ImmutableArray<ITypeSymbol>? x, ImmutableArray<ITypeSymbol>? y)
        {
            if (!x.HasValue)
            {
                return !y.HasValue;
            }

            if (!y.HasValue)
            {
                // We've established x does have a value so y not having one is a non-match
                return false;
            }

            var xArray = x.Value;
            var yArray = y.Value;

            if (xArray.Length != yArray.Length)
            {
                return false;
            }

            for (int i = 0; i < xArray.Length; i++)
            {
                if (!SymbolEqualityComparer.IncludeNullability.Equals(xArray[i], yArray[i]))
                {
                    return false;
                }
            }

            return true;
        }

        int IEqualityComparer<CreateInvocation>.GetHashCode(CreateInvocation obj)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(obj.Operation.TargetMethod.ReturnType);

                if (!obj.TypeArguments.HasValue)
                {
                    // TODO: Is 5 an okay value?
                    hash = hash * 31 + 5;
                }
                else
                {
                    var typeArguments = obj.TypeArguments.Value;
                    foreach (var typeArgument in typeArguments)
                    {
                        hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(typeArgument);
                    }
                }

                return hash;
            }
        }
    }
}