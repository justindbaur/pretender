namespace Pretender.Matchers
{
    public readonly struct AnonymousMatcher<T>
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
    }
}