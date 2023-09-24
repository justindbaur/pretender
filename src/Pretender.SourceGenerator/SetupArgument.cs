using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;
using System;
using System.Linq;
using System.Collections.Immutable;

namespace Pretender.SourceGenerator
{
    internal class ArgumentTracker
    {
        private readonly List<ILocalReferenceOperation> _neededLocals = new();
        private readonly Stack<HashSet<ILocalSymbol>> _trackedLocals = new();

        public ArgumentTracker()
        {
            _trackedLocals = new Stack<HashSet<ILocalSymbol>>();
            _trackedLocals.Push(new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default));
        }

        public ImmutableArray<ILocalReferenceOperation> NeededLocals => _neededLocals.ToImmutableArray();
        public bool NeedsCapturer { get; private set; }
        public void SetNeedsCapturer()
        {
            NeedsCapturer = true;
        }

        public bool TryTrackLocal(ILocalReferenceOperation localReferenceOperation)
        {
            var currentScope = _trackedLocals.Peek();
            if (currentScope.Contains(localReferenceOperation.Local))
            {
                // This is being tracked as created during the current scope, ignore it
                return false;
            }

            _neededLocals.Add(localReferenceOperation);
            return true;
        }

        public void LocalDefined(ILocalSymbol local)
        {
            var currentScope = _trackedLocals.Peek();
            currentScope.Add(local);
        }

        public void LocalsDefined(IEnumerable<ILocalSymbol> locals)
        {
            var currentScope = _trackedLocals.Peek();
            foreach (var local in locals)
            {
                currentScope.Add(local);
            }
        }

        // TODO: could create an IDisposable for this
        public void EnterScope()
        {
            _trackedLocals.Push(new(SymbolEqualityComparer.Default));
        }

        public void ExitScope()
        {
            _trackedLocals.Pop();
        }
    }


    internal class SetupArgument
    {
        private static readonly IdentifierNameSyntax CallInfoIdentifier = IdentifierName("callInfo");
        private static readonly IdentifierNameSyntax ArgumentsPropertyIdentifier = IdentifierName("Arguments");

        private readonly int _index;

        public SetupArgument(IArgumentOperation argumentOperation, int index, List<Diagnostic> diagnostics)
        {
            var argOperationValue = argumentOperation.Value;
            var tracker = new ArgumentTracker();
            // Walk the operation tree to find all locals
            Visit(argOperationValue, tracker);

            RequiredLocals = tracker.NeededLocals;
            NeedsCapturer = tracker.NeedsCapturer;

            ArgumentOperation = argumentOperation;
            _index = index;
        }


        public ImmutableArray<ILocalReferenceOperation> RequiredLocals { get; }

        public IArgumentOperation ArgumentOperation { get; }

        public bool NeedsCapturer { get; }
        public bool IsLiteral => ArgumentOperation.Value is ILiteralOperation;
        public bool IsInvocation => ArgumentOperation.Value is IInvocationOperation;
        public bool IsLocalReference => ArgumentOperation.Value is ILocalReferenceOperation;

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

        // Returns true if the visited operation captured a local
        private static bool Visit(IOperation? operation, ArgumentTracker tracker)
        {
            if (operation == null)
            {
                return false;
            }

            // TODO: Handle most operations
            switch (operation.Kind)
            {
                case OperationKind.Block:
                    var block = (IBlockOperation)operation;
                    return VisitMany(block.Operations, tracker);
                case OperationKind.VariableDeclarationGroup:
                    var variableDeclarationGroup = (IVariableDeclarationGroupOperation)operation;
                    return VisitMany(variableDeclarationGroup.Declarations, tracker);
                
                case OperationKind.Return:
                    var returnOp = (IReturnOperation)operation;
                    return Visit(returnOp.ReturnedValue, tracker);
                case OperationKind.Literal:
                    // Literals are the best, they are easy and the end of the line
                    return false;
                case OperationKind.Invocation:
                    var invocation = (IInvocationOperation)operation;
                    // The instance could be a local itself
                    return Visit(invocation.Instance, tracker)
                        | VisitMany(invocation.Arguments, tracker);
                case OperationKind.LocalReference:
                    var local = (ILocalReferenceOperation)operation;
                    tracker.TryTrackLocal(local);
                    return true;
                case OperationKind.ParameterReference:
                    return false;
                case OperationKind.Binary:
                    var binary = (IBinaryOperation)operation;
                    return Visit(binary.LeftOperand, tracker) | Visit(binary.RightOperand, tracker);
                case OperationKind.AnonymousFunction:
                    // TODO: I'm not sure if this belongs in here or DelegateCreation but lets go with here for now
                    tracker.EnterScope();
                    var anonymousFunction = (IAnonymousFunctionOperation)operation;
                    var found = Visit(anonymousFunction.Body, tracker);
                    tracker.ExitScope();
                    return found;
                case OperationKind.DelegateCreation:
                    var delegateCreation = (IDelegateCreationOperation)operation;
                    // TODO: Now that we are in a delegate should we ignore their locals somehow?
                    return Visit(delegateCreation.Target, tracker);
                case OperationKind.VariableInitializer:
                    var variableInitializer = (IVariableInitializerOperation)operation;
                    tracker.LocalsDefined(variableInitializer.Locals);
                    // TODO: Not sure if this is right
                    Visit(variableInitializer.Value, tracker);
                    return true;
                case OperationKind.VariableDeclaration:
                    var variableDeclaration = (IVariableDeclarationOperation)operation;
                    return VisitMany(variableDeclaration.Declarators, tracker)
                        | Visit(variableDeclaration.Initializer, tracker);
                case OperationKind.VariableDeclarator:
                    var variableDeclarator = (IVariableDeclaratorOperation)operation;
                    tracker.LocalDefined(variableDeclarator.Symbol);
                    // TODO: IgnoredArguments property?
                    return Visit(variableDeclarator.Initializer, tracker);
                

                case OperationKind.Argument:
                    var argument = (IArgumentOperation)operation;
                    return Visit(argument.Value, tracker);
                

            }

            throw new NotImplementedException($"Can't visit operation '{operation.Kind}'");
        }

        private static bool VisitMany(IEnumerable<IOperation> operations, ArgumentTracker tracker)
        {
            var foundLocal = false;
            foreach (var operation in operations)
            {
                if (Visit(operation, tracker))
                {
                    foundLocal = true;
                }
            }

            return foundLocal;
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

        public StatementSyntax[] EmitLocalIfCheck(int index)
        {
            Debug.Assert(IsLocalReference, "Shouldn't have been called.");

            var localOperation = (ILocalReferenceOperation)ArgumentOperation.Value;

            var variableName = $"{ArgumentOperation.Parameter!.Name}_local";

            var statements = new StatementSyntax[3];
            statements[0] = EmitArgumentAccessor();

            // This is for calling the UnsafeAccessor method that doesn't seem to work for my needs
            //statements[1] = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
            //    .AddVariables(VariableDeclarator(variableName)
            //        .WithInitializer(EqualsValueClause(InvocationExpression(
            //            MemberAccessExpression(
            //                SyntaxKind.SimpleMemberAccessExpression,
            //                IdentifierName($"Setup{index}Accessor"),
            //                IdentifierName(((ILocalReferenceOperation)ArgumentOperation.Value).Local.Name)
            //                )
            //            )
            //        .AddArgumentListArguments(Argument(IdentifierName("target")))))));


            //statements[1] = LocalDeclarationStatement(VariableDeclaration(localOperation.Local.Type.AsUnknownTypeSyntax())
            //    .AddVariables(VariableDeclarator(variableName)
            //        .WithInitializer(EqualsValueClause(
            //            MemberAccessExpression(
            //                SyntaxKind.SimpleMemberAccessExpression,
            //                 ParenthesizedExpression(CastExpression(ParseTypeName("dynamic"), IdentifierName("target"))),
            //                 IdentifierName(localOperation.Local.Name))))
            //                )
            //           );

            // var arg_local = target.GetType().GetField("local").GetValue(target);

            // TODO: This really sucks, but neither other way works
            statements[1] = ExpressionStatement(
                ParseExpression($"var {variableName} = target.GetType().GetField(\"{localOperation.Local.Name}\")!.GetValue(target)")
            );

            statements[2] = EmitIfCheck(IdentifierName(variableName));

            return statements;
        }

        public IfStatementSyntax EmitIfCheck(ExpressionSyntax right)
        {
            var binaryExpression = BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                IdentifierName(ArgumentLocalName),
                right
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
