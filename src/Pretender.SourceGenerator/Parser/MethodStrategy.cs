using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Pretender.SourceGenerator.Writing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.Parser
{
    internal abstract class MethodStrategy
    {
        protected MethodStrategy(IMethodSymbol method)
        {
            Method = method;
        }

        public IMethodSymbol Method { get; }
        public abstract string UniqueName { get; }

        public abstract void EmitMethodGetter(IndentedTextWriter writer, CancellationToken cancellationToken);

        protected static InvocationExpressionSyntax NameOfExpression(string identifier)
        {
            var text = SyntaxFacts.GetText(SyntaxKind.NameOfKeyword);

            var identifierSyntax = Identifier(default,
                SyntaxKind.NameOfKeyword,
                text,
                text,
                default);

            return InvocationExpression(
                IdentifierName(identifierSyntax),
                ArgumentList(SingletonSeparatedList(Argument(IdentifierName(identifier)))));
        }
    }

    internal class ByNameMethodStrategy : MethodStrategy
    {
        public ByNameMethodStrategy(IMethodSymbol method)
            : base(method)
        {

        }

        public override string UniqueName => Method.Name;

        public override void EmitMethodGetter(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            if (Method.MethodKind == MethodKind.Ordinary)
            {
                writer.Write($".GetMethod(nameof({Method.Name}))!;");
            }
            else if (Method.MethodKind == MethodKind.PropertyGet)
            {
                writer.Write($".GetProperty(nameof({Method.AssociatedSymbol!.Name}))!.GetMethod;");
            }
            else if (Method.MethodKind == MethodKind.PropertySet)
            {
                writer.Write($".GetProperty(nameof({Method.AssociatedSymbol!.Name}))!.SetMethod;");
            }
            else
            {
                throw new InvalidOperationException($"Did not expect {Method.MethodKind}");
            }
        }
    }

    internal class ByParameterCountMethodStrategy : MethodStrategy
    {
        private readonly int _parameterCount;

        public ByParameterCountMethodStrategy(IMethodSymbol method, int parameterCount)
            : base(method)
        {
            _parameterCount = parameterCount;
        }

        // TODO: Is this the naming we want? I think I just want an incrementing index
        public override string UniqueName => $"{Method.Name}_{_parameterCount}";

        public override void EmitMethodGetter(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}