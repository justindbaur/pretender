﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.Emitter
{
    internal class VerifyEmitter
    {
        private readonly ITypeSymbol _pretendType;
        private readonly ITypeSymbol? _returnType;
        private readonly SetupActionEmitter _setupActionEmitter;
        private readonly IInvocationOperation _invocationOperation;

        public VerifyEmitter(ITypeSymbol pretendType, ITypeSymbol? returnType, SetupActionEmitter setupActionEmitter, IInvocationOperation invocationOperation)
        {
            _pretendType = pretendType;
            _returnType = returnType;
            _setupActionEmitter = setupActionEmitter;
            _invocationOperation = invocationOperation;
        }

        public MethodDeclarationSyntax Emit(int index, CancellationToken cancellationToken)
        {
            var setupInvocation = _setupActionEmitter.CreateSetupGetter(cancellationToken);

            // var setup = pretend.GetOrCreateSetup(...);
            var setupLocal = LocalDeclarationStatement(VariableDeclaration(CommonSyntax.VarType)
                .WithVariables(SingletonSeparatedList(VariableDeclarator(CommonSyntax.SetupIdentifier)
                    .WithInitializer(EqualsValueClause(setupInvocation)))));

            TypeSyntax pretendType = ParseTypeName(_pretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            TypeSyntax setupExpressionType = _returnType == null
                ? GenericName("Action").AddTypeArgumentListArguments(pretendType)
                : GenericName("Func").AddTypeArgumentListArguments(pretendType, _returnType.AsUnknownTypeSyntax());

            // setup.Verify(called);
            var verifyInvocation = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(CommonSyntax.SetupIdentifier),
                    IdentifierName("Verify")
                )
            )
                .AddArgumentListArguments(Argument(IdentifierName(CommonSyntax.CalledIdentifier)));

            var interceptsInfo = new InterceptsLocationInfo(_invocationOperation);

            // public static void Verify0(
            return MethodDeclaration(CommonSyntax.VoidType, Identifier($"Verify{index}"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(
                    [
                        Parameter(Identifier("pretend"))
                            .WithType(CommonSyntax.GenericPretendType.AddTypeArgumentListArguments(pretendType))
                            .AddModifiers(Token(SyntaxKind.ThisKeyword)),
                        Parameter(Identifier("setupExpression"))
                            .WithType(setupExpressionType),
                        CommonSyntax.CalledParameter
                    ])
                .AddBodyStatements([setupLocal, ExpressionStatement(verifyInvocation)])
                .AddAttributeLists(AttributeList(SingletonSeparatedList(interceptsInfo.ToAttributeSyntax())));
        }
    }
}
