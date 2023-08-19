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
    }
}
