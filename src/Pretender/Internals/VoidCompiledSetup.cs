using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Pretender.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This method is only meant to be used by source generators")]
    public class VoidCompiledSetup<T>(Pretend<T> pretend, MethodInfo methodInfo, Matcher matcher, object? target)
        : BaseCompiledSetup<T>(pretend, methodInfo, matcher, target), IPretendSetup<T>
    {
        [DebuggerStepThrough]
        public void Execute(CallInfo callInfo)
        {
            ExecuteCore(callInfo);

            // Run behavior
            if (_behavior is null)
            {
                return;
            }

            // For void returning we just run the behavior
            _behavior.Execute(callInfo);
        }
    }
}
