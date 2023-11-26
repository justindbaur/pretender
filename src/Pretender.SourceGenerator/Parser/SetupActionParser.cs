using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.SetupArguments;

namespace Pretender.SourceGenerator.Parser
{
    internal class SetupActionParser
    {
        private readonly IOperation _setupActionArgument;
        private readonly ITypeSymbol _pretendType;
        private readonly bool _forcePropertySetter;

        // TODO: Should I have a higher IOperation kind here? Like InvocationOperation?
        public SetupActionParser(IOperation setupActionArgument, ITypeSymbol pretendType, bool forcePropertySetter)
        {
            _setupActionArgument = setupActionArgument;
            _pretendType = pretendType;
            _forcePropertySetter = forcePropertySetter;
        }

        public (SetupActionEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics) Parse(CancellationToken cancellationToken)
        {
            var candidates = GetInvocationCandidates(cancellationToken);

            if (candidates.Length == 0)
            {
                // TODO: Create error diagnostic
                return (null, null);
            }
            else if (candidates.Length != 1)
            {
                // TODO: Create error diagnostic
                return (null, null);
            }

            var candidate = candidates[0];

            var arguments = candidate.Arguments;

            var builder = ImmutableArray.CreateBuilder<SetupArgumentSpec>(arguments.Length);
            for (var i = 0; i < arguments.Length; i++)
            {
                builder.Add(SetupArgumentSpec.Create(arguments[i], i));
            }

            return (new SetupActionEmitter(_pretendType, candidate.Method, builder.MoveToImmutable()), null);
        }

        private ImmutableArray<InvocationCandidate> GetInvocationCandidates(CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<InvocationCandidate>();
            TraverseOperation(_setupActionArgument, builder, cancellationToken);
            return builder.ToImmutable();
        }

        private void TraverseOperation(IOperation operation, ImmutableArray<InvocationCandidate>.Builder invocationCandidates, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (operation.Kind)
            {
                case OperationKind.Block:
                    var blockOperation = (IBlockOperation)operation;
                    TraverseOperationList(blockOperation.Operations, invocationCandidates, cancellationToken);
                    break;
                case OperationKind.Return:
                    var returnOperation = (IReturnOperation)operation;
                    if (returnOperation.ReturnedValue != null)
                    {
                        TraverseOperation(returnOperation.ReturnedValue, invocationCandidates, cancellationToken);
                    }
                    break;
                case OperationKind.ExpressionStatement:
                    var expressionStatement = (IExpressionStatementOperation)operation;
                    TraverseOperation(expressionStatement.Operation, invocationCandidates, cancellationToken);
                    break;
                case OperationKind.Conversion:
                    var conversionOperation = (IConversionOperation)operation;
                    TraverseOperation(conversionOperation.Operand, invocationCandidates, cancellationToken);
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
                    TraverseOperation(anonymousFunctionOperation.Body, invocationCandidates, cancellationToken);
                    break;
                case OperationKind.DelegateCreation:
                    var delegateCreationOperation = (IDelegateCreationOperation)operation;
                    TraverseOperation(delegateCreationOperation.Target, invocationCandidates, cancellationToken);
                    break;
                default:
#if DEBUG
                    // TODO: Figure out what operation caused this, it's not ideal to "randomly" support operations
                    Debugger.Launch();
#endif
                    // Absolute fallback, most of our operations can be supported this way but it's nicer to be explicit
                    TraverseOperationList(operation.ChildOperations, invocationCandidates, cancellationToken);
                    break;
            }
        }

        private void TraverseOperationList(IEnumerable<IOperation> operations, ImmutableArray<InvocationCandidate>.Builder invocationCandidates, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var operation in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TraverseOperation(operation, invocationCandidates, cancellationToken);
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

            var method = _forcePropertySetter
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
            if (_forcePropertySetter)
            {
                return;
            }

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
