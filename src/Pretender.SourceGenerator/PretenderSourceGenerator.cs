using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Pretender.SourceGenerator
{
    [Generator]
    public class PretenderSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<PretendEntrypoint> pretendsWithDiagnostics =
                 context.SyntaxProvider.CreateSyntaxProvider(
                     predicate: static (node, token) =>
                     {
                         // Pretend.For<T>();
                         if (node is InvocationExpressionSyntax
                             {
                                 Expression: MemberAccessExpressionSyntax
                                 {
                                     // TODO: Will this work with a using static Pretender.Pretend
                                     // ...
                                     // For<IInterface>();
                                     Expression: IdentifierNameSyntax { Identifier.ValueText: "Pretend" },
                                     Name: GenericNameSyntax { Identifier.ValueText: "For", TypeArgumentList.Arguments.Count: 1 },
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
                context.AddSource($"Pretender.Type.{pretend.Source.PretendName}.g.cs", compilationUnit.GetText(Encoding.UTF8));
            });

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
                    members.Add(setup!.GetMethodDeclaration(i));
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

            var createCalls = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, token) =>
                {
                    if (node is InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name.Identifier.ValueText: "Create"
                            },
                            ArgumentList.Arguments.Count: 0
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
                    var invocationOperation = (IInvocationOperation?)operation;

                    if (invocationOperation?.Instance is not null)
                    {
                        // TODO: Do more validation, we should match the type this is being done on.
                        return new CreateEntrypoint(invocationOperation);
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
        }
    }
}
