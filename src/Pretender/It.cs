using Pretender.Internals;
using Pretender.Matchers;

namespace Pretender
{
    public static class It
    {
        [Matcher<AnyMatcher>]
        public static T IsAny<T>()
        {
            // This method is never normally invoked during its normal usage
            return default!;
        }

        [Matcher(typeof(AnonymousMatcher<>))]
        public static T Is<T>(Func<T?, bool> matcher)
        {
            if (MatcherListener.IsListening(out var listener))
            {
                listener.OnMatch(new AnonymousMatcher<T>(matcher));
            }
            return default!;
        }
    }
}