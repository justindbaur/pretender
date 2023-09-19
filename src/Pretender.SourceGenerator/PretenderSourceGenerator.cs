using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Pretender.SourceGenerator
{
    [Generator]
    public class PretenderSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<PretendEntrypoint> pretendsWithDiagnostics =
                 context.SyntaxProvider.CreateSyntaxProvider(
                     predicate: static (node, _) =>
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
                .Where(p => p!.Diagnostics.Count == 0);

            context.RegisterSourceOutput(pretends.Collect(), static (context, pretends) =>
            {
                var uniquePretends = pretends.ToImmutableHashSet(PretendEntrypointComparer.TypeSymbol);
                foreach (var uniquePretend in uniquePretends)
                {
                    var compilationUnit = uniquePretend.GetCompilationUnit();
                    // TODO: Should have different name per class
                    context.AddSource($"Pretender.Types.{uniquePretend.PretendName}.g.cs", compilationUnit.GetText(Encoding.UTF8));
                }
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
                            // A valid Setup call will have an invocation in the first arg
                            var firstArg = invocation!.Arguments[0];
                            return new SetupEntrypoint(invocation);
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
                    members.Add(setup!.GetMatcherDeclaration(i));
                }

                var classDeclaration = SyntaxFactory.ClassDeclaration("SetupInterceptors")
                    .WithModifiers(SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                    .AddMembers([.. members]);

                var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("Pretender.SourceGeneration"))
                    .AddMembers(classDeclaration)
                    .AddUsings(
                        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.CompilerServices")),
                        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq.Expressions")),
                        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Pretender"))
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
                        return invocationOperation.TargetMethod;
                    }

                    return null;

                })
                .Where(i => i is not null);

            context.RegisterSourceOutput(createCalls.Collect(), static (context, createCalls) =>
            {
                context.AddSource("Pretender.Creates.g.cs", SourceText.From("// hi", Encoding.UTF8));
            });
        }
    }
}
