namespace Pretender.Matchers
{
    public interface IMatcher
    {
        bool Matches(object? argument);
    }
}