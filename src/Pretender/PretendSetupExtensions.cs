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

        public static Pretend<T> Callback<T>(this IPretendSetup<T> pretendSetup, Callback callback)
        {
            pretendSetup.SetBehavior(new CallbackBehavior(callback));
            return pretendSetup.Pretend;
        }
    }
}
