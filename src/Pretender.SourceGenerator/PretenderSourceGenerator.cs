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
            // TODO: Create compilation data
            IncrementalValueProvider<CompilationData?> compilationData =
                context.CompilationProvider
                    .Select((compilation, _) => compilation.Options is CSharpCompilationOptions
                        ? new CompilationData((CSharpCompilation)compilation)
                        : null);

            #region Pretend
            IncrementalValuesProvider<(PretendEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics)> pretends =
                 context.SyntaxProvider.CreateSyntaxProvider(
                     predicate: (node, _) => PretendInvocation.IsCandidateSyntaxNode(node),
                     transform: PretendInvocation.Create)
                     .Where(static p => p != null)
                     .Combine(compilationData)
                     .Select(static (tuple, cancellationToken) =>
                     {
                         if (tuple.Right is not CompilationData compilationData)
                         {
                             return (null, null);
                         }

                         // TODO: Create Parser
                         var parser = new PretendParser(tuple.Left!, compilationData);
                         return parser.Parse(cancellationToken);
                     })
                     .WithTrackingName("Pretend");

            context.RegisterSourceOutput(pretends, static (context, pretend) =>
            {
                if (pretend.Diagnostics is ImmutableArray<Diagnostic> diagnostics)
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                }

                if (pretend.Emitter is PretendEmitter emitter)
                {
                    var compilationUnit = emitter.Emit(context.CancellationToken);
                    context.AddSource($"Pretender.Type.{emitter.PretendType.ToPretendName()}.g.cs", compilationUnit.GetText(Encoding.UTF8));
                }
            });
            #endregion

            #region Setup
            IncrementalValuesProvider<(SetupEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics)> setups =
                context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (node, _) => SetupInvocation.IsCandidateSyntaxNode(node),
                    transform: SetupInvocation.Create)
                .Where(i => i is not null)
                .Combine(compilationData)
                .Select(static (tuple, token) =>
                {
                    if (tuple.Right is not CompilationData compilationData)
                    {
                        return (null, null);
                    }

                    var parser = new SetupParser(tuple.Left!, compilationData);

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
                .Combine(compilationData)
                .Select((tuple, cancellationToken) =>
                {
                    if (tuple.Right is not CompilationData compilationData)
                    {
                        return (null, null);
                    }

                    // Create new VerifySpec
                    var parser = new VerifyParser(tuple.Left!, compilationData);

                    return parser.Parse(cancellationToken);
                })
                .WithTrackingName("Verify");

            context.RegisterSourceOutput(verifyCallsWithDiagnostics.Collect(), (context, inputs) =>
            {
                var methods = new List<MethodDeclarationSyntax>();
                for (var i = 0; i < inputs.Length; i++)
                {
                    var input = inputs[i];
                    if (input.Diagnostics is ImmutableArray<Diagnostic> diagnostics)
                    {
                        foreach (var diagnostic in diagnostics)
                        {
                            context.ReportDiagnostic(diagnostic);
                        }
                    }

                    if (input.Emitter is VerifyEmitter emitter)
                    {
                        // TODO: Emit VerifyMethod
                        var method = emitter.Emit(0, context.CancellationToken);
                        methods.Add(method);
                    }
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
                .Combine(compilationData)
                .Select((tuple, token) =>
                {
                    if (tuple.Right is not CompilationData compilationData)
                    {
                        return (null, null);
                    }

                    var parser = new CreateParser(tuple.Left.Source, tuple.Left.Elements, tuple.Left.Index, compilationData);
                    return parser.Parse(token);
                })
                .WithTrackingName("Create");

            context.RegisterSourceOutput(createCalls, static (context, createCalls) =>
            {
                if (createCalls.Diagnostics is ImmutableArray<Diagnostic> diagnostics)
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                }

                // TODO: Don't actually need a list here
                var members = new List<MemberDeclarationSyntax>();

                string? pretendName = null;
                if (createCalls.Emitter is CreateEmitter emitter)
                {
                    pretendName ??= emitter.Operation.TargetMethod.ReturnType.ToPretendName();
                    members.Add(emitter.Emit(context.CancellationToken));
                }

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

        internal sealed class CompilationData
        {
            public bool LanguageVersionIsSupported { get; }
            public KnownTypeSymbols? TypeSymbols { get; }

            public CompilationData(CSharpCompilation compilation)
            {
                // We don't have a CSharp12 value available yet. Polyfill the value here for forward compat, rather than use the LanguageVersion.Preview enum value.
                // https://github.com/dotnet/roslyn/blob/168689931cb4e3150641ec2fb188a64ce4b3b790/src/Compilers/CSharp/Portable/LanguageVersion.cs#L218-L232
                const int LangVersion_CSharp12 = 1200;
                LanguageVersionIsSupported = (int)compilation.LanguageVersion >= LangVersion_CSharp12;

                if (LanguageVersionIsSupported)
                {
                    TypeSymbols = new KnownTypeSymbols(compilation);
                }
            }
        }
    }
}
