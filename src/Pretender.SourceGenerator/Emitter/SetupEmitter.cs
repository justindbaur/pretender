using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Operations;

namespace Pretender.SourceGenerator.Emitter
{
    internal class SetupEmitter
    {
        private readonly SetupActionEmitter _setupActionEmitter;
        private readonly IInvocationOperation _setupInvocation;

        public SetupEmitter(SetupActionEmitter setupActionEmitter, IInvocationOperation setupInvocation)
        {
            _setupActionEmitter = setupActionEmitter;
            _setupInvocation = setupInvocation;
        }

        // TODO: Run cancellationToken a lot more
        public MemberDeclarationSyntax Emit(int index, CancellationToken cancellationToken)
        {
            var setupMethod = _setupActionEmitter.SetupMethod;
            var pretendType = _setupActionEmitter.PretendType;

            var interceptsLocation = new InterceptsLocationInfo(_setupInvocation);

            // TODO: This is wrong
            var typeArguments = setupMethod.ReturnsVoid
                ? TypeArgumentList(SingletonSeparatedList(ParseTypeName(pretendType.ToFullDisplayString())))
                : TypeArgumentList(SeparatedList([ParseTypeName(pretendType.ToFullDisplayString()), setupMethod.ReturnType.AsUnknownTypeSyntax()]));

            var returnType = GenericName("IPretendSetup")
                .WithTypeArgumentList(typeArguments);

            var setupCreatorInvocation = _setupActionEmitter.CreateSetupGetter(cancellationToken);

            return MethodDeclaration(returnType, $"Setup{index}")
                .WithBody(Block(ReturnStatement(setupCreatorInvocation)))
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("pretend"))
                        .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                        .WithType(ParseTypeName($"Pretend<{pretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>")),

                    Parameter(Identifier("setupExpression"))
                        .WithType(GenericName(setupMethod.ReturnsVoid ? "Action" : "Func").WithTypeArgumentList(typeArguments)),
                })))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithAttributeLists(SingletonList(AttributeList(
                    SingletonSeparatedList(interceptsLocation.ToAttributeSyntax()))));
        }
    }
}
