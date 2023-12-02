using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Pretender.SourceGenerator.Parser;
using Pretender.SourceGenerator.SetupArguments;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.Emitter
{
    internal class SetupActionEmitter
    {
        private readonly ImmutableArray<SetupArgumentSpec> _setupArgumentSpecs;
        private readonly KnownTypeSymbols _knownTypeSymbols;

        public SetupActionEmitter(ITypeSymbol pretendType, IMethodSymbol setupMethod, ImmutableArray<SetupArgumentSpec> setupArgumentSpecs, KnownTypeSymbols knownTypeSymbols)
        {
            PretendType = pretendType;
            SetupMethod = setupMethod;
            _setupArgumentSpecs = setupArgumentSpecs;
            _knownTypeSymbols = knownTypeSymbols;
        }

        public ITypeSymbol PretendType { get; }
        public IMethodSymbol SetupMethod { get; }

        public InvocationExpressionSyntax CreateSetupGetter(CancellationToken cancellationToken)
        {
            var totalMatchStatements = _setupArgumentSpecs.Sum(sa => sa.NeededMatcherStatements);
            cancellationToken.ThrowIfCancellationRequested();

            var matchStatements = new StatementSyntax[totalMatchStatements];
            int addedStatements = 0;

            for (var i = 0; i < _setupArgumentSpecs.Length; i++)
            {
                var argument = _setupArgumentSpecs[i];

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
                        IdentifierName(PretendType.ToPretendName()),
                        IdentifierName(SetupMethod.ToMethodInfoCacheName())
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
            if (SetupMethod.ReturnsVoid)
            {
                // VoidCompiledSetup<T>
                returnObjectName = GenericName("VoidCompiledSetup")
                    .AddTypeArgumentListArguments(ParseTypeName(PretendType.ToFullDisplayString()));

                getOrCreateName = IdentifierName("GetOrCreateSetup");
            }
            else
            {

                // ReturningCompiledSetup<T1, T2>
                returnObjectName = GenericName("ReturningCompiledSetup")
                    .AddTypeArgumentListArguments(
                        ParseTypeName(PretendType.ToFullDisplayString()),
                        SetupMethod.ReturnType.AsUnknownTypeSyntax());

                getOrCreateName = GenericName("GetOrCreateSetup")
                    .AddTypeArgumentListArguments(SetupMethod.ReturnType.AsUnknownTypeSyntax());

                // TODO: Recursively mock?
                ExpressionSyntax defaultValue;

                // TODO: Is this safe?
                var namedType = (INamedTypeSymbol)SetupMethod.ReturnType;

                defaultValue = namedType.ToDefaultValueSyntax(_knownTypeSymbols);

                //if (SetupMethod.ReturnType.EqualsByName(["System", "Threading", "Tasks", "Task"]))
                //{
                //    if (SetupMethod.ReturnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
                //    {
                //        // Task.FromResult<T>(default)
                //        defaultValue = KnownBlocks.TaskFromResult(
                //            namedType.TypeArguments[0].AsUnknownTypeSyntax(),
                //            LiteralExpression(SyntaxKind.DefaultLiteralExpression));
                //    }
                //    else
                //    {
                //        // Task.CompletedTask
                //        defaultValue = KnownBlocks.TaskCompletedTask;
                //    }
                //}
                //else if (SetupMethod.ReturnType.EqualsByName(["System", "Threading", "Tasks", "ValueTask"]))
                //{
                //    if (SetupMethod.ReturnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
                //    {
                //        // ValueTask.FromResult<T>(default)
                //        defaultValue = KnownBlocks.ValueTaskFromResult(
                //            namedType.TypeArguments[0].AsUnknownTypeSyntax(),
                //            LiteralExpression(SyntaxKind.DefaultLiteralExpression)
                //        );
                //    }
                //    else
                //    {
                //        // ValueTask.CompletedTask
                //        defaultValue = KnownBlocks.ValueTaskCompletedTask;
                //    }
                //}
                //else
                //{
                //    // TODO: Support custom awaitable
                //    // default
                //    defaultValue = LiteralExpression(SyntaxKind.DefaultLiteralExpression);
                //}

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
    }
}
