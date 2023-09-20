using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Pretender;
using Pretender.SourceGenerator;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace SourceGeneratorTests
{
    public static class TestHelper
    {
        internal static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Preview)
                    .WithFeatures(new[] { new KeyValuePair<string, string>("InterceptorsPreview", "") });

        public static (IEnumerable<string> NewSyntax, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string source)
        {
            var compilation = GetCompilation(source);


            // Create an instance of our EnumGenerator incremental source generator
            var generator = new PretenderSourceGenerator();

            // The GeneratorDriver is used to run our generator against a compilation
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
                .RunGeneratorsAndUpdateCompilation(compilation, 
                    out var outputCompilation,
                    out var diagnostics);

            return (outputCompilation.SyntaxTrees.Skip(1).Select(st => st.ToString()), diagnostics);
            
        }

        internal static Project CreateProject()
        {
            var projectName = $"TestProject-{Guid.NewGuid()}";
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable);

            var project = new AdhocWorkspace().CurrentSolution
                .AddProject(projectName, projectName, LanguageNames.CSharp)
                .WithCompilationOptions(compilationOptions)
                .WithParseOptions(ParseOptions);

            var dotNetDir = Path.GetDirectoryName(typeof(object).Assembly.Location);

            project = project
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(Pretend).Assembly.Location))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddMetadataReference(MetadataReference.CreateFromFile(Path.Join(dotNetDir, "System.Runtime.dll")))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));

            return project;
        }

        private static Task<Compilation> CreateCompilationAsync(string source)
        {
            source = GetTestMethodString(source);
            var project = CreateProject()
                .AddDocument("MyTest.cs", SourceText.From(source, Encoding.UTF8))
                .Project;

            return project.GetCompilationAsync()!;
        }

        public static async Task<(GeneratorRunResult, Compilation)> RunGeneratorAsync(string source)
        {
            var compilation = await CreateCompilationAsync(source);

            var generator = new PretenderSourceGenerator().AsSourceGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
                {
                    generator,
                },
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
                parseOptions: ParseOptions);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

            var runResult = driver.GetRunResult();

            return (Assert.Single(runResult.Results), updatedCompilation);
        }

        private static string GetTestMethodString(string source) => $$"""
            #nullable enable
            using System;
            using Pretender;

            public class TestClass
            {
               public void SyncTestMethod()
               {
                   {{source}}
               }
            }

            /// <summary>
            /// My Information
            /// </summary>
            public interface ISimpleInterface
            {
                string Greeting(string name, int hello);
                void VoidMethod(string name);
            }
            """;

        public static CSharpCompilation GetCompilation(string source)
        {
            // Parse the provided string into a C# syntax tree
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            // Create a Roslyn compilation for the syntax tree.
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "Tests",
                syntaxTrees: new[] { syntaxTree },
                references: new[] { MetadataReference.CreateFromFile(typeof(Pretend).Assembly.Location) });

            return compilation;
        }

        public static SyntaxNode GetSyntax(string source)
        {
            return CSharpSyntaxTree.ParseText(source).GetRoot();
        }
    }
}
