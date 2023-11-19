namespace Pretender.Matchers
{
    public sealed class AnyMatcher : IMatcher
    {
        public static AnyMatcher Instance = new();

        public bool Matches(object? argument)
        {
            return true;
        }
    }
}