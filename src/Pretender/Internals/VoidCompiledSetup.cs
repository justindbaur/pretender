using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Pretender.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VoidCompiledSetup<T>(Pretend<T> pretend, Action<T> setupExpression, MethodInfo methodInfo, Matcher matcher, object? target)
        : BaseCompiledSetup<T>(pretend, methodInfo, matcher, target), IPretendSetup<T>
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
}
