using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator
{
    internal class PretendEntrypoint
    {
        public static PretendEntrypoint FromMethodGeneric(IInvocationOperation operation)
        {
            Debug.Assert(operation.TargetMethod.TypeArguments.Length == 1, "This should have been asserted already");
            var genericLocation = ((GenericNameSyntax)((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)operation.Syntax).Expression).Name).TypeArgumentList.Arguments[0].GetLocation();
            var typeArgument = operation.TargetMethod.TypeArguments[0];
            return new PretendEntrypoint(typeArgument,
                genericLocation);
        }

        public PretendEntrypoint(ITypeSymbol typeToPretend, Location invocationLocation)
        {
            TypeToPretend = typeToPretend;

            InvocationLocation = invocationLocation;

            // TODO: Do more diagnostics
            if (TypeToPretend.IsSealed)
            {
                Diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.UnableToPretendSealedType,
                    invocationLocation,
                    TypeToPretend));
            }

            PretendName = TypeToPretend.ToPretendName();
        }

        public ITypeSymbol TypeToPretend { get; }
        public Location InvocationLocation { get; }
        public string PretendName { get; }
        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        public CompilationUnitSyntax GetCompilationUnit(CancellationToken token)
        {
            var pretendFieldAssignment = ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("_pretend"),
                    IdentifierName("pretend")
                )
            );

            var methodSymbols = new List<(IMethodSymbol Method, string Name)>();

            token.ThrowIfCancellationRequested();

            var typeMembers = TypeToPretend.GetMembers();

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

            var classDeclaration = TypeToPretend.ScaffoldImplementation(new ScaffoldTypeOptions
            {
                CustomFields = methodInfoFields.ToImmutableArray(),
                AddMethodBody = CreateMethodBody,
                CustomizeConstructor = () => (CreateConstructorParameter(), [pretendFieldAssignment]),
            });

            // TODO: Add properties

            // TODO: Generate debugger display
            classDeclaration = classDeclaration
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)));

            SyntaxTriviaList leadingTrivia = TriviaList(
                Comment("// <auto-generated/>"),
                Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)),
                Comment("/// <inheritdoc/>"));

            return CompilationUnit()
                .AddMembers(classDeclaration.WithLeadingTrivia(leadingTrivia))
                .WithLeadingTrivia(leadingTrivia)
                .NormalizeWhitespace();
        }

        private static FieldDeclarationSyntax CreateMethodInfoField(IMethodSymbol method, ExpressionSyntax expressionSyntax)
        {
            // public static readonly MethodInfo_name_4B2 = <expression>!;
            return FieldDeclaration(VariableDeclaration(ParseTypeName("global::System.Reflection.MethodInfo")))
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
                TypeOfExpression(ParseTypeName(TypeToPretend.ToFullDisplayString())),
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
            return GenericName(Identifier("global::Pretender.Pretend"),
                    TypeArgumentList(SingletonSeparatedList(ParseTypeName(TypeToPretend.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))));
        }

        private FieldDeclarationSyntax GetStaticMethodCacheField(IMethodSymbol method, int index)
        {
            // TODO: Get method info via argument types
            return FieldDeclaration(VariableDeclaration(ParseTypeName("global::System.Reflection.MethodInfo")))
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword))
                .AddDeclarationVariables(VariableDeclarator(Identifier($"__methodInfo_{method.Name}_{index}"))
                    .WithInitializer(EqualsValueClause(
                        InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, TypeOfExpression(ParseTypeName(TypeToPretend.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))), IdentifierName("GetMethod")))
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(ParseExpression($"nameof({method.Name})"))))))));
        }

        private BlockSyntax CreateMethodBody(IMethodSymbol method)
        {
            var methodBodyStatements = new List<StatementSyntax>();

            var collectionExpression = CollectionExpression()
                .AddElements(method.Parameters.Select(p =>
                {
                    return ExpressionElement(IdentifierName(p.Name));
                }).ToArray());

            // ReadOnlySpan<object?> arguments = [arg0, arg1];

            var argumentsDeclaration = LocalDeclarationStatement(
                VariableDeclaration(
                    // ReadOnlySpan<object?>
                    GenericName("Span").AddTypeArgumentListArguments(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword))))
                )
                .AddVariables(VariableDeclarator("arguments")
                    .WithInitializer(EqualsValueClause(collectionExpression))
                )
            );

            methodBodyStatements.Add(argumentsDeclaration);

            // var callInfo = new CallInfo(__methodInfo_MethodName_0, arguments);
            var callInfoCreation = LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(VariableDeclarator(Identifier("callInfo"))
                        .WithInitializer(EqualsValueClause(ObjectCreationExpression(ParseTypeName("global::Pretender.CallInfo"))
                            .AddArgumentListArguments(Argument(IdentifierName(method.ToMethodInfoCacheName())), Argument(IdentifierName("arguments")))))));

            methodBodyStatements.Add(callInfoCreation);

            // _pretend.Handle(callInfo);
            var handleCall = ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("_pretend"),
                IdentifierName("Handle")))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName("callInfo")).WithRefKindKeyword(Token(SyntaxKind.RefKeyword))))));

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
                        IdentifierName("arguments"),
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
                        ParseTypeName(method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("callInfo"), IdentifierName("ReturnValue"))));

                methodBodyStatements.Add(returnStatement);
            }

            return Block(methodBodyStatements);
        }
    }
}
