using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Parser;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal class CreateEmitter
    {
        private readonly IInvocationOperation _originalOperation;
        private readonly KnownTypeSymbols _knownTypeSymbols;
        private readonly ImmutableArray<ITypeSymbol>? _typeArguments;
#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly ImmutableArray<InterceptableLocation> _locations;
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly int _index;

#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public CreateEmitter(IInvocationOperation originalOperation, KnownTypeSymbols knownTypeSymbols, ImmutableArray<ITypeSymbol>? typeArguments, ImmutableArray<InterceptableLocation> locations, int index)
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        {
            _originalOperation = originalOperation;
            _knownTypeSymbols = knownTypeSymbols;
            _typeArguments = typeArguments;
            _locations = locations;
            _index = index;
        }

        public INamedTypeSymbol PretendType => (INamedTypeSymbol)_originalOperation.TargetMethod.ReturnType;

        public IInvocationOperation Operation => _originalOperation;

        public void Emit(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            var returnType = _originalOperation.TargetMethod.ReturnType;

            var returnTypeSyntax = returnType.ToUnknownTypeString();

            foreach (var location in _locations)
            {
#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                writer.WriteLine(location.GetInterceptsLocationAttributeSyntax());
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            }
            writer.Write($"internal static {returnType.ToUnknownTypeString()} Create{_index}");

            if (_typeArguments is ImmutableArray<ITypeSymbol> typeArguments && typeArguments.Length > 0)
            {
                // <T0, T1>(this Pretend<IInterface> pretend, T0 arg0, T1 arg1)
                writer.Write("<");

                for (var i = 0; i < _typeArguments.Value.Length; i++)
                {
                    writer.Write($"T{i}");
                }

                writer.Write($">(this Pretend<{returnTypeSyntax}> pretend");

                for (var i = 0; i < _typeArguments.Value.Length; i++)
                {
                    writer.Write($", T{i} arg{i}");
                }

                writer.WriteLine(")");
            }
            else
            {
                // TODO: Handle the params overload
                writer.WriteLine($"(this Pretend<{returnTypeSyntax}> pretend)");
            }

            using (writer.WriteBlock())
            {
                writer.Write($"return new {_knownTypeSymbols.GetPretendName(PretendType)}(pretend");

                if (_typeArguments.HasValue)
                {
                    for (int i = 0; i < _typeArguments.Value.Length; i++)
                    {
                        writer.Write($", arg{i}");
                    }

                    writer.WriteLine(");");
                }
                else
                {
                    // TODO: Handle params overload
                    writer.WriteLine(");");
                }
            }
        }
    }
}