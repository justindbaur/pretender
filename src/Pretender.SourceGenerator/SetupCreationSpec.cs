using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.SetupArguments;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator
{
    internal class SetupCreationSpec
    {
        private readonly IArgumentOperation _setupArgument;
        private readonly ITypeSymbol _pretendType;
        private readonly bool _useSetMethod;

        private readonly IMethodSymbol? _setupMethod;
        private readonly ImmutableArray<SetupArgumentSpec> _argumentSpecs;

        public SetupCreationSpec(IArgumentOperation setupArgument, ITypeSymbol pretendType, bool useSetMethod)
        {
            _setupArgument = setupArgument;
            _pretendType = pretendType;
            _useSetMethod = useSetMethod;

            var candidates = GetInvocationCandidates();

            if (candidates.Length == 0)
            {
                // TODO: Add diagnostic
                return;
            }
            else if (candidates.Length != 1)
            {
                // TODO: Add diagnostic
                return;
            }

            var candidate = candidates[0];
            _setupMethod = candidate.Method;

            var builder = ImmutableArray.CreateBuilder<SetupArgumentSpec>(candidate.Arguments.Length);
            for (var i = 0; i < candidate.Arguments.Length; i++)
            {
                builder.Add(SetupArgumentSpec.Create(candidate.Arguments[i], i));
            }

            _argumentSpecs = builder.MoveToImmutable();

            // TODO: Get argument specs diagnostics and make them my own
        }

        private ImmutableArray<InvocationCandidate> GetInvocationCandidates()
        {
            var builder = ImmutableArray.CreateBuilder<InvocationCandidate>();
            TraverseOperation(_setupArgument.Value, builder);
            return builder.ToImmutable();
        }

        private void TraverseOperation(IOperation operation, ImmutableArray<InvocationCandidate>.Builder invocationCandidates)
        {
            switch (operation.Kind)
            {
                case OperationKind.Block:
                    var blockOperation = (IBlockOperation)operation;
                    TraverseOperationList(blockOperation.Operations, invocationCandidates);
                    break;
                case OperationKind.Return:
                    var returnOperation = (IReturnOperation)operation;
                    if (returnOperation.ReturnedValue != null)
                    {
                        TraverseOperation(returnOperation.ReturnedValue, invocationCandidates);
                    }
                    break;
                case OperationKind.ExpressionStatement:
                    var expressionStatement = (IExpressionStatementOperation)operation;
                    TraverseOperation(expressionStatement.Operation, invocationCandidates);
                    break;
                case OperationKind.Conversion:
                    var conversionOperation = (IConversionOperation)operation;
                    TraverseOperation(conversionOperation.Operand, invocationCandidates);
                    break;
                case OperationKind.Invocation:
                    var invocationOperation = (IInvocationOperation)operation;
                    TryMatchInvocationOperation(invocationOperation, invocationCandidates);
                    break;
                case OperationKind.PropertyReference:
                    var propertyReferenceOperation = (IPropertyReferenceOperation)operation;
                    TryMatchPropertyReference(propertyReferenceOperation, invocationCandidates);
                    break;
                case OperationKind.AnonymousFunction:
                    var anonymousFunctionOperation = (IAnonymousFunctionOperation)operation;
                    TraverseOperation(anonymousFunctionOperation.Body, invocationCandidates);
                    break;
                case OperationKind.DelegateCreation:
                    var delegateCreationOperation = (IDelegateCreationOperation)operation;
                    TraverseOperation(delegateCreationOperation.Target, invocationCandidates);
                    break;
                default:
#if DEBUG
                    // TODO: Figure out what operation caused this, it's not ideal to "randomly" support operations
                    Debugger.Launch();
#endif
                    // Absolute fallback, most of our operations can be supported this way but it's nicer to be explicit
                    TraverseOperationList(operation.ChildOperations, invocationCandidates);
                    break;
            }
        }

        private void TraverseOperationList(IEnumerable<IOperation> operations, ImmutableArray<InvocationCandidate>.Builder invocationCandidates)
        {
            foreach (var operation in operations)
            {
                TraverseOperation(operation, invocationCandidates);
            }
        }

        private void TryMatchPropertyReference(IPropertyReferenceOperation propertyReference, ImmutableArray<InvocationCandidate>.Builder invocationCandidates)
        {
            if (propertyReference.Instance is not IParameterReferenceOperation parameterReference)
            {
                return;
            }

            if (!SymbolEqualityComparer.Default.Equals(parameterReference.Type, _pretendType))
            {
                return;
            }

            var method = _useSetMethod
                ? propertyReference.Property.SetMethod
                : propertyReference.Property.GetMethod;

            if (method == null)
            {
                return;
            }

            invocationCandidates.Add(new InvocationCandidate(method, ImmutableArray<IArgumentOperation>.Empty));
        }

        private void TryMatchInvocationOperation(IInvocationOperation invocation, ImmutableArray<InvocationCandidate>.Builder invocationCandidates)
        {
            if (invocation.Instance is not IParameterReferenceOperation parameterReference)
            {
                return;
            }

            if (!SymbolEqualityComparer.Default.Equals(parameterReference.Type, _pretendType))
            {
                return;
            }

            // TODO: Any more validation?

            invocationCandidates.Add(new InvocationCandidate(invocation.TargetMethod, invocation.Arguments));
        }

        public InvocationExpressionSyntax CreateSetupGetter(CancellationToken cancellationToken)
        {
            Debug.Assert(_setupMethod is not null, "A setup method could not be found, which means there should have been error diagnostics and this method should not have ran.");

            var totalMatchStatements = _argumentSpecs.Sum(sa => sa.NeededMatcherStatements);
            cancellationToken.ThrowIfCancellationRequested();

            var matchStatements = new StatementSyntax[totalMatchStatements];
            int addedStatements = 0;

            for (var i = 0; i < _argumentSpecs.Length; i++)
            {
                var argument = _argumentSpecs[i];

                var newStatements = argument.CreateMatcherStatements(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                newStatements.CopyTo(matchStatements, addedStatements);
                addedStatements += newStatements.Length;
            }

            ArgumentSyntax matcherArgument;
            ImmutableArray<StatementSyntax> statements;
            if (matchStatements.Length == 0)
            {
                statements = ImmutableArray<StatementSyntax>.Empty;

                // Nothing actually needs to match this will always return true, so we use a cached matcher that always returns true
                matcherArgument = Argument(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParseTypeName("Cache"),
                    IdentifierName("NoOpMatcher")))
                        .WithNameColon(NameColon("matcher"));
            }
            else
            {
                // Other match statements should have added all the ways the method could return false
                // so if it gets through all those statements it should return true at the end.
                var trueReturnStatement = ReturnStatement(LiteralExpression(SyntaxKind.TrueLiteralExpression));

                /*
                 * Matcher matchCall = static (callInfo, target) =>
                 * {
                 *     ...matching calls...
                 *     return true;
                 * }
                 */
                var matchCallIdentifier = Identifier("matchCall");

                var matcherDelegate = ParenthesizedLambdaExpression(
                ParameterList(SeparatedList([
                        Parameter(Identifier("callInfo")),
                        Parameter(Identifier("target"))
                    ])),
                Block(List([.. matchStatements, trueReturnStatement])))
                    .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)));

                statements = ImmutableArray.Create<StatementSyntax>(LocalDeclarationStatement(VariableDeclaration(
                ParseTypeName("Matcher"))
                    .WithVariables(SingletonSeparatedList(
                    VariableDeclarator(matchCallIdentifier)
                            .WithInitializer(EqualsValueClause(matcherDelegate))))));

                matcherArgument = Argument(IdentifierName(matchCallIdentifier));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var objectCreationArguments = ArgumentList(
                SeparatedList(new[]
                {
                    Argument(IdentifierName("pretend")),
                    //Argument(IdentifierName("setupExpression")),
                    Argument(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(_pretendType.ToPretendName()),
                        IdentifierName(_setupMethod!.ToMethodInfoCacheName())
                        )),
                    matcherArgument,
                    Argument(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("expr"),
                        IdentifierName("Target")
                        )),
                }));

            cancellationToken.ThrowIfCancellationRequested();

            GenericNameSyntax returnObjectName;
            SimpleNameSyntax getOrCreateName;
            if (_setupMethod!.ReturnsVoid)
            {
                // VoidCompiledSetup<T>
                returnObjectName = GenericName("VoidCompiledSetup")
                    .AddTypeArgumentListArguments(ParseTypeName(_pretendType.ToFullDisplayString()));

                getOrCreateName = IdentifierName("GetOrCreateSetup");
            }
            else
            {
                // ReturningCompiledSetup<T1, T2>
                returnObjectName = GenericName("ReturningCompiledSetup")
                    .AddTypeArgumentListArguments(
                        ParseTypeName(_pretendType.ToFullDisplayString()),
                        _setupMethod.ReturnType.AsUnknownTypeSyntax());

                getOrCreateName = GenericName("GetOrCreateSetup")
                    .AddTypeArgumentListArguments(_setupMethod.ReturnType.AsUnknownTypeSyntax());

                // TODO: Recursively mock?
                ExpressionSyntax defaultValue;

                if (_setupMethod.ReturnType.EqualsByName(["System", "Threading", "Tasks", "Task"]))
                {
                    if (_setupMethod.ReturnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
                    {
                        // Task.FromResult<T>(default)
                        defaultValue = KnownBlocks.TaskFromResult(
                            namedType.TypeArguments[0].AsUnknownTypeSyntax(),
                            LiteralExpression(SyntaxKind.DefaultLiteralExpression));
                    }
                    else
                    {
                        // Task.CompletedTask
                        defaultValue = KnownBlocks.TaskCompletedTask;
                    }
                }
                else if (_setupMethod.ReturnType.EqualsByName(["System", "Threading", "Tasks", "ValueTask"]))
                {
                    if (_setupMethod.ReturnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
                    {
                        // ValueTask.FromResult<T>(default)
                        defaultValue = KnownBlocks.ValueTaskFromResult(
                            namedType.TypeArguments[0].AsUnknownTypeSyntax(),
                            LiteralExpression(SyntaxKind.DefaultLiteralExpression)
                        );
                    }
                    else
                    {
                        // ValueTask.CompletedTask
                        defaultValue = KnownBlocks.ValueTaskCompletedTask;
                    }
                }
                else
                {
                    // TODO: Support custom awaitable
                    // default
                    defaultValue = LiteralExpression(SyntaxKind.DefaultLiteralExpression);
                }

                cancellationToken.ThrowIfCancellationRequested();

                objectCreationArguments = objectCreationArguments.AddArguments(Argument(
                    defaultValue).WithNameColon(NameColon("defaultValue")));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var compiledSetupCreation = ObjectCreationExpression(returnObjectName)
                .WithArgumentList(objectCreationArguments);

            // (pretend, expression) =>
            // {
            //     return new CompiledSetup();
            // }
            var creator = ParenthesizedLambdaExpression()
                .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                .AddParameterListParameters(Parameter(Identifier("pretend")), Parameter(Identifier("expr")))
                .AddBlockStatements([.. statements, ReturnStatement(compiledSetupCreation)]);

            // TODO: The hash code doesn't actually work, right now, this will create a new pretend every call.
            // We likely need to create our own class that can calculate the hash code and place that number in here.

            cancellationToken.ThrowIfCancellationRequested();
            // TODO: Should I have a different seed?
            //var badHashCode = _argumentSpecs.Aggregate(0, (agg, s) => HashCode.Combine(agg, s.GetHashCode()));
            var badHashCode = 0;

            cancellationToken.ThrowIfCancellationRequested();

            // pretend.GetOrCreateSetup()
            return InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("pretend"),
                getOrCreateName))
                .AddArgumentListArguments(
                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(badHashCode))),
                    Argument(creator),
                    Argument(IdentifierName("setupExpression")));
        }

        private class InvocationCandidate
        {
            public InvocationCandidate(IMethodSymbol methodSymbol, ImmutableArray<IArgumentOperation> argumentOperations)
            {
                Method = methodSymbol;
                Arguments = argumentOperations;
            }

            public IMethodSymbol Method { get; }
            public ImmutableArray<IArgumentOperation> Arguments { get; }
        }
    }
}
