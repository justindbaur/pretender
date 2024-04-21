using Pretender.Internals;

namespace Pretender.Tests.Matchers
{
    public class MatcherListenerTests
    {
        [Fact]
        public void StartListening_ReturnsSameListenerAsIsListening()
        {
            using var startedListener = MatcherListener.StartListening();
            Assert.True(MatcherListener.IsListening(out var listener));
            Assert.Equal(startedListener, listener);
        }

        [Fact]
        public void IsListening_ReturnsFalse_WhenNotStarted()
        {
            Assert.False(MatcherListener.IsListening(out var listener));
            Assert.Null(listener);
        }
    }
}