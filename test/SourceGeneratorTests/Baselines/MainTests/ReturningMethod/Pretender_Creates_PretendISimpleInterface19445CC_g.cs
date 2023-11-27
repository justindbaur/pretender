namespace System.Runtime.CompilerServices
{
    using System;
    using System.CodeDom.Compiler;

    [GeneratedCode("Pretender.SourceGenerator", "1.0.0.0")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    file sealed class InterceptsLocationAttribute : Attribute
    {
        public InterceptsLocationAttribute(string filePath, int line, int column)
        {
        }
    }
}

namespace Pretender.SourceGeneration
{
    using System.Runtime.CompilerServices;
    using Pretender;

    file static class CreateInterceptors
    {
        [InterceptsLocation("MyTest.cs", 16, 38)]
        internal static global::ISimpleInterface Create0(this Pretend<global::ISimpleInterface> pretend)
        {
            return new PretendISimpleInterface19445CC(pretend);
        }
    }
}