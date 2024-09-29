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
        public InterceptsLocationAttribute(string filePath, int line, int column)
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

    file class PretendISimpleInterface : global::ISimpleInterface
    {
        public static readonly MethodInfo Foo_MethodInfo = typeof(PretendISimpleInterface).GetMethod(nameof(Foo))!;
        public static readonly MethodInfo VoidMethod_MethodInfo = typeof(PretendISimpleInterface).GetMethod(nameof(VoidMethod))!;
        public static readonly MethodInfo AsyncMethod_MethodInfo = typeof(PretendISimpleInterface).GetMethod(nameof(AsyncMethod))!;
        public static readonly MethodInfo AsyncReturningMethod_MethodInfo = typeof(PretendISimpleInterface).GetMethod(nameof(AsyncReturningMethod))!;
        public static readonly MethodInfo TryParse_MethodInfo = typeof(PretendISimpleInterface).GetMethod(nameof(TryParse))!;
        public static readonly MethodInfo get_Bar_MethodInfo = typeof(PretendISimpleInterface).GetProperty(nameof(Bar))!.GetMethod;
        public static readonly MethodInfo set_Bar_MethodInfo = typeof(PretendISimpleInterface).GetProperty(nameof(Bar))!.SetMethod;

        private readonly ICallHandler _callHandler;

        public PretendISimpleInterface(ICallHandler callHandler)
        {
            _callHandler = callHandler;
        }

        public string? Foo(string? bar, int baz)
        {
            object?[] __arguments__ = [bar, baz];
            var __callInfo__ = new CallInfo(Foo_MethodInfo, __arguments__);
            _callHandler.Handle(__callInfo__);
            return (string?)__callInfo__.ReturnValue;
        }

        public void VoidMethod(bool baz)
        {
            object?[] __arguments__ = [baz];
            var __callInfo__ = new CallInfo(VoidMethod_MethodInfo, __arguments__);
            _callHandler.Handle(__callInfo__);
        }

        public global::System.Threading.Tasks.Task AsyncMethod()
        {
            object?[] __arguments__ = [];
            var __callInfo__ = new CallInfo(AsyncMethod_MethodInfo, __arguments__);
            _callHandler.Handle(__callInfo__);
            return (global::System.Threading.Tasks.Task)__callInfo__.ReturnValue;
        }

        public global::System.Threading.Tasks.Task<string> AsyncReturningMethod(string bar)
        {
            object?[] __arguments__ = [bar];
            var __callInfo__ = new CallInfo(AsyncReturningMethod_MethodInfo, __arguments__);
            _callHandler.Handle(__callInfo__);
            return (global::System.Threading.Tasks.Task<string>)__callInfo__.ReturnValue;
        }

        public bool TryParse(string thing, out bool myValue)
        {
            object?[] __arguments__ = [thing, myValue];
            var __callInfo__ = new CallInfo(TryParse_MethodInfo, __arguments__);
            _callHandler.Handle(__callInfo__);
            myValue = __arguments__[1];
            return (bool)__callInfo__.ReturnValue;
        }

        public string Bar
        {
            get
            {
                object?[] __arguments__ = [];
                var __callInfo__ = new CallInfo(get_Bar_MethodInfo, __arguments__);
                _callHandler.Handle(__callInfo__);
                return (string)__callInfo__.ReturnValue;
            }
            set
            {
                object?[] __arguments__ = [value];
                var __callInfo__ = new CallInfo(set_Bar_MethodInfo, __arguments__);
                _callHandler.Handle(__callInfo__);
            }
        }
    }

    file static class SetupInterceptors
    {
        [InterceptsLocation(@"MyTest.cs", 13, 6)]
        internal static IPretendSetup<global::ISimpleInterface, string?> Setup0(this Pretend<global::ISimpleInterface> pretend, Func<global::ISimpleInterface, string?> setupExpression)
        {
            return pretend.GetOrCreateSetup<string?>(0, static (pretend, expr) =>
            {
                Matcher matchCall = (callInfo, setup) =>
                {
                    var bar_arg = (string?)callInfo.Arguments[0];
                    if (bar_arg != "1")
                    {
                        return false;
                    }
                    var baz_arg = (int)callInfo.Arguments[1];
                    if (baz_arg != (int)(1))
                    {
                        return false;
                    }
                    return true;
                };
                return new ReturningCompiledSetup<global::ISimpleInterface, string?>(pretend, PretendISimpleInterface.Foo_MethodInfo, matchCall, expr, defaultValue: default);
            }, setupExpression);
        }
    }

    file static class VerifyInterceptors
    {
        [InterceptsLocation(@"MyTest.cs", 18, 24)]
        internal static void Verify0(this Pretend<global::ISimpleInterface> pretend, Func<global::ISimpleInterface, string?> setupExpression, Called called)
        {
            var setup = pretend.GetOrCreateSetup<string?>(0, static (pretend, expr) =>
            {
                Matcher matchCall = (callInfo, setup) =>
                {
                    var bar_arg = (string?)callInfo.Arguments[0];
                    if (bar_arg != "1")
                    {
                        return false;
                    }
                    var baz_arg = (int)callInfo.Arguments[1];
                    if (baz_arg != (int)(1))
                    {
                        return false;
                    }
                    return true;
                };
                return new ReturningCompiledSetup<global::ISimpleInterface, string?>(pretend, PretendISimpleInterface.Foo_MethodInfo, matchCall, expr, defaultValue: default);
            }, setupExpression);
            setup.Verify(called);
        }
    }

    file static class CreateInterceptors
    {
        [InterceptsLocation(@"MyTest.cs", 16, 38)]
        internal static global::ISimpleInterface Create0(this Pretend<global::ISimpleInterface> pretend)
        {
            return new PretendISimpleInterface(pretend);
        }
    }
}