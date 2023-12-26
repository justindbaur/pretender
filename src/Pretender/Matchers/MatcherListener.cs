using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Pretender.Matchers
{
    public sealed class MatcherListener : IDisposable
    {
        [ThreadStatic]
        private static Stack<MatcherListener>? s_listeners;

        public static MatcherListener StartListening()
        {
            var listener = new MatcherListener();
            var listeners = s_listeners;
            if (listeners == null)
            {
                s_listeners = listeners = new Stack<MatcherListener>();
            }

            listeners.Push(listener);

            return listener;
        }

        public static bool IsListening([MaybeNullWhen(false)] out MatcherListener listener)
        {
            var listeners = s_listeners;

            if (listeners != null && listeners.Count > 0)
            {
                listener = listeners.Peek();
                return true;
            }

            listener = null;
            return false;
        }

        private List<IMatcher>? _matchers;

        public void OnMatch(IMatcher matcher)
        {
            if (_matchers == null)
            {
                _matchers = [];
            }

            _matchers.Add(matcher);
        }

        public IEnumerable<IMatcher> GetMatchers()
        {
            if (_matchers == null)
            {
                return [];
            }

            return _matchers;
        }

        public void Dispose()
        {
            var listeners = s_listeners;
            Debug.Assert(listeners != null && listeners.Count > 0);
            listeners.Pop();
        }
    }
}