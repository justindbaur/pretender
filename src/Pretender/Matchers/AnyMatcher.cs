namespace Pretender.Matchers
{
    public class AnyMatcher : IMatcher
    {
        public static AnyMatcher Instance = new AnyMatcher();

        public bool Matches(object? argument)
        {
            return true;
        }
    }
}