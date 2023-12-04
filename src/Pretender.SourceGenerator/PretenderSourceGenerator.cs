using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            // TODO: Refactor our region use
            IncrementalValueProvider<KnownTypeSymbols> knownTypeSymbols =
                context.CompilationProvider
                    .Select((compilation, _) => new KnownTypeSymbols(compilation));

            // TODO: Read settings off of 
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

            #region Pretend
            IncrementalValuesProvider<(PretendEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics)> pretendsWithDiagnostics =
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

            var pretends = ReportDiagnostics(context, pretendsWithDiagnostics);

            context.RegisterSourceOutput(pretends, static (context, emitter) =>
            {
                var compilationUnit = emitter.Emit(context.CancellationToken);
                context.AddSource($"Pretender.Type.{emitter.PretendType.ToPretendName()}.g.cs", compilationUnit.GetText(Encoding.UTF8));
            });
            #endregion

            #region Setup
            IncrementalValuesProvider<(SetupEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics)> setups =
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

            context.RegisterSourceOutput(setups.Collect(), static (context, setups) =>
            {
                var members = new List<MemberDeclarationSyntax>();
                for (var i = 0; i < setups.Length; i++)
                {
                    var setup = setups[i];

                    if (setup.Diagnostics is ImmutableArray<Diagnostic> diagnostics)
                    {
                        foreach (var diagnostic in diagnostics)
                        {
                            context.ReportDiagnostic(diagnostic);
                        }
                    }

                    if (setup.Emitter is SetupEmitter emitter)
                    {
                        members.AddRange(emitter.Emit(i, context.CancellationToken));
                    }
                }

                var classDeclaration = SyntaxFactory.ClassDeclaration("SetupInterceptors")
                    .WithModifiers(SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.FileKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                    .AddMembers([.. members]);

                var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("Pretender.SourceGeneration"))
                    .AddMembers(classDeclaration)
                    .AddUsings(
                        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                        KnownBlocks.CompilerServicesUsing,
                        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq.Expressions")),
                        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")),
                        KnownBlocks.PretenderUsing,
                        KnownBlocks.PretenderInternalsUsing
                    );

                var il = KnownBlocks.InterceptsLocationAttribute;

                var compilationUnit = SyntaxFactory.CompilationUnit()
                    .AddMembers(il, namespaceDeclaration)
                    .NormalizeWhitespace();

                context.AddSource("Pretender.Setups.g.cs", compilationUnit.GetText(Encoding.UTF8));
            });
            #endregion

            #region Verify
            IncrementalValuesProvider<(VerifyEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics)> verifyCallsWithDiagnostics =
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

            var verifyEmitters = ReportDiagnostics(context, verifyCallsWithDiagnostics);

            context.RegisterSourceOutput(verifyEmitters.Collect(), (context, inputs) =>
            {
                var methods = new List<MethodDeclarationSyntax>();
                for (var i = 0; i < inputs.Length; i++)
                {
                    var input = inputs[i];

                    var method = input.Emit(0, context.CancellationToken);
                    methods.Add(method);
                }

                if (methods.Count > 0)
                {
                    // Emit all methods
                    var compilationUnit = CommonSyntax.CreateVerifyCompilationUnit([.. methods]);
                    context.AddSource("Pretender.Verifies.g.cs", compilationUnit.GetText(Encoding.UTF8));
                }
            });
            #endregion

            #region Create
            var createCalls = context.SyntaxProvider.CreateSyntaxProvider(
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

            var createEmitters = ReportDiagnostics(context, createCalls);

            context.RegisterSourceOutput(createEmitters, static (context, emitter) =>
            {
                // TODO: Don't actually need a list here
                var members = new List<MemberDeclarationSyntax>();

                string? pretendName = null;

                pretendName ??= emitter.Operation.TargetMethod.ReturnType.ToPretendName();
                members.Add(emitter.Emit(context.CancellationToken));

                if (members.Any())
                {
                    var createClass = SyntaxFactory.ClassDeclaration("CreateInterceptors")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.FileKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                        .WithMembers(SyntaxFactory.List(members));

                    var createNamespace = KnownBlocks.OurNamespace
                        .AddMembers(createClass)
                        .AddUsings(KnownBlocks.CompilerServicesUsing, KnownBlocks.PretenderUsing);

                    var cu = SyntaxFactory.CompilationUnit()
                        .AddMembers(KnownBlocks.InterceptsLocationAttribute, createNamespace)
                        .NormalizeWhitespace();

                    context.AddSource($"Pretender.Creates.{pretendName}.g.cs", cu.GetText(Encoding.UTF8));
                }
            });
            #endregion
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
