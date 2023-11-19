using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.SetupArguments
{
    internal class InvocationArgumentSpec : SetupArgumentSpec
    {
        private readonly IInvocationOperation _invocationOperation;

        public InvocationArgumentSpec(
            IInvocationOperation invocationOperation,
            IArgumentOperation originalArgument,
            int argumentPlacement)
            : base(originalArgument, argumentPlacement)
        {
            _invocationOperation = invocationOperation;
        }

        private ImmutableArray<StatementSyntax>? _cachedMatcherStatements;

        public override int NeededMatcherStatements
        {
            get
            {
                if (TryGetMatcherAttributeType(out var matcherType))
                {
                    if (matcherType.EqualsByName(["Pretender", "Matchers", "AnyMatcher"]))
                    {
                        _cachedMatcherStatements = ImmutableArray<StatementSyntax>.Empty;
                        return 0;
                    }

                    var arguments = new ArgumentSyntax[_invocationOperation.Arguments.Length];
                    bool allArgumentsSafe = true;

                    for (int i = 0; i < arguments.Length; i++)
                    {
                        var arg = _invocationOperation.Arguments[i];
                        if (arg.Value is ILiteralOperation literalOperation)
                        {
                            arguments[i] = Argument(literalOperation.ToLiteralExpression());
                        }
                        else if (arg.Value is IDelegateCreationOperation delegateCreation)
                        {

                            if (delegateCreation.Target is IAnonymousFunctionOperation anonymousFunctionOperation)
                            {
                                if (anonymousFunctionOperation.Symbol.IsStatic) // This isn't enough either though, they could call a static method that only exists in their context
                                {
                                    // If it's guaranteed to be static, we can just rewrite it in our code
                                    arguments[i] = Argument(ParseExpression(delegateCreation.Syntax.GetText().ToString()));
                                }
                                else if (false) // Is non-scope capturing
                                {
                                    // This is a lot more work but also very powerful in terms of speed
                                    // We need to rewrite the delegate and replace all local references with our getter
                                    allArgumentsSafe = false;
                                }
                                else
                                {
                                    // We need a static matcher
                                    allArgumentsSafe = false;
                                }
                            }
                            else
                            {
                                allArgumentsSafe = false;
                            }
                        }
                        else
                        {
                            allArgumentsSafe = false;
                        }
                    }

                    if (!allArgumentsSafe)
                    {
                        _cachedMatcherStatements = ImmutableArray<StatementSyntax>.Empty;
                        return 0;
                    }

                    var statements = new StatementSyntax[3];
                    var (identifier, accessor) = CreateArgumentAccessor();
                    statements[0] = accessor;

                    var matcherLocalName = $"{Parameter.Name}_matcher";

                    // var name_matcher = new global::MyMatcher(arg0, arg1);
                    statements[1] = LocalDeclarationStatement(
                        VariableDeclaration(ParseTypeName("var"))
                            .AddVariables(VariableDeclarator(matcherLocalName)
                                .WithInitializer(EqualsValueClause(ObjectCreationExpression(ParseTypeName(matcherType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
                                    .AddArgumentListArguments(arguments))
                                )
                            )
                        );

                    statements[2] = CreateIfCheck(
                        PrefixUnaryExpression(
                            SyntaxKind.LogicalNotExpression,
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(matcherLocalName),
                                    IdentifierName("Matches")
                                )
                            )
                            .AddArgumentListArguments(Argument(IdentifierName(identifier)))
                        )
                    );

                    _cachedMatcherStatements = ImmutableArray.Create(statements);
                    return 3;
                }
                else
                {
                    _cachedMatcherStatements = ImmutableArray<StatementSyntax>.Empty;
                    return 0;
                }
            }
        }

        public override ImmutableArray<StatementSyntax> CreateMatcherStatements(CancellationToken cancellationToken)
        {
            Debug.Assert(_cachedMatcherStatements.HasValue, "Should have called NeededStatements first.");
            return _cachedMatcherStatements!.Value;
        }

        private bool TryGetMatcherAttributeType(out INamedTypeSymbol matcherType)
        {
            var allAttributes = _invocationOperation.TargetMethod.GetAttributes();
            var matcherAttribute = allAttributes.Single(ad => ad.AttributeClass!.EqualsByName(["Pretender", "Matchers", "MatcherAttribute"]));

            matcherType = null!;

            if (matcherAttribute.AttributeClass!.IsGenericType)
            {
                // We are in the typed version, get the generic arg
                matcherType = (INamedTypeSymbol)matcherAttribute.AttributeClass.TypeArguments[0];
            }
            else
            {
                // We are in the base version, get the constructor arg
                // TODO: Make this work
                // matcherType = matcherAttribute.ConstructorArguments[0];
                var attributeType = matcherAttribute.ConstructorArguments[0];
                // TODO: When can Type be null?
                if (!attributeType.Type!.EqualsByName(["System", "Type"]))
                {
                    return false;
                }

                if (attributeType.Value is null)
                {
                    return false;
                }

                matcherType = (INamedTypeSymbol)attributeType.Value!;
            }

            // TODO: Write a lot more tests for this
            if (matcherType.IsUnboundGenericType)
            {
                if (_invocationOperation.TargetMethod.TypeArguments.Length != matcherType.TypeArguments.Length)
                {
                    return false;
                }

                matcherType = matcherType.ConstructedFrom.Construct([.._invocationOperation.TargetMethod.TypeArguments]);
            }

            return true;
        }

        public override int GetHashCode()
        {
            // TODO: This is not enought for uniqueness
            return SymbolEqualityComparer.Default.GetHashCode(_invocationOperation.TargetMethod);
        }
    }
}
