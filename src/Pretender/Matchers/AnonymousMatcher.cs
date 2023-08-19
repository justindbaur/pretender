namespace Pretender.Matchers
{
    internal class AnonymousMatcher<T> : IMatcher
    {
        private readonly Func<T?, bool> _matcher;

        public AnonymousMatcher(Func<T?, bool> matcher)
        {
            _matcher = matcher;
        }

        public bool Matches(object? argument)
        {
            return _matcher((T?)argument);
        }
    }
}
