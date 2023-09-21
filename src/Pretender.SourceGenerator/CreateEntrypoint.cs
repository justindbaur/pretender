using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Pretender.SourceGenerator
{
    internal class CreateEntrypoint
    {
        public CreateEntrypoint(IInvocationOperation operation)
        {
            Operation = operation;
            Location = new InterceptsLocationInfo(operation);

            // TODO: Do any Diagnostics?
        }

        public InterceptsLocationInfo Location { get; }
        public IInvocationOperation Operation { get; }

        public MethodDeclarationSyntax GetMethodDeclaration(int index)
        {
            var returnType = Operation.TargetMethod.ReturnType;
            var returnTypeName = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var returnStatement = ReturnStatement(ObjectCreationExpression(ParseTypeName(returnType.ToPretendName()))
                    .AddArgumentListArguments(Argument(IdentifierName("pretend"))));

            return MethodDeclaration(ParseTypeName(returnTypeName), $"Create{index}")
                .WithBody(Block(returnStatement))
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("pretend"))
                        .WithType(ParseTypeName($"Pretend<{returnType}>"))
                        .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword))),
                })))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)));
        }
    }

    public class CreateEntryPointComparer : IEqualityComparer<CreateEntrypoint>
    {
        public static CreateEntryPointComparer Instance = new();

        bool IEqualityComparer<CreateEntrypoint>.Equals(CreateEntrypoint x, CreateEntrypoint y)
        {
            return SymbolEqualityComparer.Default.Equals(x.Operation.TargetMethod.ReturnType, y.Operation.TargetMethod.ReturnType);
        }

        int IEqualityComparer<CreateEntrypoint>.GetHashCode(CreateEntrypoint obj)
        {
            return SymbolEqualityComparer.Default.GetHashCode(obj.Operation.TargetMethod.ReturnType);
        }
    }

}
