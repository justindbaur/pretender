namespace Pretender.Internals
{
    // TODO: Can I make this a delegate of the unsafe wrapper around this?
    public delegate bool Matcher(CallInfo callInfo, object? target);
}