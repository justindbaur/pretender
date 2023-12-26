using System.ComponentModel;

namespace Pretender.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This method is only meant to be used by source generators")]
    public static class Cache
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is only meant to be used by source generators")]
        public static readonly Matcher NoOpMatcher = delegate (CallInfo callInfo, object? target)
        {
            return true;
        };
    }
}