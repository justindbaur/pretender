using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Pretender;
using Pretender.SourceGenerator;
using System.Collections.Immutable;

namespace SourceGeneratorTests
{
    public static class TestHelper
    {
        public static (IEnumerable<string> NewSyntax, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string source)
        {
            // Parse the provided string into a C# syntax tree
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            // Create a Roslyn compilation for the syntax tree.
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "Tests",
                syntaxTrees: new[] { syntaxTree },
                references: new[] { MetadataReference.CreateFromFile(typeof(Pretend).Assembly.Location)});


            // Create an instance of our EnumGenerator incremental source generator
            var generator = new PretenderSourceGenerator();

            // The GeneratorDriver is used to run our generator against a compilation
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
                .RunGeneratorsAndUpdateCompilation(compilation, 
                    out var outputCompilation,
                    out var diagnostics);

            return (outputCompilation.SyntaxTrees.Skip(1).Select(st => st.ToString()), diagnostics);
            
        }

        public static SyntaxNode GetSyntax(string source)
        {
            return CSharpSyntaxTree.ParseText(source).GetRoot();
        }
    }
}
