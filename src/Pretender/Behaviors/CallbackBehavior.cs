namespace Pretender.Behaviors
{
    public delegate void Callback(ref CallInfo callInfo);

    internal class CallbackBehavior : Behavior
    {
        private readonly Callback _action;

        public CallbackBehavior(Callback action)
        {
            _action = action;
        }

        public override void Execute(CallInfo callInfo)
        {
            _action(ref callInfo);
        }
    }
}
