namespace Pretender.Matchers
{
    public sealed class AnyMatcher : IMatcher
    {
        public bool Matches(object? argument)
        {
            return true;
        }
    }
}