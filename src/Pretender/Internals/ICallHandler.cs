using System.ComponentModel;

namespace Pretender.Internals
{
    /// <summary>
    /// **FOR INTERNAL USE ONLY**
    /// </summary>
    public interface ICallHandler
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is only meant to be used by source generators")]
        void Handle(CallInfo callInfo);
    }
}
