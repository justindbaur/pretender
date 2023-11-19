namespace Pretender.Behaviors
{
    internal sealed class ReturnValueBehavior : Behavior
    {
        private readonly object? _value;

        public ReturnValueBehavior(object? value)
        {
            _value = value;
        }
        public override void Execute(CallInfo callInfo)
        {
            callInfo.ReturnValue = _value;
        }
    }
}
