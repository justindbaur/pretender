﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Pretender.SourceGenerator.Parser
{
    internal sealed class KnownTypeSymbols
    {
        public CSharpCompilation Compilation { get; }

        // INamedTypeSymbols
        public INamedTypeSymbol? Pretend { get; }
        public INamedTypeSymbol? Pretend_Unbound { get; }

        public KnownTypeSymbols(CSharpCompilation compilation)
        {
            Compilation = compilation;

            // TODO: Get known types
            Pretend = compilation.GetTypeByMetadataName("Pretender.Pretend`1");
            Pretend_Unbound = Pretend?.ConstructUnboundGenericType();
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