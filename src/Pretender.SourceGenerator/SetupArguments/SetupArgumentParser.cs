using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Parser;

namespace Pretender.SourceGenerator.SetupArguments
{
    internal class SetupArgumentSpec
    {
        public SetupArgumentSpec(IArgumentOperation argument, KnownTypeSymbols knownTypeSymbols)
        {
            Argument = argument;
            KnownTypeSymbols = knownTypeSymbols;
        }

        public IArgumentOperation Argument { get; }

        // I don't think a SetupArgument will ever be __argList so I'm not worried about this null assurance
        public IParameterSymbol Parameter => Argument.Parameter!;

        public KnownTypeSymbols KnownTypeSymbols { get; }
    }

    internal class SetupArgumentParser
    {
        private readonly SetupArgumentSpec _setupArgumentSpec;

        public SetupArgumentParser(SetupArgumentSpec setupArgumentSpec)
        {
            _setupArgumentSpec = setupArgumentSpec;
        }


        public (SetupArgumentEmitter? SetupArgumentEmitter, ImmutableArray<Diagnostic>? Diagnostics) Parse(CancellationToken cancellationToken)
        {
            var argumentValue = _setupArgumentSpec.Argument.Value;

            return argumentValue.Kind switch
            {
                OperationKind.Literal => (new LiteralArgumentEmitter((ILiteralOperation)argumentValue, _setupArgumentSpec), null),
                OperationKind.Invocation => ParseInvocation((IInvocationOperation)argumentValue, cancellationToken),
                OperationKind.LocalReference => (new LocalReferenceArgumentEmitter((ILocalReferenceOperation)argumentValue, _setupArgumentSpec), null),
                OperationKind.FieldReference => ParseFieldReference((IFieldReferenceOperation)argumentValue, cancellationToken),
                _ => throw new NotImplementedException($"{argumentValue.Kind} is not a supported operation in setup arguments."),
            };
        }

        private (SetupArgumentEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics) ParseFieldReference(IFieldReferenceOperation fieldReference, CancellationToken cancellationToken)
        {
            // For now fields references are just captured, we can change this to aggressively rewrite the callsite if there is desire
            return (new CapturedArgumentEmitter(_setupArgumentSpec), null);
        }

        private (SetupArgumentEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics) ParseInvocation(IInvocationOperation invocation, CancellationToken cancellationToken)
        {
            if (TryGetMatcherAttributeType(invocation, out var matcherType, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Special case AnyMatcher
                if (SymbolEqualityComparer.Default.Equals(matcherType, _setupArgumentSpec.KnownTypeSymbols.AnyMatcher))
                {
                    return (new NoopArgumentEmitter(_setupArgumentSpec), null);
                }

                // TODO: Parse args passed into the invocation
                if (invocation.Arguments.Length > 0)
                {
                    // TODO: Some of these might be safe to rewrite
                    return (new CapturedMatcherInvocationEmitter(_setupArgumentSpec), null);
                }

                return (new MatcherArgumentEmitter(matcherType, _setupArgumentSpec), null);
            }
            else
            {
                // They likely invoked their own method, we will need to run and capture output for value/matcher
                throw new NotImplementedException("We don't support user scoped invocations quite yet.");
            }
        }

        private bool TryGetMatcherAttributeType(IInvocationOperation invocation, out INamedTypeSymbol matcherType, CancellationToken cancellationToken)
        {
            var allAttributes = invocation.TargetMethod.GetAttributes();

            // TODO: Use KnownTypeSymbols
            var matcherAttribute = allAttributes.Single(ad => ad.AttributeClass!.EqualsByName(["Pretender", "Matchers", "MatcherAttribute"]));

            matcherType = null!;

            cancellationToken.ThrowIfCancellationRequested();

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
                // TODO: Use KnownTypeSymbols
                if (!attributeType.Type!.EqualsByName(["System", "Type"]))
                {
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (attributeType.Value is null)
                {
                    return false;
                }

                // Always an okay cast?
                matcherType = (INamedTypeSymbol)attributeType.Value!;
            }

            cancellationToken.ThrowIfCancellationRequested();
            // TODO: Write a lot more tests for this
            if (matcherType.IsUnboundGenericType)
            {
                if (invocation.TargetMethod.TypeArguments.Length != matcherType.TypeArguments.Length)
                {
                    return false;
                }

                matcherType = matcherType.ConstructedFrom.Construct([.. invocation.TargetMethod.TypeArguments]);
            }

            return true;
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
                    // Debugger.Launch();
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