using Pretender.Behaviors;

namespace Pretender
{
    public static class PretendSetupExtensions
    {
        public static Pretend<T> Returns<T, TResult>(this IPretendSetup<T, Task<TResult>> pretendSetup, TResult result)
        {
            pretendSetup.SetBehavior(new ReturnValueBehavior(Task.FromResult(result)));
            return pretendSetup.Pretend;
        }

        public static Pretend<T> Returns<T, TResult>(this IPretendSetup<T, TResult> pretendSetup, TResult value)
            where T : class
        {
            // TODO: Should we have a generic ReturnValueBehavior?
            pretendSetup.SetBehavior(new ReturnValueBehavior(value));
            return pretendSetup.Pretend;
        }

        public static Pretend<T> Throws<T, TException>(this IPretendSetup<T> pretendSetup, TException exception)
            where TException : Exception
        {
            pretendSetup.SetBehavior(new ThrowBehavior(exception));
            return pretendSetup.Pretend;
        }

        public static Pretend<T> Callback<T>(this IPretendSetup<T> pretendSetup, Action<CallInfo> callback)
        {
            pretendSetup.SetBehavior(new CallbackBehavior(callback));
            return pretendSetup.Pretend;
        }

        public static Pretend<T> Does<T, T1>(this IPretendSetup<T> pretendSetup, Action<T1> callback)
        {
            pretendSetup.SetBehavior(new CallbackBehavior(callInfo =>
            {
                var firstArg = (T1)callInfo.Arguments[0]!;
                callback(firstArg);
            }));
            return pretendSetup.Pretend;
        }
    }
}