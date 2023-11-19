using System.ComponentModel;
using System.Reflection;

namespace Pretender.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    // TODO: Obsolete this
    public abstract class BaseCompiledSetup<T>(
        Pretend<T> pretend,
        MethodInfo methodInfo,
        Matcher matcher,
        object? target)
    {
        private readonly MethodInfo _methodInfo = methodInfo;
        private readonly Matcher _matcher = matcher;
        private readonly object? _target = target;
        protected Behavior? _behavior;

        public Pretend<T> Pretend { get; } = pretend;
        public int TimesCalled { get; private set; }

        public void SetBehavior(Behavior behavior)
        {
            if (_behavior != null)
            {
                throw new InvalidOperationException("You can't set multiple behaviors");
            }

            _behavior = behavior;
        }

        public void ExecuteCore(CallInfo callInfo)
        {
            if (!Matches(callInfo))
            {
                return;
            }
            TimesCalled++;
        }

        public bool Matches(CallInfo callInfo)
        {
            // TODO: Mark as attempted?
            if (callInfo.MethodInfo != _methodInfo)
            {
                return false;
            }

            if (!_matcher(callInfo, _target))
            {
                return false;
            }

            return true;
        }
    }
}
