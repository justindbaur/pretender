using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator
{
    internal class SetupEntrypoint
    {
        public SetupEntrypoint(IInvocationOperation invocationOperation)
        {
            OriginalInvocation = invocationOperation;
            var setupExpressionArg = invocationOperation.Arguments[0];
            var expressionType = setupExpressionArg.Parameter!.Type;
            ExpressionType = expressionType;

            Debug.Assert(invocationOperation.Type is INamedTypeSymbol, "This should have been asserted via making sure it's the right invocation.");

            var pretendType = ((INamedTypeSymbol)invocationOperation.Type!).TypeArguments[0];

            var setupInvocation = SimplifyOperation(setupExpressionArg.Value, pretendType);

            PretendType = pretendType;

            if (setupInvocation is null)
            {
                // TODO: Add Error diagnostic
                return;
            }

            var setupMethod = setupInvocation.TargetMethod;

            if (setupMethod.ReturnsVoid)
            {
                // The setup method returns void
            }
            else
            {
                // There is a return type
            }

            SetupMethod = setupMethod;

            foreach (var argument in setupInvocation.Arguments)
            {
                ValidateArgument(argument);
            }

            Arguments = setupInvocation.Arguments;
        }
        public IInvocationOperation OriginalInvocation { get; }
        public ITypeSymbol ExpressionType { get; }
        public ITypeSymbol PretendType { get; }
        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();
        public IMethodSymbol SetupMethod { get; } = null!;
        public ImmutableArray<IArgumentOperation> Arguments { get; }

        public MethodDeclarationSyntax GetMatcherDeclaration(int index)
        {
            var statements = new List<StatementSyntax>();

            TypeSyntax returnType = SetupMethod.ReturnsVoid
                ? ParseTypeName($"global::Pretender.IPretendSetup<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>")
                : ParseTypeName($"global::Pretender.IPretendSetup<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {SetupMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>");

            var matchStatements = new List<StatementSyntax>();

            for (var i = 0; i < Arguments.Length; i++)
            {
                var argument = Arguments[i];

                var argLocalName = $"{argument.Parameter!.Name}_arg{i}";
                // var arg1 = (object)callInfo.Arguments[0];
                matchStatements.Add(LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(argLocalName)
                            .WithInitializer(EqualsValueClause(CastExpression(
                                ParseTypeName(argument.Parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                ElementAccessExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("callInfo"), IdentifierName("Arguments")))
                                .AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i)))))))))));

                if (argument.Value is ILiteralOperation literalOperation)
                {
                    
                    var binaryExpression = BinaryExpression(
                        SyntaxKind.NotEqualsExpression, 
                        IdentifierName(argLocalName), 
                        CreateLiteralExpression(literalOperation));

                    var ifStatement = IfStatement(binaryExpression, Block(new[]
                    {
                        ReturnStatement(LiteralExpression(SyntaxKind.FalseLiteralExpression)),
                    }));

                    matchStatements.Add(ifStatement);
                }
                else if (argument.Value is IInvocationOperation invocationOperation)
                {
                    // We've already been asserted that this is a static method that has a MatcherAttribute on it
                    var allAttributes = invocationOperation.TargetMethod.GetAttributes();
                    var matcherAttribute = allAttributes.Single(ad => ad.AttributeClass!.EqualsByName(["Pretender", "Matchers", "MatcherAttribute"]));

                    ITypeSymbol matcherType;
                    if (matcherAttribute.AttributeClass!.IsGenericType)
                    {
                        // We are in the typed version, get the generic arg
                        matcherType = matcherAttribute.AttributeClass.TypeArguments[0];
                    }
                    else
                    {
                        // We are in the base version, get the constructor arg
                        // TODO: Make this work
                        // matcherType = matcherAttribute.ConstructorArguments[0];
                        throw new NotImplementedException("We have not quite implemented constructor args for matcher attribute");
                    }

                    var matcherLocalName = $"{argument.Parameter.Name}_matcher{i}";
                    // Create Matcher
                    // TODO: Pass in matcher arguments
                    // TODO: For speed, I can special case the any matcher and skip doing it at all
                    var t = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                            .WithVariables(SingletonSeparatedList(VariableDeclarator(matcherLocalName)
                                .WithInitializer(EqualsValueClause(ObjectCreationExpression(ParseTypeName(matcherType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
                                    .WithArgumentList(ArgumentList()))))));

                    matchStatements.Add(t);
                    // Run matcher

                    var ifStatement = IfStatement(PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                        InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(matcherLocalName), IdentifierName("Matches")))
                            .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName(argLocalName)))))),
                        Block(new[]
                        {
                            ReturnStatement(LiteralExpression(SyntaxKind.FalseLiteralExpression)),
                        }));
                    matchStatements.Add(ifStatement);
                }
            }

            matchStatements.Add(ReturnStatement(LiteralExpression(SyntaxKind.TrueLiteralExpression)));

            var lambda = ParenthesizedLambdaExpression(
                ParameterList(SingletonSeparatedList(Parameter(Identifier("callInfo")))),
                Block(matchStatements))
                    .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)));

            var matchesCall = LocalDeclarationStatement(VariableDeclaration(
                ParseTypeName("Func<CallInfo, bool>"))
                    .WithVariables(SingletonSeparatedList(
                    VariableDeclarator("matchCall")
                            .WithInitializer(EqualsValueClause(lambda)))));

            statements.Add(matchesCall);

            var returnObjectType = SetupMethod.ReturnsVoid
                ? $"CompiledSetup<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>"
                : $"CompiledSetup<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {SetupMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";

            var returnSetupCall = ReturnStatement(
                ObjectCreationExpression(ParseTypeName(returnObjectType))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]
                        {
                            Argument(IdentifierName("pretend")),
                            Argument(IdentifierName("setupExpression")),
                            Argument(IdentifierName("matchCall"))
                        })))
                );

            statements.Add(returnSetupCall);

            var memberSyntax = (MemberAccessExpressionSyntax)((InvocationExpressionSyntax)OriginalInvocation.Syntax).Expression;
            var operationSyntaxTree = OriginalInvocation.Syntax.SyntaxTree;
            var resolver = OriginalInvocation.SemanticModel?.Compilation.Options.SourceReferenceResolver;
            var filePath = resolver?.NormalizePath(operationSyntaxTree.FilePath, null) ?? operationSyntaxTree.FilePath;

            var linePosSpan = operationSyntaxTree.GetLineSpan(memberSyntax.Name.Span);
            var lineNumber = linePosSpan.StartLinePosition.Line + 1;
            var characterNumber = linePosSpan.StartLinePosition.Character + 1;

            var interceptor = Attribute(IdentifierName("InterceptsLocation"))
                    .WithArgumentList(AttributeArgumentList(SeparatedList(new[]
                    {
                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression, 
                                Literal(filePath))),

                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(lineNumber))),

                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(characterNumber))),
                    })));

            return MethodDeclaration(returnType, $"Setup{index}")
                .WithBody(Block(statements.ToArray()))
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("pretend"))
                        .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                        .WithType(ParseTypeName($"Pretend<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>")),

                    Parameter(Identifier("setupExpression"))
                        .WithType(ParseTypeName(ExpressionType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))),
                })))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithAttributeLists(SingletonList(AttributeList(
                    SingletonSeparatedList(interceptor))));
        }

        private LiteralExpressionSyntax CreateLiteralExpression(ILiteralOperation operation)
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

            throw new NotImplementedException($"We don't support literals of {operation.Type.Name} yet.");
        }

        private void ValidateArgument(IArgumentOperation operation)
        {
            var value = operation.Value;

            var hasSupport = value switch
            {
                ILiteralOperation => true,
                // TODO: It matchers
                IInvocationOperation invocationOperation => ValidateInvocationOperation(invocationOperation),
                _ => false,
            };
        }

        private bool ValidateInvocationOperation(IInvocationOperation operation)
        {
            if (operation.Instance != null)
            {
                // TODO: Make its own descriptor and offer fixer
                Diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidSetupArgument,
                    operation.Syntax.GetLocation()
                    ));
                return false;
            }

            if (!operation.TargetMethod.IsStatic)
            {
                Diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidSetupArgument,
                    operation.Syntax.GetLocation()
                    ));
                return false;
            }

            // TODO: Validate owning type and check for attributes
            var attributes = operation.TargetMethod.GetAttributes();

            // TODO: When can attribute class be null?
            // TODO: Validate this a little more
            var matcherAttribute = attributes.SingleOrDefault(
                ad => ad.AttributeClass!.Name == "MatcherAttribute");

            if (matcherAttribute is null)
            {
                // TODO: Make this be it's own descriptor
                Diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidSetupArgument,
                    operation.Syntax.GetLocation()
                    ));
                return false;
            }

            // TODO: Validate the matcher attribute further

            return true;
        }

        private static IInvocationOperation? SimplifyBlockOperation(IBlockOperation operation, ITypeSymbol pretendType)
        {
            foreach (var childOperation in operation.Operations)
            {
                var method = SimplifyOperation(childOperation, pretendType);
                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }

        private static IInvocationOperation? SimplifyReturnOperation(IReturnOperation operation, ITypeSymbol pretendType)
        {
            return operation.ReturnedValue switch
            {
                not null => SimplifyOperation(operation.ReturnedValue, pretendType),
                // If there is not returned value, this is a dead end.
                _ => null,
            };
        }

        private static IInvocationOperation? SimplifyOperation(IOperation operation, ITypeSymbol pretendType)
        {
            // TODO: Support more operations
            return operation.Kind switch
            {
                OperationKind.Return => SimplifyReturnOperation((IReturnOperation)operation, pretendType),
                OperationKind.Conversion => SimplifyOperation(((IConversionOperation)operation).Operand, pretendType),
                OperationKind.Block => SimplifyBlockOperation((IBlockOperation)operation, pretendType),
                OperationKind.AnonymousFunction => SimplifyOperation(((IAnonymousFunctionOperation)operation).Body, pretendType),
                OperationKind.Invocation => TryMethod((IInvocationOperation)operation, pretendType),
                _ => null,
            };
        }

        private static IInvocationOperation? TryMethod(IInvocationOperation operation, ITypeSymbol pretendType)
        {
            var instance = operation.Instance;
            var method = operation.TargetMethod;

            // It should have an instance because it should be called from the pretend from the Func<,>/Action<>
            if (instance == null)
            {
                return null;
            }

            if (instance is IParameterReferenceOperation parameter)
            {
                if (!SymbolEqualityComparer.Default.Equals(parameter.Type, pretendType))
                {
                    return null;
                }
            }
            // TODO: Should we allow any other instance operation types?

            var pretendMethods = pretendType.GetMembers()
                .OfType<IMethodSymbol>();

            if (!pretendMethods.Contains(method, SymbolEqualityComparer.Default))
            {
                // This is not a method that exists on the pretend type
                // TODO: We could inspect the method body further
                return null;
            }

            return operation;
        }
    }
}
