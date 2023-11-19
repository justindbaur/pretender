using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.SetupArguments
{
    internal abstract class SetupArgumentSpec
    {
        private readonly List<Diagnostic> _diagnostics = [];
        public SetupArgumentSpec(IArgumentOperation originalArgument, int argumentPlacement)
        {
            OriginalArgument = originalArgument;
            ArgumentPlacement = argumentPlacement;

            var tracker = new ArgumentTracker();
            Visit(originalArgument, tracker);

            NeedsCapturer = tracker.NeedsCapturer;
            NeededLocals = tracker.NeededLocals;
        }

        protected IArgumentOperation OriginalArgument { get; }
        protected IParameterSymbol Parameter => OriginalArgument.Parameter!;
        protected int ArgumentPlacement { get; }
        protected void AddDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
        public bool NeedsCapturer { get; }
        public ImmutableArray<ILocalReferenceOperation> NeededLocals { get; }
        public abstract int NeededMatcherStatements { get; }

        public abstract ImmutableArray<StatementSyntax> CreateMatcherStatements(CancellationToken cancellationToken);

        protected (SyntaxToken Identifier, LocalDeclarationStatementSyntax Accessor) CreateArgumentAccessor()
        {
            var argumentLocal = Identifier($"{Parameter.Name}_arg");

            // (string?)callInfo.Arguments[index];
            ExpressionSyntax argumentGetter = CastExpression(
                    Parameter.Type.AsUnknownTypeSyntax(),
                    ElementAccessExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("callInfo"), IdentifierName("Arguments")))
                        .AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(ArgumentPlacement))))
                    );

            var localAccessor = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                .AddVariables(VariableDeclarator(argumentLocal)
                    .WithInitializer(EqualsValueClause(argumentGetter))));

            return (argumentLocal, localAccessor);
        }

        protected IfStatementSyntax CreateIfCheck(ExpressionSyntax left, ExpressionSyntax right)
        {
            var binaryExpression = BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                left,
                right);

            return CreateIfCheck(binaryExpression);
        }

        protected IfStatementSyntax CreateIfCheck(ExpressionSyntax condition)
        {
            return IfStatement(condition, Block(
                ReturnStatement(LiteralExpression(SyntaxKind.FalseLiteralExpression))));
        }

        private static void Visit(IOperation? operation, ArgumentTracker tracker)
        {
            if (operation == null)
            {
                return;
            }

            // TODO: Handle most operations
            switch (operation.Kind)
            {
                case OperationKind.Block:
                    var block = (IBlockOperation)operation;
                    VisitMany(block.Operations, tracker);
                    break;
                case OperationKind.VariableDeclarationGroup:
                    var variableDeclarationGroup = (IVariableDeclarationGroupOperation)operation;
                    VisitMany(variableDeclarationGroup.Declarations, tracker);
                    break;
                case OperationKind.Return:
                    var returnOp = (IReturnOperation)operation;
                    Visit(returnOp.ReturnedValue, tracker);
                    break;
                case OperationKind.Literal:
                    // Literals are the best, they are easy and the end of the line
                    break;
                case OperationKind.Invocation:
                    var invocation = (IInvocationOperation)operation;
                    // The instance could be a local itself
                    Visit(invocation.Instance, tracker);
                    VisitMany(invocation.Arguments, tracker);
                    break;
                case OperationKind.LocalReference:
                    var local = (ILocalReferenceOperation)operation;
                    tracker.TryTrackLocal(local);
                    break;
                case OperationKind.ParameterReference:
                    break;
                case OperationKind.Binary:
                    var binary = (IBinaryOperation)operation;
                    Visit(binary.LeftOperand, tracker);
                    Visit(binary.RightOperand, tracker);
                    break;
                case OperationKind.AnonymousFunction:
                    // TODO: I'm not sure if this belongs in here or DelegateCreation but lets go with here for now
                    tracker.EnterScope();
                    var anonymousFunction = (IAnonymousFunctionOperation)operation;
                    Visit(anonymousFunction.Body, tracker);
                    tracker.ExitScope();
                    break;
                case OperationKind.DelegateCreation:
                    var delegateCreation = (IDelegateCreationOperation)operation;
                    // TODO: Now that we are in a delegate should we ignore their locals somehow?
                    Visit(delegateCreation.Target, tracker);
                    break;
                case OperationKind.VariableInitializer:
                    var variableInitializer = (IVariableInitializerOperation)operation;
                    tracker.LocalsDefined(variableInitializer.Locals);
                    // TODO: Not sure if this is right
                    Visit(variableInitializer.Value, tracker);
                    break;
                case OperationKind.VariableDeclaration:
                    var variableDeclaration = (IVariableDeclarationOperation)operation;
                    VisitMany(variableDeclaration.Declarators, tracker);
                    Visit(variableDeclaration.Initializer, tracker);
                    break;
                case OperationKind.VariableDeclarator:
                    var variableDeclarator = (IVariableDeclaratorOperation)operation;
                    tracker.LocalDefined(variableDeclarator.Symbol);
                    // TODO: IgnoredArguments property?
                    Visit(variableDeclarator.Initializer, tracker);
                    break;
                case OperationKind.Argument:
                    var argument = (IArgumentOperation)operation;
                    Visit(argument.Value, tracker);
                    break;
                default:
#if DEBUG
                    // TODO: Figure out what operation this is
                    Debugger.Launch();
                    // TODO: Report diagnostic?
                    // TODO: Do fallback support? by looping over ChildOperations?
#endif
                    return;
            }
        }

        private static void VisitMany(IEnumerable<IOperation> operations, ArgumentTracker tracker)
        {
            foreach (var operation in operations)
            {
                Visit(operation, tracker);
            }
        }

        // Factory method for creating an ArgumentSpec based on the argument operation
        public static SetupArgumentSpec Create(IArgumentOperation argumentOperation, int argumentPlacement)
        {
            var argumentOperationValue = argumentOperation.Value;
            switch (argumentOperationValue.Kind)
            {
                case OperationKind.Literal:
                    return new LiteralArgumentSpec(
                        (ILiteralOperation)argumentOperationValue,
                        argumentOperation,
                        argumentPlacement);
                case OperationKind.Invocation:
                    return new InvocationArgumentSpec(
                        (IInvocationOperation)argumentOperationValue,
                        argumentOperation,
                        argumentPlacement);
                case OperationKind.LocalReference:
                    return new LocalReferenceArgumentSpec(
                        (ILocalReferenceOperation)argumentOperationValue,
                        argumentOperation,
                        argumentPlacement);
                default:
                    throw new NotImplementedException();
            }
        }

        private class ArgumentTracker
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
    }
}
