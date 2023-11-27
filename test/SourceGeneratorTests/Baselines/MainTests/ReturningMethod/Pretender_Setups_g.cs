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
    using System;
    using System.Runtime.CompilerServices;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Pretender;
    using Pretender.Internals;

    file static class SetupInterceptors
    {
        [InterceptsLocation("MyTest.cs", 13, 6)]
        internal static IPretendSetup<global::ISimpleInterface, string> Setup0(this Pretend<global::ISimpleInterface> pretend, Func<global::ISimpleInterface, string> setupExpression)
        {
            return pretend.GetOrCreateSetup<string>(0, static (pretend, expr) =>
            {
                return new ReturningCompiledSetup<global::ISimpleInterface, string>(pretend, PretendISimpleInterface19445CC.MethodInfo_get_Bar_2968A4B, matcher: Cache.NoOpMatcher, expr.Target, defaultValue: default);
            }, setupExpression);
        }
    }
}