using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator
{
    internal static class KnownBlocks
    {
        private static readonly AssemblyName s_assemblyName = typeof(KnownBlocks).Assembly.GetName();
        private static readonly string GeneratedCodeAnnotationString = $@"[GeneratedCode(""{s_assemblyName.Name}"", ""{s_assemblyName.Version}"")]";
        public static string InterceptsLocationAttribute { get; } = $$"""
            namespace System.Runtime.CompilerServices
            {
                using System;
                using System.CodeDom.Compiler;

                {{GeneratedCodeAnnotationString}}
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                file sealed class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(string filePath, int line, int column)
                    {
                    }
                }
            }
            """;

        public static MemberAccessExpressionSyntax TaskCompletedTask = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName("Task"),
            IdentifierName("CompletedTask")
        );

        public static MemberAccessExpressionSyntax ValueTaskCompletedTask = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName("ValueTask"),
            IdentifierName("CompletedTask")
        );

        public static InvocationExpressionSyntax TaskFromResult(TypeSyntax resultType, ExpressionSyntax resultValue) => InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("Task"),
                GenericName("FromResult")
                    .AddTypeArgumentListArguments(resultType))
        )
            .AddArgumentListArguments(Argument(resultValue));

        public static InvocationExpressionSyntax ValueTaskFromResult(TypeSyntax resultType, ExpressionSyntax resultValue)
        {
            // ValueTask.FromResult<T>
            var memberAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("ValueTask"),
                GenericName("FromResult")
                    .AddTypeArgumentListArguments(resultType));

            // ValueTask.FromResult<T>(value)
            return InvocationExpression(memberAccess,
                ArgumentList(
                    SingletonSeparatedList(Argument(resultValue))));
        }
    }
}