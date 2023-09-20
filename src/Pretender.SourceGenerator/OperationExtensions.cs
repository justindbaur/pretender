using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator
{
    internal static class OperationExtensions
    {
        public static LiteralExpressionSyntax ToLiteralExpression(this ILiteralOperation operation)
        {
            if (operation.Type is null || !operation.ConstantValue.HasValue)
            {
                return LiteralExpression(SyntaxKind.NullLiteralExpression);
            }
            else if (operation.Type.EqualsByName(["System", "String"]))
            {
                return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal((string)operation.ConstantValue.Value!));
            }
            else if (operation.Type.EqualsByName(["System", "Int32"]))
            {
                return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal((int)operation.ConstantValue.Value!));
            }
            else if (operation.Type.EqualsByName(["System", "Boolean"]))
            {
                var value = (bool)operation.ConstantValue.Value!;
                return LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
            }

            throw new NotImplementedException($"We don't support literals of {operation.Type.Name} yet.");
        }
    }
}
