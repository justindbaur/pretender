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

            Debug.Assert(invocationOperation.Type is INamedTypeSymbol, "This should have been asserted via making sure it's the right invocation.");

            var pretendType = ((INamedTypeSymbol)invocationOperation.Type!).TypeArguments[0];

            var setupMethod = SimplifyOperation(setupExpressionArg.Value, pretendType);

            PretendType = pretendType;

            if (setupMethod == default)
            {
                // Make sure one diagnostic at least is made about the failure to find
                // the method symbol
                if (Diagnostics.Count == 0)
                {
                    // TODO: Better Error diagnostic
                    Diagnostics.Add(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidSetupArgument,
                        invocationOperation.Arguments[0].Syntax.GetLocation(),
                        "ones we can't simplify"
                        ));
                }
                return;
            }

            SetupMethod = setupMethod.Method;

            if (setupMethod.Arguments != default)
            {
                foreach (var argument in setupMethod.Arguments)
                {
                    ValidateArgument(argument);
                }
            }

            Arguments = setupMethod.Arguments;
        }

        public IInvocationOperation OriginalInvocation { get; }
        public ITypeSymbol PretendType { get; }
        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();
        public IMethodSymbol SetupMethod { get; } = null!;
        public ImmutableArray<IArgumentOperation> Arguments { get; }

        public MethodDeclarationSyntax GetMethodDeclaration(int index)
        {
            var statements = new List<StatementSyntax>();

            var returnTypeString = SetupMethod.ReturnsVoid
                ? null
                : SetupMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var typeArgumentList = SetupMethod.ReturnsVoid
                ? TypeArgumentList(SingletonSeparatedList(ParseTypeName(PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))
                : TypeArgumentList(SeparatedList([ParseTypeName(PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)), ParseTypeName(returnTypeString!)]));

            var returnType = GenericName("IPretendSetup")
                .WithTypeArgumentList(typeArgumentList);

            var matchStatements = new List<StatementSyntax>();

            // Match method info first

            if (Arguments != default)
            {
                var needsCapturer = new bool[Arguments.Length];

                for (var i = 0; i < Arguments.Length; i++)
                {
                    var argument = Arguments[i];
                    var setupArgument = new SetupArgument(argument, i);

                    if (setupArgument.IsLiteral)
                    {
                        matchStatements.Add(setupArgument.EmitArgumentAccessor());
                        matchStatements.Add(setupArgument.EmitLiteralIfCheck());
                    }
                    else if (setupArgument.IsInvocation)
                    {
                        if (setupArgument.TryEmitInvocationStatements(out var invocationStatements))
                        {
                            matchStatements.AddRange(invocationStatements);
                            continue;
                        }

                        // Need a capturer
                        needsCapturer[i] = true;
                    }
                    else
                    {
                        throw new NotImplementedException($"We have not implemented arguments of kind '{argument.Kind}', please file an issue.");
                    }
                    // TODO: More Argument types
                }

                if (needsCapturer.Any(i => i))
                {
                    // Generate type
                    // Create argument capturer
                    // Create static matcher listener
                    // call method
                    // close listener
                    // Read arguments
                }
            }

            ArgumentSyntax matcherArgument;
            if (matchStatements.Count == 0)
            {
                // Nothing actually needs to match this will always return true.
                matcherArgument = Argument(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParseTypeName("Cache"),
                    IdentifierName("NoOpMatcher")))
                        .WithNameColon(NameColon("matcher"));
            }
            else
            {
                matchStatements.Add(ReturnStatement(LiteralExpression(SyntaxKind.TrueLiteralExpression)));

                /*
                 * Matcher matchCall = static (CallInfo callInfo) =>
                 * {
                 *     ...matching calls...
                 *     return true;
                 * }
                 */
                var matcherDelegate = ParenthesizedLambdaExpression(
                ParameterList(SingletonSeparatedList(Parameter(Identifier("callInfo")))),
                Block(matchStatements))
                    .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)));

                statements.Add(LocalDeclarationStatement(VariableDeclaration(
                ParseTypeName("Matcher"))
                    .WithVariables(SingletonSeparatedList(
                    VariableDeclarator("matchCall")
                            .WithInitializer(EqualsValueClause(matcherDelegate))))));

                matcherArgument = Argument(IdentifierName("matchCall"));
            }

            GenericNameSyntax returnObjectType;
            var objectCreationArguments = ArgumentList(
                SeparatedList(new[]
                {
                    Argument(IdentifierName("pretend")),
                    Argument(IdentifierName("setupExpression")),
                    Argument(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(PretendType.ToPretendName()),
                        IdentifierName(SetupMethod.ToMethodInfoCacheName())
                        )),
                    matcherArgument
                }));

            if (SetupMethod.ReturnsVoid)
            {
                returnObjectType = GenericName("VoidCompiledSetup")
                    .WithTypeArgumentList(typeArgumentList);
            }
            else
            {
                returnObjectType = GenericName("ReturningCompiledSetup")
                    .WithTypeArgumentList(typeArgumentList);

                ExpressionSyntax additionalArgument;

                if (SetupMethod.ReturnType.EqualsByName(["System", "Threading", "Tasks", "Task"]))
                {
                    if (SetupMethod.ReturnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
                    {
                        additionalArgument = ParseExpression($"Task.FromResult<{namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(default)");
                    }
                    else
                    {
                        additionalArgument = ParseExpression("Task.CompletedTask");
                    }
                }
                else if (SetupMethod.ReturnType.EqualsByName(["System", "Threading", "Tasks", "ValueTask"]))
                {
                    if (SetupMethod.ReturnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
                    {
                        additionalArgument = ParseExpression($"ValueTask.FromResult<{namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(default)");
                    }
                    else
                    {
                        additionalArgument = ParseExpression("ValueTask.CompletedTask");
                    }
                }
                else
                {
                    // TODO: Support custom awaitable
                    additionalArgument = ParseExpression("default");
                }

                objectCreationArguments = objectCreationArguments.AddArguments(Argument(additionalArgument)
                    .WithNameColon(NameColon("defaultValue")));
            }

            var compiledSetupCreation = ObjectCreationExpression(returnObjectType)
                    .WithArgumentList(objectCreationArguments);

            // var setup = new CompiledSetup(pretend, setupExpression, matchCall);
            statements.Add(LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                .WithVariables(SingletonSeparatedList(VariableDeclarator("setup")
                    .WithInitializer(EqualsValueClause(compiledSetupCreation))))));

            // pretend.Add(setup);
            statements.Add(ExpressionStatement(InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("pretend"), IdentifierName("AddSetup")),
                ArgumentList(SingletonSeparatedList(Argument(IdentifierName("setup")))))));

            var returnSetupCall = ReturnStatement(IdentifierName("setup"));

            statements.Add(returnSetupCall);

            var interceptsLocation = new InterceptsLocationInfo(OriginalInvocation);

            return MethodDeclaration(returnType, $"Setup{index}")
                .WithBody(Block(statements.ToArray()))
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("pretend"))
                        .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                        .WithType(ParseTypeName($"Pretend<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>")),

                    Parameter(Identifier("setupExpression"))
                        .WithType(GenericName(SetupMethod.ReturnsVoid ? "Action" : "Func").WithTypeArgumentList(typeArgumentList)),
                })))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithAttributeLists(SingletonList(AttributeList(
                    SingletonSeparatedList(interceptsLocation.ToAttributeSyntax()))));
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
                    operation.Syntax.GetLocation(),
                    "Instance invocation"
                    ));
                return false;
            }

            if (!operation.TargetMethod.IsStatic)
            {
                Diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidSetupArgument,
                    operation.Syntax.GetLocation(),
                    "invocation where the method is not static."
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
                    operation.Syntax.GetLocation(),
                    "Static invocation with no matcher attribute on method"
                    ));
                return false;
            }

            // TODO: Validate the matcher attribute further

            return true;
        }

        private (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) SimplifyBlockOperation(IBlockOperation operation, ITypeSymbol pretendType)
        {
            foreach (var childOperation in operation.Operations)
            {
                var method = SimplifyOperation(childOperation, pretendType);
                if (method != default)
                {
                    return method;
                }
            }

            return default;
        }

        private (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) SimplifyReturnOperation(IReturnOperation operation, ITypeSymbol pretendType)
        {
            return operation.ReturnedValue switch
            {
                not null => SimplifyOperation(operation.ReturnedValue, pretendType),
                // If there is not returned value, this is a dead end.
                _ => default,
            };
        }

        private (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) SimplifyOperation(IOperation operation, ITypeSymbol pretendType)
        {
            // TODO: Support more operations
            return operation.Kind switch
            {
                OperationKind.Return => SimplifyReturnOperation((IReturnOperation)operation, pretendType),
                OperationKind.Conversion => SimplifyOperation(((IConversionOperation)operation).Operand, pretendType),
                OperationKind.Block => SimplifyBlockOperation((IBlockOperation)operation, pretendType),
                OperationKind.AnonymousFunction => SimplifyOperation(((IAnonymousFunctionOperation)operation).Body, pretendType),
                OperationKind.Invocation => TryMethod((IInvocationOperation)operation, pretendType),
                // ExpressionStatement is probably a dead path now but who cares
                OperationKind.ExpressionStatement => SimplifyOperation(((IExpressionStatementOperation)operation).Operation, pretendType),
                OperationKind.DelegateCreation => SimplifyOperation(((IDelegateCreationOperation)operation).Target, pretendType),
                // TODO: Do something for SetupSet to get the set method instead
                OperationKind.PropertyReference => TryProperty((IPropertyReferenceOperation)operation),
                _ => default,
            };
        }

        private static (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) TryProperty(IPropertyReferenceOperation propertyReference)
        {
            var getMethod = propertyReference.Property.GetMethod;

            // TODO: Validate the get method exists on the type we are pretending
            // and that the return type it's returning is the expected one
            if (getMethod != null)
            {
                return (getMethod, []);
            }

            return default;
        }

        private (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) TryMethod(IInvocationOperation operation, ITypeSymbol pretendType)
        {
            var instance = operation.Instance;
            var method = operation.TargetMethod;

            // It should have an instance because it should be called from the pretend from the Func<,>/Action<>
            if (instance == null)
            {
                // TODO: Add diagnostic
                return default;
            }

            if (instance is IParameterReferenceOperation parameter)
            {
                if (!SymbolEqualityComparer.Default.Equals(parameter.Type, pretendType))
                {
                    return default;
                }
            }
            // TODO: Should we allow any other instance operation types?

            var pretendMethods = pretendType.GetMembers()
                .OfType<IMethodSymbol>();

            if (!pretendMethods.Contains(method, SymbolEqualityComparer.Default))
            {
                // This is not a method that exists on the pretend type
                // TODO: We could inspect the method body further
                return default;
            }

            return (operation.TargetMethod, operation.Arguments);
        }
    }
}
