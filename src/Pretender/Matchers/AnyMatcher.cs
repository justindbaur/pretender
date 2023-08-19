namespace Pretender.Matchers
{
    internal class AnyMatcher : IMatcher
    {
        internal static AnyMatcher Instance = new AnyMatcher();

        public bool Matches(object? argument)
        {
            return true;
        }
    }
}