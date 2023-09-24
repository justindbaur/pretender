using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Pretender.Internals
{
    

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ReturningCompiledSetup<T, TResult>(Pretend<T> pretend, Func<T, TResult> setupExpression, MethodInfo methodInfo, Matcher matcher, object? target, TResult defaultValue)
        : BaseCompiledSetup<T>(pretend, methodInfo, matcher, target), IPretendSetup<T, TResult>
    {
        private readonly Func<T, TResult> _setupExpression = setupExpression;
        private readonly TResult _defaultValue = defaultValue;

        public Type ReturnType => typeof(TResult);

        [DebuggerStepThrough]
        public void Execute(ref CallInfo callInfo)
        {
            ExecuteCore(ref callInfo);

            // Run behavior
            if (_behavior is null)
            {
                callInfo.ReturnValue = _defaultValue;
                return;
            }

            _behavior.Execute(ref callInfo);

            // This is where I could track nullability state and throw if the return value is null still
            callInfo.ReturnValue ??= _defaultValue;
        }
    }
}
