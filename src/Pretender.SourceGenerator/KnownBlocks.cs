using System.Reflection;

namespace Pretender.SourceGenerator
{
    internal static class KnownBlocks
    {
        private static readonly AssemblyName s_assemblyName = typeof(KnownBlocks).Assembly.GetName();
        private static readonly string GeneratedCodeAnnotationString = $@"[GeneratedCode(""{s_assemblyName.Name}"", ""{s_assemblyName.Version}"")]";
        public static string InterceptsLocationAttribute { get; } = $$"""
            namespace System.Runtime.CompilerServices
            {
                using System;
                using System.CodeDom.Compiler;

                {{GeneratedCodeAnnotationString}}
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                file sealed class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(int version, string data)
                    {
                    }
                }
            }
            """;
    }
}