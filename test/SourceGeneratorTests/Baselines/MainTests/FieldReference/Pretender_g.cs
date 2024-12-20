// <auto-generated>

#nullable enable annotations
#nullable disable warnings

// Suppress warnings about [Obsolete] member usage in generated code.
#pragma warning disable CS0612, CS0618

namespace System.Runtime.CompilerServices
{
    using System;
    using System.CodeDom.Compiler;

    [GeneratedCode("Pretender.SourceGenerator", "1.0.0.0")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    file sealed class InterceptsLocationAttribute : Attribute
    {
        public InterceptsLocationAttribute(int version, string data)
        {
        }
    }
}

namespace Pretender.SourceGeneration
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Pretender;
    using Pretender.Internals;

    file class PretendITest : global::FieldReference.ITest
    {
        public static readonly MethodInfo Method_MethodInfo = typeof(PretendITest).GetMethod(nameof(Method))!;

        private readonly ICallHandler _callHandler;

        public PretendITest(ICallHandler callHandler)
        {
            _callHandler = callHandler;
        }

        public void Method(string arg)
        {
            object?[] __arguments__ = [arg];
            var __callInfo__ = new CallInfo(Method_MethodInfo, __arguments__);
            _callHandler.Handle(__callInfo__);
        }
    }

    file static class SetupInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "xbIAQFg+yi3VoCyQ0Vr0a1IBAABNeVRlc3QuY3M=")]
        internal static IPretendSetup<global::FieldReference.ITest> Setup0(this Pretend<global::FieldReference.ITest> pretend, Action<global::FieldReference.ITest> setupExpression)
        {
            return pretend.GetOrCreateSetup(0, static (pretend, expr) =>
            {
                Matcher matchCall = (callInfo, setup) =>
                {
                    var singleUseCallHandler = new SingleUseCallHandler();
                    var fake = new PretendITest(singleUseCallHandler);

                    var listener = MatcherListener.StartListening();
                    try
                    {
                        setup.Method.Invoke(setup.Target, [fake]);
                    }
                    finally
                    {
                        listener.Dispose();
                    }

                    var capturedArguments = singleUseCallHandler.Arguments;

                    var arg_arg = (string)callInfo.Arguments[0];
                    var arg_capturedArg = (string)capturedArguments[0];
                    if (arg_arg != arg_capturedArg)
                    {
                        return false;
                    }
                    return true;
                };
                return new VoidCompiledSetup<global::FieldReference.ITest>();
            }, setupExpression);
        }
    }

    file static class VerifyInterceptors
    {
    }

    file static class CreateInterceptors
    {
    }
}