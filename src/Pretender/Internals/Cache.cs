using System.ComponentModel;

namespace Pretender.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    // TODO: Obsolete
    public static class Cache
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Matcher NoOpMatcher = delegate (CallInfo callInfo, object? target)
        {
            return true;
        };
    }
}
