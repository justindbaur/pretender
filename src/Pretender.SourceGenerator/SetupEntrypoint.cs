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
            // Is Void returning?
            var setupExpressionArg = invocationOperation.Arguments[0];
            var expressionType = setupExpressionArg.Parameter!.Type;
            ExpressionType = expressionType;

            Debug.Assert(invocationOperation.Type is INamedTypeSymbol, "This should have been asserted via making sure it's the right invocation.");

            var pretendType = ((INamedTypeSymbol)invocationOperation.Type!).TypeArguments[0];

            var setupInvocation = SimplifyOperation(setupExpressionArg.Value, pretendType);

            PretendType = pretendType;

            if (setupInvocation is null)
            {
                // TODO: Add Error diagnostic
                return;
            }

            var setupMethod = setupInvocation.TargetMethod;

            if (setupMethod.ReturnsVoid)
            {
                // The setup method returns void
            }
            else
            {
                // There is a return type
            }

            SetupMethod = setupMethod;

            foreach (var argument in setupInvocation.Arguments)
            {
                ValidateArgument(argument);
            }

            Arguments = setupInvocation.Arguments;
        }

        public ITypeSymbol ExpressionType { get; }
        public ITypeSymbol PretendType { get; }
        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();
        public IMethodSymbol SetupMethod { get; } = null!;
        public ImmutableArray<IArgumentOperation> Arguments { get; }

        public MethodDeclarationSyntax GetMatcherDeclaration(int index)
        {
            var statements = new List<StatementSyntax>();

            TypeSyntax returnType = SetupMethod.ReturnsVoid
                ? ParseTypeName($"global::Pretender.IPretendSetup<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {SetupMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>")
                : ParseTypeName($"global::Pretender.IPretendSetup<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>");

            var matchesCall = LocalDeclarationStatement(VariableDeclaration(
                ParseTypeName("global::System.Func<global::Pretender.CallInfo, bool>"))
                .WithVariables(SingletonSeparatedList(VariableDeclarator("matchCall"))));

            statements.Add(matchesCall);

            return MethodDeclaration(returnType, $"Setup{index}")
                .WithBody(Block(statements.ToArray()))
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("pretend"))
                        .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                        .WithType(ParseTypeName($"global::Pretender.Pretend<{PretendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>")),

                    Parameter(Identifier("setupExpression"))
                        .WithType(ParseTypeName(ExpressionType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))),
                })))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)));
        }

        private void ValidateArgument(IArgumentOperation operation)
        {
            var value = operation.Value;
            var hasSupport = value switch
            {
                ILiteralOperation => true,
                // TODO: It matchers
                _ => false,
            };

            if (!hasSupport)
            {
                // TODO: Add diagnostic
                Diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidSetupArgument,
                    operation.Syntax.GetLocation(),
                    value.Kind));
            }
        }

        private static IInvocationOperation? SimplifyBlockOperation(IBlockOperation operation, ITypeSymbol pretendType)
        {
            foreach (var childOperation in operation.Operations)
            {
                var method = SimplifyOperation(childOperation, pretendType);
                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }

        private static IInvocationOperation? SimplifyReturnOperation(IReturnOperation operation, ITypeSymbol pretendType)
        {
            return operation.ReturnedValue switch
            {
                not null => SimplifyOperation(operation.ReturnedValue, pretendType),
                // If there is not returned value, this is a dead end.
                _ => null,
            };
        }

        private static IInvocationOperation? SimplifyOperation(IOperation operation, ITypeSymbol pretendType)
        {
            // TODO: Support more operations
            return operation.Kind switch
            {
                OperationKind.Return => SimplifyReturnOperation((IReturnOperation)operation, pretendType),
                OperationKind.Conversion => SimplifyOperation(((IConversionOperation)operation).Operand, pretendType),
                OperationKind.Block => SimplifyBlockOperation((IBlockOperation)operation, pretendType),
                OperationKind.AnonymousFunction => SimplifyOperation(((IAnonymousFunctionOperation)operation).Body, pretendType),
                OperationKind.Invocation => TryMethod((IInvocationOperation)operation, pretendType),
                _ => null,
            };
        }

        private static IInvocationOperation? TryMethod(IInvocationOperation operation, ITypeSymbol pretendType)
        {
            var instance = operation.Instance;
            var method = operation.TargetMethod;

            // It should have an instance because it should be called from the pretend from the Func<,>/Action<>
            if (instance == null)
            {
                return null;
            }

            if (instance is IParameterReferenceOperation parameter)
            {
                if (!SymbolEqualityComparer.Default.Equals(parameter.Type, pretendType))
                {
                    return null;
                }
            }
            // TODO: Should we allow any other instance operation types?

            var pretendMethods = pretendType.GetMembers()
                .OfType<IMethodSymbol>();

            if (!pretendMethods.Contains(method, SymbolEqualityComparer.Default))
            {
                // This is not a method that exists on the pretend type
                // TODO: We could inspect the method body further
                return null;
            }

            return operation;
        }
    }
}
