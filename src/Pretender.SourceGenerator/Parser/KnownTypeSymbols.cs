using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Pretender.SourceGenerator.Parser
{
    internal sealed class KnownTypeSymbols
    {
        public Compilation Compilation { get; }

        // INamedTypeSymbols
        public INamedTypeSymbol? Pretend { get; }
        public INamedTypeSymbol? Pretend_Unbound { get; }

        public INamedTypeSymbol String { get; }
        public INamedTypeSymbol? Task { get; }
        public INamedTypeSymbol? TaskOfT_Unbound { get; }
        public INamedTypeSymbol? ValueTask { get; }
        public INamedTypeSymbol? ValueTaskOfT_Unbound { get; }

        // Known abstractions with fakes
        public INamedTypeSymbol? MicrosoftExtensionsLoggingILogger { get; }
        public INamedTypeSymbol? MicrosoftExtensionsLoggingTestingFakeLogger { get; }
        public INamedTypeSymbol? MicrosoftExtensionsLoggingAbstractionsNullLogger {  get; }

        public INamedTypeSymbol? MicrosoftExtensionsLoggingILoggerOfT { get; }



        public KnownTypeSymbols(Compilation compilation)
        {
            Compilation = compilation;

            // TODO: Get known types
            Pretend = compilation.GetTypeByMetadataName("Pretender.Pretend`1");
            Pretend_Unbound = Pretend?.ConstructUnboundGenericType();

            String = compilation.GetSpecialType(SpecialType.System_String);
            Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            TaskOfT_Unbound = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")?.ConstructUnboundGenericType();
            ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            ValueTaskOfT_Unbound = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1")?.ConstructUnboundGenericType();

            // Fakes
            // ILogger
            MicrosoftExtensionsLoggingILogger = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
            MicrosoftExtensionsLoggingTestingFakeLogger = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.Testing.FakeLogger");
            MicrosoftExtensionsLoggingAbstractionsNullLogger = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.Abstractions.NullLogger");

            // ILogger<T>
            MicrosoftExtensionsLoggingILoggerOfT = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger`1");
            MicrosoftExtensionsLoggingTestingFakeLogger = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.Testing.FakeLogger`1");
            MicrosoftExtensionsLoggingAbstractionsNullLogger = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.Abstractions.NullLogger`1");

            // ILoggerProvider

            // TODO: data protection
            // TODO: FakeTimeProvider
        }

        public static bool IsPretend(INamedTypeSymbol type)
        {
            // This should be enough
            return type is
            {
                Name: "Pretend",
                ContainingNamespace:
                {
                    Name: "Pretender",
                    ContainingNamespace.IsGlobalNamespace: true,
                },
                ContainingAssembly.Name: "Pretender",
            };
        }
    }
}
