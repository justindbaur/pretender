using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator
{
    internal static class SymbolExtensions
    {
        public static IEnumerable<IGrouping<string, IMethodSymbol>> GetGroupedMethods(this ITypeSymbol type)
        {
            return type.GetMembers()
                .OfType<IMethodSymbol>()
                .GroupBy(m => m.Name);
        }

        public static IEnumerable<(IMethodSymbol MethodSymbol, MethodDeclarationSyntax MethodDeclaration)> GetEquivalentMethodSignatures(this IEnumerable<IMethodSymbol> methods)
        {
            foreach (var method in methods)
            {
                var methodDeclaration = MethodDeclaration(
                    returnType: ParseTypeName(method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                    identifier: method.Name)
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))) // TODO: Are there other modifiers we need to copy?
                    .AddParameterListParameters(method.Parameters.Select(GetParameter).ToArray())
                    .WithInheritDoc();
             
                yield return (method,  methodDeclaration);
            }

            static ParameterSyntax GetParameter(IParameterSymbol parameter)
            {
                var parameterSyntax = Parameter(Identifier(parameter.Name))
                    .WithType(ParseTypeName(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

                if (parameter.HasExplicitDefaultValue)
                {
                    // TODO: Support default parameters
                    throw new NotImplementedException("Default parameters are not supported yet.");
                }

                return parameterSyntax;
            }
        }
    }
}
