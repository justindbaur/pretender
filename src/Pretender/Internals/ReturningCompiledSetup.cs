﻿using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Pretender.Internals
{


    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This method is only meant to be used by source generators")]
    public class ReturningCompiledSetup<T, TResult>(Pretend<T> pretend, MethodInfo methodInfo, Matcher matcher, Delegate setup, TResult defaultValue)
        : BaseCompiledSetup<T>(pretend, methodInfo, matcher, setup), IPretendSetup<T, TResult>
    {
        private readonly TResult _defaultValue = defaultValue;

        public Type ReturnType => typeof(TResult);

        [DebuggerStepThrough]
        public void Execute(CallInfo callInfo)
        {
            var matched = ExecuteCore(callInfo);

            if (!matched)
            {
                callInfo.ReturnValue ??= _defaultValue;
                return;
            }

            // Run behavior
            if (_behavior is null)
            {
                callInfo.ReturnValue = _defaultValue;
                return;
            }

            _behavior.Execute(callInfo);

            // This is where I could track nullability state and throw if the return value is null still
            callInfo.ReturnValue ??= _defaultValue;
        }
    }
}