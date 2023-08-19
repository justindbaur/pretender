using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pretender.SourceGenerator
{
    [Generator]
    public class PretenderSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
           var pretendsWithDiagnostics = 
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
                        if (false)
                        {
                            return true;
                        }
                        return false;
                    },
                    transform: static (context, token) =>
                    {
                        var operation = context.SemanticModel.GetOperation(context.Node, token);
                        // TODO: I think this is where I need to filter out false positives
                        if (operation.IsInvocationOperation(out var invocationOperation))
                        {
                            return PretendTypeDiagnostics.FromMethodGeneric(invocationOperation!, context.SemanticModel);
                        }

                        // TODO: Check for constructor invocation operation
                        // and create the PretendEntrypoint with that information
                        return null;
                    })
                    .Where(static p => p != null)
                    .WithTrackingName("FindPretendGenerics");

            context.RegisterSourceOutput(pretendsWithDiagnostics, static (context, pretend) =>
            {
                foreach (var diagnostic in pretend!.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            });

            var pretends = pretendsWithDiagnostics
                .Where(p => p!.Diagnostics.Count == 0)
                .Select((p, _) => new PretendEntrypoint(p))
                .WithTrackingName("PretendsWithoutDiagnostics");

            context.RegisterSourceOutput(pretends, static (context, pretend) =>
            {
                var compilationUnit = pretend!.GetCompilationUnit();

                // TODO: Should have different name per class
                context.AddSource($"Pretender.{pretend.PretendName}.g.cs", compilationUnit.GetText(Encoding.UTF8));
            });
        }
    }
}
