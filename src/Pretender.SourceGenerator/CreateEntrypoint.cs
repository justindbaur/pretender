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

            // TODO: Do any Diagnostics?
        }

        public IInvocationOperation Operation { get; }

        public MethodDeclarationSyntax GetMethodDeclaration(int index)
        {
            var returnType = Operation.TargetMethod.ReturnType;
            var returnTypeName = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var interceptsLocation = new InterceptsLocationInfo(Operation);

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
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithAttributeLists(SingletonList(AttributeList(
                    SingletonSeparatedList(interceptsLocation.ToAttributeSyntax()))));
        }
    }
}
