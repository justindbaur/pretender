namespace Pretender.Behaviors
{
    internal class ThrowBehavior : Behavior
    {
        private readonly Exception _exception;

        public ThrowBehavior(Exception exception)
        {
            _exception = exception;
        }

        public override void Execute(CallInfo callInfo)
        {
            throw _exception;
        }
    }
}