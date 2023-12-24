using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Pretender.SourceGenerator.Parser
{
    internal sealed class KnownTypeSymbols
    {
        private readonly ConcurrentDictionary<INamedTypeSymbol, Dictionary<IMethodSymbol, MethodStrategy>> _cachedTypeMethodNames = new(SymbolEqualityComparer.Default);

        public Compilation Compilation { get; }

        // INamedTypeSymbols
        public INamedTypeSymbol? Pretend { get; }
        public INamedTypeSymbol? Pretend_Unbound { get; }
        public INamedTypeSymbol? AnyMatcher { get; }

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
            AnyMatcher = compilation.GetTypeByMetadataName("Pretender.Matchers.AnyMatcher");


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

        public MethodStrategy GetSingleMethodStrategy(IMethodSymbol method)
        {
            return GetTypesStrategies(method.ContainingType)[method];
        }

        public IReadOnlyDictionary<IMethodSymbol, MethodStrategy> GetTypesStrategies(INamedTypeSymbol type)
        {
            return _cachedTypeMethodNames.GetOrAdd(
                type,
                static (type) =>
                {
                    Dictionary<IMethodSymbol, MethodStrategy> methodDictionary = new(SymbolEqualityComparer.Default);
                    var groupedByNameMethods = type.GetApplicableMethods()
                        .GroupBy(m => m.Name);

                    foreach (var groupedByNameMethod in groupedByNameMethods)
                    {
                        var methods = groupedByNameMethod.ToArray();
                        if (methods.Length == 1)
                        {
                            methodDictionary.Add(methods[0], new ByNameMethodStrategy(methods[0]));
                            continue;
                        }

                        // More than on method has this name, next try number of arguments
                    }

                    return methodDictionary;
                }
            );
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
