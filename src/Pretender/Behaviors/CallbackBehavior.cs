namespace Pretender.Behaviors
{
    internal class CallbackBehavior : Behavior
    {
        private readonly Action<CallInfo> _action;

        public CallbackBehavior(Action<CallInfo> action)
        {
            _action = action;
        }

        public override void Execute(CallInfo callInfo)
        {
            _action(callInfo);
        }
    }
}
