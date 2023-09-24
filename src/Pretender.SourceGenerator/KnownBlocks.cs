﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pretender.SourceGenerator
{
    internal static class KnownBlocks
    {
        private static readonly AssemblyName s_assemblyName = typeof(KnownBlocks).Assembly.GetName();
        private static readonly string GeneratedCodeAnnotationString = $@"[GeneratedCode(""{s_assemblyName.Name}"", ""{s_assemblyName.Version}"")]";
        public static MemberDeclarationSyntax InterceptsLocationAttribute { get; } = ((CompilationUnitSyntax)SyntaxFactory.ParseSyntaxTree($$"""
            namespace System.Runtime.CompilerServices
            {
                using System;
                using System.CodeDom.Compiler;

                {{GeneratedCodeAnnotationString}}
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                file sealed class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(string filePath, int line, int column)
                    {
                    }
                }
            }
            """).GetRoot()).Members[0];

        public static NamespaceDeclarationSyntax OurNamespace { get; }
            = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName("Pretender.SourceGeneration"));

        public static UsingDirectiveSyntax PretenderUsing { get; }
            = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Pretender"));

        public static UsingDirectiveSyntax PretenderInternalsUsing { get; }
            = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Pretender.Internals"));

        public static UsingDirectiveSyntax CompilerServicesUsing { get; }
            = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.CompilerServices"));
    }
}