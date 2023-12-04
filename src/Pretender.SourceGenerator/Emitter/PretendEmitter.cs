using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.Emitter
{
    internal class PretendEmitter
    {
        private readonly ITypeSymbol _pretendType;
        private readonly bool _fillExisting;

        public PretendEmitter(ITypeSymbol pretendType, bool fillExisting)
        {
            _pretendType = pretendType;
            _fillExisting = fillExisting;
        }

        public ITypeSymbol PretendType => _pretendType;

        public TypeDeclarationSyntax Emit(CancellationToken token)
        {
            var pretendFieldAssignment = ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("_pretend"),
                    IdentifierName("pretend")
                )
            );

            var methodSymbols = new List<(IMethodSymbol Method, string Name)>();

            token.ThrowIfCancellationRequested();

            var typeMembers = _pretendType.GetMembers();

            foreach (var member in typeMembers)
            {
                if (member is IMethodSymbol methodSymbol)
                {
                    methodSymbols.Add((methodSymbol, methodSymbol.Name));
                }
                else if (member is IPropertySymbol propertySymbol)
                {
                    // Property symbol is taken care of through IMethodSymbol
                }
                else if (member is IFieldSymbol fieldSymbol)
                {
                    // TODO: Do I need to do anything
                    // abstract fields?
                }
                else
                {
                    throw new NotImplementedException($"We don't support {member.Kind} quite yet, please file an issue.");
                }
            }

            token.ThrowIfCancellationRequested();

            var methodInfoFields = new List<FieldDeclarationSyntax>();

            // Find the shortest path to uniquify all method info getters
            var groupedMethodSymbols = methodSymbols
                .Where(m => m.Method.MethodKind == MethodKind.Ordinary)
                .GroupBy(m => m.Name);

            foreach (var groupedMethodSymbol in groupedMethodSymbols)
            {
                var methods = groupedMethodSymbol.ToArray();

                if (methods.Length == 1)
                {
                    var (method, name) = methods[0];
                    // No one else has this name
                    ExpressionSyntax expression = CreateSimpleMethodInfoGetter(name, "GetMethod");
                    methodInfoFields.Add(CreateMethodInfoField(method, expression));
                    continue;
                }

                // We have to do more work to fine the unique method, I also know it's not a property anymore
                // because properties have a unique name
                var groupedMethodParameterLengths = groupedMethodSymbol
                    .GroupBy(m => m.Method.Parameters.Length);

                foreach (var groupedMethodParameterLength in groupedMethodParameterLengths)
                {
                    methods = groupedMethodParameterLength.ToArray();

                    if (methods.Length == 1)
                    {
                        var method = methods[0];
                        // This method is unique from it's other matches via it's parameter length
                        // TODO: Do this
                        continue;
                    }
                }

                // TODO: Match all type parameters
                throw new NotImplementedException($"Could not find a unique way to identify method '{groupedMethodSymbol.Key}'");
            }

            var propertyMethodSymbols = methodSymbols
                .Where(m => m.Method.MethodKind == MethodKind.PropertyGet
                    || m.Method.MethodKind == MethodKind.PropertySet);

            foreach (var (method, name) in propertyMethodSymbols)
            {
                var methodName = method.MethodKind == MethodKind.PropertyGet
                    ? "GetMethod"
                    : "SetMethod";
                ExpressionSyntax expression = CreateSimplePropertyMethodInfoGetter(method.AssociatedSymbol!.Name, methodName);
                methodInfoFields.Add(CreateMethodInfoField(method, expression));
            }

            var instanceField = FieldDeclaration(VariableDeclaration(GetGenericPretendType(), SingletonSeparatedList(VariableDeclarator(Identifier("_pretend")))))
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)))
                .WithTrailingTrivia(CarriageReturnLineFeed);

            methodInfoFields.Add(instanceField);

            var classDeclaration = _pretendType.ScaffoldImplementation(new ScaffoldTypeOptions
            {
                CustomFields = methodInfoFields.ToImmutableArray(),
                AddMethodBody = CreateMethodBody,
                CustomizeConstructor = () => (CreateConstructorParameter(), [pretendFieldAssignment]),
            });

            // TODO: Add properties

            // TODO: Generate debugger display
            return classDeclaration
                .WithModifiers(TokenList(Token(SyntaxKind.FileKeyword)));
        }

        private static FieldDeclarationSyntax CreateMethodInfoField(IMethodSymbol method, ExpressionSyntax expressionSyntax)
        {
            // public static readonly MethodInfo MethodInfo_name_4B2 = <expression>!;
            return FieldDeclaration(VariableDeclaration(ParseTypeName("MethodInfo")))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword))
                .AddDeclarationVariables(VariableDeclarator(Identifier(method.ToMethodInfoCacheName()))
                    .WithInitializer(EqualsValueClause(
                        PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, expressionSyntax)))
                );
        }

        private InvocationExpressionSyntax CreateSimpleMethodInfoGetter(string name, string afterTypeOfMethod)
        {

            return InvocationExpression(MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                TypeOfExpression(ParseTypeName(_pretendType.ToFullDisplayString())),
                IdentifierName(afterTypeOfMethod)))
                .AddArgumentListArguments(Argument(NameOfExpression(name)));
        }

        private MemberAccessExpressionSyntax CreateSimplePropertyMethodInfoGetter(string propertyName, string type)
        {
            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                CreateSimpleMethodInfoGetter(propertyName, "GetProperty"),
                IdentifierName(type));
        }

        private static InvocationExpressionSyntax NameOfExpression(string identifier)
        {
            var text = SyntaxFacts.GetText(SyntaxKind.NameOfKeyword);

            var identifierSyntax = Identifier(default,
                SyntaxKind.NameOfKeyword,
                text,
                text,
                default);

            return InvocationExpression(
                IdentifierName(identifierSyntax),
                ArgumentList(SingletonSeparatedList(Argument(IdentifierName(identifier)))));
        }

        private ParameterSyntax CreateConstructorParameter()
        {
            return Parameter(Identifier("pretend"))
                .WithType(GetGenericPretendType());
        }

        private TypeSyntax GetGenericPretendType()
        {
            return GenericName(Identifier("Pretend"),
                    TypeArgumentList(SingletonSeparatedList(ParseTypeName(_pretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))));
        }

        private FieldDeclarationSyntax GetStaticMethodCacheField(IMethodSymbol method, int index)
        {
            // TODO: Get method info via argument types
            return FieldDeclaration(VariableDeclaration(ParseTypeName("MethodInfo")))
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword))
                .AddDeclarationVariables(VariableDeclarator(Identifier($"__methodInfo_{method.Name}_{index}"))
                    .WithInitializer(EqualsValueClause(
                        InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, TypeOfExpression(ParseTypeName(_pretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))), IdentifierName("GetMethod")))
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(ParseExpression($"nameof({method.Name})"))))))));
        }

        private BlockSyntax CreateMethodBody(IMethodSymbol method)
        {
            var methodBodyStatements = new List<StatementSyntax>();

            // This is using the new collection expression syntax in C# 12
            // [arg1, arg2, arg3]
            var collectionExpression = CollectionExpression()
                .AddElements(method.Parameters.Select(p
                    => ExpressionElement(IdentifierName(p.Name))).ToArray());

            // object?[]
            var typeSyntax = ArrayType(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword))))
                .WithRankSpecifiers(SingletonList(ArrayRankSpecifier()));

            var argumentsIdentifier = IdentifierName("__arguments");
            var callInfoIdentifier = IdentifierName("__callInfo");

            // I'm not currently able to use Span because I have to store CallInfo for late Setup/Verify
            // but I don't want to delete this code in case this becomes possible or I don't want to support that
            // Span<object?>
            //var typeSyntax = GenericName("Span").AddTypeArgumentListArguments(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword))));

            // object? [] arguments = [arg0, arg1];
            var argumentsDeclaration = LocalDeclarationStatement(
                VariableDeclaration(typeSyntax)
                .AddVariables(VariableDeclarator(argumentsIdentifier.Identifier)
                    .WithInitializer(EqualsValueClause(collectionExpression))
                )
            );

            methodBodyStatements.Add(argumentsDeclaration);

            // var callInfo = new CallInfo(__methodInfo_MethodName_0, arguments);
            var callInfoCreation = LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(VariableDeclarator(callInfoIdentifier.Identifier)
                        .WithInitializer(EqualsValueClause(ObjectCreationExpression(ParseTypeName("CallInfo"))
                            .AddArgumentListArguments(Argument(IdentifierName(method.ToMethodInfoCacheName())), Argument(argumentsIdentifier))))));

            methodBodyStatements.Add(callInfoCreation);

            // TODO: Call inner implementations when we support them

            // _pretend.Handle(callInfo);
            var handleCall = ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("_pretend"),
                IdentifierName("Handle")))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(callInfoIdentifier)))));

            methodBodyStatements.Add(handleCall);

            // Set ref and out parameters
            // TODO: Do I need to do refs?
            var refAndOutParameters = method.Parameters
                .Where(p => p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out);



            foreach (var p in refAndOutParameters)
            {
                // assign them to the values from arguments
                var refOrOutAssignment = AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(p.Name),
                    ElementAccessExpression(
                        argumentsIdentifier,
                        BracketedArgumentList(SingletonSeparatedList(
                            Argument(LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(p.Ordinal))))))
                );

                methodBodyStatements.Add(ExpressionStatement(refOrOutAssignment));
            }

            if (method.ReturnType.SpecialType != SpecialType.System_Void)
            {
                var returnStatement = ReturnStatement(CastExpression(
                        method.ReturnType.AsUnknownTypeSyntax(),
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, callInfoIdentifier, IdentifierName("ReturnValue"))));

                methodBodyStatements.Add(returnStatement);
            }

            return Block(methodBodyStatements);
        }
    }
}
