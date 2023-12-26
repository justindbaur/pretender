using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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