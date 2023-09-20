namespace Pretender.Behaviors
{
    internal class ReturnValueBehavior : Behavior
    {
        private readonly object? _value;

        public ReturnValueBehavior(object? value)
        {
            _value = value;
        }
        public override void Execute(ref CallInfo callInfo)
        {
            callInfo.ReturnValue = _value;
        }
    }
}
