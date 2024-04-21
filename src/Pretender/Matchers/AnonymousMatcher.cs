namespace Pretender.Matchers
{
    public readonly struct AnonymousMatcher<T> : IMatcher
    {
        private readonly Func<T?, bool> _matcher;

        public AnonymousMatcher(Func<T?, bool> matcher)
        {
            _matcher = matcher;
        }

        public bool Matches(T? argument)
        {
            return _matcher(argument);
        }

        bool IMatcher.Matches(object? argument)
        {
            return _matcher((T)argument!);
        }
    }
}