using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using Pretender.Behaviors;

namespace Pretender
{
    public delegate bool Matcher(CallInfo callInfo);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class BaseCompiledSetup<T>(Pretend<T> pretend, MethodInfo methodInfo, Matcher matcher)
    {
        private readonly MethodInfo _methodInfo = methodInfo;
        private readonly Matcher _matcher = matcher;

        protected Behavior? _behavior;

        public Pretend<T> Pretend { get; } = pretend;

        public void SetBehavior(Behavior behavior)
        {
            if (_behavior != null)
            {
                throw new InvalidOperationException("You can't set multiple behaviors");
            }

            _behavior = behavior;
        }

        public void ExecuteCore(ref CallInfo callInfo)
        {
            // TODO: Mark as attempted?
            if (callInfo.MethodInfo != _methodInfo)
            {
                return;
            }

            if (!_matcher(callInfo))
            {
                return;
            }

            // TODO: Mark as matched
            // TODO: Set times matched?
        }
    }


    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VoidCompiledSetup<T>(Pretend<T> pretend, Action<T> setupExpression, MethodInfo methodInfo, Matcher matcher)
        : BaseCompiledSetup<T>(pretend, methodInfo, matcher), IPretendSetup<T>
    {
        private readonly Action<T> _setupExpression = setupExpression;

        [DebuggerStepThrough]
        public void Execute(ref CallInfo callInfo)
        {
            ExecuteCore(ref callInfo);

            // Run behavior
            if (_behavior is null)
            {
                return;
            }

            // For void returning we just run the behavior
            _behavior.Execute(ref callInfo);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ReturningCompiledSetup<T, TResult>(Pretend<T> pretend, Func<T, TResult> setupExpression, MethodInfo methodInfo, Matcher matcher, TResult defaultValue)
        : BaseCompiledSetup<T>(pretend, methodInfo, matcher), IPretendSetup<T, TResult>
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
