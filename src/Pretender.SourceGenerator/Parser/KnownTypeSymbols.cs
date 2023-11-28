using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Pretender.SourceGenerator.Parser
{
    internal sealed class KnownTypeSymbols
    {
        public CSharpCompilation Compilation { get; }

        // INamedTypeSymbols
        public INamedTypeSymbol? Pretend { get; }
        public INamedTypeSymbol? Pretend_Unbound { get; }

        public INamedTypeSymbol String { get; }
        public INamedTypeSymbol? Task { get; }
        public INamedTypeSymbol? TaskOfT_Unbound { get; }
        public INamedTypeSymbol? ValueTask { get; }
        public INamedTypeSymbol? ValueTaskOfT_Unbound { get; }



        public KnownTypeSymbols(CSharpCompilation compilation)
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
