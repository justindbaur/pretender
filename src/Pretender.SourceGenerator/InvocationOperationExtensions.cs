using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Pretender.SourceGenerator
{
    public static class InvocationOperationExtensions
    {
        public static bool IsInvocationOperation(this IOperation? operation, out IInvocationOperation? invocationOperation)
        {
            invocationOperation = null;
            if (operation is IInvocationOperation targetOperation)
            {
                invocationOperation = targetOperation;
                return true;
            }

            return false;
        }

        public static bool IsSetupCall(this SyntaxNode node)
        {
            return node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    // pretend.Setup(i => i.Something());
                    Name.Identifier.ValueText: "Setup" or "SetupSet",
                },
                ArgumentList.Arguments.Count: 1
            };
        }

        public static bool IsValidSetupOperation(this IOperation operation, Compilation compilation, out IInvocationOperation? invocation)
        {
            var pretendType = compilation.GetTypeByMetadataName("Pretender.Pretend`1");
            invocation = null;

            // TODO: Probably need to check a few more things
            // Someone could make a Setup extension method, that doesn't look
            // like I think it should, I need to check the return type and first arg type
            // a lot more closely.
            if (operation is IInvocationOperation targetOperation 
                && targetOperation.Instance is not null
                && SymbolEqualityComparer.Default.Equals(targetOperation.Instance.Type!.OriginalDefinition, pretendType))
            {
                invocation = targetOperation;
                return true;
            }

            return false;
        }

        public static bool IsValidCreateOperation(this IOperation? operation, Compilation compilation, out IInvocationOperation invocationOperation, out ImmutableArray<ITypeSymbol>? typeArguments)
        {
            var pretendGeneric = compilation.GetTypeByMetadataName("Pretender.Pretend`1");

            if (operation is IInvocationOperation targetOperation
                && targetOperation.Instance is not null
                && SymbolEqualityComparer.Default.Equals(targetOperation.Instance.Type!.OriginalDefinition, pretendGeneric))
            {
                invocationOperation = targetOperation;
                if (targetOperation.TargetMethod.Parameters.Length == 1 && targetOperation.TargetMethod.Parameters[0].IsParams)
                {
                    // They are in the params fallback, how lol?
                    typeArguments = null;
                }
                else
                {
                    typeArguments = targetOperation.TargetMethod.TypeArguments;
                }

                return true;
            }

            invocationOperation = null!;
            typeArguments = null;
            return false;
        }
    }
}
