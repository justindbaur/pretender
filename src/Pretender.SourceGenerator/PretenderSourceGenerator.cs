using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Pretender.SourceGenerator.Emitter;
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
            IncrementalValuesProvider<PretendEntrypoint> pretendsWithDiagnostics =
                 context.SyntaxProvider.CreateSyntaxProvider(
                     predicate: static (node, token) =>
                     {
                         // Pretend.That<T>();
                         if (node is InvocationExpressionSyntax
                             {
                                 Expression: MemberAccessExpressionSyntax
                                 {
                                     // TODO: Will this work with a using static Pretender.Pretend
                                     // ...
                                     // That<IInterface>();
                                     Expression: IdentifierNameSyntax { Identifier.ValueText: "Pretend" },
                                     Name: GenericNameSyntax { Identifier.ValueText: "That", TypeArgumentList.Arguments.Count: 1 },
                                 },
                             })
                         {
                             return true;
                         }

                         // TODO: Allow constructor and shortcut Pretend.Of<T>();
                         return false;
                     },
                     transform: static (context, token) =>
                     {
                         var operation = context.SemanticModel.GetOperation(context.Node, token);
                         // TODO: I think this is where I need to filter out false positives
                         if (operation.IsInvocationOperation(out var invocationOperation))
                         {
                             token.ThrowIfCancellationRequested();

                             return PretendEntrypoint.FromMethodGeneric(invocationOperation!);
                         }

                         // TODO: Check for constructor invocation operation
                         // and create the PretendEntrypoint with that information
                         return null;
                     })
                     .Where(static p => p != null)
                     .WithTrackingName("FindPretendGenerics")!;

            context.RegisterSourceOutput(pretendsWithDiagnostics, static (context, pretend) =>
            {
                foreach (var diagnostic in pretend!.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            });

            var pretends = pretendsWithDiagnostics
                .Where(p => p!.Diagnostics.Count == 0)
                .GroupWith(s => s.InvocationLocation, PretendEntrypointComparer.TypeSymbol);

            context.RegisterSourceOutput(pretends, static (context, pretend) =>
            {
                var compilationUnit = pretend.Source.GetCompilationUnit(context.CancellationToken);
                context.AddSource($"Pretender.Type.{pretend.Source.TypeToPretend.ToPretendName()}.g.cs", compilationUnit.GetText(Encoding.UTF8));
            });
            #endregion

            #region Setup
            var setupCallsWithDiagnostics =
                context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (node, _) => node.IsSetupCall(),
                    transform: static (context, token) =>
                    {
                        // All of this should be asserted in the predicate
                        var operation = context.SemanticModel.GetOperation(context.Node, token);
                        if (operation!.IsValidSetupOperation(context.SemanticModel.Compilation, out var invocation))
                        {
                            return new SetupEntrypoint(invocation!);
                        }
                        return null;
                    })
                .Where(i => i is not null);

            context.RegisterSourceOutput(setupCallsWithDiagnostics, static (context, setup) =>
            {
                foreach (var diagnostic in setup!.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            });

            var setups = setupCallsWithDiagnostics
                .Where(s => s!.Diagnostics.Count == 0);

            context.RegisterSourceOutput(setups.Collect(), static (context, setups) =>
            {
                
                var members = new List<MemberDeclarationSyntax>();

                for (var i = 0; i < setups.Length; i++)
                {
                    var setup = setups[i];
                    members.AddRange(setup!.GetMembers(i));
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

                    return parser.GetVerifyEmitter(cancellationToken);
                })
                .WithTrackingName("Verify");

            // TODO: Register diagnostics
            context.RegisterSourceOutput(verifyCallsWithDiagnostics.Collect(), (context, inputs) =>
            {
                var methods = new List<MethodDeclarationSyntax>();
                for ( var i = 0; i < inputs.Length; i++)
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
                        var method = emitter.EmitVerifyMethod(0, context.CancellationToken);
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
                predicate: (node, token) =>
                {
                    if (node is InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name.Identifier.ValueText: "Create"
                            },
                        }
                    )
                    {
                        return true;
                    }

                    return false;
                },
                transform: (context, token) =>
                {
                    var operation = context.SemanticModel.GetOperation(context.Node);
                    if (operation.IsValidCreateOperation(context.SemanticModel.Compilation, out var invocation, out var typeArguments))
                    {
                        return new CreateEntrypoint(invocation, typeArguments);
                    }

                    return null;
                })
                .Where(i => i is not null)!
                .GroupWith(c => c.Location, CreateEntryPointComparer.Instance);

            context.RegisterSourceOutput(createCalls, static (context, createCalls) =>
            {
                var methodDeclaration = createCalls.Source.GetMethodDeclaration(createCalls.Index)
                    .AddAttributeLists(createCalls.Elements.Select(i => SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(i.ToAttributeSyntax()))).ToArray());

                var createClass = SyntaxFactory.ClassDeclaration("CreateInterceptors")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.FileKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                        .AddMembers(methodDeclaration);

                var createNamespace = KnownBlocks.OurNamespace
                    .AddMembers(createClass)
                    .AddUsings(KnownBlocks.CompilerServicesUsing, KnownBlocks.PretenderUsing);

                var cu = SyntaxFactory.CompilationUnit()
                    .AddMembers(KnownBlocks.InterceptsLocationAttribute, createNamespace)
                    .NormalizeWhitespace();

                context.AddSource($"Pretender.Creates.{createCalls.Source.Operation.TargetMethod.ReturnType.ToPretendName()}.g.cs", cu.GetText(Encoding.UTF8));
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
