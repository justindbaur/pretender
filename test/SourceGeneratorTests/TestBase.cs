using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Pretender;
using Pretender.SourceGenerator;

namespace SourceGeneratorTests
{
    public abstract class TestBase
    {
        // TODO: Will need to update this for Interceptors namespace change
        // all interceptors should exist in Pretender.SourceGeneration
        internal static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Preview)
            .WithFeatures(new[] { new KeyValuePair<string, string>("InterceptorsPreview", "") });

        internal static readonly Project BaseProject = CreateProject();

        private const string Base = $"""
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using Pretender;

            """;

        public virtual string CreateTestTemplate(string source) => $$"""
                
                public class TestClass
                {
                    public void SyncTestMethod()
                    {
                        {{source}}
                    }
                }

                public interface ISimpleInterface
                {
                    string? Foo(string? bar, int baz);
                    void VoidMethod(bool baz);
                    Task AsyncMethod();
                    Task<string> AsyncReturningMethod(string bar);
                    bool TryParse(string thing, out bool myValue);
                    string Bar { get; set; }
                }

                public abstract class SimpleAbstractClass
                {
                    private readonly string _arg;

                    public SimpleAbstractClass(string arg)
                    {
                        _arg = arg;
                    }

                    public string? Foo(string? bar, int baz)
                    {
                        return _arg;
                    }
                    public abstract void VoidMethod(bool baz);
                    public abstract Task AsyncMethod();
                    public abstract Task<string> AsyncReturningMethod(string bar);
                }
                """;

        public async Task<(GeneratorRunResult GeneratorResult, Compilation UpdateCompilation)> RunGeneratorAsync(string source)
        {
            var compilation = await CreateCompilationAsync(source);

            var generator = new PretenderSourceGenerator().AsSourceGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [generator],
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
                parseOptions: ParseOptions
            );

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updateCompilation, out var diagnostics);

            var runResult = driver.GetRunResult();

            return (Assert.Single(runResult.Results), updateCompilation);
        }

        public async Task RunAndCompareAsync(string source, [CallerMemberName] string testMethodName = null!)
        {
            var (result, _) = await RunGeneratorAsync(source);
            Assert.All(result.GeneratedSources, s =>
            {
                CompareAgainstBaseline(s, testMethodName);
            });
        }

        private void CompareAgainstBaseline(GeneratedSourceResult result, string testMethodName = null!)
        {
            var normalizedName = result.HintName[..^3].Replace('.', '_') + ".cs";
#if !GENERATE_SOURCE
            var resultFileName = result.HintName.Replace('.', '_');
            var baseLineName = $"{GetType().Name}.{testMethodName}.{normalizedName}";
            var resourceName = typeof(TestBase).Assembly.GetManifestResourceNames()
                .Single(r => r.EndsWith(baseLineName));

            using var stream = typeof(TestBase).Assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            Assert.Equal(reader.ReadToEnd(), result.SourceText.ToString());
#else
            var baseDirectory = new DirectoryInfo(typeof(TestBase).Assembly.GetAssemblyLocation())
                .Parent?.Parent?.Parent?.Parent;

            if (baseDirectory == null || !baseDirectory.Exists)
            {
                throw new Exception("Could not find directory.");
            }

            var baselinePath = Path.Combine(
                baseDirectory.FullName,
                "Baselines",
                GetType().Name,
                testMethodName,
                normalizedName);

            File.WriteAllText(baselinePath, result.SourceText.ToString());
#endif
        }

        private Task<Compilation> CreateCompilationAsync(string source)
        {
            var fullText = Base + CreateTestTemplate(source);
            var project = BaseProject
                .AddDocument("MyTest.cs", SourceText.From(fullText, Encoding.UTF8))
                .Project;

            return project.GetCompilationAsync()!;
        }

        private static Project CreateProject()
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
    }
}
