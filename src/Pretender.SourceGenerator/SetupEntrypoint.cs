using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Parser;
using Pretender.SourceGenerator.SetupArguments;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator
{
    internal class SetupEntrypoint
    {
        public SetupEntrypoint(IInvocationOperation invocationOperation)
        {
            OriginalInvocation = invocationOperation;
            var setupExpressionArg = invocationOperation.Arguments[0];
            SetupExpression = setupExpressionArg;

            Debug.Assert(invocationOperation.Type is INamedTypeSymbol, "This should have been asserted via making sure it's the right invocation.");

            var pretendType = ((INamedTypeSymbol)invocationOperation.Type!).TypeArguments[0];

            PretendType = pretendType;

            // TODO: Use correct useSetup value
            var parser = new SetupActionParser(setupExpressionArg.Value, pretendType, false);

            // TODO: Use the parser properly
            var (emitter, diagnostics) = parser.Parse(default);

            // TODO: Don't override null
            SetupCreation = emitter!;

            // TODO: Consume diagnostics
            var setupMethod = SimplifyOperation(setupExpressionArg.Value);

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

            var setupArguments = new SetupArgumentSpec[setupMethod.Arguments.Length];
            for (int i = 0; i < setupArguments.Length; i++)
            {
                setupArguments[i] = SetupArgumentSpec.Create(setupMethod.Arguments[i], i);
            }
            Arguments = setupArguments.ToImmutableArray();

            // TODO: Don't do this
            Diagnostics.AddRange(Arguments.SelectMany(s => s.Diagnostics));
        }

        public IArgumentOperation SetupExpression { get; }
        public IInvocationOperation OriginalInvocation { get; }
        public ITypeSymbol PretendType { get; }
        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();
        public IMethodSymbol SetupMethod { get; } = null!;
        public SetupActionEmitter SetupCreation { get; }
        public ImmutableArray<SetupArgumentSpec> Arguments { get; }

        public MemberDeclarationSyntax[] GetMembers(int index)
        {
            var allMembers = new List<MemberDeclarationSyntax>();

            var interceptsLocation = new InterceptsLocationInfo(OriginalInvocation);

            // TODO: This is wrong
            var typeArguments = SetupMethod.ReturnsVoid
                ? TypeArgumentList(SingletonSeparatedList(ParseTypeName(PretendType.ToFullDisplayString())))
                : TypeArgumentList(SeparatedList([ParseTypeName(PretendType.ToFullDisplayString()), SetupMethod.ReturnType.AsUnknownTypeSyntax()]));

            var returnType = GenericName("IPretendSetup")
                .WithTypeArgumentList(typeArguments);

            var setupInvocation = SetupCreation.CreateSetupGetter(default);

            var setupMethod = MethodDeclaration(returnType, $"Setup{index}")
                .WithBody(Block(ReturnStatement(setupInvocation)))
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("pretend"))
                        .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                        .WithType(ParseTypeName($"Pretend<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>")),

                    Parameter(Identifier("setupExpression"))
                        .WithType(GenericName(SetupMethod.ReturnsVoid ? "Action" : "Func").WithTypeArgumentList(typeArguments)),
                })))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithAttributeLists(SingletonList(AttributeList(
                    SingletonSeparatedList(interceptsLocation.ToAttributeSyntax()))));

            allMembers.Add(setupMethod);
            return [.. allMembers];
        }

        private (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) SimplifyBlockOperation(IBlockOperation operation)
        {
            foreach (var childOperation in operation.Operations)
            {
                var method = SimplifyOperation(childOperation);
                if (method != default)
                {
                    return method;
                }
            }

            return default;
        }

        private (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) SimplifyReturnOperation(IReturnOperation operation)
        {
            return operation.ReturnedValue switch
            {
                not null => SimplifyOperation(operation.ReturnedValue),
                // If there is not returned value, this is a dead end.
                _ => default,
            };
        }

        private (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) SimplifyOperation(IOperation operation)
        {
            // TODO: Support more operations
            return operation.Kind switch
            {
                OperationKind.Block => SimplifyBlockOperation((IBlockOperation)operation),
                OperationKind.Return => SimplifyReturnOperation((IReturnOperation)operation),
                // ExpressionStatement is probably a dead path now but who cares
                OperationKind.ExpressionStatement => SimplifyOperation(((IExpressionStatementOperation)operation).Operation),
                OperationKind.Conversion => SimplifyOperation(((IConversionOperation)operation).Operand),
                OperationKind.Invocation => TryMethod((IInvocationOperation)operation),
                OperationKind.PropertyReference => TryProperty((IPropertyReferenceOperation)operation),
                OperationKind.AnonymousFunction => SimplifyOperation(((IAnonymousFunctionOperation)operation).Body),
                OperationKind.DelegateCreation => SimplifyOperation(((IDelegateCreationOperation)operation).Target),
                _ => default,
            };
        }

        private (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) TryProperty(IPropertyReferenceOperation propertyReference)
        {
            if (propertyReference.Instance == null)
            {
                return default;
            }

            if (!SymbolEqualityComparer.Default.Equals(propertyReference.Instance.Type, PretendType))
            {
                return default;
            }

            var method = OriginalInvocation.TargetMethod.Name == "SetupSet"
                ? propertyReference.Property.SetMethod
                : propertyReference.Property.GetMethod;

            // TODO: Validate the get method exists on the type we are pretending
            // and that the return type it's returning is the expected one
            if (method == null)
            {
                return default;
            }

            // I still don't return arguments for a property setter right?
            return (method, ImmutableArray<IArgumentOperation>.Empty);
        }

        private (IMethodSymbol Method, ImmutableArray<IArgumentOperation> Arguments) TryMethod(IInvocationOperation operation)
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
                if (!SymbolEqualityComparer.Default.Equals(parameter.Type, PretendType))
                {
                    return default;
                }
            }
            // TODO: Should we allow any other instance operation types?

            var pretendMethods = PretendType.GetMembers()
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
