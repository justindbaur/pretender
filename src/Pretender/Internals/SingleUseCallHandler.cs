using System.ComponentModel;

namespace Pretender.Internals
{
    /// <summary>
    /// **FOR INTERNAL USE ONLY**
    /// </summary>
    public class SingleUseCallHandler : ICallHandler
    {
        public object?[] Arguments { get; private set; } = null!;

        /// <summary>
        /// **FOR INTERNAL USE ONLY** Method signature subject to change.
        /// </summary>
        /// <param name="callInfo"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is only meant to be used by source generators")]
        public void Handle(CallInfo callInfo)
        {
            Arguments = callInfo.Arguments;
        }
    }
}
