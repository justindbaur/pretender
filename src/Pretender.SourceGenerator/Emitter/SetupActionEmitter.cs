using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Parser;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal class SetupActionEmitter
    {
        private readonly ImmutableArray<SetupArgumentEmitter> _setupArgumentEmitters;
        private readonly KnownTypeSymbols _knownTypeSymbols;

        public SetupActionEmitter(INamedTypeSymbol pretendType, IMethodSymbol setupMethod, ImmutableArray<SetupArgumentEmitter> setupArgumentEmitters, KnownTypeSymbols knownTypeSymbols)
        {
            PretendType = pretendType;
            SetupMethod = setupMethod;
            _setupArgumentEmitters = setupArgumentEmitters;
            _knownTypeSymbols = knownTypeSymbols;
        }

        public INamedTypeSymbol PretendType { get; }
        public IMethodSymbol SetupMethod { get; }

        public void Emit(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.Write("pretend.GetOrCreateSetup");

            var returnType = SetupMethod.ReturnType.SpecialType != SpecialType.System_Void
                ? SetupMethod.ReturnType : null;

            if (returnType is not null)
            {
                writer.Write($"<{returnType.ToUnknownTypeString()}>");
            }

            writer.WriteLine("(0, static (pretend, expr) =>");
            writer.WriteLine("{");
            writer.IncreaseIndent();

            var anyEmitMatcherStatements = _setupArgumentEmitters.Any(e => e.EmitsMatcher);

            string matcherName;
            if (anyEmitMatcherStatements)
            {
                matcherName = "matchCall";
                writer.WriteLine("Matcher matchCall = (callInfo, target) =>");
                writer.WriteLine("{");
                writer.IncreaseIndent();

                foreach (var argumentEmitter in _setupArgumentEmitters)
                {
                    argumentEmitter.EmitArgumentMatcher(writer, cancellationToken);
                }

                writer.WriteLine("return true;");
                writer.DecreaseIndent();
                writer.WriteLine("};");
            }
            else
            {
                matcherName = "Cache.NoOpMatcher";
            }

            if (returnType is not null)
            {
                var methodStrategy = _knownTypeSymbols.GetSingleMethodStrategy(SetupMethod);

                // TODO: default value
                writer.WriteLine($"return new ReturningCompiledSetup<{PretendType.ToFullDisplayString()}, {returnType.ToUnknownTypeString()}>(pretend, {_knownTypeSymbols.GetPretendName(PretendType)}.{methodStrategy.UniqueName}_MethodInfo, {matcherName}, expr.Target, defaultValue: default);");
            }
            else
            {
                writer.WriteLine($"return new VoidCompiledSetup<{PretendType.ToFullDisplayString()}>();");
            }

            writer.DecreaseIndent();
            writer.WriteLine("}, setupExpression);");
        }
    }
}
