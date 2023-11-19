namespace Pretender.Matchers
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Matchers don't actually need to implement this interface, matchers are used by duck-typing.
    /// So as long as they implement a `Matches` method taking one argument and returning a bool it will be used.
    /// </remarks>
    public interface IMatcher
    {
        bool Matches(object? argument);
    }
}