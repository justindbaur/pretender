using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;
using System;
using System.Linq;

namespace Pretender.SourceGenerator
{
    internal class SetupArgument(IArgumentOperation argumentOperation, int index)
    {
        private static readonly IdentifierNameSyntax CallInfoIdentifier = IdentifierName("callInfo");
        private static readonly IdentifierNameSyntax ArgumentsPropertyIdentifier = IdentifierName("Arguments");

        private readonly int _index = index;

        public IArgumentOperation ArgumentOperation { get; } = argumentOperation;

        public bool IsLiteral => ArgumentOperation.Value is ILiteralOperation;
        public bool IsInvocation => ArgumentOperation.Value is IInvocationOperation;
        public ITypeSymbol ParameterType => ArgumentOperation.Parameter!.Type;
        public string ArgumentLocalName => $"{ArgumentOperation.Parameter!.Name}_arg";


        public LocalDeclarationStatementSyntax EmitArgumentAccessor()
        {
            // (string?)callInfo.Arguments[index];
            ExpressionSyntax argumentGetter = CastExpression(
                    ParameterType.AsUnknownTypeSyntax(),
                    ElementAccessExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, CallInfoIdentifier, ArgumentsPropertyIdentifier))
                        .AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(_index))))
                    );

            // var name_arg = (string?)callInfo.Arguments[0];
            return LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                .AddVariables(VariableDeclarator(ArgumentLocalName)
                    .WithInitializer(EqualsValueClause(argumentGetter))));
        }

        public bool TryEmitInvocationStatements(out StatementSyntax[] statements)
        {
            Debug.Assert(IsInvocation, "Should have been asserted already.");
            var invocationOperation = (IInvocationOperation)ArgumentOperation.Value;
            if (TryGetMatcherAttributeType(invocationOperation, out var matcherType))
            {
                // AnyMatcher
                if (matcherType.EqualsByName(["Pretender", "Matchers", "AnyMatcher"]))
                {
                    statements = [];
                    return true;
                }

                var arguments = new ArgumentSyntax[invocationOperation.Arguments.Length];
                bool allArgumentsSafe = true;

                for ( int i = 0; i < arguments.Length; i++)
                {
                    var arg = invocationOperation.Arguments[i];
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
                    statements = [];
                    return false;
                }

                statements = new StatementSyntax[3];
                statements[0] = EmitArgumentAccessor();

                var matcherLocalName = $"{ArgumentOperation.Parameter!.Name}_matcher";

                // var name_matcher = new global::MyMatcher(arg0, arg1);
                statements[1] = LocalDeclarationStatement(
                    VariableDeclaration(ParseTypeName("var"))
                        .AddVariables(VariableDeclarator(matcherLocalName)
                            .WithInitializer(EqualsValueClause(ObjectCreationExpression(ParseTypeName(matcherType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
                                .AddArgumentListArguments(arguments))
                            )
                        )
                    );

                statements[2] = CreateArgumentCheck(
                    PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(matcherLocalName),
                                IdentifierName("Matches")
                            )
                        )
                        .AddArgumentListArguments(Argument(IdentifierName(ArgumentLocalName)))
                    )
                );

                return true;
            }
            else
            {
                // TODO: Setup static listener
                statements = [];
                return false;
            }
        }

        private bool TryGetMatcherAttributeType(IInvocationOperation invocationOperation, out INamedTypeSymbol matcherType)
        {
            var allAttributes = invocationOperation.TargetMethod.GetAttributes();
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
                if (invocationOperation.TargetMethod.TypeArguments.Length != matcherType.TypeArguments.Length)
                {
                    return false;
                }

                matcherType = matcherType.ConstructedFrom.Construct([.. invocationOperation.TargetMethod.TypeArguments]);
            }

            return true;
        }

        public IfStatementSyntax EmitLiteralIfCheck()
        {
            Debug.Assert(IsLiteral, "This should only be called if you have already checked it's a literal operation.");
            var binaryExpression = BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                IdentifierName(ArgumentLocalName),
                ((ILiteralOperation)ArgumentOperation.Value).ToLiteralExpression()
            );

            return CreateArgumentCheck(binaryExpression);
        }

        private static IfStatementSyntax CreateArgumentCheck(ExpressionSyntax condition)
        {
            return IfStatement(condition, Block(
                    ReturnStatement(LiteralExpression(SyntaxKind.FalseLiteralExpression))
                )
            );
        }
    }
}
