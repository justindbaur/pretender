using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Invocation;
using Pretender.SourceGenerator.Parser;

namespace Pretender.SourceGenerator
{
    [Generator]
    public class PretenderSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<KnownTypeSymbols> knownTypeSymbols =
                context.CompilationProvider
                    .Select((compilation, _) => new KnownTypeSymbols(compilation));

            IncrementalValueProvider<PretenderSettings> settings = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Pretender.PretenderSettingsAttribute",
                predicate: static (node, _) => true,
                transform: static (context, _) => context.Attributes[0])
                .Collect()
                .Select(static (settings, _) =>
                {
                    if (settings.IsEmpty)
                    {
                        return PretenderSettings.Default;
                    }

                    if (settings.Length > 1)
                    {
                        throw new InvalidOperationException("Only one instance of PretenderSettingsAttribute is expected on the assembly.");
                    }

                    return PretenderSettings.FromAttribute(settings[0]);
                });

            IncrementalValuesProvider<(PretendEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics)> pretendEmittersWithDiagnostics =
                 context.SyntaxProvider.CreateSyntaxProvider(
                     predicate: (node, _) => PretendInvocation.IsCandidateSyntaxNode(node),
                     transform: PretendInvocation.Create)
                     .Where(static p => p != null)
                     .Combine(knownTypeSymbols)
                     .Combine(settings)
                     .Select(static (tuple, cancellationToken) =>
                     {
                         var ((invocation, knownTypeSymbols), settings) = tuple;
                         var parser = new PretendParser(invocation!, knownTypeSymbols, settings);
                         return parser.Parse(cancellationToken);
                     })
                     .WithTrackingName("Pretend");

            var pretendEmitters = ReportDiagnostics(context, pretendEmittersWithDiagnostics);

            IncrementalValuesProvider<(SetupEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics)> setupEmittersWithDiagnostics =
                context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (node, _) => SetupInvocation.IsCandidateSyntaxNode(node),
                    transform: SetupInvocation.Create)
                .Where(i => i is not null)
                .Combine(knownTypeSymbols)
                .Select(static (tuple, token) =>
                {
                    var parser = new SetupParser(tuple.Left!, tuple.Right);

                    return parser.Parse(token);
                })
                .WithTrackingName("Setup");

            var setups = ReportDiagnostics(context, setupEmittersWithDiagnostics);

            IncrementalValuesProvider<(VerifyEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics)> verifyEmittersWithDiagnostics =
                context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: (node, _) => VerifyInvocation.IsCandidateSyntaxNode(node),
                    transform: VerifyInvocation.Create)
                .Where(vi => vi is not null)
                .Combine(knownTypeSymbols)
                .Select((tuple, cancellationToken) =>
                {
                    // Create new VerifySpec
                    var parser = new VerifyParser(tuple.Left!, tuple.Right);

                    return parser.Parse(cancellationToken);
                })
                .WithTrackingName("Verify");

            var verifyEmitters = ReportDiagnostics(context, verifyEmittersWithDiagnostics);

            var createEmittersWithDiagnostics = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => CreateInvocation.IsCandidateSyntaxNode(node),
                transform: CreateInvocation.Create)
                .Where(i => i is not null)!
                .GroupWith(c => c.Location, CreateInvocationComparer.Instance)
                .Combine(knownTypeSymbols)
                .Select((tuple, token) =>
                {
                    var parser = new CreateParser(tuple.Left.Source, tuple.Left.Elements, tuple.Left.Index, tuple.Right);
                    return parser.Parse(token);
                })
                .WithTrackingName("Create");

            var createEmitters = ReportDiagnostics(context, createEmittersWithDiagnostics);

            context.RegisterSourceOutput(
                pretendEmitters.GroupWith(e => e, PretendEmitter.Comparer.Default).Select((t, _) => t.Source).Collect()
                .Combine(setups.Collect())
                .Combine(verifyEmitters.Collect())
                .Combine(createEmitters.Collect()), (context, emitters) =>
            {
                var (((pretends, setups), verifies), creates) = emitters;

                context.CancellationToken.ThrowIfCancellationRequested();

                var grandEmitter = new GrandEmitter(pretends, setups, verifies, creates);

                var sourceText = grandEmitter.Emit(context.CancellationToken);

                context.CancellationToken.ThrowIfCancellationRequested();

                context.AddSource("Pretender.g.cs", sourceText);
            });
        }

        private static IncrementalValuesProvider<T> ReportDiagnostics<T>(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<(T? Emitter, ImmutableArray<Diagnostic>? Diagnostics)> source)
        {
            var diagnostics = source
                .Select((v, _) => v.Diagnostics)
                .Where(d => d.HasValue && d.Value.Length > 0);

            context.RegisterSourceOutput(diagnostics, (context, diagnostics) =>
            {
                foreach (var diagnostic in diagnostics!.Value)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            });

            return source
                .Select((v, _) => v.Emitter)
                .Where(e => e != null)!;
        }
    }
}
