using System.ComponentModel;
using System.Reflection;

namespace Pretender.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
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

            if (!_matcher(callInfo, _target))
            {
                return;
            }

            // TODO: Mark as matched
            // TODO: Set times matched?
        }
    }
}
